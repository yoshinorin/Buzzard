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

see [Configuration Documentation](docs/configuration.md).

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
