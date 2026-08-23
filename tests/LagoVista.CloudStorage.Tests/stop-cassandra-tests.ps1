$ErrorActionPreference = "Stop"

$composeFile = Join-Path $PSScriptRoot "docker-compose.cassandra.yml"
Write-Host "Stopping local Cassandra integration-test container..."
docker compose -f $composeFile down -v
if ($LASTEXITCODE -ne 0) { throw "Unable to stop the Cassandra integration-test container." }
