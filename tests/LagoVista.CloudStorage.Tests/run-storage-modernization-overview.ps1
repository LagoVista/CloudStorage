$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "CloudStorage storage modernization - Card 6B"
Write-Host "============================================="
Write-Host ""

$work = @(
    [PSCustomObject]@{
        Class = "EntityPreparationCandidateRepository"
        Status = "COMPLETE"
        Parity = "5/5"
        Test = ".\test-EntityPreparationCandidateRepository.ps1"
    },
    [PSCustomObject]@{
        Class = "EntityListItemRepo"
        Status = "COMPLETE"
        Parity = "5/5"
        Test = ".\test-EntityListItemRepo.ps1"
    },
    [PSCustomObject]@{
        Class = "EntityUtilsRepository"
        Status = "READ SLICE READY"
        Parity = "0/5 reads"
        Test = ".\test-EntityUtilsRepository.ps1"
    },
    [PSCustomObject]@{
        Class = "StorageUtils"
        Status = "PENDING"
        Parity = "-"
        Test = ".\test-StorageUtils.ps1"
    },
    [PSCustomObject]@{
        Class = "CosmosSyncRepository"
        Status = "PENDING / CLASSIFY"
        Parity = "-"
        Test = ".\test-CosmosSyncRepository.ps1"
    }
)

$work | Format-Table Class, Status, Parity, Test -AutoSize

Write-Host ""
Write-Host "COMPLETE         = provider-neutral conversion implemented and parity coverage green."
Write-Host "READ SLICE READY = a coherent provider-neutral slice is ready for its focused parity run; the class is not complete yet."
Write-Host "PENDING          = not yet converted / parity-tested."
Write-Host ""
Write-Host "Run the script shown in the Test column when you want to validate one class."
Write-Host "Run .\run-storage-lab-baseline.ps1 only when you want the full authoritative milestone baseline."
Write-Host "Run .\audit-cosmos-consumers.ps1 when you want the deeper repo-wide Cosmos-reference inventory."
