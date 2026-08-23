$ErrorActionPreference = "Stop"

$composeFile = Join-Path $PSScriptRoot "docker-compose.storage-lab.yml"
$mongoContainer = "nuviot-cloudstorage-lab-mongo"
$cosmosReadyUrl = "http://localhost:8080/ready"

Write-Host "Starting local CloudStorage lab (Mongo + Cosmos emulator)..."
docker info | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Docker is not available. Start Docker Desktop and try again." }

docker compose -f $composeFile up -d
if ($LASTEXITCODE -ne 0) { throw "Unable to start the CloudStorage lab containers." }

for ($attempt = 1; $attempt -le 30; $attempt++) {
    $mongoHealth = (docker inspect --format "{{.State.Health.Status}}" $mongoContainer 2>$null | Out-String).Trim()
    if ($mongoHealth -eq "healthy") {
        Write-Host "Mongo is healthy on localhost:27018."
        break
    }

    if ($attempt -eq 30) { throw "Mongo did not become healthy. Run 'docker logs $mongoContainer' for details." }
    Write-Host "  Mongo health=$mongoHealth attempt=$attempt/30"
    Start-Sleep -Seconds 2
}

for ($attempt = 1; $attempt -le 60; $attempt++) {
    try {
        $response = Invoke-WebRequest -UseBasicParsing -Uri $cosmosReadyUrl -TimeoutSec 2
        if ($response.StatusCode -eq 200) {
            Write-Host "Cosmos emulator is ready on https://localhost:8081."
            Write-Host "Cosmos Data Explorer: http://localhost:1234"
            return
        }
    }
    catch {
    }

    Write-Host "  Cosmos readiness attempt=$attempt/60"
    Start-Sleep -Seconds 2
}

throw "Cosmos emulator did not become ready. Run 'docker logs nuviot-cloudstorage-lab-cosmos' for details."
