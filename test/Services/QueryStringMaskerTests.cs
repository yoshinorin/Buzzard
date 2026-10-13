using Buzzard.Models;
using Buzzard.Services;

namespace Buzzard.Tests;

public class QueryStringMaskerTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("?", "?")]
    [InlineData("?q=1&p=2", "?q=1&p=2")]
    [InlineData("?token=abc", "?token=***")]
    [InlineData("?q=1&TOKEN=abc&p=2", "?q=1&TOKEN=***&p=2")]
    [InlineData("?%74oken=abc", "?%74oken=***")]
    [InlineData("?api+key=abc", "?api+key=abc")]
    [InlineData("?token", "?token")]
    [InlineData("?token=", "?token=***")]
    [InlineData("?token=a=b", "?token=***")]
    [InlineData("?a=1&&token=x", "?a=1&&token=***")]
    [InlineData("token=abc", "token=***")]
    public void Mask_DefaultKeys_MasksMatchedValues(string queryString, string expected)
    {
        var masker = new QueryStringMasker(new RequestLoggingConfig());

        Assert.Equal(expected, masker.Mask(queryString));
    }

    [Fact]
    public void Mask_ConfiguredKeys_ReplacesDefaults()
    {
        var masker = new QueryStringMasker(new RequestLoggingConfig { MaskedQueryKeys = ["session"] });

        Assert.Equal("?session=***&token=abc", masker.Mask("?session=xyz&token=abc"));
    }

    [Fact]
    public void Mask_EmptyKeys_ReturnsOriginal()
    {
        var masker = new QueryStringMasker(new RequestLoggingConfig { MaskedQueryKeys = [] });

        Assert.Equal("?token=abc", masker.Mask("?token=abc"));
    }
}
