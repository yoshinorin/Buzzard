using System.Net;

namespace Buzzard.Extensions;

public static class HttpContextExtensions
{
    public static IPAddress GetClientIpAddress(this HttpContext context)
    {
        // Try X-Forwarded-For header first (for proxy scenarios)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var firstIp = forwardedFor.Split(',')[0].Trim();
            if (IPAddress.TryParse(firstIp, out var parsedIp))
                return parsedIp;
        }

        // Fallback to connection remote IP
        return context.Connection.RemoteIpAddress ?? IPAddress.Loopback;
    }
}
