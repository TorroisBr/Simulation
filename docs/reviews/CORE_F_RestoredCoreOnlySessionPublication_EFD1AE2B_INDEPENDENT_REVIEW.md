# Independent exact-tip review — CORE-F Core-only restored-session publication

**Verdict: PASS — VALIDATED_CANDIDATE**

- **Candidate branch:** `codex/core/CORE-F-restore-stage-transaction`
- **Candidate commit:** `efd1ae2b41acd54661eebf37a077e973e66a0bd3`
- **Candidate tree:** `0305a0b7d3377a5122209d9ad5016b2ad496f0f9`
- **Candidate parent:** `54cc815632b4b437bd81a13b8d4de27403feec1f`
- **Review base for the cumulative two-commit delta:** `172ca1a686835e7c0e25837e6a6763f8d6ee6753`
- **Core canonical at review:** `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- **Remote preflight:** the candidate branch pointed to the reviewed commit; Core canonical matched the SHA above.
- **Review completed:** 2026-11-05

## Reviewed change

The cumulative change adds `DailyV1Application.TryCommitInitialRestoredCoreCandidate` and removes the requirement that an initial Core restore publication also carry a host-projection transaction. The new entry point still runs under `activeSessionGate.SyncRoot`; it requires this application’s exact initial-restore admission and image token, the exact private candidate’s restore proof bound to that image, and host ports whose execution-state object is the candidate’s own execution boundary. It finalizes the restored boundary before publication, including the proof’s world/day/completed-sequence, owner-section and mutation-epoch identities, and only clears the proof after a successful commit through the existing initial-session publication gate.

The admission checks remain strict: a missing admission, admission from another application, or separately decoded same-bytes image is rejected. The tests verify rejected calls leave `ActiveSession` unpublished and do not bind or replace the candidate runtime boundary. The successful Core-only case asserts that the exact Core session becomes active while no Unity host projection is published. Existing tests continue to exercise the projection-backed commit path.

The reviewer found no actionable defect in proof/image identity, owner-thread and mutation-guard finalization, late owner-vector recapture, completed-boundary admission, publication atomicity, or projection-registration behavior. The change remains bounded to the initial restored Core-session publication seam.

## Validation evidence inspected

The official manifests and original results were inspected; the exact changed source hashes match the reviewed candidate files.

| Gate | Result | Evidence identity |
|---|---:|---|
| CoreValidation cold | 1017/1017 PASS | Run `664423020a9341ca98b61bf241bc64f9`; TRX SHA-256 `dbd6f6e71e5354cad501c870e7af1f2e0ed64ce8a9767d821b4d31a6e1446df9` |
| CoreValidation warm | 1017/1017 PASS | Same run; TRX SHA-256 `db8edaed3ba03bdbc4396c2acd6b97a72d72570e080d59146d8025dca8bfe278` |
| Full Unity EditMode | 3299/3299 PASS; 0 failed/skipped | Run `6e55704d2f554e4dbd206a5d2a89b6a5`; input stable; XML SHA-256 `01bd24da7feba10527427ea04a9daf3793f1f4edcd5e4d931fc54ae1e69d2913`; log SHA-256 `ed58a69b0cd43a610cc631344df408526096640480628fa60b544de54b1e93ef` |
| Official Smoke | 5/5 PASS | Run `6c6f15f31f614d5eb9be7a17aa9f02b7`; input stable; XML SHA-256 `3933f0a26d1a3373669e165daec8df71e19e09ac02401b5ad3f36a96bffcda99`; log SHA-256 `8da540cd0370eb7758a3c9c78838e5a9ff27b8b7b1c07f401099762953cdeadb` |
| `git diff --check` | PASS | Rechecked against the cumulative candidate delta. |

The CoreValidation manifest records `DailyV1Application.cs` at SHA-256 `32224131527945d68ec0148306d8bbb47d82b63789cc00a0d4be1f72c1d6e562`. The stable Unity input manifest records that same application hash and `P12GOwnerEntryQualificationTests.cs` at SHA-256 `f38add3b9b3d574e737a00eeaaf101d7c7892fae62bfd6d3af20151c2ee20394`; both match the candidate worktree. The CoreValidation run was performed from parent `54cc8156` with the candidate application source present as a working-tree change; its manifest also lists unrelated settings/evidence workspace changes, which are outside this Core test source identity. Unity’s full-suite input snapshot was stable.

## Scope limits

This proves that Core can publish its exact restored session without publishing a Unity projection. The restoration candidate is still constructed by the Unity restore coordinator and supplies Unity-created `SimulationRuntime` host ports. This does **not** establish standalone .NET restoration, an operational `DailyV1Application.TryStart`, a Unity-independent restore producer, or complete CORE-F acceptance. Core canonical is unchanged; this review record does not promote the candidate.