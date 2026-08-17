using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Bogus;
using Candoumbe.Forms;
using Documents.API.Features;
using Documents.API.Features.v1;
using Documents.API.Features.v1.Create;
using Documents.API.IntegrationTests.Fixtures;
using Documents.Ids;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Documents.API.IntegrationTests.Features.v1.Create;

[IntegrationTest]
[Feature(nameof(Documents))]
public class CreateDocumentEndpointShould : IAsyncLifetime
{
    private readonly DocumentApplicationFixture _fixture = new();
    private HttpClient _client;
    private static readonly Faker s_faker = new();

    ///<inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        await _fixture.InitializeAsync();
        _client = _fixture.ApiClient;
    }

    ///<inheritdoc/>
    public async ValueTask DisposeAsync() => await _fixture.DisposeAsync();


    [Fact]
    public async Task Returns_the_document_when_created_successfully()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        CreateDocumentRequest newDocumentInfo = new ()
        {
            Id = DocumentId.New(),
            Name = s_faker.System.FileName(),
            MimeType = "text/plain",
            Content = s_faker.Random.Bytes(s_faker.Random.Int(32, 64))
        };

        // Act
        _client.DefaultRequestHeaders.Add("Api-Version", "1.0");
        using HttpResponseMessage response = await _client.PostAsJsonAsync("/documents", newDocumentInfo, _fixture.ApiJsonSerializerOptions, cancellationToken: cancellationToken);

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        Browsable<DocumentInfo> browsable = await response.Content.ReadFromJsonAsync<Browsable<DocumentInfo>>(_fixture.ApiJsonSerializerOptions, cancellationToken: cancellationToken);

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