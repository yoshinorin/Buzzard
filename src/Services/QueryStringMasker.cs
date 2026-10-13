using Buzzard.Models;

namespace Buzzard.Services;

public class QueryStringMasker
{
    private const string MaskedValue = "***";

    private readonly HashSet<string> _maskedKeys;

    public QueryStringMasker(RequestLoggingConfig config)
    {
        _maskedKeys = new HashSet<string>(
            config.MaskedQueryKeys ?? RequestLoggingConfig.DefaultMaskedQueryKeys,
            StringComparer.OrdinalIgnoreCase);
    }

    public string Mask(string queryString)
    {
        if (_maskedKeys.Count == 0 || queryString.Length <= 1)
        {
            return queryString;
        }

        var hasPrefix = queryString[0] == '?';
        var parameters = (hasPrefix ? queryString[1..] : queryString).Split('&');
        for (var i = 0; i < parameters.Length; i++)
        {
            var separatorIndex = parameters[i].IndexOf('=');
            if (separatorIndex < 0)
            {
                continue;
            }

            var key = parameters[i][..separatorIndex];
            if (_maskedKeys.Contains(Uri.UnescapeDataString(key.Replace('+', ' '))))
            {
                parameters[i] = $"{key}={MaskedValue}";
            }
        }

        var masked = string.Join('&', parameters);
        return hasPrefix ? $"?{masked}" : masked;
    }
}
