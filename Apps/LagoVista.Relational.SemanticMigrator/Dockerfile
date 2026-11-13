# Build Server V2 owns exact-commit restore, EF model analysis, stable NuGet
# dependency projection, and published .NET output. Docker only packages the verified
# prebuilt output. No NuGet credentials or repository source enter this image.
# Build context: Apps/LagoVista.Relational.SemanticMigrator
FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY publish/ ./
USER 1654
ENTRYPOINT ["dotnet", "/app/LagoVista.Relational.SemanticMigrator.dll"]
