using Buzzard.Models;

namespace Buzzard.Services;

public class UserAgentValidator : IUserAgentValidator
{
    private readonly UserAgentConfig _userAgentConfig;

    public UserAgentValidator(FirewallConfig firewallConfig)
    {
        _userAgentConfig = firewallConfig.UserAgent;
    }

    public bool IsUserAgentAllowed(string userAgent)
    {
        return FindMatch(userAgent.ToLowerInvariant(), _userAgentConfig.Allow) is not null;
    }

    public bool IsUserAgentDenied(string userAgent)
    {
        return FindMatch(userAgent.ToLowerInvariant(), _userAgentConfig.Deny) is not null;
    }

    public bool IsUserAgentBlocked(string userAgent)
    {
        return FindBlockingRule(userAgent) is not null;
    }

    public string? FindBlockingRule(string userAgent)
    {
        if (IsUserAgentAllowed(userAgent))
        {
            return null;
        }

        var match = FindMatch(userAgent.ToLowerInvariant(), _userAgentConfig.Deny);
        return match is null ? null : $"UserAgent.Deny.{match}";
    }

    public bool IsPathExempt(string path)
    {
        return _userAgentConfig.ExemptPaths.Contains(path);
    }

    private static string? FindMatch(string userAgent, UserAgentRules rules)
    {
        var contains = rules.Contains.FirstOrDefault(pattern => userAgent.Contains(pattern));
        if (contains is not null)
        {
            return $"Contains:{contains}";
        }

        var startsWith = rules.StartsWith.FirstOrDefault(pattern => userAgent.StartsWith(pattern));
        if (startsWith is not null)
        {
            return $"StartsWith:{startsWith}";
        }

        var endsWith = rules.EndsWith.FirstOrDefault(pattern => userAgent.EndsWith(pattern));
        return endsWith is null ? null : $"EndsWith:{endsWith}";
    }
}
