[CmdletBinding()]
param()
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$root=Join-Path ([IO.Path]::GetTempPath()) ('trx-evidence-tests-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root -Force|Out-Null
Import-Module (Join-Path $PSScriptRoot '..\TrxValidationEvidence.psm1') -Force
Import-Module (Join-Path $PSScriptRoot '..\UnityValidationEvidence.psm1') -Force
$script:Assertions=0
function Assert-True([bool]$Value,[string]$Message){$script:Assertions++;if(-not $Value){throw "ASSERTION FAILED: $Message"}}
function Assert-Throws([scriptblock]$Action,[string]$Message){$script:Assertions++;$threw=$false;try{&$Action|Out-Null}catch{$threw=$true};if(-not $threw){throw "ASSERTION FAILED: $Message"}}
function New-TrxText {
    param([string[]]$Outcomes=@('Passed'),[switch]$PrivateMetadata,[switch]$EmptyOptionalMetadata,[switch]$AbsentOptionalMetadata,[string]$Namespace='http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
    $run=[guid]::NewGuid().ToString('D');$execs=@();$defs=@();$entries=@();$results=@();$n=0
    foreach($outcome in $Outcomes){$n++;$id=[guid]::NewGuid().ToString('D');$exec=[guid]::NewGuid().ToString('D');$execs+=,$exec;$defs+=,"<UnitTest id=`"$id`" name=`"same display name`" storage=`"C:\agent\project\bin\Tests.dll`"><Execution id=`"$exec`"/><TestMethod codeBase=`"C:\agent\project\bin\Tests.dll`" adapterTypeName=`"adapter`" className=`"Fixture.Tests`" name=`"Case`"/></UnitTest>";$entries+=,"<TestEntry testId=`"$id`" executionId=`"$exec`" testListId=`"{00000000-0000-0000-0000-000000000001}`"/>";$results+=,"<UnitTestResult executionId=`"$exec`" testId=`"$id`" testName=`"same display name($n)`" computerName=`"PRIVATE-HOST`" duration=`"00:00:00.1000000`" startTime=`"2026-10-13T00:00:00.0000000Z`" endTime=`"2026-10-13T00:00:00.1000000Z`" testType=`"{13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b}`" outcome=`"$outcome`" testListId=`"{00000000-0000-0000-0000-000000000001}`" relativeResultsDirectory=`"$run`"><Output><StdOut>case detail unchanged</StdOut></Output></UnitTestResult>"}
    $passed=@($Outcomes|Where-Object {$_ -ceq 'Passed'}).Count;$failed=@($Outcomes|Where-Object {$_ -ceq 'Failed'}).Count
    $nativeFailed=$failed
    $summary=if($failed){'Failed'}elseif(@($Outcomes|Where-Object {$_ -cne 'Passed'}).Count){'Failed'}else{'Completed'}
    $runName=if($PrivateMetadata){'agent-host run private'}else{'run fixture'};$user=if($PrivateMetadata){'PRIVATE-USER'}else{'fixture'}
    $runUserAttribute=if($AbsentOptionalMetadata){''}elseif($EmptyOptionalMetadata){' runUser=""'}else{" runUser=`"$user`""}
    $deploymentAttribute=if($AbsentOptionalMetadata){''}elseif($EmptyOptionalMetadata){' runDeploymentRoot=""'}else{' runDeploymentRoot="C:\agent\project\TestResults\run"'}
    $stdout=if($PrivateMetadata){'<Output><StdOut>At C:\agent\project\Tools\CoreValidation\Invoke.ps1 line 7</StdOut></Output>'}else{''}
    return "<?xml version=`"1.0`" encoding=`"utf-8`"?><TestRun xmlns=`"$Namespace`" id=`"$run`" name=`"$runName`"$runUserAttribute><Times creation=`"2026-10-13T00:00:00Z`" queuing=`"2026-10-13T00:00:00Z`" start=`"2026-10-13T00:00:00Z`" finish=`"2026-10-13T00:00:01Z`"/><TestSettings id=`"{00000000-0000-0000-0000-000000000002}`" name=`"fixture`"><Deployment$deploymentAttribute/></TestSettings><Results>$($results -join '')</Results><TestDefinitions>$($defs -join '')</TestDefinitions><TestEntries>$($entries -join '')</TestEntries><TestLists><TestList id=`"{00000000-0000-0000-0000-000000000001}`" name=`"Results Not in a List`"/></TestLists><ResultSummary outcome=`"$summary`"><Counters total=`"$($Outcomes.Count)`" executed=`"$($passed+$failed)`" passed=`"$passed`" failed=`"$nativeFailed`" error=`"0`" timeout=`"0`" aborted=`"0`" inconclusive=`"0`" passedButRunAborted=`"0`" notRunnable=`"0`" notExecuted=`"0`" disconnected=`"0`" warning=`"0`" completed=`"0`" inProgress=`"0`" pending=`"0`"/>$stdout</ResultSummary></TestRun>"
}
function Write-Trx([string]$Name,[string]$Text){$p=Join-Path $root $Name;[IO.File]::WriteAllText($p,$Text,[Text.UTF8Encoding]::new($false));return $p}
try {
    $passed=Write-Trx 'passed.trx' (New-TrxText -Outcomes @('Passed','Passed') -PrivateMetadata)
    $facts=Get-TrxFacts $passed;Assert-True ($facts.ResultCount -eq 2 -and $facts.GatePassed) 'passed flat report and duplicate display names'
    $failed=Write-Trx 'failed.trx' (New-TrxText -Outcomes @('Failed'))
    $f=Get-TrxFacts $failed;Assert-True (-not $f.GatePassed -and $f.OutcomeCounts.Failed -eq 1) 'valid failed report remains nonpassing'
    foreach($outcome in @('NotExecuted','NotRunnable','Inconclusive','Timeout','Aborted','Disconnected','Warning','PassedButRunAborted','Pending','InProgress','Completed','Error')){
        $text=New-TrxText -Outcomes @($outcome);$text=$text.Replace('failed="0"','failed="0"')
        if($outcome -eq 'Failed'){$null=Get-TrxFacts (Write-Trx ("$outcome.trx") $text);continue}
        # Non-pass records are retained when producer counters are coherent and never become gate passes.
        if($outcome -eq 'Error'){$text=$text.Replace('failed="0"','failed="1"').Replace('executed="0"','executed="1"')}
        if($outcome -in @('NotExecuted','NotRunnable','Inconclusive','Timeout','Aborted','Disconnected','Warning','PassedButRunAborted','Pending','InProgress','Completed')){
            # Producer-native skipped counters remain zero; these statuses are result facts only.
            $outcomeFacts=Get-TrxFacts (Write-Trx ("$outcome.trx") $text)
            Assert-True (-not $outcomeFacts.GatePassed -and $outcomeFacts.ResultCount -eq 1 -and $outcomeFacts.OutcomeCounts[$outcome] -eq 1) "$outcome is retained as a non-passing result fact"
        }
    }
    $original=Get-TrxFacts $passed
    $projected=ConvertTo-TrxProjectedDocument $original.Document @('C:\agent\project')
    $xmlBytes=ConvertTo-TrxCanonicalXml $projected;$normalized=Write-Trx 'normalized.trx' ([Text.UTF8Encoding]::new($false,$true).GetString($xmlBytes))
    $normalizedFacts=Get-TrxFacts $normalized
    Assert-True ($projected.DocumentElement.GetAttribute('runUser') -eq '<PRIVATE_RUN_USER>') 'run user placeholder'
    Assert-True ($projected.DocumentElement.GetAttribute('name') -eq "run-$($original.RunId)") 'stable display run label'
    Assert-True (@($projected.SelectNodes("//*[local-name()='UnitTestResult']"))[0].GetAttribute('computerName') -eq 'HOST-0001') 'host alias first appearance'
    Assert-True (@($projected.SelectNodes("//*[local-name()='UnitTest']"))[0].GetAttribute('storage') -eq '<WORKTREE>/bin\Tests.dll') 'storage root projection'
    Assert-True (@($projected.SelectNodes("//*[local-name()='TestMethod']"))[0].GetAttribute('codeBase') -eq '<WORKTREE>/bin\Tests.dll') 'codeBase root projection'
    Assert-True (@($projected.SelectNodes("//*[local-name()='Deployment']"))[0].GetAttribute('runDeploymentRoot') -eq "<DEPLOYMENT>/$($original.RunId)") 'deployment output root projection'
    Assert-True (@($projected.SelectNodes("//*[local-name()='StdOut']"))[0].InnerText -eq 'case detail unchanged') 'case output is immutable during privacy projection'
    Assert-True (@($projected.SelectNodes("//*[local-name()='StdOut']"))[2].InnerText -match '<WORKTREE>/Tools[\\/]CoreValidation[\\/]Invoke\.ps1 line 7') 'summary stdout root is projected while its remaining text is preserved'
    $emptyMetadata=Write-Trx 'empty-optional-metadata.trx' (New-TrxText -EmptyOptionalMetadata);$emptyDoc=ConvertTo-TrxProjectedDocument (Get-TrxFacts $emptyMetadata).Document @('C:\agent\project');$emptyDeployment=$emptyDoc.SelectSingleNode("//*[local-name()='Deployment']")
    Assert-True ($emptyDoc.DocumentElement.HasAttribute('runUser') -and $emptyDoc.DocumentElement.GetAttribute('runUser') -eq '' -and $emptyDeployment.HasAttribute('runDeploymentRoot') -and $emptyDeployment.GetAttribute('runDeploymentRoot') -eq '') 'present empty optional metadata remains present and empty'
    $absentMetadata=Write-Trx 'absent-optional-metadata.trx' (New-TrxText -AbsentOptionalMetadata);$absentDoc=ConvertTo-TrxProjectedDocument (Get-TrxFacts $absentMetadata).Document @('C:\agent\project');$absentDeployment=$absentDoc.SelectSingleNode("//*[local-name()='Deployment']")
    Assert-True (-not $absentDoc.DocumentElement.HasAttribute('runUser') -and -not $absentDeployment.HasAttribute('runDeploymentRoot')) 'absent optional metadata remains absent'
    Assert-True ((Get-TrxSemanticSha256 $projected) -eq (Get-TrxSemanticSha256 $normalizedFacts.Document)) 'canonical projection is stable'
    Assert-Throws { ConvertTo-TrxProjectedDocument $original.Document @('C:\agent','C:\agent\project') } 'overlapping root mappings reject'
    Assert-Throws { ConvertTo-TrxProjectedDocument $original.Document @() } 'missing rooted assembly mapping rejects'
    $escapeText=(New-TrxText -PrivateMetadata).Replace('C:\agent\project\bin\Tests.dll','C:\agent\project\..\escape.dll')
    Assert-Throws { $x=Write-Trx 'escape.trx' $escapeText;ConvertTo-TrxProjectedDocument (Get-TrxFacts $x).Document @('C:\agent\project') } 'mapped traversal escapes reject'
    $prefixEscape=(New-TrxText -PrivateMetadata).Replace('C:\agent\project\bin\Tests.dll','C:\agent\projectish\Tests.dll')
    Assert-Throws { $x=Write-Trx 'prefix-escape.trx' $prefixEscape;ConvertTo-TrxProjectedDocument (Get-TrxFacts $x).Document @('C:\agent\project') } 'root-prefix lookalike escape rejects'
    $caseSlash=(New-TrxText -PrivateMetadata).Replace('C:\agent\project\bin\Tests.dll','c:/AGENT/PROJECT/bin/Tests.dll')
    $caseFile=Write-Trx 'case-slash.trx' $caseSlash;$caseProjected=ConvertTo-TrxProjectedDocument (Get-TrxFacts $caseFile).Document @('C:\agent\project')
    Assert-True (@($caseProjected.SelectNodes("//*[local-name()='UnitTest']"))[0].GetAttribute('storage') -eq '<WORKTREE>/bin/Tests.dll') 'case and separator variants map to the same verified root'
    $reserved=(New-TrxText -PrivateMetadata).Replace('case detail unchanged','prefix HOST-0001 forbidden')
    $reservedFile=Write-Trx 'reserved.trx' $reserved
    Assert-Throws { ConvertTo-TrxProjectedDocument (Get-TrxFacts $reservedFile).Document @('C:\agent\project') } 'reserved alias collision in immutable output rejects'
    $diagnostic=(New-TrxText -Outcomes @('Failed')).Replace('<Output><StdOut>case detail unchanged</StdOut></Output>','<Output><StdOut>failed case output</StdOut><ErrorInfo><Message>failure reason stays exact</Message><StackTrace>failure stack stays exact</StackTrace></ErrorInfo></Output>')
    $diagnosticFile=Write-Trx 'diagnostic.trx' $diagnostic;$diagnosticDoc=ConvertTo-TrxProjectedDocument (Get-TrxFacts $diagnosticFile).Document @('C:\agent\project')
    Assert-True (@($diagnosticDoc.SelectNodes("//*[local-name()='Message']"))[0].InnerText -eq 'failure reason stays exact' -and @($diagnosticDoc.SelectNodes("//*[local-name()='StackTrace']"))[0].InnerText -eq 'failure stack stays exact' -and @($diagnosticDoc.SelectNodes("//*[local-name()='StdOut']"))[0].InnerText -eq 'failed case output') 'failure reasons, stacks, and case output survive projection unchanged'
    $unknown=Write-Trx 'unknown.trx' ((New-TrxText).Replace('<Times ','<Times bogus="1" '))
    Assert-Throws { Get-TrxFacts $unknown } 'unknown attribute rejects'
    $child=Write-Trx 'child.trx' ((New-TrxText).Replace('</Results>','<AttachmentSet uri="x"/></Results>'))
    Assert-Throws { Get-TrxFacts $child } 'attachment child rejects'
    $comment=Write-Trx 'comment.trx' ((New-TrxText).Replace('<Times','<!-- comment --><Times'))
    Assert-Throws { Get-TrxFacts $comment } 'comment rejects'
    $pi=Write-Trx 'pi.trx' ((New-TrxText).Replace('<Times','<?unsafe x?><Times'))
    Assert-Throws { Get-TrxFacts $pi } 'processing instruction rejects'
    $dtd=Write-Trx 'dtd.trx' ((New-TrxText).Replace('<?xml version="1.0" encoding="utf-8"?>','<?xml version="1.0"?><!DOCTYPE TestRun [<!ENTITY x SYSTEM "file:///private">]>'))
    Assert-Throws { Get-TrxFacts $dtd } 'DTD/external entity rejects'
    $badjoin=Write-Trx 'badjoin.trx' ((New-TrxText).Replace('<UnitTestResult executionId="','<UnitTestResult executionId="{00000000-0000-0000-0000-000000000099}" oldExecutionId="'))
    Assert-Throws { Get-TrxFacts $badjoin } 'broken result join rejects'
    $counter=Write-Trx 'counter.trx' ((New-TrxText).Replace('total="1"','total="2"'))
    Assert-Throws { Get-TrxFacts $counter } 'native counter mutation rejects'
    $emptyDiscovery=Write-Trx 'empty.trx' (New-TrxText -Outcomes @())
    Assert-Throws { Get-TrxFacts $emptyDiscovery } 'empty discovery/report rejects'
    $incompleteText=[regex]::Replace((New-TrxText),'<TestEntries>.*?</TestEntries>','<TestEntries></TestEntries>')
    $incomplete=Write-Trx 'incomplete.trx' $incompleteText
    Assert-Throws { Get-TrxFacts $incomplete } 'incomplete discovery/reference graph rejects'
    $badRunText=[regex]::Replace((New-TrxText),'(<TestRun\b[^>]*?\bid=")[^"]+','$1not-a-guid',1)
    $badRunId=Write-Trx 'bad-run-id.trx' $badRunText
    Assert-Throws { Get-TrxFacts $badRunId } 'malformed native run identity rejects'
    $badSummary=Write-Trx 'bad-summary.trx' ((New-TrxText).Replace('<ResultSummary outcome="Completed"','<ResultSummary outcome="Unsupported"'))
    Assert-Throws { Get-TrxFacts $badSummary } 'unsupported summary outcome rejects'
    $wrongNamespace=Write-Trx 'namespace.trx' (New-TrxText -Namespace 'urn:unsupported')
    Assert-Throws { Get-TrxFacts $wrongNamespace } 'unknown namespace rejects'
    $invalid=Join-Path $root 'invalid.trx';[IO.File]::WriteAllBytes($invalid,[byte[]](0xff,0xfe,0x3c,0x00,0x54,0x00))
    Assert-Throws { Get-TrxFacts $invalid } 'invalid/truncated XML encoding rejects'
    $oversized=Join-Path $root 'oversized.trx';$fs=[IO.File]::Create($oversized);try{$fs.SetLength(268435457)}finally{$fs.Dispose()};Assert-Throws { Get-TrxFacts $oversized } 'size limit rejects oversized report'
    $zip1=Join-Path $root 'one.zip';$zip2=Join-Path $root 'two.zip';Write-DeterministicEvidenceArchive -Path $zip1 -Entries @([ordered]@{name='results/run/cold.trx';bytes=$xmlBytes});Write-DeterministicEvidenceArchive -Path $zip2 -Entries @([ordered]@{name='results/run/cold.trx';bytes=$xmlBytes});Assert-True ((Get-FileHash $zip1 -Algorithm SHA256).Hash -ceq (Get-FileHash $zip2 -Algorithm SHA256).Hash) 'fixed-metadata ZIP is deterministic'
    $orderedZip=Join-Path $root 'ordered.zip';Write-DeterministicEvidenceArchive -Path $orderedZip -Entries @([ordered]@{name='z-last';bytes=$xmlBytes},[ordered]@{name='a-first';bytes=$xmlBytes});$orderArchive=[IO.Compression.ZipFile]::OpenRead($orderedZip);try{$orderedNames=@($orderArchive.Entries|ForEach-Object FullName);Assert-True (($orderedNames -join ',') -ceq 'a-first,z-last') 'ZIP entries use ordinal deterministic ordering'}finally{$orderArchive.Dispose()}
    $beforeNoOverwrite=(Get-FileHash $zip1 -Algorithm SHA256).Hash
    Assert-Throws { Write-DeterministicEvidenceArchive -Path $zip1 -Entries @([ordered]@{name='results/run/other.trx';bytes=$xmlBytes}) } 'ZIP writer refuses to overwrite existing artifacts'
    Assert-True ((Get-FileHash $zip1 -Algorithm SHA256).Hash -ceq $beforeNoOverwrite) 'no-overwrite failure leaves the original archive bytes unchanged'
    Assert-Throws { Write-DeterministicEvidenceArchive -Path (Join-Path $root 'traversal.zip') -Entries @([ordered]@{name='../escape.trx';bytes=$xmlBytes}) } 'ZIP traversal is rejected by writer'
    Assert-Throws { Write-DeterministicEvidenceArchive -Path (Join-Path $root 'duplicate.zip') -Entries @([ordered]@{name='results/run/cold.trx';bytes=$xmlBytes},[ordered]@{name='results/run/cold.trx';bytes=$xmlBytes}) } 'duplicate ZIP entry rejects'

    $runnerId='a'*32;$runs=[System.Collections.Generic.List[object]]::new();$packageEntries=[System.Collections.Generic.List[object]]::new()
    foreach($label in @('cold','warm')){$source=Write-Trx "$label-source.trx" (New-TrxText -Outcomes @('Passed','Passed') -PrivateMetadata);$sourceFacts=Get-TrxFacts $source;$portable=ConvertTo-TrxCanonicalXml (ConvertTo-TrxProjectedDocument $sourceFacts.Document @('C:\agent\project'));$entryName="results/$runnerId/$label.trx";$packageEntries.Add([ordered]@{name=$entryName;bytes=$portable});$runs.Add([ordered]@{label=$label;nativeRunId=$sourceFacts.RunId;nativeRunLabel="run-$($sourceFacts.RunId)";originalReport=[ordered]@{bytes=1;sha256=('a'*64)};normalizedReport=[ordered]@{entry=$entryName;bytes=$portable.Length;sha256=(Get-BytesSha256 $portable);semanticSha256=(Get-TrxSemanticSha256 (ConvertTo-TrxProjectedDocument $sourceFacts.Document @('C:\agent\project')))};summaryOutcome=$sourceFacts.SummaryOutcome;nativeCounters=$sourceFacts.Counters;caseOutcomeCounts=$sourceFacts.OutcomeCounts;resultCount=$sourceFacts.ResultCount;gatePassed=$sourceFacts.GatePassed;exitCode=0;elapsedMilliseconds=100;comparison='source-projection-verified'})}
    $packageZip=Join-Path $root 'valid-package.zip';Write-DeterministicEvidenceArchive -Path $packageZip -Entries @($packageEntries.ToArray());$runtime=Assert-EvidenceRuntime
    $manifest=[ordered]@{schema='validation-evidence-trx-package-v1';bundleId='trx-synthetic';format=[ordered]@{reportProfile='trx-vstest-flat-v1';counterProfile='vstest-17.14.1-flat';projectionPolicy='trx-private-metadata-v1'};tool=[ordered]@{commit=('1'*40);tree=('2'*40);version='1.2.0';files=@([ordered]@{path='Tools/UnityValidation/TrxValidationEvidence.psm1';bytes=10;sha256=('3'*64)});runtime=[ordered]@{powershellEdition='Core';powershellVersion='7.6.5';dotnetVersion='10.0.11';compressionAssemblyVersion=$runtime.compressionAssemblyVersion}};archive=[ordered]@{file='valid-package.zip';bytes=(Get-Item $packageZip).Length;sha256=(Get-FileHash $packageZip -Algorithm SHA256).Hash.ToLowerInvariant();format='zip-deflate-fixed-1980-01-01';entryPolicy='normalized-trx-only'};runs=@([ordered]@{runnerRunId=$runnerId;source=[ordered]@{commit=('4'*40);tree=('5'*40);originalInputDigest=('6'*64);inputStable=$true;files=@();generatedDependencies=@();influencingChanges=@()};originalManifest=[ordered]@{bytes=10;sha256=('7'*64)};producer=[ordered]@{vstestVersion='17.14.1';sdkVersion='9.0.315';logger=[ordered]@{file='Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.dll';fileVersion='17.1400.125.42801';productVersion='17.14.1-release-25428-01';bytes=10;sha256=('8'*64)};identityMode='recovered-validated-output';provenance=@()};execution=[ordered]@{sdkVersion='9.0.315';clrVersion='8.0.28';targetFramework='net10.0';configuration='Release';osVersion='Windows fixture';osArchitecture='X64';processArchitecture='X64';cultures=@('en-US');packageLocks=@();provenance=@()};discovery=[ordered]@{count=2;cases=@('same display name(1)','same display name(2)')};commands=@();assemblies=@();attempts=@($runs.ToArray());demonstrations=@();measurements=[ordered]@{worktreeLibraryAbsent=$true;projectBinObjBytes=@();resultBytes=20};retention=[ordered]@{handle=('evidence-'+('9'*32));ownerRole='GeneralOrchestrator';policy='DOTNET_TRX_EVIDENCE_CONTRACT.md';plannedPostPromotionDays=30;aclVerification='verified-owner-only-known-copies';provenance=@()};existingReviews=[ordered]@{sourceImplementation=('a'*40);toolImplementation=('b'*40)}})}
    $manifestPath=Join-Path $root 'valid-package.manifest.json';[IO.File]::WriteAllBytes($manifestPath,(ConvertTo-EvidenceJsonBytes $manifest));$valid=Test-DotNetTrxEvidencePackage -ManifestPath $manifestPath -ArchivePath $packageZip;Assert-True ($valid.IsValid -and $valid.PackageIntegrityVerified -and $valid.OriginalComparison -eq 'originalComparisonNotPerformed' -and $valid.EntryCount -eq 2 -and $valid.GatePassed) 'archive-only package validation and cold/warm binding'
    $mutation=ConvertFrom-Json -InputObject (ConvertTo-Json -InputObject $manifest -Depth 80 -Compress) -AsHashtable;$mutation.runs[0].attempts=@($mutation.runs[0].attempts|Where-Object label -CEQ 'cold');[IO.File]::WriteAllBytes($manifestPath,(ConvertTo-EvidenceJsonBytes $mutation));Assert-True (-not (Test-DotNetTrxEvidencePackage -ManifestPath $manifestPath -ArchivePath $packageZip).IsValid) 'missing warm attempt is rejected'
    $mutation=ConvertFrom-Json -InputObject (ConvertTo-Json -InputObject $manifest -Depth 80 -Compress) -AsHashtable;$mutation.runs[0].attempts[1].label='cold';[IO.File]::WriteAllBytes($manifestPath,(ConvertTo-EvidenceJsonBytes $mutation));Assert-True (-not (Test-DotNetTrxEvidencePackage -ManifestPath $manifestPath -ArchivePath $packageZip).IsValid) 'duplicate cold attempt label is rejected'
    $mutation=ConvertFrom-Json -InputObject (ConvertTo-Json -InputObject $manifest -Depth 80 -Compress) -AsHashtable;$mutation.runs[0].attempts[0].exitCode=1;[IO.File]::WriteAllBytes($manifestPath,(ConvertTo-EvidenceJsonBytes $mutation));Assert-True (-not (Test-DotNetTrxEvidencePackage -ManifestPath $manifestPath -ArchivePath $packageZip).IsValid) 'passing native report cannot pair with failing command exit'
    $mutated=ConvertFrom-Json ([IO.File]::ReadAllText($manifestPath)) -AsHashtable;$mutated.runs[0].attempts[0].caseOutcomeCounts.Passed=1;[IO.File]::WriteAllBytes($manifestPath,(ConvertTo-EvidenceJsonBytes $mutated));$invalid=Test-DotNetTrxEvidencePackage -ManifestPath $manifestPath -ArchivePath $packageZip;Assert-True (-not $invalid.IsValid) 'recomputed manifest mutation is detected against archived cases'
    $mutated=ConvertFrom-Json ([IO.File]::ReadAllText($manifestPath)) -AsHashtable;$mutated.extra='unapproved';[IO.File]::WriteAllBytes($manifestPath,(ConvertTo-EvidenceJsonBytes $mutated));$invalid=Test-DotNetTrxEvidencePackage -ManifestPath $manifestPath -ArchivePath $packageZip;Assert-True (-not $invalid.IsValid) 'unknown public manifest key rejects'
    $tampered=Join-Path $root 'tampered.zip';[IO.File]::Copy($packageZip,$tampered);$bytes=[IO.File]::ReadAllBytes($tampered);$bytes[$bytes.Length-8]=$bytes[$bytes.Length-8] -bxor 1;[IO.File]::WriteAllBytes($tampered,$bytes);$validManifest=Join-Path $root 'valid-copy.manifest.json';[IO.File]::WriteAllBytes($validManifest,(ConvertTo-EvidenceJsonBytes $manifest));$invalid=Test-DotNetTrxEvidencePackage -ManifestPath $validManifest -ArchivePath $tampered;Assert-True (-not $invalid.IsValid) 'archive tampering is detected'
    $privateTraceRoot=Join-Path $root 'protected-traces';$null=Set-ProtectedEvidenceDirectory $privateTraceRoot
    $traceRunnerId=[guid]::NewGuid().ToString('N');$coldNative=[guid]::NewGuid().ToString('D');$warmNative=[guid]::NewGuid().ToString('D')
    $traceTemplate=@"
Runtime config is cfg=C:\agent\project\bin\Simulation.Core.Tests.runtimeconfig.dev.json
Chose FX version [C:\Program Files\dotnet\shared\Microsoft.NETCore.App\8.0.28]
Runtime config is cfg=C:\Program Files\dotnet\host\fxr\9.0.20\dotnet.runtimeconfig.json
Chose FX version [C:\Program Files\dotnet\shared\Microsoft.NETCore.App\9.0.20]
"@
    $traceRecords=[System.Collections.Generic.List[object]]::new();foreach($label in @('cold','warm')){$tracePath=Join-Path $privateTraceRoot "$label.trace";[IO.File]::WriteAllText($tracePath,$traceTemplate,[Text.UTF8Encoding]::new($false));$traceBytes=[IO.File]::ReadAllBytes($tracePath);$traceNative=if($label -eq 'cold'){$coldNative}else{$warmNative};$traceRecords.Add([ordered]@{gateLabel=$label;runnerRunId=$traceRunnerId;path=$tracePath;bytes=$traceBytes.Length;sha256=(Get-BytesSha256 -Bytes $traceBytes);selectedClr='8.0.28'});$null=$traceNative}
    $traceContext=[ordered]@{worktreeRoots=@('C:\agent\project');runtimeSelectionTraces=@($traceRecords.ToArray());reports=@([ordered]@{label='cold';nativeRunId=$coldNative},[ordered]@{label='warm';nativeRunId=$warmNative})}
    $traceRunner=[ordered]@{runId=$traceRunnerId;gates=[ordered]@{coldTests=[ordered]@{runId=$coldNative;exitCode=0};warmTests=[ordered]@{runId=$warmNative;exitCode=0}}}
    $tracePublicRun=[ordered]@{execution=[ordered]@{clrVersion='8.0.28'};assemblies=@([ordered]@{path='bin/Simulation.Core.Tests.dll'})}
    $trxModule=Get-Module TrxValidationEvidence
    & $trxModule {param($c,$r,$p,$protectedRoot) Assert-TrxRuntimeSelectionEvidence $c $r $p $protectedRoot} $traceContext $traceRunner $tracePublicRun $privateTraceRoot
    Assert-True $true 'test-assembly runtimeconfig selects the bound CLR despite other host runtime versions'
    $tracePublicRun.assemblies[0].path='bin/Simulation.Core.Tests.exe'
    Assert-Throws { & $trxModule {param($c,$r,$p,$protectedRoot) Assert-TrxRuntimeSelectionEvidence $c $r $p $protectedRoot} $traceContext $traceRunner $tracePublicRun $privateTraceRoot } 'runtime selection evidence rejects executable substitution for the test DLL'
    $tracePublicRun.assemblies[0].path='bin/Simulation.Core.Tests.dll'
    $traceContext.runtimeSelectionTraces[0].selectedClr='9.0.20'
    Assert-Throws { & $trxModule {param($c,$r,$p,$protectedRoot) Assert-TrxRuntimeSelectionEvidence $c $r $p $protectedRoot} $traceContext $traceRunner $tracePublicRun $privateTraceRoot } 'selected CLR claim cannot substitute another host runtime version'

    $sourceInputRoot=Join-Path $root 'source-inputs';New-Item -ItemType Directory -Path $sourceInputRoot -Force|Out-Null
    $sourceFile=Join-Path $sourceInputRoot 'input.cs';[IO.File]::WriteAllText($sourceFile,'captured input',[Text.UTF8Encoding]::new($false));$sourceBytes=[IO.File]::ReadAllBytes($sourceFile)
    $sourceRecord=[ordered]@{path='input.cs';bytes=[long]$sourceBytes.Length;sha256=(Get-BytesSha256 $sourceBytes)}
    $sourceInputDigest=Get-BytesSha256 ([Text.Encoding]::UTF8.GetBytes((ConvertTo-Json -InputObject @($sourceRecord) -Depth 6 -Compress)))
    $runnerInput=[ordered]@{source=[ordered]@{commit=('1'*40);committedTree=('2'*40)};inputs=[ordered]@{files=@($sourceRecord);generatedDependencies=@();digest=$sourceInputDigest}}
    Assert-Throws { & $trxModule {param($r,$roots) Assert-TrxRunnerInputInventory $r $roots} $runnerInput @($sourceInputRoot) } 'matching captured input bytes still require exact committed source ancestry'
    [IO.File]::WriteAllText($sourceFile,'mutated input',[Text.UTF8Encoding]::new($false))
    Assert-Throws { & $trxModule {param($r,$roots) Assert-TrxRunnerInputInventory $r $roots} $runnerInput @($sourceInputRoot) } 'changed source input is rejected against its captured manifest identity'
    $ambiguousRoot=Join-Path $root 'source-inputs-copy';New-Item -ItemType Directory -Path $ambiguousRoot -Force|Out-Null;[IO.File]::WriteAllText((Join-Path $ambiguousRoot 'input.cs'),'captured input',[Text.UTF8Encoding]::new($false))
    Assert-Throws { & $trxModule {param($r,$roots) Assert-TrxRunnerInputInventory $r $roots} $runnerInput @($root+'\source-inputs-copy',$sourceInputRoot) } 'ambiguous captured source roots are rejected'

    $contextRoot=Join-Path $root 'private-context';$null=Set-ProtectedEvidenceDirectory $contextRoot
    $runnerPath=Join-Path $contextRoot 'runner-manifest.json';[IO.File]::WriteAllText($runnerPath,'{}',[Text.UTF8Encoding]::new($false))
    $contextPath=Join-Path $contextRoot 'source-context.json';$privateContext=[ordered]@{schema='dotnet-trx-source-context-v1';runnerManifestPath=$runnerPath;runnerManifestIdentity=[ordered]@{bytes=2;sha256=('0'*64)};worktreeRoots=@($sourceInputRoot);retentionHandle=('evidence-'+('1'*32));publicRuns=@();reports=@();runtimeSelectionTraces=@();loggerEvidence=[ordered]@{path='unused';bytes=1;sha256=('0'*64)}}
    [IO.File]::WriteAllBytes($contextPath,(ConvertTo-EvidenceJsonBytes $privateContext))
    Assert-Throws { & $trxModule {param($p) Read-DotNetTrxSourceContext $p} $contextPath } 'private runner-manifest locator cannot substitute for its pre-recorded identity'
    $unsupportedProvenance=[ordered]@{kind='private-context-evidence';path='source-context.json';bytes=1;sha256=('0'*64);field='/runs/0/retention/aclVerification';mode='captured'}
    Assert-Throws { & $trxModule {param($r) Assert-TrxProvenanceRecord $r} $unsupportedProvenance } 'public provenance rejects kinds outside the promoted closed enum'
    $projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'));$currentLocation=(Get-Location).Path
    $commonDirectory=(& $trxModule {param($r) Invoke-TrxGit $r @('rev-parse','--git-common-dir')} $projectRoot | Select-Object -First 1)
    $commonValue=[string]$commonDirectory;$commonPath=[IO.Path]::GetFullPath($(if([IO.Path]::IsPathRooted($commonValue)){$commonValue}else{Join-Path $projectRoot $commonValue}));$otherWorktree=Split-Path -Parent $commonPath
    $otherHead=@(& $trxModule {param($r) Invoke-TrxGit $r @('rev-parse','HEAD')} $otherWorktree)
    Assert-True ($otherWorktree -and -not [string]::Equals($otherWorktree.TrimEnd('\'),$projectRoot.TrimEnd('\'),[StringComparison]::OrdinalIgnoreCase) -and $otherHead.Count -eq 1 -and [string]$otherHead[0] -match '^[a-f0-9]{40,64}$' -and (Get-Location).Path -ceq $currentLocation) 'Push-Location Git helper resolves a second worktree and restores caller location'
    Write-Output "TRX evidence synthetic tests PASS: $script:Assertions assertions."
} finally { Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue }
