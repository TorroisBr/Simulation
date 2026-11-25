 $script:AtlasAVstestConsolePin=[ordered]@{file='vstest.console.dll';fileVersion='18.700.126.53019';productVersion='18.7.1-hotfix-26529-119';bytes=665424L;sha256='14b087bd3a77d0eb809b2d04ef38cae66050cf9efd917b4b9a8f62fe0afa00eb'}
 $script:AtlasATrxLoggerPin=[ordered]@{file='Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.dll';fileVersion='18.700.126.53019';productVersion='18.7.1-hotfix-26529-119';bytes=243496L;sha256='e942a5c25eb2b6c986d49f644515d57262b635dc545f4f0407af49ea8f73d6c8'}
 $script:AtlasAPlatformPins=@(
    [ordered]@{name='Microsoft.TestPlatform.AdapterUtilities.dll';fileVersion='18.1000.126.46511';productVersion='18.10.1';bytes=42808L;sha256='0020748391a1ae8fc6f3af6f200aae6eb4a25b9c7dacb533ebb554ed4b9441c0'},
    [ordered]@{name='Microsoft.TestPlatform.CommunicationUtilities.dll';fileVersion='18.1200.26.56015';productVersion='18.12.0';bytes=531296L;sha256='2b869a478c6a18ddf19ff77dde1c6608c031983336f528b7d6d68b2db33f5e84'},
    [ordered]@{name='Microsoft.TestPlatform.CoreUtilities.dll';fileVersion='18.1200.26.56015';productVersion='18.12.0';bytes=102712L;sha256='7aaaef512d4efb122ccc89eea54a9f0a737481424d174d6ba2477c674a3f2e7e'},
    [ordered]@{name='Microsoft.TestPlatform.CrossPlatEngine.dll';fileVersion='18.1200.26.56015';productVersion='18.12.0';bytes=735032L;sha256='53eb59bd28a22c6af9d0e85c8fc4eb0d65809ae1906e757a67fc03685b377b8a'},
    [ordered]@{name='Microsoft.TestPlatform.PlatformAbstractions.dll';fileVersion='18.1200.26.56015';productVersion='18.12.0';bytes=53048L;sha256='f7a0ee54ef6340e13a3bff85cec0ba6f77151b06f49cc92edbf4419355bc99b9'},
    [ordered]@{name='Microsoft.TestPlatform.Utilities.dll';fileVersion='18.1200.26.56015';productVersion='18.12.0';bytes=69432L;sha256='94dd571445ca0a6e106b661a4d950ad0d84d31de1a1a04ee4ceac49c3bdc049a'},
    [ordered]@{name='Microsoft.VisualStudio.TestPlatform.Common.dll';fileVersion='18.1200.26.56015';productVersion='18.12.0';bytes=252768L;sha256='42194c3073cfce7166a8aeb59c75fc2e69a2c917f8054b8da7e7cd098834a8a6'},
    [ordered]@{name='Microsoft.VisualStudio.TestPlatform.ObjectModel.dll';fileVersion='18.1200.26.56015';productVersion='18.12.0';bytes=355640L;sha256='954816d202190a5c4e861548f1319ee612b898cb56ad24fcd0ef1de921a10eb2'},
    [ordered]@{name='NUnit3.TestAdapter.dll';fileVersion='6.3.1.0';productVersion='6.3.1+3fcfb233a6b4ceee0f7f28dab41994d118e701d8';bytes=182272L;sha256='0f837859814a3e13d79bc908ce0d5f86f82f89268b2ca2ac12376f69db2a4cad'},
    [ordered]@{name='testhost.dll';fileVersion='18.1200.26.56015';productVersion='18.12.0';bytes=49504L;sha256='3e9ac238400d8d72cba32776182060a5421c7630da7355360c46c0763df9043e'}
 )

function Assert-AtlasAProducerMatrix {
    param([System.Collections.IDictionary]$Producer)
    foreach($name in @('vstestConsole','logger')){
        $expected=if($name -ceq 'vstestConsole'){$script:AtlasAVstestConsolePin}else{$script:AtlasATrxLoggerPin}
        $actual=$Producer[$name]
        foreach($field in @('file','fileVersion','productVersion','bytes','sha256')){if([string]$actual[$field] -cne [string]$expected[$field]){throw "ATLAS-A producer $name $field differs from its captured binary identity."}}
    }
    if(@($Producer.platformComponents).Count -ne $script:AtlasAPlatformPins.Count){throw 'ATLAS-A engine/testhost inventory differs from the captured 18.12.0 profile.'}
    for($index=0;$index -lt $script:AtlasAPlatformPins.Count;$index++){
        $expected=$script:AtlasAPlatformPins[$index];$actual=$Producer.platformComponents[$index]
        foreach($field in @('name','fileVersion','productVersion','bytes','sha256')){if([string]$actual[$field] -cne [string]$expected[$field]){throw "ATLAS-A platform component '$($expected.name)' $field differs from its captured binary identity."}}
    }
    if([string]$Producer.dotnetFileVersion -cne '10,0,1526,53008 @Commit: 8e0f8ed2312954727f8919d7d1c2f78b483b4ee7'){throw 'ATLAS-A dotnet host version identity is unsupported.'}
    $assembly=$Producer.testAssembly
    if([string]$assembly.path -cne 'WorldAtlas.Tests/Release/net10.0/WorldAtlas.Tests.dll' -or [string]$assembly.fullName -cne 'WorldAtlas.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null' -or [string]$assembly.mvid -cne '20061699-6e7d-4c75-92c7-7aa3e3cd93cb' -or [long]$assembly.bytes -ne 68608L -or [string]$assembly.sha256 -cne '0cbbf13c0fa1ede0f21173c309e14d04ad0a9f62188e7fcd6359f9d22b5b6f2a'){throw 'ATLAS-A test assembly identity differs from the captured validated output.'}
    if([long]$assembly.pdb.bytes -ne 22464L -or [string]$assembly.pdb.sha256 -cne 'd6f298b8affc9181b8e258c07a88c6b14e32f41dc861429e5497b4452d2f41ae'){throw 'ATLAS-A test PDB identity differs from the captured validated output.'}
}

function Get-AtlasAPathWithinRoot {
    param([string]$Root,[string]$Path,[string]$Where)
    $fullRoot=[IO.Path]::GetFullPath($Root).TrimEnd('\','/')
    $fullPath=[IO.Path]::GetFullPath($Path)
    $relative=[IO.Path]::GetRelativePath($fullRoot,$fullPath)
    if([IO.Path]::IsPathRooted($relative) -or $relative -eq '..' -or $relative.StartsWith('..'+[IO.Path]::DirectorySeparatorChar,[StringComparison]::Ordinal) -or $relative.StartsWith('../',[StringComparison]::Ordinal) -or $relative.StartsWith('..\',[StringComparison]::Ordinal)){
        throw "$Where escapes its declared root."
    }
    return $fullPath
}

function Get-AtlasAFileHashRecord {
    param([System.Collections.IDictionary]$Record,[string]$Where,[string]$Prefix='')
    Assert-TrxKeys $Record @('path','bytes','sha256') $Where
    Assert-TrxJsonInteger $Record.bytes "$Where bytes" 0
    $relative=ConvertTo-TrxPortableRelative ([string]$Record.path).Replace('\','/')
    if([string]$Record.sha256 -notmatch '^[a-fA-F0-9]{64}$'){throw "$Where SHA-256 is malformed."}
    $path=if($Prefix){ConvertTo-TrxPortableRelative "$Prefix/$relative"}else{$relative}
    return [ordered]@{path=$path;bytes=[long]$Record.bytes;sha256=([string]$Record.sha256).ToLowerInvariant()}
}

function ConvertTo-AtlasAInventory {
    param([object[]]$Items,[string]$Where,[string]$Prefix='',[int]$ExpectedCount=0)
    $map=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
    foreach($item in $Items){
        if($item -isnot [System.Collections.IDictionary]){throw "$Where entries must be objects."}
        $record=Get-AtlasAFileHashRecord $item "$Where[]" $Prefix
        if(-not $map.TryAdd([string]$record.path,$record)){throw "$Where contains a duplicate path."}
    }
    if($ExpectedCount -gt 0 -and $map.Count -ne $ExpectedCount){throw "$Where has an unexpected entry count."}
    $paths=[string[]]@($map.Keys);[Array]::Sort($paths,[StringComparer]::Ordinal)
    $result=[Collections.Generic.List[object]]::new();foreach($path in $paths){$result.Add($map[$path])}
    return @($result.ToArray())
}

function Assert-AtlasAInventory {
    param([object[]]$Items,[string]$Where,[int]$ExpectedCount=0,[string]$Prefix='')
    $normalized=ConvertTo-AtlasAInventory $Items $Where $Prefix $ExpectedCount
    if((ConvertTo-Json -InputObject @($normalized) -Depth 20 -Compress) -cne (ConvertTo-Json -InputObject @($Items) -Depth 20 -Compress)){throw "$Where is not in canonical inventory form."}
    return $normalized
}

function Assert-AtlasAFileAtPath {
    param([string]$Path,[long]$Bytes,[string]$Sha256,[string]$ProtectedRoot,[string]$Where)
    $full=[IO.Path]::GetFullPath($Path)
    if(-not (Test-Path -LiteralPath $full -PathType Leaf)){throw "$Where is missing."}
    $item=Get-Item -LiteralPath $full -Force
    if(($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw "$Where cannot be a reparse point."}
    if($ProtectedRoot){$null=Assert-ProtectedEvidenceFile -Path $full -ProtectedRoot $ProtectedRoot}
    if($item.Length -ne $Bytes -or (Get-EvidenceSha256 $full) -cne $Sha256.ToLowerInvariant()){throw "$Where bytes/hash do not match their capture identity."}
    return $full
}

function Get-AtlasAPublicArgumentVector {
    param([object[]]$ArgumentVector)
    if(@($ArgumentVector).Count -ne 18){throw 'ATLAS-A captured command must contain exactly 18 arguments.'}
    $args=[string[]]@($ArgumentVector|ForEach-Object {[string]$_})
    $args[11]='<EVIDENCE>/controlled-recapture-20261125'
    $args[13]='<EVIDENCE>/controlled-recapture-20261125/vstest-diagnostics.log'
    $helperPrefix='-p:DirectoryBuildPropsPath='
    if(-not $args[16].StartsWith($helperPrefix,[StringComparison]::OrdinalIgnoreCase)){throw 'ATLAS-A build-helper argument is missing from its captured position.'}
    $args[16]='-p:DirectoryBuildPropsPath=<TEMP>/world-atlas-build/Directory.Build.props'
    return $args
}


function Assert-AtlasACaptureRecordBinding {
    param([pscustomobject]$Private)
    $runner=$Private.Runner
    $bytes=[IO.File]::ReadAllBytes([string]$Private.CaptureRecordPath)
    if($bytes.LongLength -ne 244836L -or (Get-TrxBytesSha256 $bytes) -cne '719f990db8fd03e6a6a2215ee51f150b5606940928dc94f347388dabf6331520'){throw 'ATLAS-A controlled capture record is not the accepted immutable record.'}
    $capture=([Text.UTF8Encoding]::new($false,$true).GetString($bytes)|ConvertFrom-Json -AsHashtable -DateKind String)
    Assert-TrxKeys $capture @('schema','source','run','toolchain','inputs','invocation','artifacts') 'ATLAS-A controlled capture record'
    if($capture.schema -cne 'atlas-a-recapture-runner-v1'){throw 'Unsupported ATLAS-A controlled capture-record schema.'}
    if($capture.source.commit -cne $runner.source.commit -or $capture.source.tree -cne $runner.source.tree -or -not [bool]$capture.source.clean -or [int]$capture.source.trackedWorldAtlasFileCount -ne 50 -or $capture.source.validationRecordTip -cne '11b89d4aba3961f767bf4c86056f3382e193afd1'){throw 'ATLAS-A controlled capture source identity differs from the runner manifest.'}
    $captureFiles=ConvertTo-AtlasAInventory @($capture.source.trackedWorldAtlasInventory) 'capture source inventory' '' 50
    $runnerFiles=ConvertTo-AtlasAInventory @($runner.source.trackedWorldAtlasFiles) 'runner source inventory' '' 50
    if((ConvertTo-Json -InputObject @($captureFiles) -Depth 10 -Compress) -cne (ConvertTo-Json -InputObject @($runnerFiles) -Depth 10 -Compress)){throw 'ATLAS-A controlled capture and runner source inventories differ.'}
    if([string]$runner.source.inputDigest -cne '0b128c6340d38f86351f2908d0bb4bd4e895e2bf6aacb6ebb60b68bc309eab3b'){throw 'ATLAS-A controlled source input digest differs from the accepted capture.'}
    $inputNames=[string[]]@($capture.inputs.hashes.Keys);[Array]::Sort($inputNames,[StringComparer]::Ordinal)
    $runnerInputNames=[string[]]@($runner.source.inputHashes.Keys);[Array]::Sort($runnerInputNames,[StringComparer]::Ordinal)
    if($inputNames.Count -ne 11 -or (ConvertTo-Json -InputObject @($inputNames) -Compress) -cne (ConvertTo-Json -InputObject @($runnerInputNames) -Compress)){throw 'ATLAS-A controlled capture input-hash set differs from the runner manifest.'}
    foreach($name in $inputNames){if(([string]$capture.inputs.hashes[$name]).ToLowerInvariant() -cne ([string]$runner.source.inputHashes[$name]).ToLowerInvariant()){throw "ATLAS-A controlled capture input hash differs for '$name'."}}
    if($capture.inputs.buildHelper.path -cne 'Directory.Build.props' -or ([string]$capture.inputs.buildHelper.sha256).ToLowerInvariant() -cne ([string]$runner.inputs.buildHelper.sha256).ToLowerInvariant()){throw 'ATLAS-A controlled capture build-helper identity differs from the runner.'}
    if($capture.inputs.testOutputRoot -cne 'WorldAtlas.Tests/bin/Release/net10.0' -or $runner.inputs.testOutputRoot -cne 'WorldAtlas.Tests/Release/net10.0'){throw 'ATLAS-A controlled capture output-root mapping differs from the accepted path mapping.'}
    $captureOutput=ConvertTo-AtlasAInventory @($capture.inputs.testOutputInventory) 'capture test-output inventory' '' 288
    $runnerOutput=ConvertTo-AtlasAInventory @($runner.inputs.testOutputInventory) 'runner test-output inventory' '' 288
    if((ConvertTo-Json -InputObject @($captureOutput) -Depth 10 -Compress) -cne (ConvertTo-Json -InputObject @($runnerOutput) -Depth 10 -Compress)){throw 'ATLAS-A controlled capture and runner 288-file output inventories differ.'}
    if(-not (Test-TrxClaimValueEqual $capture.inputs.packageCacheVersionPins $runner.inputs.packageCacheVersionPins) -or -not (Test-TrxClaimValueEqual $runner.inputs.packageCacheVersionPins $runner.toolchain.packageVersions) -or $capture.inputs.runtimeSelection -cne 'WorldAtlas.sln; one test assembly; 29 result rows'){throw 'ATLAS-A controlled capture package/runtime selection differs from the runner.'}
    $run=$capture.run
    foreach($field in @('runId','startedUtc','finishedUtc','trxStart','trxFinish','exitCode','total','passed','failed','notExecuted','resultRows')){if([string]$run[$field] -cne [string]$runner.run[$field]){throw "ATLAS-A controlled capture run field '$field' differs from the runner manifest."}}
    if([string]$run.runId -cne 'e6f63b8b-9f2f-44cc-bff5-a11ee8666f4a' -or [int]$run.total -ne 29 -or [int]$run.passed -ne 29 -or [int]$run.failed -ne 0 -or [int]$run.notExecuted -ne 0 -or [int]$run.exitCode -ne 0){throw 'ATLAS-A controlled capture is not the exact accepted 29/29 passing execution.'}
    if($capture.toolchain.sdkVersion -cne $runner.toolchain.sdkVersion -or $capture.toolchain.dotnetFileVersion -cne $runner.toolchain.dotnetFileVersion -or [string]$runner.toolchain.runtimeVersion -cne '10.0.15' -or [string]$runner.toolchain.testPlatformVersion -cne '18.12.0'){throw 'ATLAS-A controlled capture SDK/runtime identity differs from the runner.'}
    $logger=$capture.toolchain.testPlatformLogger
    $runnerLoggerName=Split-Path -Leaf ([string]$runner.toolchain.logger.relativePath)
    if($logger.file -cne $runnerLoggerName -or $logger.relativePath -cne 'sdk/10.0.404/Extensions/Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.dll'){throw 'ATLAS-A controlled capture TRX logger path differs from the runner.'}
    foreach($field in @('fileVersion','productVersion','bytes','sha256')){if(([string]$logger[$field]).ToLowerInvariant() -cne ([string]$runner.toolchain.logger[$field]).ToLowerInvariant()){throw "ATLAS-A controlled capture logger $field differs from the runner."}}
    $assembly=$capture.toolchain.testAssembly;$runnerAssembly=$runner.toolchain.testAssembly
    if([IO.Path]::GetFileName([string]$assembly.path) -cne [string]$runnerAssembly.name){throw 'ATLAS-A controlled capture test-assembly path differs from the runner.'}
    foreach($pair in @(@('bytes','bytes'),@('sha256','sha256'),@('mvid','mvid'),@('pdbBytes','pdb.bytes'),@('pdbSha256','pdb.sha256'))){$left=$pair[0];$right=$pair[1];$expected=if($right.StartsWith('pdb.')){[string]$runnerAssembly.pdb[$right.Substring(4)]}else{[string]$runnerAssembly[$right]};if(([string]$assembly[$left]).ToLowerInvariant() -cne $expected.ToLowerInvariant()){throw "ATLAS-A controlled capture assembly '$left' differs from the runner."}}
    $argv=Get-AtlasAPublicArgumentVector @($runner.invocation.argv);$captureArgv=Get-AtlasAPublicArgumentVector @($capture.invocation.argv)
    if((ConvertTo-Json -InputObject @($argv) -Compress) -cne (ConvertTo-Json -InputObject @($captureArgv) -Compress)){throw 'ATLAS-A controlled capture command differs from the runner invocation.'}
    $resultsRoot=[IO.Path]::GetFullPath([string]$capture.invocation.argv[11]);$expectedEvidenceSuffix='WorldAtlas/.validation-evidence/ATLAS-A-3e4ed67a-2026-11-25/controlled-recapture-20261125'
    if(-not $resultsRoot.Replace('\','/').EndsWith('/'+$expectedEvidenceSuffix,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFullPath([string]$runner.invocation.argv[11]) -cne $resultsRoot){throw 'ATLAS-A captured test-host evidence root differs from the exact accepted run folder.'}
    if($capture.invocation.executable -cne 'dotnet' -or $capture.invocation.workingDirectory -cne '<ATLAS_SOURCE_ROOT>' -or $capture.invocation.environment.NUGET_PACKAGES -cne '<PRIVATE_NUGET_CACHE>' -or $capture.invocation.environment.DOTNET_ROOT -cne '<PRIVATE_SDK_ROOT>' -or [long]$capture.invocation.environment.processPathLengthBefore -ne 26028 -or [long]$capture.invocation.environment.processPathLengthDuring -ne 26100 -or [string]$capture.invocation.environment.DOTNET_MULTILEVEL_LOOKUP -cne '0'){throw 'ATLAS-A captured invocation/environment identity is invalid.'}
    $dotnetRoot=[IO.Path]::GetFullPath([string]$runner.invocation.environment.DOTNET_ROOT);$nugetRoot=[IO.Path]::GetFullPath([string]$runner.invocation.environment.NUGET_PACKAGES)
    if(-not (Test-Path -LiteralPath $dotnetRoot -PathType Container) -or -not (Test-Path -LiteralPath (Join-Path $dotnetRoot 'dotnet.exe') -PathType Leaf) -or -not (Test-Path -LiteralPath $nugetRoot -PathType Container)){throw 'ATLAS-A captured SDK/NuGet roots are not available for binding.'}
    $dotnetVersion=[Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $dotnetRoot 'dotnet.exe'))
    if($dotnetVersion.FileVersion -cne [string]$runner.toolchain.dotnetFileVersion){throw 'ATLAS-A captured dotnet host file-version differs from the manifest.'}
    $raw=$capture.artifacts.trx;$diag=$capture.artifacts.loggerDiagnostics;$console=$capture.artifacts.console;$old=$capture.artifacts.originalRunTrx
    foreach($pair in @(@($raw,40431L,'fa0e16f267c5a11656222ba530882741e3c0d5c10dac0618edaa879c4a9e2cf6'),@($diag,163762L,'606915d560190fee069ba8c0384c638dae140ac45a3e9a3cbfddc27798e0693b'),@($console,810L,'3534b28b7318ea5c608ea792ab7bcd04a752cb5acf77d3870ecc434340be4019'))){if([long]$pair[0].bytes -ne [long]$pair[1] -or ([string]$pair[0].sha256).ToLowerInvariant() -cne [string]$pair[2]){throw 'ATLAS-A controlled capture artifact identity differs from its fixed evidence record.'}}
    if($raw.name -cne 'ATLAS-A-3e4ed67a-recapture.trx' -or $diag.name -cne 'vstest-diagnostics.log' -or $console.name -cne 'test-console.txt' -or $old.name -cne 'ATLAS-A-ux-3e4ed67a.trx' -or [long]$old.bytes -ne 40163L -or ([string]$old.sha256).ToLowerInvariant() -cne '7b72bd1708a78eb762b95668128551f647c2c77a88f96a270d4fafc58d554907' -or -not [bool]$old.unchanged){throw 'ATLAS-A original 29/29 TRX evidence is not recorded as immutable.'}
    foreach($pair in @(@($raw,$runner.artifacts.rawReport),@($diag,$runner.artifacts.diagnostics),@($console,$runner.artifacts.console),@($old,$runner.artifacts.originalTrx))){$captured=$pair[0];$runnerRecord=$pair[1];if([long]$captured.bytes -ne [long]$runnerRecord.bytes -or ([string]$captured.sha256).ToLowerInvariant() -cne ([string]$runnerRecord.sha256).ToLowerInvariant()){throw 'ATLAS-A capture and runner artifact identities differ.'}}
    if([IO.Path]::GetFullPath([string]$runner.artifacts.rawReport.path) -cne (Join-Path $resultsRoot $raw.name) -or [IO.Path]::GetFullPath([string]$runner.artifacts.diagnostics.path) -cne (Join-Path $resultsRoot $diag.name) -or [IO.Path]::GetFullPath([string]$runner.artifacts.console.path) -cne (Join-Path $resultsRoot $console.name)){throw 'ATLAS-A captured evidence artifacts do not resolve beneath the pinned run folder.'}
    if([IO.Path]::GetFileName([string]$runner.artifacts.rawReport.path) -cne $raw.name -or [IO.Path]::GetFileName([string]$runner.artifacts.diagnostics.path) -cne $diag.name -or [IO.Path]::GetFileName([string]$runner.artifacts.console.path) -cne $console.name -or [IO.Path]::GetFileName([string]$runner.artifacts.originalTrx.path) -cne $old.name){throw 'ATLAS-A capture and runner artifact filenames differ.'}
}

function ConvertTo-AtlasAPublicEvidenceText {
    param([string]$Value,[string]$EvidenceRoot)
    if(-not [IO.Path]::IsPathRooted($EvidenceRoot)){throw 'ATLAS-A evidence path root must be absolute before privacy projection.'}
    $variants=@($EvidenceRoot,$EvidenceRoot.Replace('\','/'),$EvidenceRoot.Replace('/','\'))|Select-Object -Unique
    foreach($variant in $variants){$Value=$Value.Replace([string]$variant,'<EVIDENCE>/controlled-recapture-20261125',[StringComparison]::OrdinalIgnoreCase)}
    return $Value
}

function ConvertTo-AtlasAPublicDocument {
    param([pscustomobject]$Private,[pscustomobject]$Facts)
    $roots=@($Private.SourceRoot,$Private.TestOutputRoot)
    $document=ConvertTo-TrxProjectedDocument $Facts.Document $roots
    $arguments=@($Private.Runner.invocation.argv)
    if($arguments.Count -ne 18){throw 'ATLAS-A captured command cannot bind an evidence output directory.'}
    $evidenceRoot=[IO.Path]::GetFullPath([string]$arguments[11]);$replacementCount=0
    foreach($element in @($document.SelectNodes('//*'))){
        foreach($attribute in $element.Attributes){$before=[string]$attribute.Value;$after=ConvertTo-AtlasAPublicEvidenceText $before $evidenceRoot;if($after -cne $before){$attribute.Value=$after;$replacementCount++}}
        if($element.LocalName -in @('StdOut','StdErr','DebugTrace','Message','StackTrace','Text')){$before=[string]$element.InnerText;$after=ConvertTo-AtlasAPublicEvidenceText $before $evidenceRoot;if($after -cne $before){$element.InnerText=$after;$replacementCount++}}
    }
    if($replacementCount -lt 1){throw 'ATLAS-A captured test-host diagnostic path was absent from the TRX projection.'}
    return $document
}

function Get-AtlasAPublicRunModel {
    param([pscustomobject]$Private,[pscustomobject]$Facts,[byte[]]$NormalizedBytes)
    $runner=$Private.Runner;$capturePath=$Private.CaptureRecordPath
    $sourceFiles=ConvertTo-AtlasAInventory @($runner.source.trackedWorldAtlasFiles) 'source files' '' 50
    $generated=ConvertTo-AtlasAInventory @($runner.inputs.testOutputInventory) 'generated dependency inventory' ([string]$runner.inputs.testOutputRoot) 288
    $locks=ConvertTo-AtlasAInventory @($runner.toolchain.packageLocks) 'NuGet package locks' '' 3
    $components=[Collections.Generic.List[object]]::new()
    foreach($component in @($runner.toolchain.platformComponents)){
        $components.Add([ordered]@{name=[string]$component.name;fileVersion=[string]$component.fileVersion;productVersion=[string]$component.productVersion;bytes=[long]$component.bytes;sha256=([string]$component.sha256).ToLowerInvariant()})
    }
    $componentsArray=@($components.ToArray()|Sort-Object -Property name)
    $assembly=$runner.toolchain.testAssembly
    $assemblyPath=ConvertTo-TrxPortableRelative (([string]$runner.inputs.testOutputRoot).TrimEnd('/')+'/'+([string]$assembly.relativePath).Replace('\','/'))
    $assemblyPublic=[ordered]@{path=$assemblyPath;fullName=[string]$assembly.fullName;mvid=[string]$assembly.mvid;bytes=[long]$assembly.bytes;sha256=([string]$assembly.sha256).ToLowerInvariant();identityMode='captured-execution-output';pdb=[ordered]@{bytes=[long]$assembly.pdb.bytes;sha256=([string]$assembly.pdb.sha256).ToLowerInvariant();identityMode='captured-execution-output'}}
    $locksPublic=@($locks|ForEach-Object {[ordered]@{path=$_.path;bytes=$_.bytes;sha256=$_.sha256}})
    $argv=Get-AtlasAPublicArgumentVector @($runner.invocation.argv)
    $environment=[ordered]@{
        NUGET_PACKAGES='<PACKAGE_CACHE>'
        DOTNET_ROOT='<TOOLCHAIN>'
        DOTNET_MULTILEVEL_LOOKUP=[string]$runner.invocation.environment.DOTNET_MULTILEVEL_LOOKUP
        processPathLengthBefore=[long]$runner.invocation.environment.processPathLengthBefore
        processPathLengthDuring=[long]$runner.invocation.environment.processPathLengthDuring
    }
    $helper=[ordered]@{path='<TEMP>/world-atlas-build/Directory.Build.props';bytes=[long](Get-Item -LiteralPath $Private.BuildHelperPath).Length;sha256=([string]$runner.inputs.buildHelper.sha256).ToLowerInvariant()}
    $reportBytes=[IO.File]::ReadAllBytes([string]$Private.ReportPath)
    $runId=([guid]::Parse([string]$runner.run.runId)).ToString('N')
    $normalizedEntry="results/$runId/atlas-a.trx"
    $attempt=[ordered]@{
        label='primary';nativeRunId=$Facts.RunId;nativeRunLabel="run-$($Facts.RunId)"
        originalReport=[ordered]@{bytes=[long]$reportBytes.LongLength;sha256=(Get-TrxBytesSha256 $reportBytes)}
        normalizedReport=[ordered]@{entry=$normalizedEntry;bytes=[long]$NormalizedBytes.LongLength;sha256=(Get-TrxBytesSha256 $NormalizedBytes);semanticSha256=(Get-TrxBytesSha256 $NormalizedBytes)}
        summaryOutcome=$Facts.SummaryOutcome;nativeCounters=$Facts.Counters;caseOutcomeCounts=$Facts.OutcomeCounts
        resultCount=[int]$Facts.ResultCount;gatePassed=[bool]$Facts.GatePassed;exitCode=[int]$runner.run.exitCode
        elapsedMilliseconds=[double]$runner.run.elapsedMilliseconds;comparison='source-projection-verified'
    }
    $cases=@($runner.run.cases|ForEach-Object {[string]$_.testName})
    $retained=[ordered]@{handle=[string]$Private.Context.retentionHandle;ownerRole='GeneralOrchestrator';policy='DOTNET_TRX_EVIDENCE_CONTRACT.md';aclVerification='verified-current-owner-only';retainedOutputCount=[int]$runner.inputs.retainedOutputCount;retainedOutputBytes=[long]$runner.inputs.retainedOutputBytes}
    return [ordered]@{
        runnerRunId=$runId
        source=[ordered]@{commit=([string]$runner.source.commit).ToLowerInvariant();tree=([string]$runner.source.tree).ToLowerInvariant();originalInputDigest=([string]$runner.source.inputDigest).ToLowerInvariant();inputStable=[bool]$runner.source.clean;files=$sourceFiles;generatedDependencies=$generated}
        captureRecord=[ordered]@{bytes=[long]$runner.captureRecord.bytes;sha256=([string]$runner.captureRecord.sha256).ToLowerInvariant()}
        originalManifest=[ordered]@{bytes=[long]$Private.RunnerBytes.LongLength;sha256=$Private.RunnerSha256}
        producer=[ordered]@{
            sdkVersion=[string]$runner.toolchain.sdkVersion;runtimeVersion=[string]$runner.toolchain.runtimeVersion;dotnetFileVersion=[string]$runner.toolchain.dotnetFileVersion;testPlatformVersion=[string]$runner.toolchain.testPlatformVersion
            vstestConsole=[ordered]@{file='vstest.console.dll';fileVersion=[string]$runner.toolchain.vstestConsole.fileVersion;productVersion=[string]$runner.toolchain.vstestConsole.productVersion;bytes=[long]$runner.toolchain.vstestConsole.bytes;sha256=([string]$runner.toolchain.vstestConsole.sha256).ToLowerInvariant()}
            logger=[ordered]@{file='Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.dll';fileVersion=[string]$runner.toolchain.logger.fileVersion;productVersion=[string]$runner.toolchain.logger.productVersion;bytes=[long]$runner.toolchain.logger.bytes;sha256=([string]$runner.toolchain.logger.sha256).ToLowerInvariant()}
            platformComponents=$componentsArray;packageVersions=$runner.toolchain.packageVersions;packageCacheVersionPins=$runner.inputs.packageCacheVersionPins;packageLocks=$locksPublic;testAssembly=$assemblyPublic
        }
        execution=[ordered]@{
            startedUtc=[string]$runner.run.startedUtc;finishedUtc=[string]$runner.run.finishedUtc;trxStart=[string]$runner.run.trxStart;trxFinish=[string]$runner.run.trxFinish
            elapsedMilliseconds=[double]$runner.run.elapsedMilliseconds;exitCode=[int]$runner.run.exitCode;summaryOutcome=[string]$runner.run.summaryOutcome
            targetFramework='net10.0';configuration='Release';noBuild=$true;noRestore=$true
            invocation=[ordered]@{executable='<TOOLCHAIN>/dotnet.exe';workingDirectory='<WORKTREE>';argv=$argv;environment=$environment;buildHelper=$helper}
        }
        discovery=[ordered]@{count=[int]$runner.run.total;cases=$cases}
        capturedEvidence=[ordered]@{
            diagnostics=[ordered]@{file='vstest-diagnostics.log';bytes=[long]$runner.artifacts.diagnostics.bytes;sha256=([string]$runner.artifacts.diagnostics.sha256).ToLowerInvariant()}
            console=[ordered]@{file='test-console.txt';bytes=[long]$runner.artifacts.console.bytes;sha256=([string]$runner.artifacts.console.sha256).ToLowerInvariant()}
        }
        attempts=@($attempt);retention=$retained
    }
}

function Read-AtlasASourceContext {
    param([string]$SourceContextPath)
    $contextPath=[IO.Path]::GetFullPath($SourceContextPath);$contextRoot=Split-Path -Parent $contextPath
    $null=Test-ProtectedEvidenceDirectory $contextRoot;$null=Assert-ProtectedEvidenceFile -Path $contextPath -ProtectedRoot $contextRoot
    $contextBytes=[IO.File]::ReadAllBytes($contextPath)
    if($contextBytes.Length -gt $script:TrxMaxManifestBytes){throw 'ATLAS-A private source context exceeds the size limit.'}
    $context=([Text.UTF8Encoding]::new($false,$true).GetString($contextBytes)|ConvertFrom-Json -AsHashtable -DateKind String)
    Assert-TrxKeys $context @('schema','runnerManifestPath','runnerManifestIdentity','retentionHandle','buildHelperEvidencePath','rawReportEvidencePath','diagnosticsEvidencePath','consoleEvidencePath','captureRecordEvidencePath','retainedOutputEvidenceRoot','toolchainEvidenceRoot','testOutputRoot') 'ATLAS-A source context'
    if($context.schema -cne 'dotnet-trx-atlas-a-source-context-v1' -or [string]$context.retentionHandle -notmatch '^evidence-[a-f0-9]{32}$'){throw 'Unsupported ATLAS-A source-context schema or retention handle.'}
    Assert-TrxKeys $context.runnerManifestIdentity @('bytes','sha256') 'ATLAS-A runner manifest identity'
    Assert-TrxJsonInteger $context.runnerManifestIdentity.bytes 'ATLAS-A runner manifest bytes' 1
    if([string]$context.runnerManifestIdentity.sha256 -notmatch '^[a-f0-9]{64}$'){throw 'ATLAS-A runner manifest hash is malformed.'}
    $runnerPath=[IO.Path]::GetFullPath([string]$context.runnerManifestPath);$null=Assert-ProtectedEvidenceFile -Path $runnerPath -ProtectedRoot $contextRoot
    $runnerBytes=[IO.File]::ReadAllBytes($runnerPath)
    if($runnerBytes.LongLength -ne [long]$context.runnerManifestIdentity.bytes -or (Get-TrxBytesSha256 $runnerBytes) -cne [string]$context.runnerManifestIdentity.sha256){throw 'ATLAS-A runner manifest identity changed.'}
    $runner=([Text.UTF8Encoding]::new($false,$true).GetString($runnerBytes)|ConvertFrom-Json -AsHashtable -DateKind String)
    Assert-TrxKeys $runner @('schema','profile','captureRecord','source','run','toolchain','inputs','invocation','artifacts','retention') 'ATLAS-A runner manifest'
    if($runner.schema -cne 'atlas-a-29-runner-manifest-v1' -or $runner.profile -cne 'atlas-a-29-tests-v1'){throw 'Unsupported private ATLAS-A runner profile.'}
    $sourceRoot=[IO.Path]::GetFullPath([string]$runner.source.root)
    $testOutputRoot=[IO.Path]::GetFullPath([string]$context.testOutputRoot)
    if($testOutputRoot -cne [IO.Path]::GetFullPath([string]$runner.artifacts.sourceTestOutputRoot)){throw 'ATLAS-A source context test-output root differs from captured execution context.'}
    foreach($folder in @($testOutputRoot,$sourceRoot)){if(-not(Test-Path -LiteralPath $folder -PathType Container) -or ((Get-Item $folder -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw 'ATLAS-A source/test-output root is missing or a reparse point.'}}
    $retainedOutputRoot=Get-AtlasAPathWithinRoot $contextRoot ([string]$context.retainedOutputEvidenceRoot) 'ATLAS-A retained output root'
    $toolchainRoot=Get-AtlasAPathWithinRoot $contextRoot ([string]$context.toolchainEvidenceRoot) 'ATLAS-A protected toolchain root'
    $buildHelperPath=Get-AtlasAPathWithinRoot $contextRoot ([string]$context.buildHelperEvidencePath) 'ATLAS-A protected build-helper copy'
    $reportPath=Get-AtlasAPathWithinRoot $contextRoot ([string]$context.rawReportEvidencePath) 'ATLAS-A protected TRX copy'
    $diagnosticsPath=Get-AtlasAPathWithinRoot $contextRoot ([string]$context.diagnosticsEvidencePath) 'ATLAS-A protected diagnostic-log copy'
    $consolePath=Get-AtlasAPathWithinRoot $contextRoot ([string]$context.consoleEvidencePath) 'ATLAS-A protected console copy'
    $capturePath=Get-AtlasAPathWithinRoot $contextRoot ([string]$context.captureRecordEvidencePath) 'ATLAS-A protected execution-record copy'
    foreach($path in @($buildHelperPath,$reportPath,$diagnosticsPath,$consolePath,$capturePath)){$null=Assert-ProtectedEvidenceFile -Path $path -ProtectedRoot $contextRoot}
    return [pscustomobject]@{Path=$contextPath;Root=$contextRoot;Context=$context;ContextBytes=$contextBytes;RunnerPath=$runnerPath;RunnerBytes=$runnerBytes;Runner=$runner;RunnerSha256=(Get-TrxBytesSha256 $runnerBytes);SourceRoot=$sourceRoot;TestOutputRoot=$testOutputRoot;RetainedOutputRoot=$retainedOutputRoot;ToolchainRoot=$toolchainRoot;BuildHelperPath=$buildHelperPath;ReportPath=$reportPath;DiagnosticsPath=$diagnosticsPath;ConsolePath=$consolePath;CaptureRecordPath=$capturePath}
}

function Assert-AtlasACapturedRun {
    param([pscustomobject]$Private)
    $runner=$Private.Runner;$privateRoot=$Private.Root
    if([string]$runner.source.commit -cne '3e4ed67aecec74e857f6be080dea0e4f7cad5646' -or [string]$runner.source.tree -cne 'b0b572257a580570e8d1a36f7c0480ad937d2436' -or -not [bool]$runner.source.clean){throw 'ATLAS-A capture is not bound to the accepted clean source tree.'}
    if([int]$runner.run.total -ne 29 -or [int]$runner.run.passed -ne 29 -or [int]$runner.run.failed -ne 0 -or [int]$runner.run.notExecuted -ne 0 -or [int]$runner.run.exitCode -ne 0 -or [string]$runner.run.summaryOutcome -cne 'Completed'){throw 'ATLAS-A captured run is not the complete 29/29 passing run.'}
    if([string]$runner.toolchain.sdkVersion -cne '10.0.404' -or [string]$runner.toolchain.runtimeVersion -cne '10.0.15' -or [string]$runner.toolchain.testPlatformVersion -cne '18.12.0' -or [string]$runner.toolchain.vstestConsole.productVersion -cne '18.7.1-hotfix-26529-119' -or [string]$runner.toolchain.logger.productVersion -cne '18.7.1-hotfix-26529-119'){throw 'ATLAS-A producer version matrix does not match the captured profile.'}
    if(@($runner.source.trackedWorldAtlasFiles).Count -ne 50 -or @($runner.inputs.testOutputInventory).Count -ne 288 -or @($runner.run.cases).Count -ne 29 -or [string]$runner.source.inputDigest -notmatch '^[a-f0-9]{64}$'){throw 'ATLAS-A captured input or test inventory is incomplete.'}
    $null=Test-ProtectedEvidenceDirectory $privateRoot
    $null=Test-ProtectedEvidenceDirectory $Private.ToolchainRoot
    $null=Test-ProtectedEvidenceDirectory $Private.RetainedOutputRoot
    $capture=$runner.captureRecord;$null=Assert-AtlasAFileAtPath $Private.CaptureRecordPath ([long]$capture.bytes) ([string]$capture.sha256) $privateRoot 'ATLAS-A execution record'
    Assert-AtlasACaptureRecordBinding $Private
    $report=$runner.artifacts.rawReport;$reportPath=$Private.ReportPath;$null=Assert-AtlasAFileAtPath $reportPath ([long]$report.bytes) ([string]$report.sha256) $privateRoot 'ATLAS-A original TRX'
    $null=Assert-AtlasAFileAtPath $Private.DiagnosticsPath ([long]$runner.artifacts.diagnostics.bytes) ([string]$runner.artifacts.diagnostics.sha256) $privateRoot 'VSTest diagnostic log'
    $null=Assert-AtlasAFileAtPath $Private.ConsolePath ([long]$runner.artifacts.console.bytes) ([string]$runner.artifacts.console.sha256) $privateRoot 'ATLAS-A test console'
    $null=Assert-AtlasAFileAtPath $Private.BuildHelperPath (Get-Item $Private.BuildHelperPath).Length ([string]$runner.inputs.buildHelper.sha256) $privateRoot 'captured build helper copy'
    if([IO.Path]::GetFileName($Private.ToolchainRoot) -cne 'toolchain' -or [IO.Path]::GetFileName($Private.RetainedOutputRoot) -cne 'execution-output'){throw 'ATLAS-A secure evidence copy roots have unsupported names.'}
    foreach($record in @($runner.toolchain.vstestConsole,$runner.toolchain.logger)){if(-not ([string]$record.relativePath).StartsWith('toolchain/',[StringComparison]::Ordinal)){throw 'Captured toolchain evidence path has an unsupported prefix.'}}
    $consolePath=Assert-AtlasAFileAtPath (Join-Path $Private.ToolchainRoot ([IO.Path]::GetFileName([string]$runner.toolchain.vstestConsole.relativePath))) ([long]$runner.toolchain.vstestConsole.bytes) ([string]$runner.toolchain.vstestConsole.sha256) $privateRoot 'VSTest console binary'
    $loggerPath=Assert-AtlasAFileAtPath (Join-Path $Private.ToolchainRoot ([IO.Path]::GetFileName([string]$runner.toolchain.logger.relativePath))) ([long]$runner.toolchain.logger.bytes) ([string]$runner.toolchain.logger.sha256) $privateRoot 'VSTest TRX logger binary'
    $loggerVersion=[Diagnostics.FileVersionInfo]::GetVersionInfo($loggerPath)
    if($loggerVersion.FileVersion -cne [string]$runner.toolchain.logger.fileVersion -or $loggerVersion.ProductVersion -cne [string]$runner.toolchain.logger.productVersion){throw 'Captured VSTest logger file-version resources disagree.'}
    $consoleVersion=[Diagnostics.FileVersionInfo]::GetVersionInfo($consolePath)
    if($consoleVersion.FileVersion -cne [string]$runner.toolchain.vstestConsole.fileVersion -or $consoleVersion.ProductVersion -cne [string]$runner.toolchain.vstestConsole.productVersion){throw 'Captured VSTest console file-version resources disagree.'}
    $null=Get-AtlasAPathWithinRoot $Private.SourceRoot $Private.SourceRoot 'ATLAS-A source root'
    $commit=(@(Invoke-TrxGit $Private.SourceRoot @('rev-parse','HEAD')) -join "`n").Trim();$tree=(@(Invoke-TrxGit $Private.SourceRoot @('rev-parse','HEAD^{tree}')) -join "`n").Trim()
    if($commit -cne [string]$runner.source.commit -or $tree -cne [string]$runner.source.tree){throw 'Current ATLAS-A source worktree no longer matches the captured commit/tree.'}
    $dirty=@(Invoke-TrxGit $Private.SourceRoot @('status','--porcelain=v1','--untracked-files=no','--','WorldAtlas'));if($dirty.Count -gt 0){throw 'ATLAS-A source files changed after the captured run.'}
    $sourceMap=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
    foreach($record in @($runner.source.trackedWorldAtlasFiles)){
        $normalized=Get-AtlasAFileHashRecord $record 'captured ATLAS-A source file'
        if(-not $normalized.path.StartsWith('WorldAtlas/',[StringComparison]::Ordinal) -or -not $sourceMap.TryAdd($normalized.path,$normalized)){throw 'ATLAS-A tracked source inventory has a duplicate or out-of-scope path.'}
        $filePath=Join-Path $Private.SourceRoot ($normalized.path.Replace('/',[IO.Path]::DirectorySeparatorChar));$null=Assert-AtlasAFileAtPath $filePath $normalized.bytes $normalized.sha256 $null 'ATLAS-A source input'
        $blob=(@(Invoke-TrxGit $Private.SourceRoot @('rev-parse',([string]$runner.source.commit+':'+$normalized.path))) -join "`n").Trim();$currentBlob=(@(Invoke-TrxGit $Private.SourceRoot @('hash-object','--',$normalized.path)) -join "`n").Trim()
        if($blob -cne $currentBlob){throw 'ATLAS-A source input no longer matches its committed Git blob.'}
    }
    foreach($path in $runner.source.inputHashes.Keys){if(-not $sourceMap.ContainsKey([string]$path)){throw 'Explicit ATLAS-A input hash is not present in the tracked source inventory.'};if(([string]$runner.source.inputHashes[$path]).ToLowerInvariant() -cne [string]$sourceMap[[string]$path].sha256){throw 'Explicit ATLAS-A input hash differs from the tracked source inventory.'}}
    $null=Get-AtlasAPathWithinRoot $Private.TestOutputRoot $Private.TestOutputRoot 'ATLAS-A test output root'
    $expectedOutput=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
    foreach($item in @($runner.inputs.testOutputInventory)){$record=Get-AtlasAFileHashRecord $item 'captured generated dependency';if(-not $expectedOutput.TryAdd([string]$record.path,$record)){throw 'ATLAS-A generated output inventory contains duplicate paths.'}}
    $actualOutput=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
    $outputDirectories=@(Get-ChildItem -LiteralPath $Private.TestOutputRoot -Directory -Recurse -Force)
    foreach($directory in $outputDirectories){if(($directory.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw 'ATLAS-A generated output contains a reparse directory.'}}
    foreach($item in @(Get-ChildItem -LiteralPath $Private.TestOutputRoot -File -Recurse -Force)){
        if(($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw 'ATLAS-A generated output contains a reparse file.'}
        $relative=[IO.Path]::GetRelativePath($Private.TestOutputRoot,$item.FullName).Replace('\','/')
        if(-not $actualOutput.TryAdd($relative,[ordered]@{bytes=[long]$item.Length;sha256=(Get-EvidenceSha256 $item.FullName).ToLowerInvariant()})){throw 'ATLAS-A generated output contains duplicate paths.'}
    }
    if($actualOutput.Count -ne 288 -or $expectedOutput.Count -ne 288){throw 'ATLAS-A generated output is not the exact 288-file captured inventory.'}
    foreach($path in $expectedOutput.Keys){if(-not $actualOutput.ContainsKey($path) -or [long]$actualOutput[$path].bytes -ne [long]$expectedOutput[$path].bytes -or [string]$actualOutput[$path].sha256 -cne [string]$expectedOutput[$path].sha256){throw "ATLAS-A generated output differs from capture for '$path'."}}
    $retainedMap=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
    foreach($item in @(Get-ChildItem -LiteralPath $Private.RetainedOutputRoot -File -Recurse -Force)){
        if(($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw 'Retained ATLAS-A output contains a reparse point.'}
        $relative=[IO.Path]::GetRelativePath($Private.RetainedOutputRoot,$item.FullName).Replace('\','/')
        if($retainedMap.ContainsKey($relative)){throw 'Retained ATLAS-A output has a duplicate relative path.'}
        $retainedMap.Add($relative,[ordered]@{bytes=[long]$item.Length;sha256=(Get-EvidenceSha256 $item.FullName).ToLowerInvariant()})
    }
    if($retainedMap.Count -ne [int]$runner.inputs.retainedOutputCount){throw 'Retained ATLAS-A execution output count differs from its capture.'}
    $retainedBytes=0L;foreach($path in $retainedMap.Keys){$item=@($runner.inputs.testOutputInventory|Where-Object {([string]$_.path).Replace('\','/') -ceq $path});if($item.Count -ne 1 -or [long]$item[0].bytes -ne [long]$retainedMap[$path].bytes -or ([string]$item[0].sha256).ToLowerInvariant() -cne [string]$retainedMap[$path].sha256){throw 'Retained ATLAS-A output differs from its capture-time generated inventory.'};$retainedBytes+=[long]$retainedMap[$path].bytes}
    if($retainedBytes -ne [long]$runner.inputs.retainedOutputBytes){throw 'Retained ATLAS-A output byte count differs from its capture.'}
    $assembly=$runner.toolchain.testAssembly;$assemblyPath=Join-Path $Private.TestOutputRoot ([string]$assembly.relativePath);$assemblyPath=Assert-AtlasAFileAtPath $assemblyPath ([long]$assembly.bytes) ([string]$assembly.sha256) $null 'ATLAS-A test assembly'
    $identity=Get-TrxAssemblyIdentity $assemblyPath
    if($identity.fullName -cne [string]$assembly.fullName -or $identity.mvid -cne [string]$assembly.mvid){throw 'ATLAS-A test assembly name/MVID differs from capture.'}
    $pdb=Assert-TrxMatchingPortablePdb $assemblyPath
    if([long]$pdb.Bytes -ne [long]$assembly.pdb.bytes -or ([string]$pdb.Sha256).ToLowerInvariant() -cne ([string]$assembly.pdb.sha256).ToLowerInvariant()){throw 'ATLAS-A test assembly PDB differs from capture.'}
    $facts=Get-TrxFacts $reportPath
    # The legacy mapper requires each supplied root to match. TRX storage and codeBase are bound only to this captured assembly root.
    $assemblyRoots=@($Private.TestOutputRoot)
    foreach($definition in @($facts.Document.SelectNodes("//*[local-name()='UnitTest']"))){
        $method=$definition.SelectSingleNode("*[local-name()='TestMethod']");if($null -eq $method){throw 'ATLAS-A TRX test definition lacks its test-method assembly binding.'}
        $storage=Get-TrxMappedRelativePath $definition.GetAttribute('storage') $assemblyRoots;$codeBase=Get-TrxMappedRelativePath $method.GetAttribute('codeBase') $assemblyRoots
        if(-not [string]::Equals($storage,$codeBase,[StringComparison]::OrdinalIgnoreCase)){throw 'ATLAS-A TRX storage and codeBase do not bind to the same test assembly.'}
        $resolved=Resolve-TrxWorktreeFile $codeBase $assemblyRoots
        if(-not [string]::Equals($resolved,$assemblyPath,[StringComparison]::OrdinalIgnoreCase)){throw 'ATLAS-A TRX test definition targets an assembly outside the captured test assembly identity.'}
    }
    if(@($runner.toolchain.platformComponents).Count -ne 10){throw 'ATLAS-A VSTest platform component inventory is incomplete.'}
    foreach($component in @($runner.toolchain.platformComponents)){
        $componentPath=Join-Path $Private.TestOutputRoot ([string]$component.relativePath);$null=Assert-AtlasAFileAtPath $componentPath ([long]$component.bytes) ([string]$component.sha256) $null 'ATLAS-A test platform component'
        $version=[Diagnostics.FileVersionInfo]::GetVersionInfo($componentPath)
        if($version.FileVersion -cne [string]$component.fileVersion -or $version.ProductVersion -cne [string]$component.productVersion){throw "ATLAS-A platform component '$($component.name)' version resources differ from capture."}
    }
    if($facts.ResultCount -ne 29 -or $facts.RunId -cne [string]$runner.run.runId -or $facts.SummaryOutcome -cne 'Completed' -or -not $facts.GatePassed){throw 'ATLAS-A original TRX identity or gate result differs from the captured run.'}
    if(-not (Test-TrxClaimValueEqual $facts.Counters $runner.run.nativeCounters) -or -not (Test-TrxClaimValueEqual $facts.OutcomeCounts $runner.run.caseOutcomeCounts)){throw 'ATLAS-A original TRX native counters/outcome histogram differ from captured execution facts.'}
    if(-not (Test-TrxClaimValueEqual $runner.inputs.packageLocks $runner.toolchain.packageLocks) -or -not (Test-TrxClaimValueEqual $runner.inputs.packageCacheVersionPins $runner.toolchain.packageVersions)){throw 'ATLAS-A captured package locks or package cache pins are inconsistent.'}
    $null=Assert-TrxDiscoveryMatchesReport @($runner.run.cases|ForEach-Object {[string]$_.testName}) $facts 'ATLAS-A primary'
    $caseMap=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal);foreach($case in @($runner.run.cases)){$key="$($case.testId)|$($case.executionId)";if(-not $caseMap.TryAdd($key,$case)){throw 'ATLAS-A capture contains duplicate case identities.'}}
    foreach($result in $facts.Results){$key="$($result.GetAttribute('testId'))|$($result.GetAttribute('executionId'))";if(-not $caseMap.ContainsKey($key)){throw 'ATLAS-A TRX result identity is absent from the original capture.'};$captured=$caseMap[$key];if([string]$captured.testName -cne $result.GetAttribute('testName') -or [string]$captured.outcome -cne $result.GetAttribute('outcome')){throw 'ATLAS-A TRX result name/outcome differs from capture.'}}
    $interval=Get-TrxReportUtcInterval $facts.Document
    if($interval.StartUtc -ne [DateTimeOffset]::Parse([string]$runner.run.trxStart).ToUniversalTime() -or $interval.FinishUtc -ne [DateTimeOffset]::Parse([string]$runner.run.trxFinish).ToUniversalTime()){throw 'ATLAS-A TRX interval differs from capture.'}
    return $facts
}

function Assert-AtlasATrxPublicManifest {
    param([System.Collections.IDictionary]$Manifest)
    Assert-TrxKeys $Manifest @('schema','bundleId','format','tool','archive','runs') 'ATLAS-A manifest'
    if($Manifest.schema -cne 'validation-evidence-trx-package-v1' -or [string]$Manifest.bundleId -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,79}$'){throw 'ATLAS-A evidence package schema or bundle ID is invalid.'}
    Assert-TrxKeys $Manifest.format @('reportProfile','counterProfile','projectionPolicy','runProfile') 'ATLAS-A format'
    if($Manifest.format.reportProfile -cne 'trx-vstest-flat-v1' -or $Manifest.format.counterProfile -cne 'vstest-18.12.0-flat' -or $Manifest.format.projectionPolicy -cne $script:TrxProjection -or $Manifest.format.runProfile -cne 'atlas-a-29-tests-v1'){throw 'Unsupported ATLAS-A TRX producer/counter profile.'}
    Assert-TrxKeys $Manifest.tool @('commit','tree','version','files','runtime') 'ATLAS-A tool'
    if([string]$Manifest.tool.commit -notmatch '^[a-f0-9]{40,64}$' -or [string]$Manifest.tool.tree -notmatch '^[a-f0-9]{40,64}$' -or [string]$Manifest.tool.version -cne '1.3.0'){throw 'ATLAS-A package tool identity is malformed.'}
    $toolPaths=@('Tools/UnityValidation/Package-UnityValidationEvidence.ps1','Tools/UnityValidation/UnityValidationEvidence.psm1','Tools/UnityValidation/TrxValidationEvidence.psm1','Tools/UnityValidation/TrxAtlasAProfile.ps1')
    if(@($Manifest.tool.files).Count -ne $toolPaths.Count){throw 'ATLAS-A package tool file inventory is incomplete.'}
    for($index=0;$index -lt $toolPaths.Count;$index++){$file=$Manifest.tool.files[$index];Assert-TrxKeys $file @('path','bytes','sha256') 'ATLAS-A tool files[]';if([string]$file.path -cne $toolPaths[$index] -or [string]$file.sha256 -notmatch '^[a-f0-9]{64}$'){throw 'ATLAS-A tool source inventory is invalid.'};Assert-TrxJsonInteger $file.bytes 'ATLAS-A tool file bytes' 1}
    Assert-TrxKeys $Manifest.tool.runtime @('powershellEdition','powershellVersion','dotnetVersion','compressionAssemblyVersion') 'ATLAS-A tool runtime'
    Assert-TrxKeys $Manifest.archive @('file','bytes','sha256','format','entryPolicy') 'ATLAS-A archive'
    Assert-TrxJsonInteger $Manifest.archive.bytes 'ATLAS-A archive bytes' 1
    if([string]$Manifest.archive.file -cne ([string]$Manifest.bundleId+'.zip') -or [string]$Manifest.archive.sha256 -notmatch '^[a-f0-9]{64}$' -or $Manifest.archive.format -cne 'zip-deflate-fixed-1980-01-01' -or $Manifest.archive.entryPolicy -cne 'normalized-trx-only'){throw 'ATLAS-A archive identity or entry policy is invalid.'}
    if(@($Manifest.runs).Count -ne 1){throw 'ATLAS-A package requires exactly one captured invocation.'}
    $run=$Manifest.runs[0]
    Assert-TrxKeys $run @('runnerRunId','source','captureRecord','originalManifest','producer','execution','discovery','capturedEvidence','attempts','retention') 'ATLAS-A run'
    if([string]$run.runnerRunId -notmatch '^[a-f0-9]{32}$'){throw 'ATLAS-A runner run ID is invalid.'}
    Assert-TrxKeys $run.source @('commit','tree','originalInputDigest','inputStable','files','generatedDependencies') 'ATLAS-A source'
    if([string]$run.source.commit -cne '3e4ed67aecec74e857f6be080dea0e4f7cad5646' -or [string]$run.source.tree -cne 'b0b572257a580570e8d1a36f7c0480ad937d2436' -or [string]$run.source.originalInputDigest -notmatch '^[a-f0-9]{64}$' -or -not [bool]$run.source.inputStable){throw 'ATLAS-A public source identity is not the accepted stable tree.'}
    Assert-AtlasAInventory @($run.source.files) 'ATLAS-A source files' 50|Out-Null
    $generatedInventory=Assert-AtlasAInventory @($run.source.generatedDependencies) 'ATLAS-A generated dependencies' 288
    foreach($record in $generatedInventory){if(-not ([string]$record.path).StartsWith('WorldAtlas.Tests/Release/net10.0/',[StringComparison]::Ordinal)){throw 'ATLAS-A generated dependency paths must use the captured project output namespace.'}}
    foreach($recordName in @('captureRecord','originalManifest')){Assert-TrxKeys $run[$recordName] @('bytes','sha256') "ATLAS-A $recordName";Assert-TrxJsonInteger $run[$recordName].bytes "ATLAS-A $recordName bytes" 1;if([string]$run[$recordName].sha256 -notmatch '^[a-f0-9]{64}$'){throw "ATLAS-A $recordName hash is invalid."}}
    Assert-TrxKeys $run.producer @('sdkVersion','runtimeVersion','dotnetFileVersion','testPlatformVersion','vstestConsole','logger','platformComponents','packageVersions','packageCacheVersionPins','packageLocks','testAssembly') 'ATLAS-A producer'
    if($run.producer.sdkVersion -cne '10.0.404' -or $run.producer.runtimeVersion -cne '10.0.15' -or $run.producer.testPlatformVersion -cne '18.12.0'){throw 'ATLAS-A SDK/runtime/test platform version is unsupported.'}
    foreach($name in @('vstestConsole','logger')){Assert-TrxKeys $run.producer[$name] @('file','fileVersion','productVersion','bytes','sha256') "ATLAS-A producer.$name";Assert-TrxJsonInteger $run.producer[$name].bytes "ATLAS-A $name bytes" 1;if([string]$run.producer[$name].sha256 -notmatch '^[a-f0-9]{64}$' -or [string]$run.producer[$name].productVersion -cne '18.7.1-hotfix-26529-119'){throw "ATLAS-A $name identity does not match the actual captured logger/console producer."}}
    if([string]$run.producer.logger.file -cne 'Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.dll' -or [string]$run.producer.vstestConsole.file -cne 'vstest.console.dll'){throw 'ATLAS-A VSTest producer filenames are invalid.'}
    if(@($run.producer.platformComponents).Count -ne 10){throw 'ATLAS-A VSTest engine/testhost component inventory is incomplete.'}
    foreach($component in @($run.producer.platformComponents)){Assert-TrxKeys $component @('name','fileVersion','productVersion','bytes','sha256') 'ATLAS-A platform component';Assert-TrxJsonInteger $component.bytes 'ATLAS-A component bytes' 1;if([string]$component.sha256 -notmatch '^[a-f0-9]{64}$'){throw 'ATLAS-A component hash is malformed.'}}
    Assert-TrxKeys $run.producer.testAssembly @('path','fullName','mvid','bytes','sha256','identityMode','pdb') 'ATLAS-A test assembly'
    $null=ConvertTo-TrxPortableRelative ([string]$run.producer.testAssembly.path);Assert-TrxJsonInteger $run.producer.testAssembly.bytes 'ATLAS-A test assembly bytes' 1
    if([string]$run.producer.testAssembly.identityMode -cne 'captured-execution-output' -or [string]$run.producer.testAssembly.sha256 -notmatch '^[a-f0-9]{64}$' -or [string]$run.producer.testAssembly.mvid -notmatch '^[a-f0-9-]{36}$'){throw 'ATLAS-A test assembly identity is invalid.'}
    Assert-TrxKeys $run.producer.testAssembly.pdb @('bytes','sha256','identityMode') 'ATLAS-A test PDB';Assert-TrxJsonInteger $run.producer.testAssembly.pdb.bytes 'ATLAS-A test PDB bytes' 1
    if([string]$run.producer.testAssembly.pdb.identityMode -cne 'captured-execution-output' -or [string]$run.producer.testAssembly.pdb.sha256 -notmatch '^[a-f0-9]{64}$'){throw 'ATLAS-A PDB identity is invalid.'}
    Assert-AtlasAProducerMatrix $run.producer
    Assert-TrxKeys $run.producer.packageVersions @('NUnit3TestAdapter','MicrosoftTestPlatformTestHost','MicrosoftTestPlatformObjectModel','MicrosoftNETTestSdk') 'ATLAS-A package versions'
    if($run.producer.packageVersions.NUnit3TestAdapter -cne '6.3.1' -or $run.producer.packageVersions.MicrosoftTestPlatformTestHost -cne '18.12.0' -or $run.producer.packageVersions.MicrosoftTestPlatformObjectModel -cne '18.12.0' -or $run.producer.packageVersions.MicrosoftNETTestSdk -cne '18.12.0'){throw 'ATLAS-A NUnit/TestPlatform package versions are unsupported.'}
    foreach($lock in @($run.producer.packageLocks)){Assert-TrxKeys $lock @('path','bytes','sha256') 'ATLAS-A package lock';$null=ConvertTo-TrxPortableRelative ([string]$lock.path);Assert-TrxJsonInteger $lock.bytes 'ATLAS-A package lock bytes' 1;if([string]$lock.sha256 -notmatch '^[a-f0-9]{64}$'){throw 'ATLAS-A package lock hash is malformed.'}}
    Assert-TrxKeys $run.execution @('startedUtc','finishedUtc','trxStart','trxFinish','elapsedMilliseconds','exitCode','summaryOutcome','targetFramework','configuration','noBuild','noRestore','invocation') 'ATLAS-A execution'
    if($run.execution.targetFramework -cne 'net10.0' -or $run.execution.configuration -cne 'Release' -or -not [bool]$run.execution.noBuild -or -not [bool]$run.execution.noRestore -or [int]$run.execution.exitCode -ne 0 -or $run.execution.summaryOutcome -cne 'Completed'){throw 'ATLAS-A invocation does not match the no-build/no-restore Release contract.'}
    Assert-TrxJsonNonNegativeNumber $run.execution.elapsedMilliseconds 'ATLAS-A elapsed milliseconds'
    Assert-TrxKeys $run.execution.invocation @('executable','workingDirectory','argv','environment','buildHelper') 'ATLAS-A invocation'
    if($run.execution.invocation.executable -cne '<TOOLCHAIN>/dotnet.exe' -or $run.execution.invocation.workingDirectory -cne '<WORKTREE>' -or @($run.execution.invocation.argv).Count -ne 18){throw 'ATLAS-A public command identity is invalid.'}
    $argv=@($run.execution.invocation.argv)
    if($argv[0] -cne 'test' -or $argv[1] -cne 'WorldAtlas\WorldAtlas.sln' -or $argv[2] -cne '--configuration' -or $argv[3] -cne 'Release' -or $argv[4] -cne '--no-build' -or $argv[5] -cne '--no-restore' -or $argv[8] -cne '--logger' -or $argv[10] -cne '--results-directory' -or $argv[12] -cne '--diag' -or $argv[14] -cne '-m:1' -or $argv[15] -cne '-p:MSBuildEnableWorkloadResolver=false' -or $argv[16] -cne '-p:DirectoryBuildPropsPath=<TEMP>/world-atlas-build/Directory.Build.props' -or $argv[17] -cne '-nodeReuse:false'){throw 'ATLAS-A argument vector differs from the captured single-suite invocation.'}
    if($argv[9] -cne 'trx;LogFileName=ATLAS-A-3e4ed67a-recapture.trx' -or $argv[11] -cne '<EVIDENCE>/controlled-recapture-20261125' -or $argv[13] -cne '<EVIDENCE>/controlled-recapture-20261125/vstest-diagnostics.log'){throw 'ATLAS-A argument output paths or TRX logger name are invalid.'}
    Assert-TrxKeys $run.execution.invocation.environment @('NUGET_PACKAGES','DOTNET_ROOT','DOTNET_MULTILEVEL_LOOKUP','processPathLengthBefore','processPathLengthDuring') 'ATLAS-A process environment'
    if($run.execution.invocation.environment.NUGET_PACKAGES -cne '<PACKAGE_CACHE>' -or $run.execution.invocation.environment.DOTNET_ROOT -cne '<TOOLCHAIN>' -or [string]$run.execution.invocation.environment.DOTNET_MULTILEVEL_LOOKUP -cne '0' -or [long]$run.execution.invocation.environment.processPathLengthBefore -ne 26028 -or [long]$run.execution.invocation.environment.processPathLengthDuring -ne 26100){throw 'ATLAS-A process-scoped runtime environment is invalid.'}
    Assert-TrxKeys $run.execution.invocation.buildHelper @('path','bytes','sha256') 'ATLAS-A build helper';if($run.execution.invocation.buildHelper.path -cne '<TEMP>/world-atlas-build/Directory.Build.props' -or [string]$run.execution.invocation.buildHelper.sha256 -notmatch '^[a-f0-9]{64}$'){throw 'ATLAS-A build helper identity is malformed.'};Assert-TrxJsonInteger $run.execution.invocation.buildHelper.bytes 'ATLAS-A build helper bytes' 1
    Assert-TrxKeys $run.discovery @('count','cases') 'ATLAS-A discovery'
    if([int]$run.discovery.count -ne 29 -or @($run.discovery.cases).Count -ne 29 -or @($run.discovery.cases|Where-Object {$_ -isnot [string] -or [string]::IsNullOrWhiteSpace($_)}).Count -gt 0){throw 'ATLAS-A discovered case set is incomplete.'}
    Assert-TrxKeys $run.capturedEvidence @('diagnostics','console') 'ATLAS-A captured evidence'
    foreach($name in @('diagnostics','console')){Assert-TrxKeys $run.capturedEvidence[$name] @('file','bytes','sha256') "ATLAS-A captured evidence.$name";Assert-TrxJsonInteger $run.capturedEvidence[$name].bytes "ATLAS-A $name bytes" 1;if([string]$run.capturedEvidence[$name].sha256 -notmatch '^[a-f0-9]{64}$'){throw 'ATLAS-A captured log hash is malformed.'}}
    if($run.capturedEvidence.diagnostics.file -cne 'vstest-diagnostics.log' -or $run.capturedEvidence.console.file -cne 'test-console.txt'){throw 'ATLAS-A diagnostic evidence names are invalid.'}
    if(@($run.attempts).Count -ne 1){throw 'ATLAS-A run profile requires one test invocation.'}
    $attempt=$run.attempts[0];$attemptKeys=@('label','nativeRunId','nativeRunLabel','originalReport','normalizedReport','summaryOutcome','nativeCounters','caseOutcomeCounts','resultCount','gatePassed','exitCode','elapsedMilliseconds','comparison');Assert-TrxKeys $attempt $attemptKeys 'ATLAS-A attempt'
    if($attempt.label -cne 'primary' -or $attempt.nativeRunId -notmatch '^[0-9a-fA-F-]{32,36}$' -or $attempt.nativeRunLabel -cne "run-$($attempt.nativeRunId)" -or $attempt.summaryOutcome -cne 'Completed' -or [int]$attempt.resultCount -ne 29 -or -not [bool]$attempt.gatePassed -or [int]$attempt.exitCode -ne 0 -or $attempt.comparison -cne 'source-projection-verified'){throw 'ATLAS-A result claim is not a complete passing 29-case run.'}
    Assert-TrxKeys $attempt.originalReport @('bytes','sha256') 'ATLAS-A original report';Assert-TrxJsonInteger $attempt.originalReport.bytes 'ATLAS-A original report bytes' 1
    Assert-TrxKeys $attempt.normalizedReport @('entry','bytes','sha256','semanticSha256') 'ATLAS-A normalized report';Assert-TrxJsonInteger $attempt.normalizedReport.bytes 'ATLAS-A normalized report bytes' 1
    if([string]$attempt.originalReport.sha256 -notmatch '^[a-f0-9]{64}$' -or [string]$attempt.normalizedReport.sha256 -notmatch '^[a-f0-9]{64}$' -or [string]$attempt.normalizedReport.semanticSha256 -notmatch '^[a-f0-9]{64}$' -or [string]$attempt.normalizedReport.entry -cne "results/$($run.runnerRunId)/atlas-a.trx"){throw 'ATLAS-A source/normalized report identity is malformed.'}
    Assert-TrxJsonNonNegativeNumber $attempt.elapsedMilliseconds 'ATLAS-A attempt elapsed milliseconds'
    $counterKeys=@('total','executed','passed','failed','error','timeout','aborted','inconclusive','passedButRunAborted','notRunnable','notExecuted','disconnected','warning','completed','inProgress','pending')
    Assert-TrxKeys $attempt.nativeCounters $counterKeys 'ATLAS-A native counters'
    foreach($key in $counterKeys){if($attempt.nativeCounters[$key] -isnot [string] -or [string]$attempt.nativeCounters[$key] -notmatch '^(0|[1-9][0-9]*)$'){throw "ATLAS-A native counter '$key' is malformed."}}
    if([string]$attempt.nativeCounters.total -cne '29' -or [string]$attempt.nativeCounters.executed -cne '29' -or [string]$attempt.nativeCounters.passed -cne '29'){throw 'ATLAS-A native counters do not reflect 29 passing results.'}
    foreach($key in @('failed','error','timeout','aborted','inconclusive','passedButRunAborted','notRunnable','notExecuted','disconnected','warning','completed','inProgress','pending')){if([string]$attempt.nativeCounters[$key] -cne '0'){throw 'ATLAS-A native non-passing counters must remain zero.'}}
    $outcomeKeys=@('Error','Failed','Timeout','Aborted','Inconclusive','PassedButRunAborted','NotRunnable','NotExecuted','Disconnected','Warning','Passed','Completed','InProgress','Pending');Assert-TrxKeys $attempt.caseOutcomeCounts $outcomeKeys 'ATLAS-A result outcome counts';foreach($key in $outcomeKeys){Assert-TrxJsonInteger $attempt.caseOutcomeCounts[$key] "ATLAS-A outcome $key" 0};if([int]$attempt.caseOutcomeCounts.Passed -ne 29){throw 'ATLAS-A result outcome histogram does not contain 29 Passed cases.'};foreach($key in $outcomeKeys|Where-Object {$_ -cne 'Passed'}){if([int]$attempt.caseOutcomeCounts[$key] -ne 0){throw 'ATLAS-A result outcome histogram contains a non-passing result.'}}
    Assert-TrxKeys $run.retention @('handle','ownerRole','policy','aclVerification','retainedOutputCount','retainedOutputBytes') 'ATLAS-A retention'
    if([string]$run.retention.handle -notmatch '^evidence-[a-f0-9]{32}$' -or $run.retention.ownerRole -cne 'GeneralOrchestrator' -or $run.retention.policy -cne 'DOTNET_TRX_EVIDENCE_CONTRACT.md' -or $run.retention.aclVerification -cne 'verified-current-owner-only' -or [int]$run.retention.retainedOutputCount -ne 249 -or [long]$run.retention.retainedOutputBytes -ne 73712451){throw 'ATLAS-A protected evidence retention record is invalid.'}
}

function Assert-AtlasATrxContextBinding {
    param([pscustomobject]$Private,[System.Collections.IDictionary]$PublicRun,[System.Collections.IDictionary]$PublicManifest,[System.Collections.Generic.Dictionary[string,byte[]]]$ArchivedEntries)
    $facts=Assert-AtlasACapturedRun $Private;$roots=@($Private.SourceRoot,$Private.TestOutputRoot)
    $runId=([guid]::Parse([string]$Private.Runner.run.runId)).ToString('N');$entry="results/$runId/atlas-a.trx"
    $projected=ConvertTo-AtlasAPublicDocument $Private $facts;$normalized=ConvertTo-TrxCanonicalXml $projected
    if(-not $ArchivedEntries.ContainsKey($entry)){throw 'ATLAS-A normalized result is absent from the checked archive.'}
    $actual=$ArchivedEntries[$entry];if($actual.LongLength -ne $normalized.LongLength -or (Get-TrxBytesSha256 $actual) -cne (Get-TrxBytesSha256 $normalized)){throw 'ATLAS-A normalized TRX differs from the privacy-safe projection of the protected original.'}
    $expected=Get-AtlasAPublicRunModel $Private $facts $normalized
    if([string]$expected.attempts[0].normalizedReport.entry -cne $entry){throw 'ATLAS-A normalized report path differs from its captured run identity.'}
    if(-not (Test-TrxClaimValueEqual $PublicRun $expected)){throw 'Public ATLAS-A source, producer, execution, discovery, retention, or result claims differ from protected capture evidence.'}
    Assert-TrxToolManifestBinding $PublicManifest.tool
    $null=Test-ProtectedEvidenceDirectory $Private.Root
    $after=[IO.File]::ReadAllBytes([string]$Private.ReportPath)
    if((Get-TrxBytesSha256 $after) -cne [string]$PublicRun.attempts[0].originalReport.sha256){throw 'Protected original ATLAS-A TRX changed during source comparison.'}
    return $true
}

function New-AtlasATrxEvidencePackage {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$SourceContextPath,[Parameter(Mandatory)][string]$OutputDirectory,[string]$BundleId)
    $private=Read-AtlasASourceContext $SourceContextPath;$facts=Assert-AtlasACapturedRun $private
    $before=[IO.File]::ReadAllBytes([string]$private.ReportPath)
    $projected=ConvertTo-AtlasAPublicDocument $private $facts;$normalized=ConvertTo-TrxCanonicalXml $projected
    $temp=[IO.Path]::GetTempFileName();try{[IO.File]::WriteAllBytes($temp,$normalized);$normalizedFacts=Get-TrxFacts $temp;if((Get-TrxBytesSha256 $normalized) -cne (Get-TrxBytesSha256 (ConvertTo-TrxCanonicalXml $normalizedFacts.Document))){throw 'ATLAS-A normalized TRX is not deterministic canonical XML.'}}finally{Remove-Item -LiteralPath $temp -Force -ErrorAction SilentlyContinue}
    if((Get-TrxBytesSha256 ([IO.File]::ReadAllBytes([string]$private.ReportPath))) -cne (Get-TrxBytesSha256 $before)){throw 'ATLAS-A original TRX changed during normalization.'}
    if(-not $BundleId){$BundleId='atlas-a-29-tests-v1'};if($BundleId -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,79}$'){throw 'ATLAS-A bundle ID contains unsupported characters.'}
    $run=Get-AtlasAPublicRunModel $private $facts $normalized;$archiveName="$BundleId.zip"
    $manifest=[ordered]@{schema='validation-evidence-trx-package-v1';bundleId=$BundleId;format=[ordered]@{reportProfile='trx-vstest-flat-v1';counterProfile='vstest-18.12.0-flat';projectionPolicy=$script:TrxProjection;runProfile='atlas-a-29-tests-v1'};tool=(Get-TrxToolIdentity);archive=[ordered]@{file=$archiveName;bytes=0;sha256=('0'*64);format='zip-deflate-fixed-1980-01-01';entryPolicy='normalized-trx-only'};runs=@($run)}
    $zipPath=Join-Path ([IO.Path]::GetFullPath($OutputDirectory)) $archiveName;$manifestPath=[IO.Path]::ChangeExtension($zipPath,'.manifest.json');Assert-TrxPackageOutputNamesAvailable $zipPath $manifestPath;New-Item -ItemType Directory -Path $OutputDirectory -Force|Out-Null
    try{
        Write-DeterministicEvidenceArchive -Path $zipPath -Entries @([ordered]@{name=$run.attempts[0].normalizedReport.entry;bytes=$normalized})
        $manifest.archive.bytes=[long](Get-Item -LiteralPath $zipPath).Length;$manifest.archive.sha256=(Get-EvidenceSha256 $zipPath).ToLowerInvariant()
        Assert-TrxPublicManifest $manifest;$manifestBytes=ConvertTo-EvidenceJsonBytes $manifest;Test-PublicEvidenceText ([Text.UTF8Encoding]::new($false,$true).GetString($manifestBytes));[IO.File]::WriteAllBytes($manifestPath,$manifestBytes)
        $verified=Test-DotNetTrxEvidencePackage -ManifestPath $manifestPath -ArchivePath $zipPath -SourceContextPath @([IO.Path]::GetFullPath($SourceContextPath));if(-not $verified.IsValid){throw $verified.Error};if($verified.OriginalComparison -cne 'originalComparisonVerified'){throw 'ATLAS-A evidence package did not complete original-to-normalized comparison.'}
        return [pscustomobject]@{ManifestPath=$manifestPath;ArchivePath=$zipPath;ArchiveSha256=$manifest.archive.sha256;ArchiveBytes=$manifest.archive.bytes;OriginalComparison=$verified.OriginalComparison;ResultCount=29;GatePassed=$true}
    }catch{Remove-Item -LiteralPath $zipPath -Force -ErrorAction SilentlyContinue;Remove-Item -LiteralPath $manifestPath -Force -ErrorAction SilentlyContinue;throw}
}
