using System.Net;
using Buzzard.Middleware;
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

        _middleware = new FirewallMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            new List<string>());
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
    public async Task InvokeAsync_BlockedPath_Returns403()
    {
        var blockedPaths = new List<string> { "/admin", "/api/internal" };
        var middleware = new FirewallMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            blockedPaths);
        var context = CreateHttpContext("/admin/users");

        await middleware.InvokeAsync(context);

        _mockNext.Verify(x => x(context), Times.Never);
        Assert.Equal(403, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_AllowedPath_CallsNext()
    {
        var blockedPaths = new List<string> { "/admin", "/api/internal" };
        var middleware = new FirewallMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            blockedPaths);
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
    public async Task InvokeAsync_BlockedPathMatching_IsCaseInsensitive(string requestPath, string blockedPath)
    {
        var blockedPaths = new List<string> { blockedPath };
        var middleware = new FirewallMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            blockedPaths);
        var context = CreateHttpContext(requestPath);

        await middleware.InvokeAsync(context);

        _mockNext.Verify(x => x(context), Times.Never);
        Assert.Equal(403, context.Response.StatusCode);
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
