$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "EntityListItemRepo"
Write-Host "=================="
Write-Host "Status: NEXT"
Write-Host "Parity coverage has not been added yet. This is the current Card 6B conversion target."
Write-Host ""
& (Join-Path $PSScriptRoot "audit-cosmos-consumers.ps1") -Scope Application -FileContains "EntityListItemRepo.cs"
