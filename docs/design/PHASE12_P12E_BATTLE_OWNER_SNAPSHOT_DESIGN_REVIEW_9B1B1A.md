# P12-E Battle Owner Snapshot Design — Exact-Tip Review

**Result: PASS — no blocking findings.**

- Proposal: `9b1b1a5603056c57d89cd048b434993aeb927397`
- Proposal document: `docs/design/PHASE12_P12E_BATTLE_OWNER_SNAPSHOT_DESIGN.md` (blob `68d88d73346f2f28fccf2a4edbbf70f2b8fdc255`)
- P12 canonical base: `ef0cafb5848cadcf0ac91a3e1af7ff3faaab1367`
- Architecture authority: `47eff220c7ce00f6e7c759bdc2b76780bb46f628`
- Existing P12-D City snapshot promotion is present in the base State: implementation `58c142329d034fbed18ece25ee017d0d69f4d62f`, candidate `22f20b1ab0cf9ba4fc36155c645554c98bff9c6e`, review `14afa99c802305d75761e589c47e7997276c80c0`, Assets tree `e83151063141cb1395e1d53d30e850c6f81ed778`.

Reviewed against `docs/design/PHASE12_E_TECHNICAL_DESIGN.md`, `docs/phases/PHASE12_BRIEF.md`, `docs/PHASE12_STATE.md`, `docs/design/PHASE12_OWNER_COVERAGE_INVENTORY.md`, the existing Battle census design/review, current Battle/War/spatial owner code, and the P17-A master handoff and runtime rejection semantics.

## Findings

No blocking findings. The proposal is a bounded design for one existing P12-E owner, `PersistentBattleStore`, and does not introduce new gameplay or participant cardinality semantics.

- **Identity, provenance, and immutable values:** It carries every retained Battle keyed by `BattleId.Value`; preserves nullable `StartedAbsoluteDay`, `ConflictId`, `WarId`, and location; and preserves typed spatial references rather than flattening them. The fields match `PersistentBattleRecord`, `SpatialReference`, `PersistentBattleTerminalOutcome`, `PersistentBattleOutcomeProvenance`, and `BattleResolutionProvenance`. All eight D5 provenance fields and four non-empty D6B2 provenance fields are included. Terminal results are recorded as accepted and never recomputed or reapplied.
- **Ordering and local invariants:** Ordinal Battle, side, and binding ordering matches `PersistentBattleStore.Records` and `PersistentBattleRecord` sorted-copy construction. The design retains the existing two-side minimum, unique side/binding identities, parent links, force references, lifecycle rules, outcome/winner rules, and exact count; these are current owner invariants, not new product rules.
- **Causal writes and revisions:** The registration, binding, start, terminal-install, rejection, overflow, and rollback cases match `PersistentBattleStore.TryRegister`, `TryAddParticipantBinding`, `TryStart`, `TryCommitTerminalWrite`, `CanAdvance`, and `RestoreBattleTransactionSnapshot`. It correctly treats revision as owner-local and rollback-capable, not as a monotone epoch. The required post-assignment exception test checks that rollback restores the prior immutable row and exact revision.
- **Capture binding:** It consumes the existing P12-B completed-boundary token and exact `p12e.battles` section (schema 1, exact installed store identity, count, and local revision) without adding a capture lock, owner registration, or epoch. `PersistentBattleCensusProvider` confirms this section contract. One detached `Records` read plus owner-thread/quiescent token validation is consistent with the existing P12-B/E boundary.
- **Validation and staging:** Local Battle invariants are checked before a private factory result is accepted; Conflict, War, ArmedForce, and spatial references resolve against already staged authorities in the documented dependency order. The proposal sends no unresolved Battle relation to P12-G. Its reconstruction uses the exact admitted parent instances, records, and revision and does not call lifecycle or consequence paths.
- **P10/P17 and deferred scope:** Daily-v1 keeps P10 `LocalTopologyStore` typed `NOT_COMPOSED`; staged Battle construction uses a null topology owner, and `SubLocation` or injected topology state rejects. It retains the required-empty `p12d.explorable-sites` contract and does not change P10-A. The proposal does not capture or reinterpret P17-A strategic War state; this matches the P12-E design and `SimulationRuntime`'s explicit rejection of P17-A state outside its separate composition. It does not add P12-A integration, P12-G publication/parity, P13 guarantees, P17 behavior, new War semantics, or other product scope.

This is a design review only. PASS confirms the bounded contract is suitable for the next authorization step under the Execution Model; it does not itself authorize implementation, promote code, establish P12-E completion, make P12-A ready, or unblock P13. P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked.
