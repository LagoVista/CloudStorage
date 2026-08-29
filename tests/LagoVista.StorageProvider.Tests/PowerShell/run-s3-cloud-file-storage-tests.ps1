$ErrorActionPreference = "Stop"

& (Join-Path $PSScriptRoot "start-s3-tests.ps1")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$project = Resolve-Path (Join-Path $PSScriptRoot "..\LagoVista.StorageProvider.Tests.csproj")
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
$evidenceDir = Join-Path $repoRoot ".coverage\evidence\s3-cloud-file-storage"
$logPath = Join-Path $evidenceDir "latest.txt"
$trxPath = Join-Path $evidenceDir "latest.trx"

New-Item -ItemType Directory -Force -Path $evidenceDir | Out-Null
Remove-Item $logPath, $trxPath -Force -ErrorAction SilentlyContinue

Write-Host "Running S3CloudFileStorageClient integration tests..."
Write-Host "Evidence: $evidenceDir"

$startedUtc = [DateTime]::UtcNow.ToString("o")
@(
    "CloudStorage S3 Cloud File Storage Test Evidence"
    "StartedUtc: $startedUtc"
    "Project: $project"
    "Filter: TestCategory=S3CloudFileStorage"
    "Provider: MinIO S3-compatible endpoint http://localhost:19090"
    ""
) | Set-Content -Path $logPath

dotnet test $project `
    --filter "TestCategory=S3CloudFileStorage" `
    --logger "trx;LogFileName=$trxPath" 2>&1 | Tee-Object -FilePath $logPath -Append

$exitCode = $LASTEXITCODE
$completedUtc = [DateTime]::UtcNow.ToString("o")
@(
    ""
    "CompletedUtc: $completedUtc"
    "ExitCode: $exitCode"
) | Add-Content -Path $logPath

exit $exitCode
