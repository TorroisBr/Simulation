# P12-G Pending ActorChoice admission — independent exact-tip review

- Result: **PASS / VALIDATED_CANDIDATE**
- Candidate branch: `codex/phase12/P12GSourcePendingActorChoiceAdmission`
- Base and current P12 canonical: `70d9ac29e1ec14ffe8792e9313f42bd4821c72a9`
- Exact candidate tip: `1a7424ba19f9448158ba79dc715cf95257655ac8`
- Implementation commit: `1164966c783e41e3cc4ea446d0e0e819a828fb55`
- Implementation Git tree: `b26538ec09b91cf9bf06d2d2daa7dd68937f8ef5`
- Validated Assets tree: `af1cd303919956cadda6ceed0547e6be661a71c3`
- Independent reviewer: `/root/p12g_actorchoice_review`

## Review result

The executable diff adds only a test to `SimulationRuntimeAdmissionTests`. The fixture queues a normal trusted LocalPlayer/Request SellGoods input after a healthy completed Daily-v1 boundary. It verifies the registered P11 `p12f.actor-choice-inputs` owner row changes from cardinality 0 to 1 with one revision increment and the expected owner identity, then establishes that the old completed-boundary token is stale. Restore rejects as `InvalidSourceSession` before any private stage, with zero gameplay callback entries. Assertions preserve the pending input, source session, health, census identity/revision, sequence, facts, and roots. The source and uninterrupted control then advance normally; a valid retry and subsequent-boundary owner parity are covered.

The test uses the P11 census-provider wrapper over the same runtime store. It is a source-admission witness, not an F-hydrator Pending-snapshot test and not a P12-G completion claim. It does not claim a Person-backed actor-choice consumer in SampleScene.

## Validation evidence reviewed

The final validation manifest is [`../validation/P12GSourcePendingActorChoiceRestore/VALIDATION.md`](../validation/P12GSourcePendingActorChoiceRestore/VALIDATION.md). The reviewer verified that the final XML, compressed logs, and decompressed logs match the manifest and belong to the validated Assets tree. An earlier run using the P18 temporal provider is explicitly superseded and was not used for this review.

- Focused case: 1/1 passed; XML SHA-256 `6eb0e443f419353c11fa5ccedabc57faec37bf7f30b22949662f5c02d461c6e6`; compressed log SHA-256 `475c21b1701d70b675d8502af9699b7e5b5a922dc924a8c3e3a3d91595e00092`; decompressed log SHA-256 `5a545a9115700bf8cb5e61be34506e54f0d1169f1d63cd1c71cd0ec4e90893c8`.
- `SimulationRuntimeAdmissionTests`: 189/189 passed.
- ALL EditMode: 2863/2863 passed.
- Official Smoke: 5/5 passed.
- `git diff --check`: passed.
- All final reports have zero failures, skips, and inconclusive tests.

The exact-tip reviewer made no edits and did not rerun Unity. No material findings remain. Canonical promotion is a separate operation requiring its own refreshed preflight.
