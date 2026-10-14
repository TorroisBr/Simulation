Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:TrxNamespace = 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'
$script:TrxProfile = 'trx-vstest-flat-v1'
$script:TrxCounterProfile = 'vstest-17.14.1-flat'
$script:TrxProjection = 'trx-private-metadata-v1'
$script:TrxOutcomes = @('Error','Failed','Timeout','Aborted','Inconclusive','PassedButRunAborted','NotRunnable','NotExecuted','Disconnected','Warning','Passed','Completed','InProgress','Pending')
$script:TrxMaxXmlBytes = 268435456L
$script:TrxMaxExpandedBytes = 536870912L
$script:TrxMaxManifestBytes = 67108864L
Import-Module (Join-Path $PSScriptRoot 'UnityValidationEvidence.psm1') -Force

function Assert-TrxKeys {
    param([System.Collections.IDictionary]$Object, [string[]]$Allowed, [string]$Where)
    if ($null -eq $Object) { throw "$Where must be an object." }
    $actual = @($Object.Keys | ForEach-Object { [string]$_ })
    foreach ($key in $actual) { if ($key -cnotin $Allowed) { throw "$Where contains unsupported key '$key'." } }
    foreach ($key in $Allowed) { if ($key -notin $actual) { throw "$Where is missing required key '$key'." } }
}

function Assert-TrxJsonBoolean {
    param([object]$Value,[string]$Where)
    if($null -eq $Value -or $Value -isnot [bool]){throw "$Where must be a JSON boolean."}
}

function Assert-TrxJsonInteger {
    param([object]$Value,[string]$Where,[long]$Minimum=[long]::MinValue)
    $type=if($null -eq $Value){$null}else{$Value.GetType()}
    $integralTypes=@([byte],[sbyte],[int16],[uint16],[int32],[uint32],[int64],[uint64])
    if($null -eq $type -or $type -notin $integralTypes -or [decimal]$Value -lt [decimal]$Minimum){throw "$Where must be a JSON integer at least $Minimum."}
}

function Assert-TrxJsonNonNegativeNumber {
    param([object]$Value,[string]$Where)
    if($null -eq $Value -or $Value -is [bool] -or $Value -isnot [ValueType] -or $Value -is [char]){throw "$Where must be a JSON number."}
    if($Value -is [single] -or $Value -is [double]){if(-not [double]::IsFinite([double]$Value) -or [double]$Value -lt 0){throw "$Where must be a finite non-negative JSON number."}}
    elseif($Value -is [decimal]){if([decimal]$Value -lt 0){throw "$Where must be a non-negative JSON number."}}
    else{Assert-TrxJsonInteger $Value $Where 0}
}

function Get-TrxBytesSha256([byte[]]$Bytes) { Get-BytesSha256 -Bytes $Bytes }

function Invoke-TrxGit {
    param([Parameter(Mandatory)][string]$RepositoryRoot,[Parameter(Mandatory)][string[]]$GitArguments)
    $root=[IO.Path]::GetFullPath($RepositoryRoot)
    if(-not (Test-Path -LiteralPath $root -PathType Container)){throw 'Git source root is missing.'}
    Push-Location -LiteralPath $root
    try{$output=@(& git @GitArguments 2>$null);$exitCode=$LASTEXITCODE}finally{Pop-Location}
    if($exitCode -ne 0){throw "Git command failed in the validated source worktree: git $($GitArguments -join ' ')."}
    return $output
}

function Read-TrxDocument {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)
    $file = Get-Item -LiteralPath $Path -ErrorAction Stop
    if ($file.Length -lt 1 -or $file.Length -gt $script:TrxMaxXmlBytes) { throw 'TRX size is outside the supported limit.' }
    $settings = [System.Xml.XmlReaderSettings]::new()
    $settings.DtdProcessing = [System.Xml.DtdProcessing]::Prohibit
    $settings.XmlResolver = $null
    $settings.MaxCharactersInDocument = $script:TrxMaxXmlBytes
    $settings.MaxCharactersFromEntities = 0
    $document = [System.Xml.XmlDocument]::new()
    $document.PreserveWhitespace = $true
    $document.XmlResolver = $null
    $reader = [System.Xml.XmlReader]::Create($Path, $settings)
    try { $document.Load($reader) } finally { $reader.Dispose() }
    foreach ($node in $document.SelectNodes('//comment() | //processing-instruction()')) { throw "TRX contains unsupported XML node '$($node.NodeType)' at '$($node.Name)'" }
    if ($null -ne $document.DocumentType) { throw 'TRX document types are not supported.' }
    if ($null -eq $document.DocumentElement -or $document.DocumentElement.LocalName -cne 'TestRun' -or $document.DocumentElement.NamespaceURI -cne $script:TrxNamespace) { throw 'TRX root or namespace is unsupported.' }
    return $document
}

function Assert-TrxAttributes {
    param([System.Xml.XmlElement]$Element, [string[]]$Allowed)
    foreach ($attribute in $Element.Attributes) {
        if ($attribute.NamespaceURI -eq 'http://www.w3.org/2000/xmlns/') {
            if ($attribute.Value -cne $script:TrxNamespace -or ($attribute.Prefix -and $attribute.LocalName -cne '')) { throw "Unsupported namespace declaration on $($Element.LocalName)." }
            continue
        }
        if (-not [string]::IsNullOrEmpty($attribute.NamespaceURI) -or $attribute.Name -cnotin $Allowed) { throw "Unsupported attribute '$($attribute.Name)' on $($Element.LocalName)." }
    }
}

function Get-TrxElementChildren {
    param([System.Xml.XmlNode]$Node, [string]$Context)
    $result = [System.Collections.Generic.List[System.Xml.XmlElement]]::new()
    foreach ($child in $Node.ChildNodes) {
        if ($child.NodeType -eq [System.Xml.XmlNodeType]::Element) {
            if ($child.NamespaceURI -cne $script:TrxNamespace) { throw "Unknown namespace at $Context/$($child.LocalName)." }
            $result.Add([System.Xml.XmlElement]$child)
        } elseif ($child.NodeType -in @([System.Xml.XmlNodeType]::Text, [System.Xml.XmlNodeType]::CDATA, [System.Xml.XmlNodeType]::Whitespace, [System.Xml.XmlNodeType]::SignificantWhitespace)) {
            if (-not [string]::IsNullOrWhiteSpace($child.Value) -and $Node.LocalName -notin @('StdOut','StdErr','DebugTrace','Message','StackTrace','Text')) { throw "Unexpected mixed text in $Context." }
        } elseif ($child.NodeType -notin @([System.Xml.XmlNodeType]::XmlDeclaration)) { throw "Unsupported XML content in $Context." }
    }
    return @($result.ToArray())
}

function Assert-TrxShape {
    param([System.Xml.XmlElement]$Element, [string]$Path)
    $spec = @{
        'TestRun' = @{ attrs=@('id','name','runUser'); children=@('Times','TestSettings','Results','TestDefinitions','TestEntries','TestLists','ResultSummary') }
        'Times' = @{ attrs=@('creation','queuing','start','finish'); children=@() }
        'TestSettings' = @{ attrs=@('id','name'); children=@('Deployment') }
        'Deployment' = @{ attrs=@('runDeploymentRoot'); children=@() }
        'Results' = @{ attrs=@(); children=@('UnitTestResult') }
        'UnitTestResult' = @{ attrs=@('executionId','testId','testName','computerName','duration','startTime','endTime','testType','outcome','testListId','relativeResultsDirectory'); children=@('Output') }
        'TestDefinitions' = @{ attrs=@(); children=@('UnitTest') }
        'UnitTest' = @{ attrs=@('id','name','storage'); children=@('Execution','TestMethod') }
        'Execution' = @{ attrs=@('id'); children=@() }
        'TestMethod' = @{ attrs=@('codeBase','adapterTypeName','className','name'); children=@() }
        'TestEntries' = @{ attrs=@(); children=@('TestEntry') }
        'TestEntry' = @{ attrs=@('testId','executionId','testListId'); children=@() }
        'TestLists' = @{ attrs=@(); children=@('TestList') }
        'TestList' = @{ attrs=@('id','name'); children=@() }
        'ResultSummary' = @{ attrs=@('outcome'); children=@('Counters','Output','RunInfos') }
        'Counters' = @{ attrs=@('total','executed','passed','failed','error','timeout','aborted','inconclusive','passedButRunAborted','notRunnable','notExecuted','disconnected','warning','completed','inProgress','pending'); children=@() }
        'RunInfos' = @{ attrs=@(); children=@('RunInfo') }
        'RunInfo' = @{ attrs=@('computerName','outcome','timestamp'); children=@('Text') }
        'Output' = @{ attrs=@(); children=@('StdOut','StdErr','DebugTrace','ErrorInfo') }
        'ErrorInfo' = @{ attrs=@(); children=@('Message','StackTrace') }
        'StdOut' = @{ attrs=@(); children=@() }; 'StdErr' = @{ attrs=@(); children=@() }; 'DebugTrace' = @{ attrs=@(); children=@() }
        'Message' = @{ attrs=@(); children=@() }; 'StackTrace' = @{ attrs=@(); children=@() }; 'Text' = @{ attrs=@(); children=@() }
    }
    if (-not $spec.ContainsKey($Element.LocalName)) { throw "Unsupported TRX element '$Path/$($Element.LocalName)'." }
    $rule = $spec[$Element.LocalName]
    Assert-TrxAttributes -Element $Element -Allowed $rule.attrs
    $children = @(Get-TrxElementChildren $Element $Path)
    foreach ($child in $children) { if ($child.LocalName -cnotin $rule.children) { throw "Unsupported TRX child '$Path/$($child.LocalName)'." } }
    foreach ($group in @('Execution','TestMethod','Counters')) {
        if ($rule.children -contains $group -and @($children | Where-Object LocalName -CEQ $group).Count -ne 1) { throw "$Path requires exactly one $group." }
    }
    foreach ($singleton in @('Times','TestSettings','Deployment','ResultSummary','ErrorInfo','Message','StackTrace','Text','Counters','RunInfos')) {
        if (@($children | Where-Object LocalName -CEQ $singleton).Count -gt 1) { throw "$Path contains duplicate $singleton." }
    }
    foreach ($child in $children) { Assert-TrxShape -Element $child -Path "$Path/$($Element.LocalName)" }
    if ($Element.LocalName -in @('StdOut','StdErr','DebugTrace','Message','StackTrace','Text')) {
        if (@($children).Count -gt 0) { throw "$Path/$($Element.LocalName) may contain text only." }
    }
    if ($Element.LocalName -eq 'Output' -and @($children | Where-Object LocalName -CEQ 'StdOut').Count -gt 1) { throw "$Path/Output has duplicate StdOut." }
    if ($Element.LocalName -eq 'Output' -and @($children | Where-Object LocalName -CEQ 'StdErr').Count -gt 1) { throw "$Path/Output has duplicate StdErr." }
    if ($Element.LocalName -eq 'Output' -and @($children | Where-Object LocalName -CEQ 'DebugTrace').Count -gt 1) { throw "$Path/Output has duplicate DebugTrace." }
    if ($Element.LocalName -eq 'TestSettings' -and @($children).Count -ne 1) { throw 'TestSettings requires exactly one Deployment.' }
    if ($Element.LocalName -eq 'RunInfo' -and @($children | Where-Object LocalName -CEQ 'Text').Count -ne 1) { throw 'RunInfo requires exactly one Text.' }
}

function Get-TrxRequiredAttribute {
    param([System.Xml.XmlElement]$Element,[string]$Name)
    if (-not $Element.HasAttribute($Name) -or [string]::IsNullOrWhiteSpace($Element.GetAttribute($Name))) { throw "$($Element.LocalName)/@$Name is required." }
    return $Element.GetAttribute($Name)
}

function Get-TrxFacts {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)
    $document = Read-TrxDocument $Path
    Assert-TrxShape -Element $document.DocumentElement -Path ''
    $root = $document.DocumentElement
    $sections = @{}
    $debugChildren = @(Get-TrxElementChildren $root 'TestRun')
    Write-Verbose ("Root children: " + (($debugChildren | ForEach-Object { $_.LocalName }) -join ','))
    foreach ($name in @('Times','TestSettings','Results','TestDefinitions','TestEntries','TestLists','ResultSummary')) {
        $nodes = @($debugChildren | Where-Object { $_.LocalName -ceq $name })
        Write-Verbose ("section $name count $($nodes.Count)")
        if ($nodes.Count -ne 1) { throw "TRX requires exactly one $name section." }
        $sections[$name] = $nodes[0]
    }
    $nativeRunId = Get-TrxRequiredAttribute $root 'id'
    $parsedGuid = [Guid]::Empty
    if (-not [Guid]::TryParse($nativeRunId, [ref]$parsedGuid)) { throw 'TRX TestRun id must be a GUID.' }
    $null = Get-TrxRequiredAttribute $root 'name'
    $definitions = [System.Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
    foreach ($definition in @(Get-TrxElementChildren $sections.TestDefinitions 'TestDefinitions')) {
        $id = Get-TrxRequiredAttribute $definition 'id'
        $guid = [Guid]::Empty
        if (-not [Guid]::TryParse($id, [ref]$guid)) { throw 'TRX UnitTest id must be a GUID.' }
        if ($definitions.ContainsKey($id)) { throw "Duplicate TRX definition id '$id'." }
        $name = Get-TrxRequiredAttribute $definition 'name'
        $execution = @(Get-TrxElementChildren $definition 'UnitTest' | Where-Object LocalName -CEQ 'Execution')[0]
        $executionId = Get-TrxRequiredAttribute $execution 'id'
        $executionGuid = [Guid]::Empty
        if (-not [Guid]::TryParse($executionId, [ref]$executionGuid)) { throw 'TRX execution id must be a GUID.' }
        $method = @(Get-TrxElementChildren $definition 'UnitTest' | Where-Object LocalName -CEQ 'TestMethod')[0]
        $null = Get-TrxRequiredAttribute $method 'name'; $null = Get-TrxRequiredAttribute $method 'className'
        $definitions.Add($id,[ordered]@{ name=$name; executionId=$executionId; method=$method })
    }
    $lists = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($list in @(Get-TrxElementChildren $sections.TestLists 'TestLists')) {
        $id = Get-TrxRequiredAttribute $list 'id'; $g = [Guid]::Empty
        if (-not [Guid]::TryParse($id,[ref]$g) -or -not $lists.Add($id)) { throw 'TRX contains invalid or duplicate TestList id.' }
        $null = Get-TrxRequiredAttribute $list 'name'
    }
    if ($definitions.Count -eq 0 -or $lists.Count -eq 0) { throw 'TRX definitions and lists must be nonempty.' }
    $entries = [System.Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
    foreach ($entry in @(Get-TrxElementChildren $sections.TestEntries 'TestEntries')) {
        $testId = Get-TrxRequiredAttribute $entry 'testId'; $executionId = Get-TrxRequiredAttribute $entry 'executionId'; $listId = Get-TrxRequiredAttribute $entry 'testListId'
        if (-not $definitions.ContainsKey($testId) -or $definitions[$testId].executionId -cne $executionId -or -not $lists.Contains($listId)) { throw 'TRX TestEntry has an unresolved test, execution, or list reference.' }
        $key = "$testId|$executionId"
        if ($entries.ContainsKey($key)) { throw 'TRX contains duplicate TestEntry identity.' }
        $entries.Add($key,$entry)
    }
    $allowedOutcomes = $script:TrxOutcomes
    $hist = [ordered]@{}; foreach ($o in $allowedOutcomes) { $hist[$o]=0 }
    $seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $results = @(Get-TrxElementChildren $sections.Results 'Results')
    foreach ($result in $results) {
        $testId=Get-TrxRequiredAttribute $result 'testId'; $executionId=Get-TrxRequiredAttribute $result 'executionId'; $testName=Get-TrxRequiredAttribute $result 'testName'; $outcome=Get-TrxRequiredAttribute $result 'outcome'
        if ($outcome -cnotin $allowedOutcomes) { throw "Unsupported TRX outcome '$outcome'." }
        $key="$testId|$executionId"
        if (-not $entries.ContainsKey($key) -or -not $definitions.ContainsKey($testId) -or -not $seen.Add($key)) { throw 'TRX result has an unresolved or duplicate identity.' }
        if ($entries[$key].GetAttribute('testListId') -cne (Get-TrxRequiredAttribute $result 'testListId')) { throw 'TRX result TestList does not match its entry.' }
        $hist[$outcome]++
    }
    if ($results.Count -eq 0 -or $entries.Count -ne $results.Count -or $definitions.Count -ne $results.Count) { throw 'TRX definitions, entries, and flat results must be complete and one-to-one.' }
    $summary=$sections.ResultSummary
    $summaryOutcome=Get-TrxRequiredAttribute $summary 'outcome'
    if ($summaryOutcome -cnotin $allowedOutcomes) { throw "Unsupported TRX summary outcome '$summaryOutcome'." }
    $counters=@(Get-TrxElementChildren $summary 'ResultSummary' | Where-Object LocalName -CEQ 'Counters')
    if ($counters.Count -ne 1) { throw 'TRX summary requires one native Counters object.' }
    $counterAttrs=@('total','executed','passed','failed','error','timeout','aborted','inconclusive','passedButRunAborted','notRunnable','notExecuted','disconnected','warning','completed','inProgress','pending')
    $native=[ordered]@{}
    foreach($n in $counterAttrs){ $v=Get-TrxRequiredAttribute $counters[0] $n; $parsed=0L; if(-not [long]::TryParse($v,[Globalization.NumberStyles]::None,[Globalization.CultureInfo]::InvariantCulture,[ref]$parsed) -or $parsed -lt 0){throw "Invalid TRX counter '$n'."}; $native[$n]=$v }
    if ([long]$native.total -ne $results.Count -or [long]$native.passed -ne $hist.Passed -or [long]$native.failed -ne $hist.Failed -or [long]$native.executed -ne ([long]$hist.Passed+[long]$hist.Failed)) { throw 'TRX native total/executed/passed/failed counters disagree with result records.' }
    foreach($n in $counterAttrs | Where-Object { $_ -notin @('total','executed','passed','failed') }) { if ([long]$native[$n] -ne 0) { throw "VSTest profile requires native counter '$n' to be zero." } }
    if (@(Get-TrxElementChildren $sections.TestDefinitions 'TestDefinitions').Count -eq 0 -or @(Get-TrxElementChildren $sections.TestEntries 'TestEntries').Count -eq 0) { throw 'TRX test selection is empty.' }
    $nonPassing=0; foreach($outcome in $allowedOutcomes | Where-Object {$_ -ne 'Passed'}) { $nonPassing += [int]$hist[$outcome] }
    return [pscustomobject]@{ Document=$document; RunId=$nativeRunId; RunName=$root.GetAttribute('name'); SummaryOutcome=$summaryOutcome; Counters=$native; OutcomeCounts=$hist; ResultCount=$results.Count; Results=$results; Definitions=$definitions; GatePassed=($summaryOutcome -ceq 'Completed' -and $nonPassing -eq 0) }
}

function Get-TrxReportUtcInterval {
    param([Parameter(Mandatory)][System.Xml.XmlDocument]$Document)
    $times=$Document.SelectSingleNode("//*[local-name()='Times']")
    if($null -eq $times){throw 'Original TRX lacks its run time interval.'}
    $start=[DateTimeOffset]::Parse($times.GetAttribute('start'),[Globalization.CultureInfo]::InvariantCulture,[Globalization.DateTimeStyles]::AssumeUniversal).ToUniversalTime()
    $finish=[DateTimeOffset]::Parse($times.GetAttribute('finish'),[Globalization.CultureInfo]::InvariantCulture,[Globalization.DateTimeStyles]::AssumeUniversal).ToUniversalTime()
    if($finish -lt $start){throw 'Original TRX run interval is invalid.'}
    return [pscustomobject]@{StartUtc=$start;FinishUtc=$finish}
}

function ConvertTo-TrxPortableRelative {
    param([string]$Value)
    if ([string]::IsNullOrWhiteSpace($Value) -or [IO.Path]::IsPathRooted($Value) -or $Value -match '(^|[\\/])\.{1,2}([\\/]|$)' -or $Value -match '^[a-zA-Z][a-zA-Z0-9+.-]*:' -or $Value.Contains('?') -or $Value.Contains('#')) { throw 'TRX path is not a safe relative path.' }
    if (@($Value -split '[\\/]' | Where-Object { [string]::IsNullOrEmpty($_) }).Count -gt 0) { throw 'TRX path contains an empty component.' }
    return $Value
}

function Replace-TrxRootPrefix {
    param([string]$Value,[string[]]$Roots,[string]$Token,[switch]$RequireMatch)
    $matches=[System.Collections.Generic.List[object]]::new()
    foreach($root in $Roots){
        $spellings=@($root,$root.Replace('\','/'),$root.Replace('/','\')) | Select-Object -Unique
        foreach($spelling in $spellings){
            if($Value.StartsWith($spelling,[StringComparison]::OrdinalIgnoreCase) -and ($Value.Length -eq $spelling.Length -or $Value[$spelling.Length] -in @('/','\'))){ $matches.Add([pscustomobject]@{root=$spelling;suffix=$Value.Substring($spelling.Length)}) }
        }
    }
    if($matches.Count -gt 1){ throw 'TRX private path has ambiguous or overlapping worktree root mappings.' }
    if($matches.Count -eq 0){ if($RequireMatch){ throw 'TRX private path has no verified worktree-root mapping.' }; return $Value }
    $suffix=[string]$matches[0].suffix
    if($suffix.StartsWith('/') -or $suffix.StartsWith('\')){$suffix=$suffix.Substring(1)}
    if($suffix -match '(^|[\\/])\.\.([\\/]|$)' -or $suffix -match '^[\\/]'){throw 'TRX private path escapes the mapped root.'}
    return $Token+$suffix
}

function Get-TrxElementPath {
    param([System.Xml.XmlElement]$Element)
    $parts=[System.Collections.Generic.List[string]]::new(); $current=$Element
    while($null -ne $current -and $current.NodeType -eq [System.Xml.XmlNodeType]::Element){ $parts.Insert(0,$current.LocalName); $current=[System.Xml.XmlNode]$current.ParentNode }
    return ($parts -join '/')
}

function ConvertTo-TrxProjectedDocument {
    param([System.Xml.XmlDocument]$Document,[string[]]$WorktreeRoots)
    if($WorktreeRoots.Count -eq 0){$WorktreeRoots=@()}
    for($i=0;$i -lt $WorktreeRoots.Count;$i++){
        if([string]::IsNullOrWhiteSpace($WorktreeRoots[$i]) -or -not [IO.Path]::IsPathRooted($WorktreeRoots[$i])){throw 'Private worktree roots must be absolute paths.'}
        for($j=$i+1;$j -lt $WorktreeRoots.Count;$j++){
            $a=$WorktreeRoots[$i].TrimEnd('\','/');$b=$WorktreeRoots[$j].TrimEnd('\','/')
            if($a.Equals($b,[StringComparison]::OrdinalIgnoreCase) -or $a.StartsWith($b+'\',[StringComparison]::OrdinalIgnoreCase) -or $a.StartsWith($b+'/',[StringComparison]::OrdinalIgnoreCase) -or $b.StartsWith($a+'\',[StringComparison]::OrdinalIgnoreCase) -or $b.StartsWith($a+'/',[StringComparison]::OrdinalIgnoreCase)){throw 'Private worktree roots overlap or are ambiguous.'}
        }
    }
    $clone=[System.Xml.XmlDocument]::new(); $clone.PreserveWhitespace=$true; $clone.XmlResolver=$null; $clone.LoadXml($Document.OuterXml)
    foreach($element in @($Document.SelectNodes('//*'))){foreach($attribute in $element.Attributes){if($attribute.Value -match '<(?:WORKTREE|DEPLOYMENT|PRIVATE_RUN_USER)>|HOST-\d{4}'){throw 'Original TRX already contains a reserved privacy placeholder.'}};if($element.InnerText -match '<(?:WORKTREE|DEPLOYMENT|PRIVATE_RUN_USER)>|HOST-\d{4}'){throw 'Original TRX already contains a reserved privacy placeholder.'}}
    $root=$clone.DocumentElement
    $runId=$root.GetAttribute('id')
    $root.SetAttribute('name',"run-$runId")
    if($root.HasAttribute('runUser') -and $root.GetAttribute('runUser') -ne ''){$root.SetAttribute('runUser','<PRIVATE_RUN_USER>')}
    $hosts=[System.Collections.Generic.Dictionary[string,string]]::new([StringComparer]::OrdinalIgnoreCase); $hostIndex=0
    foreach($element in $root.SelectNodes('.//*')){
        if($element -isnot [System.Xml.XmlElement]){continue}
        if($element.LocalName -eq 'UnitTestResult' -and $element.HasAttribute('computerName')){
            $old=$element.GetAttribute('computerName'); if($old -ne ''){if(-not $hosts.ContainsKey($old)){$hostIndex++;$hosts.Add($old,('HOST-{0:D4}' -f $hostIndex))};$element.SetAttribute('computerName',$hosts[$old])}
        } elseif($element.LocalName -eq 'RunInfo' -and $element.HasAttribute('computerName')){
            $old=$element.GetAttribute('computerName'); if($old -ne ''){if(-not $hosts.ContainsKey($old)){$hostIndex++;$hosts.Add($old,('HOST-{0:D4}' -f $hostIndex))};$element.SetAttribute('computerName',$hosts[$old])}
        } elseif($element.LocalName -eq 'UnitTest'){
            if($element.HasAttribute('storage')){$v=$element.GetAttribute('storage');if($v -ne '' -and [IO.Path]::IsPathRooted($v)){$element.SetAttribute('storage',(Replace-TrxRootPrefix $v $WorktreeRoots '<WORKTREE>/' -RequireMatch))}elseif($v -ne ''){$null=ConvertTo-TrxPortableRelative $v}}
        } elseif($element.LocalName -eq 'TestMethod' -and $element.HasAttribute('codeBase')){
            $v=$element.GetAttribute('codeBase');if($v -ne '' -and [IO.Path]::IsPathRooted($v)){$element.SetAttribute('codeBase',(Replace-TrxRootPrefix $v $WorktreeRoots '<WORKTREE>/' -RequireMatch))}elseif($v -ne ''){$null=ConvertTo-TrxPortableRelative $v}
        } elseif($element.LocalName -eq 'Deployment' -and $element.HasAttribute('runDeploymentRoot')){
            if($element.GetAttribute('runDeploymentRoot') -ne ''){$element.SetAttribute('runDeploymentRoot',"<DEPLOYMENT>/$runId")}
        } elseif($element.LocalName -eq 'StdOut' -and (Get-TrxElementPath $element) -eq 'TestRun/ResultSummary/Output/StdOut'){
            $value=$element.InnerText
            foreach($rootMap in $WorktreeRoots){$forward=[regex]::Escape($rootMap.Replace('\','/'))+'(?:/|$)';$backward=[regex]::Escape($rootMap.Replace('/','\'))+'(?:\\|$)';$value=[regex]::Replace($value,$forward,'<WORKTREE>/',[Text.RegularExpressions.RegexOptions]::IgnoreCase);$value=[regex]::Replace($value,$backward,'<WORKTREE>/',[Text.RegularExpressions.RegexOptions]::IgnoreCase)}
            $element.RemoveAll();$element.AppendChild($clone.CreateTextNode($value))|Out-Null
        }
    }
    return $clone
}

function ConvertTo-TrxEscapedText([string]$Value) { [System.Security.SecurityElement]::Escape($Value) }
function ConvertTo-TrxEscapedAttribute([string]$Value) { (ConvertTo-TrxEscapedText $Value).Replace('"','&quot;').Replace("`r",'&#xD;').Replace("`n",'&#xA;').Replace("`t",'&#x9;') }
function ConvertTo-TrxCanonicalXml {
    param([System.Xml.XmlDocument]$Document)
    $sb=[Text.StringBuilder]::new(); [void]$sb.Append("<?xml version=`"1.0`" encoding=`"utf-8`"?>`n")
    function Add-TrxNode([System.Xml.XmlNode]$Node,[Text.StringBuilder]$Builder,[bool]$Root=$false){
        if($Node.NodeType -eq [System.Xml.XmlNodeType]::Element){
            $name=$Node.LocalName; [void]$Builder.Append('<').Append($name)
            if($Root){[void]$Builder.Append(' xmlns="').Append($script:TrxNamespace).Append('"')}
            $attrs=@($Node.Attributes | Where-Object { $_.NamespaceURI -ne 'http://www.w3.org/2000/xmlns/' }); [Array]::Sort($attrs,[Comparison[object]]{param($a,$b)[StringComparer]::Ordinal.Compare([string]$a.LocalName,[string]$b.LocalName)})
            foreach($a in $attrs){[void]$Builder.Append(' ').Append($a.LocalName).Append('="').Append((ConvertTo-TrxEscapedAttribute $a.Value)).Append('"')}
            $children=@($Node.ChildNodes | Where-Object {
                if ($_.NodeType -eq [System.Xml.XmlNodeType]::Element) { return $true }
                if ($_.NodeType -in @([System.Xml.XmlNodeType]::Text,[System.Xml.XmlNodeType]::CDATA,[System.Xml.XmlNodeType]::SignificantWhitespace)) { return -not [string]::IsNullOrWhiteSpace($_.Value) }
                if ($Node.LocalName -in @('StdOut','StdErr','DebugTrace','Message','StackTrace','Text') -and $_.NodeType -eq [System.Xml.XmlNodeType]::Whitespace) { return $true }
                return $false
            })
            if($children.Count -eq 0){[void]$Builder.Append('/>');return}
            [void]$Builder.Append('>'); foreach($child in $children){Add-TrxNode $child $Builder $false}; [void]$Builder.Append('</').Append($name).Append('>')
        } elseif($Node.NodeType -in @([System.Xml.XmlNodeType]::Text,[System.Xml.XmlNodeType]::CDATA,[System.Xml.XmlNodeType]::SignificantWhitespace)){[void]$Builder.Append((ConvertTo-TrxEscapedText $Node.Value))}
    }
    Add-TrxNode $Document.DocumentElement $sb $true
    return [Text.UTF8Encoding]::new($false).GetBytes($sb.ToString())
}

function Get-TrxSemanticSha256 {
    param([System.Xml.XmlDocument]$Document)
    $bytes=ConvertTo-TrxCanonicalXml $Document; Get-TrxBytesSha256 $bytes
}

function Assert-TrxProvenanceRecord {
    param([System.Collections.IDictionary]$Record)
    $allowed=@('kind','commit','blob','path','bytes','sha256','field','mode')
    if($null -eq $Record){throw 'Provenance record must be an object.'};foreach($key in @($Record.Keys)){if([string]$key -notin $allowed){throw "Provenance record contains unsupported key '$key'."}}
    if([string]$Record.kind -notin @('git-source','original-manifest','original-trx','retained-command-log','retained-output')){throw 'TRX provenance record has an unsupported evidence kind.'}
    if([string]$Record.field -notmatch '^/runs/0(?:/(?:[A-Za-z0-9._~-]|~[01])*)+$'){throw 'Provenance field must be a safe public JSON pointer.'}
    if($Record.Contains('commit') -and [string]$Record.commit -notmatch '^[a-f0-9]{40,64}$'){throw 'TRX provenance commit identity is malformed.'}
    if($Record.Contains('blob') -and [string]$Record.blob -notmatch '^[a-f0-9]{40,64}$'){throw 'TRX provenance Git blob identity is malformed.'}
    if($Record.Contains('path')){$null=ConvertTo-TrxPortableRelative ([string]$Record.path);if([string]$Record.path -match '^[A-Za-z]:' -or [IO.Path]::IsPathRooted([string]$Record.path)){throw 'Provenance path must be a safe public relative filename/path.'}}
    if($Record.Contains('bytes')){Assert-TrxJsonInteger $Record.bytes 'provenance bytes' 0}
    if($Record.Contains('sha256') -and [string]$Record.sha256 -notmatch '^[a-f0-9]{64}$'){throw 'TRX provenance SHA-256 is malformed.'}
    if([string]$Record.mode -notin @('captured','recovered-validated-output')){throw 'TRX provenance mode is unsupported.'}
    switch([string]$Record.kind){
        'git-source' {if(-not $Record.Contains('commit') -or -not $Record.Contains('blob') -or -not $Record.Contains('path') -or -not $Record.Contains('bytes') -or -not $Record.Contains('sha256') -or -not $Record.Contains('field') -or [string]$Record.mode -cne 'captured'){throw 'Git-source provenance is incomplete.'}}
        'original-manifest' {if($Record.Contains('commit') -or $Record.Contains('blob') -or -not $Record.Contains('path') -or -not $Record.Contains('bytes') -or -not $Record.Contains('sha256') -or [string]$Record.mode -cne 'captured'){throw 'Original-manifest provenance is incomplete.'}}
        'original-trx' {if($Record.Contains('commit') -or $Record.Contains('blob') -or -not $Record.Contains('path') -or -not $Record.Contains('bytes') -or -not $Record.Contains('sha256') -or [string]$Record.mode -cne 'captured'){throw 'Original-TRX provenance is incomplete.'}}
        'retained-command-log' {if($Record.Contains('commit') -or $Record.Contains('blob') -or -not $Record.Contains('path') -or -not $Record.Contains('bytes') -or -not $Record.Contains('sha256') -or [string]$Record.mode -cne 'captured'){throw 'Command-log provenance is incomplete.'}}
        'retained-output' {if($Record.Contains('commit') -or $Record.Contains('blob') -or -not $Record.Contains('path') -or -not $Record.Contains('bytes') -or -not $Record.Contains('sha256')){throw 'Retained-output provenance is incomplete.'};if([string]$Record.field -ceq '/runs/0/retention/aclVerification'){if([string]$Record.path -cne 'retention-receipt.json' -or [string]$Record.mode -cne 'captured'){throw 'Retention-receipt provenance is malformed.'}}elseif([string]$Record.mode -cne 'recovered-validated-output'){throw 'Retained-output provenance mode is unsupported.'}}
    }
}

function Assert-TrxPublicManifest {
    param([System.Collections.IDictionary]$Manifest)
    Assert-TrxKeys $Manifest @('schema','bundleId','format','tool','archive','runs') 'manifest'
    if($Manifest.schema -cne 'validation-evidence-trx-package-v1'){throw 'Unsupported TRX evidence manifest schema.'}
    if($Manifest.bundleId -isnot [string] -or [string]$Manifest.bundleId -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,79}$'){throw 'TRX bundle identifier is invalid.'}
    Assert-TrxKeys $Manifest.format @('reportProfile','counterProfile','projectionPolicy') 'manifest.format'
    if($Manifest.format.reportProfile -cne $script:TrxProfile -or $Manifest.format.counterProfile -cne $script:TrxCounterProfile -or $Manifest.format.projectionPolicy -cne $script:TrxProjection){throw 'TRX evidence profile identifiers are unsupported.'}
    Assert-TrxKeys $Manifest.tool @('commit','tree','version','files','runtime') 'manifest.tool'
    if([string]$Manifest.tool.commit -notmatch '^[a-f0-9]{40,64}$' -or [string]$Manifest.tool.tree -notmatch '^[a-f0-9]{40,64}$' -or [string]::IsNullOrWhiteSpace([string]$Manifest.tool.version)){throw 'Tool source identity scalars are malformed.'}
    foreach($file in @($Manifest.tool.files)){Assert-TrxKeys $file @('path','bytes','sha256') 'manifest.tool.files[]';Assert-TrxJsonInteger $file.bytes 'tool file bytes' 1;$null=ConvertTo-TrxPortableRelative ([string]$file.path);if([string]$file.sha256 -notmatch '^[a-f0-9]{64}$'){throw 'Tool source file identity is malformed.'}}
    Assert-TrxKeys $Manifest.tool.runtime @('powershellEdition','powershellVersion','dotnetVersion','compressionAssemblyVersion') 'manifest.tool.runtime'
    Assert-TrxKeys $Manifest.archive @('file','bytes','sha256','format','entryPolicy') 'manifest.archive'
    Assert-TrxJsonInteger $Manifest.archive.bytes 'archive bytes' 1
    $expectedArchiveFile=[string]$Manifest.bundleId+'.zip'
    if([IO.Path]::GetFileName([string]$Manifest.archive.file) -cne [string]$Manifest.archive.file -or [string]$Manifest.archive.file -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,79}\.zip$' -or [string]$Manifest.archive.file -cne $expectedArchiveFile -or [string]$Manifest.archive.sha256 -notmatch '^[a-f0-9]{64}$' -or [string]$Manifest.archive.format -cne 'zip-deflate-fixed-1980-01-01' -or $Manifest.archive.entryPolicy -cne 'normalized-trx-only'){throw 'TRX archive identity, format, or entry policy is invalid.'}
    $runKeys=@('runnerRunId','source','originalManifest','producer','execution','discovery','commands','assemblies','attempts','demonstrations','measurements','retention','existingReviews')
    $attemptKeys=@('label','nativeRunId','nativeRunLabel','originalReport','normalizedReport','summaryOutcome','nativeCounters','caseOutcomeCounts','resultCount','gatePassed','exitCode','elapsedMilliseconds','comparison')
    $basicHashKeys=@('bytes','sha256')
    $i=0
    if(@($Manifest.runs).Count -ne 1){throw 'TRX package requires exactly one source-bound runner run.'}
    foreach($run in @($Manifest.runs)){
        $i++; Assert-TrxKeys $run $runKeys "manifest.runs[$($i-1)]"
        if([string]$run.runnerRunId -notmatch '^[a-f0-9]{32}$'){throw 'TRX runner run ID is invalid.'}
        foreach($attempt in @($run.attempts)){
            Assert-TrxKeys $attempt $attemptKeys 'manifest.runs.attempt'
            Assert-TrxKeys $attempt.originalReport $basicHashKeys 'attempt.originalReport';Assert-TrxJsonInteger $attempt.originalReport.bytes 'original report bytes' 1
            Assert-TrxKeys $attempt.normalizedReport @('entry','bytes','sha256','semanticSha256') 'attempt.normalizedReport';Assert-TrxJsonInteger $attempt.normalizedReport.bytes 'normalized report bytes' 1
        }
        if(@($run.attempts).Count -ne 2 -or @($run.attempts|Select-Object -ExpandProperty label -Unique).Count -ne 2 -or @($run.attempts|Where-Object {$_.label -cnotin @('cold','warm')}).Count -ne 0 -or @($run.attempts|Where-Object {$_.label -ceq 'cold'}).Count -ne 1 -or @($run.attempts|Where-Object {$_.label -ceq 'warm'}).Count -ne 1){throw 'Each TRX run must contain exactly one cold and one warm attempt.'}
        Assert-TrxKeys $run.originalManifest $basicHashKeys 'run.originalManifest';Assert-TrxJsonInteger $run.originalManifest.bytes 'original runner manifest bytes' 1
        Assert-TrxKeys $run.source @('commit','tree','originalInputDigest','inputStable','files','generatedDependencies','influencingChanges') 'run.source'
        Assert-TrxJsonBoolean $run.source.inputStable 'source inputStable'
        if([string]$run.source.commit -notmatch '^[a-f0-9]{40,64}$' -or [string]$run.source.tree -notmatch '^[a-f0-9]{40,64}$' -or [string]$run.source.originalInputDigest -notmatch '^[a-f0-9]{64}$' -or -not $run.source.inputStable){throw 'Source commit/tree/input stability claims are malformed or incomplete.'}
        foreach($file in @($run.source.files)){Assert-TrxKeys $file @('path','bytes','sha256') 'run.source.files[]';Assert-TrxJsonInteger $file.bytes 'source input bytes' 0;$null=ConvertTo-TrxPortableRelative ([string]$file.path);if([string]$file.sha256 -notmatch '^[a-f0-9]{64}$'){throw 'Source input identity is malformed.'}}
        foreach($file in @($run.source.generatedDependencies)){Assert-TrxKeys $file @('path','bytes','sha256') 'run.source.generatedDependencies[]';Assert-TrxJsonInteger $file.bytes 'generated dependency bytes' 0;$null=ConvertTo-TrxPortableRelative ([string]$file.path);if([string]$file.sha256 -notmatch '^[a-f0-9]{64}$'){throw 'Generated dependency identity is malformed.'}}
        foreach($file in @($run.source.influencingChanges)){Assert-TrxKeys $file @('path','gitStatus','bytes','sha256') 'run.source.influencingChanges[]';Assert-TrxJsonInteger $file.bytes 'source influence bytes' 0;$null=ConvertTo-TrxPortableRelative ([string]$file.path);if([string]$file.sha256 -notmatch '^[a-f0-9]{64}$' -or [string]$file.gitStatus -notmatch '^[ MADRCU?!]{2}$'){throw 'Captured source influence identity is malformed.'}}
        Assert-TrxKeys $run.producer @('vstestVersion','sdkVersion','logger','identityMode','provenance') 'run.producer'
        Assert-TrxKeys $run.producer.logger @('file','fileVersion','productVersion','bytes','sha256') 'run.producer.logger';Assert-TrxJsonInteger $run.producer.logger.bytes 'VSTest logger bytes' 1
        Assert-TrxKeys $run.execution @('sdkVersion','clrVersion','targetFramework','configuration','osVersion','osArchitecture','processArchitecture','cultures','packageLocks','provenance') 'run.execution'
        foreach($file in @($run.execution.packageLocks)){Assert-TrxKeys $file @('path','bytes','sha256') 'run.execution.packageLocks[]';Assert-TrxJsonInteger $file.bytes 'package lock bytes' 0;$null=ConvertTo-TrxPortableRelative ([string]$file.path)}
        Assert-TrxKeys $run.discovery @('count','cases') 'run.discovery'
        Assert-TrxJsonInteger $run.discovery.count 'discovery count' 1
        if([int]$run.discovery.count -lt 1 -or @($run.discovery.cases).Count -ne [int]$run.discovery.count -or @($run.discovery.cases|Where-Object {$_ -isnot [string] -or [string]::IsNullOrWhiteSpace($_)}).Count -gt 0){throw 'Public test discovery count/cases are incomplete.'}
        foreach($cmd in @($run.commands)){Assert-TrxKeys $cmd @('label','executable','workingDirectory','argv','exitCode','elapsedMilliseconds','derivation','provenance') 'run.commands[]';Assert-TrxJsonInteger $cmd.exitCode 'command exit code' 0;Assert-TrxJsonNonNegativeNumber $cmd.elapsedMilliseconds 'command elapsedMilliseconds'}
        foreach($asm in @($run.assemblies)){Assert-TrxKeys $asm @('path','fullName','mvid','bytes','sha256','identityMode','observedAtUtc','provenance') 'run.assemblies[]';Assert-TrxJsonInteger $asm.bytes 'assembly bytes' 1;$null=ConvertTo-TrxPortableRelative ([string]$asm.path)}
        foreach($demo in @($run.demonstrations)){Assert-TrxKeys $demo @('label','exitCode','elapsedMilliseconds','output','derivation','provenance') 'run.demonstrations[]';Assert-TrxJsonInteger $demo.exitCode 'demonstration exit code' 0;Assert-TrxJsonNonNegativeNumber $demo.elapsedMilliseconds 'demonstration elapsedMilliseconds'}
        Assert-TrxKeys $run.measurements @('worktreeLibraryAbsent','projectBinObjBytes','resultBytes') 'run.measurements'
        Assert-TrxJsonBoolean $run.measurements.worktreeLibraryAbsent 'measurements.worktreeLibraryAbsent';Assert-TrxJsonInteger $run.measurements.resultBytes 'measurements.resultBytes' 0;foreach($record in @($run.measurements.projectBinObjBytes)){Assert-TrxKeys $record @('path','bytes') 'measurements.projectBinObjBytes[]';$null=ConvertTo-TrxPortableRelative ([string]$record.path);Assert-TrxJsonInteger $record.bytes 'measurements project bin/obj bytes' 0}
        Assert-TrxKeys $run.retention @('handle','ownerRole','policy','plannedPostPromotionDays','aclVerification','provenance') 'run.retention'
        Assert-TrxJsonInteger $run.retention.plannedPostPromotionDays 'retention plannedPostPromotionDays' 0
        Assert-TrxKeys $run.existingReviews @('sourceImplementation','toolImplementation') 'run.existingReviews'
        if(@($run.commands).Count -ne 5){throw 'Public command inventory must contain the five mandatory build/test/demo commands.'}
        $commandLabels=@('cold-build','cold-test','warm-test','cold-demo','warm-demo')
        for($commandIndex=0;$commandIndex -lt $commandLabels.Count;$commandIndex++){$cmd=$run.commands[$commandIndex];$label=$commandLabels[$commandIndex];$expectedCommandArgs=switch($label){'cold-build'{@('build','tests/Simulation.Core.Tests/Simulation.Core.Tests.csproj','--no-restore','--configuration','Release')};'cold-test'{@('test','tests/Simulation.Core.Tests/Simulation.Core.Tests.csproj','--no-restore','--configuration','Release','--logger','trx;LogFileName=CoreTests-cold.trx','--results-directory',"<EVIDENCE>/$($run.runnerRunId)")};'warm-test'{@('test','tests/Simulation.Core.Tests/Simulation.Core.Tests.csproj','--no-build','--no-restore','--configuration','Release','--logger','trx;LogFileName=CoreTests-warm.trx','--results-directory',"<EVIDENCE>/$($run.runnerRunId)")};'cold-demo'{@('run','--project','tools/Simulation.Core.CalendarDemo/Simulation.Core.CalendarDemo.csproj','--no-restore','--configuration','Release')};'warm-demo'{@('run','--project','tools/Simulation.Core.CalendarDemo/Simulation.Core.CalendarDemo.csproj','--no-build','--no-restore','--configuration','Release')}};if([string]$cmd.label -cne $label -or [string]$cmd.executable -cne 'dotnet' -or [string]$cmd.workingDirectory -cne '<WORKTREE>' -or [string]$cmd.derivation -cne 'recovered-from-bound-runner' -or [long]$cmd.exitCode -lt 0 -or [double]$cmd.elapsedMilliseconds -lt 0 -or -not [double]::IsFinite([double]$cmd.elapsedMilliseconds) -or (ConvertTo-Json -InputObject @($cmd.argv) -Compress) -cne (ConvertTo-Json -InputObject $expectedCommandArgs -Compress)){throw "Public command '$label' does not match its fixed recovered contract shape."}}
        if([int]$run.attempts[0].exitCode -ne [int]$run.commands[1].exitCode -or [int]$run.attempts[1].exitCode -ne [int]$run.commands[2].exitCode -or [double]$run.attempts[0].elapsedMilliseconds -ne [double]$run.commands[1].elapsedMilliseconds -or [double]$run.attempts[1].elapsedMilliseconds -ne [double]$run.commands[2].elapsedMilliseconds){throw 'Cold/warm attempt exits/durations do not bind to their corresponding command results.'}
        if(@($run.demonstrations).Count -ne 2 -or [string]$run.demonstrations[0].label -cne 'cold-demo' -or [string]$run.demonstrations[1].label -cne 'warm-demo' -or [int]$run.demonstrations[0].exitCode -ne [int]$run.commands[3].exitCode -or [int]$run.demonstrations[1].exitCode -ne [int]$run.commands[4].exitCode){throw 'Cold/warm demo claims do not bind to their corresponding command results.'}
        if([string]$run.retention.handle -notmatch '^evidence-[a-f0-9]{32}$' -or $run.retention.ownerRole -cne 'GeneralOrchestrator' -or $run.retention.aclVerification -cne 'verified-owner-only-known-copies'){throw 'TRX retention attestation is invalid.'}
        foreach($attempt in @($run.attempts)){
            if($attempt.label -cnotin @('cold','warm') -or [string]$attempt.nativeRunLabel -cne "run-$($attempt.nativeRunId)" -or [string]$attempt.nativeRunId -notmatch '^[0-9a-fA-F-]{32,36}$'){throw 'TRX native attempt identity/label is invalid.'}
            Assert-TrxKeys $attempt.nativeCounters @('total','executed','passed','failed','error','timeout','aborted','inconclusive','passedButRunAborted','notRunnable','notExecuted','disconnected','warning','completed','inProgress','pending') 'attempt.nativeCounters'
            $outcomeKeys=@('Error','Failed','Timeout','Aborted','Inconclusive','PassedButRunAborted','NotRunnable','NotExecuted','Disconnected','Warning','Passed','Completed','InProgress','Pending')
            Assert-TrxKeys $attempt.caseOutcomeCounts $outcomeKeys 'attempt.caseOutcomeCounts'
            Assert-TrxJsonInteger $attempt.resultCount 'attempt resultCount' 1;Assert-TrxJsonInteger $attempt.exitCode 'attempt exitCode' 0;Assert-TrxJsonNonNegativeNumber $attempt.elapsedMilliseconds 'attempt elapsedMilliseconds';Assert-TrxJsonBoolean $attempt.gatePassed 'attempt gatePassed'
            foreach($key in $outcomeKeys){Assert-TrxJsonInteger $attempt.caseOutcomeCounts[$key] "attempt caseOutcomeCounts.$key" 0}
            foreach($key in @('total','executed','passed','failed','error','timeout','aborted','inconclusive','passedButRunAborted','notRunnable','notExecuted','disconnected','warning','completed','inProgress','pending')){if($attempt.nativeCounters[$key] -isnot [string] -or [string]$attempt.nativeCounters[$key] -notmatch '^(0|[1-9][0-9]*)$'){throw "TRX native counter '$key' must preserve its non-negative source text."}}
            if([int]$attempt.resultCount -ne [int]$attempt.nativeCounters.total -or $attempt.comparison -cne 'source-projection-verified'){throw 'TRX result accounting/comparison claim is invalid.'}
            if([string]$attempt.summaryOutcome -cnotin $script:TrxOutcomes){throw 'TRX summary outcome is unsupported.'}
            $outcomeSum=0;foreach($key in $outcomeKeys){$value=[long]$attempt.caseOutcomeCounts[$key];if($value -lt 0){throw 'TRX outcome histogram contains a negative count.'};$outcomeSum+=$value}
            if($outcomeSum -ne [long]$attempt.resultCount){throw 'TRX outcome histogram does not sum to the result count.'}
            $expectedFailed=[long]$attempt.caseOutcomeCounts.Failed;$expectedPassed=[long]$attempt.caseOutcomeCounts.Passed;$expectedExecuted=$expectedFailed+$expectedPassed
            if([long]$attempt.nativeCounters.failed -ne $expectedFailed -or [long]$attempt.nativeCounters.passed -ne $expectedPassed -or [long]$attempt.nativeCounters.executed -ne $expectedExecuted -or [long]$attempt.nativeCounters.error -ne 0){throw 'TRX native counter claims do not match the pinned producer semantics.'}
            foreach($counter in @('timeout','aborted','inconclusive','passedButRunAborted','notRunnable','notExecuted','disconnected','warning','completed','inProgress','pending')){if([long]$attempt.nativeCounters[$counter] -ne 0){throw 'TRX native non-passed counters contradict the pinned producer profile.'}}
            $mustPass=([string]$attempt.summaryOutcome -ceq 'Completed' -and $expectedFailed -eq 0 -and $outcomeSum -eq $expectedPassed -and [long]$attempt.exitCode -eq 0)
            if([bool]$attempt.gatePassed -ne $mustPass){throw 'TRX attempt gate/exit/duration claims are inconsistent.'}
            if([string]$attempt.normalizedReport.entry -cne ('results/'+[string]$run.runnerRunId+'/'+[string]$attempt.label+'.trx')){throw 'TRX report entry path does not match its cold/warm attempt label.'}
        }
        foreach($prov in @($run.producer.provenance)+@($run.execution.provenance)+@($run.retention.provenance)+@($run.commands|ForEach-Object {$_.provenance})+@($run.demonstrations|ForEach-Object {$_.provenance})+@($run.assemblies|ForEach-Object {$_.provenance})){Assert-TrxProvenanceRecord $prov}
    }
}

function Read-DotNetTrxSourceContext {
    param([string]$SourceContextPath)
    $contextPath=[IO.Path]::GetFullPath($SourceContextPath);$contextRoot=Split-Path -Parent $contextPath
    $null=Test-ProtectedEvidenceDirectory $contextRoot;$null=Assert-ProtectedEvidenceFile -Path $contextPath -ProtectedRoot $contextRoot
    $contextBytes=[IO.File]::ReadAllBytes($contextPath);$context=([Text.UTF8Encoding]::new($false,$true).GetString($contextBytes)|ConvertFrom-Json -AsHashtable -DateKind String)
    Assert-TrxKeys $context @('schema','runnerManifestPath','runnerManifestIdentity','worktreeRoots','commandWorkingDirectory','retentionHandle','publicRuns','reports','runtimeSelectionTraces','loggerEvidence') 'source context'
    if($context.schema -cne 'dotnet-trx-source-context-v1' -or [string]$context.retentionHandle -notmatch '^evidence-[a-f0-9]{32}$'){throw 'Unsupported TRX source-context schema or retention handle.'}
    if(@($context.worktreeRoots).Count -lt 1){throw 'TRX source context has no verified worktree root mapping.'}
    Assert-TrxKeys $context.runnerManifestIdentity @('bytes','sha256') 'runner manifest identity'
    if([string]$context.runnerManifestIdentity.sha256 -notmatch '^[a-f0-9]{64}$' -or [long]$context.runnerManifestIdentity.bytes -lt 1){throw 'Private source context has invalid runner-manifest identity.'}
    $runnerPath=[IO.Path]::GetFullPath([string]$context.runnerManifestPath);$null=Assert-ProtectedEvidenceFile -Path $runnerPath -ProtectedRoot $contextRoot
    $runnerBytes=[IO.File]::ReadAllBytes($runnerPath)
    if($runnerBytes.LongLength -ne [long]$context.runnerManifestIdentity.bytes -or (Get-TrxBytesSha256 $runnerBytes) -cne [string]$context.runnerManifestIdentity.sha256){throw 'Original runner manifest bytes differ from the pre-recorded private identity.'}
    $runner=([Text.UTF8Encoding]::new($false,$true).GetString($runnerBytes)|ConvertFrom-Json -AsHashtable -DateKind String)
    Assert-TrxJsonBoolean $runner.inputs.stable 'runner inputs.stable';Assert-TrxJsonBoolean $runner.inputs.generatedDependenciesStable 'runner inputs.generatedDependenciesStable';Assert-TrxJsonBoolean $runner.storage.worktreeLibraryAbsent 'runner storage.worktreeLibraryAbsent'
    if($runner.schema -cne 'simulation-core-validation/v1' -or -not $runner.inputs.stable -or -not $runner.inputs.generatedDependenciesStable){throw 'Original CORE Validation manifest is incomplete or unstable.'}
    Assert-TrxCompleteRunnerManifest $runner
    $sourceRoot=Assert-TrxRunnerInputInventory $runner @($context.worktreeRoots)
    Assert-TrxRunnerArtifactEvidence $context $runner $contextRoot
    Assert-TrxLoggerEvidence $context.loggerEvidence $contextRoot
    return [pscustomobject]@{Path=$contextPath;Root=$contextRoot;Bytes=$contextBytes;Context=$context;RunnerPath=$runnerPath;RunnerBytes=$runnerBytes;Runner=$runner;RunnerSha256=(Get-TrxBytesSha256 $runnerBytes);SourceRoot=$sourceRoot}
}

function Assert-TrxLoggerEvidence {
    param([System.Collections.IDictionary]$Record,[string]$ProtectedRoot)
    Assert-TrxKeys $Record @('path','bytes','sha256') 'private VSTest logger evidence'
    if([long]$Record.bytes -ne 247632 -or [string]$Record.sha256 -cne '4a89511f6a3c00795e4119f4a3e192e58151c6f258e7c9b1f63aede73d76eec1'){throw 'Private VSTest logger evidence does not match the approved 17.14.1 binary identity.'}
    $path=[IO.Path]::GetFullPath([string]$Record.path);$null=Assert-ProtectedEvidenceFile -Path $path -ProtectedRoot $ProtectedRoot
    if([IO.Path]::GetFileName($path) -cne 'Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.dll'){throw 'Private VSTest logger evidence has an unexpected binary name.'}
    $file=Get-Item -LiteralPath $path
    if([long]$file.Length -ne [long]$Record.bytes -or (Get-EvidenceSha256 $path) -cne [string]$Record.sha256){throw 'Private VSTest logger binary does not match its recorded bytes/hash.'}
    $version=[Diagnostics.FileVersionInfo]::GetVersionInfo($path)
    if($version.FileVersion -cne '17.1400.125.42801' -or $version.ProductVersion -cne '17.14.1-release-25428-01'){throw 'Private VSTest logger version resources do not match the approved producer identity.'}
    return $true
}

function Assert-TrxRunnerArtifactEvidence {
    param([System.Collections.IDictionary]$Context,[System.Collections.IDictionary]$Runner,[string]$ProtectedRoot)
    $reportRecords=@($Runner.artifacts.reports);$logRecords=@($Runner.artifacts.logs)
    if($reportRecords.Count -ne 2 -or $logRecords.Count -ne 11){throw 'Original runner manifest lacks the complete report/log artifact inventory.'}
    foreach($artifact in $reportRecords){Assert-TrxKeys $artifact @('file','bytes','sha256') 'original TRX artifact';if([string]$artifact.file -cnotin @('CoreTests-cold.trx','CoreTests-warm.trx')){throw 'Runner manifest contains an unsupported TRX report artifact.'}}
    if(@($reportRecords|Select-Object -ExpandProperty file -Unique).Count -ne 2){throw 'Runner manifest TRX report artifact names are not unique.'}
    $privateReports=@($Context.reports)
    if($privateReports.Count -ne 2){throw 'Private context requires exactly cold and warm original TRX reports.'}
    $coldReportDirectory=$null
    foreach($label in @('cold','warm')){
        $records=@($privateReports|Where-Object label -CEQ $label);$g=if($label -eq 'cold'){$Runner.gates.coldTests}else{$Runner.gates.warmTests}
        if($records.Count -ne 1){throw "Private context requires exactly one $label TRX locator."};$record=$records[0]
        Assert-TrxKeys $record @('label','path','bytes','sha256','nativeRunId') "private $label TRX locator"
        $name=if($label -eq 'cold'){'CoreTests-cold.trx'}else{'CoreTests-warm.trx'}
        if([string]$g.resultFile -cne $name -or [string]$g.runId -cne [string]$record.nativeRunId -or [IO.Path]::GetFileName([string]$record.path) -cne $name){throw "Private $label TRX locator differs from the original runner gate/artifact name."}
        $reportDirectory=[IO.Path]::GetFullPath([IO.Path]::GetDirectoryName([string]$record.path));if([IO.Path]::GetFileName($reportDirectory) -cne [string]$Runner.runId){throw "Private $label TRX is outside the effective <EVIDENCE>/<runnerRunId> result directory."}
        if($label -eq 'cold'){$coldReportDirectory=$reportDirectory}elseif(-not [string]::Equals([string]$coldReportDirectory,$reportDirectory,[StringComparison]::OrdinalIgnoreCase)){throw 'Cold and warm TRX reports do not share the effective runner-specific result directory.'}
        $artifact=@($reportRecords|Where-Object file -CEQ $name);if($artifact.Count -ne 1){throw "Original runner manifest lacks the unique $label report artifact record."};$artifact=$artifact[0]
        if([long]$record.bytes -ne [long]$artifact.bytes -or [string]$record.sha256 -cne [string]$artifact.sha256){throw "Private $label report identity differs from the original runner manifest."}
        $path=[IO.Path]::GetFullPath([string]$record.path);$null=Assert-ProtectedEvidenceFile -Path $path -ProtectedRoot $ProtectedRoot
        $bytes=[IO.File]::ReadAllBytes($path);if($bytes.LongLength -ne [long]$artifact.bytes -or (Get-TrxBytesSha256 $bytes) -cne [string]$artifact.sha256){throw "Retained original $label TRX does not match runner-manifest identity."}
    }
    foreach($record in $logRecords){
        Assert-TrxKeys $record @('file','bytes','sha256') 'original command-log artifact'
        if([IO.Path]::GetFileName([string]$record.file) -cne [string]$record.file -or [string]$record.sha256 -notmatch '^[a-f0-9]{64}$' -or [long]$record.bytes -lt 0){throw 'Runner command-log artifact identity is malformed.'}
        $path=Join-Path $ProtectedRoot ([string]$record.file);$null=Assert-ProtectedEvidenceFile -Path $path -ProtectedRoot $ProtectedRoot
        $bytes=[IO.File]::ReadAllBytes($path);if($bytes.LongLength -ne [long]$record.bytes -or (Get-TrxBytesSha256 $bytes) -cne [string]$record.sha256){throw "Retained runner command log '$($record.file)' differs from its original identity."}
    }
    $expectedLogNames=@('sdk-version.log','dotnet-info.log','dotnet-runtimes.log','restore-tests-locked.log','restore-demo-locked.log','cold-build.log','list-tests.log','cold-test.log','warm-test.log','cold-demo.log','warm-demo.log')
    $actualLogNames=@($logRecords|ForEach-Object {[string]$_.file});[Array]::Sort($expectedLogNames,[StringComparer]::Ordinal);[Array]::Sort($actualLogNames,[StringComparer]::Ordinal)
    if(($expectedLogNames -join "`n") -cne ($actualLogNames -join "`n")){throw 'Runner command log inventory does not match the exact hash-bound script command set.'}
    return $true
}

function Assert-TrxCompleteRunnerManifest {
    param([System.Collections.IDictionary]$Runner)
    if([string]$Runner.status -cnotin @('Passed','Failed')){throw 'CORE runner manifest status is neither a complete passing nor complete nonpassing run.'}
    foreach($name in @('coldBuild','coldTests','warmTests','coldDemo','warmDemo')){
        $gate=$Runner.gates[$name];if($null -eq $gate){throw "CORE runner manifest is incomplete: missing $name gate."}
        if($name -in @('coldTests','warmTests')){Assert-TrxKeys $gate @('exitCode','elapsedMilliseconds','resultFile','runId','runName','summaryOutcome','counters','cases') "$name gate"}
        elseif($name -in @('coldDemo','warmDemo')){Assert-TrxKeys $gate @('exitCode','elapsedMilliseconds','output') "$name gate"}
        else{Assert-TrxKeys $gate @('exitCode','elapsedMilliseconds') "$name gate"}
        Assert-TrxJsonInteger $gate.exitCode "CORE runner $name exitCode" 0;Assert-TrxJsonNonNegativeNumber $gate.elapsedMilliseconds "CORE runner $name elapsedMilliseconds"
    }
    $hasFailedGate=@(@('coldBuild','coldTests','warmTests','coldDemo','warmDemo')|Where-Object {[long]$Runner.gates[$_].exitCode -ne 0}).Count -gt 0
    if(([string]$Runner.status -ceq 'Passed') -eq $hasFailedGate){throw 'CORE runner overall status conflicts with its completed gate exit codes.'}
    Assert-TrxJsonInteger $Runner.testDiscovery.count 'CORE runner discovery count' 1
    if([int]$Runner.testDiscovery.count -lt 1 -or @($Runner.testDiscovery.cases).Count -ne [int]$Runner.testDiscovery.count){throw 'CORE runner manifest has incomplete test discovery.'}
    foreach($label in @('coldTests','warmTests')){if(@($Runner.gates[$label].cases).Count -ne [int]$Runner.testDiscovery.count){throw "CORE runner $label gate has incomplete case identity evidence."}}
}

function Assert-TrxDiscoveryOutput {
    param([string]$Text,[string[]]$Cases)
    if($null -eq $Cases -or $Cases.Count -lt 1){throw 'Captured discovery case list is empty.'}
    $lines=@(($Text -replace "`r`n","`n" -replace "`r","`n") -split "`n")
    while($lines.Count -gt 0 -and $lines[-1] -ceq ''){$lines=@($lines|Select-Object -First ($lines.Count-1))}
    if($lines.Count -ne ($Cases.Count+1) -or $lines[0] -cne 'The following Tests are available:'){throw 'Test-list output does not exactly match the complete captured discovery output.'}
    for($i=0;$i -lt $Cases.Count;$i++){if($lines[$i+1] -cne ('    '+[string]$Cases[$i])){throw 'Test-list output order or case identity differs from captured discovery.'}}
    return $true
}

function Assert-TrxRetainedOutputShape {
    param([string]$Directory,[bool]$RequireComplete)
    if(-not (Test-Path -LiteralPath $Directory -PathType Container)){if($RequireComplete){throw 'Protected retained-output directory is missing.'};return $false}
    $expected=@('Simulation.Core.Tests.dll','Simulation.Core.Tests.pdb','Simulation.Core.dll','Simulation.Core.pdb','assembly-retention.json');$items=@(Get-ChildItem -LiteralPath $Directory -Force)
    if(@($items|Where-Object {$_.PSIsContainer}).Count -gt 0){throw 'Protected retained-output directory contains nested directories.'}
    $actual=@($items|ForEach-Object {$_.Name});[Array]::Sort($expected,[StringComparer]::Ordinal);[Array]::Sort($actual,[StringComparer]::Ordinal)
    if(($expected -join "`n") -cne ($actual -join "`n")){throw 'Protected retained-output directory is partial or contains unexpected files.'}
    return $true
}

function Assert-TrxRunnerInputInventory {
    param([System.Collections.IDictionary]$Runner,[string[]]$WorktreeRoots)
    if($WorktreeRoots.Count -lt 1){throw 'Runner input inventory has no source worktree root.'}
    foreach($root in $WorktreeRoots){
        if([string]::IsNullOrWhiteSpace($root) -or -not [IO.Path]::IsPathRooted($root) -or -not (Test-Path -LiteralPath $root -PathType Container)){throw 'Runner input worktree root is missing or not absolute.'}
        if(((Get-Item -LiteralPath $root -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw 'Runner input worktree root cannot be a reparse point.'}
    }
    $all=@(@($Runner.inputs.files)+@($Runner.inputs.generatedDependencies))
    if($all.Count -eq 0){throw 'Runner input inventory is empty.'}
    $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($record in $all){
        Assert-TrxKeys $record @('path','bytes','sha256') 'runner input inventory record'
        $relative=[string]$record.path
        if($relative.Contains('\') -or [string]::IsNullOrWhiteSpace($relative) -or $relative.StartsWith('/') -or $relative -match '^[A-Za-z]:' -or $relative -match '(^|/)\.\.(/|$)' -or $relative -match '(^|/)\.(?:/|$)' -or $relative.Contains(':')){throw 'Runner input inventory path is not a portable relative path.'}
        $null=ConvertTo-TrxPortableRelative $relative
        if(-not $seen.Add($relative)){throw "Runner input inventory contains duplicate path '$relative'."}
        if([long]$record.bytes -lt 0 -or [string]$record.sha256 -notmatch '^[a-f0-9]{64}$'){throw "Runner input inventory identity is malformed for '$relative'."}
        $resolved=[Collections.Generic.List[object]]::new()
        foreach($root in $WorktreeRoots){
            $full=[IO.Path]::GetFullPath((Join-Path $root ($relative.Replace('/',[IO.Path]::DirectorySeparatorChar))))
            $prefix=[IO.Path]::GetFullPath($root).TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar
            if(-not $full.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Runner input inventory path escaped a worktree root.'}
            if(Test-Path -LiteralPath $full -PathType Leaf){$resolved.Add([pscustomobject]@{Root=$root;Path=$full})}
        }
        if($resolved.Count -ne 1){throw "Runner input '$relative' has missing or ambiguous worktree resolution."}
        $fullPath=$resolved[0].Path;$file=Get-Item -LiteralPath $fullPath -Force
        $cursor=[IO.Path]::GetFullPath($resolved[0].Root)
        foreach($component in $relative.Split('/')){$cursor=Join-Path $cursor $component;if(((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw "Runner input '$relative' traverses a reparse point."}}
        if(($file.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw "Runner input '$relative' cannot be a reparse point."}
        if([long]$file.Length -ne [long]$record.bytes -or (Get-EvidenceSha256 $fullPath) -cne [string]$record.sha256){throw "Runner input '$relative' differs from its captured byte identity."}
    }
    $json=ConvertTo-Json -InputObject @($Runner.inputs.files) -Depth 6 -Compress
    $digest=Get-BytesSha256 -Bytes ([Text.Encoding]::UTF8.GetBytes($json))
    if($digest -cne [string]$Runner.inputs.digest){throw 'Runner input digest does not match its complete captured input inventory.'}
    if(-not $Runner.source.commit -or -not $Runner.source.committedTree){throw 'Runner manifest lacks source commit/tree identities.'}
    $matchingGitRoots=0
    foreach($root in $WorktreeRoots){
        $commit=(@(Invoke-TrxGit $root @('rev-parse','HEAD')) -join "`n").Trim();$tree=(@(Invoke-TrxGit $root @('rev-parse','HEAD^{tree}')) -join "`n").Trim()
        if($commit -ceq [string]$Runner.source.commit -and $tree -ceq [string]$Runner.source.committedTree){$matchingGitRoots++}
    }
    if($matchingGitRoots -ne 1){throw 'Runner source commit/tree does not identify exactly one declared worktree root.'}
    $matchingRoot=$null
    foreach($root in $WorktreeRoots){$commit=(@(Invoke-TrxGit $root @('rev-parse','HEAD')) -join "`n").Trim();$tree=(@(Invoke-TrxGit $root @('rev-parse','HEAD^{tree}')) -join "`n").Trim();if($commit -ceq [string]$Runner.source.commit -and $tree -ceq [string]$Runner.source.committedTree){$matchingRoot=$root}}
    $coreModule=Join-Path $matchingRoot 'Tools/CoreValidation/CoreValidation.psm1';$coreScript=Join-Path $matchingRoot 'Tools/CoreValidation/Invoke-CoreValidation.ps1'
    if(-not (Test-Path -LiteralPath $coreModule -PathType Leaf) -or -not (Test-Path -LiteralPath $coreScript -PathType Leaf)){throw 'Canonical CORE input-census module/runner are missing from the source root.'}
    Push-Location -LiteralPath $matchingRoot
    try{Import-Module $coreModule -Force -Scope Local;$liveInputs=@(Get-CoreInputSnapshot -ProjectRoot (Get-Location).Path);$liveDigest=Get-CoreInputDigest -Snapshot $liveInputs}finally{Pop-Location}
    if((ConvertTo-Json -InputObject @($liveInputs) -Depth 6 -Compress) -cne (ConvertTo-Json -InputObject @($Runner.inputs.files) -Depth 6 -Compress) -or $liveDigest -cne [string]$Runner.inputs.digest -or [string]$Runner.source.workingTreeInputDigest -cne $liveDigest){throw 'The complete current CORE source/input inventory differs from the captured runner identity.'}
    $scriptText=[IO.File]::ReadAllText($coreScript,[Text.UTF8Encoding]::new($false,$true))
    $assetsMatch=[regex]::Match($scriptText,'(?s)\$projectAssets\s*=\s*@\((?<body>.*?)\)')
    if(-not $assetsMatch.Success){throw 'The hash-bound CORE runner lacks its generated-dependency path declaration.'}
    $expectedAssets=@([regex]::Matches($assetsMatch.Groups['body'].Value,"'([^']+)'")|ForEach-Object {$_.Groups[1].Value})
    $actualAssets=@($Runner.inputs.generatedDependencies|ForEach-Object {[string]$_.path})
    [Array]::Sort($expectedAssets,[StringComparer]::Ordinal);[Array]::Sort($actualAssets,[StringComparer]::Ordinal)
    if(($expectedAssets -join "`n") -cne ($actualAssets -join "`n")){throw 'Captured generated-dependency inventory does not cover the hash-bound runner declared set.'}
    return $matchingRoot
}

function Assert-TrxRuntimeSelectionEvidence {
    param([System.Collections.IDictionary]$Context,[System.Collections.IDictionary]$Runner,[System.Collections.IDictionary]$PublicRun,[string]$ProtectedRoot)
    $records=@($Context.runtimeSelectionTraces)
    if($records.Count -ne 2){throw 'Private context requires one runtime-selection trace for each cold/warm gate.'}
    foreach($label in @('cold','warm')){
        $matches=@($records|Where-Object gateLabel -CEQ $label)
        if($matches.Count -ne 1){throw "Private context requires exactly one $label runtime-selection trace."}
        $record=$matches[0]
        Assert-TrxKeys $record @('gateLabel','runnerRunId','path','bytes','sha256','selectedClr') 'private runtime-selection trace'
        if([string]$record.runnerRunId -cne [string]$Runner.runId){throw "Private $label runtime trace is bound to a different runner run."}
        if([string]$record.selectedClr -notmatch '^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?$'){throw "Private $label runtime trace has an invalid selected CLR version."}
        $path=[IO.Path]::GetFullPath([string]$record.path);$null=Assert-ProtectedEvidenceFile -Path $path -ProtectedRoot $ProtectedRoot
        $bytes=[IO.File]::ReadAllBytes($path)
        if($bytes.LongLength -ne [long]$record.bytes -or (Get-TrxBytesSha256 $bytes) -cne [string]$record.sha256){throw "Private $label runtime trace bytes do not match their recorded identity."}
        $text=[Text.UTF8Encoding]::new($false,$true).GetString($bytes)
        # COREHOST_TRACE's selected framework marker is distinct from the
        # installed-runtime inventory and binds the effective shared framework.
        $lines=@($text -split "`r?`n")
        $configHits=[System.Collections.Generic.List[object]]::new()
        for($lineIndex=0;$lineIndex -lt $lines.Count;$lineIndex++){
            if($lines[$lineIndex] -match '(?i)^\s*Runtime config is cfg=(?<cfg>.+\.runtimeconfig\.json)\s+dev=(?<dev>.+\.runtimeconfig\.dev\.json)\s*$'){
                $configHits.Add([pscustomobject]@{Line=$lineIndex;ConfigPath=$Matches.cfg.Trim();DevPath=$Matches.dev.Trim()})
            }
        }
        $testAssemblies=@($PublicRun.assemblies|Where-Object { [IO.Path]::GetFileName([string]$_.path) -ceq 'Simulation.Core.Tests.dll' })
        if($testAssemblies.Count -ne 1){throw 'Private runtime trace cannot bind to a unique public test-assembly record.'}
        $expectedConfig=[IO.Path]::ChangeExtension([string]$testAssemblies[0].path,'runtimeconfig.json').Replace('\','/')
        $expectedDevConfig=[IO.Path]::ChangeExtension([string]$testAssemblies[0].path,'runtimeconfig.dev.json').Replace('\','/')
        $testConfigHits=[Collections.Generic.List[object]]::new()
        foreach($hit in $configHits){
            if(-not [IO.Path]::IsPathRooted([string]$hit.ConfigPath) -or -not [IO.Path]::IsPathRooted([string]$hit.DevPath)){throw "Private $label runtimeconfig trace paths are not absolute."}
            $cfgRelative=Get-TrxMappedRelativePath ([string]$hit.ConfigPath) @($Context.worktreeRoots)
            if($cfgRelative.Replace('\','/') -ceq $expectedConfig){$testConfigHits.Add($hit)}
        }
        if($testConfigHits.Count -ne 1){throw "Private $label runtime trace must contain exactly one runtimeconfig selection block for its bound test assembly."}
        $selectedConfig=$testConfigHits[0]
        $devRelative=Get-TrxMappedRelativePath ([string]$selectedConfig.DevPath) @($Context.worktreeRoots)
        if($devRelative.Replace('\','/') -cne $expectedDevConfig){throw "Private $label runtimeconfig.dev.json does not match the bound test assembly."}
        $nextConfig=$lines.Count
        for($lineIndex=[int]$selectedConfig.Line+1;$lineIndex -lt $lines.Count;$lineIndex++){if($lines[$lineIndex] -match '(?i)^\s*Runtime config is cfg='){$nextConfig=$lineIndex;break}}
        $chosen=@()
        for($lineIndex=[int]$selectedConfig.Line+1;$lineIndex -lt $nextConfig;$lineIndex++){
            if($lines[$lineIndex] -match '(?i)^\s*Chose FX version \[(?<path>[^\]\r\n]+)\]\s*$'){$chosen+=,$Matches.path}
        }
        if($chosen.Count -ne 1 -or $chosen[0] -notmatch '(?i)(?:^|[\\/])shared[\\/]Microsoft\.NETCore\.App[\\/](?<version>\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?)$' -or [string]$Matches.version -cne [string]$record.selectedClr){throw "Private $label runtime trace does not prove its declared selected Microsoft.NETCore.App CLR in the test-assembly config block."}
        $gate=if($label -eq 'cold'){$Runner.gates.coldTests}else{$Runner.gates.warmTests}
        $report=@($Context.reports|Where-Object label -CEQ $label)
        if($report.Count -ne 1 -or [string]$gate.runId -notmatch '^[0-9a-fA-F-]{36}$' -or [string]$gate.runId -cne [string]$report[0].nativeRunId){throw "Private $label runtime trace is not bound to the matching complete runner gate."}
    }
}

function New-TrxBoundSourceProvenance {
    param([string]$SourceRoot,[string]$RelativePath,[string]$Field)
    $relative=ConvertTo-TrxPortableRelative $RelativePath;$full=Resolve-TrxWorktreeFile $relative @($SourceRoot);$file=Get-Item -LiteralPath $full -Force
    $blob=(@(Invoke-TrxGit $SourceRoot @('rev-parse',"HEAD:$relative")) -join "`n").Trim();if($blob -notmatch '^[0-9a-f]{40,64}$'){throw "Source provenance '$relative' is not a committed Git blob."}
    $commit=(@(Invoke-TrxGit $SourceRoot @('rev-parse','HEAD')) -join "`n").Trim()
    return [ordered]@{kind='git-source';commit=$commit;blob=$blob;path=$relative;bytes=[long]$file.Length;sha256=(Get-EvidenceSha256 $full);field=$Field;mode='captured'}
}

function New-TrxBoundLogProvenance {
    param([System.Collections.IDictionary]$Runner,[string]$Label,[string]$Field)
    $name="$Label.log";$matches=@($Runner.artifacts.logs|Where-Object file -CEQ $name);if($matches.Count -ne 1){throw "No unique runner log artifact exists for '$Label'."};$r=$matches[0]
    return [ordered]@{kind='retained-command-log';path=[string]$r.file;bytes=[long]$r.bytes;sha256=[string]$r.sha256;field=$Field;mode='captured'}
}

function New-TrxOriginalManifestProvenance {
    param([System.Collections.IDictionary]$Runner,[byte[]]$ManifestBytes)
    return [ordered]@{kind='original-manifest';path='runner-manifest.json';bytes=[long]$ManifestBytes.LongLength;sha256=(Get-TrxBytesSha256 $ManifestBytes);field='/runs/0/originalManifest';mode='captured'}
}

function New-TrxOriginalReportProvenance {
    param([System.Collections.IDictionary]$Report,[string]$Field)
    $path=[IO.Path]::GetFileName([string]$Report.path);if([string]::IsNullOrWhiteSpace($path) -or $path -match '[\\/:]' -or $path -in @('.','..')){throw 'Original report evidence filename is not a safe portable basename.'}
    return [ordered]@{kind='original-trx';path=$path;bytes=[long]$Report.bytes;sha256=[string]$Report.sha256;field=$Field;mode='captured'}
}

function Get-TrxCapturedWorktreeInfluences {
    param([System.Collections.IDictionary]$Runner,[string]$SourceRoot)
    $captured=@($Runner.source.worktreeChanges);$current=@(Invoke-TrxGit $SourceRoot @('status','--porcelain=v1'))
    if(($captured -join "`n") -cne ($current -join "`n")){throw 'Current source worktree changes differ from the captured runner provenance.'}
    $inputByPath=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
    foreach($inputRecord in @($Runner.inputs.files)+@($Runner.inputs.generatedDependencies)){$inputPath=ConvertTo-TrxPortableRelative ([string]$inputRecord.path);if($inputByPath.ContainsKey($inputPath)){throw "Runner source/generated-input inventory duplicates '$inputPath'."};$inputByPath.Add($inputPath,$inputRecord)}
    $influences=[Collections.Generic.List[object]]::new()
    foreach($change in $captured){
        if([string]$change -notmatch '^(?<status>[ MADRCU?!]{2}) (?<path>.+)$' -or [string]$Matches.path -match '"'){throw 'Captured worktree change path cannot be mapped to a portable public influence record.'}
        $statusCode=[string]$Matches.status;$statusPath=[string]$Matches.path
        if($statusCode -ceq '  '){throw 'Captured worktree change lacks a Git status code.'}
        $relative=ConvertTo-TrxPortableRelative $statusPath;$full=Join-Path $SourceRoot ($relative.Replace('/',[IO.Path]::DirectorySeparatorChar))
        if(-not (Test-Path -LiteralPath $full -PathType Leaf)){throw "Captured worktree influence '$relative' is not a retained file whose byte identity can be verified."}
        $file=Get-Item -LiteralPath $full -Force;if(($file.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw "Captured worktree influence '$relative' cannot be a reparse point."}
        if(-not $inputByPath.ContainsKey($relative)){throw "Captured worktree influence '$relative' is absent from the complete source/generated input inventory."}
        $bound=$inputByPath[$relative];$actualSha=Get-EvidenceSha256 $full
        if([long]$bound.bytes -ne [long]$file.Length -or [string]$bound.sha256 -cne $actualSha){throw "Captured worktree influence '$relative' differs from its bound source/generated input identity."}
        $influences.Add([ordered]@{path=$relative;gitStatus=$statusCode;bytes=[long]$bound.bytes;sha256=[string]$bound.sha256})
    }
    return @($influences.ToArray())
}

function Assert-TrxRestoreProjectAssetPaths {
    param([System.Collections.IDictionary]$Runner,[string]$SourceRoot,[string]$RunnerScriptPath)
    $coreProject='src/Simulation.Core/Simulation.Core.csproj';$testProject='tests/Simulation.Core.Tests/Simulation.Core.Tests.csproj';$demoProject='tools/Simulation.Core.CalendarDemo/Simulation.Core.CalendarDemo.csproj'
    $expected=@{
        'src/Simulation.Core/obj/project.assets.json'=$coreProject
        'tests/Simulation.Core.Tests/obj/project.assets.json'=$testProject
        'tools/Simulation.Core.CalendarDemo/obj/project.assets.json'=$demoProject
    }
    $scriptText=[IO.File]::ReadAllText($RunnerScriptPath,[Text.UTF8Encoding]::new($false,$true));$assetsMatch=[regex]::Match($scriptText,'(?s)\$projectAssets\s*=\s*@\((?<body>.*?)\)')
    if(-not $assetsMatch.Success){throw 'Hash-bound runner lacks its generated project.assets.json declaration.'}
    $declared=@([regex]::Matches($assetsMatch.Groups['body'].Value,"'([^']+)'" )|ForEach-Object {$_.Groups[1].Value.Replace('\\','/')})
    $expectedPaths=@($expected.Keys);[Array]::Sort($declared,[StringComparer]::Ordinal);[Array]::Sort($expectedPaths,[StringComparer]::Ordinal)
    if(($declared -join "`n") -cne ($expectedPaths -join "`n")){throw 'Runner generated dependency declaration does not match the Core, test, and demo project asset set.'}
    $assets=@($Runner.inputs.generatedDependencies)
    if($assets.Count -ne $expected.Count){throw 'Generated dependency inventory does not contain exactly all three restored Core/test/demo project asset files.'}
    foreach($record in $assets){$relative=[string]$record.path;if(-not $expected.ContainsKey($relative)){throw "Unexpected generated project assets file '$relative'."};$full=Resolve-TrxWorktreeFile $relative @($SourceRoot);$document=([IO.File]::ReadAllText($full,[Text.UTF8Encoding]::new($false,$true))|ConvertFrom-Json -AsHashtable -Depth 100);$restore=$document.project.restore;if($null -eq $restore -or [string]::IsNullOrWhiteSpace([string]$restore.projectPath)){throw "Generated project assets '$relative' lacks project.restore.projectPath."};$actualProject=[IO.Path]::GetFullPath([string]$restore.projectPath);$wantedProject=[IO.Path]::GetFullPath((Join-Path $SourceRoot ($expected[$relative].Replace('/',[IO.Path]::DirectorySeparatorChar))));if(-not [string]::Equals($actualProject,$wantedProject,[StringComparison]::OrdinalIgnoreCase)){throw "Generated project assets '$relative' restore.projectPath does not bind to the validated source worktree project."}}
}

function Assert-TrxRecoveredCommandWorkingDirectory {
    param([System.Collections.IDictionary]$Context,[System.Collections.IDictionary]$Runner,[string]$SourceRoot,[string]$ProtectedRoot,[string]$TestAssemblyRelative)
    $record=$Context.commandWorkingDirectory
    Assert-TrxKeys $record @('path','mode','evidence') 'private recovered command working directory'
    if([string]$record.mode -cne 'recovered'){throw 'CORE-A command working directory was not captured; only evidence-backed recovered cwd is supported.'}
    $cwd=[IO.Path]::GetFullPath([string]$record.path);$source=[IO.Path]::GetFullPath($SourceRoot)
    if(-not [IO.Path]::IsPathRooted([string]$record.path) -or -not [string]::Equals($cwd,$source,[StringComparison]::OrdinalIgnoreCase)){throw 'Recovered command working directory does not equal the unique source worktree root.'}
    $requiredEvidence=@('hash-bound-runner-has-no-cwd-change','fixed-relative-project-arguments-resolve-to-captured-project-assets','captured-build-and-test-logs-resolve-to-source-output','original-trx-assembly-target-resolves-to-source-output')
    if((ConvertTo-Json -InputObject @($record.evidence) -Compress) -cne (ConvertTo-Json -InputObject $requiredEvidence -Compress)){throw 'Recovered command working-directory evidence set is incomplete or unsupported.'}
    $cursor=$cwd
    while($cursor -and $cursor -ne [IO.Path]::GetPathRoot($cursor)){$item=Get-Item -LiteralPath $cursor -Force -ErrorAction Stop;if(($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw 'Recovered command working directory path traverses a reparse point.'};$cursor=Split-Path -Parent $cursor}
    $scriptRel='Tools/CoreValidation/Invoke-CoreValidation.ps1';$scriptFull=Resolve-TrxWorktreeFile $scriptRel @($SourceRoot);$script=[IO.File]::ReadAllText($scriptFull,[Text.UTF8Encoding]::new($false,$true));$normalized=([regex]::Replace($script,'\s+',' ')).Replace('\','/')
    if($normalized -match '(?i)(?:\bSet-Location\b|\bPush-Location\b|\bPop-Location\b|\b(?:cd|sl)\s+|\bStart-Process\b|\bWorkingDirectory\s*=)' -or -not $normalized.Contains('& dotnet @Arguments')){throw 'Hash-bound runner does not preserve one inherited process working directory for its fixed dotnet commands.'}
    $projects=[ordered]@{
        'tests/Simulation.Core.Tests/Simulation.Core.Tests.csproj'='tests/Simulation.Core.Tests/obj/project.assets.json'
        'tools/Simulation.Core.CalendarDemo/Simulation.Core.CalendarDemo.csproj'='tools/Simulation.Core.CalendarDemo/obj/project.assets.json'
    }
    foreach($projectRelative in $projects.Keys){
        $assetRecord=@($Runner.inputs.generatedDependencies|Where-Object path -CEQ $projects[$projectRelative]);if($assetRecord.Count -ne 1){throw "Recovered cwd has no unique captured restore asset for '$projectRelative'."}
        $assetPath=Resolve-TrxWorktreeFile ([string]$assetRecord[0].path) @($SourceRoot);$asset=([IO.File]::ReadAllText($assetPath,[Text.UTF8Encoding]::new($false,$true))|ConvertFrom-Json -AsHashtable -Depth 100)
        $projectPath=[IO.Path]::GetFullPath([string]$asset.project.restore.projectPath);$resolvedArgument=[IO.Path]::GetFullPath((Join-Path $cwd ($projectRelative.Replace('/',[IO.Path]::DirectorySeparatorChar))))
        if(-not [string]::Equals($projectPath,$resolvedArgument,[StringComparison]::OrdinalIgnoreCase)){throw "Recovered command cwd plus relative project argument does not equal captured restore.projectPath for '$projectRelative'."}
    }
    $testDll=Resolve-TrxWorktreeFile $TestAssemblyRelative @($SourceRoot);$testDllFull=[IO.Path]::GetFullPath($testDll)
    $coldBuild=Join-Path $ProtectedRoot 'cold-build.log';$coldTests=Join-Path $ProtectedRoot 'cold-test.log';$warmTests=Join-Path $ProtectedRoot 'warm-test.log'
    foreach($log in @($coldBuild,$coldTests,$warmTests)){$null=Assert-ProtectedEvidenceFile -Path $log -ProtectedRoot $ProtectedRoot}
    $coldBuildText=[IO.File]::ReadAllText($coldBuild,[Text.UTF8Encoding]::new($false,$true));$coldTestText=[IO.File]::ReadAllText($coldTests,[Text.UTF8Encoding]::new($false,$true));$warmTestText=[IO.File]::ReadAllText($warmTests,[Text.UTF8Encoding]::new($false,$true))
    $coreDll=[IO.Path]::GetFullPath((Join-Path $SourceRoot 'src/Simulation.Core/bin/Release/netstandard2.1/Simulation.Core.dll'))
    if(-not $coldBuildText.Contains($coreDll) -or -not $coldBuildText.Contains($testDllFull) -or -not $coldTestText.Contains($testDllFull) -or -not $warmTestText.Contains($testDllFull)){throw 'Retained build/test logs do not bind the fixed relative commands to the recovered source-worktree outputs.'}
    if([IO.Path]::GetFullPath((Join-Path $cwd 'tests/Simulation.Core.Tests/Simulation.Core.Tests.csproj')) -cne [IO.Path]::GetFullPath((Join-Path $SourceRoot 'tests/Simulation.Core.Tests/Simulation.Core.Tests.csproj'))){throw 'Recovered test project argument is not rooted at the identified worktree.'}
    return $cwd
}

function Get-TrxRetainedAssemblyInventory {
    param([System.Collections.IDictionary]$Context,[string]$SourceRoot,[string]$ProtectedRoot,[System.Collections.IDictionary]$Runner,[string]$TestAssemblyRelative,[switch]$Create)
    $claims=@($Context.publicRuns[0].assemblies)
    if($claims.Count -ne 2){throw 'Private evidence context must bind exactly the two validated CORE assembly outputs.'}
    $testRelative=ConvertTo-TrxPortableRelative $TestAssemblyRelative
    if([IO.Path]::GetFileName($testRelative) -cne 'Simulation.Core.Tests.dll'){throw 'Validated output binding must originate from the exact test DLL named by TRX.'}
    $derivedPaths=[ordered]@{'Simulation.Core.Tests.dll'=$testRelative;'Simulation.Core.dll'=(Join-Path (Split-Path -Parent $testRelative) 'Simulation.Core.dll').Replace('\','/')}
    $expected=[ordered]@{
        'Simulation.Core.Tests.dll'=[ordered]@{bytes=21504L;sha256='2bb9950f25fb6c8c935b022e4e8c43c9e636f013ca4462e81a7ec084eb616505';mvid='0ca9abd7-fa47-49be-93b8-3f3d0121b396'}
        'Simulation.Core.dll'=[ordered]@{bytes=13824L;sha256='26569417bc8d538bb97746adf575108dfb212ca065e42922bdb99bdf2401f625';mvid='3bb1f3d1-3369-4cd5-bd28-d3994ee2e575'}
    }
    $byLeaf=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
    foreach($claim in $claims){
        Assert-TrxKeys $claim @('path','fullName','mvid','bytes','sha256','identityMode','observedAtUtc','provenance') 'private validated assembly identity'
        $relative=ConvertTo-TrxPortableRelative ([string]$claim.path);$leaf=[IO.Path]::GetFileName($relative)
        if(-not $expected.Contains($leaf) -or $byLeaf.ContainsKey($leaf) -or $relative -cne [string]$derivedPaths[$leaf]){throw 'Private validated assembly inventory has an unexpected, duplicate, or TRX-unbound DLL path.'}
        if([string]$claim.identityMode -cne 'recovered-validated-output'){throw 'Private assembly identity is not marked as recovered validated output.'}
        $observed=[DateTimeOffset]::MinValue
        if(-not [DateTimeOffset]::TryParseExact([string]$claim.observedAtUtc,'o',[Globalization.CultureInfo]::InvariantCulture,[Globalization.DateTimeStyles]::AssumeUniversal,[ref]$observed) -or $observed.Offset -ne [TimeSpan]::Zero){throw 'Private assembly observation time must be a UTC round-trip timestamp.'}
        $byLeaf.Add($leaf,$claim)
    }
    if(-not $byLeaf.ContainsKey('Simulation.Core.Tests.dll') -or -not $byLeaf.ContainsKey('Simulation.Core.dll')){throw 'Private validated assembly inventory is missing one required DLL.'}
    $reportCompletion=[DateTimeOffset]::MinValue;$testStart=[DateTimeOffset]::MaxValue
    foreach($report in @($Context.reports)){$facts=Get-TrxFacts ([IO.Path]::GetFullPath([string]$report.path));$interval=Get-TrxReportUtcInterval $facts.Document;if($interval.FinishUtc -gt $reportCompletion){$reportCompletion=$interval.FinishUtc};if($interval.StartUtc -lt $testStart){$testStart=$interval.StartUtc}}
    foreach($claim in $claims){$observed=[DateTimeOffset]::ParseExact([string]$claim.observedAtUtc,'o',[Globalization.CultureInfo]::InvariantCulture,[Globalization.DateTimeStyles]::AssumeUniversal);if($observed -lt $reportCompletion){throw 'Recovered assembly observation time predates completion of the original validation reports.'}}
    $retentionDir=Join-Path $ProtectedRoot ('validated-output\'+[string]$Runner.runId)
    $receiptPath=Join-Path $retentionDir 'assembly-retention.json'
    $aclReceiptPath=Join-Path $ProtectedRoot 'retention-receipt.json'
    $retentionExists=Test-Path -LiteralPath $retentionDir
    if($Create -and -not $retentionExists){if(Test-Path -LiteralPath $aclReceiptPath){throw 'Sanitized retention receipt exists without its validated-output directory.'};$null=Set-ProtectedEvidenceDirectory -Path $retentionDir}
    elseif($Create -and $retentionExists){$null=Assert-TrxRetainedOutputShape $retentionDir $true;if(-not (Test-Path -LiteralPath $aclReceiptPath -PathType Leaf)){throw 'Existing retained outputs lack the paired owner-only retention receipt.'}}
    $null=Test-ProtectedEvidenceDirectory -Path $retentionDir
    $retained=[Collections.Generic.List[object]]::new()
    foreach($leaf in @('Simulation.Core.Tests.dll','Simulation.Core.dll')){
        $claim=$byLeaf[$leaf];$relative=ConvertTo-TrxPortableRelative ([string]$claim.path);$source=Resolve-TrxWorktreeFile $relative @($Context.worktreeRoots)
        $sourceBefore=[IO.File]::ReadAllBytes($source);$sourceHash=Get-TrxBytesSha256 $sourceBefore;$fixed=$expected[$leaf]
        if((Get-Item -LiteralPath $source -Force).LastWriteTimeUtc -gt $testStart.UtcDateTime){throw "Validated output '$leaf' was written after the original test execution began."}
        if($sourceBefore.LongLength -ne [long]$fixed.bytes -or $sourceHash -cne [string]$fixed.sha256){throw "Validated output '$leaf' differs from its reviewed identity."}
        $identity=Get-TrxAssemblyIdentity $source;$pdb=Assert-TrxMatchingPortablePdb $source
        if([string]$identity.mvid -cne [string]$fixed.mvid -or [string]$identity.fullName -cne [string]$claim.fullName -or [string]$claim.mvid -cne [string]$identity.mvid -or [long]$claim.bytes -ne $sourceBefore.LongLength -or [string]$claim.sha256 -cne $sourceHash){throw "Private evidence claim for '$leaf' differs from its verified output."}
        $destination=Join-Path $retentionDir $leaf;$pdbDestination=[IO.Path]::ChangeExtension($destination,'.pdb')
        if($Create){if(-not (Test-Path -LiteralPath $destination)){[IO.File]::Copy($source,$destination,$false)};if(-not (Test-Path -LiteralPath $pdbDestination)){[IO.File]::Copy($pdb.Path,$pdbDestination,$false)}}
        $sourceAfter=[IO.File]::ReadAllBytes($source);$copyBytes=[IO.File]::ReadAllBytes($destination);$pdbCopyBytes=[IO.File]::ReadAllBytes($pdbDestination)
        if((Get-TrxBytesSha256 $sourceAfter) -cne $sourceHash -or (Get-TrxBytesSha256 $copyBytes) -cne $sourceHash -or $copyBytes.LongLength -ne $sourceBefore.LongLength -or (Get-TrxBytesSha256 ([IO.File]::ReadAllBytes($pdb.Path))) -cne $pdb.Sha256 -or (Get-TrxBytesSha256 $pdbCopyBytes) -cne $pdb.Sha256 -or $pdbCopyBytes.LongLength -ne [long]$pdb.Bytes){throw "Protected copy integrity check failed for '$leaf' or its matching PDB."}
        $copyIdentity=Get-TrxAssemblyIdentity $destination;$null=Assert-TrxMatchingPortablePdb $destination
        if([string]$copyIdentity.mvid -cne [string]$fixed.mvid -or [string]$copyIdentity.fullName -cne [string]$identity.fullName){throw "Protected copy metadata changed for '$leaf'."}
        $null=Assert-ProtectedEvidenceFile -Path $destination -ProtectedRoot $ProtectedRoot;$null=Assert-ProtectedEvidenceFile -Path $pdbDestination -ProtectedRoot $ProtectedRoot
        $retained.Add([ordered]@{path=$relative;file=$leaf;bytes=[long]$copyBytes.LongLength;sha256=(Get-TrxBytesSha256 $copyBytes);pdbFile=[IO.Path]::GetFileName($pdbDestination);pdbBytes=[long]$pdbCopyBytes.LongLength;pdbSha256=(Get-TrxBytesSha256 $pdbCopyBytes);fullName=$copyIdentity.fullName;mvid=$copyIdentity.mvid;observedAtUtc=[string]$claim.observedAtUtc})
    }
    $receipt=[ordered]@{schema='trx-validated-output-retention-v1';runnerRunId=[string]$Runner.runId;sourceCommit=[string]$Runner.source.commit;sourceTree=[string]$Runner.source.committedTree;assemblies=@($retained.ToArray())}
    $expectedReceiptBytes=ConvertTo-EvidenceJsonBytes $receipt
    if($Create){
        if(Test-Path -LiteralPath $receiptPath -PathType Leaf){
            $existingReceiptBytes=[IO.File]::ReadAllBytes((Assert-ProtectedEvidenceFile -Path $receiptPath -ProtectedRoot $ProtectedRoot))
            if(-not [Linq.Enumerable]::SequenceEqual[byte]($existingReceiptBytes,$expectedReceiptBytes)){throw 'Existing protected assembly retention receipt differs from the deterministic verified receipt.'}
        }else{
            $fs=[IO.File]::Open($receiptPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None);try{$fs.Write($expectedReceiptBytes,0,$expectedReceiptBytes.Length);$fs.Flush($true)}finally{$fs.Dispose()}
        }
    }
    $null=Assert-ProtectedEvidenceFile -Path $receiptPath -ProtectedRoot $ProtectedRoot
    $receiptBytes=[IO.File]::ReadAllBytes($receiptPath);if(-not [Linq.Enumerable]::SequenceEqual[byte]($receiptBytes,$expectedReceiptBytes)){throw 'Protected assembly retention receipt does not byte-match the deterministic verified receipt.'};$receiptText=[Text.UTF8Encoding]::new($false,$true).GetString($receiptBytes);$stored=$receiptText|ConvertFrom-Json -AsHashtable -DateKind String
    Assert-TrxKeys $stored @('schema','runnerRunId','sourceCommit','sourceTree','assemblies') 'protected assembly retention receipt'
    if($stored.schema -cne 'trx-validated-output-retention-v1' -or [string]$stored.runnerRunId -cne [string]$Runner.runId -or [string]$stored.sourceCommit -cne [string]$Runner.source.commit -or [string]$stored.sourceTree -cne [string]$Runner.source.committedTree -or (ConvertTo-Json -InputObject @($stored.assemblies) -Depth 10 -Compress) -cne (ConvertTo-Json -InputObject @($retained.ToArray()) -Depth 10 -Compress)){throw 'Protected assembly retention receipt does not match independently rechecked source and retained output identities.'}
    if($Create){
        if(-not (Test-Path -LiteralPath $aclReceiptPath)){$aclReceipt=[ordered]@{schema='trx-retention-assertion-v1';assemblyCount=2;pdbCount=2;ownerOnlyAclVerified=$true;copyHashesVerified=$true;privateLocatorsExcluded=$true;publicBinaryPayloadExcluded=$true};$aclBytes=ConvertTo-EvidenceJsonBytes $aclReceipt;$aclStream=[IO.File]::Open($aclReceiptPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None);try{$aclStream.Write($aclBytes,0,$aclBytes.Length);$aclStream.Flush($true)}finally{$aclStream.Dispose()}}
    }
    $null=Assert-ProtectedEvidenceFile -Path $aclReceiptPath -ProtectedRoot $ProtectedRoot
    $aclBytes=[IO.File]::ReadAllBytes($aclReceiptPath);$aclText=[Text.UTF8Encoding]::new($false,$true).GetString($aclBytes);$aclReceipt=$aclText|ConvertFrom-Json -AsHashtable -DateKind String
    Assert-TrxKeys $aclReceipt @('schema','assemblyCount','pdbCount','ownerOnlyAclVerified','copyHashesVerified','privateLocatorsExcluded','publicBinaryPayloadExcluded') 'sanitized retention receipt'
    foreach($field in @('ownerOnlyAclVerified','copyHashesVerified','privateLocatorsExcluded','publicBinaryPayloadExcluded')){Assert-TrxJsonBoolean $aclReceipt[$field] "retention receipt $field"}
    Assert-TrxJsonInteger $aclReceipt.assemblyCount 'retention receipt assemblyCount' 0;Assert-TrxJsonInteger $aclReceipt.pdbCount 'retention receipt pdbCount' 0
    if($aclReceipt.schema -cne 'trx-retention-assertion-v1' -or [int]$aclReceipt.assemblyCount -ne 2 -or [int]$aclReceipt.pdbCount -ne 2 -or -not $aclReceipt.ownerOnlyAclVerified -or -not $aclReceipt.copyHashesVerified -or -not $aclReceipt.privateLocatorsExcluded -or -not $aclReceipt.publicBinaryPayloadExcluded){throw 'Sanitized retention receipt does not attest the verified known-copy controls.'}
    return @($retained.ToArray())
}

function Assert-TrxPublicRunSourceClaims {
    param([System.Collections.IDictionary]$Run,[System.Collections.IDictionary]$Runner,[System.Collections.IDictionary]$Context,[string]$SourceRoot,[string]$ProtectedRoot,[byte[]]$RunnerManifestBytes,[string]$ToolImplementationCommit)
    $scriptRel='Tools/CoreValidation/Invoke-CoreValidation.ps1';$scriptFull=Resolve-TrxWorktreeFile $scriptRel @($SourceRoot);$script=[IO.File]::ReadAllText($scriptFull,[Text.UTF8Encoding]::new($false,$true))
    $testProject='tests/Simulation.Core.Tests/Simulation.Core.Tests.csproj';$demoProject='tools/Simulation.Core.CalendarDemo/Simulation.Core.CalendarDemo.csproj'
    $expectedArgs=[ordered]@{
        'cold-build'=@('build',$testProject,'--no-restore','--configuration','Release')
        'cold-test'=@('test',$testProject,'--no-restore','--configuration','Release','--logger','trx;LogFileName=CoreTests-cold.trx','--results-directory',"<EVIDENCE>/$($Runner.runId)")
        'warm-test'=@('test',$testProject,'--no-build','--no-restore','--configuration','Release','--logger','trx;LogFileName=CoreTests-warm.trx','--results-directory',"<EVIDENCE>/$($Runner.runId)")
        'cold-demo'=@('run','--project',$demoProject,'--no-restore','--configuration','Release')
        'warm-demo'=@('run','--project',$demoProject,'--no-build','--no-restore','--configuration','Release')
    }
    $normalizedScript=([regex]::Replace($script,'\s+',' ')).Replace('\','/')
    if($normalizedScript -match '(?i)(?:\bSet-Location\b|\bPush-Location\b|\bPop-Location\b|\b(?:cd|sl)\s+)'){throw 'Runner script changes process working directory, so recovered command cwd is unsupported.'}
    $expectedCalls=@(
        "Invoke-Dotnet -Name 'cold-build' -Arguments @('build', `$testProject, '--no-restore', '--configuration', 'Release')",
        "Invoke-Dotnet -Name 'cold-test' -Arguments @('test', `$testProject, '--no-restore', '--configuration', 'Release', '--logger', 'trx;LogFileName=CoreTests-cold.trx', '--results-directory', `$runDirectory)",
        "Invoke-Dotnet -Name 'warm-test' -Arguments @('test', `$testProject, '--no-build', '--no-restore', '--configuration', 'Release', '--logger', 'trx;LogFileName=CoreTests-warm.trx', '--results-directory', `$runDirectory)",
        "Invoke-Dotnet -Name 'cold-demo' -Arguments @('run', '--project', `$demoProject, '--no-restore', '--configuration', 'Release')",
        "Invoke-Dotnet -Name 'warm-demo' -Arguments @('run', '--project', `$demoProject, '--no-build', '--no-restore', '--configuration', 'Release')"
    )
    foreach($call in $expectedCalls){if(-not $normalizedScript.Contains($call)){throw 'Hash-bound runner command argv differs from the contract-reconstructed command set.'}}
    if(-not $normalizedScript.Contains("`$testProject = '$testProject'") -or -not $normalizedScript.Contains("`$demoProject = '$demoProject'") -or -not $normalizedScript.Contains("`$runDirectory = Join-Path `$EvidenceRoot `$runId")){throw 'Hash-bound runner project/results paths differ from the reconstructed command arguments.'}
    if(-not $normalizedScript.Contains("'--configuration', 'Release'")){throw 'Hash-bound runner configuration cannot be reconstructed as Release.'}
    $expectedLogLabels=@('cold-build','cold-test','warm-test','cold-demo','warm-demo');$commands=[Collections.Generic.List[object]]::new()
    for($commandIndex=0;$commandIndex -lt $expectedLogLabels.Count;$commandIndex++){$label=$expectedLogLabels[$commandIndex]
        $gate=if($label -eq 'cold-build'){$Runner.gates.coldBuild}elseif($label -eq 'cold-test'){$Runner.gates.coldTests}elseif($label -eq 'warm-test'){$Runner.gates.warmTests}elseif($label -eq 'cold-demo'){$Runner.gates.coldDemo}else{$Runner.gates.warmDemo}
        if([double]$gate.elapsedMilliseconds -lt 0){throw "Runner gate '$label' has an invalid duration."}
        $argv=@($expectedArgs[$label]);if($label -in @('cold-test','warm-test')){$report=@($Context.reports|Where-Object label -CEQ $(if($label -eq 'cold-test'){'cold'}else{'warm'}));if($report.Count -ne 1){throw "Private report path is missing for '$label'."};$reportParent=[IO.Path]::GetFullPath([IO.Path]::GetDirectoryName([string]$report[0].path));$otherReportParent=[IO.Path]::GetFullPath([IO.Path]::GetDirectoryName([string](@($Context.reports|Where-Object label -CEQ $(if($label -eq 'cold-test'){'warm'}else{'cold'}))[0].path)));if(-not [string]::Equals($reportParent,$otherReportParent,[StringComparison]::OrdinalIgnoreCase)){throw 'Cold/warm report outputs do not share the runner results directory.'};if([IO.Path]::GetFileName($reportParent) -cne [string]$Runner.runId){throw 'Runner results directory is not <EVIDENCE>/<runnerRunId>.'};$argv[-1]="<EVIDENCE>/$($Runner.runId)"}
        $commandField="/runs/0/commands/$commandIndex";$commandProvenance=[Collections.Generic.List[object]]::new();$commandProvenance.Add((New-TrxBoundSourceProvenance $SourceRoot $scriptRel $commandField));$commandProvenance.Add((New-TrxBoundLogProvenance $Runner $label $commandField))
        if($label -in @('cold-test','warm-test')){$reportLabel=if($label -eq 'cold-test'){'cold'}else{'warm'};$reportRecord=@($Context.reports|Where-Object label -CEQ $reportLabel);if($reportRecord.Count -ne 1){throw "Original $reportLabel TRX is missing for its command provenance."};$attemptIndex=if($reportLabel -eq 'cold'){0}else{1};$commandProvenance.Add((New-TrxOriginalReportProvenance $reportRecord[0] "/runs/0/attempts/$attemptIndex/originalReport"))}
        $commands.Add([ordered]@{label=$label;executable='dotnet';workingDirectory='<WORKTREE>';argv=$argv;exitCode=[int]$gate.exitCode;elapsedMilliseconds=[double]$gate.elapsedMilliseconds;derivation='recovered-from-bound-runner';provenance=@($commandProvenance.ToArray())})
    }
    $sdkLog=Join-Path $ProtectedRoot 'sdk-version.log';$null=Assert-ProtectedEvidenceFile -Path $sdkLog -ProtectedRoot $ProtectedRoot
    $sdkText=[IO.File]::ReadAllText($sdkLog,[Text.UTF8Encoding]::new($false,$true)).Trim();if($sdkText -cne [string]$Runner.toolchain.sdk){throw 'SDK output log does not match the runner manifest toolchain.'}
    $runtimeLog=Join-Path $ProtectedRoot 'dotnet-runtimes.log';$runtimeText=[IO.File]::ReadAllText($runtimeLog,[Text.UTF8Encoding]::new($false,$true));$runtimeLines=@($runtimeText -split "`r?`n"|Where-Object {$_ -ne ''})
    if((ConvertTo-Json -InputObject @($runtimeLines) -Compress) -cne (ConvertTo-Json -InputObject @($Runner.toolchain.runtimes) -Compress)){throw 'Runtime-list output log differs from runner toolchain evidence.'}
    $listLog=Join-Path $ProtectedRoot 'list-tests.log';$listText=[IO.File]::ReadAllText($listLog,[Text.UTF8Encoding]::new($false,$true));Assert-TrxDiscoveryOutput $listText @($Runner.testDiscovery.cases)
    Assert-TrxRestoreProjectAssetPaths $Runner $SourceRoot $scriptFull
    $infoLog=[IO.File]::ReadAllText((Join-Path $ProtectedRoot 'dotnet-info.log'),[Text.UTF8Encoding]::new($false,$true));if(-not $infoLog.Contains([string]$Runner.toolchain.sdk)){throw 'dotnet --info output log does not bind the declared SDK version.'}
    $logger=$Context.loggerEvidence
    if([string]$Run.producer.vstestVersion -cne '17.14.1' -or [string]$Run.producer.sdkVersion -cne [string]$Runner.toolchain.sdk -or [string]$Run.producer.logger.file -cne 'Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.dll' -or [long]$Run.producer.logger.bytes -ne [long]$logger.bytes -or [string]$Run.producer.logger.sha256 -cne [string]$logger.sha256 -or [string]$Run.producer.logger.fileVersion -cne '17.1400.125.42801' -or [string]$Run.producer.logger.productVersion -cne '17.14.1-release-25428-01'){throw 'Public producer/logger claims are not derived from the verified runner SDK and protected logger binary.'}
    $Run.producer.sdkVersion=[string]$Runner.toolchain.sdk;$Run.producer.vstestVersion='17.14.1';$Run.producer.identityMode='recovered-validated-output';$Run.producer.logger=[ordered]@{file='Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.dll';fileVersion='17.1400.125.42801';productVersion='17.14.1-release-25428-01';bytes=[long]$logger.bytes;sha256=[string]$logger.sha256};$Run.producer.provenance=@((New-TrxBoundSourceProvenance $SourceRoot $scriptRel '/runs/0/producer'),(New-TrxBoundLogProvenance $Runner 'dotnet-info' '/runs/0/producer'))
    $projectSettings=[Xml.XmlReaderSettings]::new();$projectSettings.DtdProcessing=[Xml.DtdProcessing]::Prohibit;$projectSettings.XmlResolver=$null;$projectPath=Resolve-TrxWorktreeFile $testProject @($SourceRoot);$projectReader=[Xml.XmlReader]::Create($projectPath,$projectSettings);$project=[Xml.XmlDocument]::new();$project.XmlResolver=$null;try{$project.Load($projectReader)}finally{$projectReader.Dispose()};$frameworks=@($project.SelectNodes("//*[local-name()='TargetFramework']")|ForEach-Object {$_.InnerText.Trim()});if($frameworks.Count -ne 1 -or $project.SelectNodes("//*[local-name()='TargetFrameworks']").Count -ne 0){throw 'Test project target framework cannot be reconstructed as one unambiguous value.'}
    $traceClr=@($Context.runtimeSelectionTraces|ForEach-Object {[string]$_.selectedClr}|Select-Object -Unique);if($traceClr.Count -ne 1){throw 'Cold/warm runtime trace does not prove one common selected CLR.'}
    $locks=@($Runner.inputs.files|Where-Object {[string]$_.path -match '(^|/)packages\.lock\.json$'}|ForEach-Object {[ordered]@{path=[string]$_.path;bytes=[long]$_.bytes;sha256=[string]$_.sha256}})
    $Run.execution.sdkVersion=[string]$Runner.toolchain.sdk;$Run.execution.clrVersion=$traceClr[0];$Run.execution.targetFramework=$frameworks[0];$Run.execution.configuration='Release';$Run.execution.osVersion=[string]$Runner.toolchain.os;$Run.execution.osArchitecture=[string]$Runner.toolchain.architecture;$Run.execution.processArchitecture=[string]$Runner.toolchain.processArchitecture;$Run.execution.cultures=@($Runner.toolchain.culturesCoveredBySharedCases);$Run.execution.packageLocks=$locks;$Run.execution.provenance=@((New-TrxBoundSourceProvenance $SourceRoot $scriptRel '/runs/0/execution'),(New-TrxBoundSourceProvenance $SourceRoot $testProject '/runs/0/execution/targetFramework'),(New-TrxBoundLogProvenance $Runner 'dotnet-runtimes' '/runs/0/execution/clrVersion'))
    $Run.discovery=[ordered]@{count=[int]$Runner.testDiscovery.count;cases=@($Runner.testDiscovery.cases)}
    $Run.commands=@($commands.ToArray())
    $Run.demonstrations=@(
        [ordered]@{label='cold-demo';exitCode=[int]$Runner.gates.coldDemo.exitCode;elapsedMilliseconds=[double]$Runner.gates.coldDemo.elapsedMilliseconds;output=@($Runner.gates.coldDemo.output);derivation='recovered-from-bound-runner';provenance=@((New-TrxBoundSourceProvenance $SourceRoot $scriptRel '/runs/0/demonstrations/0'),(New-TrxBoundLogProvenance $Runner 'cold-demo' '/runs/0/demonstrations/0'))},
        [ordered]@{label='warm-demo';exitCode=[int]$Runner.gates.warmDemo.exitCode;elapsedMilliseconds=[double]$Runner.gates.warmDemo.elapsedMilliseconds;output=@($Runner.gates.warmDemo.output);derivation='recovered-from-bound-runner';provenance=@((New-TrxBoundSourceProvenance $SourceRoot $scriptRel '/runs/0/demonstrations/1'),(New-TrxBoundLogProvenance $Runner 'warm-demo' '/runs/0/demonstrations/1'))}
    )
    $Run.measurements=[ordered]@{worktreeLibraryAbsent=[bool]$Runner.storage.worktreeLibraryAbsent;projectBinObjBytes=@($Runner.storage.projectBinObjBytes);resultBytes=[long]$Runner.storage.resultBytes}
    $testAssemblyRelative=$null
    foreach($report in @($Context.reports)){
        $reportPath=[IO.Path]::GetFullPath([string]$report.path);$facts=Get-TrxFacts $reportPath
        foreach($definition in @($facts.Document.SelectNodes("//*[local-name()='UnitTest']"))){$method=$definition.SelectSingleNode("*[local-name()='TestMethod']");if($null -eq $method){throw 'Original TRX UnitTest lacks its TestMethod assembly reference.'};$storage=Get-TrxMappedRelativePath $definition.GetAttribute('storage') @($Context.worktreeRoots);$codeBase=Get-TrxMappedRelativePath $method.GetAttribute('codeBase') @($Context.worktreeRoots);if(-not [string]::Equals($storage,$codeBase,[StringComparison]::OrdinalIgnoreCase)){throw 'Original TRX storage/codeBase references disagree.'};$resolved=Resolve-TrxWorktreeFile $codeBase @($Context.worktreeRoots);if([IO.Path]::GetFileName($resolved) -cne 'Simulation.Core.Tests.dll'){throw 'Original TRX does not bind to the exact Simulation.Core.Tests.dll output.'};if($null -ne $testAssemblyRelative -and -not [string]::Equals($testAssemblyRelative,$codeBase,[StringComparison]::OrdinalIgnoreCase)){throw 'Cold/warm TRX tests bind to different test assemblies.'};$testAssemblyRelative=$codeBase}
    }
    if([string]::IsNullOrWhiteSpace($testAssemblyRelative)){throw 'Original reports do not identify the validated test assembly output.'}
    $null=Assert-TrxRecoveredCommandWorkingDirectory $Context $Runner $SourceRoot $ProtectedRoot $testAssemblyRelative
    $retainedAssemblies=@(Get-TrxRetainedAssemblyInventory $Context $SourceRoot $ProtectedRoot $Runner $testAssemblyRelative -Create)
    $assembled=[Collections.Generic.List[object]]::new()
    for($assemblyIndex=0;$assemblyIndex -lt $retainedAssemblies.Count;$assemblyIndex++){$item=$retainedAssemblies[$assemblyIndex];$assemblyField="/runs/0/assemblies/$assemblyIndex";$assembled.Add([ordered]@{path=[string]$item.path;fullName=[string]$item.fullName;mvid=[string]$item.mvid;bytes=[long]$item.bytes;sha256=[string]$item.sha256;identityMode='recovered-validated-output';observedAtUtc=[string]$item.observedAtUtc;provenance=@([ordered]@{kind='retained-output';path=[string]$item.file;bytes=[long]$item.bytes;sha256=[string]$item.sha256;field=$assemblyField;mode='recovered-validated-output'},(New-TrxBoundSourceProvenance $SourceRoot 'Tools/CoreValidation/Invoke-CoreValidation.ps1' $assemblyField),(New-TrxBoundLogProvenance $Runner 'cold-build' $assemblyField))})}
    $Run.assemblies=@($assembled.ToArray())
    $Run.source.commit=[string]$Runner.source.commit;$Run.source.tree=[string]$Runner.source.committedTree;$Run.source.originalInputDigest=[string]$Runner.inputs.digest;$Run.source.inputStable=[bool]$Runner.inputs.stable;$Run.source.files=@($Runner.inputs.files);$Run.source.generatedDependencies=@($Runner.inputs.generatedDependencies)
    $Run.source.influencingChanges=@(Get-TrxCapturedWorktreeInfluences $Runner $SourceRoot)
    $toolCommit=$ToolImplementationCommit
    if([string]::IsNullOrWhiteSpace($toolCommit)){$toolCommit=[string](Get-TrxToolIdentity).commit}
    if($toolCommit -notmatch '^[a-f0-9]{40,64}$'){throw 'Tool implementation commit is not a full Git identity.'}
    $Run.existingReviews=[ordered]@{sourceImplementation=[string]$Runner.source.commit;toolImplementation=$toolCommit}
    $acl=$null
    if((Test-ProtectedEvidenceDirectory -Path $ProtectedRoot) -ne [IO.Path]::GetFullPath($ProtectedRoot)){throw 'Private retention root does not pass protected ACL verification.'}
    $aclReceiptPath=Join-Path $ProtectedRoot 'retention-receipt.json';$aclReceiptBytes=[IO.File]::ReadAllBytes((Assert-ProtectedEvidenceFile -Path $aclReceiptPath -ProtectedRoot $ProtectedRoot))
    $Run.retention=[ordered]@{handle=[string]$Context.retentionHandle;ownerRole='GeneralOrchestrator';policy='DOTNET_TRX_EVIDENCE_CONTRACT.md';plannedPostPromotionDays=30;aclVerification='verified-owner-only-known-copies';provenance=@((New-TrxOriginalManifestProvenance $Runner $RunnerManifestBytes),[ordered]@{kind='retained-output';path='retention-receipt.json';bytes=[long]$aclReceiptBytes.LongLength;sha256=(Get-TrxBytesSha256 $aclReceiptBytes);field='/runs/0/retention/aclVerification';mode='captured'})}
}

function Test-TrxClaimValueEqual {
    param([AllowNull()][object]$Actual,[AllowNull()][object]$Expected)
    if($null -eq $Expected){return $null -eq $Actual}
    if($null -eq $Actual){return $false}
    if($Expected -is [System.Collections.IDictionary]){
        if($Actual -isnot [System.Collections.IDictionary] -or $Actual.Count -ne $Expected.Count){return $false}
        foreach($key in $Expected.Keys){if(-not $Actual.Contains($key) -or -not (Test-TrxClaimValueEqual $Actual[$key] $Expected[$key])){return $false}}
        return $true
    }
    if($Expected -is [string]){return $Actual -is [string] -and [string]::Equals($Actual,$Expected,[StringComparison]::Ordinal)}
    if($Expected -is [bool]){return $Actual -is [bool] -and $Actual -eq $Expected}
    if($Expected -is [System.Collections.IEnumerable] -and $Expected -isnot [string]){
        if($Actual -isnot [System.Collections.IEnumerable] -or $Actual -is [string]){return $false}
        $actualItems=@($Actual);$expectedItems=@($Expected);if($actualItems.Count -ne $expectedItems.Count){return $false}
        for($i=0;$i -lt $expectedItems.Count;$i++){if(-not (Test-TrxClaimValueEqual $actualItems[$i] $expectedItems[$i])){return $false}}
        return $true
    }
    if($Expected -is [ValueType] -and $Expected -isnot [DateTime]){
        if($Actual -isnot [ValueType] -or $Actual -is [bool] -or $Actual -is [DateTime]){return $false}
        try{return [decimal]$Actual -eq [decimal]$Expected}catch{return $false}
    }
    return $Actual.GetType() -eq $Expected.GetType() -and $Actual.Equals($Expected)
}

function Assert-TrxBoundPublicClaims {
    param([System.Collections.IDictionary]$ActualRun,[System.Collections.IDictionary]$ExpectedRun)
    foreach($field in @('source','producer','execution','discovery','commands','assemblies','demonstrations','measurements','retention','existingReviews')){
        if(-not (Test-TrxClaimValueEqual $ActualRun[$field] $ExpectedRun[$field])){throw "Public '$field' claims do not match values reconstructed from the protected runner, source, tool, and retained evidence."}
    }
}

function Copy-TrxPublicRun {
    param([System.Collections.IDictionary]$Run)
    $json=ConvertTo-Json -InputObject $Run -Depth 100 -Compress
    return ConvertFrom-Json -InputObject $json -AsHashtable -DateKind String
}

function Assert-TrxDiscoveryMatchesReport {
    param([object[]]$DiscoveryCases,[pscustomobject]$Facts,[string]$Label)
    if($DiscoveryCases.Count -ne $Facts.Results.Count){throw "Archived $Label report result count differs from public discovery selection."}
    $remaining=[Collections.Generic.Dictionary[string,int]]::new([StringComparer]::Ordinal)
    foreach($case in $DiscoveryCases){if($case -isnot [string]){throw 'Public discovery selections must remain exact strings.'};if($remaining.ContainsKey($case)){$remaining[$case]++}else{$remaining.Add($case,1)}}
    foreach($result in $Facts.Results){$name=$result.GetAttribute('testName');if(-not $remaining.ContainsKey($name) -or $remaining[$name] -le 0){throw "Archived $Label report contains a case name outside the public discovery multiset."};$remaining[$name]--}
    if(@($remaining.Values|Where-Object {$_ -ne 0}).Count -gt 0){throw "Archived $Label report does not consume the complete public discovery multiset."}
}

function Get-TrxToolIdentity {
    $projectRoot=[IO.Directory]::GetParent([IO.Directory]::GetParent($PSScriptRoot).FullName).FullName
    $paths=@('Tools/UnityValidation/Package-UnityValidationEvidence.ps1','Tools/UnityValidation/UnityValidationEvidence.psm1','Tools/UnityValidation/TrxValidationEvidence.psm1')
    $dirty=@(Invoke-TrxGit $projectRoot (@('status','--porcelain=v1','--')+$paths))
    if($dirty.Count -gt 0){throw 'TRX package tool files must be committed and clean before packaging.'}
    $commit=(@(Invoke-TrxGit $projectRoot @('rev-parse','HEAD')) -join "`n").Trim();$tree=(@(Invoke-TrxGit $projectRoot @('rev-parse','HEAD^{tree}')) -join "`n").Trim()
    $files=[System.Collections.Generic.List[object]]::new()
    foreach($relative in $paths){$full=Join-Path $projectRoot ($relative.Replace('/',[IO.Path]::DirectorySeparatorChar));$item=Get-Item -LiteralPath $full -ErrorAction Stop;$files.Add([ordered]@{path=$relative;bytes=[long]$item.Length;sha256=(Get-EvidenceSha256 $full)})}
    $runtime=Assert-EvidenceRuntime
    return [ordered]@{commit=$commit;tree=$tree;version='1.2.0';files=@($files.ToArray());runtime=[ordered]@{powershellEdition=$runtime.powershellEdition;powershellVersion=$runtime.powershellVersion;dotnetVersion=$runtime.dotnetVersion;compressionAssemblyVersion=$runtime.compressionAssemblyVersion}}
}

function Assert-TrxToolManifestBinding {
    param([System.Collections.IDictionary]$Tool)
    $projectRoot=[IO.Directory]::GetParent([IO.Directory]::GetParent($PSScriptRoot).FullName).FullName
    $actual=Get-TrxToolIdentity
    if([string]$Tool.commit -notmatch '^[a-f0-9]{40,64}$' -or [string]$Tool.tree -notmatch '^[a-f0-9]{40,64}$'){throw 'Public TRX tool identity is not a full Git commit/tree pair.'}
    $commitTree=(@(Invoke-TrxGit $projectRoot @('rev-parse',([string]$Tool.commit+'^{tree}'))) -join "`n").Trim()
    if($commitTree -cne [string]$Tool.tree){throw 'Public TRX tool tree does not match its recorded implementation commit.'}
    $currentHead=(@(Invoke-TrxGit $projectRoot @('rev-parse','HEAD')) -join "`n").Trim()
    $null=Invoke-TrxGit $projectRoot @('merge-base','--is-ancestor',[string]$Tool.commit,$currentHead)
    if([string]$Tool.version -cne [string]$actual.version -or -not (Test-TrxClaimValueEqual $Tool.files $actual.files) -or -not (Test-TrxClaimValueEqual $Tool.runtime $actual.runtime)){throw 'Public TRX tool file/runtime identity differs from the committed tool used to verify this package.'}
    $changed=@(Invoke-TrxGit $projectRoot @('diff','--name-only',[string]$Tool.commit,$currentHead,'--','Tools/UnityValidation/Package-UnityValidationEvidence.ps1','Tools/UnityValidation/UnityValidationEvidence.psm1','Tools/UnityValidation/TrxValidationEvidence.psm1'))
    if($changed.Count -gt 0){throw 'TRX evidence tool source changed after the package-bound implementation commit.'}
}

function Get-TrxAssemblyIdentity {
    param([Parameter(Mandatory)][string]$Path)
    Add-Type -AssemblyName System.Reflection.Metadata
    $stream=[IO.File]::OpenRead($Path);try{$pe=[System.Reflection.PortableExecutable.PEReader]::new($stream);try{if(-not $pe.HasMetadata){throw 'Recovered assembly has no CLI metadata.'};$metadata=[System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe);$definition=$metadata.GetAssemblyDefinition();$module=$metadata.GetModuleDefinition();$name=[System.Reflection.AssemblyName]::GetAssemblyName($Path);return [pscustomobject]@{fullName=$name.FullName;mvid=$metadata.GetGuid($module.Mvid).ToString('D')}}finally{$pe.Dispose()}}finally{$stream.Dispose()}
}

function Assert-TrxMatchingPortablePdb {
    param([Parameter(Mandatory)][string]$AssemblyPath)
    $pdbPath=[IO.Path]::ChangeExtension($AssemblyPath,'.pdb')
    if(-not (Test-Path -LiteralPath $pdbPath -PathType Leaf)){throw "Validated assembly '$(Split-Path -Leaf $AssemblyPath)' has no matching PDB."}
    foreach($path in @($AssemblyPath,$pdbPath)){if(((Get-Item -LiteralPath $path -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw 'Validated assembly/PDB evidence cannot be a reparse point.'}}
    Add-Type -AssemblyName System.Reflection.Metadata
    $assemblyStream=[IO.File]::OpenRead($AssemblyPath);$pdbStream=$null;$provider=$null
    try{
        $pe=[System.Reflection.PortableExecutable.PEReader]::new($assemblyStream)
        try{
            $codeView=@($pe.ReadDebugDirectory()|Where-Object {$_.Type -eq [System.Reflection.PortableExecutable.DebugDirectoryEntryType]::CodeView})
            if($codeView.Count -ne 1){throw 'Validated assembly must contain exactly one CodeView PDB identity.'}
            $codeViewData=$pe.ReadCodeViewDebugDirectoryData($codeView[0])
            $pdbStream=[IO.File]::OpenRead($pdbPath);$provider=[System.Reflection.Metadata.MetadataReaderProvider]::FromPortablePdbStream($pdbStream)
            $id=[byte[]]@($provider.GetMetadataReader().DebugMetadataHeader.Id)
            if($id.Length -ne 20){throw 'Matching portable PDB identity must be exactly 20 bytes.'}
            $pdbGuid=[Guid]::new([byte[]]$id[0..15]);$pdbAge=[BitConverter]::ToUInt32($id,16)
            if($pdbGuid -ne $codeViewData.Guid -or $codeViewData.Age -ne 1){throw 'Retained portable PDB does not match the original assembly CodeView identity.'}
        }finally{$pe.Dispose()}
    }finally{if($null -ne $provider){$provider.Dispose()};if($null -ne $pdbStream){$pdbStream.Dispose()};$assemblyStream.Dispose()}
    return [pscustomobject]@{Path=$pdbPath;Bytes=[long](Get-Item -LiteralPath $pdbPath).Length;Sha256=(Get-EvidenceSha256 $pdbPath)}
}

function Resolve-TrxWorktreeFile {
    param([string]$RelativePath,[string[]]$Roots)
    $relative=ConvertTo-TrxPortableRelative $RelativePath
    $matches=[Collections.Generic.List[string]]::new()
    foreach($root in $Roots){$full=[IO.Path]::GetFullPath((Join-Path $root ($relative.Replace('/',[IO.Path]::DirectorySeparatorChar))));$prefix=[IO.Path]::GetFullPath($root).TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar;if(-not $full.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'TRX binary path escaped a declared worktree root.'};if(Test-Path -LiteralPath $full -PathType Leaf){$matches.Add($full)}}
    if($matches.Count -ne 1){throw "TRX binary '$relative' has missing or ambiguous worktree resolution."}
    if(((Get-Item -LiteralPath $matches[0] -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw "TRX binary '$relative' cannot be a reparse point."}
    return $matches[0]
}

function Get-TrxMappedRelativePath {
    param([string]$Value,[string[]]$Roots)
    if(-not [IO.Path]::IsPathRooted($Value)){return (ConvertTo-TrxPortableRelative $Value)}
    foreach($root in $Roots){$match=Replace-TrxRootPrefix $Value @($root) '<WORKTREE>/' -RequireMatch; if($match.StartsWith('<WORKTREE>/')){return (ConvertTo-TrxPortableRelative $match.Substring('<WORKTREE>/'.Length))}}
    throw 'TRX assembly path is outside every verified worktree root.'
}

function Assert-DotNetTrxContextBinding {
    param([pscustomobject]$Private,[System.Collections.IDictionary]$PublicRun,[System.Collections.IDictionary]$PublicManifest,[System.Collections.Generic.Dictionary[string,byte[]]]$ArchivedEntries)
    $runner=$Private.Runner;$context=$Private.Context
    Assert-TrxRuntimeSelectionEvidence $context $runner $PublicRun $Private.Root
    if([string]$PublicRun.runnerRunId -cne [string]$runner.runId -or [string]$PublicRun.originalManifest.sha256 -cne $Private.RunnerSha256 -or [long]$PublicRun.originalManifest.bytes -ne $Private.RunnerBytes.LongLength){throw 'Public package is not bound to the original private runner manifest.'}
    if([string]$PublicRun.source.commit -cne [string]$runner.source.commit -or [string]$PublicRun.source.tree -cne [string]$runner.source.committedTree -or [string]$PublicRun.source.originalInputDigest -cne [string]$runner.inputs.digest -or -not [bool]$PublicRun.source.inputStable){throw 'Public source identity does not match the original runner manifest.'}
    if((ConvertTo-Json -InputObject @($PublicRun.source.files) -Depth 30 -Compress) -cne (ConvertTo-Json -InputObject @($runner.inputs.files) -Depth 30 -Compress) -or (ConvertTo-Json -InputObject @($PublicRun.source.generatedDependencies) -Depth 30 -Compress) -cne (ConvertTo-Json -InputObject @($runner.inputs.generatedDependencies) -Depth 30 -Compress)){throw 'Public input inventories differ from the original runner manifest.'}
    if([int]$PublicRun.discovery.count -ne [int]$runner.testDiscovery.count -or (ConvertTo-Json -InputObject @($PublicRun.discovery.cases) -Compress) -cne (ConvertTo-Json -InputObject @($runner.testDiscovery.cases) -Compress)){throw 'Public discovery facts differ from the original runner manifest.'}
    if(@($PublicRun.attempts).Count -ne 2 -or @($runner.gates.coldTests,$runner.gates.warmTests).Count -ne 2){throw 'Public package does not preserve both original cold/warm attempts.'}
    $selectedClrs=@($context.runtimeSelectionTraces|ForEach-Object {[string]$_.selectedClr}|Select-Object -Unique)
    if($selectedClrs.Count -ne 1 -or [string]$PublicRun.execution.clrVersion -cne $selectedClrs[0]){throw 'Public selected CLR differs from the privately traced cold/warm executions.'}
    foreach($label in @('cold','warm')){
        $attempt=@($PublicRun.attempts|Where-Object label -CEQ $label);$report=@($context.reports|Where-Object label -CEQ $label);if($attempt.Count -ne 1 -or $report.Count -ne 1){throw "Private/public context requires exactly one $label attempt."};$attempt=$attempt[0];$report=$report[0]
        $path=[IO.Path]::GetFullPath([string]$report.path);$null=Assert-ProtectedEvidenceFile -Path $path -ProtectedRoot $Private.Root;$before=[IO.File]::ReadAllBytes($path)
        if($before.LongLength -ne [long]$attempt.originalReport.bytes -or (Get-TrxBytesSha256 $before) -cne [string]$attempt.originalReport.sha256 -or $before.LongLength -ne [long]$report.bytes -or (Get-TrxBytesSha256 $before) -cne [string]$report.sha256){throw "Private original $label TRX identity changed or does not match package."}
        $facts=Get-TrxFacts $path;if($facts.RunId -cne [string]$attempt.nativeRunId -or $facts.SummaryOutcome -cne [string]$attempt.summaryOutcome -or $facts.ResultCount -ne [int]$attempt.resultCount -or (ConvertTo-Json $facts.Counters -Compress) -cne (ConvertTo-Json $attempt.nativeCounters -Compress) -or (ConvertTo-Json $facts.OutcomeCounts -Compress) -cne (ConvertTo-Json $attempt.caseOutcomeCounts -Compress)){throw "Private original $label TRX result claims differ from package."}
        $gate=if($label -eq 'cold'){$runner.gates.coldTests}else{$runner.gates.warmTests}
        if([string]$gate.runId -cne $facts.RunId -or [string]$gate.summaryOutcome -cne $facts.SummaryOutcome -or [int]$gate.exitCode -ne [int]$attempt.exitCode -or [long]$gate.elapsedMilliseconds -ne [long]$attempt.elapsedMilliseconds){throw "Private runner $label gate facts do not match TRX/public package."}
        Assert-TrxDiscoveryMatchesReport @($runner.testDiscovery.cases) $facts $label
        $gateCases=@($gate.cases);if($gateCases.Count -ne $facts.Results.Count){throw "Private $label TRX case identities differ from runner evidence."}
        $gateMap=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal);foreach($case in $gateCases){$key="$($case.testId)|$($case.executionId)";if($gateMap.ContainsKey($key)){throw 'Original runner evidence contains duplicate case identity.'};$gateMap.Add($key,$case)}
        foreach($result in $facts.Results){$key="$($result.GetAttribute('testId'))|$($result.GetAttribute('executionId'))";if(-not $gateMap.ContainsKey($key)){throw 'TRX test identity is absent from original runner record.'};$case=$gateMap[$key];if([string]$case.testName -cne $result.GetAttribute('testName') -or [string]$case.outcome -cne $result.GetAttribute('outcome')){throw 'TRX case name/outcome differs from original runner record.'}}
        if(@($PublicRun.assemblies).Count -ne 2){throw 'The public assembly inventory must contain exactly Simulation.Core.Tests.dll and Simulation.Core.dll.'}
        $testAssemblyRelative=$null
        foreach($definition in @($facts.Document.SelectNodes("//*[local-name()='UnitTest']"))){
            $testMethod=$definition.SelectSingleNode("*[local-name()='TestMethod']");if($null -eq $testMethod){throw 'TRX UnitTest lacks a test assembly binding.'};$candidate=Get-TrxMappedRelativePath $testMethod.GetAttribute('codeBase') @($context.worktreeRoots);if($null -ne $testAssemblyRelative -and $candidate -cne $testAssemblyRelative){throw 'One original TRX references multiple test assemblies.'};$testAssemblyRelative=$candidate
        }
        if([string]::IsNullOrWhiteSpace($testAssemblyRelative)){throw 'Original TRX has no test assembly binding.'}
        $retainedAssemblies=@(Get-TrxRetainedAssemblyInventory $context $Private.SourceRoot $Private.Root $runner $testAssemblyRelative)
        $retainedByLeaf=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
        foreach($retained in $retainedAssemblies){$retainedByLeaf.Add([string]$retained.file,$retained)}
        $assemblyPaths=[Collections.Generic.Dictionary[string,string]]::new([StringComparer]::OrdinalIgnoreCase)
        foreach($assembly in @($PublicRun.assemblies)){
            Assert-TrxKeys $assembly @('path','fullName','mvid','bytes','sha256','identityMode','observedAtUtc','provenance') 'public assembly identity'
            $leaf=[IO.Path]::GetFileName(([string]$assembly.path).Replace('/','\'))
            if($leaf -notin @('Simulation.Core.Tests.dll','Simulation.Core.dll') -or $assemblyPaths.ContainsKey($leaf)){throw 'Public assembly inventory contains an unsupported or duplicate assembly.'}
            if([string]$assembly.identityMode -cne 'recovered-validated-output'){throw 'Public assembly identity must be recovered from retained validated output.'}
            if(-not $retainedByLeaf.ContainsKey($leaf)){throw 'Public assembly is absent from the protected validated-output inventory.'}
            $retained=$retainedByLeaf[$leaf];$relative=ConvertTo-TrxPortableRelative ([string]$assembly.path)
            if($relative -cne [string]$retained.path -or [long]$assembly.bytes -ne [long]$retained.bytes -or [string]$assembly.sha256 -cne [string]$retained.sha256 -or [string]$assembly.fullName -cne [string]$retained.fullName -or [string]$assembly.mvid -cne [string]$retained.mvid -or [string]$assembly.observedAtUtc -cne [string]$retained.observedAtUtc){throw "Public assembly inventory identity for '$leaf' differs from its protected retained output and private observation."}
            $resolved=Resolve-TrxWorktreeFile $relative @($context.worktreeRoots)
            $assemblyPaths.Add($leaf,$resolved)
        }
        if(-not $assemblyPaths.ContainsKey('Simulation.Core.Tests.dll') -or -not $assemblyPaths.ContainsKey('Simulation.Core.dll')){throw 'Public assembly inventory is missing the test or product assembly.'}
        foreach($definition in @($facts.Document.SelectNodes("//*[local-name()='UnitTest']"))){
            $method=$definition.SelectSingleNode("*[local-name()='TestMethod']");if($null -eq $method){throw 'TRX UnitTest lacks its TestMethod binding.'}
            $storage=Get-TrxMappedRelativePath $definition.GetAttribute('storage') @($context.worktreeRoots);$codeBase=Get-TrxMappedRelativePath $method.GetAttribute('codeBase') @($context.worktreeRoots)
            if(-not [string]::Equals($storage,$codeBase,[StringComparison]::OrdinalIgnoreCase)){throw 'TRX UnitTest storage and TestMethod codeBase do not bind to the same relative assembly.'}
            $assemblyPath=Resolve-TrxWorktreeFile $codeBase @($context.worktreeRoots);$leaf=[IO.Path]::GetFileName($assemblyPath)
            if(-not $assemblyPaths.ContainsKey($leaf) -or -not [string]::Equals($assemblyPath,$assemblyPaths[$leaf],[StringComparison]::OrdinalIgnoreCase)){throw "TRX UnitTest target '$codeBase' is not a member of the complete retained assembly inventory."}
        }
        if(-not $ArchivedEntries.ContainsKey([string]$attempt.normalizedReport.entry)){throw 'Normalized attempt entry is absent from verified archive.'}
        $projected=ConvertTo-TrxProjectedDocument $facts.Document @($context.worktreeRoots);$expected=ConvertTo-TrxCanonicalXml $projected;$actual=$ArchivedEntries[[string]$attempt.normalizedReport.entry]
        if($expected.LongLength -ne $actual.LongLength -or (Get-TrxBytesSha256 $expected) -cne (Get-TrxBytesSha256 $actual)){throw "Archived $label report differs from the full authorized projection of its private original."}
        $after=[IO.File]::ReadAllBytes($path);if((Get-TrxBytesSha256 $after) -cne (Get-TrxBytesSha256 $before)){throw "Private original $label TRX changed during comparison."}
        $null=Get-TrxRetainedAssemblyInventory $context $Private.SourceRoot $Private.Root $runner
    }
    Assert-TrxToolManifestBinding $PublicManifest.tool
    $expectedRun=Copy-TrxPublicRun $PublicRun
    Assert-TrxPublicRunSourceClaims $expectedRun $runner $context $Private.SourceRoot $Private.Root $Private.RunnerBytes -ToolImplementationCommit ([string]$PublicManifest.tool.commit)
    Assert-TrxBoundPublicClaims $PublicRun $expectedRun
    $acl=Test-ProtectedEvidenceDirectory $Private.Root
    if([string]$PublicRun.retention.handle -cne [string]$context.retentionHandle -or [string]$PublicRun.retention.ownerRole -cne 'GeneralOrchestrator' -or [string]$PublicRun.retention.aclVerification -cne 'verified-owner-only-known-copies'){throw 'Public retention declaration is not bound to the verified private context.'}
    return $true
}

function New-DotNetTrxEvidencePackage {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string[]]$SourceContextPath,[Parameter(Mandatory)][string]$OutputDirectory,[string]$BundleId)
    if($SourceContextPath.Count -ne 1){throw 'TRX packaging requires one combined dotnet-trx-source-context-v1 context.'}
    $null=Assert-EvidenceRuntime
    $contextPath=[IO.Path]::GetFullPath($SourceContextPath[0])
    $private=Read-DotNetTrxSourceContext $contextPath;$context=$private.Context;$contextRoot=$private.Root;$runnerBytes=$private.RunnerBytes;$runner=$private.Runner
    $manifestHash=Get-TrxBytesSha256 $runnerBytes
    $reports=@($context.reports); if($reports.Count -ne 2){throw 'The current TRX context requires exactly cold and warm reports.'}
    $outcomes=[System.Collections.Generic.List[object]]::new(); $zipEntries=[System.Collections.Generic.List[object]]::new()
    foreach($label in @('cold','warm')){
        $records=@($reports|Where-Object label -CEQ $label); if($records.Count -ne 1){throw "Source context must map one $label report."};$record=$records[0]
        $path=[IO.Path]::GetFullPath([string]$record.path);$null=Assert-ProtectedEvidenceFile -Path $path -ProtectedRoot $contextRoot
        $before=[IO.File]::ReadAllBytes($path);if($before.LongLength -ne [long]$record.bytes -or (Get-TrxBytesSha256 $before) -cne [string]$record.sha256){throw "Original $label TRX bytes do not match the private context."}
        $facts=Get-TrxFacts $path;if($facts.RunId -cne [string]$record.nativeRunId){throw "Original $label TRX TestRun ID does not match context."}
        $projected=ConvertTo-TrxProjectedDocument $facts.Document @($context.worktreeRoots)
        $normalized=ConvertTo-TrxCanonicalXml $projected
        $temp=[IO.Path]::GetTempFileName();try{[IO.File]::WriteAllBytes($temp,$normalized);$normalizedFacts=Get-TrxFacts $temp;if((Get-TrxBytesSha256 $normalized) -cne (Get-TrxBytesSha256 (ConvertTo-TrxCanonicalXml $normalizedFacts.Document))){throw 'Normalized TRX is not in deterministic canonical form.'}}finally{Remove-Item -LiteralPath $temp -Force -ErrorAction SilentlyContinue}
        $entry="results/$($runner.runId)/$label.trx";$zipEntries.Add([ordered]@{name=$entry;bytes=$normalized})
        $runnerGate=if($label -eq 'cold'){$runner.gates.coldTests}else{$runner.gates.warmTests}
        $outcomes.Add([ordered]@{label=$label;nativeRunId=$facts.RunId;nativeRunLabel="run-$($facts.RunId)";originalReport=[ordered]@{bytes=[long]$before.LongLength;sha256=(Get-TrxBytesSha256 $before)};normalizedReport=[ordered]@{entry=$entry;bytes=[long]$normalized.LongLength;sha256=(Get-TrxBytesSha256 $normalized);semanticSha256=(Get-TrxSemanticSha256 $projected)};summaryOutcome=$facts.SummaryOutcome;nativeCounters=$facts.Counters;caseOutcomeCounts=$facts.OutcomeCounts;resultCount=$facts.ResultCount;gatePassed=[bool]($facts.GatePassed -and [int]$runnerGate.exitCode -eq 0);exitCode=[int]$runnerGate.exitCode;elapsedMilliseconds=[long]$runnerGate.elapsedMilliseconds;comparison='source-projection-verified'})
        $after=[IO.File]::ReadAllBytes($path);if((Get-TrxBytesSha256 $after) -cne (Get-TrxBytesSha256 $before)){throw 'Original TRX changed during packaging.'}
    }
    $publicRuns=@($context.publicRuns);if($publicRuns.Count -ne 1){throw 'The public source manifest must contain one run matching the private runner manifest.'}
    $run=$publicRuns[0];if([string]$run.runnerRunId -cne [string]$runner.runId){throw 'Public source run identity differs from original runner manifest.'}
    Assert-TrxPublicRunSourceClaims $run $runner $context $private.SourceRoot $contextRoot $runnerBytes
    $run.attempts=@($outcomes.ToArray());$run.originalManifest=[ordered]@{bytes=[long]$runnerBytes.LongLength;sha256=$manifestHash}
    $run.source.commit=[string]$runner.source.commit;$run.source.tree=[string]$runner.source.committedTree;$run.source.originalInputDigest=[string]$runner.inputs.digest;$run.source.inputStable=[bool]$runner.inputs.stable
    $run.source.files=@($runner.inputs.files);$run.source.generatedDependencies=@($runner.inputs.generatedDependencies)
    $run.discovery=[ordered]@{count=[int]$runner.testDiscovery.count;cases=@($runner.testDiscovery.cases)}
    if(-not $BundleId){$BundleId=[Guid]::NewGuid().ToString('N')};if($BundleId -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,79}$'){throw 'BundleId may contain only letters, digits, period, underscore, or hyphen.'}
    $archiveName="$BundleId.zip"
    $manifest=[ordered]@{schema='validation-evidence-trx-package-v1';bundleId=$BundleId;format=[ordered]@{reportProfile=$script:TrxProfile;counterProfile=$script:TrxCounterProfile;projectionPolicy=$script:TrxProjection};tool=(Get-TrxToolIdentity);archive=[ordered]@{file=$archiveName;bytes=0;sha256='0'*64;format='zip-deflate-fixed-1980-01-01';entryPolicy='normalized-trx-only'};runs=@($run)}
    $manifestBytes=ConvertTo-EvidenceJsonBytes $manifest;Test-PublicEvidenceText ([Text.UTF8Encoding]::new($false,$true).GetString($manifestBytes))
    $zipPath=Join-Path ([IO.Path]::GetFullPath($OutputDirectory)) $archiveName;$manifestPath=[IO.Path]::ChangeExtension($zipPath,'.manifest.json')
    if(Test-Path -LiteralPath $zipPath -or Test-Path -LiteralPath $manifestPath){throw 'TRX evidence output already exists; refusing overwrite.'}
    New-Item -ItemType Directory -Path $OutputDirectory -Force|Out-Null
    try{Write-DeterministicEvidenceArchive -Path $zipPath -Entries @($zipEntries.ToArray());$zipRecord=Get-Item -LiteralPath $zipPath;$manifest.archive.bytes=[long]$zipRecord.Length;$manifest.archive.sha256=Get-EvidenceSha256 $zipPath;Assert-TrxPublicManifest $manifest;$manifestBytes=ConvertTo-EvidenceJsonBytes $manifest;Test-PublicEvidenceText ([Text.UTF8Encoding]::new($false,$true).GetString($manifestBytes));[IO.File]::WriteAllBytes($manifestPath,$manifestBytes);$verified=Test-DotNetTrxEvidencePackage -ManifestPath $manifestPath -ArchivePath $zipPath -SourceContextPath @($contextPath);if(-not $verified.IsValid){throw $verified.Error};return [pscustomobject]@{ManifestPath=$manifestPath;ArchivePath=$zipPath;ArchiveSha256=$manifest.archive.sha256;ArchiveBytes=$manifest.archive.bytes;OriginalComparison=$verified.OriginalComparison}}catch{Remove-Item -LiteralPath $zipPath -Force -ErrorAction SilentlyContinue;Remove-Item -LiteralPath $manifestPath -Force -ErrorAction SilentlyContinue;throw}
}

function Test-DotNetTrxEvidencePackage {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$ManifestPath,[Parameter(Mandatory)][string]$ArchivePath,[string[]]$SourceContextPath)
    try{
        $null=Assert-EvidenceRuntime;$manifestBytes=[IO.File]::ReadAllBytes($ManifestPath);if($manifestBytes.Length -gt $script:TrxMaxManifestBytes){throw 'TRX package manifest exceeds size limit.'};$manifestText=[Text.UTF8Encoding]::new($false,$true).GetString($manifestBytes);Test-PublicEvidenceText $manifestText;$manifest=$manifestText|ConvertFrom-Json -AsHashtable -DateKind String;Assert-TrxPublicManifest $manifest
        if($SourceContextPath -and $SourceContextPath.Count -ne 1){throw 'TRX comparison requires one source context.'}
        $zip=Get-Item -LiteralPath $ArchivePath -ErrorAction Stop;if([IO.Path]::GetFileName($ArchivePath) -cne [string]$manifest.archive.file -or $zip.Length -ne [long]$manifest.archive.bytes -or (Get-EvidenceSha256 $ArchivePath) -cne $manifest.archive.sha256){throw 'TRX archive identity or integrity mismatch.'}
        Add-Type -AssemblyName System.IO.Compression;$stream=[IO.File]::OpenRead($ArchivePath);try{$archive=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Read,$true);try{
            $expected=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal);foreach($run in @($manifest.runs)){foreach($attempt in @($run.attempts)){$expected.Add([string]$attempt.normalizedReport.entry,$attempt)}}
            if($archive.Entries.Count -ne $expected.Count){throw 'TRX archive entries do not match manifest.'};$seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);$expanded=0L;$actual=[Collections.Generic.List[object]]::new();$actualEntryBytes=[Collections.Generic.Dictionary[string,byte[]]]::new([StringComparer]::Ordinal)
            foreach($entry in $archive.Entries){$name=[string]$entry.FullName;if($name.Contains('\') -or [IO.Path]::IsPathRooted($name) -or $name -match '(^|/)\.\.(/|$)' -or $entry.Name.Length -eq 0){throw 'TRX archive contains an unsafe path.'};if((($entry.ExternalAttributes -shr 16) -band 0xF000) -eq 0xA000){throw 'TRX archive contains a symbolic link.'};if(-not $seen.Add($name) -or -not $expected.ContainsKey($name)){throw 'TRX archive has duplicate or unexpected entry.'};if($entry.Length -gt $script:TrxMaxXmlBytes){throw 'TRX entry exceeds size limit.'};$ms=[IO.MemoryStream]::new();$es=$entry.Open();try{$es.CopyTo($ms);$bytes=$ms.ToArray()}finally{$es.Dispose();$ms.Dispose()};$expanded+=$bytes.LongLength;if($expanded -gt $script:TrxMaxExpandedBytes){throw 'TRX archive expanded size exceeds limit.'};$claim=$expected[$name];if($bytes.LongLength -ne [long]$claim.normalizedReport.bytes -or (Get-TrxBytesSha256 $bytes) -cne $claim.normalizedReport.sha256){throw 'TRX normalized entry hash/length mismatch.'};$tmp=[IO.Path]::GetTempFileName();try{[IO.File]::WriteAllBytes($tmp,$bytes);$facts=Get-TrxFacts $tmp;Assert-TrxDiscoveryMatchesReport @($manifest.runs[0].discovery.cases) $facts ([string]$claim.label);$canonical=ConvertTo-TrxCanonicalXml $facts.Document;if((Get-TrxBytesSha256 $canonical) -cne (Get-TrxBytesSha256 $bytes) -or (Get-TrxSemanticSha256 $facts.Document) -cne $claim.normalizedReport.semanticSha256){throw 'TRX entry is not in deterministic canonical form.'};if($facts.RunId -cne $claim.nativeRunId -or $facts.SummaryOutcome -cne $claim.summaryOutcome -or $facts.ResultCount -ne [int]$claim.resultCount -or (ConvertTo-Json $facts.Counters -Compress) -cne (ConvertTo-Json $claim.nativeCounters -Compress) -or (ConvertTo-Json $facts.OutcomeCounts -Compress) -cne (ConvertTo-Json $claim.caseOutcomeCounts -Compress)){throw 'TRX manifest result claim does not match archived report.'};$expectedGate=[bool]($facts.GatePassed -and [int]$claim.exitCode -eq 0);if($expectedGate -ne [bool]$claim.gatePassed){throw 'TRX gate status claim does not match native outcome and successful command exit.'};Test-PublicEvidenceText ([Text.UTF8Encoding]::new($false,$true).GetString($bytes));$actual.Add([ordered]@{name=$name;bytes=$bytes});$actualEntryBytes.Add($name,$bytes)}finally{Remove-Item -LiteralPath $tmp -Force -ErrorAction SilentlyContinue}}
            $repro=[IO.Path]::GetTempFileName();Remove-Item -LiteralPath $repro -Force;try{Write-DeterministicEvidenceArchive -Path $repro -Entries @($actual.ToArray());if((Get-EvidenceSha256 $repro) -cne $manifest.archive.sha256){throw 'TRX ZIP bytes are not deterministic.'}}finally{Remove-Item -LiteralPath $repro -Force -ErrorAction SilentlyContinue}
        }finally{$archive.Dispose()}}finally{$stream.Dispose()}
        if($SourceContextPath){$private=Read-DotNetTrxSourceContext $SourceContextPath[0];if(@($manifest.runs).Count -ne 1){throw 'Private CORE source context binds exactly one package run.'};Assert-DotNetTrxContextBinding $private $manifest.runs[0] $manifest $actualEntryBytes|Out-Null;$comparison='originalComparisonVerified'}else{$comparison='originalComparisonNotPerformed'}
        return [pscustomobject]@{IsValid=$true;Error=$null;RunCount=@($manifest.runs).Count;EntryCount=$expected.Count;ArchiveSha256=$manifest.archive.sha256;OriginalComparison=$comparison;PackageIntegrityVerified=$true;GatePassed=(@($manifest.runs|ForEach-Object {$_.attempts}|Where-Object {-not $_.gatePassed}).Count -eq 0)}
    }catch{return [pscustomobject]@{IsValid=$false;Error=$_.Exception.Message;RunCount=0;EntryCount=0;ArchiveSha256=$null;OriginalComparison=$null}}
}

function Read-DotNetTrxEvidencePackage {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$ManifestPath)
    $text=[IO.File]::ReadAllText($ManifestPath,[Text.UTF8Encoding]::new($false,$true));Test-PublicEvidenceText $text;$manifest=$text|ConvertFrom-Json -AsHashtable -DateKind String;Assert-TrxPublicManifest $manifest
    foreach($run in @($manifest.runs)){[pscustomobject]@{RunnerRunId=$run.runnerRunId;Attempts=@($run.attempts|ForEach-Object {[pscustomobject]@{Label=$_.label;SummaryOutcome=$_.summaryOutcome;ResultCount=$_.resultCount;GatePassed=$_.gatePassed;Entry=$_.normalizedReport.entry;Sha256=$_.normalizedReport.sha256}});RetentionHandle=$run.retention.handle}}
}

function Get-EvidencePackageReportFormat {
    param([Parameter(Mandatory)][string]$ManifestPath)
    $head=Get-Content -LiteralPath $ManifestPath -Raw|ConvertFrom-Json -AsHashtable -DateKind String
    switch([string]$head.schema){'validation-evidence-trx-package-v1'{return 'DotNetTrx'}'unity-validation-evidence-package-v1'{return 'UnityNUnit'}default{throw 'Unknown evidence package schema.'}}
}

Export-ModuleMember -Function @('Get-TrxFacts','ConvertTo-TrxProjectedDocument','ConvertTo-TrxCanonicalXml','Get-TrxSemanticSha256','New-DotNetTrxEvidencePackage','Test-DotNetTrxEvidencePackage','Read-DotNetTrxEvidencePackage','Get-EvidencePackageReportFormat')
