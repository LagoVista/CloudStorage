$ErrorActionPreference = "Stop"

& (Join-Path $PSScriptRoot "start-cassandra-tests.ps1")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$env:TEST_CASSANDRA_CONTACT_POINTS = "localhost"
$env:TEST_CASSANDRA_PORT = "19042"
$env:TEST_CASSANDRA_USERNAME = "cassandra"
$env:TEST_CASSANDRA_PASSWORD = "cassandra"
$env:TEST_CASSANDRA_KEYSPACE = "nuviot_cloudstorage_tests"
$env:TEST_CASSANDRA_LOCAL_DATACENTER = "datacenter1"

$project = Join-Path $PSScriptRoot "LagoVista.CloudStorage.IntegrationTests.csproj"
Write-Host "Running Cassandra integration tests against local Docker Cassandra..."
dotnet test $project --filter "TestCategory=Cassandra"
exit $LASTEXITCODE
