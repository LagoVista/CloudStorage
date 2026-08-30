param(
    [Parameter(Mandatory = $false)]
    [string]$Version = '5.0.0',

    [Parameter(Mandatory = $false)]
    [string]$OutputDirectory = './artifacts/packages',

    [Parameter(Mandatory = $false)]
    [string]$CatalogPath = './artifacts/package-catalog.json'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Write-PackageStatus {
    param(
        [Parameter(Mandatory = $true)][string]$PackageId,
        [Parameter(Mandatory = $true)][string]$PackageVersion,
        [Parameter(Mandatory = $true)][string]$State,
        [Parameter(Mandatory = $false)][string]$Message
    )

    $payload = [ordered]@{
        type = 'package'
        state = $State
        packageId = $PackageId
        version = $PackageVersion
        message = $Message
    }
    Write-Output ('BUILD_STATUS:' + ($payload | ConvertTo-Json -Compress))
}

$repoRoot = $PSScriptRoot
Set-Location $repoRoot

$solutionPath = Join-Path $repoRoot 'CloudStorage.sln'
$propsPath = Join-Path $repoRoot 'Directory.Packages.props'
$outputPath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDirectory))
$catalogFullPath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $CatalogPath))

if (-not (Test-Path $solutionPath)) { throw "Solution not found: $solutionPath" }
if (-not (Test-Path $propsPath)) { throw "Directory.Packages.props not found: $propsPath" }

if (Test-Path $outputPath) { Remove-Item -Recurse -Force $outputPath }
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
New-Item -ItemType Directory -Force -Path (Split-Path $catalogFullPath -Parent) | Out-Null

[xml]$props = Get-Content $propsPath -Raw
$centralVersions = @{}
foreach ($node in @($props.SelectNodes('//PackageVersion'))) {
    $id = [string]$node.Include
    $versionValue = [string]$node.Version
    if (-not [string]::IsNullOrWhiteSpace($id) -and -not [string]::IsNullOrWhiteSpace($versionValue)) {
        $centralVersions[$id] = $versionValue
    }
}

$nuspecFiles = @(Get-ChildItem -Path $repoRoot -Filter 'Package.nuspec' -File -Recurse |
    Where-Object { $_.FullName -notmatch '[\\/]artifacts[\\/]' } |
    Sort-Object FullName)
if ($nuspecFiles.Count -eq 0) { throw 'No Package.nuspec files were found.' }

$packages = @()
$packageIds = @{}
foreach ($nuspec in $nuspecFiles) {
    [xml]$xml = Get-Content $nuspec.FullName -Raw
    $metadata = $xml.package.metadata
    if ($null -eq $metadata -or [string]::IsNullOrWhiteSpace([string]$metadata.id)) {
        throw "NuSpec '$($nuspec.FullName)' does not contain a package id."
    }

    $packageId = [string]$metadata.id
    if ($packageIds.ContainsKey($packageId)) { throw "Duplicate package id '$packageId'." }

    $projectFiles = @(Get-ChildItem -Path $nuspec.DirectoryName -Filter '*.csproj' -File)
    if ($projectFiles.Count -ne 1) {
        throw "Expected exactly one project beside '$($nuspec.FullName)', found $($projectFiles.Count)."
    }

    $packageIds[$packageId] = $nuspec.FullName
    $packages += [pscustomobject]@{
        Id = $packageId
        NuSpecPath = $nuspec.FullName
        ProjectPath = $projectFiles[0].FullName
        BasePath = $nuspec.DirectoryName
        Xml = $xml
    }
}

Write-Host "Discovered $($packages.Count) CloudStorage packages:"
$packages | Sort-Object Id | ForEach-Object {
    Write-Host "  $($_.Id)"
    Write-PackageStatus -PackageId $_.Id -PackageVersion $Version -State 'pending' -Message 'Waiting to pack package'
}

$xmlSettings = New-Object System.Xml.XmlWriterSettings
$xmlSettings.Indent = $true
$xmlSettings.Encoding = New-Object System.Text.UTF8Encoding($false)
$xmlSettings.NewLineChars = "`r`n"
$xmlSettings.NewLineHandling = [System.Xml.NewLineHandling]::Replace

foreach ($package in $packages) {
    $metadata = $package.Xml.package.metadata
    $metadata.version = $Version

    foreach ($dependency in @($package.Xml.SelectNodes('//dependency'))) {
        $dependencyId = [string]$dependency.id
        if ([string]::IsNullOrWhiteSpace($dependencyId)) { continue }

        if ($packageIds.ContainsKey($dependencyId)) {
            $dependency.version = $Version
        }
        elseif ($centralVersions.ContainsKey($dependencyId)) {
            $dependency.version = [string]$centralVersions[$dependencyId]
        }
        elseif ($dependencyId.StartsWith('LagoVista.', [System.StringComparison]::OrdinalIgnoreCase) -or
                $dependencyId.StartsWith('NuvIoT.', [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Internal dependency '$dependencyId' is not present in Directory.Packages.props."
        }
    }

    if (-not $IsWindows) {
        foreach ($fileNode in @($package.Xml.SelectNodes('//files/file'))) {
            $sourcePath = [string]$fileNode.src
            if (-not [string]::IsNullOrWhiteSpace($sourcePath)) {
                $sourcePath = $sourcePath.Replace('\', '/')
                $sourcePath = [regex]::Replace(
                    $sourcePath,
                    '^bin/release/',
                    'bin/Release/',
                    [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
                $fileNode.src = $sourcePath
            }
        }
    }

    $writer = [System.Xml.XmlWriter]::Create($package.NuSpecPath, $xmlSettings)
    try { $package.Xml.Save($writer) } finally { $writer.Dispose() }
}

Write-Host "Restoring CloudStorage.sln for package set $Version..."
dotnet restore $solutionPath
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed with exit code $LASTEXITCODE." }

Write-Host 'Building CloudStorage.sln...'
dotnet build $solutionPath --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE." }

$catalogPackages = @()
foreach ($package in ($packages | Sort-Object Id)) {
    Write-PackageStatus -PackageId $package.Id -PackageVersion $Version -State 'packing' -Message 'Creating NuGet package'
    Write-Host "Packing $($package.Id) $Version with .NET SDK..."
    dotnet pack $package.ProjectPath `
        --configuration Release `
        --no-build `
        --no-restore `
        --output $outputPath `
        "-p:IsPackable=true" `
        "-p:NuspecFile=$($package.NuSpecPath)" `
        "-p:NuspecBasePath=$($package.BasePath)"
    if ($LASTEXITCODE -ne 0) {
        Write-PackageStatus -PackageId $package.Id -PackageVersion $Version -State 'failed' -Message "dotnet pack exited with code $LASTEXITCODE"
        throw "dotnet pack failed for '$($package.Id)' with exit code $LASTEXITCODE."
    }

    $packageFile = "$($package.Id).$Version.nupkg"
    $packagePath = Join-Path $outputPath $packageFile
    if (-not (Test-Path $packagePath)) {
        Write-PackageStatus -PackageId $package.Id -PackageVersion $Version -State 'failed' -Message "Expected package was not produced: $packagePath"
        throw "Expected package was not produced: $packagePath"
    }
    Write-PackageStatus -PackageId $package.Id -PackageVersion $Version -State 'packed' -Message "Created $packageFile"

    $frameworks = @($package.Xml.SelectNodes('//dependencies/group') | ForEach-Object { [string]$_.targetFramework } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
    $dependencies = @()
    foreach ($dependency in @($package.Xml.SelectNodes('//dependency'))) {
        $dependencyId = [string]$dependency.id
        $kind = if ($packageIds.ContainsKey($dependencyId)) { 'repository' }
            elseif ($dependencyId.StartsWith('LagoVista.', [System.StringComparison]::OrdinalIgnoreCase) -or $dependencyId.StartsWith('NuvIoT.', [System.StringComparison]::OrdinalIgnoreCase)) { 'platform' }
            else { 'external' }
        $dependencies += [ordered]@{ id = $dependencyId; version = [string]$dependency.version; kind = $kind }
    }

    $catalogPackages += [ordered]@{
        id = $package.Id
        version = $Version
        file = $packageFile
        targetFrameworks = $frameworks
        dependencies = $dependencies
    }
}

$sourceRepository = if ($env:GITHUB_REPOSITORY) { $env:GITHUB_REPOSITORY } else { 'LagoVista/CloudStorage' }
$sourceCommit = if ($env:GITHUB_SHA) { $env:GITHUB_SHA } else { (git rev-parse HEAD).Trim() }
$sourceRef = if ($env:GITHUB_REF_NAME) { $env:GITHUB_REF_NAME } else { (git branch --show-current).Trim() }

$catalog = [ordered]@{
    schemaVersion = 1
    generatedUtc = [DateTime]::UtcNow.ToString('o')
    source = [ordered]@{ repository = $sourceRepository; commit = $sourceCommit; ref = $sourceRef }
    packages = $catalogPackages
}

$catalog | ConvertTo-Json -Depth 10 | Set-Content -Path $catalogFullPath -Encoding utf8
Write-Host "Created $($catalogPackages.Count) packages in $outputPath"
Write-Host "Created $catalogFullPath"
