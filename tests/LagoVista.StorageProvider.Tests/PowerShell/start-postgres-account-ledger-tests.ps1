$ErrorActionPreference = "Stop"

$containerName = "cloudstorage-postgres-account-ledger-tests"
$postgresPort = 19044
$image = "postgres:17-alpine"

$existing = docker ps -a --filter "name=^/$containerName$" --format "{{.Names}}"
if ($existing -eq $containerName) {
    docker rm -f $containerName | Out-Null
}

docker run -d --name $containerName -p "${postgresPort}:5432" -e POSTGRES_USER=postgres -e POSTGRES_DB=postgres -e POSTGRES_HOST_AUTH_METHOD=trust $image | Out-Null
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Waiting for PostgreSQL on localhost:$postgresPort..."
for ($attempt = 1; $attempt -le 60; $attempt++) {
    docker exec $containerName pg_isready -U postgres -d postgres 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "PostgreSQL is ready."
        exit 0
    }

    Start-Sleep -Seconds 1
}

Write-Error "PostgreSQL did not become ready within 60 seconds."
exit 1
