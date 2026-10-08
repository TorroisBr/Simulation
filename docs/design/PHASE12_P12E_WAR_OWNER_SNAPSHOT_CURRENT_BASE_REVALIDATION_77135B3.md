# P12-E War owner snapshot — current-base technical revalidation

**Status:** Docs-only current-base revalidation candidate; awaiting independent exact-content review. **Drift classification: `BASE_DRIFT_ONLY`.** The previously reviewed War owner semantics remain unchanged after P12-E Conflict promotion. This document neither implements the War snapshot nor authorizes whole-profile publication or Phase closure.

## Exact baselines and prior review

- Current P12 canonical: `codex/phase12/canonical` at `77135b3e0ca8df83c6852f2c234ff9098a833468`, tree `ea19d9b756f9272f8e6410a78386f6f5694330b2`, `Assets` tree `f503667082098813a87380b4c7f1c84de986970e`. The remote canonical ref was verified at this exact commit. This commit is the docs-only promotion record whose parent is the reviewed Conflict candidate `ff733409b4f38b4078f21d81f845e62b66ba3392`; its reviewed code tree is `3e0904b93a374b474d9d78866d00f763ca936a3c` and tested `Assets` tree is the same `f503667082098813a87380b4c7f1c84de986970e`.
- Current architecture: `codex/architecture/world-identity-projection` at `47eff220c7ce00f6e7c759bdc2b76780bb46f628`; architecture, Roadmap, and Execution Model blobs `25843842688239cdc3b80988b2e28dbaa16b4987`, `d03e144544ab25371b71db64538c0de47ae8381c`, and `fcd49f34ec8b2f0321cc444bf7b3e612ffd963b2`.
- Current P12 Brief, State, and accepted P12-E technical design blobs: `31d9e1b4df41fc994f0a747274e35c4ef40b6e3a`, `fa3b5a333f882e812ab2044343975f31299c6f32`, and `15aaee09d3295cff81a48e166b620c89f5156346`.
- Original bounded War design: branch `codex/phase12/P12EWarOwnerSnapshotDesignA4CE`, exact commit `eefefe271cfc1553be120b292015a313a08128dd`, tree `0a5265df14ac71461995983fea2f7c56b69fec76`, document blob `c0378484b14f7a36d20847e132a9a038951a2953`. Its exact-content independent design review PASS is commit `5b18b5cea413838335ee9d6a9782eae9d713d211`, review-document blob `4a9387eb39d02055b4903dc33c0dba6d1fc72b5f`. That review explicitly required revalidation after Conflict implementation promotion; it does not itself approve implementation against the current base.
- Current Conflict implementation review is `VALIDATED_CANDIDATE`, recorded at `b3ada0be866b789b52fb4ea23d6698cb27374c91` for candidate `ff733409b4f38b4078f21d81f845e62b66ba3392`.

## Drift comparison

The reviewed War contract is unchanged. `PersistentConflictWarBattleContracts.cs` is blob `30cbc501eea24796a7d3bac47515d7fba08fedc1` on both the original War design base and current canonical. The current `PersistentConflictWarBattleStores.cs` is blob `153c522cfc88adbe389f3efbd0357864db73820d`; relative to the original base blob `34beed712544d30c85f9844defc933b059831548`, its only change is a 50-line Conflict-owner staging factory inserted in the `PersistentConflictStore` region. The War and Battle regions and their mutation/invariant contracts are unchanged.

The accepted P12 Brief and P12-E technical design are unchanged from the prior review. Current State records Conflict snapshot promotion and explicitly keeps P12-E in progress, with War implementation ordered after the Conflict owner API becomes canonical. No architecture, accepted owner boundary, P17-A profile boundary, or War/Battle ordering change was found.

## Current-base technical revalidation

### Conflict parent now available

At current canonical, `PersistentConflictOwnerSnapshot` is blob `13ccab97ff53e7f3f391645dc855993360839c84`; `PersistentConflictWarBattleStores.cs` is blob `153c522cfc88adbe389f3efbd0357864db73820d`. `PersistentConflictOwnerSnapshot.TryStage(ArmedForceStore, out PersistentConflictStore, out failure)` delegates to `PersistentConflictStore.TryCreateFromOwnerSnapshot` with the supplied staged ArmedForce store. That factory validates and reconstructs into a new private Conflict store constructed with that same ArmedForce object, restores the saved revision, checks invariants, and returns that candidate only on success. The prior War requirement is therefore satisfiable with a concrete typed parent: War staging must consume the exact returned Conflict store, not the live Conflict owner, an equivalent duplicate, or ID-only evidence.

`PersistentWarStore` is constructed with both its ArmedForce and Conflict parents and exposes those same object references as `ArmedForceStore` and `ConflictStore`. Its existing record validation resolves optional `ConflictId` through that linked Conflict store and bindings through its linked ArmedForce store; it preserves registered inactive Forces in historical bindings while requiring active Forces for new bindings. A War snapshot factory should construct the candidate with the exact staged ArmedForce and the exact `PersistentConflictOwnerSnapshot.TryStage` result before validating rows, then restore the saved local War revision without replaying writes. This preserves the already reviewed War-owner rules and makes the new Conflict API a compatible dependency.

### War before Battle remains the valid order

`PersistentBattleOwnerSnapshot` is current blob `179e0bd7d3a8a9f4ee05b1d9c9ced10cf5c7646e`. Its staging boundary takes staged ArmedForce, Conflict, War, SpatialAuthority, and LocalTopology parents. The Battle factory constructs its private store with those supplied parent objects; record reconstruction resolves a War ID against the supplied staged War store, resolves Conflict IDs against the supplied staged Conflict store, and rejects contradictory War/Conflict IDs. Thus the supported order remains roots/references, ArmedForce, Conflict, War, Battle. The Battle stage must receive the same staged Conflict instance used to construct the War store and the exact staged War instance returned by War staging. Since the existing Battle factory accepts Conflict and War as separate parameters, a future package coordinator should assert `ReferenceEquals(stagedWarStore.ConflictStore, stagedConflictStore)` before staging Battle; no new Battle authority or reverse dependency is implied.

### P17-A remains rejected by Daily-v1

The accepted P12-E contract still excludes P17-A state from Daily-v1 while retaining the separate P17-A composition. The current `P17ARuntimeTests.cs` blob `ad6e872ed4e2025e2e9aa2cff6580beead28db86` includes `P17StateIsRejectedByStandardAndSelectedDailyCompositionsWithOrWithoutP16Input`; it asserts that populated P17 state is rejected when the selected Daily-v1 admission context is supplied, with or without the P16 input. The War DTO remains limited to the reviewed base War facts and has no P17 field. Capture/staging must continue to reject P17-A state rather than omit it; this revalidation does not remove or weaken the separate P17-A behavior.

## Disposition

**`BASE_DRIFT_ONLY` — the reviewed War semantics remain unchanged on current P12 canonical.** The promoted Conflict stage API resolves the explicitly pending typed-parent dependency. Keep the exact-owner, detached-value, revision/cardinality, fail-closed P17-A, and private-staging requirements from the original design. Preserve the serialized hotspot rule: Conflict is already promoted; a War implementation may now take the next isolated edit in `PersistentConflictWarBattleStores.cs`, with Battle following it. This record makes no Unity-test claim because it changes documentation only. Fresh independent exact-content review is required before implementation handoff; this record is not self-review or implementation authorization.

`Assets` remains unchanged at tree `f503667082098813a87380b4c7f1c84de986970e`.
