# P12-E War Owner Snapshot Current-Base Revalidation — Independent Review

**Verdict:** `VALIDATED_CANDIDATE` — current-base design revalidation PASS. The original bounded War owner design remains valid for implementation under the accepted P12-E prerequisite authorization; implementation must retain the serialized shared-store handoff described below.

**Review date:** 2026-10-08  
**Current P12 canonical:** `77135b3e0ca8df83c6852f2c234ff9098a833468`  
**Current architecture:** `codex/architecture/world-identity-projection` at `47eff220c7ce00f6e7c759bdc2b76780bb46f628`  
**Revalidation candidate:** `codex/phase12/P12EWarOwnerSnapshotCurrentBaseRevalidation77135B3` at `4c11486c9b918716c4fe1443cfffab681c6088eb`, tree `184ff2efaed117ae102d20e5767e67144ea534cf`  
**Candidate parent:** `77135b3e0ca8df83c6852f2c234ff9098a833468`  
**Original War design:** `eefefe271cfc1553be120b292015a313a08128dd`  
**Original exact-content design review:** `5b18b5cea413838335ee9d6a9782eae9d713d211` (`PASS`)

## Exact-content and base checks

The remote P12 canonical, architecture, candidate, original design, and review refs were refreshed and match the SHAs above. The candidate is a direct child of current canonical and adds only `docs/design/PHASE12_P12E_WAR_OWNER_SNAPSHOT_CURRENT_BASE_REVALIDATION_77135B3.md`. It does not alter the original design or implementation. `git diff --check` passes.

The original review explicitly required revalidation after the Conflict implementation was promoted. The current State records Conflict owner snapshot promotion and keeps War implementation ordered after the canonical Conflict API. The P12 Brief and accepted P12-E technical design remain unchanged from the original design review; the State update records the Conflict promotion.

## Findings

1. **Drift classification is correct: `BASE_DRIFT_ONLY`.** Between the original War base `a4ce0ab` and current canonical `77135b3`, the War/Battle contracts, Battle owner snapshot, and P17 runtime-rejection test are unchanged by blob identity. The shared `PersistentConflictWarBattleStores.cs` diff is only a 50-line Conflict snapshot staging factory inserted inside `PersistentConflictStore`; the War and Battle regions are unchanged. The newly added `PersistentConflictOwnerSnapshot.cs` supplies the matching detached Conflict contract.
2. **The promoted Conflict API satisfies the typed-parent dependency.** `PersistentConflictOwnerSnapshot.TryStage(ArmedForceStore, ...)` delegates to `PersistentConflictStore.TryCreateFromOwnerSnapshot`. That factory builds a private Conflict store with the exact supplied staged ArmedForce store, restores the saved revision, validates its invariants, and returns it only on success. This is the typed staged Conflict parent required by the original War design.
3. **War staging remains compatible with current constructors and invariants.** `PersistentWarStore` accepts and exposes both its ArmedForce and Conflict parent objects; its record validator resolves optional `ConflictId` through the linked Conflict store and participant bindings through the linked ArmedForce store. The revalidation correctly requires War staging to use the exact staged ArmedForce and the exact Conflict result from `TryStage`, rather than a live/equivalent Conflict or IDs alone. Because the constructor itself does not enforce parent-object identity, the future War factory must preserve this exact construction rule.
4. **War-before-Battle staging remains the supported order.** `PersistentBattleOwnerSnapshot` accepts staged ArmedForce, Conflict, War, spatial authority, and topology parents. Its build path checks `ReferenceEquals` across the ArmedForce/Conflict/War parent graph, and its Battle factory creates the private store with those supplied parents. The revalidation correctly requires the War store to reference the exact Conflict instance subsequently supplied to Battle. No reverse Battle dependency or duplicated Conflict/ArmedForce rows is introduced.
5. **P17-A remains fail-closed for Daily-v1.** The accepted P12-E design excludes P17-A from the selected Daily profile and requires War capture/staging to reject unsupported P17 state rather than omit it. Current `P17ARuntimeTests.P17StateIsRejectedByStandardAndSelectedDailyCompositionsWithOrWithoutP16Input` covers the selected Daily admission rejection for a War carrying P17 state. The current P12-E technical design and the original War design preserve the separate P17 profile and its semantics.
6. **Accepted War semantics remain intact.** The owner remains `PersistentWarStore` with its stable identity, lifecycle/end day, optional Conflict reference, sides, bindings, exact local revision, and exact ArmedForce references. Existing rules remain unchanged: at least two sides with no invented upper bound, registered inactive Forces may remain in historical bindings, while a new binding requires an active Force. Detached values and private reconstruction preserve those facts without replaying writers. Battle stays downstream; P12-G retains global validation/publication.
7. **No architecture or product decision is unresolved.** The current architecture, P12-E scope, Daily-v1/P17 boundary, and original exact-content review are compatible with the revalidation. P12-E remains open; this review does not establish whole-profile parity, P12-A readiness, P13 readiness, or Phase closure.

No Unity tests were run because this candidate is documentation-only. Implementation still needs the focused War/Conflict/ArmedForce suites, ALL EditMode, official Smoke, and `git diff --check` specified by the original design.

## Integration constraint

`PersistentConflictWarBattleStores.cs` remains a shared hotspot. Conflict is canonical; take the next isolated edit for War only after the owner handoff, then integrate Battle after War. The War factory must attach the exact staged ArmedForce and Conflict instances. Before Battle staging, preserve the explicit exact-parent check already required by `PersistentBattleOwnerSnapshot`.

This record reviews design freshness only. It does not review or promote implementation and does not claim P12-E completion.