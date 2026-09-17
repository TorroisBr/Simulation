Set-StrictMode -Version Latest

function ConvertTo-UnityValidationPath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw 'A path is required.'
    }

    $basePath = (Get-Location).ProviderPath
    if ([System.IO.Path]::IsPathRooted($Path)) {
        $fullPath = [System.IO.Path]::GetFullPath($Path)
    }
    else {
        $fullPath = [System.IO.Path]::GetFullPath((Join-Path -Path $basePath -ChildPath $Path))
    }

    $rootPath = [System.IO.Path]::GetPathRoot($fullPath)
    while ($fullPath.Length -gt $rootPath.Length -and ($fullPath.EndsWith('\') -or $fullPath.EndsWith('/'))) {
        $fullPath = $fullPath.Substring(0, $fullPath.Length - 1)
    }

    return $fullPath
}

function Get-UnityValidationPropertyValue {
    param(
        [AllowNull()]
        [object]$InputObject,

        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    if ($null -eq $InputObject) {
        return $null
    }

    $property = $InputObject.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $null
    }

    return $property.Value
}

function Get-UnityProjectVersion {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    $normalizedProjectPath = ConvertTo-UnityValidationPath -Path $ProjectPath
    $versionFile = Join-Path $normalizedProjectPath 'ProjectSettings\ProjectVersion.txt'
    if (-not (Test-Path -LiteralPath $versionFile -PathType Leaf)) {
        throw "ProjectVersion.txt was not found under '$normalizedProjectPath'."
    }

    $line = Get-Content -LiteralPath $versionFile | Where-Object { $_ -match '^\s*m_EditorVersion:\s*(\S+)\s*$' } | Select-Object -First 1
    if ($null -eq $line) {
        throw "Could not read m_EditorVersion from '$versionFile'."
    }

    [void]($line -match '^\s*m_EditorVersion:\s*(\S+)\s*$')
    return $Matches[1]
}

function Find-UnityEditor {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath,

        [string]$UnityPath
    )

    if (-not [string]::IsNullOrWhiteSpace($UnityPath)) {
        $explicitPath = ConvertTo-UnityValidationPath -Path $UnityPath
        if (-not (Test-Path -LiteralPath $explicitPath -PathType Leaf)) {
            throw "UNITY_NOT_FOUND: explicit UnityPath '$explicitPath' does not exist."
        }

        return $explicitPath
    }

    $version = Get-UnityProjectVersion -ProjectPath $ProjectPath
    $roots = New-Object System.Collections.Generic.List[string]
    $programFiles = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFiles)
    $programFilesX86 = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFilesX86)
    foreach ($root in @(
            (Join-Path $programFiles 'Unity\Hub\Editor'),
            (Join-Path $programFiles 'Unity Hub\Editor'),
            (Join-Path $programFilesX86 'Unity\Hub\Editor'),
            (Join-Path $programFilesX86 'Unity Hub\Editor')
        )) {
        if (-not [string]::IsNullOrWhiteSpace($root) -and -not $roots.Contains($root)) {
            [void]$roots.Add($root)
        }
    }

    foreach ($root in $roots) {
        $candidate = Join-Path $root (Join-Path $version 'Editor\Unity.exe')
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return (ConvertTo-UnityValidationPath -Path $candidate)
        }
    }

    $onPath = Get-Command Unity.exe -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -ne $onPath -and (Test-Path -LiteralPath $onPath.Source -PathType Leaf)) {
        return (ConvertTo-UnityValidationPath -Path $onPath.Source)
    }

    throw "UNITY_NOT_FOUND: no Unity $version installation was found. Provide -UnityPath."
}

function New-UnityValidationRunPaths {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath,

        [Parameter(Mandatory = $true)]
        [ValidateSet('EditMode', 'PlayMode')]
        [string]$Mode,

        [string]$ResultsDirectory
    )

    $normalizedProjectPath = ConvertTo-UnityValidationPath -Path $ProjectPath
    if (-not (Test-Path -LiteralPath $normalizedProjectPath -PathType Container)) {
        throw "Project path '$normalizedProjectPath' does not exist."
    }

    if ([string]::IsNullOrWhiteSpace($ResultsDirectory)) {
        $resolvedResultsDirectory = Join-Path $normalizedProjectPath 'Temp\ValidationResults'
    }
    elseif ([System.IO.Path]::IsPathRooted($ResultsDirectory)) {
        $resolvedResultsDirectory = $ResultsDirectory
    }
    else {
        $resolvedResultsDirectory = Join-Path $normalizedProjectPath $ResultsDirectory
    }

    $resolvedResultsDirectory = ConvertTo-UnityValidationPath -Path $resolvedResultsDirectory
    New-Item -ItemType Directory -Path $resolvedResultsDirectory -Force | Out-Null

    $timestamp = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')
    $runId = [Guid]::NewGuid().ToString('N')
    $stem = "$Mode-$timestamp-$runId"
    return [pscustomobject]@{
        RunId = $runId
        Directory = $resolvedResultsDirectory
        XmlPath = Join-Path $resolvedResultsDirectory "$stem.xml"
        LogPath = Join-Path $resolvedResultsDirectory "$stem.log"
    }
}

function Remove-UnityValidationResults {
    [CmdletBinding(SupportsShouldProcess = $true)]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResultsDirectory
    )

    $directory = ConvertTo-UnityValidationPath -Path $ResultsDirectory
    if (-not (Test-Path -LiteralPath $directory -PathType Container)) {
        return 0
    }

    $pattern = '^(EditMode|PlayMode)-\d{8}-\d{6}-[0-9a-f]{32}\.(xml|log)$'
    $removed = 0
    foreach ($file in @(Get-ChildItem -LiteralPath $directory -File -ErrorAction SilentlyContinue | Where-Object { $_.Name -match $pattern })) {
        if ($PSCmdlet.ShouldProcess($file.FullName, 'Remove Unity validation result')) {
            Remove-Item -LiteralPath $file.FullName -Force
            $removed++
        }
    }

    return $removed
}

function ConvertTo-UnityCommandLineArgument {
    param(
        [AllowEmptyString()]
        [string]$Value
    )

    if ($Value -notmatch '[\s"]') {
        return $Value
    }

    return '"' + $Value.Replace('"', '\"') + '"'
}

function Build-UnityValidationCommand {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$UnityPath,

        [Parameter(Mandatory = $true)]
        [string]$ProjectPath,

        [Parameter(Mandatory = $true)]
        [ValidateSet('EditMode', 'PlayMode')]
        [string]$Mode,

        [Parameter(Mandatory = $true)]
        [string]$ResultXmlPath,

        [Parameter(Mandatory = $true)]
        [string]$LogPath,

        [string]$TestFilter,

        [string]$TestCategory
    )

    $arguments = New-Object System.Collections.Generic.List[string]
    foreach ($argument in @(
            '-batchmode',
            '-runTests',
            '-testPlatform',
            $Mode,
            '-projectPath',
            (ConvertTo-UnityValidationPath -Path $ProjectPath),
            '-testResults',
            (ConvertTo-UnityValidationPath -Path $ResultXmlPath),
            '-logFile',
            (ConvertTo-UnityValidationPath -Path $LogPath)
        )) {
        [void]$arguments.Add([string]$argument)
    }

    if (-not [string]::IsNullOrWhiteSpace($TestFilter)) {
        [void]$arguments.Add('-testFilter')
        [void]$arguments.Add($TestFilter)
    }

    if (-not [string]::IsNullOrWhiteSpace($TestCategory)) {
        [void]$arguments.Add('-testCategory')
        [void]$arguments.Add($TestCategory)
    }

    $argumentString = (($arguments | ForEach-Object { ConvertTo-UnityCommandLineArgument -Value $_ }) -join ' ')
    $normalizedUnityPath = ConvertTo-UnityValidationPath -Path $UnityPath
    return [pscustomobject]@{
        FilePath = $normalizedUnityPath
        Arguments = @($arguments)
        ArgumentString = $argumentString
        DisplayCommand = ((ConvertTo-UnityCommandLineArgument -Value $normalizedUnityPath) + ' ' + $argumentString)
        ProjectPath = ConvertTo-UnityValidationPath -Path $ProjectPath
        Mode = $Mode
        TestFilter = $TestFilter
        TestCategory = $TestCategory
    }
}

function Get-UnityProcessSnapshot {
    [CmdletBinding()]
    param(
        [object[]]$ProcessSnapshot
    )

    if ($null -ne $ProcessSnapshot) {
        return @($ProcessSnapshot)
    }

    try {
        return @(Get-CimInstance Win32_Process -Filter "Name='Unity.exe' OR Name='Unity64.exe'")
    }
    catch {
        return @()
    }
}

function Get-UnityCommandLineProjectPath {
    [CmdletBinding()]
    param(
        [string]$CommandLine
    )

    if ([string]::IsNullOrWhiteSpace($CommandLine)) {
        return $null
    }

    $pattern = '(?i)(?:^|\s)-projectPath(?:=|\s+)(?:"(?<quoted>[^"]+)"|(?<bare>[^\s]+))'
    $match = [regex]::Match($CommandLine, $pattern)
    if (-not $match.Success) {
        return $null
    }

    $value = if ($match.Groups['quoted'].Success) { $match.Groups['quoted'].Value } else { $match.Groups['bare'].Value }
    try {
        return (ConvertTo-UnityValidationPath -Path $value)
    }
    catch {
        return $null
    }
}

function Test-UnityProjectAlreadyOpen {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath,

        [object[]]$ProcessSnapshot
    )

    $normalizedProjectPath = ConvertTo-UnityValidationPath -Path $ProjectPath
    foreach ($process in @(Get-UnityProcessSnapshot -ProcessSnapshot $ProcessSnapshot)) {
        $name = [string](Get-UnityValidationPropertyValue -InputObject $process -Name 'Name')
        if ($name -notmatch '^Unity(64)?\.exe$') {
            continue
        }

        $commandLine = [string](Get-UnityValidationPropertyValue -InputObject $process -Name 'CommandLine')
        if ($commandLine -match '(?i)(^|\s)-batchmode(\s|$)') {
            continue
        }

        $processProjectPath = Get-UnityCommandLineProjectPath -CommandLine $commandLine
        if ($null -ne $processProjectPath -and [StringComparer]::OrdinalIgnoreCase.Equals($normalizedProjectPath, $processProjectPath)) {
            return [pscustomobject]@{
                IsOpen = $true
                ProcessId = [int](Get-UnityValidationPropertyValue -InputObject $process -Name 'ProcessId')
                ProjectPath = $processProjectPath
                CommandLine = $commandLine
            }
        }
    }

    return $null
}

function Start-UnityValidationProcess {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [object]$Command
    )

    return (Start-Process -FilePath $Command.FilePath -ArgumentList $Command.ArgumentString -WorkingDirectory $Command.ProjectPath -PassThru -WindowStyle Hidden -ErrorAction Stop)
}

function Wait-UnityValidationProcess {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [System.Diagnostics.Process]$Process,

        [Parameter(Mandatory = $true)]
        [ValidateRange(1, 1440)]
        [int]$TimeoutMinutes
    )

    $watch = [System.Diagnostics.Stopwatch]::StartNew()
    $timeout = [TimeSpan]::FromMinutes($TimeoutMinutes)
    while ($true) {
        try {
            if ($Process.HasExited) {
                $Process.Refresh()
                return [pscustomobject]@{
                    Completed = $true
                    TimedOut = $false
                    ExitCode = $Process.ExitCode
                    Duration = $watch.Elapsed
                }
            }
        }
        catch {
            return [pscustomobject]@{
                Completed = $true
                TimedOut = $false
                ExitCode = $null
                Duration = $watch.Elapsed
            }
        }

        if ($watch.Elapsed -ge $timeout) {
            return [pscustomobject]@{
                Completed = $false
                TimedOut = $true
                ExitCode = $null
                Duration = $watch.Elapsed
            }
        }

        Start-Sleep -Milliseconds 250
    }
}

function Get-UnityOwnedProcessIds {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [int]$RootProcessId,

        [object[]]$ProcessSnapshot
    )

    $snapshot = if ($null -ne $ProcessSnapshot) { @($ProcessSnapshot) } else { @(Get-CimInstance Win32_Process) }
    $owned = New-Object System.Collections.Generic.HashSet[int]
    [void]$owned.Add($RootProcessId)

    $changed = $true
    while ($changed) {
        $changed = $false
        foreach ($process in $snapshot) {
            $processIdValue = Get-UnityValidationPropertyValue -InputObject $process -Name 'ProcessId'
            $parentProcessIdValue = Get-UnityValidationPropertyValue -InputObject $process -Name 'ParentProcessId'
            if ($null -eq $processIdValue -or $null -eq $parentProcessIdValue) {
                continue
            }

            if ($owned.Contains([int]$parentProcessIdValue) -and $owned.Add([int]$processIdValue)) {
                $changed = $true
            }
        }
    }

    return @($owned | Sort-Object -Descending)
}

function Get-UnityOwnedProcessCleanupPlan {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [int]$RootProcessId,

        [object[]]$ProcessSnapshot
    )

    return [pscustomobject]@{
        RootProcessId = $RootProcessId
        OwnedProcessIds = @(Get-UnityOwnedProcessIds -RootProcessId $RootProcessId -ProcessSnapshot $ProcessSnapshot)
        UsesGlobalNameKill = $false
    }
}

function Stop-UnityOwnedProcessTree {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [System.Diagnostics.Process]$RootProcess,

        [ValidateRange(0, 120)]
        [int]$GraceSeconds = 10
    )

    $rootProcessId = $RootProcess.Id
    try {
        if (-not $RootProcess.HasExited) {
            [void]$RootProcess.CloseMainWindow()
            if ($GraceSeconds -gt 0) {
                [void]$RootProcess.WaitForExit($GraceSeconds * 1000)
            }
        }
    }
    catch {
        # The process may already have exited; the ownership check below remains authoritative.
    }

    $plan = Get-UnityOwnedProcessCleanupPlan -RootProcessId $rootProcessId
    foreach ($processId in @($plan.OwnedProcessIds | Where-Object { $_ -ne $rootProcessId })) {
        try {
            Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
        }
        catch {
            # Cleanup is best effort and never broadens beyond the owned process tree.
        }
    }

    try {
        if (-not $RootProcess.HasExited) {
            Stop-Process -Id $rootProcessId -Force -ErrorAction SilentlyContinue
        }
    }
    catch {
        # The root may have exited during cleanup.
    }

    return $plan
}

function Get-UnityXmlIntAttribute {
    param(
        [Parameter(Mandatory = $true)]
        [System.Xml.XmlElement]$Element,

        [Parameter(Mandatory = $true)]
        [string[]]$Names
    )

    foreach ($name in $Names) {
        if ($Element.HasAttribute($name)) {
            try {
                return [Convert]::ToInt32($Element.GetAttribute($name), [Globalization.CultureInfo]::InvariantCulture)
            }
            catch {
                return $null
            }
        }
    }

    return $null
}

function New-InvalidUnityXmlResult {
    param(
        [Parameter(Mandatory = $true)]
        [string]$XmlPath,

        [Parameter(Mandatory = $true)]
        [string]$ErrorMessage
    )

    return [pscustomobject]@{
        XmlPath = $XmlPath
        IsValid = $false
        FrameworkResult = $null
        TotalTests = 0
        PassedTests = 0
        FailedTests = 0
        SkippedTests = 0
        InconclusiveTests = 0
        CountsCoherent = $false
        GatePassed = $false
        ErrorMessage = $ErrorMessage
    }
}

function Read-UnityTestResultXml {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$XmlPath
    )

    $normalizedXmlPath = ConvertTo-UnityValidationPath -Path $XmlPath
    if (-not (Test-Path -LiteralPath $normalizedXmlPath -PathType Leaf)) {
        return (New-InvalidUnityXmlResult -XmlPath $normalizedXmlPath -ErrorMessage 'Result XML was not produced.')
    }

    $settings = New-Object System.Xml.XmlReaderSettings
    $settings.DtdProcessing = [System.Xml.DtdProcessing]::Prohibit
    $settings.XmlResolver = $null
    $reader = $null
    $document = New-Object System.Xml.XmlDocument
    try {
        $reader = [System.Xml.XmlReader]::Create($normalizedXmlPath, $settings)
        $document.Load($reader)
    }
    catch {
        return (New-InvalidUnityXmlResult -XmlPath $normalizedXmlPath -ErrorMessage "Result XML could not be parsed: $($_.Exception.Message)")
    }
    finally {
        if ($null -ne $reader) {
            $reader.Dispose()
        }
    }

    $root = $document.DocumentElement
    if ($null -eq $root -or $root.Name -ne 'test-run') {
        return (New-InvalidUnityXmlResult -XmlPath $normalizedXmlPath -ErrorMessage 'Result XML root must be test-run.')
    }

    $testCases = @($document.SelectNodes('//test-case'))
    $derived = @{
        Total = 0
        Passed = 0
        Failed = 0
        Skipped = 0
        Inconclusive = 0
    }
    foreach ($testCase in $testCases) {
        $derived.Total++
        switch -Regex ([string]$testCase.GetAttribute('result')) {
            '^Passed$' { $derived.Passed++; break }
            '^Failed$|^Error$' { $derived.Failed++; break }
            '^Skipped$|^Ignored$|^NotRunnable$' { $derived.Skipped++; break }
            '^Inconclusive$' { $derived.Inconclusive++; break }
            default { $derived.Skipped++; break }
        }
    }

    $total = Get-UnityXmlIntAttribute -Element $root -Names @('testcasecount', 'total')
    $passed = Get-UnityXmlIntAttribute -Element $root -Names @('passed')
    $failed = Get-UnityXmlIntAttribute -Element $root -Names @('failed')
    $skipped = Get-UnityXmlIntAttribute -Element $root -Names @('skipped', 'notrun')
    $inconclusive = Get-UnityXmlIntAttribute -Element $root -Names @('inconclusive')

    if ($null -eq $total -and $testCases.Count -gt 0) { $total = $derived.Total }
    if ($null -eq $passed -and $testCases.Count -gt 0) { $passed = $derived.Passed }
    if ($null -eq $failed -and $testCases.Count -gt 0) { $failed = $derived.Failed }
    if ($null -eq $skipped -and $testCases.Count -gt 0) { $skipped = $derived.Skipped }
    if ($null -eq $inconclusive -and $testCases.Count -gt 0) { $inconclusive = $derived.Inconclusive }

    if ($null -eq $inconclusive) { $inconclusive = 0 }
    if ($null -eq $total -or $null -eq $passed -or $null -eq $failed -or $null -eq $skipped) {
        return (New-InvalidUnityXmlResult -XmlPath $normalizedXmlPath -ErrorMessage 'Result XML did not contain complete test counts.')
    }

    $frameworkResult = [string]$root.GetAttribute('result')
    $sum = [int]$passed + [int]$failed + [int]$skipped + [int]$inconclusive
    $countsCoherent = ([int]$total -eq $sum -and [int]$total -ge 0 -and [int]$passed -ge 0 -and [int]$failed -ge 0 -and [int]$skipped -ge 0 -and [int]$inconclusive -ge 0)
    $gatePassed = ($frameworkResult -ieq 'Passed' -and [int]$failed -eq 0 -and [int]$inconclusive -eq 0 -and $countsCoherent)

    return [pscustomobject]@{
        XmlPath = $normalizedXmlPath
        IsValid = $true
        FrameworkResult = $frameworkResult
        TotalTests = [int]$total
        PassedTests = [int]$passed
        FailedTests = [int]$failed
        SkippedTests = [int]$skipped
        InconclusiveTests = [int]$inconclusive
        CountsCoherent = $countsCoherent
        GatePassed = $gatePassed
        ErrorMessage = if ($countsCoherent) { $null } else { 'Result XML test counts are not coherent.' }
    }
}

function Resolve-UnityValidationStatus {
    [CmdletBinding()]
    param(
        [object]$XmlResult,

        [switch]$TimedOut,

        [switch]$LaunchFailed,

        [switch]$ProjectAlreadyOpen
    )

    if ($ProjectAlreadyOpen) { return 'ProjectAlreadyOpen' }
    if ($LaunchFailed) { return 'UnityLaunchFailed' }
    if ($TimedOut) { return 'TimedOut' }
    if ($null -eq $XmlResult) {
        return 'NoResultXml'
    }
    if (-not $XmlResult.IsValid) {
        return 'InvalidResultXml'
    }
    if ($XmlResult.GatePassed) {
        return 'Passed'
    }

    return 'Failed'
}

function Test-UnityOperationalRetryEligible {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Status
    )

    return ($Status -in @('UnityLaunchFailed', 'NoResultXml', 'OperationalFailure'))
}

function New-UnityValidationResult {
    [CmdletBinding()]
    param(
        [string]$ProjectPath,
        [string]$UnityPath,
        [string]$Mode,
        [string]$TestFilter,
        [string]$TestCategory,
        [DateTime]$StartedAt = [DateTime]::UtcNow,
        [TimeSpan]$Duration = [TimeSpan]::Zero,
        [Nullable[int]]$ProcessId,
        [Nullable[int]]$ExitCode,
        [string]$XmlPath,
        [string]$LogPath,
        [Parameter(Mandatory = $true)]
        [string]$Status,
        [int]$TotalTests = 0,
        [int]$PassedTests = 0,
        [int]$FailedTests = 0,
        [int]$SkippedTests = 0,
        [int]$InconclusiveTests = 0,
        [string]$FrameworkResult,
        [bool]$CountsCoherent = $false,
        [string]$ErrorMessage,
        [int]$AttemptCount = 1,
        [object[]]$AttemptHistory
    )

    return [pscustomobject]@{
        PSTypeName = 'UnityValidationResult'
        ProjectPath = $ProjectPath
        UnityPath = $UnityPath
        Mode = $Mode
        TestFilter = $TestFilter
        TestCategory = $TestCategory
        StartedAt = $StartedAt
        Duration = $Duration
        ProcessId = $ProcessId
        ExitCode = $ExitCode
        XmlPath = $XmlPath
        LogPath = $LogPath
        Status = $Status
        TotalTests = $TotalTests
        PassedTests = $PassedTests
        FailedTests = $FailedTests
        SkippedTests = $SkippedTests
        InconclusiveTests = $InconclusiveTests
        FrameworkResult = $FrameworkResult
        CountsCoherent = $CountsCoherent
        GatePassed = ($Status -eq 'Passed')
        ErrorMessage = $ErrorMessage
        AttemptCount = $AttemptCount
        AttemptHistory = @($AttemptHistory)
    }
}

function Invoke-UnityValidation {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath,

        [Parameter(Mandatory = $true)]
        [ValidateSet('EditMode', 'PlayMode')]
        [string]$Mode,

        [string]$UnityPath,

        [string]$TestFilter,

        [string]$TestCategory,

        [ValidateRange(1, 1440)]
        [int]$TimeoutMinutes = 30,

        [string]$ResultsDirectory,

        [switch]$RetryOperationalFailure,

        [switch]$CleanOldResults
    )

    $startedAt = [DateTime]::UtcNow
    $watch = [System.Diagnostics.Stopwatch]::StartNew()
    $normalizedProjectPath = $null
    $resolvedUnityPath = $null
    $history = New-Object System.Collections.Generic.List[object]

    try {
        $normalizedProjectPath = ConvertTo-UnityValidationPath -Path $ProjectPath
        if (-not (Test-Path -LiteralPath $normalizedProjectPath -PathType Container)) {
            throw "Project path '$normalizedProjectPath' does not exist."
        }
        $resolvedUnityPath = Find-UnityEditor -ProjectPath $normalizedProjectPath -UnityPath $UnityPath
    }
    catch {
        return (New-UnityValidationResult -ProjectPath $normalizedProjectPath -UnityPath $resolvedUnityPath -Mode $Mode -TestFilter $TestFilter -TestCategory $TestCategory -StartedAt $startedAt -Duration $watch.Elapsed -Status 'UnityLaunchFailed' -ErrorMessage $_.Exception.Message)
    }

    $effectiveResultsDirectory = if ([string]::IsNullOrWhiteSpace($ResultsDirectory)) { Join-Path $normalizedProjectPath 'Temp\ValidationResults' } elseif ([System.IO.Path]::IsPathRooted($ResultsDirectory)) { $ResultsDirectory } else { Join-Path $normalizedProjectPath $ResultsDirectory }
    if ($CleanOldResults) {
        [void](Remove-UnityValidationResults -ResultsDirectory $effectiveResultsDirectory -Confirm:$false)
    }

    $maxAttempts = if ($RetryOperationalFailure) { 2 } else { 1 }
    $lastResult = $null
    for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
        $runPaths = New-UnityValidationRunPaths -ProjectPath $normalizedProjectPath -Mode $Mode -ResultsDirectory $effectiveResultsDirectory
        $runStartedAt = [DateTime]::UtcNow
        $runWatch = [System.Diagnostics.Stopwatch]::StartNew()
        $lock = Test-UnityProjectAlreadyOpen -ProjectPath $normalizedProjectPath
        if ($null -ne $lock) {
            $lastResult = New-UnityValidationResult -ProjectPath $normalizedProjectPath -UnityPath $resolvedUnityPath -Mode $Mode -TestFilter $TestFilter -TestCategory $TestCategory -StartedAt $runStartedAt -Duration $runWatch.Elapsed -ProcessId $lock.ProcessId -XmlPath $runPaths.XmlPath -LogPath $runPaths.LogPath -Status 'ProjectAlreadyOpen' -ErrorMessage "PROJECT_ALREADY_OPEN: Unity process $($lock.ProcessId) already has this exact ProjectPath open."
        }
        else {
            $command = Build-UnityValidationCommand -UnityPath $resolvedUnityPath -ProjectPath $normalizedProjectPath -Mode $Mode -ResultXmlPath $runPaths.XmlPath -LogPath $runPaths.LogPath -TestFilter $TestFilter -TestCategory $TestCategory
            $process = $null
            try {
                $process = Start-UnityValidationProcess -Command $command
                $wait = Wait-UnityValidationProcess -Process $process -TimeoutMinutes $TimeoutMinutes
                $xmlResult = if (Test-Path -LiteralPath $runPaths.XmlPath -PathType Leaf) { Read-UnityTestResultXml -XmlPath $runPaths.XmlPath } else { $null }
                $status = Resolve-UnityValidationStatus -XmlResult $xmlResult -TimedOut:$wait.TimedOut
                if ($wait.TimedOut) {
                    [void](Stop-UnityOwnedProcessTree -RootProcess $process)
                }
                elseif ($null -eq $xmlResult) {
                    $status = 'NoResultXml'
                }
                elseif (-not $xmlResult.IsValid) {
                    $status = 'InvalidResultXml'
                }

                $lastResult = New-UnityValidationResult -ProjectPath $normalizedProjectPath -UnityPath $resolvedUnityPath -Mode $Mode -TestFilter $TestFilter -TestCategory $TestCategory -StartedAt $runStartedAt -Duration $runWatch.Elapsed -ProcessId $process.Id -ExitCode $(if ($wait.TimedOut) { $null } else { $wait.ExitCode }) -XmlPath $runPaths.XmlPath -LogPath $runPaths.LogPath -Status $status -TotalTests $(if ($null -ne $xmlResult) { $xmlResult.TotalTests } else { 0 }) -PassedTests $(if ($null -ne $xmlResult) { $xmlResult.PassedTests } else { 0 }) -FailedTests $(if ($null -ne $xmlResult) { $xmlResult.FailedTests } else { 0 }) -SkippedTests $(if ($null -ne $xmlResult) { $xmlResult.SkippedTests } else { 0 }) -InconclusiveTests $(if ($null -ne $xmlResult) { $xmlResult.InconclusiveTests } else { 0 }) -FrameworkResult $(if ($null -ne $xmlResult) { $xmlResult.FrameworkResult } else { $null }) -CountsCoherent $(if ($null -ne $xmlResult) { $xmlResult.CountsCoherent } else { $false }) -ErrorMessage $(if ($wait.TimedOut) { 'UNITY_TIMEOUT: Unity did not finish within TimeoutMinutes; owned process cleanup was attempted.' } elseif ($null -ne $xmlResult -and $null -ne $xmlResult.ErrorMessage) { $xmlResult.ErrorMessage } else { $null }) -AttemptCount $attempt
            }
            catch {
                $lastResult = New-UnityValidationResult -ProjectPath $normalizedProjectPath -UnityPath $resolvedUnityPath -Mode $Mode -TestFilter $TestFilter -TestCategory $TestCategory -StartedAt $runStartedAt -Duration $runWatch.Elapsed -XmlPath $runPaths.XmlPath -LogPath $runPaths.LogPath -Status 'UnityLaunchFailed' -ErrorMessage $_.Exception.Message -AttemptCount $attempt
            }
        }

        [void]$history.Add($lastResult)
        if ($lastResult.Status -eq 'Passed' -or -not $RetryOperationalFailure -or -not (Test-UnityOperationalRetryEligible -Status $lastResult.Status) -or $attempt -ge $maxAttempts) {
            break
        }

        Start-Sleep -Seconds 1
    }

    $lastResult.AttemptCount = $history.Count
    $lastResult.AttemptHistory = @($history.ToArray())
    return $lastResult
}

function Format-UnityValidationSummary {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [object]$Result
    )

    $label = if ($Result.Status -eq 'Passed') { 'PASS' } else { 'FAIL' }
    $duration = ([TimeSpan]$Result.Duration).ToString('hh\:mm\:ss')
    $lines = @(
        "[$label] $($Result.Mode)",
        "Tests: $($Result.TotalTests)",
        "Passed: $($Result.PassedTests)",
        "Failed: $($Result.FailedTests)",
        "Skipped: $($Result.SkippedTests)",
        "Duration: $duration",
        "XML: $($Result.XmlPath)",
        "Log: $($Result.LogPath)"
    )
    if ($Result.Status -ne 'Passed') {
        $lines += "Status: $($Result.Status)"
        if (-not [string]::IsNullOrWhiteSpace($Result.ErrorMessage)) {
            $lines += "Reason: $($Result.ErrorMessage)"
        }
    }

    return ($lines -join [Environment]::NewLine)
}

Export-ModuleMember -Function @(
    'Build-UnityValidationCommand',
    'ConvertTo-UnityValidationPath',
    'Find-UnityEditor',
    'Format-UnityValidationSummary',
    'Get-UnityCommandLineProjectPath',
    'Get-UnityOwnedProcessCleanupPlan',
    'Get-UnityOwnedProcessIds',
    'Get-UnityProjectVersion',
    'Invoke-UnityValidation',
    'New-UnityValidationResult',
    'New-UnityValidationRunPaths',
    'Read-UnityTestResultXml',
    'Remove-UnityValidationResults',
    'Resolve-UnityValidationStatus',
    'Test-UnityOperationalRetryEligible',
    'Test-UnityProjectAlreadyOpen'
)
