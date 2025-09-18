using Buzzard.Models;

namespace Buzzard.Tests;

public class PathConfigTests
{
    [Fact]
    public void PathConfig_Allow_InitializesCorrectly()
    {
        var config = new PathConfig();

        Assert.NotNull(config.Allow);
        Assert.Empty(config.Allow.Contains);
        Assert.Empty(config.Allow.StartsWith);
        Assert.Empty(config.Allow.EndsWith);
    }

    [Fact]
    public void PathConfig_Deny_InitializesCorrectly()
    {
        var config = new PathConfig();

        Assert.NotNull(config.Deny);
        Assert.Empty(config.Deny.Contains);
        Assert.Empty(config.Deny.StartsWith);
        Assert.Empty(config.Deny.EndsWith);
    }

    [Fact]
    public void PathConfig_Allow_Contains_AutomaticallyConvertsToLowerCase()
    {
        var config = new PathConfig();

        config.Allow.Contains = new List<string> { "ADMIN", "Api", "USER" };

        Assert.Equal(new List<string> { "admin", "api", "user" }, config.Allow.Contains);
    }

    [Fact]
    public void PathConfig_Allow_StartsWith_AutomaticallyConvertsToLowerCase()
    {
        var config = new PathConfig();

        config.Allow.StartsWith = new List<string> { "/API/", "/Admin/" };

        Assert.Equal(new List<string> { "/api/", "/admin/" }, config.Allow.StartsWith);
    }

    [Fact]
    public void PathConfig_Allow_EndsWith_AutomaticallyConvertsToLowerCase()
    {
        var config = new PathConfig();

        config.Allow.EndsWith = new List<string> { ".CSS", ".JS", ".PNG" };

        Assert.Equal(new List<string> { ".css", ".js", ".png" }, config.Allow.EndsWith);
    }

    [Fact]
    public void PathConfig_Deny_Contains_AutomaticallyConvertsToLowerCase()
    {
        var config = new PathConfig();

        config.Deny.Contains = new List<string> { "SECRET", "Private", "HIDDEN" };

        Assert.Equal(new List<string> { "secret", "private", "hidden" }, config.Deny.Contains);
    }

    [Fact]
    public void PathConfig_Deny_StartsWith_AutomaticallyConvertsToLowerCase()
    {
        var config = new PathConfig();

        config.Deny.StartsWith = new List<string> { "/PRIVATE/", "/Secret/" };

        Assert.Equal(new List<string> { "/private/", "/secret/" }, config.Deny.StartsWith);
    }

    [Fact]
    public void PathConfig_Deny_EndsWith_AutomaticallyConvertsToLowerCase()
    {
        var config = new PathConfig();

        config.Deny.EndsWith = new List<string> { ".BAK", ".Tmp", ".LOG" };

        Assert.Equal(new List<string> { ".bak", ".tmp", ".log" }, config.Deny.EndsWith);
    }

    [Fact]
    public void PathConfig_MixedCasePatterns_AllConvertedToLowerCase()
    {
        var config = new PathConfig();

        config.Allow.Contains = new List<string> { "MiXeD", "CaSe", "PaTtErN" };
        config.Deny.StartsWith = new List<string> { "/MiXeD/", "/CaSe/" };

        Assert.Equal(new List<string> { "mixed", "case", "pattern" }, config.Allow.Contains);
        Assert.Equal(new List<string> { "/mixed/", "/case/" }, config.Deny.StartsWith);
    }
}
