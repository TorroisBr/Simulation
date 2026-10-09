# P12-G current-base owner/cardinality reconciliation — 2026-10-09

**P12 canonical baseline:** `448d3583a85730a7db9531e61c6b6fbaaeaf6430`

**Architecture canonical:** `47eff220c7ce00f6e7c759bdc2b76780bb46f628`

**Profile:** accepted `UnityBootstrap-Daily-v1`, with `SampleScene` selecting `Simulation-DailyV1.asset`.

**Status:** bounded source revalidation of the existing 299-row manifest and 24-operation matrix. This does not certify the complete live graph or change readiness.

## Drift since the last family reconciliation

The preceding family reconciliation is based on P12 canonical
`564553f92737ddd9d190bc16ac84c4894e35913a`. Comparing its `Assets/_Project`
tree with `448d358` shows only three changed source/test files:

* `Assets/_Project/Scripts/SimulationRuntime.cs`, changed by the promoted
  City-bound materialization fix `11653ebd3947220a39d785c0c0641837e5cfadbe`;
* `Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs`,
  which adds the corresponding dynamic owner witness;
* `Assets/_Project/Tests/EditMode/Editor/P12CrimeSocialAppraisalInvalidationTests.cs`,
  which first adds the selected Daily-v1 ingress witness at
  `d859d0f36a34163a2f7dd46cbc77f7d859e46d73` and later adds the exact
  Crime/Social composite epoch assertion at
  `33ce3026297a372363624065d4f3240c9b49b44c`.

No other `Assets/_Project` file changed over that interval. The existing
299-row equation and 24-operation IDs remain current. The older 275/278 row
counts and 23-operation list remain historical and are not mixed into this
refresh.

## Revalidated row transitions

### City NPC-presence owner

For `p12b.city.important-npcs/{CityRuntimeId}`, the exact owner remains the
selected `CityRuntime`; cardinality is `ImportantNpcs.Count` and local
revision is `ImportantNpcRevision`. On successful Person-bound NPC
materialization into a non-null `startingCity`, `SimulationRuntime` marks that
City presence row changed inside the existing `runtime.npc-membership`
operation. The focused witness checks the provider owner is the same City
instance, cardinality and revision each advance by one, and the shared epoch
advances by one alongside Person/NPC membership reconciliation. The retained
tests and hashes are listed in
[`../validation/P12GCityBoundMaterialization/VALIDATION.md`](../validation/P12GCityBoundMaterialization/VALIDATION.md).

This closes that selected City-bound materialization transition only. It does
not cover every City roster path or every possible City count/identity change.

### Crime/Social appraisal owners

The row family remains three distinct child owners: `TheftOutcomeStore`,
`CrimeKnowledgeStore`, and `SocialReactionStore`, each using its own
`P12CensusRevision`. The selected authored Daily-v1 Steal action reaches the
existing `TryAcceptTheftOutcome` composite while the registered
`runtime.advance-day` operation is active. The exact-tip test at
`33ce302` reads the shared epoch immediately before and after that completed
composite, requires both reads to succeed, and asserts an exact `+1` delta.
Focused, ALL EditMode, and official Smoke results and artifact hashes are
retained in
[`../validation/P12GCrimeSocialRuntimeOperation/VALIDATION.md`](../validation/P12GCrimeSocialRuntimeOperation/VALIDATION.md);
the independent review is
[`PHASE12_G_CRIME_SOCIAL_EXACT_EPOCH_REVIEW_33CE302.md`](PHASE12_G_CRIME_SOCIAL_EXACT_EPOCH_REVIEW_33CE302.md).

This establishes the normal selected-profile composite's enclosing operation
and its own epoch contribution. It does not assert the total epoch delta for a
full day. The focused unit tests' direct composite calls do not, by themselves,
establish another normal Daily-v1 input path or registered operation.
Standalone composite calls on the bound owner thread remain within the existing
P12-B synchronous owner-thread contract and notify the shared epoch. Their
caller-quiescence relationship at a G capture remains part of the broader
runtime-wide proof. No additional operation ID or production invalidation
scope is inferred.

## Remaining inventory and readiness boundary

The 299-row manifest still independently compares expected section IDs with
protocol expected/registered sets and checks role, schema, non-null and stable
owner identity, cardinality, revision, repeated reads, and explicit-empty values. The
selected roster/materialization tests cover some dynamic NPC and Person
transitions. The row-family reconciliation remains useful source mapping, but
does not prove that every provider resolves to the correct live bootstrap
owner across all supported states, all fixed-row owner identities, all City
transitions, or every successful writer and epoch edge.

The next bounded evidence task is a source-linked fixed-row owner identity and
cardinality audit against the live bootstrap, followed by the remaining
dynamic/conditional rows and successful-writer dispositions. Any unproven
edge stays an evidence gap; it is not permission to add speculative mutation
wiring. The separate current gates remain: fresh target-owner checks,
target-bound restored-boundary admission, one active-session publication
owner/swap, and whole-graph rejection, failure-atomicity, no-replay, and
continuation-parity evidence.

P12-G and P12-A remain `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12
remains `OPEN`. This reconciliation creates no checkpoint, implementation
authorization, capture eligibility, export/hydration readiness, or closure
claim. P12-C and P12-B retain their existing completed scopes.
