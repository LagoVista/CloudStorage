<#
Runs the authoritative local storage confidence gate.

The suite owns the full local test lifecycle:
  1. Start every storage dependency used by LagoVista.StorageProvider.Tests.
  2. Build the CloudStorage solution.
  3. Run the entire storage-provider test project with no category filter.
  4. Write consolidated TXT, JSON, and TRX evidence.
  5. Tear down all storage-test containers, even when startup/build/tests fail.

Examples:
  ./run-storage-confidence-suite.ps1
  ./run-storage-confidence-suite.ps1 -Configuration Release
  ./run-storage-confidence-suite.ps1 -KeepContainers
#>
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [switch]$KeepContainers
)

$ErrorActionPreference = "Stop"

$scriptRoot = $PSScriptRoot
$repoRoot = (Resolve-Path (Join-Path $scriptRoot "..\..\..")).Path
$solution = Join-Path $repoRoot "CloudStorage.sln"
$project = (Resolve-Path (Join-Path $scriptRoot "..\LagoVista.StorageProvider.Tests.csproj")).Path
$evidenceDir = Join-Path $repoRoot ".coverage\evidence\storage-confidence-suite"
$txtPath = Join-Path $evidenceDir "latest.txt"
$jsonPath = Join-Path $evidenceDir "latest.json"
$trxPath = Join-Path $evidenceDir "latest.trx"
$powerShellExe = (Get-Process -Id $PID).Path

New-Item -ItemType Directory -Force -Path $evidenceDir | Out-Null
Remove-Item $txtPath, $jsonPath, $trxPath -Force -ErrorAction SilentlyContinue

$startedUtc = [DateTime]::UtcNow
$startupResults = [System.Collections.Generic.List[object]]::new()
$teardownResults = [System.Collections.Generic.List[object]]::new()
$buildResult = $null
$testResult = $null
$failureMessage = $null

function Write-ReportLine {
    param([string]$Message = "")

    Write-Host $Message
    Add-Content -Path $txtPath -Value $Message
}

function Invoke-ChildPowerShellScript {
    param(
        [string]$Name,
        [string]$Path,
        [System.Collections.Generic.List[object]]$Results,
        [switch]$DoNotThrow
    )

    $stopwatch = [Diagnostics.Stopwatch]::StartNew()
    Write-ReportLine "[$Name]"

    & $powerShellExe -NoProfile -ExecutionPolicy Bypass -File $Path 2>&1 | Tee-Object -FilePath $txtPath -Append
    $exitCode = $LASTEXITCODE
    $stopwatch.Stop()

    $status = if ($exitCode -eq 0) { "PASS" } else { "FAIL" }
    $Results.Add([pscustomobject]@{
        name = $Name
        status = $status
        exitCode = $exitCode
        durationSeconds = [Math]::Round($stopwatch.Elapsed.TotalSeconds, 2)
    })

    Write-ReportLine "  $status exit=$exitCode duration=$([Math]::Round($stopwatch.Elapsed.TotalSeconds, 2))s"
    Write-ReportLine

    if ($exitCode -ne 0 -and -not $DoNotThrow) {
        throw "$Name failed with exit code $exitCode."
    }
}

function Invoke-DockerCleanup {
    param(
        [string]$Name,
        [scriptblock]$Action
    )

    $stopwatch = [Diagnostics.Stopwatch]::StartNew()
    Write-ReportLine "[$Name]"
    $exitCode = 0
    $errorText = $null

    try {
        & $Action 2>&1 | Tee-Object -FilePath $txtPath -Append
        if ($LASTEXITCODE -is [int]) { $exitCode = $LASTEXITCODE }
    }
    catch {
        $exitCode = 1
        $errorText = $_.Exception.Message
        Write-ReportLine "  $errorText"
    }

    $stopwatch.Stop()
    $status = if ($exitCode -eq 0) { "PASS" } else { "FAIL" }
    $teardownResults.Add([pscustomobject]@{
        name = $Name
        status = $status
        exitCode = $exitCode
        durationSeconds = [Math]::Round($stopwatch.Elapsed.TotalSeconds, 2)
        error = $errorText
    })

    Write-ReportLine "  $status exit=$exitCode duration=$([Math]::Round($stopwatch.Elapsed.TotalSeconds, 2))s"
    Write-ReportLine
}

function Get-TrxCounters {
    if (-not (Test-Path $trxPath)) { return $null }

    try {
        [xml]$trx = Get-Content -Path $trxPath -Raw
        $counters = $trx.SelectSingleNode("//*[local-name()='Counters']")
        if ($null -eq $counters) { return $null }

        return [pscustomobject]@{
            total = [int]$counters.total
            executed = [int]$counters.executed
            passed = [int]$counters.passed
            failed = [int]$counters.failed
            error = [int]$counters.error
            timeout = [int]$counters.timeout
            aborted = [int]$counters.aborted
            inconclusive = [int]$counters.inconclusive
            notExecuted = [int]$counters.notExecuted
        }
    }
    catch {
        Write-ReportLine "WARNING: Could not parse TRX counters: $($_.Exception.Message)"
        return $null
    }
}

$branch = (& git -C $repoRoot branch --show-current 2>$null | Out-String).Trim()
$commit = (& git -C $repoRoot rev-parse HEAD 2>$null | Out-String).Trim()

@(
    "CloudStorage Full Storage Confidence Suite"
    "StartedUtc: $($startedUtc.ToString('o'))"
    "Repository: $repoRoot"
    "Branch: $branch"
    "Commit: $commit"
    "Configuration: $Configuration"
    "TestProject: $project"
    ""
) | Set-Content -Path $txtPath

try {
    Write-ReportLine "=== START STORAGE DEPENDENCIES ==="
    Write-ReportLine

    docker info | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Docker is not available. Start Docker Desktop and try again." }

    Invoke-ChildPowerShellScript "MongoDB" (Join-Path $scriptRoot "start-mongo-tests.ps1") $startupResults
    Invoke-ChildPowerShellScript "Cosmos emulator" (Join-Path $scriptRoot "start-storage-lab.ps1") $startupResults
    Invoke-ChildPowerShellScript "Cassandra" (Join-Path $scriptRoot "start-cassandra-tests.ps1") $startupResults
    Invoke-ChildPowerShellScript "Redis" (Join-Path $scriptRoot "start-redis-tests.ps1") $startupResults
    Invoke-ChildPowerShellScript "PostgreSQL + TimescaleDB" (Join-Path $scriptRoot "start-postgres-metrics-tests.ps1") $startupResults
    Invoke-ChildPowerShellScript "PostgreSQL account ledger" (Join-Path $scriptRoot "start-postgres-account-ledger-tests.ps1") $startupResults
    Invoke-ChildPowerShellScript "S3 / MinIO" (Join-Path $scriptRoot "start-s3-tests.ps1") $startupResults

    Write-ReportLine "=== BUILD SOLUTION ==="
    Write-ReportLine

    $buildStopwatch = [Diagnostics.Stopwatch]::StartNew()
    dotnet build $solution --configuration $Configuration 2>&1 | Tee-Object -FilePath $txtPath -Append
    $buildExitCode = $LASTEXITCODE
    $buildStopwatch.Stop()
    $buildResult = [pscustomobject]@{
        status = if ($buildExitCode -eq 0) { "PASS" } else { "FAIL" }
        exitCode = $buildExitCode
        durationSeconds = [Math]::Round($buildStopwatch.Elapsed.TotalSeconds, 2)
    }
    Write-ReportLine "Build: $($buildResult.status) exit=$buildExitCode duration=$($buildResult.durationSeconds)s"
    Write-ReportLine
    if ($buildExitCode -ne 0) { throw "CloudStorage solution build failed with exit code $buildExitCode." }

    Write-ReportLine "=== RUN ALL STORAGE PROVIDER TESTS ==="
    Write-ReportLine "No category filter is used. Every test currently in LagoVista.StorageProvider.Tests participates in this gate."
    Write-ReportLine

    $testStopwatch = [Diagnostics.Stopwatch]::StartNew()
    dotnet test $project --configuration $Configuration --no-build --logger "trx;LogFileName=$trxPath" 2>&1 | Tee-Object -FilePath $txtPath -Append
    $testExitCode = $LASTEXITCODE
    $testStopwatch.Stop()
    $testResult = [pscustomobject]@{
        status = if ($testExitCode -eq 0) { "PASS" } else { "FAIL" }
        exitCode = $testExitCode
        durationSeconds = [Math]::Round($testStopwatch.Elapsed.TotalSeconds, 2)
    }
    Write-ReportLine "Tests: $($testResult.status) exit=$testExitCode duration=$($testResult.durationSeconds)s"
    Write-ReportLine

    if ($testExitCode -ne 0) { throw "Storage provider tests failed with exit code $testExitCode." }
}
catch {
    $failureMessage = $_.Exception.Message
    Write-ReportLine "SUITE FAILURE: $failureMessage"
    Write-ReportLine
}
finally {
    if ($KeepContainers) {
        Write-ReportLine "=== KEEP CONTAINERS REQUESTED ==="
        Write-ReportLine "Storage test containers were left running for diagnosis."
        Write-ReportLine
    }
    else {
        Write-ReportLine "=== STOP STORAGE DEPENDENCIES ==="
        Write-ReportLine

        Invoke-ChildPowerShellScript "Stop Cassandra" (Join-Path $scriptRoot "stop-cassandra-tests.ps1") $teardownResults -DoNotThrow
        Invoke-ChildPowerShellScript "Stop MongoDB" (Join-Path $scriptRoot "stop-mongo-tests.ps1") $teardownResults -DoNotThrow
        Invoke-ChildPowerShellScript "Stop Cosmos emulator" (Join-Path $scriptRoot "stop-storage-lab.ps1") $teardownResults -DoNotThrow

        Invoke-DockerCleanup "Stop S3 / MinIO" {
            docker compose -f (Join-Path $scriptRoot "docker-compose.s3.yml") down --remove-orphans
        }

        Invoke-DockerCleanup "Remove standalone test containers" {
            $containers = @(
                "cloudstorage-redis-tests",
                "cloudstorage-postgres-metrics-tests",
                "cloudstorage-postgres-account-ledger-tests"
            )

            foreach ($container in $containers) {
                $exists = (docker ps -a --filter "name=^/$container$" --format "{{.Names}}" | Out-String).Trim()
                if ($exists -eq $container) {
                    docker rm -f $container | Out-Null
                    if ($LASTEXITCODE -ne 0) { throw "Could not remove container $container." }
                }
            }
        }
    }

    $completedUtc = [DateTime]::UtcNow
    $trxCounters = Get-TrxCounters
    $teardownFailed = $teardownResults | Where-Object { $_.status -ne "PASS" }
    $overallPassed = [String]::IsNullOrWhiteSpace($failureMessage) -and (-not $teardownFailed)

    Write-ReportLine "=== SUMMARY ==="
    Write-ReportLine "Overall: $(if ($overallPassed) { 'PASS' } else { 'FAIL' })"
    Write-ReportLine "StartedUtc: $($startedUtc.ToString('o'))"
    Write-ReportLine "CompletedUtc: $($completedUtc.ToString('o'))"
    Write-ReportLine "DurationSeconds: $([Math]::Round(($completedUtc - $startedUtc).TotalSeconds, 2))"
    if ($trxCounters) {
        Write-ReportLine "Tests: total=$($trxCounters.total) executed=$($trxCounters.executed) passed=$($trxCounters.passed) failed=$($trxCounters.failed) notExecuted=$($trxCounters.notExecuted)"
    }
    if ($failureMessage) { Write-ReportLine "Failure: $failureMessage" }
    Write-ReportLine "Evidence: $evidenceDir"

    $report = [pscustomobject]@{
        startedUtc = $startedUtc.ToString("o")
        completedUtc = $completedUtc.ToString("o")
        durationSeconds = [Math]::Round(($completedUtc - $startedUtc).TotalSeconds, 2)
        repository = $repoRoot
        branch = $branch
        commit = $commit
        configuration = $Configuration
        overall = if ($overallPassed) { "PASS" } else { "FAIL" }
        failure = $failureMessage
        startup = $startupResults
        build = $buildResult
        tests = [pscustomobject]@{
            execution = $testResult
            counters = $trxCounters
            trx = $trxPath
        }
        teardown = $teardownResults
        containersKept = [bool]$KeepContainers
    }

    $report | ConvertTo-Json -Depth 8 | Set-Content -Path $jsonPath

    if ($overallPassed) { exit 0 }
    exit 1
}
