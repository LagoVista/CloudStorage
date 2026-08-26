$ErrorActionPreference = "Stop"

$composeFile = Join-Path $PSScriptRoot "docker-compose.mongo.yml"
Write-Host "Stopping local Mongo integration-test container..."
docker compose -f $composeFile down -v
if ($LASTEXITCODE -ne 0) { throw "Unable to stop the Mongo integration-test container." }
