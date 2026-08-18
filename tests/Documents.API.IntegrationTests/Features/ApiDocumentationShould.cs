using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Documents.API.IntegrationTests.Fixtures;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Documents.API.IntegrationTests.Features;

[IntegrationTest]
public sealed class ApiDocumentationShould : IAsyncLifetime
{
    private readonly DocumentApplicationFixture _fixture = new();
    private HttpClient _client;

    public async ValueTask InitializeAsync()
    {
        await _fixture.InitializeAsync();
        _client = _fixture.ApiClient;
    }

    public async ValueTask DisposeAsync() => await _fixture.DisposeAsync();

    [Fact]
    public async Task Exposes_scalar_and_openapi_without_swagger_ui()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using HttpResponseMessage scalarResponse = await _client.GetAsync("/scalar/v1", cancellationToken);
        using HttpResponseMessage openApiResponse = await _client.GetAsync("/openapi/v1.json", cancellationToken);
        using HttpResponseMessage swaggerResponse = await _client.GetAsync("/swagger/index.html", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, scalarResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, openApiResponse.StatusCode);
        Assert.Equal("application/json", openApiResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.NotFound, swaggerResponse.StatusCode);
    }

    // Scalar emits relative asset URLs, so browsing "/scalar/v1/" resolves them one segment too deep.
    [Fact]
    public async Task Redirects_scalar_versioned_asset_to_canonical_asset_path()
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "/scalar/v1/scalar.js");
        using HttpResponseMessage response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/scalar/scalar.js", response.RequestMessage?.RequestUri?.AbsolutePath);
    }
}