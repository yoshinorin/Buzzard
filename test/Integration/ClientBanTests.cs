using System.Net;
using Buzzard.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Buzzard.Tests;

public class ClientBanTests : IAsyncLifetime
{
    private const string ClientIpHeader = "X-Test-Client-Ip";
    private const string ProxyIp = "10.0.0.1";
    private const string BannedClientIp = "203.0.113.10";
    private const string OtherClientIp = "198.51.100.20";

    private TestBackend _backend = null!;

    public async ValueTask InitializeAsync()
    {
        _backend = await TestBackend.StartAsync(_ => Task.CompletedTask);
    }

    public async ValueTask DisposeAsync()
    {
        await _backend.DisposeAsync();
    }

    [Fact]
    public async Task BansForwardedClientIpAfterFirewallRejections()
    {
        await using var factory = CreateFactory(ClientBanMode.Enforce);

        Assert.Equal(StatusCodes.Status403Forbidden, await SendAsync(factory, BannedClientIp, "/denied"));
        Assert.Equal(StatusCodes.Status403Forbidden, await SendAsync(factory, BannedClientIp, "/denied"));

        Assert.Equal(StatusCodes.Status403Forbidden, await SendAsync(factory, BannedClientIp, "/allowed"));
        Assert.Equal(StatusCodes.Status200OK, await SendAsync(factory, OtherClientIp, "/allowed"));
    }

    [Fact]
    public async Task DisabledMode_DoesNotBan()
    {
        await using var factory = CreateFactory(ClientBanMode.Disabled);

        await SendAsync(factory, BannedClientIp, "/denied");
        await SendAsync(factory, BannedClientIp, "/denied");

        Assert.Equal(StatusCodes.Status200OK, await SendAsync(factory, BannedClientIp, "/allowed"));
    }

    [Fact]
    public async Task FailsToStartWithInvalidSettings()
    {
        await using var factory = CreateFactory(ClientBanMode.Enforce, threshold: 0);

        Assert.Throws<OptionsValidationException>(() => factory.Server);
    }

    private WebApplicationFactory<Program> CreateFactory(ClientBanMode mode, int threshold = 2)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ForwardedHeaders:ClientIpHeader"] = ClientIpHeader,
            ["Firewall:ClientBan:Mode"] = mode.ToString(),
            ["Firewall:ClientBan:Threshold"] = threshold.ToString(),
        };

        return BuzzardFactory.Create(_backend.Address, settings, services =>
        {
            var firewallConfig = new FirewallConfig();
            firewallConfig.Path.Deny.Contains.Add("/denied");
            services.RemoveAll<FirewallConfig>();
            services.RemoveAll<PathConfig>();
            services.RemoveAll<UserAgentConfig>();
            services.AddSingleton(firewallConfig);
            services.AddSingleton(firewallConfig.Path);
            services.AddSingleton(firewallConfig.UserAgent);
        });
    }

    private static async Task<int> SendAsync(WebApplicationFactory<Program> factory, string clientIp, string path)
    {
        var context = await factory.Server.SendAsync(c =>
        {
            c.Request.Method = HttpMethods.Get;
            c.Request.Path = path;
            c.Connection.RemoteIpAddress = IPAddress.Parse(ProxyIp);
            c.Request.Headers[ClientIpHeader] = clientIp;
        }, TestContext.Current.CancellationToken);
        return context.Response.StatusCode;
    }
}
