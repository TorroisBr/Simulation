Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:TrxNamespace = 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'
$script:TrxProfile = 'trx-vstest-flat-v1'
$script:TrxCounterProfile = 'vstest-17.14.1-flat'
$script:TrxProjection = 'trx-private-metadata-v1'
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

function Get-TrxBytesSha256([byte[]]$Bytes) { Get-BytesSha256 -Bytes $Bytes }

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
    $allowedOutcomes = @('Error','Failed','Timeout','Aborted','Inconclusive','PassedButRunAborted','NotRunnable','NotExecuted','Disconnected','Warning','Passed','Completed','InProgress','Pending')
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
    $nativeFailedCases=[long]$hist.Failed+[long]$hist.Error
    if ([long]$native.total -ne $results.Count -or [long]$native.passed -ne $hist.Passed -or [long]$native.failed -ne $nativeFailedCases -or [long]$native.executed -ne ($hist.Passed+$nativeFailedCases)) { throw 'TRX native total/executed/passed/failed counters disagree with result records.' }
    foreach($n in $counterAttrs | Where-Object { $_ -notin @('total','executed','passed','failed') }) { if ([long]$native[$n] -ne 0) { throw "VSTest profile requires native counter '$n' to be zero." } }
    if (@(Get-TrxElementChildren $sections.TestDefinitions 'TestDefinitions').Count -eq 0 -or @(Get-TrxElementChildren $sections.TestEntries 'TestEntries').Count -eq 0) { throw 'TRX test selection is empty.' }
    $nonPassing=0; foreach($outcome in $allowedOutcomes | Where-Object {$_ -ne 'Passed'}) { $nonPassing += [int]$hist[$outcome] }
    return [pscustomobject]@{ Document=$document; RunId=$nativeRunId; RunName=$root.GetAttribute('name'); SummaryOutcome=$summaryOutcome; Counters=$native; OutcomeCounts=$hist; ResultCount=$results.Count; Results=$results; Definitions=$definitions; GatePassed=($summaryOutcome -ceq 'Completed' -and $nonPassing -eq 0) }
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
    if($root.HasAttribute('runUser')){$root.SetAttribute('runUser','<PRIVATE_RUN_USER>')}
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
            $element.SetAttribute('runDeploymentRoot',"<DEPLOYMENT>/$runId")
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

function Assert-TrxPublicManifest {
    param([System.Collections.IDictionary]$Manifest)
    Assert-TrxKeys $Manifest @('schema','bundleId','format','tool','archive','runs') 'manifest'
    if($Manifest.schema -cne 'validation-evidence-trx-package-v1'){throw 'Unsupported TRX evidence manifest schema.'}
    Assert-TrxKeys $Manifest.format @('reportProfile','counterProfile','projectionPolicy') 'manifest.format'
    if($Manifest.format.reportProfile -cne $script:TrxProfile -or $Manifest.format.counterProfile -cne $script:TrxCounterProfile -or $Manifest.format.projectionPolicy -cne $script:TrxProjection){throw 'TRX evidence profile identifiers are unsupported.'}
    Assert-TrxKeys $Manifest.tool @('commit','tree','version','files','runtime') 'manifest.tool'
    foreach($file in @($Manifest.tool.files)){Assert-TrxKeys $file @('path','bytes','sha256') 'manifest.tool.files[]';$null=ConvertTo-TrxPortableRelative ([string]$file.path)}
    Assert-TrxKeys $Manifest.tool.runtime @('powershellEdition','powershellVersion','dotnetVersion','compressionAssemblyVersion') 'manifest.tool.runtime'
    Assert-TrxKeys $Manifest.archive @('file','bytes','sha256','format','entryPolicy') 'manifest.archive'
    if($Manifest.archive.entryPolicy -cne 'normalized-trx-only'){throw 'Unsupported TRX archive entry policy.'}
    $runKeys=@('runnerRunId','source','originalManifest','producer','execution','discovery','commands','assemblies','attempts','demonstrations','measurements','retention','existingReviews')
    $attemptKeys=@('label','nativeRunId','nativeRunLabel','originalReport','normalizedReport','summaryOutcome','nativeCounters','caseOutcomeCounts','resultCount','gatePassed','exitCode','elapsedMilliseconds','comparison')
    $basicHashKeys=@('bytes','sha256')
    $i=0
    foreach($run in @($Manifest.runs)){
        $i++; Assert-TrxKeys $run $runKeys "manifest.runs[$($i-1)]"
        if([string]$run.runnerRunId -notmatch '^[a-f0-9]{32}$'){throw 'TRX runner run ID is invalid.'}
        foreach($attempt in @($run.attempts)){
            Assert-TrxKeys $attempt $attemptKeys 'manifest.runs.attempt'
            Assert-TrxKeys $attempt.originalReport $basicHashKeys 'attempt.originalReport'
            Assert-TrxKeys $attempt.normalizedReport @('entry','bytes','sha256','semanticSha256') 'attempt.normalizedReport'
        }
        Assert-TrxKeys $run.originalManifest $basicHashKeys 'run.originalManifest'
        Assert-TrxKeys $run.source @('commit','tree','originalInputDigest','inputStable','files','generatedDependencies','influencingChanges') 'run.source'
        foreach($file in @($run.source.files)){Assert-TrxKeys $file @('path','bytes','sha256') 'run.source.files[]';$null=ConvertTo-TrxPortableRelative ([string]$file.path)}
        foreach($file in @($run.source.generatedDependencies)){Assert-TrxKeys $file @('path','bytes','sha256') 'run.source.generatedDependencies[]';$null=ConvertTo-TrxPortableRelative ([string]$file.path)}
        foreach($file in @($run.source.influencingChanges)){Assert-TrxKeys $file @('path','gitStatus','bytes','sha256') 'run.source.influencingChanges[]';$null=ConvertTo-TrxPortableRelative ([string]$file.path)}
        Assert-TrxKeys $run.producer @('vstestVersion','sdkVersion','logger','identityMode','provenance') 'run.producer'
        Assert-TrxKeys $run.producer.logger @('file','fileVersion','productVersion','bytes','sha256') 'run.producer.logger'
        Assert-TrxKeys $run.execution @('sdkVersion','clrVersion','targetFramework','configuration','osVersion','osArchitecture','processArchitecture','cultures','packageLocks','provenance') 'run.execution'
        foreach($file in @($run.execution.packageLocks)){Assert-TrxKeys $file @('path','bytes','sha256') 'run.execution.packageLocks[]';$null=ConvertTo-TrxPortableRelative ([string]$file.path)}
        Assert-TrxKeys $run.discovery @('count','cases') 'run.discovery'
        foreach($cmd in @($run.commands)){Assert-TrxKeys $cmd @('label','executable','workingDirectory','argv','exitCode','elapsedMilliseconds','derivation','provenance') 'run.commands[]'}
        foreach($asm in @($run.assemblies)){Assert-TrxKeys $asm @('path','fullName','mvid','bytes','sha256','identityMode','observedAtUtc','provenance') 'run.assemblies[]';$null=ConvertTo-TrxPortableRelative ([string]$asm.path)}
        foreach($demo in @($run.demonstrations)){Assert-TrxKeys $demo @('label','exitCode','elapsedMilliseconds','output','derivation','provenance') 'run.demonstrations[]'}
        Assert-TrxKeys $run.measurements @('worktreeLibraryAbsent','projectBinObjBytes','resultBytes') 'run.measurements'
        Assert-TrxKeys $run.retention @('handle','ownerRole','policy','plannedPostPromotionDays','aclVerification','provenance') 'run.retention'
        Assert-TrxKeys $run.existingReviews @('sourceImplementation','toolImplementation') 'run.existingReviews'
        if([string]$run.retention.handle -notmatch '^evidence-[a-f0-9]{32}$' -or $run.retention.ownerRole -cne 'GeneralOrchestrator' -or $run.retention.aclVerification -cne 'verified-owner-only-known-copies'){throw 'TRX retention attestation is invalid.'}
        foreach($attempt in @($run.attempts)){
            if($attempt.label -cnotin @('cold','warm') -or [string]$attempt.nativeRunLabel -cne "run-$($attempt.nativeRunId)" -or [string]$attempt.nativeRunId -notmatch '^[0-9a-fA-F-]{32,36}$'){throw 'TRX native attempt identity/label is invalid.'}
            Assert-TrxKeys $attempt.nativeCounters @('total','executed','passed','failed','error','timeout','aborted','inconclusive','passedButRunAborted','notRunnable','notExecuted','disconnected','warning','completed','inProgress','pending') 'attempt.nativeCounters'
            $outcomeKeys=@('Error','Failed','Timeout','Aborted','Inconclusive','PassedButRunAborted','NotRunnable','NotExecuted','Disconnected','Warning','Passed','Completed','InProgress','Pending')
            Assert-TrxKeys $attempt.caseOutcomeCounts $outcomeKeys 'attempt.caseOutcomeCounts'
            if([int]$attempt.resultCount -ne [int]$attempt.nativeCounters.total -or $attempt.comparison -cne 'source-projection-verified'){throw 'TRX result accounting/comparison claim is invalid.'}
            if([string]$attempt.normalizedReport.entry -notmatch ('^results/'+[regex]::Escape([string]$run.runnerRunId)+'/(cold|warm)\.trx$')){throw 'TRX report entry path is invalid.'}
        }
        foreach($prov in @($run.producer.provenance)+@($run.execution.provenance)+@($run.retention.provenance)){
            $keys=@('kind','commit','blob','path','bytes','sha256','field','mode');foreach($key in $keys){if($prov.Contains($key) -and $key -eq 'path'){$null=ConvertTo-TrxPortableRelative ([string]$prov.path)}}
        }
    }
}

function Read-DotNetTrxSourceContext {
    param([string]$SourceContextPath)
    $contextPath=[IO.Path]::GetFullPath($SourceContextPath);$contextRoot=Split-Path -Parent $contextPath
    $null=Test-ProtectedEvidenceDirectory $contextRoot;$null=Assert-ProtectedEvidenceFile -Path $contextPath -ProtectedRoot $contextRoot
    $contextBytes=[IO.File]::ReadAllBytes($contextPath);$context=([Text.UTF8Encoding]::new($false,$true).GetString($contextBytes)|ConvertFrom-Json -AsHashtable -DateKind String)
    Assert-TrxKeys $context @('schema','runnerManifestPath','worktreeRoots','retentionHandle','publicRuns','reports','runtimeSelectionTraces') 'source context'
    if($context.schema -cne 'dotnet-trx-source-context-v1' -or [string]$context.retentionHandle -notmatch '^evidence-[a-f0-9]{32}$'){throw 'Unsupported TRX source-context schema or retention handle.'}
    if(@($context.worktreeRoots).Count -lt 1){throw 'TRX source context has no verified worktree root mapping.'}
    $runnerPath=[IO.Path]::GetFullPath([string]$context.runnerManifestPath);$null=Assert-ProtectedEvidenceFile -Path $runnerPath -ProtectedRoot $contextRoot
    $runnerBytes=[IO.File]::ReadAllBytes($runnerPath);$runner=([Text.UTF8Encoding]::new($false,$true).GetString($runnerBytes)|ConvertFrom-Json -AsHashtable -DateKind String)
    if($runner.schema -cne 'simulation-core-validation/v1' -or -not $runner.inputs.stable -or -not $runner.inputs.generatedDependenciesStable -or $runner.status -cne 'Passed'){throw 'Original CORE Validation manifest is incomplete or unstable.'}
    return [pscustomobject]@{Path=$contextPath;Root=$contextRoot;Bytes=$contextBytes;Context=$context;RunnerPath=$runnerPath;RunnerBytes=$runnerBytes;Runner=$runner;RunnerSha256=(Get-TrxBytesSha256 $runnerBytes)}
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
        $configHits=[System.Collections.Generic.List[int]]::new()
        for($lineIndex=0;$lineIndex -lt $lines.Count;$lineIndex++){
            if($lines[$lineIndex] -match '(?i)^\s*Runtime config is cfg=(?<path>.+Simulation\.Core\.Tests\.runtimeconfig\.dev\.json)\s*$'){$configHits.Add($lineIndex)}
        }
        if($configHits.Count -ne 1){throw "Private $label runtime trace must contain exactly one test-assembly runtimeconfig selection block."}
        $configLine=$lines[$configHits[0]]
        $null=$configLine -match '(?i)^\s*Runtime config is cfg=(?<path>.+Simulation\.Core\.Tests\.runtimeconfig\.dev\.json)\s*$'
        $runtimeConfigPath=$Matches.path.Trim()
        if(-not [IO.Path]::IsPathRooted($runtimeConfigPath)){throw "Private $label runtimeconfig trace path is not absolute."}
        $runtimeConfigRelative=Get-TrxMappedRelativePath $runtimeConfigPath @($Context.worktreeRoots)
        $testAssemblies=@($PublicRun.assemblies|Where-Object { [IO.Path]::GetFileNameWithoutExtension([string]$_.path) -ceq 'Simulation.Core.Tests' })
        if($testAssemblies.Count -ne 1){throw 'Private runtime trace cannot bind to a unique public test-assembly record.'}
        $expectedConfig=[IO.Path]::ChangeExtension([string]$testAssemblies[0].path,'runtimeconfig.dev.json').Replace('\','/')
        if($runtimeConfigRelative.Replace('\','/') -cne $expectedConfig){throw "Private $label runtimeconfig is not colocated with the bound test assembly."}
        $nextConfig=$lines.Count
        for($lineIndex=$configHits[0]+1;$lineIndex -lt $lines.Count;$lineIndex++){if($lines[$lineIndex] -match '(?i)^\s*Runtime config is cfg='){$nextConfig=$lineIndex;break}}
        $chosen=@()
        for($lineIndex=$configHits[0]+1;$lineIndex -lt $nextConfig;$lineIndex++){
            if($lines[$lineIndex] -match '(?i)^\s*Chose FX version \[(?<path>[^\]\r\n]+)\]\s*$'){$chosen+=,$Matches.path}
        }
        if($chosen.Count -ne 1 -or $chosen[0] -notmatch '(?i)(?:^|[\\/])shared[\\/]Microsoft\.NETCore\.App[\\/](?<version>\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?)$' -or [string]$Matches.version -cne [string]$record.selectedClr){throw "Private $label runtime trace does not prove its declared selected Microsoft.NETCore.App CLR in the test-assembly config block."}
        $gate=if($label -eq 'cold'){$Runner.gates.coldTests}else{$Runner.gates.warmTests}
        $report=@($Context.reports|Where-Object label -CEQ $label)
        if($report.Count -ne 1 -or [string]$gate.runId -notmatch '^[0-9a-fA-F-]{36}$' -or [string]$gate.runId -cne [string]$report[0].nativeRunId -or [int]$gate.exitCode -ne 0){throw "Private $label runtime trace is not bound to the successful matching runner gate."}
    }
}

function Get-TrxToolIdentity {
    $projectRoot=[IO.Directory]::GetParent([IO.Directory]::GetParent($PSScriptRoot).FullName).FullName
    $paths=@('Tools/UnityValidation/Package-UnityValidationEvidence.ps1','Tools/UnityValidation/UnityValidationEvidence.psm1','Tools/UnityValidation/TrxValidationEvidence.psm1')
    $dirty=& git -C $projectRoot status --porcelain=v1 -- @paths 2>$null
    if($LASTEXITCODE -ne 0 -or @($dirty).Count -gt 0){throw 'TRX package tool files must be committed and clean before packaging.'}
    $commit=((& git -C $projectRoot rev-parse HEAD 2>$null)|Out-String).Trim();$tree=((& git -C $projectRoot rev-parse 'HEAD^{tree}' 2>$null)|Out-String).Trim()
    if($LASTEXITCODE -ne 0){throw 'Cannot identify exact committed TRX packager source.'}
    $files=[System.Collections.Generic.List[object]]::new()
    foreach($relative in $paths){$full=Join-Path $projectRoot ($relative.Replace('/',[IO.Path]::DirectorySeparatorChar));$item=Get-Item -LiteralPath $full -ErrorAction Stop;$files.Add([ordered]@{path=$relative;bytes=[long]$item.Length;sha256=(Get-EvidenceSha256 $full)})}
    $runtime=Assert-EvidenceRuntime
    return [ordered]@{commit=$commit;tree=$tree;version='1.2.0';files=@($files.ToArray());runtime=[ordered]@{powershellEdition=$runtime.powershellEdition;powershellVersion=$runtime.powershellVersion;dotnetVersion=$runtime.dotnetVersion;compressionAssemblyVersion=$runtime.compressionAssemblyVersion}}
}

function Get-TrxAssemblyIdentity {
    param([Parameter(Mandatory)][string]$Path)
    Add-Type -AssemblyName System.Reflection.Metadata
    $stream=[IO.File]::OpenRead($Path);try{$pe=[System.Reflection.PortableExecutable.PEReader]::new($stream);try{if(-not $pe.HasMetadata){throw 'Recovered assembly has no CLI metadata.'};$metadata=$pe.GetMetadataReader();$definition=$metadata.GetAssemblyDefinition();$module=$metadata.GetModuleDefinition();$name=[System.Reflection.AssemblyName]::GetAssemblyName($Path);return [pscustomobject]@{fullName=$name.FullName;mvid=$metadata.GetGuid($module.Mvid).ToString('D')}}finally{$pe.Dispose()}}finally{$stream.Dispose()}
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
        $expectedNames=@($runner.testDiscovery.cases);$actualNames=@($facts.Results|ForEach-Object {$_.GetAttribute('testName')});[Array]::Sort($expectedNames,[StringComparer]::Ordinal);[Array]::Sort($actualNames,[StringComparer]::Ordinal);if(($expectedNames -join "`n") -cne ($actualNames -join "`n")){throw "Private $label TRX case-name multiset differs from captured discovery."}
        $gateCases=@($gate.cases);if($gateCases.Count -ne $facts.Results.Count){throw "Private $label TRX case identities differ from runner evidence."}
        $gateMap=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal);foreach($case in $gateCases){$key="$($case.testId)|$($case.executionId)";if($gateMap.ContainsKey($key)){throw 'Original runner evidence contains duplicate case identity.'};$gateMap.Add($key,$case)}
        foreach($result in $facts.Results){$key="$($result.GetAttribute('testId'))|$($result.GetAttribute('executionId'))";if(-not $gateMap.ContainsKey($key)){throw 'TRX test identity is absent from original runner record.'};$case=$gateMap[$key];if([string]$case.testName -cne $result.GetAttribute('testName') -or [string]$case.outcome -cne $result.GetAttribute('outcome')){throw 'TRX case name/outcome differs from original runner record.'}}
        foreach($definition in @($facts.Document.SelectNodes("//*[local-name()='UnitTest']"))){$method=$definition.SelectSingleNode("*[local-name()='TestMethod']");$storage=Get-TrxMappedRelativePath $definition.GetAttribute('storage') @($context.worktreeRoots);$codeBase=Get-TrxMappedRelativePath $method.GetAttribute('codeBase') @($context.worktreeRoots);if($storage -cne $codeBase){throw 'TRX UnitTest storage and TestMethod codeBase do not bind to the same relative assembly.'};$assembly=@($PublicRun.assemblies|Where-Object { [string]$_.path -ceq $codeBase });if($assembly.Count -ne 1){throw "TRX assembly target '$codeBase' has no unique public binary identity record."};$assembly=$assembly[0];$assemblyPath=$null;foreach($root in @($context.worktreeRoots)){$candidate=Join-Path $root ($codeBase.Replace('/',[IO.Path]::DirectorySeparatorChar));if(Test-Path -LiteralPath $candidate -PathType Leaf){$assemblyPath=$candidate;break}};if($null -eq $assemblyPath){throw "TRX assembly target '$codeBase' is missing from original output context."};$binary=Get-Item -LiteralPath $assemblyPath;$hash=Get-EvidenceSha256 $assemblyPath;$identity=Get-TrxAssemblyIdentity $assemblyPath;if([long]$assembly.bytes -ne $binary.Length -or [string]$assembly.sha256 -cne $hash -or [string]$assembly.fullName -cne $identity.fullName -or [string]$assembly.mvid -cne $identity.mvid){throw "Recovered assembly identity for '$codeBase' does not match binary bytes/metadata."}}
        if(-not $ArchivedEntries.ContainsKey([string]$attempt.normalizedReport.entry)){throw 'Normalized attempt entry is absent from verified archive.'}
        $projected=ConvertTo-TrxProjectedDocument $facts.Document @($context.worktreeRoots);$expected=ConvertTo-TrxCanonicalXml $projected;$actual=$ArchivedEntries[[string]$attempt.normalizedReport.entry]
        if($expected.LongLength -ne $actual.LongLength -or (Get-TrxBytesSha256 $expected) -cne (Get-TrxBytesSha256 $actual)){throw "Archived $label report differs from the full authorized projection of its private original."}
        $after=[IO.File]::ReadAllBytes($path);if((Get-TrxBytesSha256 $after) -cne (Get-TrxBytesSha256 $before)){throw "Private original $label TRX changed during comparison."}
    }
    $acl=Test-ProtectedEvidenceDirectory $Private.Root
    if([string]$PublicRun.retention.handle -cne [string]$context.retentionHandle -or [string]$PublicRun.retention.ownerRole -cne 'GeneralOrchestrator' -or [string]$PublicRun.retention.aclVerification -cne 'verified-owner-only-known-copies'){throw 'Public retention declaration is not bound to the verified private context.'}
    return $true
}

function New-DotNetTrxEvidencePackage {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string[]]$SourceContextPath,[Parameter(Mandatory)][string]$OutputDirectory,[string]$BundleId)
    if($SourceContextPath.Count -ne 1){throw 'TRX packaging requires one combined dotnet-trx-source-context-v1 context.'}
    $null=Assert-EvidenceRuntime
    $contextPath=[IO.Path]::GetFullPath($SourceContextPath[0]); $contextRoot=Split-Path -Parent $contextPath
    $null=Test-ProtectedEvidenceDirectory $contextRoot; $null=Assert-ProtectedEvidenceFile -Path $contextPath -ProtectedRoot $contextRoot
    $contextBytes=[IO.File]::ReadAllBytes($contextPath); $context=([Text.UTF8Encoding]::new($false,$true).GetString($contextBytes)|ConvertFrom-Json -AsHashtable -DateKind String)
    Assert-TrxKeys $context @('schema','runnerManifestPath','worktreeRoots','retentionHandle','publicRuns','reports','runtimeSelectionTraces') 'source context'
    if($context.schema -cne 'dotnet-trx-source-context-v1'){throw 'Unsupported TRX source context schema.'}
    if([string]$context.retentionHandle -notmatch '^evidence-[a-f0-9]{32}$'){throw 'Invalid private evidence retention handle.'}
    $runnerPath=[IO.Path]::GetFullPath([string]$context.runnerManifestPath); $null=Assert-ProtectedEvidenceFile -Path $runnerPath -ProtectedRoot $contextRoot
    $runnerBytes=[IO.File]::ReadAllBytes($runnerPath); $runnerText=[Text.UTF8Encoding]::new($false,$true).GetString($runnerBytes); $runner=$runnerText|ConvertFrom-Json -AsHashtable -DateKind String
    if($runner.schema -cne 'simulation-core-validation/v1' -or -not $runner.inputs.stable -or -not $runner.inputs.generatedDependenciesStable -or $runner.status -cne 'Passed'){throw 'Original CORE validation manifest is incomplete, unstable, or unsupported.'}
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
        $outcomes.Add([ordered]@{label=$label;nativeRunId=$facts.RunId;nativeRunLabel="run-$($facts.RunId)";originalReport=[ordered]@{bytes=[long]$before.LongLength;sha256=(Get-TrxBytesSha256 $before)};normalizedReport=[ordered]@{entry=$entry;bytes=[long]$normalized.LongLength;sha256=(Get-TrxBytesSha256 $normalized);semanticSha256=(Get-TrxSemanticSha256 $projected)};summaryOutcome=$facts.SummaryOutcome;nativeCounters=$facts.Counters;caseOutcomeCounts=$facts.OutcomeCounts;resultCount=$facts.ResultCount;gatePassed=[bool]$facts.GatePassed;exitCode=[int]$record.exitCode;elapsedMilliseconds=[long]$record.elapsedMilliseconds;comparison='source-projection-verified'})
        $after=[IO.File]::ReadAllBytes($path);if((Get-TrxBytesSha256 $after) -cne (Get-TrxBytesSha256 $before)){throw 'Original TRX changed during packaging.'}
    }
    $publicRuns=@($context.publicRuns);if($publicRuns.Count -ne 1){throw 'The public source manifest must contain one run matching the private runner manifest.'}
    $run=$publicRuns[0];if([string]$run.runnerRunId -cne [string]$runner.runId){throw 'Public source run identity differs from original runner manifest.'}
    $run.attempts=@($outcomes.ToArray());$run.originalManifest=[ordered]@{bytes=[long]$runnerBytes.LongLength;sha256=$manifestHash}
    $run.source.commit=[string]$runner.source.commit;$run.source.tree=[string]$runner.source.committedTree;$run.source.originalInputDigest=[string]$runner.inputs.digest;$run.source.inputStable=[bool]$runner.inputs.stable
    $run.source.files=@($runner.inputs.files);$run.source.generatedDependencies=@($runner.inputs.generatedDependencies);$run.source.influencingChanges=@()
    $run.discovery=[ordered]@{count=[int]$runner.testDiscovery.count;cases=@($runner.testDiscovery.cases)}
    if(-not $BundleId){$BundleId=[Guid]::NewGuid().ToString('N')};if($BundleId -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,79}$'){throw 'BundleId may contain only letters, digits, period, underscore, or hyphen.'}
    $archiveName="$BundleId.zip"
    $manifest=[ordered]@{schema='validation-evidence-trx-package-v1';bundleId=$BundleId;format=[ordered]@{reportProfile=$script:TrxProfile;counterProfile=$script:TrxCounterProfile;projectionPolicy=$script:TrxProjection};tool=(Get-TrxToolIdentity);archive=[ordered]@{file=$archiveName;bytes=0;sha256='0'*64;format='zip-deflate-fixed-1980-01-01';entryPolicy='normalized-trx-only'};runs=@($run)}
    Assert-TrxPublicManifest $manifest
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
        $zip=Get-Item -LiteralPath $ArchivePath -ErrorAction Stop;if($zip.Length -ne [long]$manifest.archive.bytes -or (Get-EvidenceSha256 $ArchivePath) -cne $manifest.archive.sha256){throw 'TRX archive integrity mismatch.'}
        Add-Type -AssemblyName System.IO.Compression;$stream=[IO.File]::OpenRead($ArchivePath);try{$archive=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Read,$true);try{
            $expected=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal);foreach($run in @($manifest.runs)){foreach($attempt in @($run.attempts)){$expected.Add([string]$attempt.normalizedReport.entry,$attempt)}}
            if($archive.Entries.Count -ne $expected.Count){throw 'TRX archive entries do not match manifest.'};$seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);$expanded=0L;$actual=[Collections.Generic.List[object]]::new();$actualEntryBytes=[Collections.Generic.Dictionary[string,byte[]]]::new([StringComparer]::Ordinal)
            foreach($entry in $archive.Entries){$name=[string]$entry.FullName;if($name.Contains('\') -or [IO.Path]::IsPathRooted($name) -or $name -match '(^|/)\.\.(/|$)' -or $entry.Name.Length -eq 0){throw 'TRX archive contains an unsafe path.'};if((($entry.ExternalAttributes -shr 16) -band 0xF000) -eq 0xA000){throw 'TRX archive contains a symbolic link.'};if(-not $seen.Add($name) -or -not $expected.ContainsKey($name)){throw 'TRX archive has duplicate or unexpected entry.'};if($entry.Length -gt $script:TrxMaxXmlBytes){throw 'TRX entry exceeds size limit.'};$ms=[IO.MemoryStream]::new();$es=$entry.Open();try{$es.CopyTo($ms);$bytes=$ms.ToArray()}finally{$es.Dispose();$ms.Dispose()};$expanded+=$bytes.LongLength;if($expanded -gt $script:TrxMaxExpandedBytes){throw 'TRX archive expanded size exceeds limit.'};$claim=$expected[$name];if($bytes.LongLength -ne [long]$claim.normalizedReport.bytes -or (Get-TrxBytesSha256 $bytes) -cne $claim.normalizedReport.sha256){throw 'TRX normalized entry hash/length mismatch.'};$tmp=[IO.Path]::GetTempFileName();try{[IO.File]::WriteAllBytes($tmp,$bytes);$facts=Get-TrxFacts $tmp;$canonical=ConvertTo-TrxCanonicalXml $facts.Document;if((Get-TrxBytesSha256 $canonical) -cne (Get-TrxBytesSha256 $bytes) -or (Get-TrxSemanticSha256 $facts.Document) -cne $claim.normalizedReport.semanticSha256){throw 'TRX entry is not in deterministic canonical form.'};if($facts.RunId -cne $claim.nativeRunId -or $facts.SummaryOutcome -cne $claim.summaryOutcome -or $facts.ResultCount -ne [int]$claim.resultCount -or (ConvertTo-Json $facts.Counters -Compress) -cne (ConvertTo-Json $claim.nativeCounters -Compress) -or (ConvertTo-Json $facts.OutcomeCounts -Compress) -cne (ConvertTo-Json $claim.caseOutcomeCounts -Compress)){throw 'TRX manifest result claim does not match archived report.'};if($facts.GatePassed -ne [bool]$claim.gatePassed){throw 'TRX gate status claim mismatch.'};Test-PublicEvidenceText ([Text.UTF8Encoding]::new($false,$true).GetString($bytes));$actual.Add([ordered]@{name=$name;bytes=$bytes});$actualEntryBytes.Add($name,$bytes)}finally{Remove-Item -LiteralPath $tmp -Force -ErrorAction SilentlyContinue}}
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
