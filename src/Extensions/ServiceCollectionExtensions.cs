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

        var requestLoggingConfig = configuration.GetSection("RequestLogging").Get<RequestLoggingConfig>() ?? new RequestLoggingConfig();
        services.AddSingleton(requestLoggingConfig);
        services.AddSingleton<QueryStringMasker>();
        services.AddSingleton<HeaderMasker>();
        services.AddSingleton<RequestLogScope>();

        return services;
    }
}
