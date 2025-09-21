using System.Text.Json;
using ZLogger;
using ZLogger.Formatters;
using ZLogger.Providers;

namespace Buzzard.Extensions;

public static class LoggingExtensions
{
    public static void ConfigureLogging(this WebApplicationBuilder builder)
    {
        // https://github.com/Cysharp/ZLogger
        builder.Logging
            .ClearProviders()
            .AddZLoggerRollingFile(options => ConfigureFileLogging(options))
            .AddZLoggerConsole(options => ConfigureConsoleLogging(options, builder.Environment));
    }

    private static void ConfigureFileLogging(ZLoggerRollingFileOptions options)
    {
        options.FilePathSelector = (timestamp, sequenceNumber) =>
            $"logs/application_{timestamp.ToLocalTime():yyyy-MM-dd}_{sequenceNumber}.log";
        options.RollingInterval = RollingInterval.Day;
        options.RollingSizeKB = 1024;
        options.UseJsonFormatter(ConfigureJsonFormatter);
    }

    private static void ConfigureConsoleLogging(ZLoggerConsoleOptions options, IWebHostEnvironment environment)
    {
        options.UseJsonFormatter(formatter =>
        {
            if (environment.IsDevelopment())
            {
                formatter.JsonSerializerOptions.WriteIndented = true;
            }
            ConfigureJsonFormatter(formatter);
        });
    }

    private static void ConfigureJsonFormatter(SystemTextJsonZLoggerFormatter formatter)
    {
        formatter.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        formatter.JsonPropertyNames = JsonPropertyNames.Default with
        {
            Message = JsonEncodedText.Encode("message"),
            Timestamp = JsonEncodedText.Encode("timestamp"),
            LogLevel = JsonEncodedText.Encode("logLevel"),
            Category = JsonEncodedText.Encode("category"),
            Exception = JsonEncodedText.Encode("exception")
        };
    }
}
