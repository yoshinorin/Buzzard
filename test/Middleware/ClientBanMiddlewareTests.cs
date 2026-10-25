using System.Net;
using Buzzard.Extensions;
using Buzzard.Middleware;
using Buzzard.Models;
using Buzzard.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Yarp.ReverseProxy.Forwarder;

namespace Buzzard.Tests;

public class ClientBanMiddlewareTests
{
    private const string ClientIp = "203.0.113.10";

    private readonly FakeTimeProvider _timeProvider = new();
    private Action<HttpContext> _respond = context => context.Response.StatusCode = StatusCodes.Status404NotFound;
    private int _nextCalls;

    [Fact]
    public async Task BelowThreshold_IsNotBanned()
    {
        var middleware = CreateMiddleware();
        await SendErrorsAsync(middleware, 2);

        Assert.Equal(StatusCodes.Status200OK, await SendSuccessAsync(middleware));
    }

    [Theory]
    [InlineData(StatusCodes.Status403Forbidden)]
    [InlineData(StatusCodes.Status404NotFound)]
    public async Task ReachingThreshold_BlocksSubsequentRequests(int statusCode)
    {
        var middleware = CreateMiddleware();
        _respond = context => context.Response.StatusCode = statusCode;
        await SendErrorsAsync(middleware, 3);
        var nextCallsBeforeBan = _nextCalls;

        Assert.Equal(StatusCodes.Status403Forbidden, await SendSuccessAsync(middleware));
        Assert.Equal(nextCallsBeforeBan, _nextCalls);
    }

    [Fact]
    public async Task Ban_IsLiftedAfterBanSeconds()
    {
        var middleware = CreateMiddleware();
        await SendErrorsAsync(middleware, 3);

        _timeProvider.Advance(TimeSpan.FromSeconds(599));
        Assert.Equal(StatusCodes.Status403Forbidden, await SendSuccessAsync(middleware));

        _timeProvider.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(StatusCodes.Status200OK, await SendSuccessAsync(middleware));
    }

    [Fact]
    public async Task ErrorsOutsideWindow_AreNotCounted()
    {
        var middleware = CreateMiddleware();
        await SendErrorsAsync(middleware, 2);
        _timeProvider.Advance(TimeSpan.FromSeconds(10));
        await SendErrorsAsync(middleware, 1);

        Assert.Equal(StatusCodes.Status200OK, await SendSuccessAsync(middleware));
    }

    [Theory]
    [InlineData(StatusCodes.Status500InternalServerError)]
    [InlineData(StatusCodes.Status502BadGateway)]
    [InlineData(StatusCodes.Status503ServiceUnavailable)]
    [InlineData(StatusCodes.Status504GatewayTimeout)]
    [InlineData(StatusCodes.Status499ClientClosedRequest)]
    public async Task UncountedStatusCodes_DoNotBan(int statusCode)
    {
        var middleware = CreateMiddleware();
        _respond = context => context.Response.StatusCode = statusCode;
        await SendErrorsAsync(middleware, 3);

        Assert.Equal(StatusCodes.Status200OK, await SendSuccessAsync(middleware));
    }

    [Fact]
    public async Task CountedStatusCodes_CanBeConfigured()
    {
        var middleware = CreateMiddleware(config => config.CountedStatusCodes = [StatusCodes.Status500InternalServerError]);
        await SendErrorsAsync(middleware, 3);
        Assert.Equal(StatusCodes.Status200OK, await SendSuccessAsync(middleware));

        _respond = context => context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await SendErrorsAsync(middleware, 3);
        Assert.Equal(StatusCodes.Status403Forbidden, await SendSuccessAsync(middleware));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task PathRuleWeight_AppliesOnlyToPathRuleBlocks(bool blockedByPathRule, bool banned)
    {
        var middleware = CreateMiddleware(config => config.PathRuleWeight = 3);
        _respond = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            if (blockedByPathRule)
            {
                context.MarkBlockedByPathRule();
            }
        };
        await SendErrorsAsync(middleware, 1);

        Assert.Equal(banned ? StatusCodes.Status403Forbidden : StatusCodes.Status200OK, await SendSuccessAsync(middleware));
    }

    [Fact]
    public async Task ForwarderErrors_AreNotCounted()
    {
        var middleware = CreateMiddleware();
        _respond = context =>
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Features.Set<IForwarderErrorFeature>(new TestForwarderErrorFeature());
        };
        await SendErrorsAsync(middleware, 3);

        Assert.Equal(StatusCodes.Status200OK, await SendSuccessAsync(middleware));
    }

    [Fact]
    public async Task SafetyActivation_SuspendsNewBansAndKeepsExistingBans()
    {
        var middleware = CreateMiddleware();
        await SendErrorsAsync(middleware, 3, "203.0.113.1");
        await SendErrorsAsync(middleware, 3, "203.0.113.2");
        await SendErrorsAsync(middleware, 3, "203.0.113.3");
        await SendErrorsAsync(middleware, 3, "203.0.113.4");

        Assert.Equal(StatusCodes.Status403Forbidden, await SendSuccessAsync(middleware, "203.0.113.1"));
        Assert.Equal(StatusCodes.Status403Forbidden, await SendSuccessAsync(middleware, "203.0.113.2"));
        Assert.Equal(StatusCodes.Status200OK, await SendSuccessAsync(middleware, "203.0.113.3"));
        Assert.Equal(StatusCodes.Status200OK, await SendSuccessAsync(middleware, "203.0.113.4"));
    }

    [Fact]
    public async Task SafetyDeactivation_ResumesNewBans()
    {
        var middleware = CreateMiddleware();
        await SendErrorsAsync(middleware, 3, "203.0.113.1");
        await SendErrorsAsync(middleware, 3, "203.0.113.2");
        await SendErrorsAsync(middleware, 3, "203.0.113.3");

        _timeProvider.Advance(TimeSpan.FromSeconds(60));
        await SendErrorsAsync(middleware, 3, "203.0.113.4");

        Assert.Equal(StatusCodes.Status403Forbidden, await SendSuccessAsync(middleware, "203.0.113.4"));
    }

    [Fact]
    public async Task ObserveMode_DoesNotBlock()
    {
        var middleware = CreateMiddleware(config => config.Mode = ClientBanMode.Observe);
        await SendErrorsAsync(middleware, 3);

        Assert.Equal(StatusCodes.Status200OK, await SendSuccessAsync(middleware));
    }

    [Theory]
    [InlineData("203.0.113.0/24", ClientIp)]
    [InlineData(ClientIp, ClientIp)]
    [InlineData("203.0.113.0/24", "::ffff:203.0.113.10")]
    public async Task ExemptNetworks_AreNotBanned(string exemptNetwork, string remoteIp)
    {
        var middleware = CreateMiddleware(config => config.ExemptNetworks = [exemptNetwork]);
        await SendErrorsAsync(middleware, 3, remoteIp);

        Assert.Equal(StatusCodes.Status200OK, await SendSuccessAsync(middleware, remoteIp));
    }

    [Fact]
    public async Task OtherClients_AreNotAffectedByBan()
    {
        var middleware = CreateMiddleware();
        await SendErrorsAsync(middleware, 3);

        Assert.Equal(StatusCodes.Status200OK, await SendSuccessAsync(middleware, "198.51.100.20"));
    }

    [Theory]
    [InlineData("2001:db8:0:1::2", true)]
    [InlineData("2001:db8:0:1:ffff:ffff:ffff:ffff", true)]
    [InlineData("2001:db8:0:2::1", false)]
    public async Task IPv6_IsBannedPer64ByDefault(string otherIp, bool banned)
    {
        var middleware = CreateMiddleware();
        await SendErrorsAsync(middleware, 3, "2001:db8:0:1::1");

        Assert.Equal(banned ? StatusCodes.Status403Forbidden : StatusCodes.Status200OK, await SendSuccessAsync(middleware, otherIp));
    }

    [Fact]
    public async Task IPv6_ErrorsAreCountedPer64()
    {
        var middleware = CreateMiddleware();
        await SendErrorsAsync(middleware, 1, "2001:db8:0:1::1");
        await SendErrorsAsync(middleware, 1, "2001:db8:0:1::2");
        await SendErrorsAsync(middleware, 1, "2001:db8:0:1::3");

        Assert.Equal(StatusCodes.Status403Forbidden, await SendSuccessAsync(middleware, "2001:db8:0:1::4"));
    }

    [Theory]
    [InlineData("203.0.113.11", false, 32)]
    [InlineData("203.0.113.11", true, 24)]
    [InlineData("203.0.114.10", false, 24)]
    public async Task IPv4Prefix_CanBeConfigured(string otherIp, bool banned, int prefix)
    {
        var middleware = CreateMiddleware(config => config.IPv4Prefix = prefix);
        await SendErrorsAsync(middleware, 3);

        Assert.Equal(banned ? StatusCodes.Status403Forbidden : StatusCodes.Status200OK, await SendSuccessAsync(middleware, otherIp));
    }

    [Fact]
    public async Task IPv6Prefix_128_BansPerAddress()
    {
        var middleware = CreateMiddleware(config => config.IPv6Prefix = 128);
        await SendErrorsAsync(middleware, 3, "2001:db8:0:1::1");

        Assert.Equal(StatusCodes.Status403Forbidden, await SendSuccessAsync(middleware, "2001:db8:0:1::1"));
        Assert.Equal(StatusCodes.Status200OK, await SendSuccessAsync(middleware, "2001:db8:0:1::2"));
    }

    [Fact]
    public async Task IPv4MappedIPv6_UsesIPv4Prefix()
    {
        var middleware = CreateMiddleware();
        await SendErrorsAsync(middleware, 3, "::ffff:203.0.113.10");

        Assert.Equal(StatusCodes.Status403Forbidden, await SendSuccessAsync(middleware));
        Assert.Equal(StatusCodes.Status200OK, await SendSuccessAsync(middleware, "203.0.113.11"));
    }

    [Fact]
    public async Task RequestCount_IsDisabledByDefault()
    {
        var middleware = CreateMiddleware();
        await SendRequestsAsync(middleware, 3, "/articles/a/");

        Assert.Equal(StatusCodes.Status200OK, await SendSuccessAsync(middleware));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/articles/a")]
    [InlineData("/articles/a/")]
    [InlineData("/articles/a.b/")]
    public async Task ExtensionlessRequests_ReachingThreshold_Ban(string path)
    {
        var middleware = CreateMiddleware(EnableRequestCount);
        await SendRequestsAsync(middleware, 3, path);

        Assert.Equal(StatusCodes.Status403Forbidden, await SendSuccessAsync(middleware));
    }

    [Theory]
    [InlineData("/thumb.png")]
    [InlineData("/_next/static/chunks/a.js")]
    [InlineData("/feeds/index.xml")]
    public async Task RequestsWithExtension_AreNotCounted(string path)
    {
        var middleware = CreateMiddleware(EnableRequestCount);
        await SendRequestsAsync(middleware, 3, path);

        Assert.Equal(StatusCodes.Status200OK, await SendSuccessAsync(middleware));
    }

    [Theory]
    [InlineData("/index.html", true)]
    [InlineData("/INDEX.HTML", true)]
    [InlineData("/articles/a/", true)]
    [InlineData("/thumb.png", false)]
    public async Task RequestCount_ExtensionsCanBeConfigured(string path, bool banned)
    {
        var middleware = CreateMiddleware(config =>
        {
            EnableRequestCount(config);
            config.RequestCount.Extensions = ["", "html"];
        });
        await SendRequestsAsync(middleware, 3, path);

        Assert.Equal(banned ? StatusCodes.Status403Forbidden : StatusCodes.Status200OK, await SendSuccessAsync(middleware, path: "/thumb.png"));
    }

    [Fact]
    public async Task RequestCount_EmptyExtensions_CountsAllPaths()
    {
        var middleware = CreateMiddleware(config =>
        {
            EnableRequestCount(config);
            config.RequestCount.Extensions = [];
        });
        await SendRequestsAsync(middleware, 3, "/thumb.png");

        Assert.Equal(StatusCodes.Status403Forbidden, await SendSuccessAsync(middleware));
    }

    [Theory]
    [InlineData("GET", true)]
    [InlineData("HEAD", true)]
    [InlineData("POST", false)]
    public async Task RequestCount_CountsDefaultMethods(string method, bool banned)
    {
        var middleware = CreateMiddleware(EnableRequestCount);
        await SendRequestsAsync(middleware, 3, "/articles/a/", method);

        Assert.Equal(banned ? StatusCodes.Status403Forbidden : StatusCodes.Status200OK, await SendSuccessAsync(middleware));
    }

    [Fact]
    public async Task RequestCount_MethodsCanBeConfigured()
    {
        var middleware = CreateMiddleware(config =>
        {
            EnableRequestCount(config);
            config.RequestCount.Methods = ["post"];
        });
        await SendRequestsAsync(middleware, 3, "/articles/a/");
        Assert.Equal(StatusCodes.Status200OK, await SendSuccessAsync(middleware));

        await SendRequestsAsync(middleware, 3, "/articles/a/", HttpMethods.Post);
        Assert.Equal(StatusCodes.Status403Forbidden, await SendSuccessAsync(middleware));
    }

    [Fact]
    public async Task RequestCount_CountsRequestsWithQueryByDefault()
    {
        var middleware = CreateMiddleware(EnableRequestCount);
        await SendRequestsAsync(middleware, 3, "/articles/a/", query: "?_rsc=abc");

        Assert.Equal(StatusCodes.Status403Forbidden, await SendSuccessAsync(middleware));
    }

    [Fact]
    public async Task RequestCount_ExcludedQueryKeysCanBeConfigured()
    {
        var middleware = CreateMiddleware(config =>
        {
            EnableRequestCount(config);
            config.RequestCount.ExcludedQueryKeys = ["preview"];
        });
        await SendRequestsAsync(middleware, 3, "/articles/a/", query: "?preview=1");
        Assert.Equal(StatusCodes.Status200OK, await SendSuccessAsync(middleware));

        await SendRequestsAsync(middleware, 3, "/articles/a/", query: "?_rsc=abc");
        Assert.Equal(StatusCodes.Status403Forbidden, await SendSuccessAsync(middleware));
    }

    [Fact]
    public async Task BannedRequest_LogsWarningWithRule()
    {
        var logger = new Mock<ILogger<ClientBanMiddleware>>();
        logger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        var scopes = new List<Dictionary<string, object?>>();
        logger
            .Setup(x => x.BeginScope(It.IsAny<Dictionary<string, object?>>()))
            .Callback<Dictionary<string, object?>>(scopes.Add)
            .Returns(Mock.Of<IDisposable>());
        var middleware = CreateMiddleware(logger: logger.Object);
        await SendErrorsAsync(middleware, 3);

        await SendAsync(middleware, ClientIp, "/blocked", query: "?token=secret");

        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state != null && state.ToString() == $"Request blocked by ClientBan: {ClientIp} GET /blocked "),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once());
        var scope = Assert.Single(scopes);
        Assert.Equal("ClientBan", scope["Rule"]);
        Assert.Equal("?token=***", scope["QueryString"]);
    }

    [Fact]
    public async Task ErrorsAndRequests_AreCountedSeparately()
    {
        var middleware = CreateMiddleware(EnableRequestCount);
        _respond = context => context.Response.StatusCode = StatusCodes.Status404NotFound;
        await SendErrorsAsync(middleware, 2, path: "/missing.png");
        await SendRequestsAsync(middleware, 2, "/articles/a/");

        Assert.Equal(StatusCodes.Status200OK, await SendSuccessAsync(middleware, path: "/thumb.png"));
    }

    private static void EnableRequestCount(ClientBanConfig config)
    {
        config.RequestCount.Enabled = true;
    }

    private ClientBanMiddleware CreateMiddleware(Action<ClientBanConfig>? configure = null, ILogger<ClientBanMiddleware>? logger = null)
    {
        var config = new ClientBanConfig
        {
            Mode = ClientBanMode.Enforce,
            Threshold = 3,
            WindowSeconds = 10,
            BanSeconds = 600,
            GlobalSafetyThreshold = 2,
            GlobalSafetyWindowSeconds = 60,
        };
        configure?.Invoke(config);
        var options = Options.Create(config);
        var store = new ClientBanStore(options, _timeProvider, NullLogger<ClientBanStore>.Instance);
        RequestDelegate next = context =>
        {
            _nextCalls++;
            _respond(context);
            return Task.CompletedTask;
        };
        var requestLogScope = new RequestLogScope(new QueryStringMasker(new RequestLoggingConfig()), new HeaderMasker(new RequestLoggingConfig()));
        return new ClientBanMiddleware(next, logger ?? NullLogger<ClientBanMiddleware>.Instance, store, options, requestLogScope);
    }

    private static async Task SendErrorsAsync(ClientBanMiddleware middleware, int count, string remoteIp = ClientIp, string path = "/test")
    {
        for (var i = 0; i < count; i++)
        {
            await SendAsync(middleware, remoteIp, path);
        }
    }

    private async Task SendRequestsAsync(ClientBanMiddleware middleware, int count, string path, string method = "GET", string query = "")
    {
        _respond = context => context.Response.StatusCode = StatusCodes.Status200OK;
        for (var i = 0; i < count; i++)
        {
            await SendAsync(middleware, ClientIp, path, method, query);
        }
    }

    private async Task<int> SendSuccessAsync(ClientBanMiddleware middleware, string remoteIp = ClientIp, string path = "/test")
    {
        _respond = context => context.Response.StatusCode = StatusCodes.Status200OK;
        return await SendAsync(middleware, remoteIp, path);
    }

    private static async Task<int> SendAsync(ClientBanMiddleware middleware, string remoteIp, string path, string method = "GET", string query = "")
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.QueryString = new QueryString(query);
        await middleware.InvokeAsync(context);
        return context.Response.StatusCode;
    }

    private sealed class TestForwarderErrorFeature : IForwarderErrorFeature
    {
        public ForwarderError Error => ForwarderError.Request;
        public Exception? Exception => null;
    }
}
