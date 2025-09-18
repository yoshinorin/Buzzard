namespace Buzzard.Services;

public interface IUserAgentValidator
{
    bool IsUserAgentAllowed(string userAgent);
    bool IsUserAgentDenied(string userAgent);
    bool IsUserAgentBlocked(string userAgent);
}
