using System.Net;
using Buzzard.Middleware;
using Buzzard.Models;
using Buzzard.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Yarp.ReverseProxy.Forwarder;

namespace Buzzard.Tests;

public class FirewallMiddlewareTests
{
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly Mock<ILogger<FirewallMiddleware>> _mockLogger;
    private readonly Mock<IPathValidator> _mockPathValidator;
    private readonly Mock<IUserAgentValidator> _mockUserAgentValidator;
    private readonly FirewallMiddleware _middleware;
    private readonly List<Dictionary<string, object?>> _scopes = [];

    public FirewallMiddlewareTests()
    {
        _mockNext = new Mock<RequestDelegate>();
        _mockLogger = new Mock<ILogger<FirewallMiddleware>>();
        _mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        _mockLogger
            .Setup(x => x.BeginScope(It.IsAny<Dictionary<string, object?>>()))
            .Callback<Dictionary<string, object?>>(state => _scopes.Add(state))
            .Returns(Mock.Of<IDisposable>());
        _mockPathValidator = new Mock<IPathValidator>();
        _mockUserAgentValidator = new Mock<IUserAgentValidator>();

        _middleware = new FirewallMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            _mockPathValidator.Object,
            _mockUserAgentValidator.Object,
            new QueryStringMasker(new RequestLoggingConfig()),
            new HeaderMasker(new RequestLoggingConfig()));
    }

    [Fact]
    public async Task InvokeAsync_AllowedRequest_CallsNext()
    {
        var context = CreateHttpContext("/test");

        await _middleware.InvokeAsync(context);

        _mockNext.Verify(x => x(context), Times.Once);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("/admin")]
    [InlineData("/admin/users")]
    [InlineData("/api/internal/test")]
    public async Task InvokeAsync_BlockedPaths_Returns403(string requestPath)
    {
        var context = CreateHttpContext(requestPath);
        _mockPathValidator.Setup(x => x.FindBlockingRule(requestPath)).Returns("Path.Deny.Contains:/admin");

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
        _mockUserAgentValidator.Setup(x => x.FindBlockingRule(userAgent)).Returns("UserAgent.Deny.Contains:bot");

        await _middleware.InvokeAsync(context);

        _mockNext.Verify(x => x(context), Times.Never);
        Assert.Equal(403, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_BlockedPath_LogsWarningWithRule()
    {
        var context = CreateHttpContext("/admin");
        _mockPathValidator.Setup(x => x.FindBlockingRule("/admin")).Returns("Path.Deny.Contains:/admin");

        await _middleware.InvokeAsync(context);

        VerifyLogged(LogLevel.Warning, "Request blocked by Path: 127.0.0.1 GET /admin Test-Agent");
        var scope = Assert.Single(_scopes);
        Assert.Equal("Path.Deny.Contains:/admin", scope["Rule"]);
        Assert.Equal("example.com", scope["Host"]);
    }

    [Fact]
    public async Task InvokeAsync_BlockedUserAgent_LogsWarningWithRule()
    {
        var context = CreateHttpContext("/test");
        _mockUserAgentValidator.Setup(x => x.FindBlockingRule("Test-Agent")).Returns("UserAgent.Deny.StartsWith:test");

        await _middleware.InvokeAsync(context);

        VerifyLogged(LogLevel.Warning, "Request blocked by UserAgent: 127.0.0.1 GET /test Test-Agent");
        var scope = Assert.Single(_scopes);
        Assert.Equal("UserAgent.Deny.StartsWith:test", scope["Rule"]);
    }

    [Fact]
    public async Task InvokeAsync_AllowedRequest_LogsInformationWithResponseFields()
    {
        var context = CreateHttpContext("/test");
        context.Request.QueryString = new QueryString("?q=1&token=secret");
        _mockNext.Setup(x => x(context)).Callback<HttpContext>(c => c.Response.StatusCode = 404).Returns(Task.CompletedTask);

        await _middleware.InvokeAsync(context);

        VerifyLogged(LogLevel.Information, "Request allowed: 127.0.0.1 GET /test Test-Agent");
        var scope = Assert.Single(_scopes);
        Assert.Equal(404, scope["StatusCode"]);
        Assert.IsType<double>(scope["ElapsedMs"]);
        Assert.Equal("?q=1&token=***", scope["QueryString"]);
        Assert.False(scope.ContainsKey("Rule"));
    }

    [Fact]
    public async Task InvokeAsync_Request_LogsAllHeadersWithMasking()
    {
        var context = CreateHttpContext("/test");
        context.Request.Headers["CF-Ray"] = "abc123-NRT";
        context.Request.Headers.Cookie = "session=secret";
        context.Request.Headers.Authorization = "Bearer secret";
        context.Request.Headers["X-Long"] = new string('c', 2000);

        await _middleware.InvokeAsync(context);

        var scope = Assert.Single(_scopes);
        Assert.Equal("abc123-NRT", scope["Header.cf-ray"]);
        Assert.Equal("Test-Agent", scope["Header.user-agent"]);
        Assert.Equal("example.com", scope["Header.host"]);
        Assert.Equal("***", scope["Header.cookie"]);
        Assert.Equal("***", scope["Header.authorization"]);
        Assert.Equal(1024, ((string)scope["Header.x-long"]!).Length);
    }

    [Fact]
    public async Task InvokeAsync_ForwarderError_LogsErrorWithException()
    {
        var context = CreateHttpContext("/test");
        var exception = new HttpRequestException("Connection refused");
        _mockNext
            .Setup(x => x(context))
            .Callback<HttpContext>(c =>
            {
                c.Response.StatusCode = 502;
                c.Features.Set<IForwarderErrorFeature>(new TestForwarderErrorFeature(ForwarderError.Request, exception));
            })
            .Returns(Task.CompletedTask);

        await _middleware.InvokeAsync(context);

        VerifyLogged(LogLevel.Error, "Request forwarding failed: 127.0.0.1 GET /test Test-Agent", exception);
        VerifyLogged(LogLevel.Information, "Request allowed: 127.0.0.1 GET /test Test-Agent", times: Times.Never());
        var scope = Assert.Single(_scopes);
        Assert.Equal(502, scope["StatusCode"]);
        Assert.Equal("Request", scope["ForwarderError"]);
    }

    [Fact]
    public async Task InvokeAsync_LongValues_TruncatesLoggedValues()
    {
        var longPath = "/" + new string('a', 2000);
        var context = CreateHttpContext(longPath);
        context.Request.QueryString = new QueryString("?q=" + new string('b', 2000));

        await _middleware.InvokeAsync(context);

        _mockPathValidator.Verify(x => x.FindBlockingRule(longPath), Times.Once);
        VerifyLogged(LogLevel.Information, $"Request allowed: 127.0.0.1 GET {longPath[..1024]} Test-Agent");
        Assert.Equal(1024, ((string)_scopes.Single()["QueryString"]!).Length);
    }

    private void VerifyLogged(LogLevel level, string message, Exception? exception = null, Times? times = null)
    {
        _mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state != null && state.ToString() == message),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times ?? Times.Once());
    }

    private static DefaultHttpContext CreateHttpContext(string path = "/")
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = "GET";
        context.Request.Host = new HostString("example.com");
        context.Request.Headers.UserAgent = "Test-Agent";
        context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        context.Response.Body = new MemoryStream();
        return context;
    }

    private sealed class TestForwarderErrorFeature(ForwarderError error, Exception? exception) : IForwarderErrorFeature
    {
        public ForwarderError Error { get; } = error;
        public Exception? Exception { get; } = exception;
    }
}
