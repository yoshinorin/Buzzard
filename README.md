# Buzzard

![](https://img.shields.io/github/v/release/yoshinorin/Buzzard?sort=semver&style=flat&label=Release) [![CI](https://github.com/yoshinorin/Buzzard/actions/workflows/ci.yml/badge.svg)](https://github.com/yoshinorin/Buzzard/actions/workflows/ci.yml)

A firewall-enabled reverse proxy server built with .NET Kestrel and YARP (Yet Another Reverse Proxy).

## Requirements

- .NET 10.0

## Supported Platforms

| Platform | x64 | x86 | ARM64 |
|----------|-----|-----|-------|
| Windows  | ✓   | ✓   | ✓     |
| Linux    | ✓   |     | ✓     |
| macOS    | ✓   |     | ✓     |

## Getting Started

Exec release binary from [releases page](https://github.com/yoshinorin/Buzzard/releases).

```bash
# Using default configuration
./Buzzard

# Using custom configuration
./Buzzard --configuration my-config.json
```

The server will start and listen on the configured ports.

## Configuration

See: [Configuration Documentation](docs/configuration.md).

## Development

### Build and Run

```bash
# Build the project
dotnet build

# Run the server
cd src
dotnet run
dotnet watch run

# Or project root
dotnet run --project src
dotnet watch run --project src
```

The server will start on `http://localhost:5134`, `5135` and `5136` by default.

### Build Release Binary

```bash
# Build release for current platform
dotnet publish src -c Release --self-contained false -o release

# Build release for specific platform (e.g: Windows x64)
dotnet publish src -c Release -r win-x64 --self-contained true -o release/win-x64
```


### Test

```bash
# simple
dotnet test

# detailed
dotnet test --logger "console;verbosity=detailed"
```

## Docker 

### Build Image

```
# Exec on project root directory
$ docker build --progress=plain -f docker/Dockerfile .
```

### Release build example with docker

```
$ cd docker/examples
$ docker compose up
```

## License

This code is open source software licensed under the [MIT License](LICENSE).
