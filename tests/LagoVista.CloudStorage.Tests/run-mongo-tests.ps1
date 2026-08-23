$ErrorActionPreference = "Stop"

& (Join-Path $PSScriptRoot "start-mongo-tests.ps1")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$env:TEST_MONGO_HOSTS = "localhost"
$env:TEST_MONGO_PORT = "27018"
$env:TEST_MONGO_USERNAME = "nuviot-test"
$env:TEST_MONGO_PASSWORD = "nuviot-test-password"
$env:TEST_MONGO_AUTHENTICATION_DATABASE = "admin"
$env:TEST_MONGO_REPLICA_SET = ""
$env:TEST_MONGO_USE_TLS = "false"

$project = Join-Path $PSScriptRoot "LagoVista.CloudStorage.IntegrationTests.csproj"
Write-Host "Running Mongo integration tests against local Docker Mongo..."
dotnet test $project --filter "TestCategory=Mongo"
exit $LASTEXITCODE
