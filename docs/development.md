# Development

## Build and Run

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

## Build Release Binary

```bash
# Build release for current platform
dotnet publish src -c Release --self-contained false -o release

# Build release for specific platform (e.g: Windows x64)
dotnet publish src -c Release -r win-x64 --self-contained true -o release/win-x64
```

## Test

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

