Set-StrictMode -Version Latest

function Get-CoreRepositoryRelativeIdentity {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$RepositoryRoot, [Parameter(Mandatory)][string]$FullPath)
    return Test-CoreRepositoryPathContainment -RepositoryRoot $RepositoryRoot -FullPath $FullPath
}

function Test-CoreRepositoryPathContainment {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][string]$FullPath,
        [switch]$AllowMissingPath
    )

    $root = [IO.Path]::GetFullPath($RepositoryRoot).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $rootPrefix = $root + [IO.Path]::DirectorySeparatorChar
    $full = [IO.Path]::GetFullPath($FullPath)
    if (-not $full.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'A compile input resolved outside the repository root.'
    }

    $rootItem = Get-Item -LiteralPath $root -Force -ErrorAction Stop
    if ($rootItem.Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw 'The repository root is a reparse point and cannot establish source containment.'
    }

    $relative = $full.Substring($rootPrefix.Length)
    $current = $root
    foreach ($segment in ($relative -split '[\\/]')) {
        if ([string]::IsNullOrWhiteSpace($segment)) { continue }
        $current = Join-Path $current $segment
        try {
            $component = Get-Item -LiteralPath $current -Force -ErrorAction Stop
        } catch {
            if ($AllowMissingPath -and $_.CategoryInfo.Category -eq [Management.Automation.ErrorCategory]::ObjectNotFound) {
                break
            }
            throw
        }
        if ($component.Attributes -band [IO.FileAttributes]::ReparsePoint) {
            throw 'A compile input traverses a reparse point below the repository root.'
        }
    }
    return $relative.Replace('\', '/')
}

function Get-CoreProjectCompileInputs {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$ProjectRoot,
        [Parameter(Mandatory)][string[]]$ProjectPaths
    )

    $root = (Resolve-Path -LiteralPath $ProjectRoot).Path
    $pending = [System.Collections.Generic.Queue[string]]::new()
    foreach ($projectPath in $ProjectPaths) { $pending.Enqueue($projectPath) }
    $seenProjects = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $inputPaths = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)

    while ($pending.Count -gt 0) {
        $projectIdentity = $pending.Dequeue().Replace('\', '/')
        $projectPath = Join-Path $root ($projectIdentity -replace '/', [IO.Path]::DirectorySeparatorChar)
        if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) { throw "Validation project is missing: $projectIdentity" }
        $projectFullPath = [IO.Path]::GetFullPath($projectPath)
        $projectRelative = Get-CoreRepositoryRelativeIdentity -RepositoryRoot $root -FullPath $projectFullPath
        if (-not $seenProjects.Add($projectRelative)) { continue }
        [void]$inputPaths.Add($projectRelative)

        # Ask MSBuild for its evaluated Compile and ProjectReference items.
        # This resolves explicit links, imports, conditions, and SDK globs
        # without duplicating MSBuild's evaluation rules in this runner.
        $evaluationOutput = @(& dotnet msbuild $projectFullPath -nologo -getItem:Compile -getItem:ProjectReference 2>$null | ForEach-Object { $_.ToString() })
        $evaluationExitCode = $LASTEXITCODE
        if ($evaluationExitCode -ne 0) { throw "MSBuild input evaluation failed for project $projectRelative." }
        try {
            $evaluation = [string]::Join([Environment]::NewLine, [string[]]$evaluationOutput) | ConvertFrom-Json -ErrorAction Stop
        } catch {
            throw "MSBuild returned an unreadable input evaluation for project $projectRelative."
        }

        foreach ($item in @($evaluation.Items.Compile)) {
            if ($null -eq $item) { continue }
            $itemPath = [string]$item.FullPath
            if ([string]::IsNullOrWhiteSpace($itemPath)) {
                $itemPath = [string]$item.Identity
                if (-not [IO.Path]::IsPathRooted($itemPath)) { $itemPath = Join-Path (Split-Path -Parent $projectFullPath) $itemPath }
            }
            if ([string]::IsNullOrWhiteSpace($itemPath)) { throw "MSBuild returned a Compile item without a path for $projectRelative." }
            $itemFullPath = [IO.Path]::GetFullPath($itemPath)
            # Validate root containment and every existing path component before
            # excluding SDK-generated outputs. Their final leaves may not exist
            # yet, but an existing junction anywhere in the path must be rejected.
            $isGeneratedOutput = $itemFullPath -match '(?i)[\\/](?:obj|bin)[\\/]'
            $itemRelative = Test-CoreRepositoryPathContainment -RepositoryRoot $root -FullPath $itemFullPath -AllowMissingPath:$isGeneratedOutput
            # SDK-generated compilation inputs are derived from tracked source,
            # project, SDK and package inputs; they are not source identities.
            if ($isGeneratedOutput) { continue }
            if (-not (Test-Path -LiteralPath $itemFullPath -PathType Leaf)) { throw "Declared compile input is missing: $itemRelative" }
            [void]$inputPaths.Add($itemRelative)
        }

        foreach ($reference in @($evaluation.Items.ProjectReference)) {
            if ($null -eq $reference) { continue }
            $referencePath = [string]$reference.FullPath
            if ([string]::IsNullOrWhiteSpace($referencePath)) {
                $referencePath = [string]$reference.Identity
                if (-not [IO.Path]::IsPathRooted($referencePath)) { $referencePath = Join-Path (Split-Path -Parent $projectFullPath) $referencePath }
            }
            if ([string]::IsNullOrWhiteSpace($referencePath)) { throw "MSBuild returned a ProjectReference without a path for $projectRelative." }
            $referenceFullPath = [IO.Path]::GetFullPath($referencePath)
            $referenceRelative = Get-CoreRepositoryRelativeIdentity -RepositoryRoot $root -FullPath $referenceFullPath
            $pending.Enqueue($referenceRelative)
        }
    }

    return @($inputPaths | Sort-Object)
}

function Get-CoreInputSnapshot {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$ProjectRoot,
        [string[]]$ProjectPaths = @(
            'tests/Simulation.Core.Tests/Simulation.Core.Tests.csproj',
            'tools/Simulation.Core.CalendarDemo/Simulation.Core.CalendarDemo.csproj',
            'tools/Simulation.Lab/Simulation.Lab.csproj'
        )
    )

    $root = (Resolve-Path -LiteralPath $ProjectRoot).Path
    $fixedInputs = @(
        'Assets/_Project/Tests/EditMode/Editor/Core/DailyV1CapturedSelectedContentFixture.cs',
        'Assets/_Project/Tests/EditMode/Editor/Core/DailyV1CapturedSelectedContentFixture.cs.meta',
        'Assets/_Project/Tests/EditMode/Editor/Core/DailyV1DefinitionAdapterTypedParityTests.cs',
        'Assets/_Project/Tests/EditMode/Editor/Core/DailyV1DefinitionAdapterTypedParityTests.cs.meta',
        'Assets/_Project/Tests/EditMode/Editor/Core/CORE_F_CompatibilityParityTests.cs',
        'Assets/_Project/Tests/EditMode/Editor/Core/CORE_F_CompatibilityParityTests.cs.meta',
        'Assets/_Project/Scripts/CoreAdapters/DailyV1DefinitionAdapter.cs',
        'Assets/_Project/Scripts/CoreAdapters/DailyV1DefinitionAdapter.cs.meta',
        'Assets/_Project/Scripts/CoreAdapters/P12ADailyV1ContentAdapter.cs',
        'Assets/_Project/Scripts/CoreAdapters/P12ADailyV1ContentAdapter.cs.meta',
        'Assets/_Project/Scripts/SpatialRouteKnowledge.cs',
        'Assets/_Project/Tests/EditMode/Editor/Calendar/CalendarCoreTests.cs',
        'Assets/_Project/Tests/EditMode/Editor/Calendar/CalendarCoreTests.cs.meta',
        'Assets/_Project/Tests/EditMode/Editor/Calendar/CalendarParityTests.cs',
        'Assets/_Project/Tests/EditMode/Editor/Calendar/CalendarParityTests.cs.meta',
        'Assets/_Project/Tests/EditMode/Editor/Core/DeterministicDemographyPolicyTests.cs',
        'Assets/_Project/Tests/EditMode/Editor/Core/DeterministicDemographyPolicyTests.cs.meta',
        'Assets/_Project/Tests/EditMode/Editor/Core/DeterministicRandomCoreTests.cs',
        'Assets/_Project/Tests/EditMode/Editor/Core/DeterministicRandomCoreTests.cs.meta',
        'Assets/_Project/Tests/EditMode/Editor/Core/FiniteSourceProductionKernelTests.cs',
        'Assets/_Project/Tests/EditMode/Editor/Core/FiniteSourceProductionKernelTests.cs.meta',
        'Assets/_Project/Tests/EditMode/Editor/Core/FiniteSourceScenarioParityVectors.cs',
        'Assets/_Project/Tests/EditMode/Editor/Core/FiniteSourceScenarioParityVectors.cs.meta',
        'Assets/_Project/Tests/EditMode/Editor/Core/FiniteSourceScenarioRuntimeTests.cs',
        'Assets/_Project/Tests/EditMode/Editor/Core/FiniteSourceScenarioRuntimeTests.cs.meta',
        'Assets/_Project/Tests/EditMode/Editor/Core/MarketPriceCalculatorParityTests.cs',
        'Assets/_Project/Tests/EditMode/Editor/Core/MarketPriceCalculatorParityTests.cs.meta',
        'Assets/_Project/Tests/EditMode/Editor/Core/MarketPriceParityVectors.cs',
        'Assets/_Project/Tests/EditMode/Editor/Core/MarketPriceParityVectors.cs.meta',
        'Assets/_Project/Tests/EditMode/Editor/Core/PersistenceFoundationFaultMatrixTests.cs',
        'Assets/_Project/Tests/EditMode/Editor/Core/PersistenceFoundationFaultMatrixTests.cs.meta',
        'Assets/_Project/Tests/EditMode/Editor/Core/PersistenceFoundationTests.cs',
        'Assets/_Project/Tests/EditMode/Editor/Core/PersistenceFoundationTests.cs.meta',
        'Assets/_Project/Tests/EditMode/Editor/Core/StableKeyEncodingTests.cs',
        'Assets/_Project/Tests/EditMode/Editor/Core/StableKeyEncodingTests.cs.meta',
        'Assets/_Project/Tests/EditMode/Editor/DeterministicRandomRootSnapshotTests.cs',
        'Assets/_Project/Tests/EditMode/Editor/DeterministicRandomTests.cs',
        'Assets/_Project/Tests/EditMode/Editor/LogicalTimelineTests.cs',
        'Packages/com.simulation.core/Runtime.meta',
         'Packages/com.simulation.core/Runtime/Application/DailyV1SelectedCrimeJusticeDailyStage.cs',
         'Packages/com.simulation.core/Runtime/Application/DailyV1SelectedCrimeJusticeDailyStage.cs.meta',
        'Packages/com.simulation.core/Runtime/Calendar.meta',
        'Packages/com.simulation.core/Runtime/Calendar/CalendarDefinition.cs',
        'Packages/com.simulation.core/Runtime/Calendar/CalendarDefinition.cs.meta',
        'Packages/com.simulation.core/Runtime/Calendar/SimulationCalendar.cs',
        'Packages/com.simulation.core/Runtime/Calendar/SimulationCalendar.cs.meta',
        'Packages/com.simulation.core/Runtime/Calendar/SimulationDate.cs',
        'Packages/com.simulation.core/Runtime/Calendar/SimulationDate.cs.meta',
        'Packages/com.simulation.core/Runtime/Configuration.meta',
        'Packages/com.simulation.core/Runtime/Configuration/SimulationConfigurationDefaults.cs',
        'Packages/com.simulation.core/Runtime/Configuration/SimulationConfigurationDefaults.cs.meta',
        'Packages/com.simulation.core/Runtime/Configuration/SimulationConfigurationResolver.cs',
        'Packages/com.simulation.core/Runtime/Configuration/SimulationConfigurationResolver.cs.meta',
        'Packages/com.simulation.core/Runtime/Configuration/SimulationConfigurationTypes.cs',
        'Packages/com.simulation.core/Runtime/Configuration/SimulationConfigurationTypes.cs.meta',
        'Packages/com.simulation.core/Runtime/Configuration/SimulationConfigurationValidationResult.cs',
        'Packages/com.simulation.core/Runtime/Configuration/SimulationConfigurationValidationResult.cs.meta',
        'Packages/com.simulation.core/Runtime/Configuration/SimulationConfigurationValidator.cs',
        'Packages/com.simulation.core/Runtime/Configuration/SimulationConfigurationValidator.cs.meta',
        'Packages/com.simulation.core/Runtime/Definitions.meta',
        'Packages/com.simulation.core/Runtime/Definitions/DailyV1ContentProjection.cs',
        'Packages/com.simulation.core/Runtime/Definitions/DailyV1ContentProjection.cs.meta',
        'Packages/com.simulation.core/Runtime/Definitions/DailyV1DefinitionIdentity.cs',
        'Packages/com.simulation.core/Runtime/Definitions/DailyV1DefinitionIdentity.cs.meta',
        'Packages/com.simulation.core/Runtime/Definitions/DailyV1EffectiveDefinitions.cs',
        'Packages/com.simulation.core/Runtime/Definitions/DailyV1EffectiveDefinitions.cs.meta',
        'Packages/com.simulation.core/Runtime/Definitions/DailyV1TypedDefinitionValues.cs',
        'Packages/com.simulation.core/Runtime/Definitions/DailyV1TypedDefinitionValues.cs.meta',
        'Packages/com.simulation.core/Runtime/Determinism.meta',
        'Packages/com.simulation.core/Runtime/Determinism/DeterministicRandom.cs',
        'Packages/com.simulation.core/Runtime/Determinism/DeterministicRandom.cs.meta',
        'Packages/com.simulation.core/Runtime/Economy.meta',
        'Packages/com.simulation.core/Runtime/Economy/FiniteSourceProductionKernel.cs',
        'Packages/com.simulation.core/Runtime/Economy/FiniteSourceProductionKernel.cs.meta',
        'Packages/com.simulation.core/Runtime/Economy/FiniteSourceScenarioRuntime.cs',
        'Packages/com.simulation.core/Runtime/Economy/FiniteSourceScenarioRuntime.cs.meta',
        'Packages/com.simulation.core/Runtime/Economy/MarketItemDefinition.cs',
        'Packages/com.simulation.core/Runtime/Economy/MarketItemDefinition.cs.meta',
        'Packages/com.simulation.core/Runtime/Economy/MarketPriceCalculator.cs',
        'Packages/com.simulation.core/Runtime/Economy/MarketPriceCalculator.cs.meta',
        'Packages/com.simulation.core/Runtime/Identity.meta',
        'Packages/com.simulation.core/Runtime/Identity/PersonId.cs',
        'Packages/com.simulation.core/Runtime/Identity/PersonId.cs.meta',
        'Packages/com.simulation.core/Runtime/Identity/StableKeyEncoding.cs',
        'Packages/com.simulation.core/Runtime/Identity/StableKeyEncoding.cs.meta',
        'Packages/com.simulation.core/Runtime/Numerics.meta',
        'Packages/com.simulation.core/Runtime/Numerics/UnityMathfCompatibility.cs',
        'Packages/com.simulation.core/Runtime/Numerics/UnityMathfCompatibility.cs.meta',
        'Packages/com.simulation.core/Runtime/Persistence.meta',
        'Packages/com.simulation.core/Runtime/Persistence/AtomicFileSlot.cs',
        'Packages/com.simulation.core/Runtime/Persistence/AtomicFileSlot.cs.meta',
        'Packages/com.simulation.core/Runtime/Persistence/ContinuationDescriptorV2.cs',
        'Packages/com.simulation.core/Runtime/Persistence/ContinuationDescriptorV2.cs.meta',
        'Packages/com.simulation.core/Runtime/Persistence/ContinuationWire.cs',
        'Packages/com.simulation.core/Runtime/Persistence/ContinuationWire.cs.meta',
        'Packages/com.simulation.core/Runtime/Persistence/NumericProfileIdentity.cs',
        'Packages/com.simulation.core/Runtime/Persistence/NumericProfileIdentity.cs.meta',
        'Packages/com.simulation.core/Runtime/Persistence/P18DCommercialSharingCodec.cs',
        'Packages/com.simulation.core/Runtime/Persistence/P18DCommercialSharingCodec.cs.meta',
        'Packages/com.simulation.core/Runtime/Persistence/P12ACompatibilityDescriptor.cs',
        'Packages/com.simulation.core/Runtime/Persistence/P12ACompatibilityDescriptor.cs.meta',
        'Packages/com.simulation.core/Runtime/Population.meta',
        'Packages/com.simulation.core/Runtime/Population/AggregateDemographyPolicyContracts.cs',
        'Packages/com.simulation.core/Runtime/Population/AggregateDemographyPolicyContracts.cs.meta',
        'Packages/com.simulation.core/Runtime/Population/DemographicSampling.cs',
        'Packages/com.simulation.core/Runtime/Population/DemographicSampling.cs.meta',
        'Packages/com.simulation.core/Runtime/Population/DeterministicAggregateDemographyProvider.cs',
        'Packages/com.simulation.core/Runtime/Population/DeterministicAggregateDemographyProvider.cs.meta',
        'Packages/com.simulation.core/Runtime/Simulation.Core.asmdef',
        'Packages/com.simulation.core/Runtime/Simulation.Core.asmdef.meta',
        'Packages/com.simulation.core/Runtime/Temporal.meta',
        'Packages/com.simulation.core/Runtime/Temporal/LogicalTimeline.cs',
        'Packages/com.simulation.core/Runtime/Temporal/LogicalTimeline.cs.meta',
        'Packages/com.simulation.core/package.json',
        'Packages/com.simulation.core/package.json.meta',
        'Tools/CoreValidation/CoreValidation.psm1',
        'Tools/CoreValidation/Invoke-CoreValidation.ps1',
        'Tools/CoreValidation/Invoke-LabValidation.ps1',
        'Tools/CoreValidation/README.md',
        'Tools/CoreValidation/tests/Invoke-CoreValidationTests.ps1',
        'Tools/UnityValidation/UnityValidationEvidence.psm1',
        'Tools/generate_daily_v1_captured_content_fixture.py',
        'docs/architecture/CORE_F_SELECTED_VALUE_MEMBERS.csv',
        'docs/validation/CORE_F_SharedRuntimeSource/JSON_PAYLOAD_ORACLE.csv',
        'docs/validation/CORE_F_SharedRuntimeSource/NUMERIC_ORACLE_VECTORS.csv',
        'docs/validation/CORE_F_SharedRuntimeSource/UNITY_ORACLE_CAPTURE_RESULTS_d8b4c40e.csv',
        'global.json',
        'src/Simulation.Core/Simulation.Core.csproj',
        'tests/Simulation.Core.Tests/DailyV1ContentProjectionTests.cs',
        'tests/Simulation.Core.Tests/DailyV1DefinitionIdentityTests.cs',
        'tests/Simulation.Core.Tests/DailyV1DefinitionProviderVectors.cs',
        'tests/Simulation.Core.Tests/DailyV1SelectedMemberCoverageTests.cs',
        'tests/Simulation.Core.Tests/DailyV1SelectedCrimeJusticeDailyStageTests.cs',
        'tests/Simulation.Core.Tests/DailyV1TypedDefinitionGraphTests.cs',
        'tests/Simulation.Core.Tests/Fixtures/CORE_F_WP1_VALUES_BASELINE.md',
        'tests/Simulation.Core.Tests/Fixtures/CORE_F_WP1_VALUES_BASELINE.txt',
        'tests/Simulation.Core.Tests/Simulation.Core.Tests.csproj',
        'tests/Simulation.Core.Tests/SimulationConfigurationResolverPrecedenceTests.cs',
        'tests/Simulation.Core.Tests/packages.lock.json',
        'tools/Simulation.Core.CalendarDemo/Program.cs',
        'tools/Simulation.Core.CalendarDemo/Simulation.Core.CalendarDemo.csproj',
        'tools/Simulation.Core.CalendarDemo/packages.lock.json',
        'tools/Simulation.Lab/Program.cs',
        'tools/Simulation.Lab/Simulation.Lab.csproj',
        'tools/Simulation.Lab/packages.lock.json'
    )

    $gitPointer = Join-Path $root '.git'
    if (Test-Path -LiteralPath $gitPointer -PathType Container) {
        $gitDirectory = $gitPointer
    } else {
        $pointerText = Get-Content -LiteralPath $gitPointer -Raw
        if ($pointerText -notmatch '(?m)^gitdir:\s*(.+?)\s*$') { throw 'Could not resolve the worktree Git directory.' }
        $gitDirectory = $Matches[1]
        if (-not [IO.Path]::IsPathRooted($gitDirectory)) { $gitDirectory = [IO.Path]::GetFullPath((Join-Path $root $gitDirectory)) }
    }
    $trackedConfig = @(& git "--git-dir=$gitDirectory" "--work-tree=$root" ls-files | Where-Object {
        $_ -match '(^|/)(Directory\.Build\.(props|targets)|Directory\.Packages\.props|NuGet\.Config|nuget\.config|[^/]+\.runsettings)$'
    })
    if ($LASTEXITCODE -ne 0) { throw 'Could not enumerate tracked build configuration inputs.' }
    $projectInputs = Get-CoreProjectCompileInputs -ProjectRoot $root -ProjectPaths $ProjectPaths
    $relativeInputs = @($fixedInputs + $trackedConfig + $projectInputs | Sort-Object -Unique)
    $entries = foreach ($relative in $relativeInputs) {
        $path = Join-Path $root ($relative -replace '/', [IO.Path]::DirectorySeparatorChar)
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Required validation input is missing: $relative" }
        $file = Get-Item -LiteralPath $path
        [pscustomobject]@{
            path = $relative.Replace('\', '/')
            bytes = [long]$file.Length
            sha256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    }
    return @($entries)
}

function Get-CoreInputDigest {
    [CmdletBinding()]
    param([Parameter(Mandatory)][object[]]$Snapshot)
    $json = ConvertTo-Json -InputObject @($Snapshot) -Depth 6 -Compress
    $bytes = [Text.Encoding]::UTF8.GetBytes($json)
    $hash = [Security.Cryptography.SHA256]::HashData($bytes)
    return [Convert]::ToHexString($hash).ToLowerInvariant()
}

function Compare-CoreInputSnapshots {
    [CmdletBinding()]
    param([Parameter(Mandatory)][object[]]$Before, [Parameter(Mandatory)][object[]]$After)
    $beforeJson = ConvertTo-Json -InputObject @($Before) -Depth 6 -Compress
    $afterJson = ConvertTo-Json -InputObject @($After) -Depth 6 -Compress
    $beforeMap = @{}
    foreach ($item in $Before) { $beforeMap[[string]$item.path] = "$($item.bytes):$($item.sha256)" }
    $afterMap = @{}
    foreach ($item in $After) { $afterMap[[string]$item.path] = "$($item.bytes):$($item.sha256)" }
    $changed = @()
    foreach ($path in @($beforeMap.Keys + $afterMap.Keys | Sort-Object -Unique)) {
        if ($beforeMap[$path] -cne $afterMap[$path]) { $changed += $path }
    }
    return [pscustomobject]@{
        stable = $beforeJson -ceq $afterJson
        beforeDigest = Get-CoreInputDigest -Snapshot $Before
        afterDigest = Get-CoreInputDigest -Snapshot $After
        changedPaths = $changed
    }
}

function New-CoreHostTraceRunSettings {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$TraceFilePath,
        [Parameter(Mandatory)][string]$SdkVersion
    )

    if ($SdkVersion -notmatch '^(?<major>\d+)\.') { throw 'Pinned SDK version is not a valid semantic version.' }
    $tracePrefix = if ([int]$Matches.major -ge 10) { 'DOTNET_HOST' } else { 'COREHOST' }
    $settingsPath = [IO.Path]::GetFullPath($Path)
    $tracePath = [IO.Path]::GetFullPath($TraceFilePath)
    $document = [Xml.XmlDocument]::new()
    $runSettings = $document.CreateElement('RunSettings')
    $null = $document.AppendChild($runSettings)
    $runConfiguration = $document.CreateElement('RunConfiguration')
    $null = $runSettings.AppendChild($runConfiguration)
    $environmentVariables = $document.CreateElement('EnvironmentVariables')
    $null = $runConfiguration.AppendChild($environmentVariables)
    foreach ($entry in @(
        @{ Name = $tracePrefix + '_TRACE'; Value = '1' },
        @{ Name = $tracePrefix + '_TRACEFILE'; Value = $tracePath },
        @{ Name = $tracePrefix + '_TRACE_VERBOSITY'; Value = '4' }
    )) {
        $element = $document.CreateElement($entry.Name)
        $element.InnerText = $entry.Value
        $null = $environmentVariables.AppendChild($element)
    }
    [IO.File]::WriteAllText($settingsPath, $document.OuterXml, [Text.UTF8Encoding]::new($false))
    $bytes = [IO.File]::ReadAllBytes($settingsPath)
    return [pscustomobject]@{
        path = $settingsPath
        bytes = [long]$bytes.LongLength
        sha256 = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
        tracePrefix = $tracePrefix
        traceFilePath = $tracePath
    }
}

function Get-CoreRuntimeSelectionTrace {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$TraceDirectory,
        [Parameter(Mandatory)][string]$TestAssemblyPath
    )

    if (-not (Test-Path -LiteralPath $TraceDirectory -PathType Container)) { throw 'Runtime trace directory is missing.' }
    $testAssembly = [IO.Path]::GetFullPath($TestAssemblyPath)
    if ($testAssembly -notmatch '(?i)\.dll$') { throw 'Runtime trace binding requires a test assembly DLL path.' }
    $expectedConfig = [regex]::Replace($testAssembly, '(?i)\.dll$', '.runtimeconfig.json')
    $expectedDevConfig = [regex]::Replace($testAssembly, '(?i)\.dll$', '.runtimeconfig.dev.json')
    $traceFiles = @(Get-ChildItem -LiteralPath $TraceDirectory -Filter '*.log' -File)
    if ($traceFiles.Count -eq 0) { throw 'Runtime trace directory contains no process traces.' }

    # Host trace filenames depend on how the SDK launches a process (for
    # example, testhost.exe versus dotnet.exe hosting testhost.dll). Bind by
    # the test assembly's exact runtime-config paths instead of the process
    # filename, and require exactly one matching process trace.
    $boundTraces = [System.Collections.Generic.List[object]]::new()
    foreach ($candidateTrace in $traceFiles) {
        $candidateBytes = [IO.File]::ReadAllBytes($candidateTrace.FullName)
        $candidateText = [Text.UTF8Encoding]::new($false, $true).GetString($candidateBytes)
        $candidateLines = @([regex]::Split($candidateText, '\r?\n'))
        $candidateConfigHits = [System.Collections.Generic.List[object]]::new()
        for ($lineIndex = 0; $lineIndex -lt $candidateLines.Count; $lineIndex++) {
            if ($candidateLines[$lineIndex] -match '(?i)^\s*Runtime config is cfg=(?<cfg>.+\.runtimeconfig\.json)\s+dev=(?<dev>.+\.runtimeconfig\.dev\.json)\s*$') {
                $candidateConfigHits.Add([pscustomobject]@{ Line = $lineIndex; ConfigPath = $Matches.cfg.Trim(); DevPath = $Matches.dev.Trim() })
            }
        }
        $candidateTestConfigHits = @($candidateConfigHits | Where-Object {
            [IO.Path]::IsPathRooted($_.ConfigPath) -and [IO.Path]::IsPathRooted($_.DevPath) -and
            [string]::Equals([IO.Path]::GetFullPath($_.ConfigPath), $expectedConfig, [StringComparison]::OrdinalIgnoreCase)
        })
        if ($candidateTestConfigHits.Count -gt 1) {
            throw 'A process trace contains multiple runtimeconfig selection blocks for the test assembly.'
        }
        if ($candidateTestConfigHits.Count -eq 0) { continue }

        $selectedConfig = $candidateTestConfigHits[0]
        if (-not [string]::Equals([IO.Path]::GetFullPath($selectedConfig.DevPath), $expectedDevConfig, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'The test assembly runtimeconfig.dev.json does not match its runtimeconfig.json.'
        }
        $boundTraces.Add([pscustomobject]@{
            File = $candidateTrace
            Bytes = $candidateBytes
            Lines = $candidateLines
            ConfigHits = $candidateConfigHits
            SelectedConfig = $selectedConfig
        })
    }
    if ($boundTraces.Count -ne 1) {
        throw 'Exactly one process trace must bind to the test assembly runtimeconfig.'
    }
    $boundTrace = $boundTraces[0]
    $traceFile = $boundTrace.File
    $bytes = [byte[]]$boundTrace.Bytes
    $lines = [string[]]$boundTrace.Lines
    $selectedConfig = $boundTrace.SelectedConfig

    $nextConfig = $lines.Count
    for ($lineIndex = [int]$selectedConfig.Line + 1; $lineIndex -lt $lines.Count; $lineIndex++) {
        if ($lines[$lineIndex] -match '(?i)^\s*Runtime config is cfg=') { $nextConfig = $lineIndex; break }
    }
    $chosen = [System.Collections.Generic.List[string]]::new()
    for ($lineIndex = [int]$selectedConfig.Line + 1; $lineIndex -lt $nextConfig; $lineIndex++) {
        if ($lines[$lineIndex] -match '(?i)^\s*Chose FX version \[(?<path>[^\]\r\n]+)\]\s*$') { $chosen.Add($Matches.path) }
    }
    $runtimeMatch = if ($chosen.Count -eq 1) { [regex]::Match($chosen[0], '(?i)(?:^|[\\/])shared[\\/]Microsoft\.NETCore\.App[\\/](?<version>\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?)$') } else { [regex]::Match('', 'a^') }
    if ($chosen.Count -ne 1 -or -not $runtimeMatch.Success) {
        throw 'The process trace does not prove exactly one selected Microsoft.NETCore.App CLR for the bound test assembly.'
    }

    return [pscustomobject]@{
        path = $traceFile.FullName
        fileName = $traceFile.Name
        bytes = [long]$bytes.LongLength
        sha256 = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
        selectedClr = $runtimeMatch.Groups['version'].Value
    }
}

function Get-CoreTrxAssessment {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][AllowEmptyCollection()][string[]]$ExpectedTests
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw 'TRX report is missing.' }
    if ($ExpectedTests.Count -eq 0) { throw 'Expected test discovery is empty.' }

    $settings = [System.Xml.XmlReaderSettings]::new()
    $settings.DtdProcessing = [System.Xml.DtdProcessing]::Prohibit
    $settings.XmlResolver = $null
    $document = [System.Xml.XmlDocument]::new()
    $document.XmlResolver = $null
    $reader = [System.Xml.XmlReader]::Create((Resolve-Path -LiteralPath $Path).Path, $settings)
    try { $document.Load($reader) } finally { $reader.Dispose() }

    $run = $document.DocumentElement
    if ($null -eq $run -or $run.LocalName -cne 'TestRun') { throw 'TRX root is not TestRun.' }
    $runId = $run.GetAttribute('id')
    if ([string]::IsNullOrWhiteSpace($runId)) { throw 'TRX run identity is missing.' }
    $definitionsNode = $run.SelectSingleNode("*[local-name()='TestDefinitions']")
    $entriesNode = $run.SelectSingleNode("*[local-name()='TestEntries']")
    $resultsNode = $run.SelectSingleNode("*[local-name()='Results']")
    $summaryNode = $run.SelectSingleNode("*[local-name()='ResultSummary']")
    if ($null -eq $definitionsNode -or $null -eq $entriesNode -or $null -eq $resultsNode -or $null -eq $summaryNode) {
        throw 'TRX is missing required run-local sections.'
    }

    $definitions = @{}
    foreach ($node in $definitionsNode.SelectNodes("*[local-name()='UnitTest']")) {
        $id = $node.GetAttribute('id')
        $name = $node.GetAttribute('name')
        if ([string]::IsNullOrWhiteSpace($id) -or [string]::IsNullOrWhiteSpace($name)) { throw 'TRX contains a test definition without identity.' }
        if ($definitions.ContainsKey($id)) { throw "TRX contains duplicate test definition id '$id'." }
        $method = $node.SelectSingleNode(".//*[local-name()='TestMethod']")
        if ($null -eq $method -or [string]::IsNullOrWhiteSpace($method.GetAttribute('name'))) { throw "TRX definition '$id' lacks method identity." }
        $definitions[$id] = [pscustomobject]@{ name = $name; methodName = $method.GetAttribute('name'); className = $method.GetAttribute('className') }
    }
    if ($definitions.Count -eq 0) { throw 'TRX contains no test definitions.' }

    $entries = @{}
    foreach ($node in $entriesNode.SelectNodes("*[local-name()='TestEntry']")) {
        $testId = $node.GetAttribute('testId')
        $executionId = $node.GetAttribute('executionId')
        if (-not $definitions.ContainsKey($testId)) { throw "TRX entry references unknown test id '$testId'." }
        if ([string]::IsNullOrWhiteSpace($executionId)) { throw 'TRX entry lacks execution identity.' }
        $key = "$testId/$executionId"
        if ($entries.ContainsKey($key)) { throw "TRX contains duplicate test entry '$key'." }
        $entries[$key] = $true
    }
    if ($entries.Count -eq 0) { throw 'TRX contains no test entries.' }

    $results = [System.Collections.Generic.List[object]]::new()
    $resultKeys = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($node in $resultsNode.SelectNodes("*[local-name()='UnitTestResult']")) {
        $testId = $node.GetAttribute('testId')
        $executionId = $node.GetAttribute('executionId')
        $testName = $node.GetAttribute('testName')
        $outcome = $node.GetAttribute('outcome')
        if (-not $definitions.ContainsKey($testId)) { throw "TRX result references unknown test id '$testId'." }
        $key = "$testId/$executionId"
        if (-not $entries.ContainsKey($key)) { throw "TRX result '$key' has no matching test entry." }
        if (-not $resultKeys.Add($key)) { throw "TRX contains duplicate result '$key'." }
        if ([string]::IsNullOrWhiteSpace($testName) -or [string]::IsNullOrWhiteSpace($outcome)) { throw 'TRX result lacks case identity or outcome.' }
        $reasonNode = $node.SelectSingleNode(".//*[local-name()='Message']")
        $stackNode = $node.SelectSingleNode(".//*[local-name()='StackTrace']")
        $stdoutNode = $node.SelectSingleNode(".//*[local-name()='StdOut']")
        $stderrNode = $node.SelectSingleNode(".//*[local-name()='StdErr']")
        $results.Add([pscustomobject]@{
            testId = $testId
            executionId = $executionId
            definitionName = $definitions[$testId].name
            definitionMethod = $definitions[$testId].methodName
            definitionClass = $definitions[$testId].className
            testName = $testName
            outcome = $outcome
            duration = $node.GetAttribute('duration')
            reason = if ($null -eq $reasonNode) { '' } else { $reasonNode.InnerText }
            error = if ($null -eq $stackNode) { '' } else { $stackNode.InnerText }
            stdout = if ($null -eq $stdoutNode) { '' } else { $stdoutNode.InnerText }
            stderr = if ($null -eq $stderrNode) { '' } else { $stderrNode.InnerText }
        })
    }
    if ($results.Count -eq 0) { throw 'TRX contains no executed result records.' }
    if ($results.Count -ne $entries.Count) { throw 'TRX result and test-entry counts differ.' }

    $actualNames = @($results | ForEach-Object { $_.testName } | Sort-Object)
    $expectedNames = @($ExpectedTests | Sort-Object)
    if (($actualNames -join "`n") -cne ($expectedNames -join "`n")) { throw 'TRX per-case identity multiset differs from discovered expected tests.' }

    $countersNode = $summaryNode.SelectSingleNode("*[local-name()='Counters']")
    if ($null -eq $countersNode) { throw 'TRX summary lacks counters.' }
    $summaryOutcome = $summaryNode.GetAttribute('outcome')
    if ([string]::IsNullOrWhiteSpace($summaryOutcome)) { throw 'TRX summary outcome is missing.' }
    if ($summaryOutcome -cne 'Completed') { throw "TRX summary outcome must be 'Completed', found '$summaryOutcome'." }
    $counterNames = @('total','executed','passed','failed','error','timeout','aborted','inconclusive','notExecuted')
    $counters = @{}
    foreach ($name in $counterNames) {
        $raw = $countersNode.GetAttribute($name)
        [long]$parsed = 0
        if ([string]::IsNullOrWhiteSpace($raw) -or -not [long]::TryParse($raw, [Globalization.NumberStyles]::Integer, [Globalization.CultureInfo]::InvariantCulture, [ref]$parsed)) {
            throw "TRX summary counter '$name' is missing or invalid."
        }
        $counters[$name] = $parsed
    }
    if ($counters.total -ne $results.Count) { throw 'TRX total counter does not match result records.' }
    if (($counters.executed + $counters.notExecuted) -ne $counters.total) { throw 'TRX executed/notExecuted counters are incoherent.' }

    $outcomeCounts = @{}
    foreach ($result in $results) {
        if (-not $outcomeCounts.ContainsKey($result.outcome)) { $outcomeCounts[$result.outcome] = 0L }
        $outcomeCounts[$result.outcome]++
    }
    $expectedCounters = @{
        passed = if ($outcomeCounts.ContainsKey('Passed')) { $outcomeCounts.Passed } else { 0L }
        failed = if ($outcomeCounts.ContainsKey('Failed')) { $outcomeCounts.Failed } else { 0L }
        error = if ($outcomeCounts.ContainsKey('Error')) { $outcomeCounts.Error } else { 0L }
        timeout = if ($outcomeCounts.ContainsKey('Timeout')) { $outcomeCounts.Timeout } else { 0L }
        aborted = if ($outcomeCounts.ContainsKey('Aborted')) { $outcomeCounts.Aborted } else { 0L }
        inconclusive = if ($outcomeCounts.ContainsKey('Inconclusive')) { $outcomeCounts.Inconclusive } else { 0L }
        notExecuted = if ($outcomeCounts.ContainsKey('NotExecuted')) { $outcomeCounts.NotExecuted } else { 0L }
    }
    foreach ($name in $expectedCounters.Keys) {
        if ($counters[$name] -ne $expectedCounters[$name]) { throw "TRX '$name' counter does not match outcomes." }
    }
    $knownOutcomes = @('Passed','Failed','Error','Timeout','Aborted','Inconclusive','NotExecuted')
    foreach ($outcome in $outcomeCounts.Keys) { if ($outcome -cnotin $knownOutcomes) { throw "Unknown TRX outcome '$outcome'." } }

    $nonPassed = @($results | Where-Object { $_.outcome -cne 'Passed' }).Count
    return [pscustomobject]@{
        runId = $runId
        runName = $run.GetAttribute('name')
        summaryOutcome = $summaryOutcome
        counters = $counters
        resultCount = $results.Count
        gatePassed = ($nonPassed -eq 0 -and $counters.failed -eq 0 -and $counters.error -eq 0 -and $counters.timeout -eq 0 -and $counters.aborted -eq 0 -and $counters.inconclusive -eq 0 -and $counters.notExecuted -eq 0)
        results = @($results.ToArray())
    }
}

Export-ModuleMember -Function Get-CoreProjectCompileInputs, Get-CoreInputSnapshot, Get-CoreInputDigest, Compare-CoreInputSnapshots, New-CoreHostTraceRunSettings, Get-CoreRuntimeSelectionTrace, Get-CoreTrxAssessment
