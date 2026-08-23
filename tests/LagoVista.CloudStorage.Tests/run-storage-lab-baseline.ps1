$ErrorActionPreference = "Stop"

& (Join-Path $PSScriptRoot "start-storage-lab.ps1")

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$project = Join-Path $PSScriptRoot "LagoVista.CloudStorage.IntegrationTests.csproj"
$cloudStorageProject = Join-Path $repoRoot "src\LagoVista.CloudStorage\LagoVista.CloudStorage.csproj"
$testLogger = "console;verbosity=normal"

Write-Host ""
Write-Host "[1/5] Building LagoVista.CloudStorage..."
dotnet build $cloudStorageProject
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "[2/5] Running Mongo integration baseline..."
dotnet test $project --filter "TestCategory=Mongo" --logger $testLogger
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "[3/5] Running Cosmos emulator smoke test..."
dotnet test $project --filter "TestCategory=CosmosSandbox" --logger $testLogger
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "[4/5] Running Cosmos-vs-Mongo storage parity..."
Write-Host "      EntityPreparationCandidateRepository: 5 named parity contracts"
dotnet test $project --filter "TestCategory=StorageParity" --logger $testLogger
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "[5/5] Auditing production direct Cosmos consumers..."
& (Join-Path $PSScriptRoot "audit-cosmos-consumers.ps1")

Write-Host ""
Write-Host "Storage lab baseline complete. Mongo, Cosmos, and provider parity are green; use the Cosmos audit as the Card 6B work queue."
