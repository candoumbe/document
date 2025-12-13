using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Aspire.Hosting;
using AwesomeAssertions;
using Bogus;
using Candoumbe.Forms;
using DataFilters.Converters;
using Documents.API.Features;
using Documents.API.Features.v1;
using Documents.API.Features.v1.Create;
using Documents.API.IntegrationTests.Fixtures;
using Documents.Ids;
using Json.More;
using Json.Patch;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using xRetry.v3;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Documents.API.IntegrationTests.Features.v1.Create;

[IntegrationTests]
[Feature(nameof(Documents))]
public class CreateDocumentEndpointShould(ITestOutputHelper outputHelper) : IAsyncLifetime
{
    private HttpClient _client;
    private static readonly Faker s_faker = new();
    private DocumentApplicationTestingBuilder _appHost;
    private static readonly JsonSerializerOptions s_jsonSerializerOptions;
    private DistributedApplication _sut;

    static CreateDocumentEndpointShould()
    {
        s_jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            AllowTrailingCommas = true
        };

        s_jsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        s_jsonSerializerOptions.Converters.Add(new MultiFilterConverter());
        s_jsonSerializerOptions.Converters.Add(new FilterConverter());
        s_jsonSerializerOptions.Converters.Add(new PatchJsonConverter());
        s_jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter<OperationType>());
        s_jsonSerializerOptions.Converters.Add(new EnumStringConverter<OperationType>());
        s_jsonSerializerOptions.Converters.Add(new DocumentId.DocumentIdSystemTextJsonConverter());
    }


    ///<inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        _appHost = await DistributedApplicationTestingBuilderFactory.CreateBuilderAsync(outputHelper);
        _sut = await _appHost.StartAsync(TestContext.Current.CancellationToken);
        _client = _appHost.ApiClient;
    }

    ///<inheritdoc/>
    public async ValueTask DisposeAsync() => await _appHost.DisposeAsync();


    [RetryFact(maxRetries: 3, delayBetweenRetriesMs: 2000, SkipExceptions = [typeof(DistributedApplicationException)])]
    public async Task Returns_the_document_when_created_successfully()
    {
        // Arrange
        //_client = _sut.CreateHttpClient("api");
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        outputHelper.WriteLine("Client: " + _client.BaseAddress);

        CreateDocumentRequest newDocumentInfo = new ()
        {
            Id = DocumentId.New(),
            Name = s_faker.System.FileName(),
            MimeType = "text/plain",
            Content = s_faker.Random.Bytes(s_faker.Random.Int(32, 64))
        };

        // Act
        _client.DefaultRequestHeaders.Add("Api-Version", "1.0");
        using HttpResponseMessage response = await _client.PostAsJsonAsync("/documents", newDocumentInfo, s_jsonSerializerOptions, cancellationToken: cancellationToken);

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        Browsable<DocumentInfo> browsable = await response.Content.ReadFromJsonAsync<Browsable<DocumentInfo>>(s_jsonSerializerOptions, cancellationToken: cancellationToken);

        IEnumerable<Link> links = browsable.Links;
        links.Should()
            .OnlyContain(link => !string.IsNullOrWhiteSpace(link.Href))
            .And.OnlyContain(link => Uri.IsWellFormedUriString(link.Href, UriKind.Absolute), "all links must be absolute URIs")
            .And.OnlyContain(link => link.Relations.AtLeastOnce())
            .And.Contain(link => link.Relations.Once(rel => rel == LinkRelation.Self));

        DocumentInfo resource = browsable.Resource;
        resource.Id.Should().Be(newDocumentInfo.Id);
    }
}