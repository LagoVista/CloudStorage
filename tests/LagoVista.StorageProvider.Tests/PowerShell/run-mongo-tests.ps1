$ErrorActionPreference = "Stop"

& (Join-Path $PSScriptRoot "start-mongo-tests.ps1")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$project = Join-Path $PSScriptRoot "LagoVista.CloudStorage.IntegrationTests.csproj"
Write-Host "Running Mongo integration tests against local Docker Mongo..."
dotnet test $project --filter "TestCategory=Mongo"
exit $LASTEXITCODE
