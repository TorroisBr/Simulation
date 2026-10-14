[CmdletBinding()]
param(
    [string]$ProjectRoot = (Join-Path $PSScriptRoot '..\..'),
    [string]$EvidenceRoot,
    [switch]$SelfTest
)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'CoreValidation.psm1') -Force

if ($SelfTest) {
    & (Join-Path $PSScriptRoot 'tests\Invoke-CoreValidationTests.ps1')
    exit $LASTEXITCODE
}

$root = (Resolve-Path -LiteralPath $ProjectRoot).Path
if (-not $EvidenceRoot) {
    $EvidenceRoot = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Simulation\CoreValidation'
}
Import-Module (Join-Path $PSScriptRoot '..\UnityValidation\UnityValidationEvidence.psm1') -Force
$runId = [Guid]::NewGuid().ToString('N')
$runDirectory = Join-Path $EvidenceRoot $runId
if (Test-Path -LiteralPath $runDirectory) { throw 'Unique CoreValidation run directory already exists; refusing to alter its ACL or contents.' }
$null = Set-ProtectedEvidenceDirectory -Path $runDirectory
$null = Test-ProtectedEvidenceDirectory -Path $runDirectory
$testProject = 'tests/Simulation.Core.Tests/Simulation.Core.Tests.csproj'
$demoProject = 'tools/Simulation.Core.CalendarDemo/Simulation.Core.CalendarDemo.csproj'
$projectAssets = @(
    'src/Simulation.Core/obj/project.assets.json',
    'tests/Simulation.Core.Tests/obj/project.assets.json',
    'tools/Simulation.Core.CalendarDemo/obj/project.assets.json'
)
$records = [System.Collections.Generic.List[object]]::new()

function Invoke-Dotnet {
    param([string]$Name, [string[]]$Arguments)
    $logPath = Join-Path $runDirectory ($Name + '.log')
    $watch = [Diagnostics.Stopwatch]::StartNew()
    # Keep CLI headings stable across host locales. Test cases still exercise
    # their declared en-US and pt-BR cultures inside the test process.
    $previousUiLanguage = [Environment]::GetEnvironmentVariable('DOTNET_CLI_UI_LANGUAGE', 'Process')
    try {
        [Environment]::SetEnvironmentVariable('DOTNET_CLI_UI_LANGUAGE', 'en-US', 'Process')
        $output = @(& dotnet @Arguments 2>&1 | ForEach-Object { $_.ToString() })
        $exitCode = $LASTEXITCODE
    } finally {
        [Environment]::SetEnvironmentVariable('DOTNET_CLI_UI_LANGUAGE', $previousUiLanguage, 'Process')
    }
    $watch.Stop()
    [IO.File]::WriteAllLines($logPath, [string[]]$output, [Text.UTF8Encoding]::new($false))
    $record = [pscustomobject]@{
        name = $Name
        arguments = @($Arguments)
        exitCode = $exitCode
        elapsedMilliseconds = [Math]::Round($watch.Elapsed.TotalMilliseconds, 3)
        logPath = $logPath
        output = $output
    }
    $records.Add($record)
    return $record
}

function Assert-CommandPassed([object]$Record) {
    if ($Record.exitCode -ne 0) { throw "Command '$($Record.name)' failed with exit $($Record.exitCode). No retry is performed." }
}

function Get-GeneratedInputSnapshot {
    param([string[]]$Paths)
    $items = foreach ($relative in $Paths) {
        $full = Join-Path $root ($relative -replace '/', [IO.Path]::DirectorySeparatorChar)
        if (-not (Test-Path -LiteralPath $full -PathType Leaf)) { throw "Expected generated build input is missing: $relative" }
        $file = Get-Item -LiteralPath $full
        [pscustomobject]@{ path = $relative; bytes = [long]$file.Length; sha256 = (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.ToLowerInvariant() }
    }
    return @($items)
}

try {
    $beforeInputs = Get-CoreInputSnapshot -ProjectRoot $root
    $sdkRecord = Invoke-Dotnet -Name 'sdk-version' -Arguments @('--version')
    Assert-CommandPassed $sdkRecord
    $sdkVersion = ($sdkRecord.output | Select-Object -Last 1).Trim()
    if ($sdkVersion -cne '9.0.315') { throw "Pinned SDK mismatch: expected 9.0.315, found $sdkVersion." }
    $info = Invoke-Dotnet -Name 'dotnet-info' -Arguments @('--info')
    Assert-CommandPassed $info
    $runtime = Invoke-Dotnet -Name 'dotnet-runtimes' -Arguments @('--list-runtimes')
    Assert-CommandPassed $runtime

    $restoreTests = Invoke-Dotnet -Name 'restore-tests-locked' -Arguments @('restore', $testProject, '--locked-mode')
    Assert-CommandPassed $restoreTests
    $restoreDemo = Invoke-Dotnet -Name 'restore-demo-locked' -Arguments @('restore', $demoProject, '--locked-mode')
    Assert-CommandPassed $restoreDemo
    $generatedBefore = Get-GeneratedInputSnapshot -Paths $projectAssets

    $coldBuild = Invoke-Dotnet -Name 'cold-build' -Arguments @('build', $testProject, '--no-restore', '--configuration', 'Release')
    Assert-CommandPassed $coldBuild

    $list = Invoke-Dotnet -Name 'list-tests' -Arguments @('test', $testProject, '--no-build', '--no-restore', '--configuration', 'Release', '--list-tests')
    Assert-CommandPassed $list
    $expectedTests = [System.Collections.Generic.List[string]]::new()
    $insideList = $false
    foreach ($line in $list.output) {
        if ($line -match '^\s*The following Tests are available:\s*$') { $insideList = $true; continue }
        if ($insideList -and -not [string]::IsNullOrWhiteSpace($line)) {
            $name = $line.Trim()
            if ($name -notmatch '^(Test run|VSTest|Starting test execution|No test matches)') { $expectedTests.Add($name) }
        }
    }
    if ($expectedTests.Count -eq 0) { throw 'Test discovery returned no expected cases.' }

    $coldTest = Invoke-Dotnet -Name 'cold-test' -Arguments @('test', $testProject, '--no-restore', '--configuration', 'Release', '--logger', 'trx;LogFileName=CoreTests-cold.trx', '--results-directory', $runDirectory)
    $coldTrxPath = Join-Path $runDirectory 'CoreTests-cold.trx'
    $coldAssessment = Get-CoreTrxAssessment -Path $coldTrxPath -ExpectedTests $expectedTests.ToArray()
    if ($coldTest.exitCode -ne 0 -or -not $coldAssessment.gatePassed) { throw 'Cold test gate did not pass. Valid test failures are not retried.' }

    $warmTest = Invoke-Dotnet -Name 'warm-test' -Arguments @('test', $testProject, '--no-build', '--no-restore', '--configuration', 'Release', '--logger', 'trx;LogFileName=CoreTests-warm.trx', '--results-directory', $runDirectory)
    $warmTrxPath = Join-Path $runDirectory 'CoreTests-warm.trx'
    $warmAssessment = Get-CoreTrxAssessment -Path $warmTrxPath -ExpectedTests $expectedTests.ToArray()
    if ($warmTest.exitCode -ne 0 -or -not $warmAssessment.gatePassed) { throw 'Warm test measurement/gate did not pass.' }

    $coldDemo = Invoke-Dotnet -Name 'cold-demo' -Arguments @('run', '--project', $demoProject, '--no-restore', '--configuration', 'Release')
    Assert-CommandPassed $coldDemo
    $warmDemo = Invoke-Dotnet -Name 'warm-demo' -Arguments @('run', '--project', $demoProject, '--no-build', '--no-restore', '--configuration', 'Release')
    Assert-CommandPassed $warmDemo
    $expectedDemo = @(
        'Semantic identity: simulation-calendar',
        'Semantic version: 3/1/35/custom:10,20,5',
        'Absolute day 0 => Year 0 / Month 1 / Day 1',
        'Absolute day 10 => Year 0 / Month 2 / Day 1',
        'Absolute day 30 => Year 0 / Month 3 / Day 1',
        'Absolute day 35 => Year 1 / Month 1 / Day 1',
        'Completed age before birthday: 0',
        'Completed age on birthday: 1',
        'Negative absolute day: rejected'
    )
    foreach ($line in $expectedDemo) {
        if (-not ($coldDemo.output -ccontains $line) -or -not ($warmDemo.output -ccontains $line)) { throw "CalendarDemo result mismatch: $line" }
    }

    $generatedAfter = Get-GeneratedInputSnapshot -Paths $projectAssets
    $generatedComparison = Compare-CoreInputSnapshots -Before $generatedBefore -After $generatedAfter
    if (-not $generatedComparison.stable) { throw "Generated dependency inputs changed during validation: $($generatedComparison.changedPaths -join ', ')" }
    $afterInputs = Get-CoreInputSnapshot -ProjectRoot $root
    $inputComparison = Compare-CoreInputSnapshots -Before $beforeInputs -After $afterInputs
    if (-not $inputComparison.stable) { throw "Validation inputs changed during execution: $($inputComparison.changedPaths -join ', ')" }

    $libraryPath = Join-Path $root 'Library'
    $libraryAbsent = -not (Test-Path -LiteralPath $libraryPath)
    $storage = @()
    foreach ($relative in @('src/Simulation.Core/bin','src/Simulation.Core/obj','tests/Simulation.Core.Tests/bin','tests/Simulation.Core.Tests/obj','tools/Simulation.Core.CalendarDemo/bin','tools/Simulation.Core.CalendarDemo/obj')) {
        $full = Join-Path $root ($relative -replace '/', [IO.Path]::DirectorySeparatorChar)
        if (Test-Path -LiteralPath $full) {
            $bytes = (Get-ChildItem -LiteralPath $full -File -Recurse | Measure-Object -Property Length -Sum).Sum
            $storage += [pscustomobject]@{ path = $relative; bytes = [long]$bytes }
        }
    }
    $trxArtifacts = foreach ($path in @($coldTrxPath,$warmTrxPath)) {
        $file = Get-Item -LiteralPath $path
        [pscustomobject]@{ file = [IO.Path]::GetFileName($path); bytes = [long]$file.Length; sha256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() }
    }
    $logArtifacts = foreach ($record in $records | Where-Object { $_.logPath -and (Test-Path -LiteralPath $_.logPath) }) {
        $file = Get-Item -LiteralPath $record.logPath
        [pscustomobject]@{ file = [IO.Path]::GetFileName($record.logPath); bytes = [long]$file.Length; sha256 = (Get-FileHash -LiteralPath $record.logPath -Algorithm SHA256).Hash.ToLowerInvariant() }
    }
    $manifest = [ordered]@{
        schema = 'simulation-core-validation/v1'
        runId = $runId
        status = 'Passed'
        source = [ordered]@{ commit = (& git -C $root rev-parse HEAD).Trim(); committedTree = (& git -C $root rev-parse 'HEAD^{tree}').Trim(); workingTreeInputDigest = $inputComparison.afterDigest; worktreeChanges = @(& git -C $root status --porcelain=v1) }
        toolchain = [ordered]@{ sdk = $sdkVersion; dotnetInfoLog = 'dotnet-info.log'; runtimes = @($runtime.output); os = [Environment]::OSVersion.VersionString; architecture = [Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString(); processArchitecture = [Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString(); culturesCoveredBySharedCases = @('en-US','pt-BR') }
        inputs = [ordered]@{ digest = $inputComparison.afterDigest; stable = $inputComparison.stable; files = $afterInputs; generatedDependencies = $generatedAfter; generatedDependenciesStable = $generatedComparison.stable }
        testDiscovery = [ordered]@{ count = $expectedTests.Count; cases = $expectedTests.ToArray() }
        gates = [ordered]@{
            coldBuild = [ordered]@{ exitCode = $coldBuild.exitCode; elapsedMilliseconds = $coldBuild.elapsedMilliseconds }
            coldTests = [ordered]@{ exitCode = $coldTest.exitCode; elapsedMilliseconds = $coldTest.elapsedMilliseconds; resultFile = 'CoreTests-cold.trx'; runId = $coldAssessment.runId; runName = $coldAssessment.runName; summaryOutcome = $coldAssessment.summaryOutcome; counters = $coldAssessment.counters; cases = $coldAssessment.results }
            warmTests = [ordered]@{ exitCode = $warmTest.exitCode; elapsedMilliseconds = $warmTest.elapsedMilliseconds; resultFile = 'CoreTests-warm.trx'; runId = $warmAssessment.runId; runName = $warmAssessment.runName; summaryOutcome = $warmAssessment.summaryOutcome; counters = $warmAssessment.counters; cases = $warmAssessment.results }
            coldDemo = [ordered]@{ exitCode = $coldDemo.exitCode; elapsedMilliseconds = $coldDemo.elapsedMilliseconds; output = $coldDemo.output }
            warmDemo = [ordered]@{ exitCode = $warmDemo.exitCode; elapsedMilliseconds = $warmDemo.elapsedMilliseconds; output = $warmDemo.output }
        }
        storage = [ordered]@{ worktreeLibraryAbsent = $libraryAbsent; projectBinObjBytes = $storage; resultBytes = [long]((Get-ChildItem -LiteralPath $runDirectory -File | Measure-Object -Property Length -Sum).Sum) }
        artifacts = [ordered]@{ reports = @($trxArtifacts); logs = @($logArtifacts) }
        publication = 'Raw TRX/logs remain private until their actual contents pass privacy review; no normalization is performed by this runner.'
    }
    $manifestPath = Join-Path $runDirectory 'manifest.json'
    $json = ConvertTo-Json -InputObject $manifest -Depth 16
    [IO.File]::WriteAllText($manifestPath, $json, [Text.UTF8Encoding]::new($false))
    $null = Test-ProtectedEvidenceDirectory -Path $runDirectory
    foreach ($evidenceFile in Get-ChildItem -LiteralPath $runDirectory -File -Force -Recurse) {
        $null = Assert-ProtectedEvidenceFile -Path $evidenceFile.FullName -ProtectedRoot $runDirectory
    }
    Write-Output "CORE validation PASS: run=$runId tests=$($coldAssessment.resultCount) coldTestMs=$($coldTest.elapsedMilliseconds) warmTestMs=$($warmTest.elapsedMilliseconds) libraryAbsent=$libraryAbsent"
    Write-Output "Private evidence manifest: <PRIVATE_EVIDENCE>/$runId/manifest.json"
    if (-not $libraryAbsent) { Write-Warning 'This checkout contains a Unity Library; run the .NET-only confirmation in a separate Library-free worktree before claiming Library independence.' }
} catch {
    $failure = [ordered]@{ runId = $runId; status = 'Failed'; error = $_.Exception.Message; commands = @($records | ForEach-Object { [pscustomobject]@{ name = $_.name; exitCode = $_.exitCode; elapsedMilliseconds = $_.elapsedMilliseconds; logFile = [IO.Path]::GetFileName($_.logPath) } }) }
    $failurePath = Join-Path $runDirectory 'failure.json'
    [IO.File]::WriteAllText($failurePath, (ConvertTo-Json -InputObject $failure -Depth 8), [Text.UTF8Encoding]::new($false))
    $null = Test-ProtectedEvidenceDirectory -Path $runDirectory
    foreach ($evidenceFile in Get-ChildItem -LiteralPath $runDirectory -File -Force -Recurse) {
        $null = Assert-ProtectedEvidenceFile -Path $evidenceFile.FullName -ProtectedRoot $runDirectory
    }
    Write-Error "CORE validation failed; original logs/reports remain at private run $runId. $($_.Exception.Message)"
    exit 1
}
