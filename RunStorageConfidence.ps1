param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [switch]$KeepContainers
)

$runner = Join-Path $PSScriptRoot "tests\LagoVista.StorageProvider.Tests\PowerShell\run-storage-confidence-suite.ps1"
& $runner -Configuration $Configuration -KeepContainers:$KeepContainers
exit $LASTEXITCODE
