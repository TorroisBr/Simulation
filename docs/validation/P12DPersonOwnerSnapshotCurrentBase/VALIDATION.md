# P12-D Person owner snapshot focused validation

**Result:** PASS — focused Person EditMode filter, 182/182 tests.

## Candidate

- Branch: `codex/phase12/P12DPersonOwnerSnapshotCurrentBase`
- Exact code tip: `6c872c5ca6e997844d01c18bfee8909c16317455`
- Candidate `Assets` tree: `b84164f70736c1d5c7643e107f06178798238ca0`
- Current P12 canonical baseline: `da2a73896bc405ae6f11c536a5fbe8d471b00c21`
- The candidate includes the canonical docs-only correction as an additive merge; no published candidate history was rewritten.
- Changed code/test files: `PersonRuntime.cs`, `PersonStore.cs`, `PersonOwnerSnapshotTests.cs`, and its Unity `.meta` file.

## Contract verified

`PersonStoreOwnerSnapshot` carries schema ID `p12d-person-store-owner` and
schema version `1`, including for an empty owner. Staging rejects an unknown or
missing schema ID and an unsupported schema version. The schema ID is distinct
from P12-B census section IDs.

The focused owner tests also cover exact detached values/order, derived index
reconstruction, revision preservation, malformed local rows, and no partial
staged publication. They do not claim the deferred merged-D NPC, City, or
absolute-day checks.

## Validation

Command:

```powershell
Tools/UnityValidation/Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter Person -ResultsDirectory Temp/ValidationResults/P12DPersonOwnerSnapshotCurrentBaseFocused -TimeoutMinutes 20
```

Unity reported `Passed` with 182 passed, 0 failed, and 0 skipped. The command
`git diff --check da2a73896bc405ae6f11c536a5fbe8d471b00c21..6c872c5ca6e997844d01c18bfee8909c16317455`
passed.

## Raw artifacts

- `Raw/PersonFocused.xml` — SHA-256 `9a234c5b8710a58f7c3ebad2e822961a225d5c22c34e536f9d5c8f2b04b9c095`
- `Raw/PersonFocused.log.gz` — lossless GZip artifact SHA-256 `f8f4ce9884701a06db2720a514c1631774e7822356a17deb922807805eae8162`; decompressed raw log SHA-256 `3a1050b15b54f7116c661bef76e6850254a6e516fafd2fee0b6983a28d0d54c6`

This is focused implementation validation only. ALL EditMode, official Smoke,
whole-graph integration, P12-A readiness, and canonical promotion remain
outside this result.
