using Buzzard.Models;
using Buzzard.Services;

namespace Buzzard.Tests;

public class PathValidatorTests
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/status")]
    [InlineData("/api/health")]
    public void IsPathAllowed_Contains_ReturnsTrue(string path)
    {
        var config = new PathConfig();
        config.Allow.Contains.UnionWith(["/health", "/status"]);
        var validator = new PathValidator(config);

        Assert.True(validator.IsPathAllowed(path));
    }

    [Theory]
    [InlineData("/api/public/users")]
    [InlineData("/api/public/data")]
    public void IsPathAllowed_StartsWith_ReturnsTrue(string path)
    {
        var config = new PathConfig();
        config.Allow.StartsWith.Add("/api/public");
        var validator = new PathValidator(config);

        Assert.True(validator.IsPathAllowed(path));
    }

    [Theory]
    [InlineData("/styles.css")]
    [InlineData("/app.js")]
    [InlineData("/logo.png")]
    public void IsPathAllowed_EndsWith_ReturnsTrue(string path)
    {
        var config = new PathConfig();
        config.Allow.EndsWith.UnionWith([".css", ".js", ".png"]);
        var validator = new PathValidator(config);

        Assert.True(validator.IsPathAllowed(path));
    }

    [Theory]
    [InlineData("/admin")]
    [InlineData("/config")]
    [InlineData("/api/admin")]
    public void IsPathDenied_Contains_ReturnsTrue(string path)
    {
        var config = new PathConfig();
        config.Deny.Contains.UnionWith(["/admin", "/config"]);
        var validator = new PathValidator(config);

        Assert.True(validator.IsPathDenied(path));
    }

    [Theory]
    [InlineData("/api/private/users")]
    [InlineData("/private/data")]
    public void IsPathDenied_StartsWith_ReturnsTrue(string path)
    {
        var config = new PathConfig();
        config.Deny.StartsWith.UnionWith(["/api/private", "/private/"]);
        var validator = new PathValidator(config);

        var result = validator.IsPathDenied(path);

        Assert.True(result);
    }

    [Theory]
    [InlineData("/config.bak")]
    [InlineData("/data.tmp")]
    public void IsPathDenied_EndsWith_ReturnsTrue(string path)
    {
        var config = new PathConfig();
        config.Deny.EndsWith.UnionWith([".bak", ".tmp"]);
        var validator = new PathValidator(config);

        var result = validator.IsPathDenied(path);

        Assert.True(result);
    }

    [Fact]
    public void IsPathBlocked_AllowOverridesDeny_ReturnsFalse()
    {
        var config = new PathConfig();
        config.Allow.Contains.Add("/admin/health");
        config.Deny.StartsWith.Add("/admin");
        var validator = new PathValidator(config);

        var result = validator.IsPathBlocked("/admin/health");

        Assert.False(result);
    }

    [Fact]
    public void IsPathBlocked_OnlyDenyMatches_ReturnsTrue()
    {
        var config = new PathConfig();
        config.Deny.StartsWith.Add("/admin");
        var validator = new PathValidator(config);

        var result = validator.IsPathBlocked("/admin/users");

        Assert.True(result);
    }

    [Fact]
    public void IsPathBlocked_NoRulesMatch_ReturnsFalse()
    {
        var config = new PathConfig();
        var validator = new PathValidator(config);

        var result = validator.IsPathBlocked("/some/random/path");

        Assert.False(result);
    }

    [Theory]
    [InlineData("/ADMIN", "/admin")]
    [InlineData("/Admin/Users", "/admin")]
    [InlineData("/API/ADMIN", "/admin")]
    public void IsPathDenied_CaseInsensitive_ReturnsTrue(string path, string pattern)
    {
        var config = new PathConfig();
        config.Deny.Contains.Add(pattern);
        var validator = new PathValidator(config);

        Assert.True(validator.IsPathDenied(path));
    }

    [Fact]
    public void PathRules_Contains_AutomaticallyConvertsToLowerCase()
    {
        var pathRules = new PathRules();

        pathRules.Contains = new HashSet<string> { "ADMIN", "Api", "USER" };

        Assert.Equal(new HashSet<string> { "admin", "api", "user" }, pathRules.Contains);
    }

    [Fact]
    public void PathRules_StartsWith_AutomaticallyConvertsToLowerCase()
    {
        var pathRules = new PathRules();

        pathRules.StartsWith = new HashSet<string> { "/API/", "/Admin/" };

        Assert.Equal(new HashSet<string> { "/api/", "/admin/" }, pathRules.StartsWith);
    }

    [Fact]
    public void PathRules_EndsWith_AutomaticallyConvertsToLowerCase()
    {
        var pathRules = new PathRules();

        pathRules.EndsWith = new HashSet<string> { ".CSS", ".JS", ".PNG" };

        Assert.Equal(new HashSet<string> { ".css", ".js", ".png" }, pathRules.EndsWith);
    }

    [Fact]
    public void PathRules_SetNull_CreatesEmptyList()
    {
        var pathRules = new PathRules();

        pathRules.Contains = null;
        pathRules.StartsWith = null;
        pathRules.EndsWith = null;

        Assert.Empty(pathRules.Contains);
        Assert.Empty(pathRules.StartsWith);
        Assert.Empty(pathRules.EndsWith);
    }
}
