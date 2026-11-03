# CORE-F Selected Runtime Publication and Admission Review

## Verdict

**VALIDATED_CANDIDATE** - bounded exact-tip review only.** This review applies to candidate `e7da26a3ee8ececaf3a0f31f81235e72c4d75fbf` against base `ff3852fd3a283a2ad2bcba8fe29d3660908e397f`. It does not authorize canonical promotion or claim full CORE-F completion.

## Review identities

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Reviewed candidate: `e7da26a3ee8ececaf3a0f31f81235e72c4d75fbf`
- Candidate parent: `e24d121e5fbd1930f9461a4e75152de5c168d9e0`
- Original implementation base: `ff3852fd3a283a2ad2bcba8fe29d3660908e397f`
- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Review branch: `codex/review/coref-runtime-admission-e7da26a`

## Scope and findings

The reviewed boundary removes Unity's duplicated factual-publication readback. Core reads `IsFactualReadWorldPublished` from the exact `DailyV1ExecutionBoundaryState` already bound to the selected session; Unity's existing publication timing remains unchanged.

DailyAdvance admission is split deliberately. The Unity host confirms the complete-census protocol exists, remains healthy, is bound to the current owner thread, and reports the same active runtime-operation count as Core. The Core execution state then atomically requires that count to be zero, validates the sealed expected DailyAdvance contract and owner thread, and registers the operation under its operation lock. This preserves rejection of a locally faulted or unavailable host census while making the idle decision atomic in Core. Ordinary P12 protocol operations continue to use the nested-capable admission overload after DailyAdvance starts.

Regression coverage checks host-census-not-ready rejection, Core rejection of a second DailyAdvance while a registered runtime operation is active, and application-level overlap rejection before clock advancement. The application regression verifies day remains zero, the held operation remains registered until its owner closes it, no selected execution boundary is requested, and the application does not fault. Restore fixtures mark the exact Core state published after creating the restored boundary token, matching production restore finalization. No test assertion was weakened.

No census provider, owner-section writer, mutation epoch, reservation, or P12 owner authority moved in this slice. The 89-family complete-vector contract remains in force. `DailyV1Application.TryStart`, detached restoration, the .NET host, and CORE-F closure remain incomplete.

## Source identity and validation

Recorded source SHA-256 values match the exact Core and Unity run-manifest input inventories:

| Source file | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs` | `ac84c6d680fed0a9937aacaa4ed985549fb3cc93e438f14c2c167930e0b9bf21` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs` | `1ee817832ea3a63bbdca981e0a9f77f6a9c754ccdfdce3bee446d8b289927e89` |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `146f23bb141da9a461b6abd2db744d4fa005fe09938187c2fd5e0ccd852d1064` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs` | `ebbf557e76bb53af9f33e19b2c5d35d1a979392d4356e2a5d6adf0b83dbe84a2` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeOrchestrationTests.cs` | `f22ece63a84a96490e006a2764ca18bd593f2e2ff784787780157ac685489306` |

- CoreValidation input digest: `0a21b6e61363fb05cc3ba1ef10669031977188a7034e3abfcb3e5d1672ed3f60`.
- Unity input digest: `4591637f835c2b96bf29f27c47b5171d6d623b3ce072be097f6a1b4a587f0cc2`; inputs were stable before and after.
- Official CoreValidation run `f205a3c6ec8d4bb58913c83b0eac2226`, pinned SDK `9.0.315`: cold `999/999` and warm `999/999`, no failures. This was the documented `-SkipLab` profile; no Lab-validation claim is made.
- Official Unity EditMode run `98c98f9660754a9296f018b46bdc4a3c`: `3296/3296`, zero failures, skips, or inconclusive results, Unity `6000.3.9f1`.
- Official filtered Smoke run `df2b7400f8ee4eb19e6154a2d7c848f5`: `5/5`, zero failures, skips, or inconclusive results, on the same Unity input digest.
- `git show --check` passed.

## Evidence integrity and limitation

The current privacy-safe Unity package is `coref-selected-runtime-admission-readiness-full-20261103.manifest.json` with archive `coref-selected-runtime-admission-readiness-full-20261103.zip`. Manifest SHA-256 is `4ddf3c3a8f83ae2e622be842952ddcc5d4ab3a8bded67a4834629235d2ff6c72`; archive SHA-256 is `bfbd8d9df9c300e48816cabd435f07a8774b9510cb16b5c386a3539e39c7172c`. The package verifier record reports `runs=1 entries=1`; the archive's single normalized XML matches its recorded byte count and hash. The packaged manifest and report contain no workstation paths, user or host identifiers, or credential-like values. Earlier publication-only and admission packages are explicitly marked historical; the readiness package is the one applicable to this candidate.

The CoreValidation private manifest SHA-256 is `02c0c483e69b6452ebe8549e1f0bb64bbc73728ccef9a43bd6d9e9fd8b191c06`; cold TRX SHA-256 is `59bb89272d6ec0d8209cb22d4857a715e7033b31977fe9ec9703ef916e7dc0cd`; warm TRX SHA-256 is `074002ae305f8b569700f3c9621a6efd1bc0e6f00189ffe0f5926c5d864867ad`. These originals remain private and unchanged. The approved `DotNetTrx` adapter rejects this runner's manifest format; no unsupported context was fabricated and no raw .NET TRX was published. A durable public .NET evidence package is still required before canonical promotion.

## Status

This exact candidate has no outstanding implementation-review findings for the bounded publication/admission change. It remains an intermediate candidate only. P12 capture/restoration, `TryStart`, the detached .NET host, separate-process continuation, and CORE-F closure remain open.
