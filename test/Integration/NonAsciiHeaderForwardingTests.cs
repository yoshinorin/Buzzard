using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Buzzard.Tests;

public class NonAsciiHeaderForwardingTests : IAsyncLifetime
{
    private const string TestHeaderName = "X-Test-Header";
    private const string NonAsciiValue = "Tōkyō";

    private TestBackend _backend = null!;

    public async ValueTask InitializeAsync()
    {
        _backend = await TestBackend.StartAsync(context => context.Response.WriteAsync(context.Request.Headers[TestHeaderName].ToString()));
    }

    public async ValueTask DisposeAsync()
    {
        await _backend.DisposeAsync();
    }

    [Fact]
    public async Task ForwardsNonAsciiHeaderWhenRequestHeaderEncodingIsUtf8()
    {
        await using var factory = BuzzardFactory.Create(_backend.Address, new Dictionary<string, string?>
        {
            ["ReverseProxy:Clusters:test:HttpClient:RequestHeaderEncoding"] = "utf-8",
        });

        var response = await SendAsync(factory);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(NonAsciiValue, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReturnsBadGatewayForNonAsciiHeaderWithoutRequestHeaderEncoding()
    {
        await using var factory = BuzzardFactory.Create(_backend.Address, new Dictionary<string, string?>());

        var response = await SendAsync(factory);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> SendAsync(WebApplicationFactory<Program> factory)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test");
        request.Headers.TryAddWithoutValidation(TestHeaderName, NonAsciiValue);
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
