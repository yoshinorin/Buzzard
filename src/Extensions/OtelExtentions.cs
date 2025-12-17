using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;


namespace Buzzard.Extensions;

public static class OtelExtentions
{
    public static void AddOpentelemetry(this WebApplicationBuilder builder)
    {
        var otelConfig = builder.Configuration.GetSection("OpenTelemetry");
        var otlpEndpoint = otelConfig["OtlpEndpoint"];

        if (!Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var validatedUri))
        {
            Console.WriteLine($"Invalid OpenTelemetry OTLP endpoint URI: {otlpEndpoint}. Skipping OpenTelemetry setup.");
            return;
        }

        var serviceName = otelConfig["ServiceName"] ?? "buzzard";
        var serviceNameSpace = otelConfig["ServiceNameSpace"] ?? "";
        var serviceVersion = otelConfig["ServiceVersion"] ?? "1.0.0";
        var environment = otelConfig["Environment"] ?? "development";
        var headers = otelConfig["Headers"] ?? "";
        var ignoredPaths = otelConfig.GetSection("IgnoredPaths").Get<string[]>() ?? [];

        var instanceId = $"{Environment.MachineName}-{Guid.NewGuid()}";

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeScopes = true;
            options.ParseStateValues = true;
            options.IncludeFormattedMessage = true;
            options.SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName: serviceName, serviceNamespace: serviceNameSpace, serviceVersion: serviceVersion, serviceInstanceId: instanceId)
                .AddAttributes(new[] { new KeyValuePair<string, object>("environment", environment) }));

            options.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = validatedUri;
                otlpOptions.Headers = headers;
            });
        });

        builder.Services.AddOpenTelemetry().ConfigureResource(resource =>
        {
            resource.AddService(
                serviceName: serviceName,
                serviceNamespace: serviceNameSpace,
                serviceVersion: serviceVersion,
                serviceInstanceId: instanceId
            )
            .AddAttributes(new[] { new KeyValuePair<string, object>("environment", environment) });
        })
        .WithMetrics(providerBuilder =>
        {
            providerBuilder.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = validatedUri;
                otlpOptions.Headers = headers;
            });
        })
        .WithTracing(providerBuilder =>
        {
            providerBuilder
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.Filter = (httpContext) =>
                    {
                        var path = httpContext.Request.Path.Value;
                        return !ignoredPaths.Any(ignoredPath => path?.StartsWith(ignoredPath, StringComparison.OrdinalIgnoreCase) ?? false);
                    };
                })
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(otlpOptions =>
                {
                    otlpOptions.Endpoint = validatedUri;
                    otlpOptions.Headers = headers;
                });
        });
    }
}
