using System.Runtime.Serialization;
using Buzzard.Extensions;
using Buzzard.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureLogging();

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddFirewall(builder.Configuration);

var app = builder.Build();
app.UseMiddleware<FirewallMiddleware>();
app.MapReverseProxy();

app.Run();
