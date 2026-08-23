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

Write-Host "Auditing production CloudStorage source for direct Cosmos usage..."

$files = Get-ChildItem -Path $sourceRoot -Recurse -Filter *.cs | ForEach-Object {
    $path = $_.FullName
    $content = Get-Content -Raw $path
    $matches = $patterns | Where-Object { $content.Contains($_) }
    if ($matches.Count -gt 0) {
        [PSCustomObject]@{
            File = $path.Substring($repoRoot.Path.Length + 1)
            Matches = ($matches -join ", ")
        }
    }
}

if (-not $files) {
    Write-Host "No direct Cosmos usage found in production CloudStorage source."
    return
}

$files | Sort-Object File | Format-Table -AutoSize
Write-Host ""
Write-Host "Found $($files.Count) production files with direct Cosmos-related usage."
Write-Host "Every file must have an explicit Card 6B disposition before Mongo cutover: provider implementation, provider-neutralize, intentional Cosmos-only, or remove/defer."
