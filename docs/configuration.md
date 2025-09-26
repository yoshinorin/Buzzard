# Configuration

This document describes all configuration options available in Buzzard.

## Configuration Files

Buzzard uses ASP.NET Core's configuration system with the following precedence:

1. `appsettings.json` (base configuration)
2. `appsettings.{Environment}.json` (environment-specific, overwrites base)
3. Environment variables
4. Command line arguments

## Reverse Proxy Settings

Configure YARP reverse proxy in `appsettings.json`:

```json
"ReverseProxy": {
  "Routes": {
    "route1": {
      "ClusterId": "cluster1",
      "Match": { "Path": "{**catch-all}" }
    }
  },
  "Clusters": {
    "cluster1": {
      "Destinations": {
        "destination1": {
          "Address": "https://example.com/"
        }
      }
    }
  }
}
```

## Firewall Settings

Configure firewall rules to allow/deny requests:

```json
"Firewall": {
  "Path": {
    "Allow": {
      "Contains": ["/health", "/status"],
      "StartsWith": ["/api/public"],
      "EndsWith": [".css", ".js", ".png", ".ico"]
    },
    "Deny": {
      "Contains": ["/admin", "/config", "/.env"],
      "StartsWith": ["/private/", "/internal/"],
      "EndsWith": [".bak", ".tmp", ".log"]
    }
  },
  "UserAgent": {
    "Deny": {
      "Contains": [
        "bad"
      ],
      "StartsWith": [
        "evil/"
      ],
      "EndsWith": [
        "bot"
      ]
    }
  }
}
```

## OpenTelemetry Settings

Buzzard supports OpenTelemetry for observability (tracing, metrics, and logs). Configure it in `appsettings.json`:

```json
"OpenTelemetry": {
  "ServiceName": "Buzzard",
  "ServiceNameSpace": "Buzzard",
  "ServiceVersion": "1.0.0",
  "Environment": "development",
  "OtlpEndpoint": "http://localhost:4317",
  "Headers": "Authorization=Basic {base64-encoded-credentials}"  // If you need
}
```

Configuration options:

- `ServiceName`: Name of your service (default: "buzzard")
- `ServiceNameSpace`: Service namespace for grouping (default: "")
- `ServiceVersion`: Version of your service (default: "1.0.0")
- `Environment`: Environment identifier (default: "development")
- `OtlpEndpoint`: OTLP collector endpoint URL (required - if not set, OpenTelemetry is disabled)
- `Headers`: Additional headers for authentication (default: "")

**Note**: If `OtlpEndpoint` is not configured, OpenTelemetry setup will be skipped and a message will be logged.

## Environment-Specific Configuration

### Development vs Production Settings

ASP.NET Core automatically loads environment-specific configuration files:

1. `appsettings.json` (base configuration)
2. `appsettings.{Environment}.json` (environment-specific, overwrites base)
3. Environment variables
4. Command line arguments

### Running in Different Environments

#### Development (default):

```bash
dotnet run
# Loads: appsettings.json + appsettings.Development.json
```

#### Production:

```bash
ASPNETCORE_ENVIRONMENT=Production dotnet run
# Loads: appsettings.json + appsettings.Production.json
```

## Port Configuration

### Development

Port configuration is handled by `Properties/launchSettings.json` (Visual Studio/dotnet run only).

### Production

`launchSettings.json` is **not used in production**. Configure ports using:

#### 1. Environment variables (recommended):

```bash
export ASPNETCORE_URLS="https://+:443;http://+:80"
dotnet Buzzard.dll
```

#### 2. Command line arguments:

```bash
dotnet Buzzard.dll --urls "https://+:443;http://+:80"
```

#### 3. appsettings.Production.json:

```json
{
    "Kestrel": {
    "Endpoints": {
        "Http": { "Url": "http://+:80" },
        "Https": { "Url": "https://+:443" }
    }
    }
}
```

## Configuration Changes

- Configuration changes in `appsettings.json` require server restart
- No rebuild needed for configuration changes
- Configuration files are not hot-reloaded in ASP.NET Core by default
