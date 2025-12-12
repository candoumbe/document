using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Documents.API.IntegrationTests.Fixtures;

public static class DistributedApplicationExtensions
{
    private static readonly Faker s_faker = new();

    /// <summary>
    /// Ensures all parameters in the application configuration have values set.
    /// </summary>
    public static TBuilder WithRandomParameterValues<TBuilder>(this TBuilder builder)
        where TBuilder : IDistributedApplicationTestingBuilder
    {
        List<ParameterResource> parameters = builder.Resources.OfType<ParameterResource>().Where(p => !p.IsConnectionString).ToList();
        foreach (ParameterResource parameter in parameters)
        {
            builder.Configuration[$"Parameters:{parameter.Name}"] = parameter.Secret
                                                                        ? s_faker.Internet.Password()
                                                                        : Convert.ToHexString(RandomNumberGenerator.GetBytes(4));
        }

        return builder;
    }

    /// <summary>
    /// Waits for all resources in the application to reach one of the specified states.
    /// </summary>
    /// <remarks>
    /// If <paramref name="targetStates"/> is null, the default states are <see cref="KnownResourceStates.Running"/> and <see cref="KnownResourceStates.Hidden"/>.
    /// </remarks>
    public static async Task WaitForResourcesAsync(this DistributedApplication app, IReadOnlyList<string> targetStates = null, CancellationToken cancellationToken = default)
    {
        ILogger logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger($"{nameof(IntegrationTests)}.{nameof(WaitForResourcesAsync)}");

        targetStates ??= [KnownResourceStates.Running, ..KnownResourceStates.TerminalStates];
        DistributedApplicationModel applicationModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        Dictionary<string, Task<(string Name, string State)>> resourceTasks = new();

        foreach (IResource resource in applicationModel.Resources.Where(resource => resource is not IResourceWithoutLifetime))
        {
            resourceTasks[resource.Name] = GetResourceWaitTask(resource.Name, targetStates, cancellationToken);
        }

        logger.LogInformation("Waiting for resources [{Resources}] to reach one of target states [{TargetStates}].",
                              string.Join(',', resourceTasks.Keys),
                              string.Join(',', targetStates));

        while (resourceTasks.Count > 0)
        {
            Task<(string Name, string State)> completedTask = await Task.WhenAny(resourceTasks.Values);
            (string completedResourceName, string targetStateReached) = await completedTask;

            if (targetStateReached == KnownResourceStates.FailedToStart)
            {
                throw new DistributedApplicationException($"Resource '{completedResourceName}' failed to start.");
            }

            resourceTasks.Remove(completedResourceName);

            logger.LogInformation("Wait for resource '{ResourceName}' completed with state '{ResourceState}'", completedResourceName, targetStateReached);

            // Ensure resources being waited on still exist
            List<string> remainingResources = [.. resourceTasks.Keys];
            for (int i = remainingResources.Count - 1; i > 0; i--)
            {
                string name = remainingResources[i];

                // If the resource was deleted while waiting for it, remove it from the list.
                if (applicationModel.Resources.All(r => r.Name != name))
                {
                    logger.LogInformation("Resource '{ResourceName}' was deleted while waiting for it.", name);
                    resourceTasks.Remove(name);
                    remainingResources.RemoveAt(i);
                }
            }

            if (resourceTasks.Count > 0)
            {
                logger.LogInformation("Still waiting for resources [{Resources}] to reach one of target states [{TargetStates}].",
                                      string.Join(',', remainingResources),
                                      string.Join(',', targetStates));
            }
        }

        logger.LogInformation("Wait for all resources completed successfully!");

        return;

        async Task<(string Name, string State)> GetResourceWaitTask(string resourceName, IEnumerable<string> targetResourceStates, CancellationToken ct)
        {
            string state = await app.ResourceNotifications.WaitForResourceAsync(resourceName, targetResourceStates, ct);
            return (resourceName, state);
        }
    }

    /// <summary>
    /// Sets the container lifetime for all container resources in the application.
    /// </summary>
    public static TBuilder WithContainersLifetime<TBuilder>(this TBuilder builder, ContainerLifetime containerLifetime)
        where TBuilder : IDistributedApplicationTestingBuilder
    {
        List<ContainerLifetimeAnnotation> containerLifetimeAnnotations = builder.Resources.SelectMany(r => r.Annotations
                                                                                                          .OfType<ContainerLifetimeAnnotation>()
                                                                                                          .Where(c => c.Lifetime != containerLifetime))
            .ToList();

        foreach (ContainerLifetimeAnnotation annotation in containerLifetimeAnnotations)
        {
            annotation.Lifetime = containerLifetime;
        }

        return builder;
    }

    /// <summary>
    /// Replaces all named volumes with anonymous volumes so they're isolated across test runs and from the volume the app uses during development.
    /// </summary>
    /// <remarks>
    /// Note that if multiple resources share a volume, the volume will instead be given a random name so that it's still shared across those resources in the test run.
    /// </remarks>
    public static TBuilder WithRandomVolumeNames<TBuilder>(this TBuilder builder) where TBuilder : IDistributedApplicationTestingBuilder
    {
        // Named volumes that aren't shared across resources should be replaced with anonymous volumes.
        // Named volumes shared by mulitple resources need to have their name randomized but kept shared across those resources.

        // Find all shared volumes and make a map of their original name to a new randomized name
        List<(IResource Resource, ContainerMountAnnotation Volume)> allResourceNamedVolumes = builder.Resources.SelectMany(r => r.Annotations
                                                                                                                               .OfType<ContainerMountAnnotation>()
                                                                                                                               .Where(m => m.Type == ContainerMountType.Volume && !string.IsNullOrEmpty(m.Source))
                                                                                                                               .Select(m => (Resource: r, Volume: m)))
            .ToList();
        HashSet<string> seenVolumes = new HashSet<string>();
        Dictionary<string, string> renamedVolumes = new Dictionary<string, string>();
        foreach ((IResource Resource, ContainerMountAnnotation Volume) resourceVolume in allResourceNamedVolumes)
        {
            string name = resourceVolume.Volume.Source!;
            if (!seenVolumes.Add(name) && !renamedVolumes.ContainsKey(name))
            {
                renamedVolumes[name] = $"{name}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(4))}";
            }
        }

        // Replace all named volumes with randomly named or anonymous volumes
        foreach ((IResource Resource, ContainerMountAnnotation Volume) resourceVolume in allResourceNamedVolumes)
        {
            IResource resource = resourceVolume.Resource;
            ContainerMountAnnotation volume = resourceVolume.Volume;
            string newName = renamedVolumes.TryGetValue(volume.Source!, out string randomName) ? randomName : null;
            ContainerMountAnnotation newMount = new ContainerMountAnnotation(newName, volume.Target, ContainerMountType.Volume, volume.IsReadOnly);
            resource.Annotations.Remove(volume);
            resource.Annotations.Add(newMount);
        }

        return builder;
    }

    /// <summary>
    /// Waits for the specified resource to reach the specified state.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="resourceName">The name of the resource to wait for.</param>
    /// <param name="targetState">The state to wait for. If null, the default state is <see cref="KnownResourceStates.Running"/>.</param>
    /// <param name="cancellationToken"></param>
    public static Task WaitForResource(this DistributedApplication app, string resourceName, string targetState = null, CancellationToken cancellationToken = default)
    {
        targetState ??= KnownResourceStates.Running;
        ResourceNotificationService resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();

        return resourceNotificationService.WaitForResourceAsync(resourceName, targetState, cancellationToken);
    }

    /// <summary>
    /// Gets the app host and resource logs from the application.
    /// </summary>
    public static (IReadOnlyList<FakeLogRecord> AppHostLogs, IReadOnlyList<FakeLogRecord> ResourceLogs) GetLogs(this DistributedApplication app)
    {
        var environment = app.Services.GetRequiredService<IHostEnvironment>();
        var logCollector = app.Services.GetFakeLogCollector();
        var logs = logCollector.GetSnapshot();
        List<FakeLogRecord> appHostLogs = logs.Where(l => l.Category?.StartsWith($"{environment.ApplicationName}.Resources") == false).ToList();
        List<FakeLogRecord> resourceLogs = logs.Where(l => l.Category?.StartsWith($"{environment.ApplicationName}.Resources") == true).ToList();

        return (appHostLogs, resourceLogs);
    }

    /// <summary>
    /// Asserts that no errors were logged by the application or any of its resources.
    /// </summary>
    /// <remarks>
    /// Some resource types are excluded from this check because they tend to write to stderr for various non-error reasons.
    /// </remarks>
    /// <param name="app"></param>
    public static void EnsureNoErrorsLogged(this DistributedApplication app)
    {
        var environment = app.Services.GetRequiredService<IHostEnvironment>();
        var applicationModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        List<string> assertableResourceLogNames = applicationModel.Resources.Where(ShouldAssertErrorsForResource).Select(r => $"{environment.ApplicationName}.Resources.{r.Name}").ToList();

        var (appHostlogs, resourceLogs) = app.GetLogs();

        Assert.DoesNotContain(appHostlogs, log => log.Level >= LogLevel.Error);
        Assert.DoesNotContain(resourceLogs, log => log.Category is { Length: > 0 } category && assertableResourceLogNames.Contains(category) && log.Level >= LogLevel.Error);

        return;

        static bool ShouldAssertErrorsForResource(IResource resource)
        {
#pragma warning disable ASPIREHOSTINGPYTHON001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            return resource
                       is
                       // Container resources tend to write to stderr for various reasons so only assert projects and executables
                       (ProjectResource or ExecutableResource);
#pragma warning restore ASPIREHOSTINGPYTHON001
        }
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> configured to communicate with the specified resource with custom configuration.
    /// </summary>
    public static HttpClient CreateHttpClient(this DistributedApplication app, string resourceName, string endpointName = null, Action<IHttpClientBuilder> configure = null)
    {


        ServiceProvider services = new ServiceCollection()
            .AddHttpClient()
            .ConfigureHttpClientDefaults(builder =>  configure?.Invoke(builder))
            .BuildServiceProvider();
        IHttpClientFactory httpClientFactory = services.GetRequiredService<IHttpClientFactory>();

        HttpClient httpClient = httpClientFactory.CreateClient();
        httpClient.BaseAddress = app.GetEndpoint(resourceName, endpointName);

        return httpClient;
    }

}