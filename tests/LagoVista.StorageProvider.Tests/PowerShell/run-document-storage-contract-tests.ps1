$ErrorActionPreference = "Stop"

$scriptRoot = $PSScriptRoot
& (Join-Path $scriptRoot "start-mongo-tests.ps1")

$project = Join-Path $scriptRoot "..\LagoVista.StorageProvider.Tests.csproj"
$repoRoot = (Resolve-Path (Join-Path $scriptRoot "..\..\..")).Path
$evidenceDir = Join-Path $repoRoot ".coverage\evidence\document-storage-contract"
$txtPath = Join-Path $evidenceDir "latest.txt"
$trxPath = Join-Path $evidenceDir "latest.trx"

New-Item -ItemType Directory -Path $evidenceDir -Force | Out-Null
Remove-Item $txtPath -ErrorAction SilentlyContinue
Remove-Item $trxPath -ErrorAction SilentlyContinue

@(
    "CloudStorage Document Storage Contract Test Evidence",
    "StartedUtc: $([DateTime]::UtcNow.ToString('o'))",
    "Project: $project",
    "Filter: TestCategory=DocumentStorageContract",
    "Provider: MongoDB localhost:27018",
    ""
) | Set-Content -Path $txtPath

$arguments = @(
    "test",
    $project,
    "--filter", "TestCategory=DocumentStorageContract",
    "--logger", "trx;LogFileName=$trxPath"
)

& dotnet @arguments 2>&1 | Tee-Object -FilePath $txtPath -Append
$exitCode = $LASTEXITCODE

@(
    "",
    "CompletedUtc: $([DateTime]::UtcNow.ToString('o'))",
    "ExitCode: $exitCode"
) | Add-Content -Path $txtPath

exit $exitCode
