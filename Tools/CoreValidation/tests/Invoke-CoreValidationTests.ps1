[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot '..\CoreValidation.psm1') -Force
Import-Module (Join-Path $PSScriptRoot '..\..\UnityValidation\UnityValidationEvidence.psm1') -Force

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Assert-Rejected([scriptblock]$Action, [string]$Message) {
    $rejected = $false
    try { & $Action } catch { $rejected = $true }
    if (-not $rejected) { throw $Message }
}

function New-SyntheticTrx {
    param([string[]]$Names, [string[]]$Outcomes, [int]$CounterAdjustment = 0, [string]$ResultTestIdOverride = '', [string]$RunId = 'synthetic-run', [string]$SummaryOutcome = 'Completed')
    $definitions = [System.Text.StringBuilder]::new()
    $entries = [System.Text.StringBuilder]::new()
    $results = [System.Text.StringBuilder]::new()
    $passed = 0; $failed = 0; $notExecuted = 0; $executed = 0
    for ($i = 0; $i -lt $Names.Count; $i++) {
        $testId = "t$i"; $executionId = "e$i"; $name = [Security.SecurityElement]::Escape($Names[$i])
        [void]$definitions.Append("<UnitTest id=`"$testId`" name=`"$name`"><TestMethod name=`"$name`" className=`"Synthetic.Tests`" /></UnitTest>")
        [void]$entries.Append("<TestEntry testId=`"$testId`" executionId=`"$executionId`" />")
        $resultTestId = if ($ResultTestIdOverride) { $ResultTestIdOverride } else { $testId }
        $outcome = $Outcomes[$i]
        if ($outcome -eq 'Passed') { $passed++; $executed++ }
        elseif ($outcome -eq 'NotExecuted') { $notExecuted++ }
        else { $failed++; $executed++ }
        [void]$results.Append("<UnitTestResult testId=`"$resultTestId`" executionId=`"$executionId`" testName=`"$name`" outcome=`"$outcome`" duration=`"00:00:00.001`"><Output><StdOut>synthetic-output</StdOut></Output></UnitTestResult>")
    }
    $total = $Names.Count + $CounterAdjustment
    return "<TestRun id=`"$RunId`" name=`"synthetic`"><TestDefinitions>$definitions</TestDefinitions><TestEntries>$entries</TestEntries><Results>$results</Results><ResultSummary outcome=`"$SummaryOutcome`"><Counters total=`"$total`" executed=`"$executed`" passed=`"$passed`" failed=`"$failed`" error=`"0`" timeout=`"0`" aborted=`"0`" inconclusive=`"0`" notExecuted=`"$notExecuted`" /></ResultSummary></TestRun>"
}

function Write-SyntheticTrx([string]$Xml) {
    $path = Join-Path ([IO.Path]::GetTempPath()) ("core-validation-test-" + [Guid]::NewGuid().ToString('N') + '.trx')
    [IO.File]::WriteAllText($path, $Xml, [Text.UTF8Encoding]::new($false))
    return $path
}

$passPath = Write-SyntheticTrx (New-SyntheticTrx -Names @('Parameterized case (int: 1)', 'Parameterized case (string: "1")') -Outcomes @('Passed','Passed'))
try {
    $pass = Get-CoreTrxAssessment -Path $passPath -ExpectedTests @('Parameterized case (int: 1)', 'Parameterized case (string: "1")')
    Assert-True $pass.gatePassed 'Populated parameterized passing TRX should pass.'
    Assert-True ($pass.runId -ceq 'synthetic-run') 'TRX run identity must be retained.'
    Assert-True ($pass.summaryOutcome -ceq 'Completed') 'TRX summary outcome must be retained.'
    Assert-True ($pass.results.Count -eq 2) 'Both parameterized rows must be retained.'
    Assert-Rejected { Get-CoreTrxAssessment -Path $passPath -ExpectedTests @() } 'Empty discovery must be rejected.'
} finally { Remove-Item -LiteralPath $passPath -Force }

$failedPath = Write-SyntheticTrx (New-SyntheticTrx -Names @('Failure case') -Outcomes @('Failed'))
try {
    $failed = Get-CoreTrxAssessment -Path $failedPath -ExpectedTests @('Failure case')
    Assert-True (-not $failed.gatePassed) 'A valid failed TRX must remain failed.'
} finally { Remove-Item -LiteralPath $failedPath -Force }

$skippedPath = Write-SyntheticTrx (New-SyntheticTrx -Names @('Skipped case') -Outcomes @('NotExecuted'))
try {
    $skipped = Get-CoreTrxAssessment -Path $skippedPath -ExpectedTests @('Skipped case')
    Assert-True (-not $skipped.gatePassed) 'A valid not-executed TRX must not pass.'
} finally { Remove-Item -LiteralPath $skippedPath -Force }

Assert-Rejected { Get-CoreTrxAssessment -Path (Join-Path $env:TEMP 'missing-core-validation.trx') -ExpectedTests @('Missing') } 'Missing report must be rejected.'
$malformedPath = Write-SyntheticTrx '<TestRun'
try { Assert-Rejected { Get-CoreTrxAssessment -Path $malformedPath -ExpectedTests @('Malformed') } 'Malformed report must be rejected.' }
finally { Remove-Item -LiteralPath $malformedPath -Force }

$badIdPath = Write-SyntheticTrx (New-SyntheticTrx -Names @('Case') -Outcomes @('Passed') -ResultTestIdOverride 'unknown')
try { Assert-Rejected { Get-CoreTrxAssessment -Path $badIdPath -ExpectedTests @('Case') } 'A result with an unbound test id must be rejected.' }
finally { Remove-Item -LiteralPath $badIdPath -Force }

$badCounterPath = Write-SyntheticTrx (New-SyntheticTrx -Names @('Case') -Outcomes @('Passed') -CounterAdjustment 1)
try { Assert-Rejected { Get-CoreTrxAssessment -Path $badCounterPath -ExpectedTests @('Case') } 'Mismatched TRX counters must be rejected.' }
finally { Remove-Item -LiteralPath $badCounterPath -Force }

$missingRunIdPath = Write-SyntheticTrx (New-SyntheticTrx -Names @('Case') -Outcomes @('Passed') -RunId '')
try { Assert-Rejected { Get-CoreTrxAssessment -Path $missingRunIdPath -ExpectedTests @('Case') } 'Missing TRX run identity must be rejected.' }
finally { Remove-Item -LiteralPath $missingRunIdPath -Force }

$missingSummaryOutcomePath = Write-SyntheticTrx (New-SyntheticTrx -Names @('Case') -Outcomes @('Passed') -SummaryOutcome '')
try { Assert-Rejected { Get-CoreTrxAssessment -Path $missingSummaryOutcomePath -ExpectedTests @('Case') } 'Missing TRX summary outcome must be rejected.' }
finally { Remove-Item -LiteralPath $missingSummaryOutcomePath -Force }

$contradictorySummaryPath = Write-SyntheticTrx (New-SyntheticTrx -Names @('Case') -Outcomes @('Passed') -SummaryOutcome 'Failed')
try { Assert-Rejected { Get-CoreTrxAssessment -Path $contradictorySummaryPath -ExpectedTests @('Case') } 'A non-completed TRX summary must be rejected.' }
finally { Remove-Item -LiteralPath $contradictorySummaryPath -Force }

$before = @([pscustomobject]@{ path = 'input.cs'; bytes = 1; sha256 = 'a' })
$afterSame = @([pscustomobject]@{ path = 'input.cs'; bytes = 1; sha256 = 'a' })
$afterChanged = @([pscustomobject]@{ path = 'input.cs'; bytes = 1; sha256 = 'b' })
Assert-True (Compare-CoreInputSnapshots -Before $before -After $afterSame).stable 'Equal input snapshots should be stable.'
Assert-True (-not (Compare-CoreInputSnapshots -Before $before -After $afterChanged).stable) 'Changed input snapshots must be rejected as unstable.'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$compileInputs = Get-CoreProjectCompileInputs -ProjectRoot $repositoryRoot -ProjectPaths @('tests/Simulation.Core.Tests/Simulation.Core.Tests.csproj')
foreach ($requiredInput in @(
    'Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs',
    'tests/Simulation.Core.Tests/DailyV1ExecutionBoundaryStateTests.cs',
    'src/Simulation.DailyV1.DotNetHostIdentity/DailyV1DotNetHostEnvironmentIdentityFactory.cs',
    'src/Simulation.Core/Simulation.Core.csproj',
    'src/Simulation.DailyV1.DotNetHostIdentity/Simulation.DailyV1.DotNetHostIdentity.csproj'
)) {
    Assert-True ($compileInputs -ccontains $requiredInput) "Evaluated compile graph must include $requiredInput."
}
Assert-True (@($compileInputs | Where-Object { [IO.Path]::IsPathRooted($_) }).Count -eq 0) 'Compile input identities must be repository-relative.'

$reparseTestRoot = Join-Path ([IO.Path]::GetTempPath()) ('core-validation-reparse-' + [Guid]::NewGuid().ToString('N'))
$repositoryRootUnderTest = Join-Path $reparseTestRoot 'repository'
$externalSourceRoot = Join-Path $reparseTestRoot 'external'
New-Item -ItemType Directory -Path $repositoryRootUnderTest -Force | Out-Null
New-Item -ItemType Directory -Path $externalSourceRoot -Force | Out-Null
[IO.File]::WriteAllText((Join-Path $externalSourceRoot 'ExternalCompileInput.cs'), 'class ExternalCompileInput {}', [Text.UTF8Encoding]::new($false))
try {
    $generatedJunctionPath = Join-Path $repositoryRootUnderTest 'bin'
    New-Item -ItemType Junction -Path $generatedJunctionPath -Target $externalSourceRoot -ErrorAction Stop | Out-Null
    Assert-Rejected {
        Get-CoreRepositoryRelativeIdentity -RepositoryRoot $repositoryRootUnderTest -FullPath (Join-Path $generatedJunctionPath 'ExternalCompileInput.cs')
    } 'A compile input reached through an in-repository junction must be rejected.'

    $syntheticProject = Join-Path $repositoryRootUnderTest 'Synthetic.csproj'
    $syntheticProjectXml = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="bin\ExternalCompileInput.cs" />
  </ItemGroup>
</Project>
'@
    [IO.File]::WriteAllText($syntheticProject, $syntheticProjectXml, [Text.UTF8Encoding]::new($false))
    Assert-Rejected {
        Get-CoreProjectCompileInputs -ProjectRoot $repositoryRootUnderTest -ProjectPaths @('Synthetic.csproj')
    } 'Generated-output exclusion must not allow an in-repository bin junction to bypass compile-input containment.'
} finally {
    if (Test-Path -LiteralPath (Join-Path $repositoryRootUnderTest 'bin')) { [IO.Directory]::Delete((Join-Path $repositoryRootUnderTest 'bin')) }
    if (Test-Path -LiteralPath $reparseTestRoot) { Remove-Item -LiteralPath $reparseTestRoot -Recurse -Force }
}

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path
$coreInputs = @(Get-CoreInputSnapshot -ProjectRoot $repositoryRoot | ForEach-Object { [string]$_.path })
foreach ($requiredInput in @(
    'Packages/com.simulation.core/Runtime/Persistence/P18DCommercialSharingCodec.cs',
    'Packages/com.simulation.core/Runtime/Numerics/UnityMathfCompatibility.cs',
    'Assets/_Project/Tests/EditMode/Editor/Core/CORE_F_CompatibilityParityTests.cs',
    'docs/validation/CORE_F_SharedRuntimeSource/JSON_PAYLOAD_ORACLE.csv',
    'docs/validation/CORE_F_SharedRuntimeSource/NUMERIC_ORACLE_VECTORS.csv',
    'docs/validation/CORE_F_SharedRuntimeSource/UNITY_ORACLE_CAPTURE_RESULTS_d8b4c40e.csv'
)) {
    Assert-True ($coreInputs -ccontains $requiredInput) "Core validation snapshot omits compatibility input $requiredInput."
}

$aclTestDirectory = Join-Path ([IO.Path]::GetTempPath()) ('core-validation-acl-' + [Guid]::NewGuid().ToString('N'))
try {
    $null = Set-ProtectedEvidenceDirectory -Path $aclTestDirectory
    $null = Test-ProtectedEvidenceDirectory -Path $aclTestDirectory
    $aclTestFile = Join-Path $aclTestDirectory 'private-evidence.txt'
    [IO.File]::WriteAllText($aclTestFile, 'protected test evidence', [Text.UTF8Encoding]::new($false))
    $null = Assert-ProtectedEvidenceFile -Path $aclTestFile -ProtectedRoot $aclTestDirectory

    $traceDirectory = Join-Path $aclTestDirectory 'runtime-traces'
    $null = Set-ProtectedEvidenceDirectory -Path $traceDirectory
    $settingsPath = Join-Path $aclTestDirectory 'testhost.runsettings'
    $traceFilePath = Join-Path $traceDirectory 'testhost-runtime-selection.log'
    $settingsRecord = New-CoreHostTraceRunSettings -Path $settingsPath -TraceFilePath $traceFilePath -SdkVersion '9.0.315'
    $null = Assert-ProtectedEvidenceFile -Path $settingsPath -ProtectedRoot $aclTestDirectory
    [xml]$settingsXml = [IO.File]::ReadAllText($settingsPath)
    $settingsEnvironment = $settingsXml.RunSettings.RunConfiguration.EnvironmentVariables
    Assert-True ($settingsRecord.tracePrefix -ceq 'COREHOST') 'SDK 9 runsettings must use the supported COREHOST trace prefix.'
    Assert-True ($settingsEnvironment.COREHOST_TRACE -ceq '1') 'Runsettings must enable tracing inside the testhost process.'
    Assert-True ([IO.Path]::IsPathFullyQualified($settingsEnvironment.COREHOST_TRACEFILE)) 'Runsettings must send the trace to an absolute file path.'
    Assert-True ($settingsEnvironment.COREHOST_TRACEFILE -ceq [IO.Path]::GetFullPath($traceFilePath)) 'Runsettings must bind the trace to the protected run directory.'
    $settings10 = New-CoreHostTraceRunSettings -Path (Join-Path $aclTestDirectory 'testhost-net10.runsettings') -TraceFilePath $traceFilePath -SdkVersion '10.0.100'
    Assert-True ($settings10.tracePrefix -ceq 'DOTNET_HOST') 'SDK 10 runsettings must use the renamed DOTNET_HOST trace prefix.'
    $testAssemblyPath = Join-Path $aclTestDirectory 'output/Simulation.Core.Tests.dll'
    $runtimeConfigPath = [regex]::Replace($testAssemblyPath, '(?i)\.dll$', '.runtimeconfig.json')
    $runtimeDevConfigPath = [regex]::Replace($testAssemblyPath, '(?i)\.dll$', '.runtimeconfig.dev.json')
    $tracePath = Join-Path $traceDirectory 'dotnet.12345.log'
    $traceLines = @(
        'Tracing enabled @ Wed Oct 14 16:33:24 2026 GMT',
        "Runtime config is cfg=$runtimeConfigPath dev=$runtimeDevConfigPath",
        'Chose FX version [C:\Program Files\dotnet\shared\Microsoft.NETCore.App\8.0.28]'
    )
    [IO.File]::WriteAllLines($tracePath, [string[]]$traceLines, [Text.UTF8Encoding]::new($false))
    $null = Assert-ProtectedEvidenceFile -Path $tracePath -ProtectedRoot $traceDirectory
    $selection = Get-CoreRuntimeSelectionTrace -TraceDirectory $traceDirectory -TestAssemblyPath $testAssemblyPath
    Assert-True ($selection.selectedClr -ceq '8.0.28') 'The selected CLR must come from the bound testhost runtime-config block.'
    Assert-True ($selection.fileName -ceq 'dotnet.12345.log') 'The trace must be selected by exact test-assembly runtime-config identity, independent of process filename.'
    $traceBytes=[IO.File]::ReadAllBytes($tracePath)
    Assert-True ($selection.bytes -eq $traceBytes.LongLength) 'Recorded trace length must match the parsed byte snapshot.'
    $traceSha=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($traceBytes)).ToLowerInvariant()
    Assert-True ($selection.sha256 -ceq $traceSha) 'Recorded trace hash must match the same parsed byte snapshot.'

    $unrelatedTrace = Join-Path $traceDirectory 'MSBuild.12344.log'
    $unrelatedLines = @(
        'Tracing enabled @ Wed Oct 14 16:33:24 2026 GMT',
        'Runtime config is cfg=C:\Program Files\dotnet\sdk\9.0.315\MSBuild.runtimeconfig.json dev=C:\Program Files\dotnet\sdk\9.0.315\MSBuild.runtimeconfig.dev.json',
        'Chose FX version [C:\Program Files\dotnet\shared\Microsoft.NETCore.App\9.0.20]'
    )
    [IO.File]::WriteAllLines($unrelatedTrace, [string[]]$unrelatedLines, [Text.UTF8Encoding]::new($false))
    $selectionWithUnrelated = Get-CoreRuntimeSelectionTrace -TraceDirectory $traceDirectory -TestAssemblyPath $testAssemblyPath
    Assert-True ($selectionWithUnrelated.fileName -ceq 'dotnet.12345.log') 'Unrelated SDK host traces must not change the exact test-assembly binding.'
    Remove-Item -LiteralPath $unrelatedTrace -Force

    $duplicateTrace = Join-Path $traceDirectory 'dotnet.12346.log'
    [IO.File]::WriteAllText($duplicateTrace, [string]::Join([Environment]::NewLine, $traceLines), [Text.UTF8Encoding]::new($false))
    Assert-Rejected { Get-CoreRuntimeSelectionTrace -TraceDirectory $traceDirectory -TestAssemblyPath $testAssemblyPath } 'Multiple process traces bound to the test assembly must be rejected as ambiguous.'
    Remove-Item -LiteralPath $duplicateTrace -Force

    $mismatchedConfig = @($traceLines)
    $mismatchedConfig[1] = $mismatchedConfig[1].Replace('Simulation.Core.Tests.runtimeconfig.json', 'Other.Tests.runtimeconfig.json')
    [IO.File]::WriteAllLines($tracePath, [string[]]$mismatchedConfig, [Text.UTF8Encoding]::new($false))
    Assert-Rejected { Get-CoreRuntimeSelectionTrace -TraceDirectory $traceDirectory -TestAssemblyPath $testAssemblyPath } 'A trace for a different assembly runtime config must be rejected.'
}
finally {
    if (Test-Path -LiteralPath $aclTestDirectory -PathType Container) {
        $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
        $fullAclTestDirectory = [IO.Path]::GetFullPath($aclTestDirectory)
        if (-not $fullAclTestDirectory.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'ACL self-test cleanup escaped the unique temporary root.' }
        if ((Get-Item -LiteralPath $fullAclTestDirectory -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'ACL self-test cleanup refused a reparse-point directory.' }
        Remove-Item -LiteralPath $fullAclTestDirectory -Recurse -Force
    }
}

Write-Output 'Core validation self-tests PASS: populated/parameterized, failed, not-executed, empty discovery, malformed/missing report, run identity/summary outcome, ID/count integrity, input stability, evaluated transitive compile-source closure, reparse-point rejection, SDK-specific testhost trace settings, exact assembly runtime selection, and protected evidence ACL.'
