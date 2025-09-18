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

        config.Allow.Contains = new HashSet<string> { "ADMIN", "Api", "USER" };

        Assert.Equal(new HashSet<string> { "admin", "api", "user" }, config.Allow.Contains);
    }

    [Fact]
    public void PathConfig_Allow_StartsWith_AutomaticallyConvertsToLowerCase()
    {
        var config = new PathConfig();

        config.Allow.StartsWith = new HashSet<string> { "/API/", "/Admin/" };

        Assert.Equal(new HashSet<string> { "/api/", "/admin/" }, config.Allow.StartsWith);
    }

    [Fact]
    public void PathConfig_Allow_EndsWith_AutomaticallyConvertsToLowerCase()
    {
        var config = new PathConfig();

        config.Allow.EndsWith = new HashSet<string> { ".CSS", ".JS", ".PNG" };

        Assert.Equal(new HashSet<string> { ".css", ".js", ".png" }, config.Allow.EndsWith);
    }

    [Fact]
    public void PathConfig_Deny_Contains_AutomaticallyConvertsToLowerCase()
    {
        var config = new PathConfig();

        config.Deny.Contains = new HashSet<string> { "SECRET", "Private", "HIDDEN" };

        Assert.Equal(new HashSet<string> { "secret", "private", "hidden" }, config.Deny.Contains);
    }

    [Fact]
    public void PathConfig_Deny_StartsWith_AutomaticallyConvertsToLowerCase()
    {
        var config = new PathConfig();

        config.Deny.StartsWith = new HashSet<string> { "/PRIVATE/", "/Secret/" };

        Assert.Equal(new HashSet<string> { "/private/", "/secret/" }, config.Deny.StartsWith);
    }

    [Fact]
    public void PathConfig_Deny_EndsWith_AutomaticallyConvertsToLowerCase()
    {
        var config = new PathConfig();

        config.Deny.EndsWith = new HashSet<string> { ".BAK", ".Tmp", ".LOG" };

        Assert.Equal(new HashSet<string> { ".bak", ".tmp", ".log" }, config.Deny.EndsWith);
    }

    [Fact]
    public void PathConfig_MixedCasePatterns_AllConvertedToLowerCase()
    {
        var config = new PathConfig();

        config.Allow.Contains = new HashSet<string> { "MiXeD", "CaSe", "PaTtErN" };
        config.Deny.StartsWith = new HashSet<string> { "/MiXeD/", "/CaSe/" };

        Assert.Equal(new HashSet<string> { "mixed", "case", "pattern" }, config.Allow.Contains);
        Assert.Equal(new HashSet<string> { "/mixed/", "/case/" }, config.Deny.StartsWith);
    }

    [Fact]
    public void PathConfig_Allow_Contains_RemovesDuplicates()
    {
        var config = new PathConfig();

        config.Allow.Contains = new HashSet<string> { "ADMIN", "api", "ADMIN", "User", "API" };

        Assert.Equal(3, config.Allow.Contains.Count);
        Assert.Equal(new HashSet<string> { "admin", "api", "user" }, config.Allow.Contains);
    }

    [Fact]
    public void PathConfig_Allow_StartsWith_RemovesDuplicates()
    {
        var config = new PathConfig();

        config.Allow.StartsWith = new HashSet<string> { "/API/", "/Admin/", "/api/", "/USER/" };

        Assert.Equal(3, config.Allow.StartsWith.Count);
        Assert.Equal(new HashSet<string> { "/api/", "/admin/", "/user/" }, config.Allow.StartsWith);
    }

    [Fact]
    public void PathConfig_Allow_EndsWith_RemovesDuplicates()
    {
        var config = new PathConfig();

        config.Allow.EndsWith = new HashSet<string> { ".CSS", ".js", ".css", ".PNG", ".JS" };

        Assert.Equal(3, config.Allow.EndsWith.Count);
        Assert.Equal(new HashSet<string> { ".css", ".js", ".png" }, config.Allow.EndsWith);
    }

    [Fact]
    public void PathConfig_Deny_Contains_RemovesDuplicates()
    {
        var config = new PathConfig();

        config.Deny.Contains = new HashSet<string> { "SECRET", "Private", "secret", "HIDDEN" };

        Assert.Equal(3, config.Deny.Contains.Count);
        Assert.Equal(new HashSet<string> { "secret", "private", "hidden" }, config.Deny.Contains);
    }

    [Fact]
    public void PathConfig_Deny_StartsWith_RemovesDuplicates()
    {
        var config = new PathConfig();

        config.Deny.StartsWith = new HashSet<string> { "/PRIVATE/", "/Secret/", "/private/" };

        Assert.Equal(2, config.Deny.StartsWith.Count);
        Assert.Equal(new HashSet<string> { "/private/", "/secret/" }, config.Deny.StartsWith);
    }

    [Fact]
    public void PathConfig_Deny_EndsWith_RemovesDuplicates()
    {
        var config = new PathConfig();

        config.Deny.EndsWith = new HashSet<string> { ".BAK", ".tmp", ".bak", ".LOG", ".TMP" };

        Assert.Equal(3, config.Deny.EndsWith.Count);
        Assert.Equal(new HashSet<string> { ".bak", ".tmp", ".log" }, config.Deny.EndsWith);
    }

    [Fact]
    public void PathConfig_CaseInsensitiveDuplicates_RemovesDuplicatesAfterLowerCase()
    {
        var config = new PathConfig();

        config.Allow.Contains = new HashSet<string> { "ADMIN", "admin", "Admin", "API", "api" };

        Assert.Equal(2, config.Allow.Contains.Count);
        Assert.Equal(new HashSet<string> { "admin", "api" }, config.Allow.Contains);
    }
}
