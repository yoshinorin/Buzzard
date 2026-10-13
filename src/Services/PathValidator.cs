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
        return FindMatch(path.ToLowerInvariant(), _pathConfig.Allow) is not null;
    }

    public bool IsPathDenied(string path)
    {
        return FindDenyMatch(path) is not null;
    }

    public bool IsPathBlocked(string path)
    {
        return FindBlockingRule(path) is not null;
    }

    public string? FindBlockingRule(string path)
    {
        // Backends may interpret these characters differently, so allow rules must not override deny rules for such paths.
        if (!IsAmbiguous(path) && IsPathAllowed(path))
        {
            return null;
        }

        var match = FindDenyMatch(path);
        return match is null ? null : $"Path.Deny.{match}";
    }

    private string? FindDenyMatch(string path)
    {
        var p = path.ToLowerInvariant();
        // Also match the normalized form because backends may decode or normalize the forwarded path.
        return FindMatch(p, _pathConfig.Deny) ?? FindMatch(Normalize(p), _pathConfig.Deny);
    }

    private static bool IsAmbiguous(string path) =>
        path.IndexOfAny([';', '%', '\\']) >= 0;

    private static string? FindMatch(string path, PathRules rules)
    {
        var contains = rules.Contains.FirstOrDefault(pattern => path.Contains(pattern));
        if (contains is not null)
        {
            return $"Contains:{contains}";
        }

        var startsWith = rules.StartsWith.FirstOrDefault(pattern => path.StartsWith(pattern));
        if (startsWith is not null)
        {
            return $"StartsWith:{startsWith}";
        }

        var endsWith = rules.EndsWith.FirstOrDefault(pattern => path.EndsWith(pattern));
        return endsWith is null ? null : $"EndsWith:{endsWith}";
    }

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
