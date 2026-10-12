Set-StrictMode -Version Latest

$script:EvidenceToolVersion = '1.0.0'
$script:MaximumXmlBytes = 268435456
$script:MaximumArchiveExpandedBytes = 536870912
$script:MaximumManifestBytes = 67108864
$script:RequiredSourceRoots = @('Assets', 'Packages', 'ProjectSettings', 'Tools/UnityValidation')

function Get-EvidenceSha256 {
    param([Parameter(Mandatory)][string]$LiteralPath)
    $stream = [System.IO.File]::OpenRead($LiteralPath)
    try {
        $sha = [System.Security.Cryptography.SHA256]::Create()
        try { return ([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-', '').ToLowerInvariant() }
        finally { $sha.Dispose() }
    }
    finally { $stream.Dispose() }
}

function Get-BytesSha256 {
    param([Parameter(Mandatory)][byte[]]$Bytes)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($Bytes))).Replace('-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
}

function ConvertTo-EvidenceJsonBytes {
    param([Parameter(Mandatory)][object]$Value)
    $json = ConvertTo-Json -InputObject $Value -Depth 80
    return [System.Text.UTF8Encoding]::new($false).GetBytes($json + [Environment]::NewLine)
}

function Write-EvidenceJson {
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][object]$Value)
    [System.IO.File]::WriteAllBytes($Path, (ConvertTo-EvidenceJsonBytes $Value))
}

function Get-GitValue {
    param([Parameter(Mandatory)][string]$ProjectPath, [Parameter(Mandatory)][string[]]$GitArguments)
    $value = & git -C $ProjectPath @GitArguments 2>$null
    if ($LASTEXITCODE -ne 0) { throw "Git identity query failed: git $($GitArguments -join ' ')." }
    return (($value | Out-String).Trim())
}

function ConvertTo-PublicRemoteIdentity {
    param([AllowEmptyString()][string]$Remote)
    if ([string]::IsNullOrWhiteSpace($Remote)) { return $null }
    try {
        if ($Remote -match '^https?://') {
            $uri = [Uri]$Remote
            if ($uri.Scheme -notin @('http', 'https') -or [string]::IsNullOrWhiteSpace($uri.Host)) { return 'redacted-unrecognized-remote' }
            return ('{0}://{1}{2}' -f $uri.Scheme.ToLowerInvariant(), $uri.Host.ToLowerInvariant(), $uri.AbsolutePath.TrimEnd('/'))
        }
        if ($Remote -match '^(?:[^@/]+@)?([^:/]+):(.+)$') {
            $hostName = $Matches[1].ToLowerInvariant()
            $repoPath = $Matches[2].TrimStart('/').TrimEnd('/')
            if ($repoPath -match '(^|/)\.\.(/|$)' -or $repoPath -match '[\r\n]') { return 'redacted-unrecognized-remote' }
            return ('ssh://{0}/{1}' -f $hostName, $repoPath)
        }
    }
    catch { return 'redacted-unrecognized-remote' }
    return 'redacted-unrecognized-remote'
}

function Assert-EvidenceRuntime {
    $expectedPowerShell = '7.6.5'
    $expectedDotnet = '10.0.11'
    if ($PSVersionTable.PSEdition -ne 'Core' -or $PSVersionTable.PSVersion.ToString() -ne $expectedPowerShell) { throw "Evidence packaging requires PowerShell $expectedPowerShell Core." }
    if ([Environment]::Version.ToString() -ne $expectedDotnet) { throw "Evidence packaging requires .NET runtime $expectedDotnet." }
    if ($env:OS -ne 'Windows_NT') { throw 'Evidence packaging requires Windows.' }
    $compression = [System.IO.Compression.ZipArchive].Assembly.GetName().Version.ToString()
    return [ordered]@{ powershellEdition = 'Core'; powershellVersion = $expectedPowerShell; dotnetVersion = $expectedDotnet; compressionAssemblyVersion = $compression; operatingSystem = [System.Runtime.InteropServices.RuntimeInformation]::OSDescription; architecture = [string][System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture; culture = [Globalization.CultureInfo]::CurrentCulture.Name; timeZone = [TimeZoneInfo]::Local.Id }
}

function Get-UnityEvidenceInputSnapshot {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$ProjectPath)
    $project = [System.IO.Path]::GetFullPath($ProjectPath)
    if (-not (Test-Path -LiteralPath $project -PathType Container)) { throw 'The project path does not exist.' }
    $head = Get-GitValue $project @('rev-parse', 'HEAD')
    $tree = Get-GitValue $project @('rev-parse', 'HEAD^{tree}')
    $branch = Get-GitValue $project @('branch', '--show-current')
    $remoteRaw = Get-GitValue $project @('config', '--get', 'remote.origin.url')
    $files = [System.Collections.Generic.List[object]]::new()
    foreach ($sourceRoot in $script:RequiredSourceRoots) {
        $rootPath = Join-Path $project ($sourceRoot.Replace('/', [System.IO.Path]::DirectorySeparatorChar))
        if (-not (Test-Path -LiteralPath $rootPath -PathType Container)) { throw "Required source root '$sourceRoot' is missing." }
        $rootItems = @(Get-ChildItem -LiteralPath $rootPath -Force -Recurse -ErrorAction Stop)
        if (@($rootItems | Where-Object { ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 }).Count -gt 0) { throw "Validation input root '$sourceRoot' contains a reparse point that cannot be inventoried safely." }
        if (@($rootItems | Where-Object { $_.Name -eq '.git' }).Count -gt 0) { throw "Validation input root '$sourceRoot' contains a nested Git repository." }
        $filesByRelativePath = [System.Collections.Generic.SortedDictionary[string, System.IO.FileInfo]]::new([StringComparer]::Ordinal)
        foreach ($file in @($rootItems | Where-Object { -not $_.PSIsContainer })) {
            $relative = [System.IO.Path]::GetRelativePath($project, $file.FullName).Replace('\', '/')
            $filesByRelativePath.Add($relative, [System.IO.FileInfo]$file)
        }
        foreach ($relative in $filesByRelativePath.Keys) {
            $file = $filesByRelativePath[$relative]
            $files.Add([ordered]@{ path = $relative; bytes = [long]$file.Length; sha256 = (Get-EvidenceSha256 -LiteralPath $file.FullName) })
        }
    }
    $projectVersionPath = Join-Path $project 'ProjectSettings/ProjectVersion.txt'
    $unityVersion = $null
    if (Test-Path -LiteralPath $projectVersionPath -PathType Leaf) {
        $match = [regex]::Match([System.IO.File]::ReadAllText($projectVersionPath), '(?m)^\s*m_EditorVersion:\s*(\S+)')
        if ($match.Success) { $unityVersion = $match.Groups[1].Value }
    }
    $snapshotCore = [ordered]@{
        schema = 'unity-validation-input-snapshot-v1'
        repository = [ordered]@{ origin = (ConvertTo-PublicRemoteIdentity $remoteRaw); branch = $branch; head = $head; tree = $tree }
        unityEditorVersion = $unityVersion
        files = @($files.ToArray())
    }
    return [ordered]@{
        schema = $snapshotCore.schema
        repository = $snapshotCore.repository
        unityEditorVersion = $snapshotCore.unityEditorVersion
        files = $snapshotCore.files
        sha256 = (Get-BytesSha256 (ConvertTo-EvidenceJsonBytes $snapshotCore))
    }
}

function Set-ProtectedEvidenceDirectory {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)
    $full = [System.IO.Path]::GetFullPath($Path)
    if ($full.StartsWith('\\', [StringComparison]::Ordinal) -or [string]::IsNullOrWhiteSpace([System.IO.Path]::GetPathRoot($full))) { throw 'Protected evidence must use a local drive path.' }
    $drive = [System.IO.DriveInfo]::new([System.IO.Path]::GetPathRoot($full))
    if ($drive.DriveType -ne [System.IO.DriveType]::Fixed) { throw 'Protected evidence must be stored on a fixed local drive.' }
    if (-not (Test-Path -LiteralPath $full -PathType Container)) { New-Item -ItemType Directory -Path $full -Force | Out-Null }
    if ($env:OS -ne 'Windows_NT') { throw 'Protected local evidence capture currently requires Windows ACL support.' }
    $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    $sid = $identity.User
    $acl = [System.Security.AccessControl.DirectorySecurity]::new()
    $acl.SetOwner($sid)
    $acl.SetAccessRuleProtection($true, $false)
    $rule = [System.Security.AccessControl.FileSystemAccessRule]::new($sid, [System.Security.AccessControl.FileSystemRights]::FullControl, ([System.Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [System.Security.AccessControl.InheritanceFlags]::ObjectInherit), [System.Security.AccessControl.PropagationFlags]::None, [System.Security.AccessControl.AccessControlType]::Allow)
    [void]$acl.AddAccessRule($rule)
    Add-Type -AssemblyName System.IO.FileSystem.AccessControl
    $directoryInfo = [System.IO.DirectoryInfo]::new($full)
    [System.IO.FileSystemAclExtensions]::SetAccessControl($directoryInfo, $acl)
    $check = [System.IO.FileSystemAclExtensions]::GetAccessControl($directoryInfo)
    if (-not $check.AreAccessRulesProtected) { throw 'Evidence directory ACL inheritance could not be disabled.' }
    if ($check.GetOwner([System.Security.Principal.SecurityIdentifier]).Value -ne $sid.Value) { throw 'Evidence directory owner is not the current account.' }
    $rules = @($check.GetAccessRules($true, $true, [System.Security.Principal.SecurityIdentifier]))
    if ($rules.Count -ne 1 -or (($rules[0].FileSystemRights -band [System.Security.AccessControl.FileSystemRights]::FullControl) -ne [System.Security.AccessControl.FileSystemRights]::FullControl)) { throw 'Evidence directory does not grant exactly one full-control rule.' }
    foreach ($access in $rules) {
        if ($access.IdentityReference.Value -ne $sid.Value -or $access.AccessControlType -ne [System.Security.AccessControl.AccessControlType]::Allow) {
            throw 'Evidence directory ACL contains an unexpected principal or deny rule.'
        }
    }
    return $full
}

function Get-FileRecord {
    param([AllowNull()][string]$Path)
    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $null }
    $item = Get-Item -LiteralPath $Path
    return [ordered]@{ bytes = [long]$item.Length; sha256 = (Get-EvidenceSha256 $Path) }
}

function ConvertTo-PublicUnityArguments {
    param([Parameter(Mandatory)][string[]]$Arguments)
    $result = [System.Collections.Generic.List[string]]::new()
    $replaceNext = $null
    foreach ($argument in $Arguments) {
        if ($null -ne $replaceNext) {
            $result.Add($replaceNext)
            $replaceNext = $null
            continue
        }
        $result.Add([string]$argument)
        switch -CaseSensitive ([string]$argument) {
            '-projectPath' { $replaceNext = '<PROJECT_ROOT>' }
            '-testResults' { $replaceNext = '<RESULT_XML>' }
            '-logFile' { $replaceNext = '<RAW_LOG>' }
        }
    }
    return @($result.ToArray())
}

function Invoke-UnityValidationEvidenceCapture {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$ProjectPath,
        [Parameter(Mandatory)][ValidateSet('EditMode', 'PlayMode')][string]$Mode,
        [string]$UnityPath,
        [string]$TestFilter,
        [string]$TestCategory,
        [ValidateRange(1, 1440)][int]$TimeoutMinutes = 30,
        [switch]$RetryOperationalFailure,
        [string]$EvidenceRoot = (Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) 'Simulation/ValidationEvidence')
    )
    $runner = Assert-EvidenceRuntime
    $project = [System.IO.Path]::GetFullPath($ProjectPath)
    $root = Set-ProtectedEvidenceDirectory -Path $EvidenceRoot
    $runId = [Guid]::NewGuid().ToString('N')
    $runDirectory = Join-Path $root $runId
    $null = Set-ProtectedEvidenceDirectory -Path $runDirectory
    $resultsDirectory = Join-Path $runDirectory 'original'
    New-Item -ItemType Directory -Path $resultsDirectory -Force | Out-Null
    $null = Set-ProtectedEvidenceDirectory -Path $resultsDirectory
    $before = Get-UnityEvidenceInputSnapshot -ProjectPath $project
    $startedAt = [DateTime]::UtcNow
    Import-Module (Join-Path $PSScriptRoot 'UnityValidation.psm1') -Force
    $unityPathForInvoke = $UnityPath
    $unityExecutableBefore = $null
    try {
        $unityPathForInvoke = Find-UnityEditor -ProjectPath $project -UnityPath $UnityPath
        $unityExecutableBefore = Get-FileRecord -Path $unityPathForInvoke
    }
    catch { }
    $result = Invoke-UnityValidation -ProjectPath $project -Mode $Mode -UnityPath $unityPathForInvoke -TestFilter $TestFilter -TestCategory $TestCategory -TimeoutMinutes $TimeoutMinutes -ResultsDirectory $resultsDirectory -RetryOperationalFailure:$RetryOperationalFailure
    $finishedAt = [DateTime]::UtcNow
    $after = Get-UnityEvidenceInputSnapshot -ProjectPath $project
    $unityExecutableAfter = Get-FileRecord -Path $result.UnityPath
    $unityExecutableStable = ($null -ne $unityExecutableBefore -and $null -ne $unityExecutableAfter -and $unityExecutableBefore.sha256 -ceq $unityExecutableAfter.sha256 -and $unityExecutableBefore.bytes -eq $unityExecutableAfter.bytes)
    $attempts = [System.Collections.Generic.List[object]]::new()
    foreach ($attempt in @($result.AttemptHistory | Where-Object { $null -ne $_ })) {
        $xmlRecord = Get-FileRecord -Path $attempt.XmlPath
        $logRecord = Get-FileRecord -Path $attempt.LogPath
        $argumentVector = @()
        $publicArgumentVector = @()
        if (-not [string]::IsNullOrWhiteSpace($result.UnityPath) -and -not [string]::IsNullOrWhiteSpace($attempt.XmlPath) -and -not [string]::IsNullOrWhiteSpace($attempt.LogPath)) {
            $command = Build-UnityValidationCommand -UnityPath $result.UnityPath -ProjectPath $project -Mode $Mode -ResultXmlPath $attempt.XmlPath -LogPath $attempt.LogPath -TestFilter $TestFilter -TestCategory $TestCategory
            $argumentVector = @($command.Arguments)
            $publicArgumentVector = @(ConvertTo-PublicUnityArguments -Arguments $argumentVector)
        }
        $attempts.Add([ordered]@{
            status = [string]$attempt.Status; startedAtUtc = ConvertTo-EvidenceUtcText $attempt.StartedAt; durationMilliseconds = [long]([TimeSpan]$attempt.Duration).TotalMilliseconds
            processId = $attempt.ProcessId; exitCode = $attempt.ExitCode; total = [int]$attempt.TotalTests; passed = [int]$attempt.PassedTests; failed = [int]$attempt.FailedTests; skipped = [int]$attempt.SkippedTests; inconclusive = [int]$attempt.InconclusiveTests
            frameworkResult = $attempt.FrameworkResult; countsCoherent = [bool]$attempt.CountsCoherent; errorMessage = $attempt.ErrorMessage
            xmlPath = $attempt.XmlPath; xml = $xmlRecord; logPath = $attempt.LogPath; log = $logRecord; argumentVector = $argumentVector; publicArguments = $publicArgumentVector
        })
    }
    $localManifest = [ordered]@{
        schema = 'unity-validation-private-run-v1'; toolVersion = $script:EvidenceToolVersion; runId = $runId
        startedAtUtc = $startedAt.ToString('o'); finishedAtUtc = $finishedAt.ToString('o')
        projectPath = $project; unityPath = $result.UnityPath
        unityExecutable = $unityExecutableBefore
        unityExecutableStable = $unityExecutableStable
        runner = $runner
        invocation = [ordered]@{ mode = $Mode; testFilter = $TestFilter; testCategory = $TestCategory; selection = $(if ([string]::IsNullOrWhiteSpace($TestFilter) -and [string]::IsNullOrWhiteSpace($TestCategory)) { 'all' } else { 'filtered' }); timeoutMinutes = $TimeoutMinutes; retryOperationalFailure = [bool]$RetryOperationalFailure }
        inputBefore = $before; inputAfter = $after; inputStable = ($before.sha256 -ceq $after.sha256 -and $unityExecutableStable)
        status = [string]$result.Status; attempts = @($attempts.ToArray())
    }
    $manifestPath = Join-Path $runDirectory 'run-manifest.json'
    Write-EvidenceJson -Path $manifestPath -Value $localManifest
    return [pscustomobject]@{ RunId = $runId; Status = [string]$result.Status; ManifestPath = $manifestPath }
}

function Get-XmlAttributeRequired {
    param([Parameter(Mandatory)][System.Xml.XmlElement]$Element, [Parameter(Mandatory)][string]$Name)
    if (-not $Element.HasAttribute($Name) -or [string]::IsNullOrWhiteSpace($Element.GetAttribute($Name))) { throw "XML element '$($Element.LocalName)' is missing required attribute '$Name'." }
    return $Element.GetAttribute($Name)
}

function ConvertFrom-UnityResultLabel {
    param([Parameter(Mandatory)][string]$Value)
    switch -CaseSensitive ($Value) {
        'Passed' { return 'passed' }
        'Failed' { return 'failed' }
        'Error' { return 'failed' }
        'Skipped' { return 'skipped' }
        'Ignored' { return 'skipped' }
        'NotRunnable' { return 'skipped' }
        'Inconclusive' { return 'inconclusive' }
        default { throw "Unknown Unity/NUnit result semantics '$Value'." }
    }
}

function Get-UnityEvidenceReportFacts {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$XmlPath)
    $item = Get-Item -LiteralPath $XmlPath -ErrorAction Stop
    if ($item.Length -le 0 -or $item.Length -gt $script:MaximumXmlBytes) { throw "XML report size must be between 1 and $($script:MaximumXmlBytes) bytes." }
    $settings = [System.Xml.XmlReaderSettings]::new()
    $settings.DtdProcessing = [System.Xml.DtdProcessing]::Prohibit
    $settings.XmlResolver = $null
    $settings.MaxCharactersInDocument = $script:MaximumXmlBytes
    $reader = [System.Xml.XmlReader]::Create($XmlPath, $settings)
    $document = [System.Xml.XmlDocument]::new()
    $document.XmlResolver = $null
    try { $document.Load($reader) }
    finally { $reader.Dispose() }
    if ($null -eq $document.DocumentElement -or $document.DocumentElement.LocalName -ne 'test-run') { throw 'The XML root must be test-run.' }
    $root = $document.DocumentElement
    $requiredCounts = @('total', 'passed', 'failed', 'skipped', 'inconclusive', 'testcasecount')
    $rootFacts = [ordered]@{}
    foreach ($name in $requiredCounts) {
        $raw = Get-XmlAttributeRequired $root $name
        $parsed = 0
        if (-not [int]::TryParse($raw, [Globalization.NumberStyles]::None, [Globalization.CultureInfo]::InvariantCulture, [ref]$parsed) -or $parsed -lt 0) { throw "Root count '$name' is not a nonnegative integer." }
        $rootFacts[$name] = $parsed
    }
    $rootResult = Get-XmlAttributeRequired $root 'result'
    $null = ConvertFrom-UnityResultLabel $rootResult
    $testCases = @($root.SelectNodes(".//*[local-name()='test-case']"))
    if ($rootFacts.testcasecount -ne $testCases.Count) { throw 'Root testcasecount does not match the number of test-case records.' }
    $caseCounts = [ordered]@{ passed = 0; failed = 0; skipped = 0; inconclusive = 0 }
    $identitySet = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($case in $testCases) {
        if ($case -isnot [System.Xml.XmlElement]) { throw 'Malformed test-case node.' }
        foreach ($name in @('id', 'name', 'fullname', 'result', 'runstate')) { $null = Get-XmlAttributeRequired $case $name }
        $label = ConvertFrom-UnityResultLabel $case.GetAttribute('result')
        $caseCounts[$label]++
        $identity = $case.GetAttribute('id') + [char]0 + $case.GetAttribute('fullname')
        if (-not $identitySet.Add($identity)) { throw 'Duplicate test-case identity (id plus fullname) was found.' }
    }
    $suites = @($root.SelectNodes(".//*[local-name()='test-suite']"))
    foreach ($suite in $suites) {
        foreach ($name in @('id', 'type', 'name', 'fullname', 'result', 'runstate', 'testcasecount')) { $null = Get-XmlAttributeRequired $suite $name }
        $null = ConvertFrom-UnityResultLabel $suite.GetAttribute('result')
        $suiteCount = 0
        if (-not [int]::TryParse($suite.GetAttribute('testcasecount'), [Globalization.NumberStyles]::None, [Globalization.CultureInfo]::InvariantCulture, [ref]$suiteCount) -or $suiteCount -lt 0) { throw 'A suite testcasecount is not a nonnegative integer.' }
        $actualSuiteCount = @($suite.SelectNodes(".//*[local-name()='test-case']")).Count
        if ($suiteCount -ne $actualSuiteCount) { throw "Suite '$($suite.GetAttribute('fullname'))' testcasecount does not match its descendants." }
    }
    if ($rootFacts.total -ne ($caseCounts.passed + $caseCounts.failed + $caseCounts.skipped + $caseCounts.inconclusive)) { throw 'Root total does not equal the test-case outcome counts.' }
    foreach ($name in @('passed', 'failed', 'skipped', 'inconclusive')) { if ($rootFacts[$name] -ne $caseCounts[$name]) { throw "Root '$name' count does not match test-case outcomes." } }
    if ($rootFacts.testcasecount -ne $rootFacts.total) { throw 'Root testcasecount and total differ.' }
    return [ordered]@{ document = $document; result = $rootResult; counts = $rootFacts; cases = $testCases.Count; caseOutcomes = $caseCounts }
}

function ConvertTo-UnityXmlSemanticNode {
    param([Parameter(Mandatory)][System.Xml.XmlNode]$Node)
    switch ($Node.NodeType) {
        ([System.Xml.XmlNodeType]::Element) {
            $element = [System.Xml.XmlElement]$Node
            if ($element.LocalName -eq 'output') { return $null }
            $attributes = [System.Collections.Generic.List[object]]::new()
            foreach ($attribute in Get-OrdinalSortedXmlAttributes -Attributes @($element.Attributes)) {
                $attributes.Add([ordered]@{ namespaceUri = $attribute.NamespaceURI; localName = $attribute.LocalName; prefix = $attribute.Prefix; value = $attribute.Value })
            }
            $children = [System.Collections.Generic.List[object]]::new()
            foreach ($child in $element.ChildNodes) {
                $projected = ConvertTo-UnityXmlSemanticNode $child
                if ($null -eq $projected) { continue }
                if ($projected.type -eq 'text' -and $children.Count -gt 0 -and $children[$children.Count - 1].type -eq 'text') {
                    $children[$children.Count - 1].value = [string]$children[$children.Count - 1].value + [string]$projected.value
                }
                else { $children.Add($projected) }
            }
            return [ordered]@{ type = 'element'; namespaceUri = $element.NamespaceURI; localName = $element.LocalName; prefix = $element.Prefix; attributes = @($attributes.ToArray()); children = @($children.ToArray()) }
        }
        ([System.Xml.XmlNodeType]::Text) { return [ordered]@{ type = 'text'; value = $Node.Value } }
        ([System.Xml.XmlNodeType]::CDATA) { return [ordered]@{ type = 'cdata'; value = $Node.Value } }
        ([System.Xml.XmlNodeType]::Comment) { return [ordered]@{ type = 'comment'; value = $Node.Value } }
        ([System.Xml.XmlNodeType]::ProcessingInstruction) { return [ordered]@{ type = 'pi'; name = $Node.Name; value = $Node.Value } }
        ([System.Xml.XmlNodeType]::Whitespace) { return $null }
        ([System.Xml.XmlNodeType]::SignificantWhitespace) { return [ordered]@{ type = 'significant-whitespace'; value = $Node.Value } }
        default { return $null }
    }
}

function Get-OrdinalSortedXmlAttributes {
    param([Parameter(Mandatory)][AllowEmptyCollection()][System.Xml.XmlAttribute[]]$Attributes)
    $sorted = [System.Collections.Generic.SortedDictionary[string, System.Xml.XmlAttribute]]::new([StringComparer]::Ordinal)
    foreach ($attribute in $Attributes) {
        $key = [string]$attribute.NamespaceURI + [char]0 + [string]$attribute.LocalName + [char]0 + [string]$attribute.Prefix
        $sorted.Add($key, $attribute)
    }
    return @($sorted.Values)
}

function Get-UnityXmlSemanticHash {
    param([Parameter(Mandatory)][System.Xml.XmlDocument]$Document)
    $projection = ConvertTo-UnityXmlSemanticNode $Document.DocumentElement
    return Get-BytesSha256 (ConvertTo-EvidenceJsonBytes $projection)
}

function Assert-PublicInputSnapshot {
    param([Parameter(Mandatory)][System.Collections.IDictionary]$Snapshot)
    if ($Snapshot.schema -ne 'unity-validation-input-snapshot-v1') { throw 'Unsupported input snapshot schema.' }
    $seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($file in @($Snapshot.files)) {
        $path = [string]$file.path
        if ([string]::IsNullOrWhiteSpace($path) -or $path.Contains('\') -or $path.StartsWith('/') -or [System.IO.Path]::IsPathRooted($path) -or $path -match '(^|/)\.\.?(/|$)|//|[\r\n\x00]') { throw 'Input manifest contains an unsafe or absolute path.' }
        if (-not $seen.Add($path)) { throw 'Input manifest contains duplicate paths.' }
        if ([long]$file.bytes -lt 0 -or [string]$file.sha256 -notmatch '^[a-fA-F0-9]{64}$') { throw 'Input manifest contains an invalid file identity.' }
    }
    if ([string]$Snapshot.repository.head -notmatch '^[a-fA-F0-9]{40,64}$' -or [string]$Snapshot.repository.tree -notmatch '^[a-fA-F0-9]{40,64}$') { throw 'Input manifest contains an invalid Git object identity.' }
    if ($Snapshot.repository.origin -match '://[^/]*@') { throw 'Input manifest contains remote credentials.' }
}

function Write-NormalizedXmlNode {
    param([Parameter(Mandatory)][System.Xml.XmlNode]$Node, [Parameter(Mandatory)][System.Xml.XmlWriter]$Writer)
    switch ($Node.NodeType) {
        ([System.Xml.XmlNodeType]::Element) {
            $element = [System.Xml.XmlElement]$Node
            if ($element.LocalName -eq 'output') { return }
            $Writer.WriteStartElement($element.Prefix, $element.LocalName, $element.NamespaceURI)
            foreach ($attribute in Get-OrdinalSortedXmlAttributes -Attributes @($element.Attributes)) {
                if ($attribute.Prefix -eq 'xmlns' -or $attribute.Name -eq 'xmlns') { $Writer.WriteAttributeString($attribute.Prefix, $attribute.LocalName, 'http://www.w3.org/2000/xmlns/', $attribute.Value) }
                else { $Writer.WriteAttributeString($attribute.Prefix, $attribute.LocalName, $attribute.NamespaceURI, $attribute.Value) }
            }
            foreach ($child in $element.ChildNodes) { Write-NormalizedXmlNode $child $Writer }
            $Writer.WriteEndElement()
        }
        ([System.Xml.XmlNodeType]::Text) { $Writer.WriteString($Node.Value) }
        ([System.Xml.XmlNodeType]::CDATA) { $Writer.WriteCData($Node.Value) }
        ([System.Xml.XmlNodeType]::Comment) { $Writer.WriteComment($Node.Value) }
        ([System.Xml.XmlNodeType]::ProcessingInstruction) { $Writer.WriteProcessingInstruction($Node.Name, $Node.Value) }
        ([System.Xml.XmlNodeType]::Whitespace) { if (-not [string]::IsNullOrWhiteSpace($Node.Value)) { $Writer.WriteString($Node.Value) } }
        ([System.Xml.XmlNodeType]::SignificantWhitespace) { $Writer.WriteString($Node.Value) }
    }
}

function ConvertTo-NormalizedUnityReport {
    param([Parameter(Mandatory)][System.Xml.XmlDocument]$Document, [switch]$ClaimRequiresOutput)
    $outputs = @($Document.SelectNodes("//*[local-name()='output']"))
    if ($ClaimRequiresOutput) {
        foreach ($output in $outputs) { if (-not [string]::IsNullOrWhiteSpace($output.InnerText)) { throw 'The requested claim requires output that privacy policy strips; no package was written.' } }
    }
    $buffer = [System.IO.MemoryStream]::new()
    $writerSettings = [System.Xml.XmlWriterSettings]::new()
    $writerSettings.Encoding = [System.Text.UTF8Encoding]::new($false)
    $writerSettings.Indent = $false
    $writerSettings.NewLineHandling = [System.Xml.NewLineHandling]::None
    $writerSettings.OmitXmlDeclaration = $false
    $writer = [System.Xml.XmlWriter]::Create($buffer, $writerSettings)
    try {
        if ($null -ne $Document.FirstChild -and $Document.FirstChild.NodeType -eq [System.Xml.XmlNodeType]::XmlDeclaration) { $writer.WriteStartDocument() }
        foreach ($child in $Document.ChildNodes) { Write-NormalizedXmlNode $child $writer }
        $writer.Flush()
        return ,$buffer.ToArray()
    }
    finally { $writer.Dispose(); $buffer.Dispose() }
}

function Test-PublicEvidenceText {
    param([Parameter(Mandatory)][string]$Text)
    if ($Text -match '(?i)(?:\b[A-Z]:[\\/]|\\\\[^\s\\]+\\|(?<!:)//[^/\s]+/|/(?:Users|home|private|tmp|mnt|var|opt|root|workspace|builds|agent|work|app|repo|src|code|github|actions|etc)/)') { throw 'Public evidence contains an absolute local filesystem path.' }
    if ($Text -match '(?i)\b(?:password|passwd|secret|api[_-]?key|access[_-]?token|refresh[_-]?token|authorization|bearer)\b\s*[:= ]\s*(?:Bearer\s+)?[^\s,;]+') { throw 'Public evidence contains a credential-like value.' }
}

function Test-ProtectedEvidenceDirectory {
    param([Parameter(Mandatory)][string]$Path)
    if ($env:OS -ne 'Windows_NT') { throw 'Protected local evidence verification requires Windows ACL support.' }
    $full = [System.IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $full -PathType Container)) { throw 'Protected local evidence directory is missing.' }
    $item = Get-Item -LiteralPath $full -Force
    if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Protected local evidence directory cannot be a reparse point.' }
    Add-Type -AssemblyName System.IO.FileSystem.AccessControl
    $acl = [System.IO.FileSystemAclExtensions]::GetAccessControl([System.IO.DirectoryInfo]::new($full))
    $sid = [System.Security.Principal.WindowsIdentity]::GetCurrent().User
    if (-not $acl.AreAccessRulesProtected) { throw 'Protected local evidence directory inherits permissions.' }
    if ($acl.GetOwner([System.Security.Principal.SecurityIdentifier]).Value -ne $sid.Value) { throw 'Protected local evidence directory owner is not the current account.' }
    $rules = @($acl.GetAccessRules($true, $true, [System.Security.Principal.SecurityIdentifier]))
    if ($rules.Count -ne 1 -or (($rules[0].FileSystemRights -band [System.Security.AccessControl.FileSystemRights]::FullControl) -ne [System.Security.AccessControl.FileSystemRights]::FullControl)) { throw 'Protected local evidence directory does not grant exactly one full-control rule.' }
    foreach ($access in $rules) {
        if ($access.IdentityReference.Value -ne $sid.Value -or $access.AccessControlType -ne [System.Security.AccessControl.AccessControlType]::Allow) {
            throw 'Protected local evidence directory has an unexpected ACL principal or deny rule.'
        }
    }
    return $full
}

function Assert-ProtectedEvidenceFile {
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][string]$ProtectedRoot)
    $full = [System.IO.Path]::GetFullPath($Path)
    $root = [System.IO.Path]::GetFullPath($ProtectedRoot).TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    if (-not $full.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) { throw 'Private evidence file is outside its protected run directory.' }
    if (-not (Test-Path -LiteralPath $full -PathType Leaf)) { throw 'Private evidence file is missing.' }
    $item = Get-Item -LiteralPath $full -Force
    if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Private evidence file cannot be a reparse point.' }
    $directory = Split-Path -Parent $full
    while ($directory.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) {
        $null = Test-ProtectedEvidenceDirectory $directory
        $directory = Split-Path -Parent $directory
    }
    Add-Type -AssemblyName System.IO.FileSystem.AccessControl
    $acl = [System.IO.FileSystemAclExtensions]::GetAccessControl([System.IO.FileInfo]::new($full))
    $sid = [System.Security.Principal.WindowsIdentity]::GetCurrent().User
    if ($acl.GetOwner([System.Security.Principal.SecurityIdentifier]).Value -ne $sid.Value) { throw 'Private evidence file owner is not the current account.' }
    $rules = @($acl.GetAccessRules($true, $true, [System.Security.Principal.SecurityIdentifier]))
    if ($rules.Count -lt 1) { throw 'Private evidence file has no access rule.' }
    foreach ($access in $rules) {
        if ($access.IdentityReference.Value -ne $sid.Value -or $access.AccessControlType -ne [System.Security.AccessControl.AccessControlType]::Allow) {
            throw 'Private evidence file has an unexpected ACL principal or deny rule.'
        }
    }
    return $full
}

function Assert-OriginalGateOutcome {
    param([Parameter(Mandatory)][string]$XmlPath, [Parameter(Mandatory)][System.Collections.IDictionary]$Attempt)
    Import-Module (Join-Path $PSScriptRoot 'UnityValidation.psm1') -Force
    $harnessResult = Read-UnityTestResultXml -XmlPath $XmlPath
    if (-not $harnessResult.IsValid) { throw 'Legacy harness does not accept the retained original XML.' }
    foreach ($name in @('TotalTests', 'PassedTests', 'FailedTests', 'SkippedTests', 'InconclusiveTests')) {
        $privateName = switch ($name) { 'TotalTests' { 'total' } 'PassedTests' { 'passed' } 'FailedTests' { 'failed' } 'SkippedTests' { 'skipped' } 'InconclusiveTests' { 'inconclusive' } }
        if ([int]$harnessResult.$name -ne [int]$Attempt[$privateName]) { throw "Original XML and harness report disagree on $privateName." }
    }
    if ([bool]$harnessResult.CountsCoherent -ne [bool]$Attempt.countsCoherent -or [string]$harnessResult.FrameworkResult -cne [string]$Attempt.frameworkResult) { throw 'Original XML framework result or count-coherence value differs from the retained harness result.' }
    $timedOut = ([string]$Attempt.status -eq 'TimedOut')
    $expectedStatus = Resolve-UnityValidationStatus -XmlResult $harnessResult -TimedOut:$timedOut
    if ([string]$Attempt.status -cne $expectedStatus) { throw 'Original XML does not reproduce the retained Unity gate status.' }
}

function ConvertTo-EvidenceUtcText {
    param([Parameter(Mandatory)][object]$Value)
    $date = [DateTime]::Parse([string]$Value, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::RoundtripKind)
    return $date.ToUniversalTime().ToString('o', [Globalization.CultureInfo]::InvariantCulture)
}

function Get-RunManifestPublicRecord {
    param([Parameter(Mandatory)][string]$ManifestPath, [switch]$ClaimRequiresOutput)
    $fullManifestPath = [System.IO.Path]::GetFullPath($ManifestPath)
    if ((Get-Item -LiteralPath $fullManifestPath -ErrorAction Stop).Length -gt 536870912) { throw 'Private run manifest exceeds the size limit.' }
    $privateRoot = Split-Path -Parent $fullManifestPath
    $null = Test-ProtectedEvidenceDirectory $privateRoot
    $null = Assert-ProtectedEvidenceFile -Path $fullManifestPath -ProtectedRoot $privateRoot
    $private = Get-Content -LiteralPath $fullManifestPath -Raw | ConvertFrom-Json -AsHashtable -DateKind String
    if ($private.schema -ne 'unity-validation-private-run-v1') { throw 'Unsupported private run manifest schema.' }
    if ([string]$private.runId -notmatch '^[a-fA-F0-9]{32}$') { throw 'Run id must be a 32-character hexadecimal identifier.' }
    if ($private.runner.powershellEdition -ne 'Core' -or $private.runner.powershellVersion -ne '7.6.5' -or $private.runner.dotnetVersion -ne '10.0.11' -or $private.runner.compressionAssemblyVersion -cne [System.IO.Compression.ZipArchive].Assembly.GetName().Version.ToString()) { throw 'Run was captured under a packer runtime outside the pinned toolchain.' }
    if (-not $private.inputStable -or -not $private.unityExecutableStable) { throw "Run $($private.runId) changed validation inputs or Unity executable during execution." }
    Assert-PublicInputSnapshot -Snapshot $private.inputBefore
    Assert-PublicInputSnapshot -Snapshot $private.inputAfter
    if (@($private.attempts).Count -eq 0) { throw "Run $($private.runId) has no recorded attempts." }
    if (@($private.attempts).Count -gt 2) { throw "Run $($private.runId) exceeds the harness retry limit." }
    if (@($private.attempts).Count -gt 1 -and -not $private.invocation.retryOperationalFailure) { throw "Run $($private.runId) contains a retry not authorized by its invocation." }
    if ([string]$private.status -cne [string]$private.attempts[-1].status) { throw "Run $($private.runId) status does not match its final attempt." }
    $attemptRecords = [System.Collections.Generic.List[object]]::new()
    $normalizedEntries = [System.Collections.Generic.List[object]]::new()
    $lastAttemptHasXml = $false
    foreach ($attempt in @($private.attempts)) {
        if ([string]$attempt.status -notin @('Passed', 'Failed', 'TimedOut', 'NoResultXml', 'InvalidResultXml', 'UnityLaunchFailed', 'OperationalFailure', 'ProjectAlreadyOpen')) { throw "Unknown Unity validation gate status '$($attempt.status)'." }
        $record = [ordered]@{
            status = [string]$attempt.status; startedAtUtc = ConvertTo-EvidenceUtcText $attempt.startedAtUtc; durationMilliseconds = [long]$attempt.durationMilliseconds
            exitCode = $attempt.exitCode; total = [int]$attempt.total; passed = [int]$attempt.passed; failed = [int]$attempt.failed; skipped = [int]$attempt.skipped; inconclusive = [int]$attempt.inconclusive
            frameworkResult = $attempt.frameworkResult; countsCoherent = [bool]$attempt.countsCoherent; errorMessage = $attempt.errorMessage
            originalXml = $attempt.xml
            effectiveArguments = @($attempt.publicArguments)
        }
        if ($null -ne $attempt.xml) {
            $lastAttemptHasXml = $true
            $originalPath = [string]$attempt.xmlPath
            $null = Assert-ProtectedEvidenceFile -Path $originalPath -ProtectedRoot $privateRoot
            if (-not (Test-Path -LiteralPath $originalPath -PathType Leaf)) { throw "Original XML for run $($private.runId) is missing." }
            $actualXml = Get-FileRecord $originalPath
            if ($actualXml.sha256 -cne $attempt.xml.sha256 -or $actualXml.bytes -ne $attempt.xml.bytes) { throw "Original XML integrity mismatch for run $($private.runId)." }
            $facts = Get-UnityEvidenceReportFacts $originalPath
            if (-not $attempt.countsCoherent -or $facts.counts.total -ne [int]$attempt.total -or $facts.counts.passed -ne [int]$attempt.passed -or $facts.counts.failed -ne [int]$attempt.failed -or $facts.counts.skipped -ne [int]$attempt.skipped -or $facts.counts.inconclusive -ne [int]$attempt.inconclusive) { throw "Original XML does not agree with the harness result for run $($private.runId)." }
            Assert-OriginalGateOutcome -XmlPath $originalPath -Attempt $attempt
            $normalized = ConvertTo-NormalizedUnityReport -Document $facts.document -ClaimRequiresOutput:$ClaimRequiresOutput
            $normalizedText = [System.Text.UTF8Encoding]::new($false, $true).GetString($normalized)
            Test-PublicEvidenceText $normalizedText
            $normalizedTemp = [System.IO.Path]::GetTempFileName()
            try {
                [System.IO.File]::WriteAllBytes($normalizedTemp, $normalized)
                $normalizedFacts = Get-UnityEvidenceReportFacts -XmlPath $normalizedTemp
                if ((Get-UnityXmlSemanticHash $facts.document) -cne (Get-UnityXmlSemanticHash $normalizedFacts.document)) { throw "Normalized XML changed required report semantics for run $($private.runId)." }
            }
            finally { Remove-Item -LiteralPath $normalizedTemp -Force -ErrorAction SilentlyContinue }
            $entryName = 'results/{0}/attempt-{1:D2}.xml' -f $private.runId, $attemptRecords.Count
            $record.normalizedXml = [ordered]@{ entry = $entryName; bytes = [long]$normalized.Length; sha256 = (Get-BytesSha256 $normalized) }
            $normalizedEntries.Add([ordered]@{ name = $entryName; bytes = $normalized })
            $record.reportResult = $facts.result
            $record.caseCount = $facts.cases
        }
        elseif ([string]$attempt.status -eq 'Passed' -or [long]$attempt.total -gt 0) {
            $lastAttemptHasXml = $false
            throw "Attempt for run $($private.runId) is missing its required XML report."
        }
        else { $lastAttemptHasXml = $false }
        if ($null -ne $attempt.log) { $null = Assert-ProtectedEvidenceFile -Path ([string]$attempt.logPath) -ProtectedRoot $privateRoot }
        $attemptRecords.Add($record)
    }
    if (-not $lastAttemptHasXml) { throw "Final attempt for run $($private.runId) has no result XML." }
    $publicRun = [ordered]@{
        runId = [string]$private.runId; startedAtUtc = ConvertTo-EvidenceUtcText $private.startedAtUtc; finishedAtUtc = ConvertTo-EvidenceUtcText $private.finishedAtUtc
        inputSnapshot = $private.inputBefore; unityExecutable = $private.unityExecutable; runner = $private.runner; invocation = $private.invocation; status = [string]$private.status; attempts = @($attemptRecords.ToArray())
    }
    $publicBytes = ConvertTo-EvidenceJsonBytes $publicRun
    Test-PublicEvidenceText ([System.Text.UTF8Encoding]::new($false, $true).GetString($publicBytes))
    return [ordered]@{ record = $publicRun; entries = @($normalizedEntries.ToArray()) }
}

function Write-DeterministicEvidenceArchive {
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][object[]]$Entries)
    Add-Type -AssemblyName System.IO.Compression
    $stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
    $completed = $false
    try {
        $archive = [System.IO.Compression.ZipArchive]::new($stream, [System.IO.Compression.ZipArchiveMode]::Create, $true)
        try {
            foreach ($item in @($Entries | Sort-Object name -CaseSensitive)) {
                $entry = $archive.CreateEntry([string]$item.name, [System.IO.Compression.CompressionLevel]::Optimal)
                $entry.LastWriteTime = [DateTimeOffset]::new(1980, 1, 1, 0, 0, 0, [TimeSpan]::Zero)
                $entry.ExternalAttributes = 0
                $entryStream = $entry.Open()
                try { $data = [byte[]]$item.bytes; $entryStream.Write($data, 0, $data.Length) }
                finally { $entryStream.Dispose() }
            }
        }
        finally { $archive.Dispose() }
        $completed = $true
    }
    finally {
        $stream.Dispose()
        if (-not $completed -and (Test-Path -LiteralPath $Path)) { Remove-Item -LiteralPath $Path -Force -ErrorAction SilentlyContinue }
    }
}

function New-UnityValidationEvidencePackage {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string[]]$RunManifestPath,
        [Parameter(Mandatory)][string]$OutputDirectory,
        [string]$BundleId = ([Guid]::NewGuid().ToString('N')),
        [switch]$ClaimRequiresOutput
    )
    $packerRuntime = Assert-EvidenceRuntime
    if ($BundleId -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,79}$') { throw 'BundleId may contain only letters, digits, period, underscore, or hyphen.' }
    $out = [System.IO.Path]::GetFullPath($OutputDirectory)
    New-Item -ItemType Directory -Path $out -Force | Out-Null
    $records = [System.Collections.Generic.List[object]]::new()
    $entries = [System.Collections.Generic.List[object]]::new()
    $ids = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $paths = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($path in $RunManifestPath) {
        $full = [System.IO.Path]::GetFullPath($path)
        if (-not $paths.Add($full)) { throw 'Duplicate run manifest path was supplied.' }
        $item = Get-Content -LiteralPath $full -Raw | ConvertFrom-Json -AsHashtable -DateKind String
        if (-not $ids.Add([string]$item.runId)) { throw 'Duplicate runId was supplied.' }
        $prepared = Get-RunManifestPublicRecord -ManifestPath $full -ClaimRequiresOutput:$ClaimRequiresOutput
        $records.Add($prepared.record)
        foreach ($entry in $prepared.entries) { $entries.Add($entry) }
    }
    if ($records.Count -eq 0) { throw 'At least one run manifest is required.' }
    if ($records.Count -gt 1000) { throw 'Package exceeds the run-count limit.' }
    $expandedBytes = 0L
    foreach ($entry in $entries) {
        $expandedBytes += ([byte[]]$entry.bytes).LongLength
        if ($expandedBytes -gt $script:MaximumArchiveExpandedBytes) { throw 'Package exceeds the total expanded-result size limit.' }
    }
    $entryNames = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in $entries) { if (-not $entryNames.Add([string]$entry.name)) { throw 'Duplicate archive entry name.' } }
    $archivePath = Join-Path $out "$BundleId.zip"
    $manifestPath = Join-Path $out "$BundleId.manifest.json"
    if ((Test-Path -LiteralPath $archivePath) -or (Test-Path -LiteralPath $manifestPath)) { throw 'Bundle output already exists; choose a new BundleId.' }
    $archiveCreated = $false
    $manifestCreated = $false
    try {
        Write-DeterministicEvidenceArchive -Path $archivePath -Entries @($entries.ToArray())
        $archiveCreated = $true
        $archiveRecord = Get-FileRecord $archivePath
        $manifest = [ordered]@{
            schema = 'unity-validation-evidence-package-v1'; toolVersion = $script:EvidenceToolVersion; bundleId = $BundleId; packerRuntime = $packerRuntime
            claimRequiresOutput = [bool]$ClaimRequiresOutput; outputPolicy = 'remove-output-elements'
            archive = [ordered]@{ file = "$BundleId.zip"; bytes = $archiveRecord.bytes; sha256 = $archiveRecord.sha256; format = 'zip-deflate-fixed-1980-01-01'; entryPolicy = 'normalized-test-result-xml-only' }
            runs = @($records.ToArray())
        }
        $manifestBytes = ConvertTo-EvidenceJsonBytes $manifest
        Test-PublicEvidenceText ([System.Text.UTF8Encoding]::new($false, $true).GetString($manifestBytes))
        $manifestStream = [System.IO.File]::Open($manifestPath, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
        $manifestCreated = $true
        try { $manifestStream.Write($manifestBytes, 0, $manifestBytes.Length) }
        finally { $manifestStream.Dispose() }
        $verified = Test-UnityValidationEvidencePackage -ManifestPath $manifestPath -ArchivePath $archivePath
        if (-not $verified.IsValid) { throw "Package verification failed: $($verified.Error)" }
    }
    catch {
        if ($archiveCreated -and (Test-Path -LiteralPath $archivePath)) { Remove-Item -LiteralPath $archivePath -Force }
        if ($manifestCreated -and (Test-Path -LiteralPath $manifestPath)) { Remove-Item -LiteralPath $manifestPath -Force }
        throw
    }
    return [pscustomobject]@{ ManifestPath = $manifestPath; ArchivePath = $archivePath; BundleId = $BundleId; ArchiveSha256 = $archiveRecord.sha256; ArchiveBytes = $archiveRecord.bytes }
}

function Test-UnityValidationEvidencePackage {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$ManifestPath, [Parameter(Mandatory)][string]$ArchivePath, [string[]]$RunManifestPath)
    try {
        $currentRuntime = Assert-EvidenceRuntime
        if ((Get-Item -LiteralPath $ManifestPath -ErrorAction Stop).Length -gt $script:MaximumManifestBytes) { throw 'Package manifest exceeds the size limit.' }
        $manifestText = Get-Content -LiteralPath $ManifestPath -Raw
        Test-PublicEvidenceText $manifestText
        $manifest = $manifestText | ConvertFrom-Json -AsHashtable -DateKind String
        if ($manifest.schema -ne 'unity-validation-evidence-package-v1') { throw 'Unsupported package manifest schema.' }
        if ($manifest.outputPolicy -ne 'remove-output-elements') { throw 'Unsupported result-output privacy policy.' }
        foreach ($key in @('powershellEdition', 'powershellVersion', 'dotnetVersion', 'compressionAssemblyVersion')) { if ([string]$manifest.packerRuntime[$key] -cne [string]$currentRuntime[$key]) { throw 'Package was produced with a different pinned packer toolchain.' } }
        $archiveFile = Get-Item -LiteralPath $ArchivePath -ErrorAction Stop
        if ($archiveFile.Length -gt 2147483648) { throw 'Archive exceeds the size limit.' }
        $archiveHash = Get-EvidenceSha256 $ArchivePath
        if ($archiveHash -cne $manifest.archive.sha256 -or $archiveFile.Length -ne [long]$manifest.archive.bytes) { throw 'Archive hash or length does not match the manifest.' }
        Add-Type -AssemblyName System.IO.Compression
        $stream = [System.IO.File]::OpenRead($ArchivePath)
        try {
            $archive = [System.IO.Compression.ZipArchive]::new($stream, [System.IO.Compression.ZipArchiveMode]::Read, $true)
            try {
                $expected = [System.Collections.Generic.Dictionary[string, object]]::new([StringComparer]::OrdinalIgnoreCase)
                $runIds = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
                foreach ($run in @($manifest.runs)) {
                    if ([string]$run.runId -notmatch '^[a-fA-F0-9]{32}$') { throw 'Manifest contains an invalid run id.' }
                    if (-not $runIds.Add([string]$run.runId)) { throw 'Manifest contains duplicate run entries.' }
                    Assert-PublicInputSnapshot -Snapshot $run.inputSnapshot
                    $snapshotCore = [ordered]@{ schema = $run.inputSnapshot.schema; repository = $run.inputSnapshot.repository; unityEditorVersion = $run.inputSnapshot.unityEditorVersion; files = @($run.inputSnapshot.files) }
                    if ((Get-BytesSha256 (ConvertTo-EvidenceJsonBytes $snapshotCore)) -cne $run.inputSnapshot.sha256) { throw "Input inventory integrity mismatch for run $($run.runId)." }
                    foreach ($attempt in @($run.attempts)) {
                        if ($null -ne $attempt.normalizedXml) {
                            $entryName = [string]$attempt.normalizedXml.entry
                            if ($entryName -notmatch ('^results/' + [regex]::Escape([string]$run.runId) + '/attempt-\d{2,}\.xml$')) { throw 'Manifest contains a result entry outside the required package namespace.' }
                            $expected.Add($entryName, $attempt.normalizedXml)
                        }
                    }
                }
                if ($archive.Entries.Count -gt 10000 -or $archive.Entries.Count -ne $expected.Count) { throw 'Archive entry count does not match the manifest or exceeds the limit.' }
                $seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
                $actualEntryBytes = [System.Collections.Generic.Dictionary[string, byte[]]]::new([StringComparer]::OrdinalIgnoreCase)
                $expandedBytes = 0L
                foreach ($entry in $archive.Entries) {
                    $name = [string]$entry.FullName
                    if ($name.Contains('\') -or $name.StartsWith('/') -or $name -match '(^|/)\.\.?(/|$)' -or [System.IO.Path]::IsPathRooted($name) -or $entry.Name.Length -eq 0) { throw "Unsafe archive entry '$name'." }
                    if ((($entry.ExternalAttributes -shr 16) -band 0xF000) -eq 0xA000) { throw "Symbolic link entry '$name' is not permitted." }
                    if (-not $seen.Add($name)) { throw "Duplicate archive entry '$name'." }
                    if (-not $expected.ContainsKey($name)) { throw "Unexpected archive entry '$name'." }
                    if ($entry.Length -gt $script:MaximumXmlBytes) { throw "Archive entry '$name' exceeds the XML size limit." }
                    $entryStream = $entry.Open()
                    $memory = [System.IO.MemoryStream]::new()
                    try {
                        $chunk = [byte[]]::new(81920)
                        while (($read = $entryStream.Read($chunk, 0, $chunk.Length)) -gt 0) {
                            if ($memory.Length + $read -gt $script:MaximumXmlBytes) { throw "Archive entry '$name' exceeds the expanded XML size limit." }
                            $memory.Write($chunk, 0, $read)
                        }
                        $bytes = $memory.ToArray()
                    }
                    finally { $memory.Dispose(); $entryStream.Dispose() }
                    $expandedBytes += $bytes.LongLength
                    if ($expandedBytes -gt $script:MaximumArchiveExpandedBytes) { throw 'Archive exceeds the total expanded-result size limit.' }
                    $actualEntryBytes.Add($name, $bytes)
                    $record = $expected[$name]
                    if ($bytes.Length -ne [long]$record.bytes -or (Get-BytesSha256 $bytes) -cne $record.sha256) { throw "Entry integrity mismatch for '$name'." }
                    $tempXml = [System.IO.Path]::GetTempFileName()
                    try {
                        [System.IO.File]::WriteAllBytes($tempXml, $bytes)
                        $facts = Get-UnityEvidenceReportFacts $tempXml
                        $normalized = ConvertTo-NormalizedUnityReport -Document $facts.document
                        if ((Get-BytesSha256 $normalized) -cne $record.sha256) { throw "Entry '$name' is not in normalized deterministic form." }
                    }
                    finally { Remove-Item -LiteralPath $tempXml -Force -ErrorAction SilentlyContinue }
                    Test-PublicEvidenceText ([System.Text.UTF8Encoding]::new($false, $true).GetString($bytes))
                }
                if ($seen.Count -ne $expected.Count) { throw 'Archive entries are missing.' }
                $reproPath = [System.IO.Path]::GetTempFileName()
                Remove-Item -LiteralPath $reproPath -Force
                try {
                    $reproEntries = [System.Collections.Generic.List[object]]::new()
                    foreach ($name in $expected.Keys) { $reproEntries.Add([ordered]@{ name = $name; bytes = $actualEntryBytes[$name] }) }
                    Write-DeterministicEvidenceArchive -Path $reproPath -Entries @($reproEntries.ToArray())
                    $reproRecord = Get-FileRecord $reproPath
                    if ($reproRecord.bytes -ne $archiveFile.Length -or $reproRecord.sha256 -cne $archiveHash) { throw 'Archive metadata or byte ordering is not canonical for this packer toolchain.' }
                }
                finally { Remove-Item -LiteralPath $reproPath -Force -ErrorAction SilentlyContinue }
            }
            finally { $archive.Dispose() }
        }
        finally { $stream.Dispose() }
        if ($RunManifestPath) {
            $expectedRaw = @{}
            foreach ($path in $RunManifestPath) {
                $fullPrivatePath = [System.IO.Path]::GetFullPath($path)
                $privateRoot = Split-Path -Parent $fullPrivatePath
                $null = Test-ProtectedEvidenceDirectory $privateRoot
                $null = Assert-ProtectedEvidenceFile -Path $fullPrivatePath -ProtectedRoot $privateRoot
                $private = Get-Content -LiteralPath $fullPrivatePath -Raw | ConvertFrom-Json -AsHashtable -DateKind String
                if ($expectedRaw.ContainsKey([string]$private.runId)) { throw 'Duplicate private source run id was supplied.' }
                $expectedRaw[[string]$private.runId] = [ordered]@{ manifest = $private; root = $privateRoot }
            }
            foreach ($run in @($manifest.runs)) {
                if (-not $expectedRaw.ContainsKey([string]$run.runId)) { throw "Private source run $($run.runId) was not supplied." }
                $privateSource = $expectedRaw[[string]$run.runId]
                $private = $privateSource.manifest
                $privateRoot = [string]$privateSource.root
                if ($private.inputBefore.sha256 -cne $run.inputSnapshot.sha256 -or -not $private.inputStable -or -not $private.unityExecutableStable) { throw "Private source identity mismatch for run $($run.runId)." }
                if ($private.status -cne $run.status -or (ConvertTo-EvidenceUtcText $private.startedAtUtc) -cne (ConvertTo-EvidenceUtcText $run.startedAtUtc) -or (ConvertTo-EvidenceUtcText $private.finishedAtUtc) -cne (ConvertTo-EvidenceUtcText $run.finishedAtUtc)) { throw "Private gate result or run interval mismatch for run $($run.runId): status $($private.status)/$($run.status), start $(ConvertTo-EvidenceUtcText $private.startedAtUtc)/$(ConvertTo-EvidenceUtcText $run.startedAtUtc), finish $(ConvertTo-EvidenceUtcText $private.finishedAtUtc)/$(ConvertTo-EvidenceUtcText $run.finishedAtUtc)." }
                if ((Get-BytesSha256 (ConvertTo-EvidenceJsonBytes $private.invocation)) -cne (Get-BytesSha256 (ConvertTo-EvidenceJsonBytes $run.invocation))) { throw "Private invocation selection mismatch for run $($run.runId)." }
                if ((Get-BytesSha256 (ConvertTo-EvidenceJsonBytes $private.runner)) -cne (Get-BytesSha256 (ConvertTo-EvidenceJsonBytes $run.runner))) { throw "Private toolchain identity mismatch for run $($run.runId)." }
                if ((Get-BytesSha256 (ConvertTo-EvidenceJsonBytes $private.unityExecutable)) -cne (Get-BytesSha256 (ConvertTo-EvidenceJsonBytes $run.unityExecutable))) { throw "Private Unity executable identity mismatch for run $($run.runId)." }
                if (@($private.attempts).Count -ne @($run.attempts).Count) { throw "Private attempt count mismatch for run $($run.runId)." }
                $snapshotCore = [ordered]@{ schema = $run.inputSnapshot.schema; repository = $run.inputSnapshot.repository; unityEditorVersion = $run.inputSnapshot.unityEditorVersion; files = @($run.inputSnapshot.files) }
                if ((Get-BytesSha256 (ConvertTo-EvidenceJsonBytes $snapshotCore)) -cne $run.inputSnapshot.sha256) { throw "Public input inventory integrity mismatch for run $($run.runId)." }
                for ($index = 0; $index -lt @($run.attempts).Count; $index++) {
                    $publicAttempt = $run.attempts[$index]
                    $privateAttempt = $private.attempts[$index]
                    $privateAttemptView = [ordered]@{ status = $privateAttempt.status; startedAtUtc = ConvertTo-EvidenceUtcText $privateAttempt.startedAtUtc; durationMilliseconds = $privateAttempt.durationMilliseconds; exitCode = $privateAttempt.exitCode; total = $privateAttempt.total; passed = $privateAttempt.passed; failed = $privateAttempt.failed; skipped = $privateAttempt.skipped; inconclusive = $privateAttempt.inconclusive; frameworkResult = $privateAttempt.frameworkResult; countsCoherent = $privateAttempt.countsCoherent; errorMessage = $privateAttempt.errorMessage; originalXml = $privateAttempt.xml; effectiveArguments = @(ConvertTo-PublicUnityArguments -Arguments @($privateAttempt.argumentVector)) }
                    $publicAttemptView = [ordered]@{ status = $publicAttempt.status; startedAtUtc = $publicAttempt.startedAtUtc; durationMilliseconds = $publicAttempt.durationMilliseconds; exitCode = $publicAttempt.exitCode; total = $publicAttempt.total; passed = $publicAttempt.passed; failed = $publicAttempt.failed; skipped = $publicAttempt.skipped; inconclusive = $publicAttempt.inconclusive; frameworkResult = $publicAttempt.frameworkResult; countsCoherent = $publicAttempt.countsCoherent; errorMessage = $publicAttempt.errorMessage; originalXml = $publicAttempt.originalXml; effectiveArguments = @($publicAttempt.effectiveArguments) }
                    if ((Get-BytesSha256 (ConvertTo-EvidenceJsonBytes $privateAttemptView)) -cne (Get-BytesSha256 (ConvertTo-EvidenceJsonBytes $publicAttemptView))) { throw "Private attempt result or command mismatch for run $($run.runId): private=$(ConvertTo-Json -InputObject $privateAttemptView -Compress -Depth 20) public=$(ConvertTo-Json -InputObject $publicAttemptView -Compress -Depth 20)" }
                    if (($null -eq $privateAttempt.xml) -ne ($null -eq $publicAttempt.originalXml)) { throw "Private XML presence mismatch for run $($run.runId)." }
                    if ($null -ne $publicAttempt.originalXml) {
                        $null = Assert-ProtectedEvidenceFile -Path ([string]$privateAttempt.xmlPath) -ProtectedRoot $privateRoot
                        $raw = Get-FileRecord ([string]$privateAttempt.xmlPath)
                        if ($null -eq $raw -or $raw.sha256 -cne $publicAttempt.originalXml.sha256 -or $raw.bytes -ne [long]$publicAttempt.originalXml.bytes) { throw "Retained original XML mismatch for run $($run.runId)." }
                        $facts = Get-UnityEvidenceReportFacts ([string]$privateAttempt.xmlPath)
                        if ($facts.counts.total -ne [int]$publicAttempt.total -or $facts.counts.passed -ne [int]$publicAttempt.passed -or $facts.counts.failed -ne [int]$publicAttempt.failed -or $facts.counts.skipped -ne [int]$publicAttempt.skipped -or $facts.counts.inconclusive -ne [int]$publicAttempt.inconclusive) { throw "Retained source report outcome mismatch for run $($run.runId)." }
                        Assert-OriginalGateOutcome -XmlPath ([string]$privateAttempt.xmlPath) -Attempt $privateAttempt
                $normalizedSource = ConvertTo-NormalizedUnityReport -Document $facts.document -ClaimRequiresOutput:([bool]$manifest.claimRequiresOutput)
                        $normalizedRecord = $publicAttempt.normalizedXml
                        if ($null -eq $normalizedRecord -or $normalizedSource.Length -ne [long]$normalizedRecord.bytes -or (Get-BytesSha256 $normalizedSource) -cne $normalizedRecord.sha256) { throw "Retained source report normalization mismatch for run $($run.runId)." }
                $normalizedTemp = [System.IO.Path]::GetTempFileName()
                try {
                    [System.IO.File]::WriteAllBytes($normalizedTemp, $normalizedSource)
                    $normalizedFacts = Get-UnityEvidenceReportFacts -XmlPath $normalizedTemp
                    if ((Get-UnityXmlSemanticHash $facts.document) -cne (Get-UnityXmlSemanticHash $normalizedFacts.document)) { throw "Retained report semantics changed for run $($run.runId)." }
                }
                finally { Remove-Item -LiteralPath $normalizedTemp -Force -ErrorAction SilentlyContinue }
                        $archivedSource = $actualEntryBytes[[string]$normalizedRecord.entry]
                        if ($archivedSource.Length -ne $normalizedSource.Length -or (Get-BytesSha256 $archivedSource) -cne (Get-BytesSha256 $normalizedSource)) { throw "Archived report does not match retained source XML for run $($run.runId)." }
                    }
                }
            }
        }
        return [pscustomobject]@{ IsValid = $true; Error = $null; RunCount = @($manifest.runs).Count; EntryCount = $expected.Count; ArchiveSha256 = $archiveHash }
    }
    catch { return [pscustomobject]@{ IsValid = $false; Error = $_.Exception.Message; RunCount = 0; EntryCount = 0; ArchiveSha256 = $null } }
}

function Read-UnityValidationEvidencePackage {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$ManifestPath)
    $null = Assert-EvidenceRuntime
    if ((Get-Item -LiteralPath $ManifestPath -ErrorAction Stop).Length -gt $script:MaximumManifestBytes) { throw 'Package manifest exceeds the size limit.' }
    $manifestText = Get-Content -LiteralPath $ManifestPath -Raw
    Test-PublicEvidenceText $manifestText
    $manifest = $manifestText | ConvertFrom-Json -AsHashtable -DateKind String
    if ($manifest.schema -ne 'unity-validation-evidence-package-v1') { throw 'Unsupported package manifest schema.' }
    if ($manifest.outputPolicy -ne 'remove-output-elements') { throw 'Unsupported result-output privacy policy.' }
    foreach ($run in @($manifest.runs)) {
        $attemptSummary = [System.Collections.Generic.List[object]]::new()
        foreach ($attempt in @($run.attempts)) {
            $attemptSummary.Add([pscustomobject]@{ Status = $attempt.status; Total = $attempt.total; Passed = $attempt.passed; Failed = $attempt.failed; Skipped = $attempt.skipped; Inconclusive = $attempt.inconclusive; HasReport = ($null -ne $attempt.normalizedXml) })
        }
        [pscustomobject]@{ RunId = $run.runId; GateStatus = $run.status; Mode = $run.invocation.mode; StartedAtUtc = $run.startedAtUtc; Attempts = @($attemptSummary.ToArray()); InputSha256 = $run.inputSnapshot.sha256 }
    }
}

Export-ModuleMember -Function @(
    'Get-UnityEvidenceInputSnapshot', 'Invoke-UnityValidationEvidenceCapture', 'New-UnityValidationEvidencePackage',
    'Test-UnityValidationEvidencePackage', 'Read-UnityValidationEvidencePackage', 'Get-UnityEvidenceReportFacts',
    'ConvertTo-NormalizedUnityReport', 'Get-UnityXmlSemanticHash', 'ConvertTo-EvidenceJsonBytes', 'Get-BytesSha256', 'Set-ProtectedEvidenceDirectory', 'Test-ProtectedEvidenceDirectory', 'ConvertTo-PublicRemoteIdentity'
)
