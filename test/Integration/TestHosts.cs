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

public sealed class TestBackend : IAsyncDisposable
{
    private readonly WebApplication _app;

    public string Address { get; }

    private TestBackend(WebApplication app)
    {
        _app = app;
        Address = app.Urls.Single();
    }

    public static async Task<TestBackend> StartAsync(RequestDelegate handler)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        var app = builder.Build();
        app.Run(handler);
        await app.StartAsync();
        return new TestBackend(app);
    }

    public ValueTask DisposeAsync() => _app.DisposeAsync();
}

public static class BuzzardFactory
{
    public static WebApplicationFactory<Program> Create(
        string backendAddress, IDictionary<string, string?> settings, Action<IServiceCollection>? configureServices = null)
    {
        var allSettings = new Dictionary<string, string?>(settings)
        {
            ["ReverseProxy:Routes:test:ClusterId"] = "test",
            ["ReverseProxy:Routes:test:Match:Path"] = "{**catch-all}",
            ["ReverseProxy:Clusters:test:Destinations:backend:Address"] = backendAddress,
        };

        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(allSettings));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ILoggerProvider>();
                configureServices?.Invoke(services);
            });
        });
    }
}
