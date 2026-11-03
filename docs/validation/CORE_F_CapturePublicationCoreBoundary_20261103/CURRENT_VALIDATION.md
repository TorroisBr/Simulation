# CORE-F selected runtime publication/admission boundary

## Scope

This candidate makes two adjacent Core-owned boundary corrections:

- Factual-read publication checks use the exact `DailyV1ExecutionBoundaryState`
  already bound to the admitted session. Unity's publication timing and setter
  are unchanged; the redundant host readback was removed.
- Initial DailyAdvance admission checks the exact registered Core runtime
  operation inventory and active-operation count under the same Core lock that
  registers the DailyAdvance scope. Unity no longer reports an independently
  read idle count. This preserves rejection of nested/busy DailyAdvance entry;
  ordinary P12 protocol operations remain nestable after the outer advance has
  begun.

The host still owns full owner-section census capture, the compatibility
operation-context check, mutation-guard binding, and restore-candidate census.
No census writer/provider, mutation-epoch, reservation, or owner-section
authority moved. All 89 P12 family rows remain subject to the existing complete
vector protocol. This does not make `DailyV1Application.TryStart`
implementation-ready, complete detached restoration, or complete CORE-F.

The Core admission regression holds a registered runtime operation and verifies
that the Core boundary rejects a second DailyAdvance before requesting a
selected execution boundary. It then closes the held operation and asserts the
registry returns to zero. Existing restore fixtures mark their exact Core
execution states published after restored-token creation, matching production
finalization. No assertion was weakened.

## Source identity

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Base at validation: `ff3852fd3a283a2ad2bcba8fe29d3660908e397f`
- Committed tree at validation: `0cd628e54979c57a53d9fc126bcd2a12150d1362`
- CoreValidation input digest: `f9e7f1bd90fddb344fca3d1e9606dea6f4cd5d8ff8a43ad3ccab70aff4fae0cf`
- Unity input digest: `49e885745eaed664e5f8324ffb36b5e2eb7940979639826cfde79e6c186d4e6e`
- Both official runners reported stable before/after inputs. Their private
  manifests bind the applicable source hashes below.

| Source file | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs` | `ac84c6d680fed0a9937aacaa4ed985549fb3cc93e438f14c2c167930e0b9bf21` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs` | `a62f0db16b2e2560959eed30b9e00e3bfb18ba8140f990033b3558c1ece3ddc5` |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `775d81c29ed0b39b26966ffffaf6574d15d37421368ab2392a233c1e528bf20e` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs` | `02df554e0560f912903ca0ae3fe739ce1b8e6a626d3a44a7e3998bc271e1dcbc` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeOrchestrationTests.cs` | `f15f8c67513d4bf16e94efd948be2e057c3c7e5aeb24dc41974299a8a335fafb` |

The three existing local `ProjectSettings` modifications and unrelated
untracked files were preserved and not staged. Unity's private input inventory
records the `ProjectSettings` bytes used by both Unity runs; they are not
claimed as part of the candidate source changes.

## Validation

- Official CoreValidation run `aa3d87bbe2784bcda666a1cca196c779`, pinned SDK
  `9.0.315`: cold `997/997`, warm `997/997`; build and locked restore passed.
  This was the documented `-SkipLab` profile; no Lab-validation claim is made.
- Official Unity EditMode run `b38dbb78ce57495aa5c2fcab4ec175d4`:
  `3296/3296`, zero failures/skips/inconclusive; Unity `6000.3.9f1`.
- Official filtered EditMode Smoke run `9ba5738b81ce4b87bfe380a27da4158e`:
  `5/5`, zero failures/skips/inconclusive. It used the same Unity input digest.
- `git diff --check`: passed after source changes; rerun after this record edit.

Two earlier CoreValidation attempts with this source/test slice failed and are
retained privately: one selected an operation absent from that test fixture's
registered inventory; the next entered through the application layer, which
correctly rejected the already-active advance earlier than the intended Core
boundary assertion. The regression was corrected to exercise the Core boundary
directly with a registered operation. The final official cold/warm run passed.
An earlier invocation from outside the E: worktree did not execute tests
because the runner could not resolve its relative project path; it is not
validation evidence.

## Retained evidence

The normalized full-EditMode package for this exact Unity input is
[`coref-selected-runtime-admission-full-20261103.zip`](Evidence/coref-selected-runtime-admission-full-20261103.zip)
with manifest
[`coref-selected-runtime-admission-full-20261103.manifest.json`](Evidence/coref-selected-runtime-admission-full-20261103.manifest.json).
The approved verifier passed with `runs=1 entries=1`. Manifest SHA-256:
`904557677865b7d37bb048db3cd4bb535a420cb3539fd8a8e2c86982d7fa99d3`;
archive SHA-256:
`0ab8855bafa377c20101367b48e3d5258a368cb8b0af5a5bced0f84324d700a1`.
The archive contains normalized NUnit XML only. The separate Smoke result was
kept private; no attempt was made to combine it with the full report.

CoreValidation's original cold/warm TRX reports and private manifest remain in
the owner-only E: evidence directory, unchanged. Private manifest SHA-256:
`fc9ed6540893f99340487c476fb4ae29f3f63f5f0538e03001ffbd3dfbccc188`; cold TRX SHA-256:
`657f676b5f163265d483265f3bcd754fef48e6ff9d552fa9f9cfa6f7413fb281`;
warm TRX SHA-256:
`fb1c610a77e30c53aa4eec053685bb111ae531015fc0e0d27ffee03d9ac2188b`.
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

## Status

This is a validated intermediate candidate only. Exact-tip independent review
of the combined diff is pending. It is not promoted and does not complete the
89-family census, Core-owned P12 capture/restoration, `TryStart`, or the
detached .NET host.
