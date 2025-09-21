# Buzzard

[![CI](https://github.com/yoshinorin/Buzzard/actions/workflows/ci.yml/badge.svg)](https://github.com/yoshinorin/Buzzard/actions/workflows/ci.yml)

A firewall-enabled reverse proxy server built with .NET Kestrel and YARP (Yet Another Reverse Proxy).

## Quick Start

### Prerequisites

- .NET 10.0

### Build and Run

```bash
# Build the project
dotnet build

# Run the server
cd src
dotnet run
```

The server will start on `http://localhost:5134`, `5135` and `5136`  by default.

### Test

```bash
# simple
dotnet test

# detailed
dotnet test --logger "console;verbosity=detailed"
```

### Configuration

Edit `src/appsettings.json` to configure:

#### Reverse Proxy Settings

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

#### Firewall Settings

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

### Configuration Changes

- Configuration changes require server restart
- No rebuild needed for `appsettings.json` changes
- Use `dotnet watch run` for automatic restart on file changes

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

### Port Configuration

#### Development:

Port configuration is handled by `Properties/launchSettings.json` (Visual Studio/dotnet run only):

#### Production:

`launchSettings.json` is **not used in production**. Configure ports using:

##### 1. **Environment variables** (recommended):

```bash
export ASPNETCORE_URLS="https://+:443;http://+:80"
dotnet Buzzard.dll
```

##### 2. **Command line arguments**:

```bash
dotnet Buzzard.dll --urls "https://+:443;http://+:80"
```

##### 3. **appsettings.Production.json**:

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
