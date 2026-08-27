$ErrorActionPreference = "Stop"

$composeFile = Join-Path $PSScriptRoot "docker-compose.storage-lab.yml"
$cosmosReadyUrl = "http://localhost:18080/ready"

Write-Host "Starting local Cosmos emulator lab..."
docker info | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Docker is not available. Start Docker Desktop and try again." }

docker compose -f $composeFile up -d cosmos
if ($LASTEXITCODE -ne 0) { throw "Unable to start the Cosmos emulator container." }

for ($attempt = 1; $attempt -le 60; $attempt++) {
    try {
        $response = Invoke-WebRequest -UseBasicParsing -Uri $cosmosReadyUrl -TimeoutSec 2
        if ($response.StatusCode -eq 200) {
            Write-Host "Cosmos emulator is ready on https://localhost:18081."
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
