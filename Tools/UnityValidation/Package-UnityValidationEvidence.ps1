[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('Package', 'Verify', 'Read')][string]$Action,
    [string[]]$RunManifestPath,
    [string]$OutputDirectory,
    [string]$ManifestPath,
    [string]$ArchivePath,
    [string[]]$VerifyRunManifestPath,
    [string]$BundleId,
    [switch]$ClaimRequiresOutput
)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'UnityValidationEvidence.psm1') -Force
try {
    switch ($Action) {
        'Package' {
            if (-not $RunManifestPath -or [string]::IsNullOrWhiteSpace($OutputDirectory)) { throw 'Package requires -RunManifestPath and -OutputDirectory.' }
            $arguments = @{ RunManifestPath = $RunManifestPath; OutputDirectory = $OutputDirectory; ClaimRequiresOutput = $ClaimRequiresOutput }
            if (-not [string]::IsNullOrWhiteSpace($BundleId)) { $arguments.BundleId = $BundleId }
            $result = New-UnityValidationEvidencePackage @arguments
            Write-Output "Manifest: $($result.ManifestPath)"
            Write-Output "Archive: $($result.ArchivePath)"
            Write-Output "Archive SHA-256: $($result.ArchiveSha256)"
            Write-Output "Archive bytes: $($result.ArchiveBytes)"
        }
        'Verify' {
            if ([string]::IsNullOrWhiteSpace($ManifestPath) -or [string]::IsNullOrWhiteSpace($ArchivePath)) { throw 'Verify requires -ManifestPath and -ArchivePath.' }
            $result = Test-UnityValidationEvidencePackage -ManifestPath $ManifestPath -ArchivePath $ArchivePath -RunManifestPath $VerifyRunManifestPath
            if (-not $result.IsValid) { throw $result.Error }
            Write-Output "VALID: runs=$($result.RunCount) entries=$($result.EntryCount) archiveSha256=$($result.ArchiveSha256)"
        }
        'Read' {
            if ([string]::IsNullOrWhiteSpace($ManifestPath)) { throw 'Read requires -ManifestPath.' }
            Read-UnityValidationEvidencePackage -ManifestPath $ManifestPath | Format-List
        }
    }
}
catch {
    Write-Error $_
    exit 1
}
