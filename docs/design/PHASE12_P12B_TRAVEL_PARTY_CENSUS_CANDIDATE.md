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

The code-bearing candidate for fresh exact-tip review is
`8c446ed8920028566b8af84583e7599bc907791c` (tree
`bf95798acdf932a91d54f52c8604ba20928f2cd0`), based on the SHA above. It adds
the reviewed return-association compensation exercise and direct rejected/no-op
witness checks. The return-association test uses the existing public
`ExpeditionRuntime.TryComplete` lifecycle writer concurrently with
`ExpeditionSystem.TryBeginReturn`; it forces the association failure after the
TravelParty Add, then asserts only the store's Add+Remove count/revision result.
The test passed in the focused Expedition suite on repeated runs. It does not
claim cross-owner Expedition, NPC, or cost rollback.

Validation ran on this exact code-bearing tree:

- `TravelPartyCensusTests`: 10/10 —
  `Temp/ValidationResults/EditMode-20260930-221423-8e87ca97cba34a13bf9369dcb2144413.xml`
- `GroupTravelTests`: 32/32 —
  `Temp/ValidationResults/EditMode-20260930-221516-1cfb95e3a92540368f5ea600167f9bb3.xml`
- `ExpeditionTests`: 14/14 —
  `Temp/ValidationResults/EditMode-20260930-221500-9920edfa08ba4773b1f4885ac0b76fa1.xml`
- `SimulationBootstrapCompositionTests`: 14/14 —
  `Temp/ValidationResults/EditMode-20260930-221532-c80af612fcbf441792b4193ea395c8b0.xml`
- ALL EditMode: 2061/2061 —
  `Temp/ValidationResults/EditMode-20260930-221550-3050e57238034222a75c2913a83b7573.xml`
- Complete official `Smoke`: 5/5 —
  `Temp/ValidationResults/EditMode-20260930-221627-2af6ba476b074328855384625b167069.xml`
- `git diff --check`: passed.

Independent exact-tip code review is pending. This branch is not a canonical
promotion.

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
