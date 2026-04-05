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
COPY src/JuntosSomosMais.Ziggurat/*.csproj src/JuntosSomosMais.Ziggurat/
COPY src/JuntosSomosMais.Ziggurat.CapAdapter/*.csproj src/JuntosSomosMais.Ziggurat.CapAdapter/
COPY src/JuntosSomosMais.Ziggurat.MongoDB/*.csproj src/JuntosSomosMais.Ziggurat.MongoDB/
COPY src/JuntosSomosMais.Ziggurat.SqlServer/*.csproj src/JuntosSomosMais.Ziggurat.SqlServer/
COPY tests/JuntosSomosMais.Ziggurat.Tests/*.csproj tests/JuntosSomosMais.Ziggurat.Tests/
COPY tests/JuntosSomosMais.Ziggurat.MongoDB.Tests/*.csproj tests/JuntosSomosMais.Ziggurat.MongoDB.Tests/
COPY tests/JuntosSomosMais.Ziggurat.SqlServer.Tests/*.csproj tests/JuntosSomosMais.Ziggurat.SqlServer.Tests/
COPY samples/Sample.Cap.SqlServer/*.csproj samples/Sample.Cap.SqlServer/
COPY samples/Sample.Cap.Mongo/*.csproj samples/Sample.Cap.Mongo/
RUN dotnet restore

# Tools used during development
COPY dotnet-tools.json ./
RUN dotnet tool restore

COPY . ./
