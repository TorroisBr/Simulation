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

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'UnityValidationEvidence.psm1') -Force
try {
    $run = Invoke-UnityValidationEvidenceCapture @PSBoundParameters
    Write-Host "RunId: $($run.RunId)"
    Write-Host "Status: $($run.Status)"
    Write-Host "Private run manifest: $($run.ManifestPath)"
    Write-Output $run.ManifestPath
}
catch {
    Write-Error $_
    exit 1
}
