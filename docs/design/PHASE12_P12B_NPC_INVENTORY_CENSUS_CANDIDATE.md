# P12-B dynamic NPC Inventory census candidate

**Status:** promoted to P12 canonical at `15e5a543f3896c02f9dbd9c36c9ba75bc90dac2b`;
this is not a P12-B readiness claim. Exact-tip implementation review is
recorded as
`docs/design/PHASE12_P12B_NPC_INVENTORY_CENSUS_IMPLEMENTATION_REVIEW.md` on
`codex/phase12/P12BNpcInventoryCensusReviewRecord` at
`f98c5d4f3ddc3721b4b31de1a40f063e2b333831`.

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
An existing RuntimeId section may not be rebound to a new NPC or inventory
owner: a same-roster InventoryRuntime replacement discovered while an
unrelated NPC registration crosses the outer membership reconciliation faults
the census and retains the previously published provider snapshot. The roster
registration itself reports its domain insertion result; census assessment
then fails closed.

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

- `NpcInventoryCensusTests`: 7/7 with the outer membership reconciliation
  exercise, `EditMode-20260930-210124-5abc057da95f429cbd64e85dcf26997e.xml`.
- `SimulationBootstrapCompositionTests`: 14/14, `EditMode-20260930-204203-595e024e090d4074a1c04389c9c5c47a.xml`.
- ALL EditMode after the reconciliation test update: 2049/2049,
  `EditMode-20260930-210156-d10dd0089a78481a890267893d4755e8.xml`.
- Complete official Smoke after the reconciliation test update: 5/5,
  `EditMode-20260930-210325-f6a427f360e44a339cdd55b75febc38e.xml`.
- `git diff --check`: passed.

This candidate does not complete P12-B, change P12-A readiness, establish
global owner-thread/quiescence, connect direct InventoryRuntime writes to the
shared epoch, seal City composition, or add export/hydration/capture support.
