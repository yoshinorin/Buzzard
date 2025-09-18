namespace Buzzard.Models;

public class PathConfig
{
    public PathRules Allow { get; set; } = new();
    public PathRules Deny { get; set; } = new();
}

public class PathRules
{
    private List<string> _contains = new();
    private List<string> _startsWith = new();
    private List<string> _endsWith = new();

    public List<string> Contains
    {
        get => _contains;
        set => _contains = value?.Select(p => p.ToLowerInvariant()).ToList() ?? new();
    }

    public List<string> StartsWith
    {
        get => _startsWith;
        set => _startsWith = value?.Select(p => p.ToLowerInvariant()).ToList() ?? new();
    }

    public List<string> EndsWith
    {
        get => _endsWith;
        set => _endsWith = value?.Select(p => p.ToLowerInvariant()).ToList() ?? new();
    }
}
