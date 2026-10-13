using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Buzzard.Tests;

public class ClientIpForwardingTests : IAsyncLifetime
{
    private const string ClientIpHeader = "X-Test-Client-Ip";
    private const string DownstreamClientIpHeader = "X-Test-Downstream-Ip";
    private const string ProxyIp = "10.0.0.1";
    private const string ClientIp = "203.0.113.10";
    private const string UntrustedIp = "198.51.100.20";

    private TestBackend _backend = null!;

    public async ValueTask InitializeAsync()
    {
        _backend = await TestBackend.StartAsync(context => context.Response.WriteAsync(
            $"{context.Request.Headers["X-Forwarded-For"]}|{context.Request.Headers[DownstreamClientIpHeader]}|{context.Request.Headers[ClientIpHeader]}"));
    }

    public async ValueTask DisposeAsync()
    {
        await _backend.DisposeAsync();
    }

    [Theory]
    [InlineData("ForwardedHeaders:KnownProxies:0", ProxyIp)]
    [InlineData("ForwardedHeaders:KnownNetworks:0", "10.0.0.0/8")]
    public async Task UsesClientIpHeaderFromTrustedProxyAndKeepsOriginalHeader(string key, string value)
    {
        await using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:ClientIpHeader"] = ClientIpHeader,
            [key] = value,
        });

        var forwarded = await SendAsync(factory, ProxyIp, (ClientIpHeader, ClientIp));

        Assert.Equal((ClientIp, ClientIp, ClientIp), forwarded);
    }

    [Fact]
    public async Task IgnoresClientIpHeaderFromUntrustedConnection()
    {
        await using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:ClientIpHeader"] = ClientIpHeader,
            ["ForwardedHeaders:KnownProxies:0"] = ProxyIp,
        });

        var forwarded = await SendAsync(factory, UntrustedIp, (ClientIpHeader, ClientIp));

        Assert.Equal((UntrustedIp, UntrustedIp, ClientIp), forwarded);
    }

    [Fact]
    public async Task UsesClientIpHeaderFromAnyConnectionWithoutKnownProxiesAndNetworks()
    {
        await using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:ClientIpHeader"] = ClientIpHeader,
        });

        var forwarded = await SendAsync(factory, UntrustedIp, (ClientIpHeader, ClientIp));

        Assert.Equal((ClientIp, ClientIp, ClientIp), forwarded);
    }

    [Fact]
    public async Task UsesConnectionIpWhenClientIpHeaderIsInvalid()
    {
        await using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:ClientIpHeader"] = ClientIpHeader,
            ["ForwardedHeaders:KnownProxies:0"] = ProxyIp,
        });

        var forwarded = await SendAsync(factory, ProxyIp, (ClientIpHeader, "invalid-ip"));

        Assert.Equal((ProxyIp, ProxyIp, "invalid-ip"), forwarded);
    }

    [Fact]
    public async Task OverwritesSpoofedForwardingHeadersFromClient()
    {
        await using var factory = CreateFactory(new Dictionary<string, string?>());

        var forwarded = await SendAsync(factory, ProxyIp, ("X-Forwarded-For", ClientIp), (DownstreamClientIpHeader, ClientIp));

        Assert.Equal((ProxyIp, ProxyIp, ""), forwarded);
    }

    [Fact]
    public async Task FailsToStartWithInvalidKnownProxy()
    {
        await using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:KnownProxies:0"] = "invalid-ip",
        });

        Assert.ThrowsAny<FormatException>(() => factory.Server);
    }

    private WebApplicationFactory<Program> CreateFactory(Dictionary<string, string?> settings)
    {
        settings["ForwardedHeaders:DownstreamClientIpHeader"] = DownstreamClientIpHeader;
        return BuzzardFactory.Create(_backend.Address, settings);
    }

    private static async Task<(string ForwardedFor, string DownstreamClientIp, string OriginalClientIp)> SendAsync(
        WebApplicationFactory<Program> factory, string remoteIp, params (string Name, string Value)[] headers)
    {
        var context = await factory.Server.SendAsync(c =>
        {
            c.Request.Method = HttpMethods.Get;
            c.Request.Path = "/test";
            c.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
            foreach (var (name, value) in headers)
            {
                c.Request.Headers[name] = value;
            }
        }, TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync(TestContext.Current.CancellationToken);
        var parts = body.Split('|');
        return (parts[0], parts[1], parts[2]);
    }
}
