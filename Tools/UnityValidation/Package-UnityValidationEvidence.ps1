[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('Package', 'Verify', 'Read')][string]$Action,
    [string[]]$RunManifestPath,
    [string]$OutputDirectory,
    [string]$ManifestPath,
    [string]$ArchivePath,
    [string[]]$VerifyRunManifestPath,
    [string]$BundleId,
    [ValidateSet('UnityNUnit', 'DotNetTrx')][string]$ReportFormat = 'UnityNUnit',
    [switch]$ClaimRequiresOutput
)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'UnityValidationEvidence.psm1') -Force
Import-Module (Join-Path $PSScriptRoot 'TrxValidationEvidence.psm1') -Force
try {
    switch ($Action) {
        'Package' {
            if (-not $RunManifestPath -or [string]::IsNullOrWhiteSpace($OutputDirectory)) { throw 'Package requires -RunManifestPath and -OutputDirectory.' }
            if ($ReportFormat -eq 'DotNetTrx') {
                $trxArguments = @{ SourceContextPath = $RunManifestPath; OutputDirectory = $OutputDirectory }
                if (-not [string]::IsNullOrWhiteSpace($BundleId)) { $trxArguments.BundleId = $BundleId }
                $result = New-DotNetTrxEvidencePackage @trxArguments
                Write-Output "Manifest: $($result.ManifestPath)"
                Write-Output "Archive: $($result.ArchivePath)"
                Write-Output "Archive SHA-256: $($result.ArchiveSha256)"
                Write-Output "Archive bytes: $($result.ArchiveBytes)"
                break
            }
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
            $format = Get-EvidencePackageReportFormat -ManifestPath $ManifestPath
            if ($PSBoundParameters.ContainsKey('ReportFormat') -and $ReportFormat -ne $format) { throw 'The requested report format conflicts with the declared package schema.' }
            if ($format -eq 'DotNetTrx') {
                $trxResult = Test-DotNetTrxEvidencePackage -ManifestPath $ManifestPath -ArchivePath $ArchivePath -SourceContextPath $VerifyRunManifestPath
                if (-not $trxResult.IsValid) { throw $trxResult.Error }
                Write-Output "VALID: runs=$($trxResult.RunCount) entries=$($trxResult.EntryCount) archiveSha256=$($trxResult.ArchiveSha256) originalComparison=$($trxResult.OriginalComparison)"
                break
            }
            $result = Test-UnityValidationEvidencePackage -ManifestPath $ManifestPath -ArchivePath $ArchivePath -RunManifestPath $VerifyRunManifestPath
            if (-not $result.IsValid) { throw $result.Error }
            Write-Output "VALID: runs=$($result.RunCount) entries=$($result.EntryCount) archiveSha256=$($result.ArchiveSha256)"
        }
        'Read' {
            if ([string]::IsNullOrWhiteSpace($ManifestPath)) { throw 'Read requires -ManifestPath.' }
            $format = Get-EvidencePackageReportFormat -ManifestPath $ManifestPath
            if ($PSBoundParameters.ContainsKey('ReportFormat') -and $ReportFormat -ne $format) { throw 'The requested report format conflicts with the declared package schema.' }
            if ($format -eq 'DotNetTrx') { Read-DotNetTrxEvidencePackage -ManifestPath $ManifestPath | Format-List; break }
            Read-UnityValidationEvidencePackage -ManifestPath $ManifestPath | Format-List
        }
    }
}
catch {
    Write-Error $_
    exit 1
}
