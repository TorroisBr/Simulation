# P12-B dynamic NPC Inventory census candidate

**Status:** implementation candidate; not promoted and not a P12-B readiness
claim.

**Base:** `f961477380508647d2975272da72d96892944ed9`.

The candidate adds one schema-v1 `p12f.inventory/<RuntimeId>` witness for each
currently rostered NPC, ordered by ordinal RuntimeId. Each witness binds the
exact `NpcRuntime` and its existing exact `InventoryRuntime`, reports the
InventoryRuntime item-row count and its existing revision, and reads row
cardinality without initializing absent item storage. The NPC inventory getter
is not used for owner-presence checks.

Inventory sections are staged and reconciled alongside the promoted dynamic
SpatialKnowledge family and fixed PersonStore sections in the existing outer
NPC membership protocol operation. A successful add, unregister, or separate
same-ID re-registration publishes one combined delta and advances the protocol
epoch at most once per operation. Rostered dead/emigrated NPCs retain their
section. Inventory revision/owner drift or malformed/missing owners fail
closed. Direct inventory writes retain InventoryRuntime's existing revision
only; this candidate adds no writer notifications or global epoch wiring.

The separate City ImportantNpcs projection remains outside this family and
epoch. A targeted test leaves a stale City projection after NPC unregister:
inventory reconciliation continues to describe the roster, while the existing
City presence provider rejects the non-rostered projection member. No City
cleanup or City behavior is added. A StartingCity outside the composed City
list remains outside this witness set and full-profile inventory/admission
remains a separate blocker.

## Validation

All results were produced in this candidate worktree and retained under
`Library/ValidationResults/P12BNpcInventoryCensus`:

- `NpcInventoryCensusTests`: 6/6, `EditMode-20260930-204649-20f2b613076e452282aac67e6c7d75b2.xml`.
- `SimulationBootstrapCompositionTests`: 14/14, `EditMode-20260930-204203-595e024e090d4074a1c04389c9c5c47a.xml`.
- ALL EditMode: 2048/2048, `EditMode-20260930-204714-b9592a0d1f974f648e38c5167a916bfe.xml`.
- Complete official Smoke: 5/5, `EditMode-20260930-204809-5d133bd480a442d7b8dfd825ebee37f0.xml`.
- `git diff --check`: passed.

This candidate does not complete P12-B, change P12-A readiness, establish
global owner-thread/quiescence, connect direct InventoryRuntime writes to the
shared epoch, seal City composition, or add export/hydration/capture support.
