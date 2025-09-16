using Buzzard.Extensions;
using Buzzard.Middleware;
using Buzzard.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddFirewall(builder.Configuration);

var app = builder.Build();
app.UseMiddleware<FirewallMiddleware>(app.Services.GetRequiredService<FirewallConfig>().BlockedPaths);
app.MapReverseProxy();

app.Run();
