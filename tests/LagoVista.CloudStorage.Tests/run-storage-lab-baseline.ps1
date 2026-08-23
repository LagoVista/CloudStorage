$ErrorActionPreference = "Stop"

& (Join-Path $PSScriptRoot "start-storage-lab.ps1")

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$project = Join-Path $PSScriptRoot "LagoVista.CloudStorage.IntegrationTests.csproj"
$cloudStorageProject = Join-Path $repoRoot "src\LagoVista.CloudStorage\LagoVista.CloudStorage.csproj"

Write-Host ""
Write-Host "[1/3] Building LagoVista.CloudStorage..."
dotnet build $cloudStorageProject
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "[2/3] Running Mongo integration baseline..."
dotnet test $project --filter "TestCategory=Mongo"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "[3/3] Auditing production direct Cosmos consumers..."
& (Join-Path $PSScriptRoot "audit-cosmos-consumers.ps1")

Write-Host ""
Write-Host "Storage lab baseline complete. Mongo is green; use the Cosmos audit as the Card 6B work queue."
