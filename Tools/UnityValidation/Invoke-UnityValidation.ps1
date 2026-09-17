[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectPath,

    [ValidateSet('EditMode', 'PlayMode')]
    [string]$Mode = 'EditMode',

    [switch]$All,

    [string]$TestFilter,

    [string]$TestCategory,

    [string]$UnityPath,

    [ValidateRange(1, 1440)]
    [int]$TimeoutMinutes = 30,

    [string]$ResultsDirectory,

    [switch]$RetryOperationalFailure,

    [switch]$CleanOldResults
)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'UnityValidation.psm1') -Force

if ($All -and (-not [string]::IsNullOrWhiteSpace($TestFilter) -or -not [string]::IsNullOrWhiteSpace($TestCategory))) {
    Write-Error '-All cannot be combined with -TestFilter or -TestCategory.'
    exit 1
}

$result = Invoke-UnityValidation -ProjectPath $ProjectPath -Mode $Mode -UnityPath $UnityPath -TestFilter $TestFilter -TestCategory $TestCategory -TimeoutMinutes $TimeoutMinutes -ResultsDirectory $ResultsDirectory -RetryOperationalFailure:$RetryOperationalFailure -CleanOldResults:$CleanOldResults
Write-Output (Format-UnityValidationSummary -Result $result)

if ($result.Status -eq 'Passed') {
    exit 0
}

exit 1
