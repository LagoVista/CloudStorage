$ErrorActionPreference = "Stop"

& (Join-Path $PSScriptRoot "start-cassandra-tests.ps1")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$project = Join-Path $PSScriptRoot "LagoVista.CloudStorage.IntegrationTests.csproj"
Write-Host "Running Cassandra integration tests against local Docker Cassandra..."
dotnet test $project --filter "TestCategory=Cassandra"
exit $LASTEXITCODE
