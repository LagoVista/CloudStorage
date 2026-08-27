$ErrorActionPreference = "Stop"

$composeFile = Join-Path $PSScriptRoot "docker-compose.s3.yml"
$containerName = "nuviot-cloudstorage-s3-tests"
$healthUri = "http://localhost:19090/minio/health/live"

Write-Host "Starting local S3-compatible integration-test container..."
docker info | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Docker is not available. Start Docker Desktop and try again." }

docker compose -f $composeFile up -d
if ($LASTEXITCODE -ne 0) { throw "Unable to start the S3 integration-test container." }

for ($attempt = 1; $attempt -le 30; $attempt++) {
    try {
        $response = Invoke-WebRequest -Uri $healthUri -UseBasicParsing -TimeoutSec 2
        if ($response.StatusCode -eq 200) {
            Write-Host "S3-compatible integration-test container is healthy on localhost:19090."
            return
        }
    }
    catch {
    }

    $state = (docker inspect --format "{{.State.Status}}" $containerName 2>$null | Out-String).Trim()
    Write-Host "  S3 health pending state=$state attempt=$attempt/30"
    Start-Sleep -Seconds 2
}

throw "S3 integration-test container did not become healthy. Run 'docker logs $containerName' for details."
