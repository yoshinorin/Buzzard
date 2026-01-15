# Buzzard

![](https://img.shields.io/github/v/release/yoshinorin/Buzzard?sort=semver&style=flat&label=Release) [![CI](https://github.com/yoshinorin/Buzzard/actions/workflows/ci.yml/badge.svg)](https://github.com/yoshinorin/Buzzard/actions/workflows/ci.yml)

A firewall-enabled reverse proxy server built with .NET Kestrel and YARP (Yet Another Reverse Proxy).

## Features

* Reverse proxy powered by YARP
* Path-based request filtering (allow/deny by prefix, suffix, or contains)
* User-Agent based request filtering (allow/deny by prefix, suffix, or contains)
* OpenTelemetry support (logging, metrics, tracing)

## Requirements

- .NET 10.0

## Supported Platforms

| Platform | x64 | x86 | ARM64 |
|----------|-----|-----|-------|
| Windows  | ✓   | ✓   | ✓     |
| Linux    | ✓   |     | ✓     |
| macOS    | ✓   |     | ✓     |

## Getting Started

### Using Binary

Download the release binary for your platform from the [releases page](https://github.com/yoshinorin/Buzzard/releases).

```bash
# Using default configuration
./Buzzard

# Using custom configuration
./Buzzard --configuration my-config.json
```

The server will start and listen on the configured ports.

### Using Docker

Please see [GHCR](https://github.com/yoshinorin/Buzzard/pkgs/container/docker-buzzard) and [examples](docker/examples).

## Configuration

See: [Configuration Documentation](docs/configuration.md).

## Development

See: [Development Documentation](docs/development.md).

## Using Stacks

| Name | Description |
|------|-------------|
| [C#](https://github.com/dotnet/csharplang) | Programming language |
| [Kestrel](https://github.com/dotnet/aspnetcore) | Web server |
| [YARP](https://github.com/dotnet/yarp) | Reverse proxy |
| [ZLogger](https://github.com/Cysharp/ZLogger) | Structured logging |
| [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet) | Observability (logging, metrics, tracing) |
| [xUnit](https://github.com/xunit/xunit) | Testing framework |
| [Moq](https://github.com/devlooped/moq) | Mocking library |

## License

This code is open source software licensed under the [MIT License](LICENSE).
