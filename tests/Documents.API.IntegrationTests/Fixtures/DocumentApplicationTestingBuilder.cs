using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Documents.API.IntegrationTests.Fixtures;

public class DocumentApplicationTestingBuilder : IAsyncLifetime
{
    private readonly IDistributedApplicationTestingBuilder _sutBuilder;
    private DistributedApplication _app;
    /// <summary>
    /// HTTP client for the API.
    /// </summary>
    public HttpClient ApiClient { get; private set; }

    /// <summary>
    /// HTTP client for the API that does not attach any bearer token.
    /// </summary>
    public HttpClient AnonymousApiClient { get; private set; }

    public const string ApiResourceName = "api";

    /// <summary>
    /// Time to wait after which the application under test will be considered as "not started".
    /// </summary>
    public static readonly TimeSpan StartStopTimeout = ResolveStartStopTimeout();
    private const string StartStopTimeoutSecondsEnvironmentVariable = "DOCUMENTS_INTEGRATION_TESTS_STARTSTOP_TIMEOUT_SECONDS";
    private static readonly TimeSpan s_readinessProbeDelay = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan s_requestProbeTimeout = TimeSpan.FromSeconds(5);
    private const int RequiredConsecutiveSuccessfulProbes = 1;
    /// <summary>
    /// Time to wait after which building the infrastructure will be considered as failed.
    /// </summary>
    private static readonly TimeSpan s_buildStopTimeout = TimeSpan.FromSeconds(60);

    internal static TimeSpan ResolveStartStopTimeout(string ci, string githubActions)
    {
        bool isCi = string.Equals(ci, bool.TrueString, StringComparison.OrdinalIgnoreCase)
            || string.Equals(githubActions, bool.TrueString, StringComparison.OrdinalIgnoreCase);

        return isCi ? TimeSpan.FromMinutes(5) : TimeSpan.FromMinutes(2);
    }

    private static TimeSpan ResolveStartStopTimeout()
    {
        string configuredTimeoutSeconds = Environment.GetEnvironmentVariable(StartStopTimeoutSecondsEnvironmentVariable);
        if (int.TryParse(configuredTimeoutSeconds, out int timeoutSeconds)
            && timeoutSeconds > 0)
        {
            return TimeSpan.FromSeconds(timeoutSeconds);
        }

        string ci = Environment.GetEnvironmentVariable("CI");
        string githubActions = Environment.GetEnvironmentVariable("GITHUB_ACTIONS");
        return ResolveStartStopTimeout(ci, githubActions);
    }


    /// <summary>
    /// Creates a new instance of the <see cref="DocumentApplicationTestingBuilder"/> class.
    /// </summary>
    /// <param name="builder">The builder that will be used to create the infrastructure of the application under test.</param>
    public DocumentApplicationTestingBuilder(IDistributedApplicationTestingBuilder builder)
    {
        _sutBuilder = builder;
    }

    /// <summary>
    /// Builds the infrastructure and starts the application under test.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>The application under test</returns>
    /// <remarks>
    /// The application under test is started after the infrastructure is built.
    /// This method will wait for the application to reach the "running" state (i.e. all resources are running or have exited with a success code).
    /// </remarks>
    public async Task<DistributedApplication> StartAsync(CancellationToken cancellationToken)
    {
        _app  = await _sutBuilder.BuildAsync(cancellationToken).WaitAsync(s_buildStopTimeout, cancellationToken);

        await _app.StartAsync(cancellationToken).WaitAsync(StartStopTimeout, cancellationToken);
        await _app.ResourceNotifications.WaitForResourceHealthyAsync(ApiResourceName, cancellationToken).WaitAsync(StartStopTimeout, cancellationToken);
        ApiClient = _app.CreateHttpClient(ApiResourceName, endpointName: "http", builder =>
        {
            builder.AddStandardResilienceHandler();
        });
        AnonymousApiClient = _app.CreateHttpClient(ApiResourceName, endpointName: "http", builder =>
        {
            builder.AddStandardResilienceHandler();
        });
        await WaitUntilApiIsReachableAsync(cancellationToken);

        return _app;
    }

    /// <inheritdoc />
    public async ValueTask InitializeAsync() => await StartAsync(TestContext.Current.CancellationToken);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // Two-phase shutdown: graceful attempt first, forced cleanup if needed.
        bool stopped = await TryGracefulStopAsync();

        if (!stopped && _app is not null)
        {
            Console.WriteLine("Graceful shutdown failed, forcing resource cleanup...");
            await _app.DisposeAsync();
        }

        await _sutBuilder.DisposeAsync();
    }

    private async Task<bool> TryGracefulStopAsync()
    {
        if (_app == null)
        {
            return true;
        }

        try
        {
            // Shorter timeout for graceful stop.
            using CancellationTokenSource cts = new (StartStopTimeout);
            await _app.StopAsync(cts.Token);
            return true;
        }
        catch (Exception ex) when (ex is OperationCanceledException or TimeoutException)
        {
            Console.WriteLine($"Timeout during graceful stop: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during graceful stop: {ex.Message}");
            return false;
        }
    }

    private async Task WaitUntilApiIsReachableAsync(CancellationToken cancellationToken)
    {
        Exception lastException = null;
        int consecutiveSuccessCount = 0;

        using CancellationTokenSource timeoutCancellationTokenSource = new(StartStopTimeout);
        using CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellationTokenSource.Token);

        while (!linkedCancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                // Probe the readiness endpoint to avoid coupling test bootstrap
                // to business routes that may evolve independently.
                using HttpRequestMessage request = new(HttpMethod.Get, "/health");
                using CancellationTokenSource requestCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(linkedCancellationTokenSource.Token);
                requestCancellationTokenSource.CancelAfter(s_requestProbeTimeout);

                using HttpResponseMessage response = await ApiClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, requestCancellationTokenSource.Token);

                if (response.IsSuccessStatusCode)
                {
                    consecutiveSuccessCount++;
                    if (consecutiveSuccessCount >= RequiredConsecutiveSuccessfulProbes)
                    {
                        return;
                    }
                }
                else
                {
                    consecutiveSuccessCount = 0;
                }
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                lastException = exception;
                consecutiveSuccessCount = 0;
            }

            await Task.Delay(s_readinessProbeDelay, linkedCancellationTokenSource.Token);
        }

        throw new TimeoutException("The API endpoint did not become reachable before the startup timeout elapsed.", lastException);
    }
