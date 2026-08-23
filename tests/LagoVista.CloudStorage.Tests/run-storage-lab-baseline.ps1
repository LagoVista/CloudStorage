$ErrorActionPreference = "Stop"

& (Join-Path $PSScriptRoot "start-storage-lab.ps1")

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$project = Join-Path $PSScriptRoot "LagoVista.CloudStorage.IntegrationTests.csproj"
$cloudStorageProject = Join-Path $repoRoot "src\LagoVista.CloudStorage\LagoVista.CloudStorage.csproj"

Write-Host ""
Write-Host "[1/4] Building LagoVista.CloudStorage..."
dotnet build $cloudStorageProject
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "[2/4] Running Mongo integration baseline..."
dotnet test $project --filter "TestCategory=Mongo"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "[3/4] Running Cosmos emulator smoke test..."
dotnet test $project --filter "TestCategory=CosmosSandbox"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "[4/4] Auditing production direct Cosmos consumers..."
& (Join-Path $PSScriptRoot "audit-cosmos-consumers.ps1")

Write-Host ""
Write-Host "Storage lab baseline complete. Mongo and Cosmos are reachable; use the Cosmos audit as the Card 6B work queue."
