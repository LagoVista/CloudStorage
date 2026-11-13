# Build Server V2 owns exact-commit restore, EF model analysis, stable NuGet
# dependency projection, and published .NET output. Docker only packages the verified
# prebuilt output. No NuGet credentials or repository source enter this image.
# Build context: Apps/LagoVista.Relational.SemanticMigrator
# The trusted publish output references Microsoft.AspNetCore.App 9.0;
# dotnet/runtime only includes Microsoft.NETCore.App. Use the ASP.NET
# runtime for the framework-dependent executable (no SDK in the image).
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY publish/ ./
USER 1654
ENTRYPOINT ["dotnet", "/app/LagoVista.Relational.SemanticMigrator.dll"]
