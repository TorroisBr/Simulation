# CORE-F selected runtime publication/admission boundary

## Scope

This candidate makes two adjacent Core-owned boundary corrections:

- Factual-read publication checks use the exact `DailyV1ExecutionBoundaryState`
  already bound to the admitted session. Unity's publication timing and setter
  are unchanged; the redundant host readback was removed.
- Initial DailyAdvance admission now has two explicit parts. Unity verifies
  that its still-required complete-census protocol is present, healthy, bound
  to the current owner thread, and reports the same active-operation count as
  Core. Core then checks that the count is zero and registers DailyAdvance
  under one lock. This preserves the host protocol fault gate while making the
  no-nested-advance decision atomic in the Core execution state; ordinary P12
  protocol operations remain nestable after the outer advance has begun.

The host still owns full owner-section census capture, the compatibility
operation-context check, mutation-guard binding, and restore-candidate census.
No census writer/provider, mutation-epoch, reservation, or owner-section
authority moved. All 89 P12 family rows remain subject to the existing complete
vector protocol. This does not make `DailyV1Application.TryStart`
implementation-ready, complete detached restoration, or complete CORE-F.

The Core regressions cover both rejection boundaries: a host census protocol
reported as unavailable cannot open DailyAdvance, and a registered Core runtime
operation prevents the Core boundary from opening a second DailyAdvance before
requesting selected execution. A separate application-level regression
confirms overlapping work leaves the day unchanged. Held operations are closed
and the registry is asserted to return to zero. Existing restore fixtures mark
their exact Core execution states published after restored-token creation,
matching production finalization. No assertion was weakened.

## Source identity

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Base at validation: `e24d121e5fbd1930f9461a4e75152de5c168d9e0`
- Committed tree at validation: `949c14e314db2b9f3858aaca153037cd22af78f6`
- CoreValidation input digest: `0a21b6e61363fb05cc3ba1ef10669031977188a7034e3abfcb3e5d1672ed3f60`
- Unity input digest: `4591637f835c2b96bf29f27c47b5171d6d623b3ce072be097f6a1b4a587f0cc2`
- Both official runners reported stable before/after inputs. Their private
  manifests bind the applicable source hashes below.

| Source file | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs` | `ac84c6d680fed0a9937aacaa4ed985549fb3cc93e438f14c2c167930e0b9bf21` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs` | `1ee817832ea3a63bbdca981e0a9f77f6a9c754ccdfdce3bee446d8b289927e89` |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `146f23bb141da9a461b6abd2db744d4fa005fe09938187c2fd5e0ccd852d1064` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs` | `ebbf557e76bb53af9f33e19b2c5d35d1a979392d4356e2a5d6adf0b83dbe84a2` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeOrchestrationTests.cs` | `f22ece63a84a96490e006a2764ca18bd593f2e2ff784787780157ac685489306` |

The three existing local `ProjectSettings` modifications and unrelated
untracked files were preserved and not staged. Unity's private input inventory
records the `ProjectSettings` bytes used by both Unity runs; they are not
claimed as part of the candidate source changes.

## Validation

- Official CoreValidation run `f205a3c6ec8d4bb58913c83b0eac2226`, pinned SDK
  `9.0.315`: cold `999/999`, warm `999/999`; build and locked restore passed.
  This was the documented `-SkipLab` profile; no Lab-validation claim is made.
- Official Unity EditMode run `98c98f9660754a9296f018b46bdc4a3c`:
  `3296/3296`, zero failures/skips/inconclusive; Unity `6000.3.9f1`.
- Official filtered EditMode Smoke run `df2b7400f8ee4eb19e6154a2d7c848f5`:
  `5/5`, zero failures/skips/inconclusive. It used the same Unity input digest.
- `git diff --check`: passed after source changes; rerun after this record edit.

The earlier commit `e24d121e` received exact-tip review status NEEDS_CHANGES
because its Core admission gate did not preserve the host census protocol's
local health/owner-thread readiness check. This follow-up restores that check
as a readiness-and-count-parity port while keeping the idle-count decision
atomic in Core, and adds a regression for host-protocol rejection. The earlier
reviewed commit was not promoted. Earlier failed test attempts from the
pre-checkpoint iteration remain documented in `VALIDATION.md`; the final
official run here passed.

## Retained evidence

The normalized full-EditMode package for this exact Unity input is
[`coref-selected-runtime-admission-readiness-full-20261103.zip`](Evidence/coref-selected-runtime-admission-readiness-full-20261103.zip)
with manifest
[`coref-selected-runtime-admission-readiness-full-20261103.manifest.json`](Evidence/coref-selected-runtime-admission-readiness-full-20261103.manifest.json).
The approved verifier passed with `runs=1 entries=1`. Manifest SHA-256:
`4ddf3c3a8f83ae2e622be842952ddcc5d4ab3a8bded67a4834629235d2ff6c72`;
archive SHA-256:
`bfbd8d9df9c300e48816cabd435f07a8774b9510cb16b5c386a3539e39c7172c`.
The archive contains normalized NUnit XML only. The separate Smoke result was
kept private; no attempt was made to combine it with the full report.

CoreValidation's original cold/warm TRX reports and private manifest remain in
the owner-only E: evidence directory, unchanged. Private manifest SHA-256:
`02c0c483e69b6452ebe8549e1f0bb64bbc73728ccef9a43bd6d9e9fd8b191c06`; cold TRX SHA-256:
`59bb89272d6ec0d8209cb22d4857a715e7033b31977fe9ec9703ef916e7dc0cd`;
warm TRX SHA-256:
`074002ae305f8b569700f3c9621a6efd1bc0e6f00189ffe0f5926c5d864867ad`.
The approved `DotNetTrx` packager does not accept the CoreValidation manifest
as `dotnet-trx-source-context-v1`; no unsupported context was fabricated and
no raw .NET TRX was published. A durable public .NET report package is still
required before canonical promotion. The Unity worktree had a `Library` cache;
this run makes no Library-independence claim.

The earlier publication-state-only package remains historical at
[`coref-selected-boundary-full-20261103.zip`](Evidence/coref-selected-boundary-full-20261103.zip)
and its manifest. Its corrected manifest SHA-256 is
`5eaf9330bcdff4a105a4e3e673dd2d2720c46be5f2332465a93b623828978632`; it does
not cover the later Core-owned idle-admission change.

The intermediate `coref-selected-runtime-admission-full-20261103` package
predates the host census-readiness correction. Only the `readiness-full`
package above applies to the corrected admission boundary.

## Status

This is a validated intermediate candidate only. Exact-tip independent review
of candidate `e7da26a3ee8ececaf3a0f31f81235e72c4d75fbf` passed as
`VALIDATED_CANDIDATE` against `ff3852fd3a283a2ad2bcba8fe29d3660908e397f`.
The durable review record is [CORE-F Selected Runtime Publication and Admission
Review](../../reviews/CORE_F_CAPTURE_PUBLICATION_ADMISSION_E7DA26A3.md), commit
`04b13be8dac3011381dfc2d26dfd66ef1e30ac69` on
`codex/review/coref-runtime-admission-e7da26a`. The candidate is not promoted;
the private .NET TRX packaging limitation remains. This slice does not complete
the 89-family census, Core-owned P12 capture/restoration, `TryStart`, or the
detached .NET host.
