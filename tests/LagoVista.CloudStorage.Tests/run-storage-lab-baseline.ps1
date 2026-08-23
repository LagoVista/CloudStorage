param(
    [ValidateSet("All", "Build", "Mongo", "Cosmos", "Parity", "Audit")]
    [string[]]$Gate = @("All"),

    [string]$ParityTarget,

    [ValidateSet("All", "Application", "Tracked")]
    [string]$AuditScope = "All",

    [switch]$SkipLabStart
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$project = Join-Path $PSScriptRoot "LagoVista.CloudStorage.IntegrationTests.csproj"
$cloudStorageProject = Join-Path $repoRoot "src\LagoVista.CloudStorage\LagoVista.CloudStorage.csproj"
$testLogger = "console;verbosity=normal"

$allGates = @("Build", "Mongo", "Cosmos", "Parity", "Audit")
$selectedGates = if ($Gate -contains "All") { $allGates } else { $allGates | Where-Object { $Gate -contains $_ } }

if (-not $selectedGates) {
    throw "No storage-lab gates were selected."
}

$labRequired = ($selectedGates -contains "Mongo") -or ($selectedGates -contains "Cosmos") -or ($selectedGates -contains "Parity")
if ($labRequired -and -not $SkipLabStart) {
    & (Join-Path $PSScriptRoot "start-storage-lab.ps1")
}

Write-Host ""
Write-Host "Selected storage-lab gates: $($selectedGates -join ', ')"
if (($selectedGates -contains "Parity") -and $ParityTarget) {
    Write-Host "Parity target: $ParityTarget"
}
if ($selectedGates -contains "Audit") {
    Write-Host "Audit scope: $AuditScope"
}

if ($selectedGates -contains "Build") {
    Write-Host ""
    Write-Host "[Build] Building LagoVista.CloudStorage..."
    dotnet build $cloudStorageProject
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if ($selectedGates -contains "Mongo") {
    Write-Host ""
    Write-Host "[Mongo] Running Mongo integration baseline..."
    dotnet test $project --filter "TestCategory=Mongo" --logger $testLogger
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if ($selectedGates -contains "Cosmos") {
    Write-Host ""
    Write-Host "[Cosmos] Running Cosmos emulator smoke tests..."
    dotnet test $project --filter "TestCategory=CosmosSandbox" --logger $testLogger
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if ($selectedGates -contains "Parity") {
    Write-Host ""
    Write-Host "[Parity] Running Cosmos-vs-Mongo storage parity..."

    $parityFilter = "TestCategory=StorageParity"
    if ($ParityTarget) {
        $parityFilter = "$parityFilter&FullyQualifiedName~$ParityTarget"
        Write-Host "         Target filter: FullyQualifiedName contains '$ParityTarget'"
    }
    else {
        Write-Host "         Target filter: all StorageParity tests"
    }

    dotnet test $project --filter $parityFilter --logger $testLogger
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if ($selectedGates -contains "Audit") {
    Write-Host ""
    Write-Host "[Audit] Auditing production direct Cosmos consumers..."
    & (Join-Path $PSScriptRoot "audit-cosmos-consumers.ps1") -Scope $AuditScope
}

Write-Host ""
Write-Host "Selected storage-lab gates completed successfully."
if ($selectedGates.Count -eq $allGates.Count) {
    Write-Host "Full baseline is green: build, Mongo, Cosmos, provider parity, and Cosmos-consumer audit all completed."
}
else {
    Write-Host "This was a selective run. Use -Gate All for the authoritative full baseline before a Card 6B milestone or cutover decision."
}
