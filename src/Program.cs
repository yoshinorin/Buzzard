using Buzzard.Extensions;
using Buzzard.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureLogging();
builder.AddOpentelemetry();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddDownstreamClientIpHeader();
builder.Services.AddClientIpForwarding(builder.Configuration);
builder.Services.AddFirewall(builder.Configuration);

var app = builder.Build();
app.UseClientIpForwarding();
app.UseMiddleware<FirewallMiddleware>();
app.MapReverseProxy();

app.Run();
