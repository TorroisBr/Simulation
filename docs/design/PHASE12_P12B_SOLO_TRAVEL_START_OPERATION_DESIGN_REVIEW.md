# P12-B selected-profile solo travel-start operation design review

**Verdict:** `PASS` — ready for bounded implementation within the previously
accepted and authorized P12-B capability scope. This review is not a code
review, canonical promotion, P12-B completion, P12-A readiness, P13 readiness,
or Phase closure.

**Independent reviewer:** Luna design reviewer (`solo_travel_design_review`).

**Canonical base:** `e76e50bfefc68b827d54ceb74e0b25293e0ad7b8`.

**Exact design tip reviewed:**
`91728e4aee7717c6b002691a9a9e96c4ad22ac71`.

**Design:**
[`PHASE12_P12B_SOLO_TRAVEL_START_OPERATION_DESIGN.md`](PHASE12_P12B_SOLO_TRAVEL_START_OPERATION_DESIGN.md).

## Review findings

The final design matches the current selected-profile source path and limits
the operation to the existing solo `TravelActionProvider` invocation after
the action success roll. The owner set is bounded to the exact installed NPC
account, travel-state and travel-plan owners; paired SpatialKnowledge
sections; conditional source-City presence; the selected RuntimeIdAllocator
Event counter; and SimulationRecordSequence. Destination City presence,
TravelParty, Inventory, Market, unrelated allocator counters, and
DomainEventStore are correctly excluded.

The design preserves owner-thread, identity/cardinality, baseline and
mutation-epoch-capacity preflight; batches current owner callbacks once on
operation close; retains the existing nested daily operation; and preserves
charge/compensation, post-start event failure, and partial-commit semantics.
The review caught and corrected an initial mistaken claim that travel
charge/restore already entered `runtime.economy.money-transfer`; canonical
source calls `MoneyAccount.TryDebit`/`TryCredit` directly. The final design
does not introduce a synthetic child operation. No remaining contract or
owner-coverage mismatch was found.

The required test plan covers exact owner identities/cardinality/baselines,
operation nesting, successful batching, compensation, refusal/no-op,
event-failure and partial-commit behavior, scheduled-action routing, and
scope exclusions. `git diff --check` passed at the exact design tip.

## Revalidation boundary

Implementation must start from a freshly verified P12 canonical base and
revalidate the source assumptions. A changed code tree requires affected
focused/full validation and a fresh exact-tip implementation review. P12-B
remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked.
