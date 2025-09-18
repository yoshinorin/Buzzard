using System.Net;
using Buzzard.Middleware;
using Buzzard.Models;
using Buzzard.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace Buzzard.Tests;

public class FirewallMiddlewareTests
{
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly Mock<ILogger<FirewallMiddleware>> _mockLogger;
    private readonly Mock<IPathValidator> _mockPathValidator;
    private readonly Mock<IUserAgentValidator> _mockUserAgentValidator;
    private readonly FirewallMiddleware _middleware;

    public FirewallMiddlewareTests()
    {
        _mockNext = new Mock<RequestDelegate>();
        _mockLogger = new Mock<ILogger<FirewallMiddleware>>();
        _mockPathValidator = new Mock<IPathValidator>();
        _mockUserAgentValidator = new Mock<IUserAgentValidator>();

        _middleware = new FirewallMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            _mockPathValidator.Object,
            _mockUserAgentValidator.Object);
    }

    [Fact]
    public async Task InvokeAsync_AllowedRequest_CallsNext()
    {
        var context = CreateHttpContext("/test");
        _mockPathValidator.Setup(x => x.IsPathBlocked("/test")).Returns(false);
        _mockUserAgentValidator.Setup(x => x.IsUserAgentBlocked("Test-Agent")).Returns(false);

        await _middleware.InvokeAsync(context);

        _mockNext.Verify(x => x(context), Times.Once);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_BlockedPath_Returns403()
    {
        var context = CreateHttpContext("/admin/users");
        _mockPathValidator.Setup(x => x.IsPathBlocked("/admin/users")).Returns(true);
        _mockUserAgentValidator.Setup(x => x.IsUserAgentBlocked("Test-Agent")).Returns(false);

        await _middleware.InvokeAsync(context);

        _mockNext.Verify(x => x(context), Times.Never);
        Assert.Equal(403, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("/admin")]
    [InlineData("/admin/users")]
    [InlineData("/api/internal/test")]
    public async Task InvokeAsync_BlockedPaths_Returns403(string requestPath)
    {
        var context = CreateHttpContext(requestPath);
        _mockPathValidator.Setup(x => x.IsPathBlocked(requestPath)).Returns(true);
        _mockUserAgentValidator.Setup(x => x.IsUserAgentBlocked("Test-Agent")).Returns(false);

        await _middleware.InvokeAsync(context);

        _mockNext.Verify(x => x(context), Times.Never);
        Assert.Equal(403, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_BlockedUserAgent_Returns403()
    {
        var context = CreateHttpContext("/test");
        _mockPathValidator.Setup(x => x.IsPathBlocked("/test")).Returns(false);
        _mockUserAgentValidator.Setup(x => x.IsUserAgentBlocked("Test-Agent")).Returns(true);

        await _middleware.InvokeAsync(context);

        _mockNext.Verify(x => x(context), Times.Never);
        Assert.Equal(403, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("BadBot/1.0")]
    [InlineData("SpamCrawler/2.0")]
    [InlineData("MaliciousBot/1.5")]
    public async Task InvokeAsync_BlockedUserAgents_Returns403(string userAgent)
    {
        var context = CreateHttpContext("/test");
        context.Request.Headers.UserAgent = userAgent;
        _mockPathValidator.Setup(x => x.IsPathBlocked("/test")).Returns(false);
        _mockUserAgentValidator.Setup(x => x.IsUserAgentBlocked(userAgent)).Returns(true);

        await _middleware.InvokeAsync(context);

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
