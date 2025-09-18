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
        config.Allow.Contains.AddRange(["/health", "/status"]);
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
        config.Allow.EndsWith.AddRange([".css", ".js", ".png"]);
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
        config.Deny.Contains.AddRange(["/admin", "/config"]);
        var validator = new PathValidator(config);

        Assert.True(validator.IsPathDenied(path));
    }

    [Theory]
    [InlineData("/api/private/users")]
    [InlineData("/private/data")]
    public void IsPathDenied_StartsWith_ReturnsTrue(string path)
    {
        var config = new PathConfig();
        config.Deny.StartsWith.AddRange(["/api/private", "/private/"]);
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
        config.Deny.EndsWith.AddRange([".bak", ".tmp"]);
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
}
