$ErrorActionPreference = "Stop"

$composeFile = Join-Path $PSScriptRoot "docker-compose.cassandra.yml"
$containerName = "nuviot-cloudstorage-cassandra-tests"

Write-Host "Starting local Cassandra integration-test container..."
docker info | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Docker is not available. Start Docker Desktop and try again." }

docker compose -f $composeFile up -d
if ($LASTEXITCODE -ne 0) { throw "Unable to start the Cassandra integration-test container." }

$dockerHealthy = $false
for ($attempt = 1; $attempt -le 45; $attempt++) {
    $health = (docker inspect --format "{{.State.Health.Status}}" $containerName 2>$null | Out-String).Trim()
    if ($health -eq "healthy") {
        $dockerHealthy = $true
        Write-Host "Cassandra node is healthy and joined. Waiting for authenticated CQL readiness..."
        break
    }

    Write-Host "  Cassandra health=$health attempt=$attempt/45"
    Start-Sleep -Seconds 3
}

if (-not $dockerHealthy) {
    throw "Cassandra integration-test container did not become healthy. Run 'docker logs $containerName' for details."
}

for ($attempt = 1; $attempt -le 30; $attempt++) {
    docker exec $containerName cqlsh -u cassandra -p cassandra -e "SELECT release_version FROM system.local;" *> $null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Cassandra integration-test container is ready for authenticated CQL on localhost:19042."
        exit 0
    }

    Write-Host "  Cassandra CQL not ready attempt=$attempt/30"
    Start-Sleep -Seconds 2
}

throw "Cassandra joined the cluster but authenticated CQL did not become ready. Run 'docker logs $containerName' for details."
