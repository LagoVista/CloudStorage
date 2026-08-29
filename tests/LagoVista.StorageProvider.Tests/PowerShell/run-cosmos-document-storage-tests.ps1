$ErrorActionPreference = "Stop"

& (Join-Path $PSScriptRoot "start-storage-lab.ps1")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$project = Resolve-Path (Join-Path $PSScriptRoot "..\LagoVista.StorageProvider.Tests.csproj")
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
$evidenceDir = Join-Path $repoRoot ".coverage\evidence\cosmos-document-storage-client"
$logPath = Join-Path $evidenceDir "latest.txt"
$trxPath = Join-Path $evidenceDir "latest.trx"

New-Item -ItemType Directory -Force -Path $evidenceDir | Out-Null
Remove-Item $logPath, $trxPath -Force -ErrorAction SilentlyContinue

Write-Host "Running Cosmos IDocumentStorageClient shared contract tests..."
Write-Host "Evidence: $evidenceDir"

$startedUtc = [DateTime]::UtcNow.ToString("o")
@(
    "CloudStorage Cosmos Document Storage Contract Test Evidence"
    "StartedUtc: $startedUtc"
    "Project: $project"
    "Filter: TestCategory=CosmosDocumentStorageContract"
    "Provider: Cosmos emulator https://localhost:18081"
    ""
) | Set-Content -Path $logPath

dotnet test $project `
    --filter "TestCategory=CosmosDocumentStorageContract" `
    --logger "trx;LogFileName=$trxPath" 2>&1 | Tee-Object -FilePath $logPath -Append

$exitCode = $LASTEXITCODE
$completedUtc = [DateTime]::UtcNow.ToString("o")
@(
    ""
    "CompletedUtc: $completedUtc"
    "ExitCode: $exitCode"
) | Add-Content -Path $logPath

exit $exitCode
