using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Buzzard.Tests;

public class NonAsciiHeaderForwardingTests : IAsyncLifetime
{
    private const string TestHeaderName = "X-Test-Header";
    private const string NonAsciiValue = "Tōkyō";

    private WebApplication _backend = null!;
    private string _backendAddress = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        _backend = builder.Build();
        _backend.Run(context => context.Response.WriteAsync(context.Request.Headers[TestHeaderName].ToString()));
        await _backend.StartAsync();
        _backendAddress = _backend.Urls.Single();
    }

    public async ValueTask DisposeAsync()
    {
        await _backend.DisposeAsync();
    }

    [Fact]
    public async Task ForwardsNonAsciiHeaderWhenRequestHeaderEncodingIsUtf8()
    {
        await using var factory = CreateFactory(requestHeaderEncoding: "utf-8");

        var response = await SendAsync(factory);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(NonAsciiValue, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReturnsBadGatewayForNonAsciiHeaderWithoutRequestHeaderEncoding()
    {
        await using var factory = CreateFactory(requestHeaderEncoding: null);

        var response = await SendAsync(factory);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    private WebApplicationFactory<Program> CreateFactory(string? requestHeaderEncoding)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ReverseProxy:Routes:test:ClusterId"] = "test",
            ["ReverseProxy:Routes:test:Match:Path"] = "{**catch-all}",
            ["ReverseProxy:Clusters:test:Destinations:backend:Address"] = _backendAddress,
        };
        if (requestHeaderEncoding is not null)
        {
            settings["ReverseProxy:Clusters:test:HttpClient:RequestHeaderEncoding"] = requestHeaderEncoding;
        }

        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(settings));
            builder.ConfigureTestServices(services => services.RemoveAll<ILoggerProvider>());
        });
    }

    private static async Task<HttpResponseMessage> SendAsync(WebApplicationFactory<Program> factory)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test");
        request.Headers.TryAddWithoutValidation(TestHeaderName, NonAsciiValue);
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
