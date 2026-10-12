[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Import-Module (Join-Path $PSScriptRoot '..\UnityValidationEvidence.psm1') -Force

$script:Assertions = 0
$assetFixture = $null
$packageFixture = $null
$root = Join-Path ([System.IO.Path]::GetTempPath()) ('unity-evidence-selftest-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root -Force | Out-Null

function Assert-True([bool]$Condition, [string]$Message) {
    $script:Assertions++
    if (-not $Condition) { throw "ASSERTION FAILED: $Message" }
}

function Assert-Throws([scriptblock]$Action, [string]$Message) {
    $script:Assertions++
    $threw = $false
    try { & $Action | Out-Null } catch { $threw = $true }
    if (-not $threw) { throw "ASSERTION FAILED: $Message" }
}

function New-TestXml {
    param([Parameter(Mandatory)][string[]]$Outcomes, [switch]$WithSensitiveOutput)
    $totals = [ordered]@{ Passed = 0; Failed = 0; Skipped = 0; Inconclusive = 0 }
    foreach ($outcome in $Outcomes) {
        switch ($outcome) {
            'Passed' { $totals.Passed++ }
            { $_ -in @('Failed', 'Error') } { $totals.Failed++ }
            { $_ -in @('Skipped', 'Ignored', 'NotRunnable') } { $totals.Skipped++ }
            'Inconclusive' { $totals.Inconclusive++ }
            default { }
        }
    }
    $result = if ($totals.Failed -gt 0) { 'Failed' } elseif ($totals.Inconclusive -gt 0) { 'Inconclusive' } elseif ($totals.Skipped -gt 0) { 'Skipped' } else { 'Passed' }
    $caseXml = [System.Collections.Generic.List[string]]::new()
    $index = 0
    foreach ($outcome in $Outcomes) {
        $index++
        $name = if ($index -le 2) { 'duplicate display name' } else { "case $index" }
        $fullName = "Fixture.$name.$index"
        $output = if ($WithSensitiveOutput -and $index -eq 1) { '<output>C:\Users\fixture\private.log password=sample-secret</output>' } else { '' }
        $failure = if ($outcome -in @('Failed', 'Error')) { '<failure><message>synthetic failure reason</message></failure>' } else { '' }
        $label = if ($outcome -in @('Failed', 'Error')) { 'label="Failure" ' } elseif ($outcome -in @('Skipped', 'Ignored', 'NotRunnable')) { 'label="Ignored" ' } else { '' }
        $caseXml.Add(('<test-case id="{0}" name="{1}" fullname="{2}" result="{3}" {4}runstate="Runnable" duration="1,25">{5}{6}</test-case>' -f $index, $name, $fullName, $outcome, $label, $output, $failure))
    }
    $caseBody = $caseXml -join ''
    return ('<test-run id="0" result="{0}" total="{1}" passed="{2}" failed="{3}" skipped="{4}" inconclusive="{5}" testcasecount="{1}" duration="1,25"><test-suite id="suite" type="Assembly" name="Fixture" fullname="Fixture" result="{0}" runstate="Runnable" testcasecount="{1}">{6}</test-suite></test-run>' -f $result, $Outcomes.Count, $totals.Passed, $totals.Failed, $totals.Skipped, $totals.Inconclusive, $caseBody)
}

function New-PrivateRunFixture {
    param([string]$RunId, [string]$XmlText, [string]$Status = 'Passed', [switch]$MissingXml)
    $runDir = Join-Path $root $RunId
    New-Item -ItemType Directory -Path $runDir -Force | Out-Null
    $null = Set-ProtectedEvidenceDirectory -Path $runDir
    $xmlPath = $null
    $xmlRecord = $null
    $total = 0; $passed = 0; $failed = 0; $skipped = 0; $inconclusive = 0
    $coherent = $false
    if (-not $MissingXml) {
        $xmlPath = Join-Path $runDir 'result.xml'
        [System.IO.File]::WriteAllText($xmlPath, $XmlText, [System.Text.UTF8Encoding]::new($false))
        $xmlRecord = [ordered]@{ bytes = (Get-Item $xmlPath).Length; sha256 = (Get-FileHash $xmlPath -Algorithm SHA256).Hash.ToLowerInvariant() }
        $facts = Get-UnityEvidenceReportFacts -XmlPath $xmlPath
        $total = $facts.counts.total; $passed = $facts.counts.passed; $failed = $facts.counts.failed; $skipped = $facts.counts.skipped; $inconclusive = $facts.counts.inconclusive
        $coherent = $true
    }
    $logPath = Join-Path $runDir 'raw.log'
    [System.IO.File]::WriteAllText($logPath, 'raw-log-private-value', [System.Text.UTF8Encoding]::new($false))
    $logRecord = [ordered]@{ bytes = (Get-Item $logPath).Length; sha256 = (Get-FileHash $logPath -Algorithm SHA256).Hash.ToLowerInvariant() }
    $inputCore = [ordered]@{
        schema = 'unity-validation-input-snapshot-v1'
        repository = [ordered]@{ origin = 'https://github.com/TorroisBr/Simulation'; branch = 'fixture'; head = ('a' * 40); tree = ('b' * 40) }
        unityEditorVersion = '6000.0.0f1'
        files = @([ordered]@{ path = 'Assets/Fixture.cs'; bytes = 12; sha256 = ('c' * 64) })
    }
    $input = [ordered]@{ schema = $inputCore.schema; repository = $inputCore.repository; unityEditorVersion = $inputCore.unityEditorVersion; files = $inputCore.files; sha256 = (Get-BytesSha256 (ConvertTo-EvidenceJsonBytes $inputCore)) }
    $attempt = [ordered]@{
        status = $Status; startedAtUtc = '2026-10-12T00:00:00.0000000Z'; durationMilliseconds = 250
        processId = 1234; exitCode = 0; total = $total; passed = $passed; failed = $failed; skipped = $skipped; inconclusive = $inconclusive
        frameworkResult = $(if ($MissingXml) { $null } else { $Status }); countsCoherent = $coherent; errorMessage = $null
        xmlPath = $xmlPath; xml = $xmlRecord; logPath = $logPath; log = $logRecord; argumentVector = @('-runTests'); publicArguments = @('-runTests')
    }
    $manifest = [ordered]@{
        schema = 'unity-validation-private-run-v1'; toolVersion = '1.0.0'; runId = $RunId
        startedAtUtc = '2026-10-12T00:00:00.0000000Z'; finishedAtUtc = '2026-10-12T00:00:01.0000000Z'
        projectPath = 'C:\private\project'; unityPath = 'C:\private\Unity.exe'; unityExecutable = [ordered]@{ bytes = 40; sha256 = ('e' * 64) }; unityExecutableStable = $true
        runner = [ordered]@{ powershellEdition = 'Core'; powershellVersion = '7.6.5'; dotnetVersion = '10.0.11'; compressionAssemblyVersion = [System.IO.Compression.ZipArchive].Assembly.GetName().Version.ToString(); operatingSystem = 'Windows test fixture'; architecture = 'X64'; culture = 'en-US'; timeZone = 'UTC' }
        invocation = [ordered]@{ mode = 'EditMode'; testFilter = $null; testCategory = $null; selection = 'all'; timeoutMinutes = 30; retryOperationalFailure = $false }
        inputBefore = $input; inputAfter = $input; inputStable = $true; status = $Status; attempts = @($attempt)
    }
    $manifestPath = Join-Path $runDir 'run-manifest.json'
    [System.IO.File]::WriteAllText($manifestPath, (ConvertTo-Json -InputObject $manifest -Depth 80), [System.Text.UTF8Encoding]::new($false))
    return $manifestPath
}

function New-MaliciousZip {
    param([string]$Path, [string]$EntryName, [byte[]]$Bytes)
    Add-Type -AssemblyName System.IO.Compression
    $stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
    try {
        $zip = [System.IO.Compression.ZipArchive]::new($stream, [System.IO.Compression.ZipArchiveMode]::Create, $true)
        try {
            $entry = $zip.CreateEntry($EntryName)
            $entry.LastWriteTime = [DateTimeOffset]::new(1980, 1, 1, 0, 0, 0, [TimeSpan]::Zero)
            $writer = $entry.Open()
            try { $writer.Write($Bytes, 0, $Bytes.Length) } finally { $writer.Dispose() }
        }
        finally { $zip.Dispose() }
    }
    finally { $stream.Dispose() }
}

try {
    $aclPath = Set-ProtectedEvidenceDirectory -Path (Join-Path $root 'protected-local')
    $acl = [System.IO.FileSystemAclExtensions]::GetAccessControl([System.IO.DirectoryInfo]::new($aclPath))
    Assert-True $acl.AreAccessRulesProtected 'local evidence directory disables inherited ACLs'
    $currentSid = [System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value
    $aclRules = @($acl.GetAccessRules($true, $true, [System.Security.Principal.SecurityIdentifier]))
    Assert-True ($aclRules.Count -eq 1 -and $aclRules[0].IdentityReference.Value -eq $currentSid) 'local evidence directory grants only the current account'

    $inputProject = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
    $assetFixture = Join-Path $inputProject 'Assets/PackagerEvidenceFixture.tmp'
    $packageFixture = Join-Path $inputProject 'Packages/PackagerEvidenceFixture.tmp'
    New-Item -ItemType Directory -Path (Split-Path $assetFixture), (Split-Path $packageFixture) -Force | Out-Null
    [System.IO.File]::WriteAllText($assetFixture, 'fixture source', [System.Text.UTF8Encoding]::new($false))
    [System.IO.File]::WriteAllText($packageFixture, 'untracked validation input', [System.Text.UTF8Encoding]::new($false))
    $snapshotA = Get-UnityEvidenceInputSnapshot -ProjectPath $inputProject
    $sanitizedRemote = ConvertTo-PublicRemoteIdentity 'https://fixture-user:fixture-secret@github.com/TorroisBr/Simulation.git'
    Assert-True ($sanitizedRemote -eq 'https://github.com/TorroisBr/Simulation.git') 'public repository identity strips remote credentials'
    Assert-True (@($snapshotA.files | Where-Object path -eq 'Assets/PackagerEvidenceFixture.tmp').Count -eq 1) 'input inventory records project file identities'
    [System.IO.File]::WriteAllText($packageFixture, 'changed untracked validation input', [System.Text.UTF8Encoding]::new($false))
    $snapshotB = Get-UnityEvidenceInputSnapshot -ProjectPath $inputProject
    Assert-True ($snapshotA.sha256 -cne $snapshotB.sha256) 'inventory hash changes when an untracked validation input changes'
    Assert-True (@($snapshotB.files | Where-Object path -eq 'Packages/PackagerEvidenceFixture.tmp').Count -eq 1) 'untracked validation inputs are included'
    $captureRoot = Join-Path $root 'capture'
    $capture = Invoke-UnityValidationEvidenceCapture -ProjectPath $inputProject -Mode EditMode -UnityPath (Join-Path $root 'missing-Unity.exe') -EvidenceRoot $captureRoot
    Assert-True ($capture.Status -eq 'UnityLaunchFailed') 'capture records a missing Unity executable as a failed gate without launching a process'
    Assert-True ((Test-ProtectedEvidenceDirectory -Path $captureRoot) -eq [System.IO.Path]::GetFullPath($captureRoot)) 'capture root passes the protected ACL check'
    $capturedManifest = Get-Content -LiteralPath $capture.ManifestPath -Raw | ConvertFrom-Json -AsHashtable -DateKind String
    Assert-True ($capturedManifest.attempts.Count -eq 0 -and -not $capturedManifest.inputStable) 'failed launch is retained locally and cannot be packaged as complete evidence'

    $mixedXml = New-TestXml -Outcomes @('Passed', 'Passed', 'Failed', 'Skipped', 'Inconclusive') -WithSensitiveOutput
    $mixedRun = New-PrivateRunFixture -RunId '11111111111111111111111111111111' -XmlText $mixedXml -Status 'Failed'
    $outDir = Join-Path $root 'packages'
    $bundleA = New-UnityValidationEvidencePackage -RunManifestPath $mixedRun -OutputDirectory $outDir -BundleId 'deterministic-a'
    $bundleB = New-UnityValidationEvidencePackage -RunManifestPath $mixedRun -OutputDirectory $outDir -BundleId 'deterministic-b'
    Assert-True ((Get-FileHash $bundleA.ArchivePath -Algorithm SHA256).Hash -eq (Get-FileHash $bundleB.ArchivePath -Algorithm SHA256).Hash) 'identical source run produces byte-identical ZIP bytes'
    $verify = Test-UnityValidationEvidencePackage -ManifestPath $bundleA.ManifestPath -ArchivePath $bundleA.ArchivePath -RunManifestPath $mixedRun
    Assert-True $verify.IsValid "package and retained raw XML verify: $($verify.Error)"
    $ordinaryManifest = Get-Content -LiteralPath $bundleA.ManifestPath -Raw | ConvertFrom-Json -AsHashtable
    Assert-True ($ordinaryManifest.pathPolicy -eq 'none' -and $ordinaryManifest.runs[0].attempts[0].normalizedXml.pathPolicy -eq 'none') 'ordinary report package declares that no local Assembly path was removed'
    $module = Get-Module UnityValidationEvidence
    $relativeRepositoryPathAccepted = $true
    try { & $module { param($text) Test-PublicEvidenceText $text } '{"path":"Assets/_Project/Scripts/work/root/agent/Invariant.cs"}' } catch { $relativeRepositoryPathAccepted = $false }
    Assert-True $relativeRepositoryPathAccepted 'privacy scanning accepts repository-relative file paths even when a directory is named like a Unix root'
    Assert-Throws { & $module { param($text) Test-PublicEvidenceText $text } '{"path":"/home/fixture/private.xml"}' } 'privacy scanning still rejects absolute Unix paths'
    Assert-Throws { & $module { param($text) Test-PublicEvidenceText $text } '{"message":"(/home/runner/test.cs)"}' } 'privacy scanning rejects parenthesized absolute Unix paths in retained failure text'
    Assert-Throws { & $module { param($text) Test-PublicEvidenceText $text } 'FileNotFoundException(/home/runner/test.cs)' } 'privacy scanning rejects exception-style absolute Unix paths'
    Assert-Throws { & $module { param($text) Test-PublicEvidenceText $text } '{"path":"file://localhost/home/runner/test.cs"}' } 'privacy scanning rejects localhost file URIs in retained public evidence'

    $localAssemblyPath = 'C:\Users\fixture\project\Library\ScriptAssemblies\assembly-CSharp-Editor.dll'
    $assemblyPathXml = (New-TestXml -Outcomes @('Passed')).Replace('fullname="Fixture"', ('fullname="' + $localAssemblyPath + '"'))
    $assemblyPathRun = New-PrivateRunFixture -RunId '55555555555555555555555555555555' -XmlText $assemblyPathXml -Status 'Passed'
    $assemblyPathBundle = New-UnityValidationEvidencePackage -RunManifestPath $assemblyPathRun -OutputDirectory $outDir -BundleId 'assembly-path-redaction'
    $assemblyPathManifest = Get-Content -LiteralPath $assemblyPathBundle.ManifestPath -Raw | ConvertFrom-Json -AsHashtable
    Assert-True ($assemblyPathManifest.pathPolicy -eq 'redact-absolute-local-assembly-suite-fullnames') 'manifest declares the exact Assembly suite path field category that was removed'
    Assert-True ($assemblyPathManifest.runs[0].attempts[0].normalizedXml.pathPolicy -eq $assemblyPathManifest.pathPolicy) 'result entry binds its normalization to the declared path policy'
    $assemblyStream = [System.IO.File]::OpenRead($assemblyPathBundle.ArchivePath)
    try {
        $assemblyZip = [System.IO.Compression.ZipArchive]::new($assemblyStream, [System.IO.Compression.ZipArchiveMode]::Read)
        try {
            $assemblyReader = [System.IO.StreamReader]::new($assemblyZip.Entries[0].Open())
            try { $portableAssemblyXml = $assemblyReader.ReadToEnd() } finally { $assemblyReader.Dispose() }
        }
        finally { $assemblyZip.Dispose() }
    }
    finally { $assemblyStream.Dispose() }
    Assert-True (-not $portableAssemblyXml.Contains('C:\Users\fixture')) 'published result contains no incidental local Windows home path'
    Assert-True ($portableAssemblyXml.Contains('fullname="&lt;PROJECT_ROOT&gt;/Library/ScriptAssemblies/assembly-CSharp-Editor.dll"')) 'only the local root is replaced and the assembly-relative identity casing is retained'
    $assemblySourcePath = Join-Path (Split-Path $assemblyPathRun) 'result.xml'
    $assemblySourceFacts = Get-UnityEvidenceReportFacts -XmlPath $assemblySourcePath
    $portablePath = Join-Path $root 'portable-assembly-result.xml'
    [System.IO.File]::WriteAllText($portablePath, $portableAssemblyXml, [System.Text.UTF8Encoding]::new($false))
    $portableFacts = Get-UnityEvidenceReportFacts -XmlPath $portablePath
    $sourceAssembly = $assemblySourceFacts.document.SelectSingleNode("//*[local-name()='test-suite' and @type='Assembly']")
    $portableAssembly = $portableFacts.document.SelectSingleNode("//*[local-name()='test-suite' and @type='Assembly']")
    foreach ($attributeName in @('id', 'type', 'name', 'result', 'runstate', 'testcasecount')) {
        Assert-True ($sourceAssembly.GetAttribute($attributeName) -ceq $portableAssembly.GetAttribute($attributeName)) "Assembly suite $attributeName remains unchanged"
    }
    $sourceSemanticHash = Get-UnityXmlSemanticHash -Document $assemblySourceFacts.document -RedactLocalAssemblyPaths
    $portableSemanticHash = Get-UnityXmlSemanticHash -Document $portableFacts.document -RedactLocalAssemblyPaths
    Assert-True ($sourceSemanticHash -ceq $portableSemanticHash) 'declared path projection preserves the full result hierarchy, identities, outcomes, counts, and decision-bearing fields'
    Assert-True ((Test-UnityValidationEvidencePackage -ManifestPath $assemblyPathBundle.ManifestPath -ArchivePath $assemblyPathBundle.ArchivePath -RunManifestPath $assemblyPathRun).IsValid) 'sanitized package verifies against its protected original report'

    $casePathXml = (New-TestXml -Outcomes @('Passed')).Replace('fullname="Fixture.duplicate display name.1"', 'fullname="C:\Users\fixture\private\case.cs"')
    $casePathRun = New-PrivateRunFixture -RunId '66666666666666666666666666666666' -XmlText $casePathXml -Status 'Passed'
    Assert-Throws { New-UnityValidationEvidencePackage -RunManifestPath $casePathRun -OutputDirectory $outDir -BundleId 'reject-case-path' } 'path-like test-case identities remain protected and are not covered by the Assembly-only redaction'
    $unsupportedAssemblyXml = (New-TestXml -Outcomes @('Passed')).Replace('fullname="Fixture"', 'fullname="C:\Users\fixture\project\Library\ScriptAssemblies\Other.dll"')
    $unsupportedAssemblyRun = New-PrivateRunFixture -RunId '77777777777777777777777777777777' -XmlText $unsupportedAssemblyXml -Status 'Passed'
    Assert-Throws { New-UnityValidationEvidencePackage -RunManifestPath $unsupportedAssemblyRun -OutputDirectory $outDir -BundleId 'reject-unsupported-assembly-path' } 'unapproved Assembly path suffixes fail closed'
    $fileUriAssemblyXml = (New-TestXml -Outcomes @('Passed')).Replace('fullname="Fixture"', 'fullname="file://localhost/home/runner/project/Library/ScriptAssemblies/Assembly-CSharp-Editor.dll"')
    Assert-Throws { & $module { param($xmlText) $document = [System.Xml.XmlDocument]::new(); $document.LoadXml($xmlText); Get-UnityReportPathPolicy -Document $document } $fileUriAssemblyXml } 'file URI Assembly identities are not misclassified as a report with no absolute path'
    $fileUriAssemblyRun = New-PrivateRunFixture -RunId '77777777777777777777777777777778' -XmlText $fileUriAssemblyXml -Status 'Passed'
    Assert-Throws { New-UnityValidationEvidencePackage -RunManifestPath $fileUriAssemblyRun -OutputDirectory $outDir -BundleId 'reject-file-uri-assembly-path' } 'file URI Assembly paths are recognized as absolute and fail closed'
    $ambiguousAssemblyXml = $assemblyPathXml.Replace('</test-run>', '<test-suite id="second" type="Assembly" name="Other" fullname="D:\fixture\Library\ScriptAssemblies\Assembly-CSharp-Editor.dll" result="Passed" runstate="Runnable" testcasecount="0"></test-suite></test-run>')
    $ambiguousAssemblyRun = New-PrivateRunFixture -RunId '88888888888888888888888888888888' -XmlText $ambiguousAssemblyXml -Status 'Passed'
    Assert-Throws { New-UnityValidationEvidencePackage -RunManifestPath $ambiguousAssemblyRun -OutputDirectory $outDir -BundleId 'reject-ambiguous-assembly-path' } 'multiple absolute Assembly paths fail closed as ambiguous'
    $resultTamperedManifestPath = Join-Path $outDir 'result-tampered.manifest.json'
    $resultTamperedManifest = Get-Content $bundleA.ManifestPath -Raw | ConvertFrom-Json -AsHashtable
    $resultTamperedManifest.runs[0].attempts[0].passed++
    [System.IO.File]::WriteAllText($resultTamperedManifestPath, (ConvertTo-Json -InputObject $resultTamperedManifest -Depth 40), [System.Text.UTF8Encoding]::new($false))
    $resultTamperedVerify = Test-UnityValidationEvidencePackage -ManifestPath $resultTamperedManifestPath -ArchivePath $bundleA.ArchivePath
    Assert-True (-not $resultTamperedVerify.IsValid) 'archive-only verification rejects public result counts that disagree with XML'
    $statusTamperedManifestPath = Join-Path $outDir 'status-tampered.manifest.json'
    $statusTamperedManifest = Get-Content $bundleA.ManifestPath -Raw | ConvertFrom-Json -AsHashtable
    $statusTamperedManifest.runs[0].status = 'Passed'
    [System.IO.File]::WriteAllText($statusTamperedManifestPath, (ConvertTo-Json -InputObject $statusTamperedManifest -Depth 40), [System.Text.UTF8Encoding]::new($false))
    $statusTamperedVerify = Test-UnityValidationEvidencePackage -ManifestPath $statusTamperedManifestPath -ArchivePath $bundleA.ArchivePath
    Assert-True (-not $statusTamperedVerify.IsValid) 'archive-only verification rejects run status that disagrees with the final attempt'
    $retryTamperedManifestPath = Join-Path $outDir 'retry-tampered.manifest.json'
    $retryTamperedManifest = Get-Content $bundleA.ManifestPath -Raw | ConvertFrom-Json -AsHashtable
    $retryOriginalAttempt = $retryTamperedManifest.runs[0].attempts[0]
    $retryTamperedManifest.runs[0].invocation.retryOperationalFailure = $true
    $retryTamperedManifest.runs[0].attempts = @($retryOriginalAttempt.Clone(), $retryOriginalAttempt)
    [System.IO.File]::WriteAllText($retryTamperedManifestPath, (ConvertTo-Json -InputObject $retryTamperedManifest -Depth 40), [System.Text.UTF8Encoding]::new($false))
    $retryTamperedVerify = Test-UnityValidationEvidencePackage -ManifestPath $retryTamperedManifestPath -ArchivePath $bundleA.ArchivePath
    Assert-True (-not $retryTamperedVerify.IsValid -and $retryTamperedVerify.Error -like '*retry transition is not eligible*') 'archive-only verification rejects retries after a valid failed test result'
    $noXmlTamperedManifestPath = Join-Path $outDir 'no-xml-status-tampered.manifest.json'
    $noXmlTamperedManifest = Get-Content $bundleA.ManifestPath -Raw | ConvertFrom-Json -AsHashtable
    $noXmlAttempt = $noXmlTamperedManifest.runs[0].attempts[0]
    $noXmlAttempt.status = 'Failed'
    $noXmlAttempt.normalizedXml = $null
    $noXmlAttempt.originalXml = $null
    $noXmlAttempt.reportResult = $null
    $noXmlAttempt.caseCount = $null
    $noXmlAttempt.total = 0
    $noXmlAttempt.passed = 0
    $noXmlAttempt.failed = 0
    $noXmlAttempt.skipped = 0
    $noXmlAttempt.inconclusive = 0
    [System.IO.File]::WriteAllText($noXmlTamperedManifestPath, (ConvertTo-Json -InputObject $noXmlTamperedManifest -Depth 40), [System.Text.UTF8Encoding]::new($false))
    $noXmlTamperedVerify = Test-UnityValidationEvidencePackage -ManifestPath $noXmlTamperedManifestPath -ArchivePath $bundleA.ArchivePath
    Assert-True (-not $noXmlTamperedVerify.IsValid -and $noXmlTamperedVerify.Error -like '*unsupported result claims for an attempt without XML*') 'archive-only verification rejects a failed attempt without XML'

    $ordinalArchive = Join-Path $outDir 'ordinal-order.zip'
    $savedCulture = [System.Globalization.CultureInfo]::CurrentCulture
    try {
        [System.Globalization.CultureInfo]::CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US')
        $module = Get-Module UnityValidationEvidence
        & $module {
            param($path)
            $items = @(
                [pscustomobject]@{ name = 'results/z.xml'; bytes = [byte[]]@(1) },
                [pscustomobject]@{ name = 'results/ä.xml'; bytes = [byte[]]@(2) },
                [pscustomobject]@{ name = 'results/a.xml'; bytes = [byte[]]@(3) }
            )
            Write-DeterministicEvidenceArchive -Path $path -Entries $items
        } $ordinalArchive
        $ordinalStream = [System.IO.File]::OpenRead($ordinalArchive)
        try {
            $ordinalZip = [System.IO.Compression.ZipArchive]::new($ordinalStream, [System.IO.Compression.ZipArchiveMode]::Read)
            try { $ordinalNames = @($ordinalZip.Entries | ForEach-Object FullName) } finally { $ordinalZip.Dispose() }
        } finally { $ordinalStream.Dispose() }
        Assert-True (($ordinalNames -join '|') -eq 'results/a.xml|results/z.xml|results/ä.xml') 'ZIP entry ordering is ordinal under an English collation that sorts ä near a'
    }
    finally { [System.Globalization.CultureInfo]::CurrentCulture = $savedCulture }
    $manifestJson = Get-Content $bundleA.ManifestPath -Raw
    $publicManifest = $manifestJson | ConvertFrom-Json -AsHashtable
    Assert-True ($publicManifest.packerRuntime.Contains('culture')) 'manifest records the capture culture as provenance'
    $crossCultureBundle = $null
    $savedCulture = [System.Globalization.CultureInfo]::CurrentCulture
    try {
        [System.Globalization.CultureInfo]::CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo('pt-BR')
        $crossCultureBundle = New-UnityValidationEvidencePackage -RunManifestPath $mixedRun -OutputDirectory $outDir -BundleId 'deterministic-ptbr'
        Assert-True ((Get-FileHash $bundleA.ArchivePath -Algorithm SHA256).Hash -eq (Get-FileHash $crossCultureBundle.ArchivePath -Algorithm SHA256).Hash) 'result archive bytes are culture-independent'
        [System.Globalization.CultureInfo]::CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US')
        $crossCultureVerify = Test-UnityValidationEvidencePackage -ManifestPath $crossCultureBundle.ManifestPath -ArchivePath $crossCultureBundle.ArchivePath
        Assert-True $crossCultureVerify.IsValid "package verifies under a different culture: $($crossCultureVerify.Error)"
    }
    finally { [System.Globalization.CultureInfo]::CurrentCulture = $savedCulture }
    $zipStream = [System.IO.File]::OpenRead($bundleA.ArchivePath)
    try {
        $zip = [System.IO.Compression.ZipArchive]::new($zipStream, [System.IO.Compression.ZipArchiveMode]::Read)
        try {
            Assert-True ($zip.Entries.Count -eq 1) 'archive contains only the normalized result XML'
            $entryReader = [System.IO.StreamReader]::new($zip.Entries[0].Open())
            try { $normalizedText = $entryReader.ReadToEnd() } finally { $entryReader.Dispose() }
        } finally { $zip.Dispose() }
    } finally { $zipStream.Dispose() }
    Assert-True (-not $normalizedText.Contains('C:\Users\fixture')) 'local absolute output path is stripped'
    Assert-True (-not $normalizedText.Contains('sample-secret')) 'credential-like output is stripped'
    Assert-True ($normalizedText.Contains('duration="1,25"')) 'locale-formatted duration remains intact'
    Assert-True ($normalizedText.Contains('duplicate display name')) 'duplicate display names are preserved'
    Assert-True ($normalizedText.Contains('synthetic failure reason')) 'failure reasons remain intact'
    Assert-True ($normalizedText.Contains('label="Failure"')) 'outcome labels remain intact'
    Assert-True (-not $manifestJson.Contains('C:\private\project')) 'private project path is absent from public manifest'
    Assert-True (-not $manifestJson.Contains('raw-log-private-value')) 'raw log content is never included in the public manifest'
    $rawInfo = (Get-Content $bundleA.ManifestPath -Raw | ConvertFrom-Json -AsHashtable).runs[0].attempts[0].originalXml
    $rawPath = Join-Path (Split-Path $mixedRun) 'result.xml'
    Assert-True ($rawInfo.sha256 -eq (Get-FileHash $rawPath -Algorithm SHA256).Hash.ToLowerInvariant()) 'manifest retains exact original XML SHA-256'
    Assert-True ($rawInfo.bytes -eq (Get-Item $rawPath).Length) 'manifest retains exact original XML byte length'

    foreach ($case in @(
        @{ name = 'truncated'; xml = '<test-run' },
        @{ name = 'dtd'; xml = '<!DOCTYPE test-run [<!ENTITY x SYSTEM "file:///etc/passwd">]><test-run>&x;</test-run>' },
        @{ name = 'unknown-outcome'; xml = (New-TestXml -Outcomes @('Flaky')) },
        @{ name = 'invalid-count'; xml = ((New-TestXml -Outcomes @('Passed')).Replace('passed="1"', 'passed="2"')) },
        @{ name = 'missing-required-field'; xml = ((New-TestXml -Outcomes @('Passed')).Replace('runstate="Runnable"', 'runstate=""')) },
        @{ name = 'hierarchy-count-mismatch'; xml = ((New-TestXml -Outcomes @('Passed')).Replace('fullname="Fixture" result="Passed" runstate="Runnable" testcasecount="1"', 'fullname="Fixture" result="Passed" runstate="Runnable" testcasecount="0"')) }
    )) {
        $badPath = Join-Path $root ($case.name + '.xml')
        [System.IO.File]::WriteAllText($badPath, $case.xml, [System.Text.UTF8Encoding]::new($false))
        Assert-Throws { Get-UnityEvidenceReportFacts -XmlPath $badPath } "$($case.name) XML is rejected"
    }

    $claimRun = New-PrivateRunFixture -RunId '22222222222222222222222222222222' -XmlText $mixedXml -Status 'Failed'
    Assert-Throws { New-UnityValidationEvidencePackage -RunManifestPath $claimRun -OutputDirectory $outDir -BundleId 'output-required' -ClaimRequiresOutput } 'claim requiring output fails closed when nonempty output would be stripped'
    $missingRun = New-PrivateRunFixture -RunId '33333333333333333333333333333333' -XmlText '' -Status 'NoResultXml' -MissingXml
    Assert-Throws { New-UnityValidationEvidencePackage -RunManifestPath $missingRun -OutputDirectory $outDir -BundleId 'missing-report' } 'missing final XML report cannot be packaged'

    $tamperBundle = New-UnityValidationEvidencePackage -RunManifestPath $mixedRun -OutputDirectory $outDir -BundleId 'tamper-check'
    $tampered = [System.IO.File]::ReadAllBytes($tamperBundle.ArchivePath)
    $tampered[$tampered.Length - 8] = $tampered[$tampered.Length - 8] -bxor 0x01
    [System.IO.File]::WriteAllBytes($tamperBundle.ArchivePath, $tampered)
    Assert-True (-not (Test-UnityValidationEvidencePackage -ManifestPath $tamperBundle.ManifestPath -ArchivePath $tamperBundle.ArchivePath).IsValid) 'archive tampering is detected'

    $safePath = Join-Path $root 'safe-source.xml'
    $safeXml = [System.Text.UTF8Encoding]::new($false).GetBytes((New-TestXml -Outcomes @('Passed')))
    [System.IO.File]::WriteAllBytes($safePath, $safeXml)
    $slipArchive = Join-Path $root 'slip.zip'
    New-MaliciousZip -Path $slipArchive -EntryName '../escape.xml' -Bytes $safeXml
    $slipHash = (Get-FileHash $slipArchive -Algorithm SHA256).Hash.ToLowerInvariant()
    $slipRecord = [ordered]@{ entry = '../escape.xml'; bytes = $safeXml.Length; sha256 = (Get-FileHash $safePath -Algorithm SHA256).Hash.ToLowerInvariant() }
    $slipManifest = [ordered]@{
        schema = 'unity-validation-evidence-package-v1'; toolVersion = '1.0.0'; bundleId = 'slip'
        packerRuntime = [ordered]@{ powershellEdition = 'Core'; powershellVersion = $PSVersionTable.PSVersion.ToString(); dotnetVersion = [Environment]::Version.ToString(); compressionAssemblyVersion = [System.IO.Compression.ZipArchive].Assembly.GetName().Version.ToString() }
        claimRequiresOutput = $false; outputPolicy = 'remove-output-elements'
        archive = [ordered]@{ file = 'slip.zip'; bytes = (Get-Item $slipArchive).Length; sha256 = $slipHash; format = 'zip-deflate-fixed-1980-01-01'; entryPolicy = 'normalized-test-result-xml-only' }
        runs = @([ordered]@{ runId = '44444444444444444444444444444444'; attempts = @([ordered]@{ normalizedXml = $slipRecord }) })
    }
    $slipManifestPath = Join-Path $root 'slip.manifest.json'
    [System.IO.File]::WriteAllText($slipManifestPath, (ConvertTo-Json -InputObject $slipManifest -Depth 30), [System.Text.UTF8Encoding]::new($false))
    Assert-True (-not (Test-UnityValidationEvidencePackage -ManifestPath $slipManifestPath -ArchivePath $slipArchive).IsValid) 'ZIP-slip entry is rejected before extraction'

    $duplicateRun = New-PrivateRunFixture -RunId '11111111111111111111111111111111' -XmlText $mixedXml -Status 'Failed'
    Assert-Throws { New-UnityValidationEvidencePackage -RunManifestPath @($mixedRun, $duplicateRun) -OutputDirectory $outDir -BundleId 'duplicate-run' } 'duplicate run identity is rejected'

    Write-Output "PASS: $script:Assertions assertions"
}
finally {
    if ($assetFixture -and (Test-Path -LiteralPath $assetFixture)) { Remove-Item -LiteralPath $assetFixture -Force }
    if ($packageFixture -and (Test-Path -LiteralPath $packageFixture)) { Remove-Item -LiteralPath $packageFixture -Force }
    Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
}
