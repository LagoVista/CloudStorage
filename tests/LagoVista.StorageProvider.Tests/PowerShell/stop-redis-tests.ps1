$ErrorActionPreference = "Stop"

$containerName = "cloudstorage-redis-tests"
$existing = docker ps -a --filter "name=^/$containerName$" --format "{{.Names}}"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if ($existing -eq $containerName) {
    docker rm -f $containerName | Out-Null
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host "Redis integration fixture stopped."
