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
}

Write-Host "Auditing production CloudStorage source for direct Cosmos usage..."

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
        }
    }
}

if (-not $files) {
    Write-Host "No direct Cosmos usage found in production CloudStorage source."
    return
}

$files | Sort-Object File | Format-Table File, Matches, Parity -AutoSize
Write-Host ""
Write-Host "Found $($files.Count) production files with direct Cosmos-related usage."
Write-Host "Parity shows passed/total named StorageParity contracts for converted application-facing consumers; '-' means not yet tracked by that suite."
Write-Host "Every file must have an explicit Card 6B disposition before Mongo cutover: provider implementation, provider-neutralize, intentional Cosmos-only, or remove/defer."
