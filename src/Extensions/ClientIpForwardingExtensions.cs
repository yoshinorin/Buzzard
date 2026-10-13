using System.Net;
using Buzzard.Models;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Transforms;

namespace Buzzard.Extensions;

public static class ClientIpForwardingExtensions
{
    private static readonly object OriginalClientIpKey = new();

    public static IServiceCollection AddClientIpForwarding(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ForwardedHeadersConfig>().Bind(configuration.GetSection("ForwardedHeaders"));
        services.AddOptions<ForwardedHeadersOptions>().Configure<IOptions<ForwardedHeadersConfig>>((options, configOptions) =>
        {
            var config = configOptions.Value;
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
            if (!string.IsNullOrEmpty(config.ClientIpHeader))
            {
                options.ForwardedForHeaderName = config.ClientIpHeader;
            }
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();
            foreach (var proxy in config.KnownProxies)
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            }
            foreach (var network in config.KnownNetworks)
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }
        });

        return services;
    }

    public static IReverseProxyBuilder AddDownstreamClientIpHeader(this IReverseProxyBuilder builder)
    {
        return builder.AddTransforms(context =>
        {
            var headerName = context.Services.GetRequiredService<IOptions<ForwardedHeadersConfig>>().Value.DownstreamClientIpHeader;
            if (string.IsNullOrEmpty(headerName))
            {
                return;
            }

            context.AddRequestTransform(transformContext =>
            {
                // Always replace the incoming value to prevent spoofing.
                transformContext.ProxyRequest.Headers.Remove(headerName);
                var ip = transformContext.HttpContext.Connection.RemoteIpAddress;
                if (ip is not null)
                {
                    transformContext.ProxyRequest.Headers.TryAddWithoutValidation(headerName, ip.ToString());
                }
                return ValueTask.CompletedTask;
            });
        });
    }

    public static WebApplication UseClientIpForwarding(this WebApplication app)
    {
        // Resolve options at startup so that invalid KnownProxies / KnownNetworks fail fast.
        _ = app.Services.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        var config = app.Services.GetRequiredService<IOptions<ForwardedHeadersConfig>>().Value;
        if (string.IsNullOrEmpty(config.ClientIpHeader))
        {
            return app;
        }

        // ForwardedHeadersMiddleware skips the trusted proxy check when both lists are empty.
        if (config.KnownProxies.Length == 0 && config.KnownNetworks.Length == 0)
        {
            app.Logger.LogWarning("ForwardedHeaders:ClientIpHeader is accepted from any client because neither KnownProxies nor KnownNetworks is configured.");
        }

        // ForwardedHeadersMiddleware removes the consumed header, so restore it to forward the original value to the destination.
        var headerName = config.ClientIpHeader;
        app.Use((context, next) =>
        {
            context.Items[OriginalClientIpKey] = context.Request.Headers[headerName];
            return next(context);
        });
        app.UseForwardedHeaders();
        app.Use((context, next) =>
        {
            if (context.Items[OriginalClientIpKey] is StringValues original && !StringValues.IsNullOrEmpty(original))
            {
                context.Request.Headers[headerName] = original;
            }
            return next(context);
        });
        return app;
    }
}
