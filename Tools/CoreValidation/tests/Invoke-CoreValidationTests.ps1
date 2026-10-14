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

$aclTestDirectory = Join-Path ([IO.Path]::GetTempPath()) ('core-validation-acl-' + [Guid]::NewGuid().ToString('N'))
try {
    $null = Set-ProtectedEvidenceDirectory -Path $aclTestDirectory
    $null = Test-ProtectedEvidenceDirectory -Path $aclTestDirectory
    $aclTestFile = Join-Path $aclTestDirectory 'private-evidence.txt'
    [IO.File]::WriteAllText($aclTestFile, 'protected test evidence', [Text.UTF8Encoding]::new($false))
    $null = Assert-ProtectedEvidenceFile -Path $aclTestFile -ProtectedRoot $aclTestDirectory
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

Write-Output 'Core validation self-tests PASS: populated/parameterized, failed, not-executed, empty discovery, malformed/missing report, run identity/summary outcome, ID/count integrity, input stability, and protected evidence ACL.'
