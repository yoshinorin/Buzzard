FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /usr/opt/buzzard

COPY ../ ./
RUN dotnet restore src/Buzzard.csproj && \
    dotnet publish src/Buzzard.csproj -c Release --self-contained false -o out && \
    rm -f out/appsettings.json out/appsettings.*.json out/*.log out/*.pdb out/.editorconfig

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0.1
WORKDIR /usr/opt/buzzard

COPY --from=build /usr/opt/buzzard/out .
ENTRYPOINT ["dotnet", "Buzzard.dll"]
