using Buzzard.Models;

namespace Buzzard.Services;

public class PathValidator : IPathValidator
{
    private readonly PathConfig _pathConfig;

    public PathValidator(PathConfig pathConfig)
    {
        _pathConfig = pathConfig;
    }

    private const int MaxDecodeIterations = 3;

    public bool IsPathAllowed(string path)
    {
        return Matches(path.ToLowerInvariant(), _pathConfig.Allow);
    }

    public bool IsPathDenied(string path)
    {
        var p = path.ToLowerInvariant();
        // Also match the normalized form because backends may decode or normalize the forwarded path.
        return Matches(p, _pathConfig.Deny) || Matches(Normalize(p), _pathConfig.Deny);
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

    private static bool Matches(string path, PathRules rules) =>
        rules.Contains.Any(pattern => path.Contains(pattern)) ||
        rules.StartsWith.Any(pattern => path.StartsWith(pattern)) ||
        rules.EndsWith.Any(pattern => path.EndsWith(pattern));

    private static string Normalize(string path)
    {
        var decoded = path;
        for (var i = 0; i < MaxDecodeIterations; i++)
        {
            var next = Uri.UnescapeDataString(decoded);
            if (next == decoded)
            {
                break;
            }
            decoded = next;
        }

        var rawSegments = decoded.Replace('\\', '/').Split('/');
        var segments = new List<string>();
        var trailingSlash = false;
        foreach (var rawSegment in rawSegments)
        {
            var parameterIndex = rawSegment.IndexOf(';');
            var segment = parameterIndex >= 0 ? rawSegment[..parameterIndex] : rawSegment;

            trailingSlash = segment is "" or "." or "..";
            switch (segment)
            {
                case "":
                case ".":
                    break;
                case "..":
                    if (segments.Count > 0)
                    {
                        segments.RemoveAt(segments.Count - 1);
                    }
                    break;
                default:
                    segments.Add(segment);
                    break;
            }
        }

        var normalized = "/" + string.Join('/', segments);
        return trailingSlash && segments.Count > 0 ? normalized + "/" : normalized;
    }
}
