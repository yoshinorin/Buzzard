using Buzzard.Models;
using Buzzard.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Buzzard.Tests;

public class HeaderMaskerTests
{
    [Fact]
    public void Mask_DefaultHeaders_MasksSensitiveValues()
    {
        var headers = new HeaderDictionary
        {
            ["Authorization"] = "Bearer secret",
            ["proxy-authorization"] = "Basic secret",
            ["Cookie"] = "session=secret",
            ["X-API-KEY"] = "secret",
            ["X-Auth-Token"] = "secret",
            ["CF-Ray"] = "abc123-NRT",
        };
        var masker = new HeaderMasker(new RequestLoggingConfig());

        var result = masker.Mask(headers).ToDictionary();

        Assert.Equal("***", result["authorization"]);
        Assert.Equal("***", result["proxy-authorization"]);
        Assert.Equal("***", result["cookie"]);
        Assert.Equal("***", result["x-api-key"]);
        Assert.Equal("***", result["x-auth-token"]);
        Assert.Equal("abc123-NRT", result["cf-ray"]);
    }

    [Fact]
    public void Mask_MultipleValues_JoinsValues()
    {
        var headers = new HeaderDictionary { ["Accept"] = new StringValues(["text/html", "application/json"]) };
        var masker = new HeaderMasker(new RequestLoggingConfig());

        var result = Assert.Single(masker.Mask(headers));

        Assert.Equal("accept", result.Key);
        Assert.Equal("text/html,application/json", result.Value);
    }

    [Fact]
    public void Mask_ConfiguredHeaders_ReplacesDefaults()
    {
        var headers = new HeaderDictionary
        {
            ["X-Session"] = "secret",
            ["Cookie"] = "session=secret",
        };
        var masker = new HeaderMasker(new RequestLoggingConfig { MaskedHeaders = ["X-Session"] });

        var result = masker.Mask(headers).ToDictionary();

        Assert.Equal("***", result["x-session"]);
        Assert.Equal("session=secret", result["cookie"]);
    }
}
