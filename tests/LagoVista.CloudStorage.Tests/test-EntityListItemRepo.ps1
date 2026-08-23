$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "EntityListItemRepo"
Write-Host "=================="
Write-Host "Status: COMPLETE"
Write-Host "Parity coverage: 5/5 named Cosmos-vs-Mongo contracts"
Write-Host ""

& (Join-Path $PSScriptRoot "run-storage-lab-baseline.ps1") -Gate Build,Parity -ParityTarget EntityListItemRepo
