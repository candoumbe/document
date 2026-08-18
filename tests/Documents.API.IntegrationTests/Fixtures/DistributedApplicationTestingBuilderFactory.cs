extern alias AspireHost;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using AwesomeAssertions.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Documents.API.IntegrationTests.Fixtures;

/// <summary>
/// Factory for creating <see cref="DocumentApplicationTestingBuilder"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// This class is used to create a new instance of the <see cref="DocumentApplicationTestingBuilder"/> class for each test.
/// </para>
/// <para>
/// This is required because the <see cref="DocumentApplicationTestingBuilder"/> class is not thread safe and each test should use its own instance.
/// </para>
/// For more information, <see href="https://github.com/dotnet/aspire-samples/blob/main/tests/SamplesIntegrationTests/Infrastructure/DistributedApplicationTestFactory.cs">the GitHub sample</see>.
/// </remarks>
public static class DistributedApplicationTestingBuilderFactory
{
    private static readonly TimeSpan s_defaultTimeout = 30.Seconds();

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedApplicationTestingBuilderFactory"/> class.
    /// </summary>
    /// <param name="outputHelper"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<DocumentApplicationTestingBuilder> CreateBuilderAsync(ITestOutputHelper outputHelper = null, CancellationToken cancellationToken = default)
    {
        // The AppHost reads the flag while it is being built, so it must be visible before CreateAsync.
        Environment.SetEnvironmentVariable(AspireHost::Program.RunningIntegrationTestsConfigName, bool.TrueString);

        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder.CreateAsync<AspireHost::Projects.Documents_Aspire>(cancellationToken);

        builder.Configuration.AddInMemoryCollection([new KeyValuePair<string, string>(AspireHost::Program.RunningIntegrationTestsConfigName, bool.TrueString)]);
        builder.WithRandomParameterValues();
        builder.WithRandomVolumeNames();
        // Containers should be re-created for each test.
        builder.WithContainersLifetime(ContainerLifetime.Session);

        builder.Services.ConfigureHttpClientDefaults(clientBuilder =>
                                                     {
                                                         clientBuilder.AddStandardResilienceHandler();
                                                     });

        builder.Services.AddHttpLogging();
        builder.Services.AddLogging(logging =>
                                    {
                                        logging.ClearProviders();
                                        logging.AddSimpleConsole();
                                        if (outputHelper is not null)
                                        {
                                            logging.AddXUnit(outputHelper);
                                        }
                                        logging.SetMinimumLevel(LogLevel.Debug);
                                        logging.AddFilter("Aspire", LogLevel.Critical);
                                        logging.AddFilter(builder.Environment.ApplicationName, LogLevel.Information);
                                    });

        return new DocumentApplicationTestingBuilder(builder);
    }
}