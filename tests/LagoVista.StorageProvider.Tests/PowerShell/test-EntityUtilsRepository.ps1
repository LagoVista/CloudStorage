$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "EntityUtilsRepository"
Write-Host "====================="
Write-Host "Status: READ SLICE READY TO VERIFY"
Write-Host "Parity coverage: 5 named Cosmos-vs-Mongo read contracts"
Write-Host "Mutation/patch behavior remains Cosmos-backed and is not counted as complete yet."
Write-Host ""

& (Join-Path $PSScriptRoot "run-storage-lab-baseline.ps1") -Gate Build,Parity -ParityTarget EntityUtilsRepository
