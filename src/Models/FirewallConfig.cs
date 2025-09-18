namespace Buzzard.Models;

public class FirewallConfig
{
    public PathConfig Path { get; set; } = new();
    public UserAgentConfig UserAgent { get; set; } = new();
}
