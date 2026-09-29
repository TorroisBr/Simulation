# Phase 12 State — Save & Deterministic Continuation

**Status:** PHASE 12 IN PROGRESS — P12-A WAIT_DEPENDENCY; P12-B INCOMPLETE
(PARTIAL FOUNDATION PROMOTED).

**Latest promoted P12-B candidate tip:** reviewed P8-A passive geography
witness candidate `644bdae8ded1d8a938ec380370966ca6c235b881`, approved and
fast-forwarded to `codex/phase12/canonical` from `06145c7cbc258c56cc1be1a24adaa1751d32bc01`.
Its code-bearing tip is `9efba61`; the independent exact-tip review record is
branch `codex/phase12/P12BP8APopulatedReviewRecord` at `a401860`. The prior
P8-D candidate was `d92fdfb6b5ceb517c210be7cea5faab52ebb5641`.

**Architecture baseline:** `c285466c355103d3637ac165246591b72eb7bda0`, with
the intraday/extensibility alignment at `4b6dd1d38cffeaf3cc1ac3effea0f8ede8771194`
and the multi-participant activity alignment at
`c285466c355103d3637ac165246591b72eb7bda0`.

**Planning authority:** `docs/phases/PHASE12_BRIEF.md` and the accepted
P12-B–P12-G capability decomposition in
`docs/design/PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md`.

## Checkpoint status

| Checkpoint | Status | Current evidence and limits |
|---|---|---|
| P12-A — `UnityBootstrap-Daily-v1` profile integration | `WAIT_DEPENDENCY` | Scope accepted. No included-owner export plus staged-hydration coverage or validated complete live profile inventory exists yet. Its separate implementation authorization remains outstanding. |
| P12-B — profile admission and completed-boundary lifecycle | `INCOMPLETE — PARTIAL FOUNDATION PROMOTED` | The reviewed non-admitting census kernel and exact receipt-owner witnesses were promoted at `9da25b47ab0bc0f6a1a10032dfceaad05f6316e6`; passive P8-C City/Site binding and Person-position witnesses at `481358d1f8967d1c0199370601597c329fce69b2`; P8-B passage/barrier/crossing witnesses at `04d39b23b8509609dcd96990a214922dc0220e8b`; P8-D route-observation/plan-history witnesses at `d92fdfb6b5ceb517c210be7cea5faab52ebb5641`; and P8-A populated Hex/Location/scale-context witnesses at `644bdae8ded1d8a938ec380370966ca6c235b881`. The static C/D/E/F writer map and partial selected-profile day-zero evidence are recorded in `docs/design/PHASE12_B_BLOCKER_RESOLUTION.md`. These promotions do not register a complete profile census, connect committed writes to the shared epoch, prove Unity owner-thread/quiescence, or issue a capture token. |
| P12-C — identity, provenance, deterministic roots | `BLOCKED_ON_P12-B` | Preserve `codex/phase12/P12CIdentityRuntimeSnapshot` at `531d835` for selective reintegration only after B promotion and revalidation. It does not satisfy B or provide the remaining C root witnesses. |
| P12-D — factual roots and Person/population relations | `BLOCKED_ON_P12-B_AND_C` | Owner inventory and accepted scope remain; no complete export/hydration capability is claimed. |
| P12-E — core and official daily-domain owners | `BLOCKED_ON_P12-B_AND_C` | Owner inventory and accepted scope remain; effective-profile provider coverage and exact owner exports are incomplete. |
| P12-F — Knowledge, directives, choices, commitments | `BLOCKED_ON_P12-C_D_E` | Accepted scope remains dependency-gated; no complete export/hydration capability is claimed. |
| P12-G — staged restore, graph validation, publication, parity | `BLOCKED_ON_P12-B_THROUGH_F` | No whole-graph staged restore or continuation-parity capability is claimed. |

Phase 12 remains open. P13 remains dependency-gated. This State does not claim
save/load support, P12-A readiness, P12-B readiness, Phase closure, or a P13
historical fork guarantee.

## Promoted P12-B partial-foundation evidence

The candidate branch `codex/phase12/P12BCoordinatorReviewFix` was based on
the previous canonical SHA `04105d31e88fca97888dddb8e974236a7f4b6804`.
Its code-bearing tree was `a67beacf5aad9da11a070eae48a45fcd50ffb44b`; the
reviewed promotion tip is `9da25b47ab0bc0f6a1a10032dfceaad05f6316e6`.
Canonical promotion was approved and completed at `9da25b47ab0bc0f6a1a10032dfceaad05f6316e6`. It includes:

- a versioned owner-section census protocol with exact section role, owner
  identity, schema, cardinality, revision/snapshot stamp, fail-closed owner
  coverage, serialized owner-thread binding, operation accounting, atomic
  changed-owner batch validation, and a monotonically advancing mutation epoch;
- exact owner-issued census witnesses for the conditional P18 decision
  occurrence-receipt and economy keyed-sale-receipt ledgers, including retained
  terminal/preflight-failure receipts, same-cardinality replacement,
  replay, and collision behavior;
- the selected-profile day-zero census evidence and the bounded post-promotion
  P8-C witness-adapter work package in the blocker-resolution plan.

Independent implementation review passed on code tip
`dbe3db08c54c7380f26c89b7ee07e0742730d95b` against the previous canonical
base. Independent review of the P8-C dependency update passed on exact tip
`a67beacf5aad9da11a070eae48a45fcd50ffb44b`; the final State and test-plan
wording were also reviewed on the promoted exact tip. These reviews retain the
limitations listed above.

Validation on the exact code-bearing candidate tree `a67beacf5aad9da11a070eae48a45fcd50ffb44b` passed: ALL EditMode `1952/1952`
(`Temp/ValidationResults/EditMode-20260929-191720-bf896e633b224549a5bc349eed4e2908.xml`),
official complete Smoke `5/5`
(`Temp/ValidationResults/EditMode-20260929-191810-0384b2eb5dcf452ca01f484050aebe82.xml`),
and `git diff --check`. The tested code-bearing tree and promoted tip differ
only by reviewed documentation commits. This is a reviewed, promoted
non-admitting foundation, not a completed P12-B checkpoint.

## Promoted P8-A populated geography census witnesses

The P8-A passive witness candidate was based on canonical
`06145c7cbc258c56cc1be1a24adaa1751d32bc01` and, with approval, fast-forwarded
to `codex/phase12/canonical` at `644bdae8ded1d8a938ec380370966ca6c235b881`.
Its code-bearing tip is `9efba61`; independent exact-tip implementation
review passed on `644bdae`. The durable review record is branch
`codex/phase12/P12BP8APopulatedReviewRecord` at `a401860`.

It adds three schema-v1 passive witnesses backed by the installed runtime
`SpatialAuthorityStore`: `p8a.hexes` reports `HexCount`, `p8a.locations`
reports `LocationCount`, and `p8a.scale-context` reports `HasGeography ? 1 : 0`.
Each reports the store's shared `Revision`. The selected profile verifies the
populated day-zero values 1/1/1 at revision 1 and the installed owner identity.
A separate temporal test confirms that successful barrier registration
advances the shared revision from 1 to 2 while these cardinalities remain
7/1/1 in its multi-Hex fixture.

Validation on the code-bearing tree passed: `SpatialGeographyTests` 15/15,
`SimulationBootstrapCompositionTests` 14/14, ALL EditMode 1957/1957, official
complete Smoke 5/5, and `git diff --check`. Exact result paths and scope limits
are recorded in `docs/design/PHASE12_P8A_CENSUS_CANDIDATE.md`; independent
review is recorded in `docs/design/PHASE12_P8A_CENSUS_REVIEW.md` on its review
branch.

These unsynchronized providers remain passive. They are not registered in a
complete profile census, do not connect spatial writes to the shared mutation
epoch, do not prove owner-thread/quiescence, and do not grant capture
eligibility. P12-B remains incomplete and P12-A remains `WAIT_DEPENDENCY`.

## Promoted P8-C passive census witnesses

The P8-C passive witness candidate was based on canonical
`a43316858b7006c624ed1a950097210ae55e95f7` and promoted to
`codex/phase12/canonical` at `481358d1f8967d1c0199370601597c329fce69b2`.
Independent exact-tip review passed. It adds schema-v1 owner witnesses for
`p8c.city-site-location-bindings` and `p8c.person-positions`, using the
published runtime's installed `LegacySpatialAnchorBindingStore` and
`PersonSpatialPositionStore` references as identity and their exact local
count/revision values.

Focused `PersonSpatialPresenceTests` passed 9/9 and
`SimulationBootstrapCompositionTests` passed 14/14. ALL EditMode passed
1953/1953 and official complete Smoke passed 5/5. Result XMLs are recorded in
`docs/design/PHASE12_P8C_CENSUS_CANDIDATE.md` and retained in the candidate
worktree under `Library/ValidationResults/P12BP8C`.

These unsynchronized providers remain passive. They are not registered in the
incomplete profile census, do not connect writes to the shared mutation epoch,
do not prove owner-thread/quiescence, and do not grant capture eligibility.
P12-B remains incomplete and P12-A remains `WAIT_DEPENDENCY`.

## Promoted P8-B passive census witnesses

The P8-B passage and crossing candidate was based on canonical
`4d48a5f88145d748c32c8dc42ab251dcb04f81e4` and promoted to
`codex/phase12/canonical` at `04d39b23b8509609dcd96990a214922dc0220e8b`.
Independent exact-tip review passed. It adds schema-v1 witnesses for
`p8b.passage-option-barrier-state` and `p8b.crossings`. The passage witness
uses the installed `SpatialPassageAuthority` child as identity, checked
`Options.Count + Barriers.Count` as cardinality, and its parent's revision as
the conservative stamp. The crossing witness uses the installed
`SpatialAuthorityStore` identity, `CrossingCount`, and that same revision.
Crossings projected into `OptionStates` are not double-counted as passage
membership.

Validation on the code-bearing commit `e77d671df7240c71c885004110fa096a9ed67802`
passed: `SpatialPassageAuthorityTests` 13/13,
`SimulationBootstrapCompositionTests` 14/14, ALL EditMode 1954/1954, official
complete Smoke 5/5, and `git diff --check`. XML paths and scope limits are
recorded in `docs/design/PHASE12_P8B_CENSUS_CANDIDATE.md`.

These unsynchronized providers remain passive. They are not registered in the
profile census, do not connect writes to the shared mutation epoch, do not
prove owner-thread/quiescence, and do not grant capture eligibility. P12-B
remains incomplete and P12-A remains `WAIT_DEPENDENCY`.

## Promoted P8-D passive census witnesses

The P8-D route-owner witness candidate was based on canonical
`c7ff9fd9f4245c3f8968b9196a4c20de19dd0fb2` and promoted with approval to
`codex/phase12/canonical` at candidate tip
`d92fdfb6b5ceb517c210be7cea5faab52ebb5641`. Its code-bearing commit is
`f0575ef43a77898aae8fb8565d4b709b850a46d8`. Independent exact-tip
implementation review passed on candidate tip `d92fdfb`; the durable review
record is branch `codex/phase12/P12BP8DReviewRecord` at
`934741142cd74e06d2c284af45a62a068fa3d9de`.

It adds schema-v1 passive witnesses for:

- `p8d.spatial-route-observations`, using the installed runtime
  `SpatialRouteKnowledgeStore`, `ObservationCount`, and local `Revision`;
- `p8d.person-route-plan-history`, using the installed runtime
  `PersonRoutePlanStore`, `PlanCount` for all retained rows, and local
  `Revision`.

Validation on the code-bearing tree passed: `SpatialRoutePlanningTests`
21/21, `SimulationBootstrapCompositionTests` 14/14, ALL EditMode 1955/1955,
official complete Smoke 5/5, and `git diff --check`. Exact XML paths are
recorded in `docs/design/PHASE12_P8D_CENSUS_CANDIDATE.md` and remain in the
candidate worktree under `Library/ValidationResults/P12BP8D`.

The profile test confirms day-zero exact-zero values and installed-owner
identity. Mutation tests confirm new observations, replay/conflict behavior,
plan-history append/stale rejection, and P8-E status changes that advance the
plan revision without adding a history row. These reads remain unsynchronized
and passive. They are not registered in a complete profile census, do not
connect writes to the shared mutation epoch, do not prove owner-thread or
quiescence, and do not grant capture eligibility. P12-B remains incomplete;
P12-A remains `WAIT_DEPENDENCY`.

## Remaining dependency-ordered P12-B blockers

The static successful-writer inventory is recorded; proving every included
commit reaches the shared invalidation epoch remains open. The selected live
day-zero test covers only a subset of owners and does not prove evolved
cardinality, owner-thread identity, or quiescence. Causal C roots, factual D,
official E, and commitment F owners still need exact witness providers and
supported-writer coverage.

P8-A through P8-D owner witnesses are now promoted. This adds positive
day-zero P8-A geography cardinalities while P8-B/C/D witnesses cover their
separate child sections. Causal C roots beyond P8-A, factual D, official E,
and commitment F owners still need exact witness providers and supported-writer
coverage. The remaining committed-write invalidation and
owner-thread/quiescence blockers are unchanged. Phase 12 remains open and no
P12-A implementation authorization is implied.
