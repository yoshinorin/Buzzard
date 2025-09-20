using System.Runtime.Serialization;
using System.Text.Json;
using Buzzard.Extensions;
using Buzzard.Middleware;
using Buzzard.Models;
using ZLogger;
using ZLogger.Formatters;
using ZLogger.Providers;

var builder = WebApplication.CreateBuilder(args);

// https://github.com/Cysharp/ZLogger
builder.Logging
    .ClearProviders()
    .AddZLoggerRollingFile(options =>
    {
        options.FilePathSelector = (timestamp, sequenceNumber) => $"logs/application_{timestamp.ToLocalTime():yyyy-MM-dd}_{sequenceNumber}.log";
        options.RollingInterval = RollingInterval.Day;
        options.RollingSizeKB = 1024;
        options.UseJsonFormatter(formatter =>
        {
            formatter.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            formatter.JsonPropertyNames = JsonPropertyNames.Default with
            {
                Message = JsonEncodedText.Encode("message"),
                Timestamp = JsonEncodedText.Encode("timestamp"),
                LogLevel = JsonEncodedText.Encode("logLevel"),
                Category = JsonEncodedText.Encode("category"),
                Exception = JsonEncodedText.Encode("exception")
            };
        });
    })
    .AddZLoggerConsole(options =>
    {
        options.UseJsonFormatter(formatter =>
        {
            if (builder.Environment.IsDevelopment())
            {
                formatter.JsonSerializerOptions.WriteIndented = true;
            }
            formatter.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            formatter.JsonPropertyNames = JsonPropertyNames.Default with
            {
                Message = JsonEncodedText.Encode("message"),
                Timestamp = JsonEncodedText.Encode("timestamp"),
                LogLevel = JsonEncodedText.Encode("logLevel"),
                Category = JsonEncodedText.Encode("category"),
                Exception = JsonEncodedText.Encode("exception")
            };
        });
    });


builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddFirewall(builder.Configuration);

var app = builder.Build();
app.UseMiddleware<FirewallMiddleware>();
app.MapReverseProxy();

app.Run();
