# P12-B TravelParty census candidate

**Status:** Implementation candidate; not promoted. P12-B remains incomplete
and P12-A remains `WAIT_DEPENDENCY`.

**Base:** canonical `codex/phase12/canonical` at
`e9ced8e451f42e80ed2132ce494cd5c26e439894`.

**Reviewed design:**
`docs/design/PHASE12_P12B_TRAVEL_PARTY_CENSUS_DESIGN.md` at
`765e346214e128104ce18c967121ef29abb88195`; durable design review branch
`codex/phase12/P12BTravelPartyCensusDesignReview` at
`194661af6a1d7e9c901a17f35ce2dc7ff4d8fdf8`.

## Delivered candidate slice

The candidate adds the fixed passive `p12f.travel-parties` schema-v1 witness
for the exact `TravelPartyStore` installed in bootstrap. The owner now exposes
a local monotone revision. Successful public Add, Complete, and Remove commits
advance it once; no-op, rejected, guard-denied, and saturated operations do not
change the owner or revision. Revision capacity is checked before writes.

A private reentrant store monitor serializes those public mutations and the
compound TravelParty start, daily advance/completion, and Expedition return
windows. Start reserves capacity for Add plus its possible compensating Remove
before travel preparation or non-store effects. Final arrival checks the
completion capacity before advancing any member. Bootstrap publishes a fixed
provider backed by its already-installed store. `ExpeditionSystem` rejects a
split TravelParty owner and holds the same store window across return start,
association, and compensation.

Focused tests cover witness identity and counts, Add/Complete/Remove revision
semantics, capacity saturation, start-event compensation, saturated arrival,
cross-thread blocking for each public mutation, ordinary start/arrival, normal
selected-profile bootstrap identity, and split-store rejection.

## Validation

Validation ran on the candidate worktree based on the SHA above:

- `TravelPartyCensusTests`: 9/9 —
  `Temp/ValidationResults/EditMode-20260930-220047-0adab0a11cfc4d26a2e651afab22c501.xml`
- `GroupTravelTests`: 32/32 —
  `Temp/ValidationResults/EditMode-20260930-215906-7afe36c28f504aaabfb2e0dc1bf5be9f.xml`
- `ExpeditionTests`: 13/13 —
  `Temp/ValidationResults/EditMode-20260930-220106-9505d943b06d46b5bfe186fdf47be794.xml`
- `SimulationBootstrapCompositionTests`: 14/14 —
  `Temp/ValidationResults/EditMode-20260930-215939-28423f519bd64debbbed7f276aee947b.xml`
- ALL EditMode: 2059/2059 —
  `Temp/ValidationResults/EditMode-20260930-220124-eb51e8ae21ef479e9b43d9e244da6208.xml`
- Complete official `Smoke`: 5/5 —
  `Temp/ValidationResults/EditMode-20260930-220206-9780518c95884d42ad41f7f316640ad5.xml`
- `git diff --check`: passed.

The final exact candidate SHA and independent code-review record are appended
after the candidate is committed. This branch is not a canonical promotion.

## Scope limits

The passive witness is unsynchronized; it does not establish owner-thread
identity, quiescence, shared-epoch invalidation, a complete profile census, or
capture eligibility. The candidate does not implement P12-F export/hydration,
snapshot or graph validation, or continuation parity. The mutation window only
protects TravelParty membership and revision; it does not make Expedition,
NPC, cost, or runtime state generally thread-safe. Return-association
compensation asserts only TravelParty store count/revision; the existing
Expedition path does not roll back NPC travel fields or costs after a nested
party start has succeeded. Arbitrary same-thread callbacks that re-enter the
store during a compound operation remain outside the supported path.

P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, and no later P12
checkpoint becomes ready solely from this passive owner witness.
