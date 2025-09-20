using Buzzard.Models;
using Buzzard.Services;

namespace Buzzard.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFirewall(this IServiceCollection services, IConfiguration configuration)
    {
        var firewallConfig = configuration.GetSection("Firewall").Get<FirewallConfig>() ?? new FirewallConfig();

        services.AddSingleton(firewallConfig);
        services.AddSingleton(firewallConfig.Path);
        services.AddSingleton(firewallConfig.UserAgent);
        services.AddSingleton<IPathValidator, PathValidator>();
        services.AddSingleton<IUserAgentValidator, UserAgentValidator>();

        return services;
    }
}
