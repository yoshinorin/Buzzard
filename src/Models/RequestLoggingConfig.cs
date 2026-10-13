namespace Buzzard.Models;

public class RequestLoggingConfig
{
    public static readonly string[] DefaultMaskedQueryKeys = ["token", "access_token", "api_key", "apikey", "key", "password", "secret"];
    public static readonly string[] DefaultMaskedHeaders = ["Authorization", "Proxy-Authorization", "Cookie", "X-Api-Key", "X-Auth-Token"];

    public string[]? MaskedQueryKeys { get; set; }
    public string[]? MaskedHeaders { get; set; }
}
