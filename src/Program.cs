using Buzzard.Extensions;
using Buzzard.Middleware;
using Buzzard.Models;
using ZLogger;
using ZLogger.Providers;

var builder = WebApplication.CreateBuilder(args);

// https://github.com/Cysharp/ZLogger
builder.Logging
    .ClearProviders()
    .AddZLoggerFile("logs/application.log") // Is this settings really needed?
    .AddZLoggerRollingFile(options =>
    {
        options.FilePathSelector = (timestamp, sequenceNumber) => $"logs/application_{timestamp.ToLocalTime():yyyy-MM-dd}_{sequenceNumber}.log";
        options.RollingInterval = RollingInterval.Day;
        options.RollingSizeKB = 1024;
    })
    .AddZLoggerConsole(options =>
    {
        options.UseJsonFormatter(formatter =>
        {
            if (builder.Environment.IsDevelopment())
            {
                formatter.JsonSerializerOptions.WriteIndented = true;
            }
        });
    });


builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddFirewall(builder.Configuration);

var app = builder.Build();
app.UseMiddleware<FirewallMiddleware>(app.Services.GetRequiredService<FirewallConfig>());
app.MapReverseProxy();

app.Run();
