using System.Net;
using Buzzard.Middleware;
using Buzzard.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace Buzzard.Tests;

public class FirewallMiddlewareTests
{
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly Mock<ILogger<FirewallMiddleware>> _mockLogger;
    private readonly FirewallMiddleware _middleware;

    public FirewallMiddlewareTests()
    {
        _mockNext = new Mock<RequestDelegate>();
        _mockLogger = new Mock<ILogger<FirewallMiddleware>>();

        var config = new FirewallConfig();
        _middleware = new FirewallMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            config);
    }

    [Fact]
    public async Task InvokeAsync_AllowedRequest_CallsNext()
    {
        var context = CreateHttpContext();

        await _middleware.InvokeAsync(context);

        _mockNext.Verify(x => x(context), Times.Once);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_PathDenyStartsWith_Returns403_Legacy()
    {
        var config = new FirewallConfig();
        config.Path.Deny.StartsWith.AddRange(["/admin", "/api/internal"]);

        var middleware = new FirewallMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            config);
        var context = CreateHttpContext("/admin/users");

        await middleware.InvokeAsync(context);

        _mockNext.Verify(x => x(context), Times.Never);
        Assert.Equal(403, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_AllowedPath_CallsNext_Default()
    {
        var config = new FirewallConfig();

        var middleware = new FirewallMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            config);
        var context = CreateHttpContext("/api/public");

        await middleware.InvokeAsync(context);

        _mockNext.Verify(x => x(context), Times.Once);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("/admin", "/admin")]
    [InlineData("/admin/users", "/admin")]
    [InlineData("/ADMIN/Users", "/admin")]
    [InlineData("/api/internal/test", "/api/internal")]
    public async Task InvokeAsync_PathDenyStartsWith_IsCaseInsensitive(string requestPath, string denyPath)
    {
        var config = new FirewallConfig();
        config.Path.Deny.StartsWith.Add(denyPath);

        var middleware = new FirewallMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            config);
        var context = CreateHttpContext(requestPath);

        await middleware.InvokeAsync(context);

        _mockNext.Verify(x => x(context), Times.Never);
        Assert.Equal(403, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/status")]
    [InlineData("/api/health")]
    public async Task InvokeAsync_PathAllowContains_CallsNext(string requestPath)
    {
        var config = new FirewallConfig();
        config.Path.Allow.Contains.AddRange(["/health", "/status"]);
        config.Path.Deny.Contains.AddRange(["/admin"]);

        var middleware = new FirewallMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            config);
        var context = CreateHttpContext(requestPath);

        await middleware.InvokeAsync(context);

        _mockNext.Verify(x => x(context), Times.Once);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("/admin")]
    [InlineData("/config")]
    [InlineData("/api/admin")]
    public async Task InvokeAsync_PathDenyContains_Returns403(string requestPath)
    {
        var config = new FirewallConfig();
        config.Path.Deny.Contains.AddRange(["/admin", "/config"]);

        var middleware = new FirewallMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            config);
        var context = CreateHttpContext(requestPath);

        await middleware.InvokeAsync(context);

        _mockNext.Verify(x => x(context), Times.Never);
        Assert.Equal(403, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("/api/public/users")]
    [InlineData("/api/public/data")]
    public async Task InvokeAsync_PathAllowStartsWith_CallsNext(string requestPath)
    {
        var config = new FirewallConfig();
        config.Path.Allow.StartsWith.AddRange(["/api/public"]);
        config.Path.Deny.StartsWith.AddRange(["/api/private"]);

        var middleware = new FirewallMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            config);
        var context = CreateHttpContext(requestPath);

        await middleware.InvokeAsync(context);

        _mockNext.Verify(x => x(context), Times.Once);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("/api/private/users")]
    [InlineData("/private/data")]
    public async Task InvokeAsync_PathDenyStartsWith_Returns403(string requestPath)
    {
        var config = new FirewallConfig();
        config.Path.Deny.StartsWith.AddRange(["/api/private", "/private/"]);

        var middleware = new FirewallMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            config);
        var context = CreateHttpContext(requestPath);

        await middleware.InvokeAsync(context);

        _mockNext.Verify(x => x(context), Times.Never);
        Assert.Equal(403, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("/styles.css")]
    [InlineData("/app.js")]
    [InlineData("/logo.png")]
    public async Task InvokeAsync_PathAllowEndsWith_CallsNext(string requestPath)
    {
        var config = new FirewallConfig();
        config.Path.Allow.EndsWith.AddRange([".css", ".js", ".png"]);
        config.Path.Deny.EndsWith.AddRange([".bak"]);

        var middleware = new FirewallMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            config);
        var context = CreateHttpContext(requestPath);

        await middleware.InvokeAsync(context);

        _mockNext.Verify(x => x(context), Times.Once);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("/config.bak")]
    [InlineData("/data.tmp")]
    public async Task InvokeAsync_PathDenyEndsWith_Returns403(string requestPath)
    {
        var config = new FirewallConfig();
        config.Path.Deny.EndsWith.AddRange([".bak", ".tmp"]);

        var middleware = new FirewallMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            config);
        var context = CreateHttpContext(requestPath);

        await middleware.InvokeAsync(context);

        _mockNext.Verify(x => x(context), Times.Never);
        Assert.Equal(403, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_AllowOverridesDeny_CallsNext()
    {
        var config = new FirewallConfig();
        config.Path.Allow.Contains.Add("/admin/health");
        config.Path.Deny.StartsWith.Add("/admin");

        var middleware = new FirewallMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            config);
        var context = CreateHttpContext("/admin/health");

        await middleware.InvokeAsync(context);

        _mockNext.Verify(x => x(context), Times.Once);
        Assert.Equal(200, context.Response.StatusCode);
    }

    private static DefaultHttpContext CreateHttpContext(string path = "/")
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = "GET";
        context.Request.Headers.UserAgent = "Test-Agent";
        context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        context.Response.Body = new MemoryStream();
        return context;
    }
}
