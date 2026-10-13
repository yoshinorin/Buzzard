namespace Buzzard.Models;

public class ForwardedHeadersConfig
{
    public string? ClientIpHeader { get; set; }
    public string[] KnownProxies { get; set; } = [];
    public string[] KnownNetworks { get; set; } = [];
    public string? DownstreamClientIpHeader { get; set; }
}
