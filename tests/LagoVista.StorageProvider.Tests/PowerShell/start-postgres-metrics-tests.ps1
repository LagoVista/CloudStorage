$ErrorActionPreference = "Stop"

$containerName = "cloudstorage-postgres-metrics-tests"
$postgresPort = 19043
$image = "timescale/timescaledb:latest-pg17"

$existing = docker ps -a --filter "name=^/$containerName$" --format "{{.Names}}"
if ($existing -eq $containerName) {
    docker rm -f $containerName | Out-Null
}

docker run -d --name $containerName -p "${postgresPort}:5432" -e POSTGRES_USER=postgres -e POSTGRES_DB=postgres -e POSTGRES_HOST_AUTH_METHOD=trust $image | Out-Null
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Waiting for PostgreSQL + TimescaleDB on localhost:$postgresPort..."
for ($attempt = 1; $attempt -le 60; $attempt++) {
    docker exec $containerName pg_isready -U postgres -d postgres 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) {
        $timescaleVersion = docker exec $containerName psql -U postgres -d postgres -tAc "SELECT default_version FROM pg_available_extensions WHERE name = 'timescaledb';" 2>$null
        if (-not [String]::IsNullOrWhiteSpace($timescaleVersion)) {
            Write-Host "PostgreSQL is ready with TimescaleDB $($timescaleVersion.Trim())."
            exit 0
        }
    }

    Start-Sleep -Seconds 1
}

Write-Error "PostgreSQL + TimescaleDB did not become ready within 60 seconds."
exit 1
