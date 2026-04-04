FROM mcr.microsoft.com/dotnet/sdk:10.0

# Project targets net8.0 or net9.0 - install their runtimes so dotnet test/run works
COPY --from=mcr.microsoft.com/dotnet/runtime:8.0 /usr/share/dotnet/shared /usr/share/dotnet/shared
COPY --from=mcr.microsoft.com/dotnet/aspnet:8.0 /usr/share/dotnet/shared /usr/share/dotnet/shared
COPY --from=mcr.microsoft.com/dotnet/runtime:9.0 /usr/share/dotnet/shared /usr/share/dotnet/shared
COPY --from=mcr.microsoft.com/dotnet/aspnet:9.0 /usr/share/dotnet/shared /usr/share/dotnet/shared

WORKDIR /app

# Restores (downloads) all NuGet packages on a separate layer for caching
COPY *.sln ./
COPY src/Directory.Build.props src/Directory.Packages.props src/
COPY tests/Directory.Build.props tests/Directory.Packages.props tests/
COPY src/Ziggurat/*.csproj src/Ziggurat/
COPY src/Ziggurat.CapAdapter/*.csproj src/Ziggurat.CapAdapter/
COPY src/Ziggurat.MongoDB/*.csproj src/Ziggurat.MongoDB/
COPY src/Ziggurat.SqlServer/*.csproj src/Ziggurat.SqlServer/
COPY tests/Ziggurat.Tests/*.csproj tests/Ziggurat.Tests/
COPY tests/Ziggurat.MongoDB.Tests/*.csproj tests/Ziggurat.MongoDB.Tests/
COPY tests/Ziggurat.SqlServer.Tests/*.csproj tests/Ziggurat.SqlServer.Tests/
COPY samples/Sample.Cap.SqlServer/*.csproj samples/Sample.Cap.SqlServer/
COPY samples/Sample.Cap.Mongo/*.csproj samples/Sample.Cap.Mongo/
RUN dotnet restore

# Tools used during development
COPY dotnet-tools.json ./
RUN dotnet tool restore

COPY . ./
