using System.Net;
using Buzzard.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace BuzzardTests;

public class HttpContextExtensionsTests
{
    [Fact]
    public void GetClientIpAddress_WithXForwardedFor_ReturnsForwardedIp()
    {
        var context = CreateHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "192.168.1.100";

        var result = context.GetClientIpAddress();

        Assert.Equal(IPAddress.Parse("192.168.1.100"), result);
    }

    [Fact]
    public void GetClientIpAddress_WithMultipleXForwardedFor_ReturnsFirstIp()
    {
        var context = CreateHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "192.168.1.100, 10.0.0.1, 172.16.0.1";

        var result = context.GetClientIpAddress();

        Assert.Equal(IPAddress.Parse("192.168.1.100"), result);
    }

    [Fact]
    public void GetClientIpAddress_WithInvalidXForwardedFor_FallsBackToRemoteIp()
    {
        var context = CreateHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "invalid-ip";
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");

        var result = context.GetClientIpAddress();

        Assert.Equal(IPAddress.Parse("10.0.0.1"), result);
    }

    [Fact]
    public void GetClientIpAddress_WithoutXForwardedFor_ReturnsRemoteIp()
    {
        var context = CreateHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.200");

        var result = context.GetClientIpAddress();

        Assert.Equal(IPAddress.Parse("192.168.1.200"), result);
    }

    [Fact]
    public void GetClientIpAddress_WithoutAnyIp_ReturnsLoopback()
    {
        var context = CreateHttpContext();
        context.Connection.RemoteIpAddress = null;

        var result = context.GetClientIpAddress();

        Assert.Equal(IPAddress.Loopback, result);
    }

    [Fact]
    public void GetClientIpAddress_WithEmptyXForwardedFor_ReturnsRemoteIp()
    {
        var context = CreateHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "";
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.5");

        var result = context.GetClientIpAddress();

        Assert.Equal(IPAddress.Parse("10.0.0.5"), result);
    }

    [Fact]
    public void GetClientIpAddress_WithWhitespaceInXForwardedFor_TrimsAndParses()
    {
        var context = CreateHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "  192.168.1.150  ";

        var result = context.GetClientIpAddress();

        Assert.Equal(IPAddress.Parse("192.168.1.150"), result);
    }

    [Fact]
    public void Extract_WithAllProperties_ReturnsCompleteRequest()
    {
        var context = CreateHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/test";
        context.Request.QueryString = new QueryString("?param=value&foo&bar");
        context.Request.Headers.UserAgent = "TestAgent/1.0";
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.100");

        var result = context.Extract();

        Assert.Equal("192.168.1.100", result.Ip);
        Assert.Equal("GET", result.Method);
        Assert.Equal("/api/test", result.Path);
        Assert.Equal("?param=value&foo&bar", result.QueryString);
        Assert.Equal("TestAgent/1.0", result.UserAgent);
    }

    [Fact]
    public void Extract_WithEmpty_ReturnsDefaults()
    {
        var context = CreateHttpContext();
        var result = context.Extract();

        Assert.Equal("127.0.0.1", result.Ip);
        Assert.Equal("", result.Method);
        Assert.Equal("", result.Path);
        Assert.Equal("", result.QueryString);
        Assert.Equal("", result.UserAgent);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpConnectionFeature>(new HttpConnectionFeature());
        return context;
    }
}
