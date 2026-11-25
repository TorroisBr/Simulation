$ErrorActionPreference='Stop'
$root=[IO.Directory]::GetParent([IO.Directory]::GetParent([IO.Directory]::GetParent($PSScriptRoot).FullName).FullName).FullName
Import-Module (Join-Path $root 'Tools/UnityValidation/TrxValidationEvidence.psm1') -Force
$m=Get-Module TrxValidationEvidence
$script:Assertions=0
function Check($ok,$message){if(-not $ok){throw $message};$script:Assertions++}
function Reject([scriptblock]$action,[string]$pattern,[string]$message){try{& $action;throw 'MISSING_EXPECTED_REJECTION'}catch{if($_.Exception.Message -ceq 'MISSING_EXPECTED_REJECTION'){throw $message};if($_.Exception.Message -notlike $pattern){throw "$message Unexpected error: $($_.Exception.Message)"}};$script:Assertions++}
function Copy-JsonValue($v){ConvertFrom-Json -InputObject (ConvertTo-Json -InputObject $v -Depth 30 -Compress) -AsHashtable}

$p=& $m {
 $components=@($script:AtlasAPlatformPins|ForEach-Object{[ordered]@{name=$_.name;fileVersion=$_.fileVersion;productVersion=$_.productVersion;bytes=$_.bytes;sha256=$_.sha256}})
 [ordered]@{
  vstestConsole=[ordered]@{file=$script:AtlasAVstestConsolePin.file;fileVersion=$script:AtlasAVstestConsolePin.fileVersion;productVersion=$script:AtlasAVstestConsolePin.productVersion;bytes=$script:AtlasAVstestConsolePin.bytes;sha256=$script:AtlasAVstestConsolePin.sha256}
  logger=[ordered]@{file=$script:AtlasATrxLoggerPin.file;fileVersion=$script:AtlasATrxLoggerPin.fileVersion;productVersion=$script:AtlasATrxLoggerPin.productVersion;bytes=$script:AtlasATrxLoggerPin.bytes;sha256=$script:AtlasATrxLoggerPin.sha256}
  platformComponents=$components;dotnetFileVersion='10,0,1526,53008 @Commit: 8e0f8ed2312954727f8919d7d1c2f78b483b4ee7'
  testAssembly=[ordered]@{path='WorldAtlas.Tests/Release/net10.0/WorldAtlas.Tests.dll';fullName='WorldAtlas.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null';mvid='20061699-6e7d-4c75-92c7-7aa3e3cd93cb';bytes=68608L;sha256='0cbbf13c0fa1ede0f21173c309e14d04ad0a9f62188e7fcd6359f9d22b5b6f2a';pdb=[ordered]@{bytes=22464L;sha256='d6f298b8affc9181b8e258c07a88c6b14e32f41dc861429e5497b4452d2f41ae'}}
 }
}
& $m {param($x) Assert-AtlasAProducerMatrix $x} $p
$script:Assertions++

$b=Copy-JsonValue $p;$b.vstestConsole.sha256='0'*64
Reject {& $m {param($x) Assert-AtlasAProducerMatrix $x} $b} '*vstestConsole sha256*' 'VSTest console mutation was accepted.'
$b=Copy-JsonValue $p;$b.logger.productVersion='18.12.0'
Reject {& $m {param($x) Assert-AtlasAProducerMatrix $x} $b} '*logger productVersion*' 'Logger mutation was accepted.'
$b=Copy-JsonValue $p;$b.platformComponents[2].sha256='0'*64
Reject {& $m {param($x) Assert-AtlasAProducerMatrix $x} $b} '*platform component*' 'Platform hash mutation was accepted.'
$b=Copy-JsonValue $p;$t=$b.platformComponents[0];$b.platformComponents[0]=$b.platformComponents[1];$b.platformComponents[1]=$t
Reject {& $m {param($x) Assert-AtlasAProducerMatrix $x} $b} '*platform component*' 'Platform order mutation was accepted.'
$b=Copy-JsonValue $p;$b.testAssembly.mvid='30061699-6e7d-4c75-92c7-7aa3e3cd93cb'
Reject {& $m {param($x) Assert-AtlasAProducerMatrix $x} $b} '*test assembly identity*' 'Assembly mutation was accepted.'
$b=Copy-JsonValue $p;$b.testAssembly.pdb.sha256='0'*64
Reject {& $m {param($x) Assert-AtlasAProducerMatrix $x} $b} '*test PDB identity*' 'PDB mutation was accepted.'

$r=Join-Path ([IO.Path]::GetTempPath()) ('AtlasAProfileTests-'+[guid]::NewGuid().ToString('N'))
$inside=Join-Path $r 'nested/file.json'
$resolved=& $m {param($a,$b) Get-AtlasAPathWithinRoot $a $b 'test'} $r $inside
Check ([IO.Path]::GetFullPath($resolved) -ceq [IO.Path]::GetFullPath($inside)) 'Valid root mapping failed.'
Reject {& $m {param($a,$b) Get-AtlasAPathWithinRoot $a $b 'escape'} $r (Join-Path $r '..\outside')} '*escapes its declared root*' 'Escaping root was accepted.'

$evidenceRoot='C:\Private\ATLAS-A\controlled-recapture-20261125'
$mappedEvidence=& $m {param($v,$root) ConvertTo-AtlasAPublicEvidenceText $v $root} ($evidenceRoot+'\vstest-diagnostics.host.log') $evidenceRoot
Check ($mappedEvidence -ceq '<EVIDENCE>/controlled-recapture-20261125\vstest-diagnostics.host.log') 'Evidence-root privacy mapping failed.'
$mappedEvidenceForward=& $m {param($v,$root) ConvertTo-AtlasAPublicEvidenceText $v $root} 'c:/private/atlas-a/controlled-recapture-20261125/vstest-diagnostics.host.log' $evidenceRoot
Check ($mappedEvidenceForward -ceq '<EVIDENCE>/controlled-recapture-20261125/vstest-diagnostics.host.log') 'Case/slash-variant evidence-root privacy mapping failed.'

$captureArgv=[string[]]::new(18);$captureArgv[11]='C:\Private\ATLAS-A\run';$captureArgv[13]='C:\Private\ATLAS-A\run\vstest-diagnostics.log';$captureArgv[16]='-p:DirectoryBuildPropsPath=C:\Temp\world-atlas-build\Directory.Build.props'
$runnerArgv=[string[]]$captureArgv.Clone();$expectedHelper='C:\Temp\world-atlas-build\Directory.Build.props'
& $m {param($c,$r,$root,$diag,$helper) Assert-AtlasARawCommandPathBinding $c $r $root $diag $helper} $captureArgv $runnerArgv 'C:\Private\ATLAS-A\run' 'C:\Private\ATLAS-A\run\vstest-diagnostics.log' $expectedHelper
$script:Assertions++
$badCapture=[string[]]$captureArgv.Clone();$badCapture[13]='C:\Private\ATLAS-A\wrong.log'
Reject {& $m {param($c,$r,$root,$diag,$helper) Assert-AtlasARawCommandPathBinding $c $r $root $diag $helper} $badCapture $runnerArgv 'C:\Private\ATLAS-A\run' 'C:\Private\ATLAS-A\run\vstest-diagnostics.log' $expectedHelper} '*raw results/diagnostic*' 'Altered raw diagnostic argv was accepted.'
$badRunner=[string[]]$runnerArgv.Clone();$badRunner[16]='-p:DirectoryBuildPropsPath=C:\Temp\other\Directory.Build.props'
Reject {& $m {param($c,$r,$root,$diag,$helper) Assert-AtlasARawCommandPathBinding $c $r $root $diag $helper} $captureArgv $badRunner 'C:\Private\ATLAS-A\run' 'C:\Private\ATLAS-A\run\vstest-diagnostics.log' $expectedHelper} '*raw build-helper*' 'Altered raw build-helper argv was accepted.'

$input=@([ordered]@{path='test-output/b.bin';bytes=2;sha256='b'*64},[ordered]@{path='test-output/a.bin';bytes=1;sha256='a'*64})
$sorted=& $m {param($x) ConvertTo-AtlasAInventory $x 'test' '' 2} $input
Check ($sorted[0].path -ceq 'test-output/a.bin') 'Inventory ordering is not ordinal.'
$dup=@($input[0],$input[0])
Reject {& $m {param($x) ConvertTo-AtlasAInventory $x 'duplicates' '' 0} $dup} '*duplicate path*' 'Duplicate path was accepted.'
$absolute=@([ordered]@{path='C:\private\file.bin';bytes=1;sha256='a'*64})
Reject {& $m {param($x) ConvertTo-AtlasAInventory $x 'absolute' '' 0} $absolute} '*safe relative path*' 'Absolute path was accepted.'

$projection=& $m {$script:TrxProjection}
$manifest=[ordered]@{schema='validation-evidence-trx-package-v1';bundleId='atlas-a-profile-test';format=[ordered]@{reportProfile='trx-vstest-flat-v1';counterProfile='vstest-18.12.0-flat';projectionPolicy=$projection;runProfile='unsupported-profile'};tool=[ordered]@{};archive=[ordered]@{};runs=@()}
Reject {& $m {param($x) Assert-AtlasATrxPublicManifest $x} $manifest} '*Unsupported ATLAS-A TRX producer/counter profile*' 'Unknown run profile was accepted.'

Write-Output "ATLAS-A profile synthetic tests PASS: $script:Assertions assertions."
