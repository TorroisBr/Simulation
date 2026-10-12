# Validation Evidence Tool

The standalone validation evidence packager is part of the canonical repository under `Tools/UnityValidation`.

## Capability and authority

- `Invoke-UnityValidation.ps1` remains the result authority and retains its existing timeout, retry, and process-ownership behavior.
- `Invoke-UnityValidationWithEvidence.ps1` captures the validation invocation, exact repository source identity, complete validation-input inventory, per-attempt gate facts, and protected local diagnostic originals.
- `Package-UnityValidationEvidence.ps1` creates and verifies a deterministic manifest plus result-only ZIP. Verification checks archive integrity and binds reported results and gate facts to the XML in the package.
- New authorized validation candidates should use the wrapper and packager. Historical evidence remains unchanged and is not retroactively repackaged.

## Tool and package locator

Tool entry points:

- `Tools/UnityValidation/Invoke-UnityValidationWithEvidence.ps1`
- `Tools/UnityValidation/Package-UnityValidationEvidence.ps1`
- `Tools/UnityValidation/UnityValidationEvidence.psm1`

Each run's private capture manifest is written under `%LOCALAPPDATA%\Simulation\ValidationEvidence`. To create a public package, choose a per-run output directory and bundle ID, for example:

```powershell
$runManifest = .\Tools\UnityValidation\Invoke-UnityValidationWithEvidence.ps1 `
    -ProjectPath . `
    -Mode EditMode

.\Tools\UnityValidation\Package-UnityValidationEvidence.ps1 `
    -Action Package `
    -RunManifestPath $runManifest `
    -OutputDirectory .\artifacts\validation `
    -BundleId p12-editmode-final
```

The command prints the resulting manifest and archive paths. The caller records those exact paths as the package locator for the candidate's validation record. Package output is local and caller-selected; the tool does not upload, publish, register, or delete evidence.

Use `-Action Verify` with the printed manifest and archive paths before relying on a package. Follow the tool README for output-dependent claims, runtime pinning, retention, and safe handling of original diagnostics.
