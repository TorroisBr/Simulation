# P12-B Dynamic NPC Inventory census implementation review

**Result:** PASS at exact candidate tip
`15e5a543f3896c02f9dbd9c36c9ba75bc90dac2b`.

- **Implementation branch:** `codex/phase12/P12BNpcInventoryCensus`
- **Exact candidate/code tip:** `15e5a543f3896c02f9dbd9c36c9ba75bc90dac2b`
- **Exact implementation base:** `f961477380508647d2975272da72d96892944ed9`
- **Current canonical branch at review:** `codex/phase12/canonical` at
  `f961477380508647d2975272da72d96892944ed9`
- **Accepted design contract:** `f3c7edee28968b6af0020c09a08fc0ab8740fe64`

## Review findings

The complete committed diff was reviewed against the exact canonical base,
including the census protocol, InventoryRuntime and NpcRuntime accessors,
provider composition, SimulationRuntime registration, tests, and candidate
evidence. The implementation publishes one schema-v1 `p12f.inventory/<RuntimeId>`
section for each currently rostered NPC, ordered ordinally and bound to the
exact NpcRuntime and InventoryRuntime instances. Its cardinality read does not
initialize absent inventory rows; its revision witnesses same-row value
changes as well as row-count changes.

Inventory sections are staged with the existing dynamic SpatialKnowledge
family and affected fixed PersonStore sections in the outer NPC membership
operation. The final protocol guard faults if an already registered section's
NpcRuntime or InventoryRuntime identity changes; it does not rebase that
section. A new owner is seeded only when its section is absent, as after an
explicit unregister followed by a separate same-ID registration. Drift or
malformed/missing owners fail the census without publishing partial family
state, and one successful combined family change increments the shared partial
census epoch at most once.

The first exact-tip review at `1ca59e4f477b3e681fa7746c765aa2ce292e957c`
returned NEEDS_CHANGES because the staging path accepted an existing section
with a replacement owner. The implementation added the existing-section
identity guard. The final candidate tip `15e5a54` also adds the decisive
regression: it replaces a rostered NPC's InventoryRuntime, registers an
unrelated NPC to drive normal membership reconciliation, and verifies the
census becomes faulted while retaining the prior provider snapshot and owner.
The domain registration reports its insertion result; subsequent census
assessment fails closed. This is a passive census boundary, not a transaction
rollback or capture-admission guarantee.

Tests also confirm separate unregister/re-register operations produce distinct
owner sections and epoch changes, dead/emigrated roster members retain their
inventory section, direct unnotified inventory revision drift fails closed,
revision/cardinality behavior, ordinal ordering, and non-materializing reads.
The targeted City test preserves the existing boundary: inventory census may
still describe the roster after unregister, while the separate City presence
provider rejects a stale `ImportantNpcs` projection. A `StartingCity` outside
the composed City set remains outside this witness and full-profile admission
remains blocked; no City cleanup or composition behavior is added.

No blocking correctness or scope issue remains in this candidate. It does not
connect direct InventoryRuntime writers to the shared epoch, prove global
runtime owner-thread affinity or quiescence, or expand census coverage to the
full selected profile.

## Validation evidence

The retained Unity XMLs and logs were inspected and the XML counts/results
parsed as passed:

- `NpcInventoryCensusTests`: 7/7,
  `Library/ValidationResults/P12BNpcInventoryCensus/EditMode-20260930-210124-5abc057da95f429cbd64e85dcf26997e.xml`.
- `SimulationBootstrapCompositionTests`: 14/14,
  `Library/ValidationResults/P12BNpcInventoryCensus/EditMode-20260930-204203-595e024e090d4074a1c04389c9c5c47a.xml`.
- ALL EditMode: 2049/2049,
  `Library/ValidationResults/P12BNpcInventoryCensus/EditMode-20260930-210156-d10dd0089a78481a890267893d4755e8.xml`.
- Complete official `Smoke` filter: 5/5,
  `Library/ValidationResults/P12BNpcInventoryCensus/EditMode-20260930-210325-f6a427f360e44a339cdd55b75febc38e.xml`.
  The matching log confirms `-testFilter Smoke` and a successful Unity test-run exit.
- `git diff --check` passed on the committed base-to-candidate diff.

The candidate worktree's unrelated generated ProjectSettings changes and
ArmedForce `.meta` files are not included in the candidate diff.

## Limits and verdict boundary

This review approves only the submitted partial passive NPC Inventory census
candidate. P12-B remains incomplete and P12-A remains `WAIT_DEPENDENCY`. The
candidate does not complete the live profile owner inventory, committed-write
invalidation, City composition sealing/drift detection, global
owner-thread/quiescence proof, capture eligibility, or any export/hydration
capability. Canonical promotion and Phase closure are not approved by this
review.
