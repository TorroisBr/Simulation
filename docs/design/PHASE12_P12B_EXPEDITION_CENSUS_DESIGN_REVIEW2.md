# P12-B Expedition census design revalidation

**Result: PASS — implementation may begin within the accepted P12-B capability scope.**

Reviewed candidate: `codex/phase12/P12BExpeditionCensusDesign` at
`3a862b0980fff0dc26ab90f7bd6c068876e14964`.

Exact current P12 canonical review base:
`19d0373d6a71b63536248ecc9091e66c9b3a708b`.

The design's original evidence-base note names the earlier `e9ced8e` baseline.
That note is historical provenance, not a current compatibility claim; this
review re-read the complete design against the current canonical State,
PHASE12 brief and capability decomposition, blocker-resolution inventory,
canonical Expedition/TravelParty/bootstrap code, and both architecture
alignment records. No design change was needed after the TravelParty promotion.

## Review findings

- **Owner and cardinality:** The proposed fixed Required section counts active
  Expedition instances from the exact `ExpeditionStore` owner and reports that
  owner's revision. It validates list/index/object identity and fails closed
  on drift. It does not derive truth from events or participant/party state.
  Exact-object completion removes and transitions the same active row in one
  owner commit.
- **Current TravelParty contract:** The promoted P12 witness counts party
  instances, not member NPCs, and owns its own revision. The Expedition design
  preserves that separation. It does not turn a party/expedition into a
  participant count or claim a cross-owner transaction. Its prescribed
  `TravelPartyStore` → `ExpeditionStore` acquisition order matches the
  canonical return coordinator; the Expedition monitor is released around
  external-owner work and per-object fencing protects the in-flight row.
- **Start and return windows:** The new exact-object fences close the
  Add-to-TravelParty and Returning-to-party-association race windows. Two
  reserved revision slots cover the first owner commit and either its matching
  second commit or explicit compensation. Fences and unused reservations are
  released on all success, failure, and exception exits. Other expeditions
  remain independently mutable subject to the shared reserved-capacity
  accounting. The design does not misstate these as global rollback.
- **Capacity and interleavings:** Saturation is preflighted before external
  effects. Deterministic tests cover one-slot rejection, exactly-two-slot
  interleavings at both start and return boundaries, ordinary-capacity
  competing exact-object writes, success and compensation, reservation
  consumption/leaks, and retry after fence release. Objective-completion
  reservations separately pin the matching incomplete objective across
  retrieval/opposition effects and protect against competing writes without
  holding the Expedition monitor across external owners.
- **Runtime mutation coverage:** Attached runtime/objective leaf methods,
  traversal/progress, arrival, exact completion, autonomous consumers,
  retrieval/opposition completion, compensation, and direct store changes are
  covered by one serialized Expedition owner boundary. Revisions follow actual
  committed owner mutations, including distinct compensation writes; semantic
  no-ops do not advance. Bootstrap composition binds the provider to the exact
  installed store and system.
- **Temporal and participant alignment:** The design adds no intraday
  timeline state, ordering, or P18 dependency; the selected P12 daily profile
  excludes those facts. Expedition identity is separate from participant
  identity, and cardinality is the number of expedition instances. The design
  neither asserts one activity per actor nor establishes permanent
  Activity-to-Actor ownership, so it remains compatible with the P20
  multi-participant alignment. Participant IDs remain the current runtime
  bindings and are not presented as a new persistent membership contract.
- **Scope and readiness:** This is a passive P12-B owner witness and bounded
  owner-mutation closure. The explicit limits correctly leave global
  invalidation epoch coverage, runtime-wide owner-thread/quiescence, full
  profile inventory, capture eligibility, exports/hydration, P12-B completion,
  and P12-A readiness unresolved. It adds no product semantics or later-phase
  gameplay scope.

The contract is technically sufficient for an isolated P12-B implementation
to start under the previously accepted capability authorization. Implementation
must retain the specified lock order and must serialize the shared
`ExpeditionSystem` / `ExpeditionStore` hotspot with other current candidates;
that is an execution/integration constraint, not a design blocker. This review
does not itself authorize canonical promotion or claim implementation delivery.

## Evidence checked

- `docs/PHASE12_STATE.md` at exact review base `19d0373`.
- `docs/phases/PHASE12_BRIEF.md` and
  `docs/design/PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md` at the same tip.
- `docs/design/PHASE12_B_BLOCKER_RESOLUTION.md`, including current TravelParty
  owner/cardinality evidence and remaining P12-B blockers.
- `docs/architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md` and
  `docs/architecture/MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md`.
- Full candidate design at `3a862b0`, including its current start/return
  interleaving amendments.
- Canonical `ExpeditionSystem`, `ExpeditionStore`, `ExpeditionRuntime`, and
  `SimulationBootstrapComposition` at `19d0373`.

No Unity tests were run for this documentation-only independent review.
