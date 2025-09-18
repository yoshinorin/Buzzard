using Buzzard.Models;
using Buzzard.Services;

namespace Buzzard.Tests;

public class UserAgentValidatorTests
{
    [Theory]
    [InlineData("TestBot/1.0")]
    [InlineData("SampleCrawler/2.1")]
    [InlineData("ExampleBot/1.5")]
    public void IsUserAgentAllowed_Contains_ReturnsTrue(string userAgent)
    {
        var firewallConfig = new FirewallConfig();
        firewallConfig.UserAgent.Allow.Contains.UnionWith(["testbot", "samplecrawler", "examplebot"]);
        var validator = new UserAgentValidator(firewallConfig);

        Assert.True(validator.IsUserAgentAllowed(userAgent));
    }

    [Theory]
    [InlineData("TestBrowser/5.0 (compatible)")]
    [InlineData("TestCrawler/2.0 (bot)")]
    public void IsUserAgentAllowed_StartsWith_ReturnsTrue(string userAgent)
    {
        var firewallConfig = new FirewallConfig();
        firewallConfig.UserAgent.Allow.StartsWith.Add("test");
        var validator = new UserAgentValidator(firewallConfig);

        Assert.True(validator.IsUserAgentAllowed(userAgent));
    }

    [Theory]
    [InlineData("SomeBot/1.0 (+http://example.test/bot)")]
    [InlineData("Crawler/2.0 (+http://test.example)")]
    public void IsUserAgentAllowed_EndsWith_ReturnsTrue(string userAgent)
    {
        var firewallConfig = new FirewallConfig();
        firewallConfig.UserAgent.Allow.EndsWith.UnionWith(["+http://example.test/bot)", "+http://test.example)"]);
        var validator = new UserAgentValidator(firewallConfig);

        Assert.True(validator.IsUserAgentAllowed(userAgent));
    }

    [Theory]
    [InlineData("BadBot/1.0")]
    [InlineData("SpamCrawler/2.0")]
    [InlineData("MaliciousBot/1.5")]
    public void IsUserAgentDenied_Contains_ReturnsTrue(string userAgent)
    {
        var firewallConfig = new FirewallConfig();
        firewallConfig.UserAgent.Deny.Contains.UnionWith(["badbot", "spam", "malicious"]);
        var validator = new UserAgentValidator(firewallConfig);

        Assert.True(validator.IsUserAgentDenied(userAgent));
    }

    [Theory]
    [InlineData("BadBot/1.0 (compatible)")]
    [InlineData("BadBotCrawler/2.0")]
    public void IsUserAgentDenied_StartsWith_ReturnsTrue(string userAgent)
    {
        var firewallConfig = new FirewallConfig();
        firewallConfig.UserAgent.Deny.StartsWith.Add("badbot");
        var validator = new UserAgentValidator(firewallConfig);

        Assert.True(validator.IsUserAgentDenied(userAgent));
    }

    [Theory]
    [InlineData("SomeBot/1.0 (scraper)")]
    [InlineData("Crawler/2.0 (spam)")]
    public void IsUserAgentDenied_EndsWith_ReturnsTrue(string userAgent)
    {
        var firewallConfig = new FirewallConfig();
        firewallConfig.UserAgent.Deny.EndsWith.UnionWith(["(scraper)", "(spam)"]);
        var validator = new UserAgentValidator(firewallConfig);

        Assert.True(validator.IsUserAgentDenied(userAgent));
    }

    [Fact]
    public void IsUserAgentBlocked_AllowOverridesDeny_ReturnsFalse()
    {
        var firewallConfig = new FirewallConfig();
        firewallConfig.UserAgent.Allow.Contains.Add("goodbot");
        firewallConfig.UserAgent.Deny.StartsWith.Add("good");
        var validator = new UserAgentValidator(firewallConfig);

        var result = validator.IsUserAgentBlocked("GoodBot/1.0");

        Assert.False(result);
    }

    [Fact]
    public void IsUserAgentBlocked_OnlyDenyMatches_ReturnsTrue()
    {
        var firewallConfig = new FirewallConfig();
        firewallConfig.UserAgent.Deny.StartsWith.Add("bad");
        var validator = new UserAgentValidator(firewallConfig);

        var result = validator.IsUserAgentBlocked("BadBot/1.0");

        Assert.True(result);
    }

    [Fact]
    public void IsUserAgentBlocked_NoRulesMatch_ReturnsFalse()
    {
        var firewallConfig = new FirewallConfig();
        var validator = new UserAgentValidator(firewallConfig);

        var result = validator.IsUserAgentBlocked("RandomBot/1.0");

        Assert.False(result);
    }

    [Theory]
    [InlineData("BADBOT/1.0", "badbot")]
    [InlineData("BadBot/1.0", "badbot")]
    [InlineData("TESTBOT/2.1", "testbot")]
    public void IsUserAgentDenied_CaseInsensitive_ReturnsTrue(string userAgent, string pattern)
    {
        var firewallConfig = new FirewallConfig();
        firewallConfig.UserAgent.Deny.Contains.Add(pattern);
        var validator = new UserAgentValidator(firewallConfig);

        Assert.True(validator.IsUserAgentDenied(userAgent));
    }

    [Fact]
    public void UserAgentRules_Contains_AutomaticallyConvertsToLowerCase()
    {
        var userAgentRules = new UserAgentRules();

        userAgentRules.Contains = new HashSet<string> { "TESTBOT", "SampleBot", "EXAMPLEBOT" };

        Assert.Equal(new HashSet<string> { "testbot", "samplebot", "examplebot" }, userAgentRules.Contains);
    }

    [Fact]
    public void UserAgentRules_StartsWith_AutomaticallyConvertsToLowerCase()
    {
        var userAgentRules = new UserAgentRules();

        userAgentRules.StartsWith = new HashSet<string> { "TestAgent/5.0", "SAMPLE/" };

        Assert.Equal(new HashSet<string> { "testagent/5.0", "sample/" }, userAgentRules.StartsWith);
    }

    [Fact]
    public void UserAgentRules_EndsWith_AutomaticallyConvertsToLowerCase()
    {
        var userAgentRules = new UserAgentRules();

        userAgentRules.EndsWith = new HashSet<string> { "+HTTP://TEST.EXAMPLE)", "(COMPATIBLE)" };

        Assert.Equal(new HashSet<string> { "+http://test.example)", "(compatible)" }, userAgentRules.EndsWith);
    }

    [Fact]
    public void UserAgentRules_SetNull_CreatesEmptySet()
    {
        var userAgentRules = new UserAgentRules();

        userAgentRules.Contains = null;
        userAgentRules.StartsWith = null;
        userAgentRules.EndsWith = null;

        Assert.Empty(userAgentRules.Contains);
        Assert.Empty(userAgentRules.StartsWith);
        Assert.Empty(userAgentRules.EndsWith);
    }
}
