using Buzzard.Models;

namespace Buzzard.Services;

public class PathValidator : IPathValidator
{
    private readonly PathConfig _pathConfig;

    public PathValidator(PathConfig pathConfig)
    {
        _pathConfig = pathConfig;
    }

    public bool IsPathAllowed(string path)
    {
        var allow = _pathConfig.Allow;
        return allow.Contains.Any(pattern => path.Contains(pattern, StringComparison.OrdinalIgnoreCase)) ||
               allow.StartsWith.Any(pattern => path.StartsWith(pattern, StringComparison.OrdinalIgnoreCase)) ||
               allow.EndsWith.Any(pattern => path.EndsWith(pattern, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsPathDenied(string path)
    {
        var deny = _pathConfig.Deny;
        return deny.Contains.Any(pattern => path.Contains(pattern, StringComparison.OrdinalIgnoreCase)) ||
               deny.StartsWith.Any(pattern => path.StartsWith(pattern, StringComparison.OrdinalIgnoreCase)) ||
               deny.EndsWith.Any(pattern => path.EndsWith(pattern, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsPathBlocked(string path)
    {
        if (IsPathAllowed(path))
        {
            return false;
        }

        if (IsPathDenied(path))
        {
            return true;
        }
        return false;
    }
}
