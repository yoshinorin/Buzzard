using Buzzard.Models;

namespace Buzzard.Tests;

public class UserAgentConfigTests
{
    [Fact]
    public void UserAgentConfig_Allow_InitializesCorrectly()
    {
        var config = new UserAgentConfig();

        Assert.NotNull(config.Allow);
        Assert.Empty(config.Allow.Contains);
        Assert.Empty(config.Allow.StartsWith);
        Assert.Empty(config.Allow.EndsWith);
    }

    [Fact]
    public void UserAgentConfig_Deny_InitializesCorrectly()
    {
        var config = new UserAgentConfig();

        Assert.NotNull(config.Deny);
        Assert.Empty(config.Deny.Contains);
        Assert.Empty(config.Deny.StartsWith);
        Assert.Empty(config.Deny.EndsWith);
    }

    [Fact]
    public void UserAgentConfig_Allow_Contains_AutomaticallyConvertsToLowerCase()
    {
        var config = new UserAgentConfig();

        config.Allow.Contains = new HashSet<string> { "TESTBOT", "SampleBot", "EXAMPLEBOT" };

        Assert.Equal(new HashSet<string> { "testbot", "samplebot", "examplebot" }, config.Allow.Contains);
    }

    [Fact]
    public void UserAgentConfig_Allow_StartsWith_AutomaticallyConvertsToLowerCase()
    {
        var config = new UserAgentConfig();

        config.Allow.StartsWith = new HashSet<string> { "TESTBROWSER", "SampleBot" };

        Assert.Equal(new HashSet<string> { "testbrowser", "samplebot" }, config.Allow.StartsWith);
    }

    [Fact]
    public void UserAgentConfig_Allow_EndsWith_AutomaticallyConvertsToLowerCase()
    {
        var config = new UserAgentConfig();

        config.Allow.EndsWith = new HashSet<string> { "(COMPATIBLE)", "+HTTP://EXAMPLE.TEST)" };

        Assert.Equal(new HashSet<string> { "(compatible)", "+http://example.test)" }, config.Allow.EndsWith);
    }

    [Fact]
    public void UserAgentConfig_Deny_Contains_AutomaticallyConvertsToLowerCase()
    {
        var config = new UserAgentConfig();

        config.Deny.Contains = new HashSet<string> { "BADBOT", "SpamBot", "MALICIOUS" };

        Assert.Equal(new HashSet<string> { "badbot", "spambot", "malicious" }, config.Deny.Contains);
    }

    [Fact]
    public void UserAgentConfig_Deny_StartsWith_AutomaticallyConvertsToLowerCase()
    {
        var config = new UserAgentConfig();

        config.Deny.StartsWith = new HashSet<string> { "BADBOT", "SpamCrawler" };

        Assert.Equal(new HashSet<string> { "badbot", "spamcrawler" }, config.Deny.StartsWith);
    }

    [Fact]
    public void UserAgentConfig_Deny_EndsWith_AutomaticallyConvertsToLowerCase()
    {
        var config = new UserAgentConfig();

        config.Deny.EndsWith = new HashSet<string> { "(SCRAPER)", "(SPAM)" };

        Assert.Equal(new HashSet<string> { "(scraper)", "(spam)" }, config.Deny.EndsWith);
    }

    [Fact]
    public void UserAgentConfig_MixedCasePatterns_AllConvertedToLowerCase()
    {
        var config = new UserAgentConfig();

        config.Allow.Contains = new HashSet<string> { "TeStBoT", "SaMpLeBoT", "ExAmPlE" };
        config.Deny.StartsWith = new HashSet<string> { "BaDBoT", "SpAmBoT" };

        Assert.Equal(new HashSet<string> { "testbot", "samplebot", "example" }, config.Allow.Contains);
        Assert.Equal(new HashSet<string> { "badbot", "spambot" }, config.Deny.StartsWith);
    }

    [Fact]
    public void UserAgentConfig_Allow_Contains_RemovesDuplicates()
    {
        var config = new UserAgentConfig();

        config.Allow.Contains = new HashSet<string> { "TESTBOT", "samplebot", "TESTBOT", "ExampleBot", "SAMPLEBOT" };

        Assert.Equal(3, config.Allow.Contains.Count);
        Assert.Equal(new HashSet<string> { "testbot", "samplebot", "examplebot" }, config.Allow.Contains);
    }

    [Fact]
    public void UserAgentConfig_Allow_StartsWith_RemovesDuplicates()
    {
        var config = new UserAgentConfig();

        config.Allow.StartsWith = new HashSet<string> { "TESTBOT", "SampleBot", "testbot", "EXAMPLEBOT" };

        Assert.Equal(3, config.Allow.StartsWith.Count);
        Assert.Equal(new HashSet<string> { "testbot", "samplebot", "examplebot" }, config.Allow.StartsWith);
    }

    [Fact]
    public void UserAgentConfig_Allow_EndsWith_RemovesDuplicates()
    {
        var config = new UserAgentConfig();

        config.Allow.EndsWith = new HashSet<string> { "(COMPATIBLE)", "(bot)", "(compatible)", "(TEST)", "(BOT)" };

        Assert.Equal(3, config.Allow.EndsWith.Count);
        Assert.Equal(new HashSet<string> { "(compatible)", "(bot)", "(test)" }, config.Allow.EndsWith);
    }

    [Fact]
    public void UserAgentConfig_Deny_Contains_RemovesDuplicates()
    {
        var config = new UserAgentConfig();

        config.Deny.Contains = new HashSet<string> { "BADBOT", "SpamBot", "badbot", "MALICIOUS" };

        Assert.Equal(3, config.Deny.Contains.Count);
        Assert.Equal(new HashSet<string> { "badbot", "spambot", "malicious" }, config.Deny.Contains);
    }

    [Fact]
    public void UserAgentConfig_Deny_StartsWith_RemovesDuplicates()
    {
        var config = new UserAgentConfig();

        config.Deny.StartsWith = new HashSet<string> { "BADBOT", "SpamBot", "badbot" };

        Assert.Equal(2, config.Deny.StartsWith.Count);
        Assert.Equal(new HashSet<string> { "badbot", "spambot" }, config.Deny.StartsWith);
    }

    [Fact]
    public void UserAgentConfig_Deny_EndsWith_RemovesDuplicates()
    {
        var config = new UserAgentConfig();

        config.Deny.EndsWith = new HashSet<string> { "(SCRAPER)", "(spam)", "(scraper)", "(MALICIOUS)", "(SPAM)" };

        Assert.Equal(3, config.Deny.EndsWith.Count);
        Assert.Equal(new HashSet<string> { "(scraper)", "(spam)", "(malicious)" }, config.Deny.EndsWith);
    }

    [Fact]
    public void UserAgentConfig_CaseInsensitiveDuplicates_RemovesDuplicatesAfterLowerCase()
    {
        var config = new UserAgentConfig();

        config.Allow.Contains = new HashSet<string> { "TESTBOT", "testbot", "TestBot", "SAMPLEBOT", "samplebot" };

        Assert.Equal(2, config.Allow.Contains.Count);
        Assert.Equal(new HashSet<string> { "testbot", "samplebot" }, config.Allow.Contains);
    }

    [Fact]
    public void UserAgentRules_SetNull_CreatesEmptyHashSet()
    {
        var rules = new UserAgentRules();

        rules.Contains = null!;
        rules.StartsWith = null!;
        rules.EndsWith = null!;

        Assert.NotNull(rules.Contains);
        Assert.NotNull(rules.StartsWith);
        Assert.NotNull(rules.EndsWith);
        Assert.Empty(rules.Contains);
        Assert.Empty(rules.StartsWith);
        Assert.Empty(rules.EndsWith);
    }

    [Fact]
    public void UserAgentRules_Contains_HandleEmptyValues()
    {
        var rules = new UserAgentRules();

        rules.Contains = new HashSet<string> { "", "  ", "testbot", "" };

        Assert.Equal(new HashSet<string> { "", "  ", "testbot" }, rules.Contains);
    }

    [Fact]
    public void UserAgentRules_StartsWith_HandleEmptyValues()
    {
        var rules = new UserAgentRules();

        rules.StartsWith = new HashSet<string> { "", "TestBot", "  " };

        Assert.Equal(new HashSet<string> { "", "testbot", "  " }, rules.StartsWith);
    }

    [Fact]
    public void UserAgentRules_EndsWith_HandleEmptyValues()
    {
        var rules = new UserAgentRules();

        rules.EndsWith = new HashSet<string> { "", "(compatible)", "  " };

        Assert.Equal(new HashSet<string> { "", "(compatible)", "  " }, rules.EndsWith);
    }
}
