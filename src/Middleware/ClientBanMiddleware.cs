using System.Net;
using Buzzard.Extensions;
using Buzzard.Models;
using Buzzard.Services;
using Microsoft.Extensions.Options;

namespace Buzzard.Middleware;

public class ClientBanMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ClientBanMiddleware> _logger;
    private readonly IClientBanStore _store;
    private readonly ClientBanConfig _config;
    private readonly HashSet<int> _countedStatusCodes;
    private readonly List<IPNetwork> _exemptNetworks;
    private readonly HashSet<string> _countedMethods;
    private readonly HashSet<string> _countedExtensions;
    private readonly string[] _excludedQueryKeys;

    public ClientBanMiddleware(
        RequestDelegate next,
        ILogger<ClientBanMiddleware> logger,
        IClientBanStore store,
        IOptions<ClientBanConfig> config)
    {
        _next = next;
        _logger = logger;
        _store = store;
        _config = config.Value;
        _countedStatusCodes = [.. _config.CountedStatusCodes ?? ClientBanConfig.DefaultCountedStatusCodes];
        _exemptNetworks = _config.ExemptNetworks.Select(ParseNetwork).ToList();
        _countedMethods = new HashSet<string>(
            _config.RequestCount.Methods ?? ClientBanRequestCountConfig.DefaultMethods,
            StringComparer.OrdinalIgnoreCase);
        _countedExtensions = new HashSet<string>(
            (_config.RequestCount.Extensions ?? ClientBanRequestCountConfig.DefaultExtensions).Select(NormalizeExtension),
            StringComparer.OrdinalIgnoreCase);
        _excludedQueryKeys = _config.RequestCount.ExcludedQueryKeys;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ip = context.GetClientIpAddress();
        if (ip.IsIPv4MappedToIPv6)
        {
            ip = ip.MapToIPv4();
        }

        if (_exemptNetworks.Any(network => network.Contains(ip)))
        {
            await _next(context);
            return;
        }

        var clientIp = ip.ToString();
        if (_store.IsBanned(clientIp) && _config.Mode == ClientBanMode.Enforce)
        {
            _logger.LogDebug("Request blocked by ClientBan: {Ip} {Method} {Path}", clientIp, context.Request.Method, context.Request.Path.Value);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        if (IsCountedRequest(context.Request))
        {
            _store.RecordRequest(clientIp, context.Request.Path.Value ?? "");
        }

        await _next(context);

        // Forwarding failures are caused by the destination, not the client.
        if (context.GetForwarderErrorFeature() is not null || !_countedStatusCodes.Contains(context.Response.StatusCode))
        {
            return;
        }

        _store.RecordError(clientIp, context.Request.Path.Value ?? "");
    }

    private bool IsCountedRequest(HttpRequest request)
    {
        if (!_config.RequestCount.Enabled
            || !_countedMethods.Contains(request.Method)
            || _excludedQueryKeys.Any(request.Query.ContainsKey))
        {
            return false;
        }

        if (_countedExtensions.Count == 0)
        {
            return true;
        }

        var path = request.Path.Value ?? "";
        return _countedExtensions.Contains(Path.GetExtension(path[(path.LastIndexOf('/') + 1)..]));
    }

    private static string NormalizeExtension(string extension) =>
        extension.Length == 0 || extension.StartsWith('.') ? extension : $".{extension}";

    private static IPNetwork ParseNetwork(string value)
    {
        if (value.Contains('/'))
        {
            return IPNetwork.Parse(value);
        }

        var address = IPAddress.Parse(value);
        return new IPNetwork(address, address.GetAddressBytes().Length * 8);
    }
}
