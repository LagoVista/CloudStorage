$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "CosmosSyncRepository"
Write-Host "===================="
Write-Host "Status: PENDING / CLASSIFY"
Write-Host "This class still needs method-by-method Card 6B classification before parity expectations are finalized."
Write-Host ""
& (Join-Path $PSScriptRoot "audit-cosmos-consumers.ps1") -Scope Application -FileContains "CosmosSyncRepository.cs"
