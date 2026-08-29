$ErrorActionPreference = "Stop"

& (Join-Path $PSScriptRoot "start-postgres-account-ledger-tests.ps1")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$project = Resolve-Path (Join-Path $PSScriptRoot "..\LagoVista.StorageProvider.Tests.csproj")
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
$evidenceDir = Join-Path $repoRoot ".coverage\evidence\account-ledger-postgres-store"
$logPath = Join-Path $evidenceDir "latest.txt"
$trxPath = Join-Path $evidenceDir "latest.trx"

New-Item -ItemType Directory -Force -Path $evidenceDir | Out-Null
Remove-Item $logPath, $trxPath -Force -ErrorAction SilentlyContinue

Write-Host "Running PostgreSQL Account Ledger integration tests..."
Write-Host "Evidence: $evidenceDir"

$startedUtc = [DateTime]::UtcNow.ToString("o")
@(
    "CloudStorage PostgreSQL Account Ledger Integration Test Evidence"
    "StartedUtc: $startedUtc"
    "Project: $project"
    "Filter: TestCategory=PostgresAccountLedger"
    "Provider: PostgreSQL localhost:19044"
    ""
) | Set-Content -Path $logPath

dotnet test $project `
    --filter "TestCategory=PostgresAccountLedger" `
    --logger "trx;LogFileName=$trxPath" 2>&1 | Tee-Object -FilePath $logPath -Append

$exitCode = $LASTEXITCODE
$completedUtc = [DateTime]::UtcNow.ToString("o")
@(
    ""
    "CompletedUtc: $completedUtc"
    "ExitCode: $exitCode"
) | Add-Content -Path $logPath

exit $exitCode
