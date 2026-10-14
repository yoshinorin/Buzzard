using Buzzard.Middleware;
using Buzzard.Models;
using Buzzard.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Buzzard.Extensions;

public static class ClientBanExtensions
{
    public static IServiceCollection AddClientBan(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ClientBanConfig>()
            .Bind(configuration.GetSection("Firewall:ClientBan"))
            .Validate(
                config => config.Threshold > 0
                    && config.WindowSeconds > 0
                    && config.BanSeconds > 0
                    && config.MaxTrackedClients > 0
                    && config.GlobalSafetyThreshold > 0
                    && config.GlobalSafetyWindowSeconds > 0,
                "Firewall:ClientBan numeric settings must be greater than 0.");
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IClientBanStore, ClientBanStore>();

        return services;
    }

    public static WebApplication UseClientBan(this WebApplication app)
    {
        // Resolve options at startup so that invalid settings fail fast.
        var config = app.Services.GetRequiredService<IOptions<ClientBanConfig>>().Value;
        if (config.Mode == ClientBanMode.Disabled)
        {
            return app;
        }

        app.UseMiddleware<ClientBanMiddleware>();
        return app;
    }
}
