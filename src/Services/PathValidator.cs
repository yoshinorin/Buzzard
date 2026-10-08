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
        var p = path.ToLowerInvariant();
        var allow = _pathConfig.Allow;
        return allow.Contains.Any(pattern => p.Contains(pattern)) ||
               allow.StartsWith.Any(pattern => p.StartsWith(pattern)) ||
               allow.EndsWith.Any(pattern => p.EndsWith(pattern));
    }

    public bool IsPathDenied(string path)
    {
        var p = path.ToLowerInvariant();
        var deny = _pathConfig.Deny;
        return deny.Contains.Any(pattern => p.Contains(pattern)) ||
               deny.StartsWith.Any(pattern => p.StartsWith(pattern)) ||
               deny.EndsWith.Any(pattern => p.EndsWith(pattern));
    }

    public bool IsPathBlocked(string path)
    {
        // Backends may interpret these characters differently, so allow rules must not override deny rules for such paths.
        if (!IsAmbiguous(path) && IsPathAllowed(path))
        {
            return false;
        }

        if (IsPathDenied(path))
        {
            return true;
        }
        return false;
    }

    private static bool IsAmbiguous(string path) =>
        path.IndexOfAny([';', '%', '\\']) >= 0;
}
