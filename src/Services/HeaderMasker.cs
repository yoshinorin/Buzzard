using Buzzard.Models;

namespace Buzzard.Services;

public class HeaderMasker
{
    private const string MaskedValue = "***";

    private readonly HashSet<string> _maskedHeaders;

    public HeaderMasker(RequestLoggingConfig config)
    {
        _maskedHeaders = new HashSet<string>(
            config.MaskedHeaders ?? RequestLoggingConfig.DefaultMaskedHeaders,
            StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<KeyValuePair<string, string>> Mask(IHeaderDictionary headers)
    {
        foreach (var (name, values) in headers)
        {
            yield return new(name.ToLowerInvariant(), _maskedHeaders.Contains(name) ? MaskedValue : values.ToString());
        }
    }
}
