$ErrorActionPreference = "Stop"

$composeFile = Join-Path $PSScriptRoot "docker-compose.storage-lab.yml"
Write-Host "Stopping local CloudStorage lab..."
docker compose -f $composeFile down -v
if ($LASTEXITCODE -ne 0) { throw "Unable to stop the CloudStorage lab containers." }
