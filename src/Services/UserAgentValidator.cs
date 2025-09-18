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
        var ua = userAgent.ToLowerInvariant();
        var allow = _userAgentConfig.Allow;
        return allow.Contains.Any(pattern => ua.Contains(pattern)) ||
               allow.StartsWith.Any(pattern => ua.StartsWith(pattern)) ||
               allow.EndsWith.Any(pattern => ua.EndsWith(pattern));
    }

    public bool IsUserAgentDenied(string userAgent)
    {
        var ua = userAgent.ToLowerInvariant();
        var deny = _userAgentConfig.Deny;
        return deny.Contains.Any(pattern => ua.Contains(pattern)) ||
               deny.StartsWith.Any(pattern => ua.StartsWith(pattern)) ||
               deny.EndsWith.Any(pattern => ua.EndsWith(pattern));
    }

    public bool IsUserAgentBlocked(string userAgent)
    {
        if (IsUserAgentAllowed(userAgent))
        {
            return false;
        }

        if (IsUserAgentDenied(userAgent))
        {
            return true;
        }
        return false;
    }
}
