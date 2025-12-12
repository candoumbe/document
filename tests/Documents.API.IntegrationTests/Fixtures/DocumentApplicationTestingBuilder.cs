using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
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
    public const string ApiResourceName = "api";

    /// <summary>
    /// Time to wait after which the application under test will be considered as "not started".
    /// </summary>
    private static readonly TimeSpan s_startStopTimeout = TimeSpan.FromSeconds(120);
    /// <summary>
    /// Time to wait after which building the infrastructure will be considered as failed.
    /// </summary>
    private static readonly TimeSpan s_buildStopTimeout = TimeSpan.FromSeconds(60);


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

        await _app.StartAsync(cancellationToken).WaitAsync(s_startStopTimeout, cancellationToken);
        await _app.WaitForResourcesAsync(cancellationToken: cancellationToken).WaitAsync(s_startStopTimeout, cancellationToken);

        ApiClient = _app.CreateHttpClient(ApiResourceName);

        return _app;
    }


    /// <inheritdoc />
    public async ValueTask InitializeAsync() => await StartAsync(TestContext.Current.CancellationToken);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // Approche en deux phases : arrêt gracieux puis forcé
        bool stopped = await TryGracefulStopAsync();

        if (!stopped && _app is not null)
        {
            Console.WriteLine("Arrêt gracieux échoué, nettoyage forcé...");
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
            // Timeout plus court pour l'arrêt gracieux
            using CancellationTokenSource cts = new (s_startStopTimeout);
            await _app.StopAsync(cts.Token);
            return true;
        }
        catch (Exception ex) when (ex is OperationCanceledException or TimeoutException)
        {
            Console.WriteLine($"Timeout lors de l'arrêt gracieux: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de l'arrêt gracieux: {ex.Message}");
            return false;
        }
    }
}