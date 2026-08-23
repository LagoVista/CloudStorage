$ErrorActionPreference = "Stop"

$composeFile = Join-Path $PSScriptRoot "docker-compose.mongo.yml"
$containerName = "nuviot-cloudstorage-mongo-tests"

Write-Host "Starting local Mongo integration-test container..."
docker info | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Docker is not available. Start Docker Desktop and try again." }

docker compose -f $composeFile up -d
if ($LASTEXITCODE -ne 0) { throw "Unable to start the Mongo integration-test container." }

for ($attempt = 1; $attempt -le 30; $attempt++) {
    $health = (docker inspect --format "{{.State.Health.Status}}" $containerName 2>$null | Out-String).Trim()
    if ($health -eq "healthy") {
        Write-Host "Mongo integration-test container is healthy on localhost:27018."
        exit 0
    }

    Write-Host "  Mongo health=$health attempt=$attempt/30"
    Start-Sleep -Seconds 2
}

throw "Mongo integration-test container did not become healthy. Run 'docker logs $containerName' for details."
