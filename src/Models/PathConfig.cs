namespace Buzzard.Models;

public class PathConfig
{
    public PathRules Allow { get; set; } = new();
    public PathRules Deny { get; set; } = new();
}

public class PathRules
{
    private HashSet<string> _contains = new();
    private HashSet<string> _startsWith = new();
    private HashSet<string> _endsWith = new();

    public HashSet<string> Contains
    {
        get => _contains;
        set => _contains = value?.Select(p => p.ToLowerInvariant()).ToHashSet() ?? new();
    }

    public HashSet<string> StartsWith
    {
        get => _startsWith;
        set => _startsWith = value?.Select(p => p.ToLowerInvariant()).ToHashSet() ?? new();
    }

    public HashSet<string> EndsWith
    {
        get => _endsWith;
        set => _endsWith = value?.Select(p => p.ToLowerInvariant()).ToHashSet() ?? new();
    }
}
