$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "StorageUtils"
Write-Host "============"
Write-Host "Status: PENDING"
Write-Host "Provider-neutral conversion and parity coverage have not been added yet."
Write-Host ""
& (Join-Path $PSScriptRoot "audit-cosmos-consumers.ps1") -Scope Application -FileContains "StorageUtils.cs"
