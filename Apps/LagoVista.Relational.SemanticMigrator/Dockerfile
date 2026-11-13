# Build from the CloudStorage repository root:
# docker build -f Apps/LagoVista.Relational.SemanticMigrator/Dockerfile -t nuviot/semantic-schema-migrator:v1 .
# Publishing may require an authenticated NuGet feed configured by the trusted build environment.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source
COPY . .
RUN dotnet publish Apps/LagoVista.Relational.SemanticMigrator/LagoVista.Relational.SemanticMigrator.csproj -c Release -o /publish

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /publish ./
USER 1654
ENTRYPOINT ["dotnet", "/app/LagoVista.Relational.SemanticMigrator.dll"]
