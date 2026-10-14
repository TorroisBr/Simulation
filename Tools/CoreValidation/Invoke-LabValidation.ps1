[CmdletBinding()]
param(
    [string]$ProjectRoot = (Join-Path $PSScriptRoot '..\..'),
    [string]$EvidenceRoot
)

$ErrorActionPreference = 'Stop'
$coreModulePath = Join-Path $PSScriptRoot 'CoreValidation.psm1'
Import-Module $coreModulePath -Force
Import-Module (Join-Path $PSScriptRoot '..\UnityValidation\UnityValidationEvidence.psm1') -Force

$root = (Resolve-Path -LiteralPath $ProjectRoot).Path
if (-not $EvidenceRoot) {
    $EvidenceRoot = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Simulation\LabValidation'
}
$runId = [Guid]::NewGuid().ToString('N')
$runDirectory = Join-Path $EvidenceRoot $runId
if (Test-Path -LiteralPath $runDirectory) { throw 'Unique Lab validation directory already exists; refusing to alter its ACL or contents.' }
$null = Set-ProtectedEvidenceDirectory -Path $runDirectory
$null = Test-ProtectedEvidenceDirectory -Path $runDirectory

function Invoke-LabDotnet {
    param([string]$Name, [string[]]$Arguments)
    $previousUiLanguage = [Environment]::GetEnvironmentVariable('DOTNET_CLI_UI_LANGUAGE', 'Process')
    $watch = [Diagnostics.Stopwatch]::StartNew()
    try {
        [Environment]::SetEnvironmentVariable('DOTNET_CLI_UI_LANGUAGE', 'en-US', 'Process')
        $output = @(& dotnet @Arguments 2>&1 | ForEach-Object { $_.ToString() })
        $exitCode = $LASTEXITCODE
    } finally {
        [Environment]::SetEnvironmentVariable('DOTNET_CLI_UI_LANGUAGE', $previousUiLanguage, 'Process')
    }
    $watch.Stop()
    $logPath = Join-Path $runDirectory ($Name + '.log')
    [IO.File]::WriteAllLines($logPath, [string[]]$output, [Text.UTF8Encoding]::new($false))
    return [pscustomobject]@{
        name = $Name
        arguments = @($Arguments)
        exitCode = $exitCode
        elapsedMilliseconds = [Math]::Round($watch.Elapsed.TotalMilliseconds, 3)
        output = @($output)
        logPath = $logPath
    }
}

function Assert-LabCommandPassed([object]$Command) {
    if ($Command.exitCode -ne 0) { throw "Lab command '$($Command.name)' failed with exit $($Command.exitCode)." }
}

try {
    $beforeStatus = @(& git -C $root status --porcelain=v1)
    if ($LASTEXITCODE -ne 0 -or $beforeStatus.Count -ne 0) { throw 'Lab validation requires the exact committed candidate in a clean worktree.' }
    $commit = (& git -C $root rev-parse HEAD).Trim()
    $tree = (& git -C $root rev-parse 'HEAD^{tree}').Trim()
    $beforeInputs = @(Get-CoreInputSnapshot -ProjectRoot $root)
    $project = 'tools/Simulation.Lab/Simulation.Lab.csproj'
    $assetsPath = Join-Path $root 'tools/Simulation.Lab/obj/project.assets.json'

    $sdk = Invoke-LabDotnet -Name 'sdk-version' -Arguments @('--version')
    Assert-LabCommandPassed $sdk
    $sdkVersion = ($sdk.output | Select-Object -Last 1).Trim()
    if ($sdkVersion -cne '9.0.315') { throw "Pinned SDK mismatch: expected 9.0.315, found $sdkVersion." }
    $restore = Invoke-LabDotnet -Name 'restore-locked' -Arguments @('restore', $project, '--locked-mode')
    Assert-LabCommandPassed $restore
    if (-not (Test-Path -LiteralPath $assetsPath -PathType Leaf)) { throw 'Locked Lab restore did not produce project.assets.json.' }
    $assetsBefore = Get-Item -LiteralPath $assetsPath
    $assetsIdentity = [ordered]@{
        path = 'tools/Simulation.Lab/obj/project.assets.json'
        bytes = [long]$assetsBefore.Length
        sha256 = (Get-FileHash -LiteralPath $assetsPath -Algorithm SHA256).Hash.ToLowerInvariant()
    }

    $build = Invoke-LabDotnet -Name 'build' -Arguments @('build', $project, '--no-restore', '--configuration', 'Release')
    Assert-LabCommandPassed $build
    $cold = Invoke-LabDotnet -Name 'cold' -Arguments @('run', '--project', $project, '--no-build', '--no-restore', '--configuration', 'Release')
    Assert-LabCommandPassed $cold
    $warm = Invoke-LabDotnet -Name 'warm' -Arguments @('run', '--project', $project, '--no-build', '--no-restore', '--configuration', 'Release')
    Assert-LabCommandPassed $warm

    $expected = @(
        'scenario location=location.p14b-lab settlement=settlement.p14b-lab source=source.p14b-lab market=market.p14b-lab item=item.p14b-lab',
        'day=1 status=Applied produced=3 reserve=2 sourceRevision=1 marketRevision=1 stock=5 price=3.2 priceBits=404CCCCD',
        'day=2 status=Applied produced=2 reserve=0 sourceRevision=2 marketRevision=2 stock=7 price=2.2857144 priceBits=40124925',
        'day=3 status=Exhausted produced=0 reserve=0 sourceRevision=2 marketRevision=2 stock=7 price=2.2857144 priceBits=40124925'
    )
    if ((ConvertTo-Json -InputObject @($cold.output) -Compress) -cne (ConvertTo-Json -InputObject $expected -Compress)) {
        throw 'Cold Lab output differs from the fixed P14-B scenario result.'
    }
    if ((ConvertTo-Json -InputObject @($warm.output) -Compress) -cne (ConvertTo-Json -InputObject $expected -Compress)) {
        throw 'Warm Lab output differs from the fixed P14-B scenario result.'
    }

    $assetsAfter = Get-Item -LiteralPath $assetsPath
    $assetsAfterHash = (Get-FileHash -LiteralPath $assetsPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ([long]$assetsAfter.Length -ne [long]$assetsIdentity.bytes -or $assetsAfterHash -cne $assetsIdentity.sha256) {
        throw 'Generated Lab restore inputs changed after the locked restore.'
    }
    $afterInputs = @(Get-CoreInputSnapshot -ProjectRoot $root)
    $inputComparison = Compare-CoreInputSnapshots -Before $beforeInputs -After $afterInputs
    if (-not $inputComparison.stable) { throw "Source inputs changed during Lab validation: $($inputComparison.changedPaths -join ', ')" }
    $afterStatus = @(& git -C $root status --porcelain=v1)
    if ($LASTEXITCODE -ne 0 -or $afterStatus.Count -ne 0) { throw 'The candidate worktree changed during Lab validation.' }

    $commands = @($sdk, $restore, $build, $cold, $warm)
    $artifacts = foreach ($command in $commands) {
        $file = Get-Item -LiteralPath $command.logPath
        [ordered]@{
            name = $command.name
            file = [IO.Path]::GetFileName($command.logPath)
            bytes = [long]$file.Length
            sha256 = (Get-FileHash -LiteralPath $command.logPath -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    }
    $libraryAbsent = -not (Test-Path -LiteralPath (Join-Path $root 'Library'))
    $manifest = [ordered]@{
        schema = 'simulation-lab-validation/v1'
        runId = $runId
        status = 'Passed'
        source = [ordered]@{
            commit = $commit
            tree = $tree
            inputDigest = $inputComparison.afterDigest
            inputs = $afterInputs
            stable = $inputComparison.stable
            worktreeClean = $true
        }
        toolchain = [ordered]@{
            sdk = $sdkVersion
            targetFramework = 'net8.0'
            configuration = 'Release'
        }
        generatedDependency = $assetsIdentity
        libraryAbsent = $libraryAbsent
        commands = @($commands | ForEach-Object {
            [ordered]@{
                name = $_.name
                arguments = @($_.arguments)
                exitCode = [int]$_.exitCode
                elapsedMilliseconds = [double]$_.elapsedMilliseconds
                output = @($_.output)
            }
        })
        artifacts = @($artifacts)
    }
    $manifestPath = Join-Path $runDirectory 'manifest.json'
    [IO.File]::WriteAllText($manifestPath, (ConvertTo-Json -InputObject $manifest -Depth 20), [Text.UTF8Encoding]::new($false))
    $null = Test-ProtectedEvidenceDirectory -Path $runDirectory
    foreach ($file in Get-ChildItem -LiteralPath $runDirectory -File -Force -Recurse) {
        $null = Assert-ProtectedEvidenceFile -Path $file.FullName -ProtectedRoot $runDirectory
    }
    Write-Output "Simulation Lab validation PASS: run=$runId commit=$commit libraryAbsent=$libraryAbsent"
    Write-Output 'Cold and warm outputs match the four fixed scenario lines.'
    Write-Output "Private evidence manifest: <PRIVATE_EVIDENCE>/$runId/manifest.json"
} catch {
    $failure = [ordered]@{ runId = $runId; status = 'Failed'; error = $_.Exception.Message }
    $failurePath = Join-Path $runDirectory 'failure.json'
    [IO.File]::WriteAllText($failurePath, (ConvertTo-Json -InputObject $failure -Depth 6), [Text.UTF8Encoding]::new($false))
    $null = Test-ProtectedEvidenceDirectory -Path $runDirectory
    foreach ($file in Get-ChildItem -LiteralPath $runDirectory -File -Force -Recurse) {
        $null = Assert-ProtectedEvidenceFile -Path $file.FullName -ProtectedRoot $runDirectory
    }
    Write-Error "Simulation Lab validation failed; logs remain in private run $runId. $($_.Exception.Message)"
    exit 1
}
