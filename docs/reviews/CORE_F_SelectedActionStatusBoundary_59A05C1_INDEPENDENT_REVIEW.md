# Independent review — CORE-F selected action-status boundary

Date: 2026-10-30

## Verdict

**PASS — `VALIDATED_CANDIDATE`** for candidate `59a05c1c4b38b147c8765ec66989b8115c8bf080`, tree `d00c6c638694cb8008f396960a8a480c6d61724d`.

- Base: `ceb030343353614b12820b7528ec7e0f2aeeaffc`.
- Current Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`.
- Exact-tip implementation and evidence review was performed independently from the candidate author. A second independent review of the same immutable commit also returned `VALIDATED_CANDIDATE` with no actionable findings.

## Scope reviewed

The complete base-to-candidate change was reviewed, including the `SimulationRuntime` host adapter, `DailyV1SelectedActionExecutionStage`, the Core regression tests, and the validation record/package. The ordinary admitted action-status route is selected only for an admitted UnityBootstrap-Daily-v1 execution and pins the exact session, owner graph, actor/target instances, protocol and coordinator. Core opens the exact P12 status mutation scope, checks the selected owner before writes, updates Core revision and section notification state; Unity retains the exact runtime projection checks and mirrors. The forced-Escape route that reuses its wider receipt operation remains on the existing host adapter. I found no new mixed-roster behavior change.

The added tests verify exact actor/target section admission, revision increments and notification order, plus projection and P12 admission rejection before owner writes. Existing actor-first partial-target failure coverage remains unchanged. I found no weakened assertion or actionable correctness defect. The tests do not inject an exception after a successful owner write during projection refresh or P12 notification; the prior boundary did not roll back that committed owner mutation either, so I do not treat the missing injection as a regression in this bounded change.

## Evidence checked

- CoreValidation: **968/968 cold and 968/968 warm**.
- Focused Unity `SimulationRuntimeAdmissionTests`: **230/230 passed**.
- Full Unity EditMode: **3,283/3,283 passed**, including Smoke **5/5**.
- Privacy-safe Unity evidence package verifies against the original run manifest; the package manifest and archive are part of the candidate.
- `git diff --check`: **PASS**.

The two earlier 39-test diagnostic runs are not evidence for this candidate: one had unstable inputs; the other used different `SimulationRuntime.cs` and Core stage source hashes. The source-matched full validation and these earlier diagnostics are distinguished in the candidate `VALIDATION.md`.

The original CoreValidation TRX evidence remains protected and is not part of the Unity package. The approved normalized .NET TRX adapter is not yet available, as recorded by the candidate; this review does not claim a public normalized .NET package, final CORE-F acceptance, or canonical promotion.

## Review disposition

No actionable findings. This review accepts only the exact bounded change at `59a05c1c4b38b147c8765ec66989b8115c8bf080`; it does not review any later candidate changes and does not declare the CORE-F package complete.
