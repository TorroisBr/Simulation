[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$modulePath = Join-Path $PSScriptRoot '..\UnityValidation.psm1'
Import-Module $modulePath -Force

$passed = 0
$failed = 0

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Assert-Equal {
    param($Expected, $Actual, [string]$Message)
    if ($Expected -ne $Actual) {
        throw "$Message Expected '$Expected', got '$Actual'."
    }
}

function Assert-Contains {
    param([string]$Text, [string]$Needle, [string]$Message)
    Assert-True ($Text.Contains($Needle)) "$Message Missing '$Needle'."
}

function Invoke-Case {
    param([string]$Name, [scriptblock]$Body)
    try {
        & $Body
        $script:passed++
        Write-Output "[PASS] $Name"
    }
    catch {
        $script:failed++
        Write-Output "[FAIL] $Name :: $($_.Exception.Message)"
    }
}

$projectPath = (Get-Location).ProviderPath
$testRoot = Join-Path $projectPath ('Temp\UnityValidationSelfTests-' + [Guid]::NewGuid().ToString('N'))
$resultsRoot = Join-Path $testRoot 'Results'
New-Item -ItemType Directory -Path $resultsRoot -Force | Out-Null

function New-TestOwnedProcess {
    $pwshPath = (Get-Command pwsh -ErrorAction Stop).Source
    return (Start-Process -FilePath $pwshPath -ArgumentList @('-NoProfile', '-Command', 'Start-Sleep -Seconds 30') -PassThru)
}

function Stop-TestOwnedProcess {
    param([System.Diagnostics.Process]$Process)
    if ($null -ne $Process) {
        try {
            if (-not $Process.HasExited) {
                Stop-Process -Id $Process.Id -Force -ErrorAction SilentlyContinue
            }
        }
        catch {
        }
    }
}

try {
    Invoke-Case 'NormalizesProjectPath' {
        $normalized = ConvertTo-UnityValidationPath -Path (Join-Path $projectPath '.')
        Assert-Equal (ConvertTo-UnityValidationPath -Path $projectPath) $normalized 'Project path normalization failed.'
    }

    Invoke-Case 'BuildsEditModeCommandWithoutQuit' {
        $command = Build-UnityValidationCommand -UnityPath 'C:\Program Files\Unity\Editor\Unity.exe' -ProjectPath $projectPath -Mode EditMode -ResultXmlPath (Join-Path $resultsRoot 'a.xml') -LogPath (Join-Path $resultsRoot 'a.log')
        Assert-Contains $command.ArgumentString '-runTests' 'EditMode command missing -runTests.'
        Assert-Contains $command.ArgumentString '-testPlatform EditMode' 'EditMode platform missing.'
        Assert-True (-not $command.ArgumentString.Contains('-quit')) 'Normal test command must not contain -quit.'
    }

    Invoke-Case 'BuildsPlayModeCommandWithoutQuit' {
        $command = Build-UnityValidationCommand -UnityPath 'C:\Program Files\Unity\Editor\Unity.exe' -ProjectPath $projectPath -Mode PlayMode -ResultXmlPath (Join-Path $resultsRoot 'b.xml') -LogPath (Join-Path $resultsRoot 'b.log')
        Assert-Contains $command.ArgumentString '-testPlatform PlayMode' 'PlayMode platform missing.'
        Assert-True (-not $command.ArgumentString.Contains('-quit')) 'Normal test command must not contain -quit.'
    }

    Invoke-Case 'PassesExplicitTestFilter' {
        $command = Build-UnityValidationCommand -UnityPath 'Unity.exe' -ProjectPath $projectPath -Mode EditMode -ResultXmlPath (Join-Path $resultsRoot 'c.xml') -LogPath (Join-Path $resultsRoot 'c.log') -TestFilter 'AdventureExpeditionAutonomyTests'
        Assert-Contains $command.ArgumentString '-testFilter AdventureExpeditionAutonomyTests' 'Test filter was not forwarded.'
    }

    Invoke-Case 'UsesUniqueResultXmlPath' {
        $one = New-UnityValidationRunPaths -ProjectPath $projectPath -Mode EditMode -ResultsDirectory $resultsRoot
        $two = New-UnityValidationRunPaths -ProjectPath $projectPath -Mode EditMode -ResultsDirectory $resultsRoot
        Assert-True ($one.XmlPath -ne $two.XmlPath) 'Result XML paths were not unique.'
    }

    Invoke-Case 'UsesUniqueLogPath' {
        $one = New-UnityValidationRunPaths -ProjectPath $projectPath -Mode EditMode -ResultsDirectory $resultsRoot
        $two = New-UnityValidationRunPaths -ProjectPath $projectPath -Mode EditMode -ResultsDirectory $resultsRoot
        Assert-True ($one.LogPath -ne $two.LogPath) 'Log paths were not unique.'
    }

    $missingXml = Join-Path $resultsRoot 'missing.xml'
    Invoke-Case 'MissingXmlIsFailure' {
        $xml = Read-UnityTestResultXml -XmlPath $missingXml
        Assert-Equal $false $xml.IsValid 'Missing XML should be invalid.'
        Assert-Equal 'NoResultXml' (Resolve-UnityValidationStatus -XmlResult $null) 'Missing XML status is not failure.'
    }

    $malformedXml = Join-Path $resultsRoot 'malformed.xml'
    Set-Content -LiteralPath $malformedXml -Value '<test-run result="Passed"><test-case>' -Encoding UTF8
    Invoke-Case 'MalformedXmlIsFailure' {
        $xml = Read-UnityTestResultXml -XmlPath $malformedXml
        Assert-Equal $false $xml.IsValid 'Malformed XML should be invalid.'
        Assert-Equal 'InvalidResultXml' (Resolve-UnityValidationStatus -XmlResult $xml) 'Malformed XML should not pass.'
    }

    $passedXml = Join-Path $resultsRoot 'passed.xml'
    Set-Content -LiteralPath $passedXml -Value '<test-run result="Passed" testcasecount="2" total="2" passed="2" failed="0" skipped="0" inconclusive="0" />' -Encoding UTF8
    Invoke-Case 'PassedXmlProducesPassedStatus' {
        $xml = Read-UnityTestResultXml -XmlPath $passedXml
        Assert-Equal 'Passed' (Resolve-UnityValidationStatus -XmlResult $xml) 'Passed XML was not accepted.'
        Assert-Equal 2 $xml.TotalTests 'Passed count total mismatch.'
    }

    $failedXml = Join-Path $resultsRoot 'failed.xml'
    Set-Content -LiteralPath $failedXml -Value '<test-run result="Failed" testcasecount="2" total="2" passed="1" failed="1" skipped="0" inconclusive="0" />' -Encoding UTF8
    Invoke-Case 'FailedXmlProducesFailedStatus' {
        $xml = Read-UnityTestResultXml -XmlPath $failedXml
        Assert-Equal 'Failed' (Resolve-UnityValidationStatus -XmlResult $xml) 'Failed XML was accepted as pass.'
        Assert-Equal 1 $xml.FailedTests 'Failed count mismatch.'
    }

    Invoke-Case 'FailedXmlDoesNotBecomeOperationalRetryByDefault' {
        Assert-Equal $false (Test-UnityOperationalRetryEligible -Status 'Failed') 'Real test failure is retry-eligible.'
    }

    Invoke-Case 'TimeoutIsFailure' {
        $result = New-UnityValidationResult -ProjectPath $projectPath -Mode EditMode -Status TimedOut -XmlPath (Join-Path $resultsRoot 'timeout.xml') -LogPath (Join-Path $resultsRoot 'timeout.log')
        Assert-Equal 'TimedOut' $result.Status 'Timeout status mismatch.'
        Assert-True ($result.Status -ne 'Passed') 'Timeout became a pass.'
    }

    Invoke-Case 'TimeoutPreservesLogPath' {
        $logPath = Join-Path $resultsRoot 'timeout-preserved.log'
        $result = New-UnityValidationResult -ProjectPath $projectPath -Mode EditMode -Status TimedOut -LogPath $logPath
        Assert-Equal $logPath $result.LogPath 'Timeout did not preserve log path.'
    }

    Invoke-Case 'ExitCodeZeroRequiresPassedGate' {
        Assert-Equal 'NoResultXml' (Resolve-UnityValidationStatus -XmlResult $null) 'Exit code zero without XML passed the gate.'
        $xml = Read-UnityTestResultXml -XmlPath $failedXml
        Assert-Equal 'Failed' (Resolve-UnityValidationStatus -XmlResult $xml) 'Failed XML passed despite a hypothetical zero exit code.'
    }

    $sameProjectProcess = [pscustomobject]@{ Name = 'Unity.exe'; ProcessId = 101; ParentProcessId = 1; CommandLine = "Unity.exe -projectPath `"$projectPath`"" }
    $otherProjectPath = Join-Path (Split-Path $projectPath -Parent) 'Simulation-other-worktree'
    $otherProjectProcess = [pscustomobject]@{ Name = 'Unity.exe'; ProcessId = 202; ParentProcessId = 1; CommandLine = "Unity.exe -projectPath `"$otherProjectPath`"" }
    Invoke-Case 'ProjectAlreadyOpenIsDetectedForSameExactPath' {
        $lock = Test-UnityProjectAlreadyOpen -ProjectPath (Join-Path $projectPath '.') -ProcessSnapshot @($sameProjectProcess)
        Assert-Equal 101 $lock.ProcessId 'Same-project Unity process was not detected.'
    }

    Invoke-Case 'DifferentWorktreeDoesNotCountAsSameProject' {
        $lock = Test-UnityProjectAlreadyOpen -ProjectPath $projectPath -ProcessSnapshot @($otherProjectProcess)
        Assert-True ($null -eq $lock) 'A different worktree was treated as the same project.'
    }

    $ownedChild = [pscustomobject]@{ Name = 'UnityPackageManager.exe'; ProcessId = 102; ParentProcessId = 101; CommandLine = 'helper' }
    $unrelatedUnity = [pscustomobject]@{ Name = 'Unity.exe'; ProcessId = 303; ParentProcessId = 1; CommandLine = "Unity.exe -projectPath `"$otherProjectPath`"" }
    Invoke-Case 'ProcessCleanupSelectsOnlyOwnedPid' {
        $plan = Get-UnityOwnedProcessCleanupPlan -RootProcessId 101 -ProcessSnapshot @($sameProjectProcess, $ownedChild, $unrelatedUnity)
        Assert-True ($plan.OwnedProcessIds -contains 101) 'Owned root PID was not selected.'
        Assert-True ($plan.OwnedProcessIds -contains 102) 'Owned child PID was not selected.'
        Assert-True (-not ($plan.OwnedProcessIds -contains 303)) 'Unrelated Unity PID was selected.'
    }

    Invoke-Case 'ProcessCleanupDoesNotSelectUnrelatedUnityProcess' {
        $plan = Get-UnityOwnedProcessCleanupPlan -RootProcessId 101 -ProcessSnapshot @($sameProjectProcess, $unrelatedUnity)
        Assert-Equal 1 @($plan.OwnedProcessIds).Count 'Unrelated Unity process expanded the cleanup tree.'
    }

    Invoke-Case 'StartedProcessIsCleanedAfterOperationalException' {
        $process = $null
        $script:exceptionCleanupCalls = 0
        try {
            $attempt = Invoke-UnityValidationAttempt -StartAction { $script:exceptionProcess = New-TestOwnedProcess; $script:exceptionProcess } -RunAction { throw 'synthetic operational exception' } -CleanupAction {
                param([System.Diagnostics.Process]$ownedProcess)
                $script:exceptionCleanupCalls++
                Stop-TestOwnedProcess -Process $ownedProcess
            }
            $process = $script:exceptionProcess
            Assert-Equal $false $attempt.Succeeded 'Operational exception was reported as success.'
            Assert-Equal 'OperationalFailure' $attempt.Status 'Started-process exception did not become OperationalFailure.'
            Assert-Equal $true $attempt.CleanupAttempted 'Started process was not marked for cleanup.'
            Assert-Equal 1 $script:exceptionCleanupCalls 'Cleanup action was not called exactly once.'
            Start-Sleep -Milliseconds 100
            Assert-True $process.HasExited 'Owned root process remained alive after operational exception.'
        }
        finally {
            Stop-TestOwnedProcess -Process $process
        }
    }

    Invoke-Case 'OperationalRetryDoesNotLeavePreviousOwnedRootRunning' {
        $previousProcess = $null
        $processes = New-Object System.Collections.Generic.List[object]
        try {
            for ($retry = 1; $retry -le 2; $retry++) {
                if ($null -ne $previousProcess) {
                    Assert-True $previousProcess.HasExited 'Retry started while the previous owned root was still alive.'
                }

                $attempt = Invoke-UnityValidationAttempt -StartAction { $script:retryProcess = New-TestOwnedProcess; $script:retryProcess } -RunAction { throw 'synthetic retryable operational exception' } -CleanupAction {
                    param([System.Diagnostics.Process]$ownedProcess)
                    Stop-TestOwnedProcess -Process $ownedProcess
                }
                $previousProcess = $script:retryProcess
                [void]$processes.Add($previousProcess)
                Assert-Equal 'OperationalFailure' $attempt.Status 'Retryable exception had the wrong status.'
                Assert-True $attempt.CleanupAttempted 'Retryable exception did not attempt cleanup.'
                Start-Sleep -Milliseconds 100
                Assert-True $previousProcess.HasExited 'Retryable attempt left its root alive.'
            }
            Assert-Equal 2 $processes.Count 'Retry test did not execute two isolated attempts.'
        }
        finally {
            foreach ($testProcess in $processes) { Stop-TestOwnedProcess -Process $testProcess }
        }
    }

    Invoke-Case 'CleanupAfterExceptionUsesOwnedProcessTreeOnly' {
        $process = $null
        $script:exceptionCleanupPlan = $null
        try {
            $attempt = Invoke-UnityValidationAttempt -StartAction { $script:treeProcess = New-TestOwnedProcess; $script:treeProcess } -RunAction { throw 'synthetic tree exception' } -CleanupAction {
                param([System.Diagnostics.Process]$ownedProcess)
                $script:exceptionCleanupPlan = Get-UnityOwnedProcessCleanupPlan -RootProcessId $ownedProcess.Id -ProcessSnapshot @(
                    [pscustomobject]@{ ProcessId = $ownedProcess.Id; ParentProcessId = 1; Name = 'Unity.exe'; CommandLine = 'owned root' },
                    [pscustomobject]@{ ProcessId = $ownedProcess.Id + 1; ParentProcessId = $ownedProcess.Id; Name = 'UnityPackageManager.exe'; CommandLine = 'owned child' },
                    [pscustomobject]@{ ProcessId = $ownedProcess.Id + 2; ParentProcessId = 1; Name = 'Unity.exe'; CommandLine = 'other worktree' }
                )
                Stop-TestOwnedProcess -Process $ownedProcess
            }
            $process = $script:treeProcess
            Assert-Equal $true $attempt.CleanupAttempted 'Exception cleanup was not attempted.'
            Assert-True ($script:exceptionCleanupPlan.OwnedProcessIds -contains $process.Id) 'Exception cleanup omitted the owned root.'
            Assert-True ($script:exceptionCleanupPlan.OwnedProcessIds -contains ($process.Id + 1)) 'Exception cleanup omitted the owned child.'
            Assert-True (-not ($script:exceptionCleanupPlan.OwnedProcessIds -contains ($process.Id + 2))) 'Exception cleanup selected an unrelated Unity process.'
        }
        finally {
            Stop-TestOwnedProcess -Process $process
        }
    }

    Invoke-Case 'CleanupAfterExceptionDoesNotSelectUnrelatedWorktree' {
        $process = $null
        $script:worktreeCleanupPlan = $null
        try {
            $attempt = Invoke-UnityValidationAttempt -StartAction { $script:worktreeProcess = New-TestOwnedProcess; $script:worktreeProcess } -RunAction { throw 'synthetic worktree exception' } -CleanupAction {
                param([System.Diagnostics.Process]$ownedProcess)
                $script:worktreeCleanupPlan = Get-UnityOwnedProcessCleanupPlan -RootProcessId $ownedProcess.Id -ProcessSnapshot @(
                    [pscustomobject]@{ ProcessId = $ownedProcess.Id; ParentProcessId = 1; Name = 'Unity.exe'; CommandLine = 'Unity.exe -projectPath Simulation-unity-validation' },
                    [pscustomobject]@{ ProcessId = $ownedProcess.Id + 3; ParentProcessId = 1; Name = 'Unity.exe'; CommandLine = 'Unity.exe -projectPath Simulation-population-foundation' }
                )
                Stop-TestOwnedProcess -Process $ownedProcess
            }
            $process = $script:worktreeProcess
            Assert-Equal $true $attempt.CleanupAttempted 'Worktree exception cleanup was not attempted.'
            Assert-Equal 1 @($script:worktreeCleanupPlan.OwnedProcessIds).Count 'Unrelated worktree entered exception cleanup.'
            Assert-True (-not ($script:worktreeCleanupPlan.OwnedProcessIds -contains ($process.Id + 3))) 'Unrelated worktree root was selected.'
        }
        finally {
            Stop-TestOwnedProcess -Process $process
        }
    }

    Invoke-Case 'RealTestFailureDoesNotTriggerOperationalCleanupOrRetry' {
        $process = $null
        $script:failureCleanupCalls = 0
        try {
            $attempt = Invoke-UnityValidationAttempt -StartAction { $script:failureProcess = New-TestOwnedProcess; $script:failureProcess } -RunAction {
                return [pscustomobject]@{ Status = 'Failed' }
            } -CleanupAction {
                param([System.Diagnostics.Process]$ownedProcess)
                $script:failureCleanupCalls++
                Stop-TestOwnedProcess -Process $ownedProcess
            }
            $process = $script:failureProcess
            Assert-Equal $true $attempt.Succeeded 'Real test failure entered the operational exception path.'
            Assert-Equal 0 $script:failureCleanupCalls 'Real test failure triggered operational cleanup.'
            Assert-Equal $false (Test-UnityOperationalRetryEligible -Status 'Failed') 'Real test failure became retryable.'
        }
        finally {
            Stop-TestOwnedProcess -Process $process
        }
    }

    Invoke-Case 'NoGlobalKillCommandIsGenerated' {
        $plan = Get-UnityOwnedProcessCleanupPlan -RootProcessId 101 -ProcessSnapshot @($sameProjectProcess, $unrelatedUnity)
        Assert-Equal $false $plan.UsesGlobalNameKill 'Cleanup plan permits global name kill.'
        $source = Get-Content -Raw (Join-Path $PSScriptRoot '..\UnityValidation.psm1')
        Assert-True (-not $source.Contains('taskkill')) 'Harness contains taskkill.'
        Assert-True (-not ($source -match 'Stop-Process\s+-Name')) 'Harness contains name-based process kill.'
    }

    Invoke-Case 'ExplicitUnityPathStillWinsDiscovery' {
        $explicit = Join-Path $resultsRoot 'Unity.exe'
        Set-Content -LiteralPath $explicit -Value 'mock' -Encoding ASCII
        Assert-Equal (ConvertTo-UnityValidationPath -Path $explicit) (Find-UnityEditor -ProjectPath $projectPath -UnityPath $explicit) 'Explicit Unity path did not win.'
    }

    $missingHubProject = Join-Path $testRoot 'MissingHubProject'
    New-Item -ItemType Directory -Path (Join-Path $missingHubProject 'ProjectSettings') -Force | Out-Null
    $missingHubVersionFile = Join-Path $missingHubProject 'ProjectSettings\ProjectVersion.txt'
    Set-Content -LiteralPath $missingHubVersionFile -Value 'm_EditorVersion: 9999.0.0f1' -Encoding UTF8
    $fakePathUnityDirectory = Join-Path $testRoot 'PathUnity'
    New-Item -ItemType Directory -Path $fakePathUnityDirectory -Force | Out-Null
    $fakePathUnity = Join-Path $fakePathUnityDirectory 'Unity.exe'
    Set-Content -LiteralPath $fakePathUnity -Value 'path mock' -Encoding ASCII

    Invoke-Case 'AutomaticDiscoveryDoesNotUseArbitraryUnityFromPath' {
        $source = Get-Content -Raw (Join-Path $PSScriptRoot '..\UnityValidation.psm1')
        Assert-True (-not $source.Contains('Get-Command Unity.exe')) 'Automatic discovery still contains arbitrary PATH fallback.'
        $oldPath = $env:PATH
        try {
            $env:PATH = "$fakePathUnityDirectory;$oldPath"
            $threw = $false
            try {
                [void](Find-UnityEditor -ProjectPath $missingHubProject)
            }
            catch {
                $threw = $true
                Assert-Contains $_.Exception.Message 'UNITY_NOT_FOUND' 'Missing exact Hub version did not fail safely.'
            }
            Assert-True $threw 'Automatic discovery accepted a Unity executable from PATH.'
        }
        finally {
            $env:PATH = $oldPath
        }
    }

    Invoke-Case 'MissingExactHubVersionFailsInsteadOfUsingPathUnity' {
        $threw = $false
        try {
            [void](Find-UnityEditor -ProjectPath $missingHubProject)
        }
        catch {
            $threw = $true
            Assert-Contains $_.Exception.Message 'UNITY_NOT_FOUND' 'Missing exact Hub version returned the wrong diagnostic.'
        }
        Assert-True $threw 'Discovery did not fail when the exact Hub version was absent.'
    }

    Invoke-Case 'ProjectVersionRemainsUnmodifiedOnDiscoveryFailure' {
        $before = Get-FileHash -LiteralPath $missingHubVersionFile -Algorithm SHA256
        try { [void](Find-UnityEditor -ProjectPath $missingHubProject) } catch { }
        $after = Get-FileHash -LiteralPath $missingHubVersionFile -Algorithm SHA256
        Assert-Equal $before.Hash $after.Hash 'Discovery failure modified ProjectVersion.txt.'
    }

    Invoke-Case 'ProjectVersionCanBeReadWithoutModification' {
        $versionFile = Join-Path $projectPath 'ProjectSettings\ProjectVersion.txt'
        $before = Get-FileHash -LiteralPath $versionFile -Algorithm SHA256
        $version = Get-UnityProjectVersion -ProjectPath $projectPath
        $after = Get-FileHash -LiteralPath $versionFile -Algorithm SHA256
        Assert-True (-not [string]::IsNullOrWhiteSpace($version)) 'Project version was empty.'
        Assert-Equal $before.Hash $after.Hash 'ProjectVersion.txt was modified.'
    }

    Invoke-Case 'ResultSummaryContainsCounts' {
        $result = New-UnityValidationResult -ProjectPath $projectPath -Mode EditMode -Status Failed -TotalTests 5 -PassedTests 3 -FailedTests 2 -SkippedTests 0 -Duration ([TimeSpan]::FromSeconds(2))
        $summary = Format-UnityValidationSummary -Result $result
        Assert-Contains $summary 'Tests: 5' 'Summary omitted total count.'
        Assert-Contains $summary 'Passed: 3' 'Summary omitted passed count.'
        Assert-Contains $summary 'Failed: 2' 'Summary omitted failed count.'
    }

    Invoke-Case 'NullOrMissingTestFilterRunsUnfiltered' {
        $command = Build-UnityValidationCommand -UnityPath 'Unity.exe' -ProjectPath $projectPath -Mode EditMode -ResultXmlPath (Join-Path $resultsRoot 'unfiltered.xml') -LogPath (Join-Path $resultsRoot 'unfiltered.log')
        Assert-True (-not $command.ArgumentString.Contains('-testFilter')) 'Missing filter added a test filter argument.'
    }

    Invoke-Case 'RelativeProjectPathBecomesAbsolute' {
        Push-Location $projectPath
        try {
            $normalized = ConvertTo-UnityValidationPath -Path '.'
        }
        finally {
            Pop-Location
        }
        Assert-True ([System.IO.Path]::IsPathRooted($normalized)) 'Relative project path did not become absolute.'
    }
}
finally {
    if (Test-Path -LiteralPath $testRoot -PathType Container) {
        Remove-Item -LiteralPath $testRoot -Recurse -Force
    }
}

Write-Output "Self-tests: $passed passed, $failed failed"
if ($failed -gt 0) { exit 1 }
exit 0
