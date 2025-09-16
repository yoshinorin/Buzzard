using Buzzard.Models;

namespace Buzzard.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFirewall(this IServiceCollection services, IConfiguration configuration)
    {
        var firewallConfig = configuration.GetSection("Firewall").Get<FirewallConfig>() ?? new FirewallConfig();

        services.AddSingleton(firewallConfig);

        return services;
    }
}
