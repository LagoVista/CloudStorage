param(
    [ValidateSet("All", "Application", "Forbidden")]
    [string]$Scope = "All",

    [string]$FileContains
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$sourceRoot = Join-Path $repoRoot "src\LagoVista.CloudStorage"

$cosmosPatterns = @(
    "Microsoft.Azure.Cosmos",
    "CosmosClient",
    "ICosmosClientProvider",
    "QueryDefinition",
    "PatchOperation",
    "GetItemQueryIterator",
    "GetItemLinqQueryable",
    "FeedIterator"
)

$mongoPatterns = @(
    "MongoDB.Driver",
    "MongoDB.Bson",
    "MongoClient",
    "IMongoCollection",
    "Builders<",
    "BsonDocument"
)

# Direct Cosmos SDK access is intentionally constrained to exactly two provider-boundary files.
# Everything else must go through IDocumentStorageClient / IDocumentStorageClientProvider.
$allowedCosmosSdkFiles = @(
    "src\LagoVista.CloudStorage\Storage\StorageProviders\CosmosDB\CosmosDocumentStorageClient.cs",
    "src\LagoVista.CloudStorage\Storage\StorageProviders\CosmosDB\CosmosDocumentCollectionProvisioner.cs"
)

# Mongo SDK access is allowed only inside the Mongo provider implementation folder.
$allowedMongoPrefix = "src\LagoVista.CloudStorage\Storage\StorageProviders\Mongo\"

# Application-facing consumers are the provider-neutralization queue. Keep this list aligned
# with the current project layout so the audit cannot silently miss moved files.
$applicationConsumers = @(
    "src\LagoVista.CloudStorage\Repositories\EntityPreparationCandidateRepository.cs",
    "src\LagoVista.CloudStorage\Repositories\EntityListItemRepo.cs",
    "src\LagoVista.CloudStorage\Repositories\EntityUtilsRepository.cs",
    "src\LagoVista.CloudStorage\Utils\StorageUtils.cs",
    "src\LagoVista.CloudStorage\Repositories\CosmosSyncRepository.cs",
    "src\LagoVista.CloudStorage\Storage\DocumentDBRepoBase.cs"
)

Write-Host "Auditing production CloudStorage source for direct document SDK usage..."
Write-Host "Scope: $Scope"
if ($FileContains) { Write-Host "File filter: $FileContains" }

$files = Get-ChildItem -Path $sourceRoot -Recurse -Filter *.cs | ForEach-Object {
    $path = $_.FullName
    $relativePath = $path.Substring($repoRoot.Path.Length + 1)
    $content = Get-Content -Raw $path

    $cosmosMatches = @($cosmosPatterns | Where-Object { $content.Contains($_) })
    $mongoMatches = @($mongoPatterns | Where-Object { $content.Contains($_) })

    if ($cosmosMatches.Count -gt 0 -or $mongoMatches.Count -gt 0) {
        $cosmosAllowed = $cosmosMatches.Count -eq 0 -or $allowedCosmosSdkFiles -contains $relativePath
        $mongoAllowed = $mongoMatches.Count -eq 0 -or $relativePath.StartsWith($allowedMongoPrefix, [System.StringComparison]::OrdinalIgnoreCase)

        [PSCustomObject]@{
            File = $relativePath
            Cosmos = ($cosmosMatches -join ", ")
            Mongo = ($mongoMatches -join ", ")
            IsApplication = $applicationConsumers -contains $relativePath
            Allowed = $cosmosAllowed -and $mongoAllowed
        }
    }
}

$filteredFiles = @($files)
switch ($Scope) {
    "Application" { $filteredFiles = @($filteredFiles | Where-Object { $_.IsApplication }) }
    "Forbidden" { $filteredFiles = @($filteredFiles | Where-Object { -not $_.Allowed }) }
}

if ($FileContains) {
    $filteredFiles = @($filteredFiles | Where-Object { $_.File -like "*$FileContains*" })
}

if ($filteredFiles.Count -eq 0) {
    Write-Host "No document SDK consumers matched the selected audit filters."
}
else {
    $filteredFiles | Sort-Object File | Select-Object File, Cosmos, Mongo, Allowed | Format-Table -AutoSize
    Write-Host ""
    Write-Host "Showing $($filteredFiles.Count) of $(@($files).Count) production files with direct document-SDK-related usage."
}

$forbidden = @($files | Where-Object { -not $_.Allowed })
$cosmosSdkFiles = @($files | Where-Object { $_.Cosmos -and ($_.Cosmos -split ", ") -contains "Microsoft.Azure.Cosmos" })

Write-Host ""
Write-Host "Cosmos SDK boundary files: $($cosmosSdkFiles.Count) (required final state: exactly 2)."
Write-Host "Forbidden document SDK consumers: $($forbidden.Count)."

if ($forbidden.Count -gt 0) {
    Write-Host ""
    Write-Host "FAIL: direct Cosmos/Mongo SDK usage exists outside the approved provider boundaries." -ForegroundColor Red
    $forbidden | Sort-Object File | Select-Object File, Cosmos, Mongo | Format-Table -AutoSize
    exit 1
}

if ($cosmosSdkFiles.Count -ne 2) {
    Write-Host "FAIL: expected exactly two production files to reference the Cosmos SDK directly." -ForegroundColor Red
    exit 1
}

Write-Host "PASS: document SDK usage is confined to the approved provider boundaries." -ForegroundColor Green
