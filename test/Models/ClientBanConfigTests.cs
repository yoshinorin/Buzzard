using System.Text;
using Buzzard.Models;
using Microsoft.Extensions.Configuration;

namespace Buzzard.Tests;

public class ClientBanConfigTests
{
    [Fact]
    public void RequestCount_Extensions_IsNullWhenOmitted()
    {
        var config = Bind("{}");

        Assert.Null(config.RequestCount.Extensions);
    }

    [Theory]
    [InlineData("[]", new string[0])]
    [InlineData("[\"\"]", new[] { "" })]
    [InlineData("[\"\", \".html\"]", new[] { "", ".html" })]
    public void RequestCount_Extensions_BindsFromJson(string json, string[] expected)
    {
        var config = Bind($"{{\"Extensions\": {json}}}");

        Assert.Equal(expected, config.RequestCount.Extensions);
    }

    private static ClientBanConfig Bind(string requestCountJson)
    {
        var json = $"{{\"RequestCount\": {requestCountJson}}}";
        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)))
            .Build();
        return configuration.Get<ClientBanConfig>()!;
    }
}
