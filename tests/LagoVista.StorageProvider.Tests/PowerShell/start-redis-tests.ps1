$ErrorActionPreference = "Stop"

$containerName = "cloudstorage-redis-tests"
$redisPort = 19079

$existing = docker ps -a --filter "name=^/$containerName$" --format "{{.Names}}"
if ($existing -eq $containerName) {
    docker rm -f $containerName | Out-Null
}

docker run -d --name $containerName -p "${redisPort}:6379" redis:7-alpine redis-server --save "" --appendonly no | Out-Null
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Waiting for Redis on localhost:$redisPort..."
for ($attempt = 1; $attempt -le 30; $attempt++) {
    $pong = docker exec $containerName redis-cli ping 2>$null
    if ($pong -eq "PONG") {
        Write-Host "Redis is ready."
        exit 0
    }

    Start-Sleep -Seconds 1
}

Write-Error "Redis did not become ready within 30 seconds."
exit 1
