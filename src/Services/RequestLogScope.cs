using Buzzard.Models;

namespace Buzzard.Services;

public class RequestLogScope
{
    private const int MaxValueLength = 1024;

    private readonly QueryStringMasker _queryStringMasker;
    private readonly HeaderMasker _headerMasker;

    public RequestLogScope(QueryStringMasker queryStringMasker, HeaderMasker headerMasker)
    {
        _queryStringMasker = queryStringMasker;
        _headerMasker = headerMasker;
    }

    public IDisposable? Begin(ILogger logger, HttpContext context, Request request, params (string Key, object? Value)[] fields)
    {
        var state = new Dictionary<string, object?>
        {
            ["Host"] = Truncate(request.Host),
            ["QueryString"] = Truncate(_queryStringMasker.Mask(request.QueryString)),
        };
        foreach (var (name, value) in _headerMasker.Mask(context.Request.Headers))
        {
            state[$"Header.{name}"] = Truncate(value);
        }
        foreach (var (key, value) in fields)
        {
            state[key] = value;
        }
        return logger.BeginScope(state);
    }

    public static string Truncate(string value) =>
        value.Length <= MaxValueLength ? value : value[..MaxValueLength];
}
