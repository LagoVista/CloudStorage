param(
    [ValidateSet("All", "Application", "Tracked")]
    [string]$Scope = "All",

    [string]$FileContains
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$sourceRoot = Join-Path $repoRoot "src\LagoVista.CloudStorage"
$patterns = @(
    "Microsoft.Azure.Cosmos",
    "CosmosClient",
    "Container",
    "QueryDefinition",
    "PatchOperation",
    "GetItemQueryIterator",
    "GetItemLinqQueryable",
    "FeedIterator"
)

# Keep this table intentionally small and explicit. As Card 6B converts an
# application-facing Cosmos consumer, record its named StorageParity coverage here.
$parityProgress = @{
    "src\LagoVista.CloudStorage\Storage\EntityPreparationCandidateRepository.cs" = "5/5"
    "src\LagoVista.CloudStorage\Storage\EntityListItemRepo.cs" = "5/5"
    "src\LagoVista.CloudStorage\Storage\EntityUtilsRepository.cs" = "5/5 reads"
}

# Application-facing consumers are the Card 6B conversion queue. Infrastructure
# providers and migration/security surfaces remain visible in Scope=All.
$applicationConsumers = @(
    "src\LagoVista.CloudStorage\Storage\EntityPreparationCandidateRepository.cs",
    "src\LagoVista.CloudStorage\Storage\EntityListItemRepo.cs",
    "src\LagoVista.CloudStorage\Storage\EntityUtilsRepository.cs",
    "src\LagoVista.CloudStorage\Storage\StorageUtils.cs",
    "src\LagoVista.CloudStorage\Storage\CosmosSyncRepository.cs"
)

Write-Host "Auditing production CloudStorage source for direct Cosmos usage..."
Write-Host "Scope: $Scope"
if ($FileContains) { Write-Host "File filter: $FileContains" }

$files = Get-ChildItem -Path $sourceRoot -Recurse -Filter *.cs | ForEach-Object {
    $path = $_.FullName
    $relativePath = $path.Substring($repoRoot.Path.Length + 1)
    $content = Get-Content -Raw $path
    $matches = $patterns | Where-Object { $content.Contains($_) }
    if ($matches.Count -gt 0) {
        [PSCustomObject]@{
            File = $relativePath
            Matches = ($matches -join ", ")
            Parity = if ($parityProgress.ContainsKey($relativePath)) { $parityProgress[$relativePath] } else { "-" }
            IsApplication = $applicationConsumers -contains $relativePath
        }
    }
}

if (-not $files) {
    Write-Host "No direct Cosmos usage found in production CloudStorage source."
    return
}

$filteredFiles = $files
switch ($Scope) {
    "Application" { $filteredFiles = $filteredFiles | Where-Object { $_.IsApplication } }
    "Tracked" { $filteredFiles = $filteredFiles | Where-Object { $_.Parity -ne "-" } }
}

if ($FileContains) {
    $filteredFiles = $filteredFiles | Where-Object { $_.File -like "*$FileContains*" }
}

$filteredFiles = @($filteredFiles)
if ($filteredFiles.Count -eq 0) {
    Write-Host "No Cosmos consumers matched the selected audit filters."
    return
}

$filteredFiles | Sort-Object File | Select-Object File, Matches, Parity | Format-Table -AutoSize
Write-Host ""
Write-Host "Showing $($filteredFiles.Count) of $($files.Count) production files with direct Cosmos-related usage."
Write-Host "Parity shows passed/total named StorageParity contracts for converted application-facing consumers; '-' means not yet tracked by that suite."
Write-Host "Every file must have an explicit Card 6B disposition before Mongo cutover: provider implementation, provider-neutralize, intentional Cosmos-only, or remove/defer."
