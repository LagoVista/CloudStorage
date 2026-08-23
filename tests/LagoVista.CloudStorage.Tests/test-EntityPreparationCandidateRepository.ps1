$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "Testing EntityPreparationCandidateRepository"
Write-Host "============================================"
Write-Host "Runs the five named Cosmos-vs-Mongo parity contracts for this class."
Write-Host ""

& (Join-Path $PSScriptRoot "run-storage-lab-baseline.ps1") `
    -Gate Build,Parity `
    -ParityTarget EntityPreparationCandidateRepository

exit $LASTEXITCODE
