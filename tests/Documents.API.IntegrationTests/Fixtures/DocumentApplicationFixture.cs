using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Aspire.Hosting;
using Candoumbe.Forms;
using DataFilters.Converters;
using Documents.Ids;
using Json.More;
using Json.Patch;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Xunit;

namespace Documents.API.IntegrationTests.Fixtures;

public sealed class DocumentApplicationFixture : IAsyncLifetime
{
    private DocumentApplicationTestingBuilder _appHost;
    private readonly Dictionary<string, string> _accessTokenCache = new();
    private readonly SemaphoreSlim _accessTokenCacheLock = new(1, 1);

    public HttpClient ApiClient { get; private set; }

    public HttpClient AnonymousApiClient { get; private set; }

    public JsonSerializerOptions ApiJsonSerializerOptions { get; }

    public DocumentApplicationFixture()
    {
        ApiJsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            AllowTrailingCommas = true
        };

        ApiJsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        ApiJsonSerializerOptions.Converters.Add(new MultiFilterConverter());
        ApiJsonSerializerOptions.Converters.Add(new FilterConverter());
        ApiJsonSerializerOptions.Converters.Add(new PatchJsonConverter());
        ApiJsonSerializerOptions.Converters.Add(new JsonStringEnumConverter<OperationType>());
        ApiJsonSerializerOptions.Converters.Add(new EnumStringConverter<OperationType>());
        ApiJsonSerializerOptions.Converters.Add(new DocumentId.DocumentIdSystemTextJsonConverter());
    }

    ///<inheritdoc />
    public async ValueTask InitializeAsync()
    {
        _appHost = await DistributedApplicationTestingBuilderFactory.CreateBuilderAsync(cancellationToken: TestContext.Current.CancellationToken);
        DistributedApplication app = await _appHost.StartAsync(TestContext.Current.CancellationToken);
        await app.ResourceNotifications.WaitForResourceHealthyAsync(DocumentApplicationTestingBuilder.ApiResourceName, TestContext.Current.CancellationToken).WaitAsync(DocumentApplicationTestingBuilder.StartStopTimeout, TestContext.Current.CancellationToken);
        ApiClient = _appHost.ApiClient;
        AnonymousApiClient = _appHost.AnonymousApiClient;
    }

    ///<inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_appHost is not null)
        {
            await _appHost.DisposeAsync();
        }

        _accessTokenCacheLock.Dispose();
    }
}
