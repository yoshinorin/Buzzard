namespace Buzzard.Models;

public class FirewallConfig
{
    public PathConfig Path { get; set; } = new();
}

public class PathConfig
{
    public PathRules Allow { get; set; } = new();
    public PathRules Deny { get; set; } = new();
}

public class PathRules
{
    public List<string> Contains { get; set; } = new();
    public List<string> StartsWith { get; set; } = new();
    public List<string> EndsWith { get; set; } = new();
}
