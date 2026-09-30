# Phase 12 State — Save & Deterministic Continuation

**Status:** PHASE 12 IN PROGRESS — P12-A WAIT_DEPENDENCY; P12-B INCOMPLETE
(PARTIAL FOUNDATION PROMOTED).

**Previously promoted cumulative P12-B owner-witness candidate tip:** passive
census stack `codex/phase12/P12EPropertyCensus` at
`b889b4747738d933fe48311ef89fc33a40e3dfa0`, fast-forwarded from canonical
`69f456d5e3c6d6f7e4b85b36e98968ced0549bf3`. Its code-bearing tree is
`65ebc7f7ab6dd834e2326ff426600483a74c162c`; exact integration review passed
at `35ec9887add812922908d1f402d02d5db3500d44`, and the final docs-only tip
check passed at `b889b47`. It retains the separately promoted
`RuntimeIdentityRegistry` witness at `033854452c1167e053f57076803819ffe3a16840`
and the earlier P8-A witness at
`644bdae8ded1d8a938ec380370966ca6c235b881`.

## Canonical refresh and current cumulative census candidate — 2026-09-30

P12 canonical and its remote are synchronized at
`676196bcd807603deb9d01bd2855342a7d47a01e`. This tip records the approved
promotion of the P12-D Genealogy saturated-rollback correction at
`5ef2615bb7d3de6280a2f7a6943a1669ead9002c`; the correction remains limited to
named-birth compensation at revision saturation. It does not complete P12-D.

The cumulative owner-witness candidate
`codex/phase12/P12BCensusOwnersCumulativeIntegration` retains code tip
`44fc3ab94c9666f656149f346fb2cc553d3cb689` and code tree
`b0be75370d32679d0745ed15d29ce359dada0bb6`, based on canonical `676196b`.
The exact code/design/integration review passed, required Unity evidence is
recorded in its candidate, and the exact integration documentation review
passed at `a7e078d0eb3e136cb64f58ef3e4879f66fb76450`. The State/candidate
refresh was independently revalidated at docs tip `4daa0f1`; the separate
SpatialNetwork design review passed at `11b4a27`. The candidate adds published
PersonStore, ExplorableSite, and per-City SettlementPopulation census
providers while retaining the promoted Genealogy provider. Canonical
promotion is still pending explicit approval.

This candidate remains a partial passive owner-census foundation. P12-B is
incomplete and P12-A remains `WAIT_DEPENDENCY`. The candidate does not provide
a complete effective-profile inventory, shared mutation-epoch coverage,
owner-thread/quiescence proof, capture eligibility, immutable exports, or
staged hydration. P12-D remains blocked on P12-B and P12-C. The next bounded
owner-inventory slice, the legacy `SpatialNetworkRuntime` location/route
census, has a reviewed technical design at
[`PHASE12_P12B_SPATIAL_NETWORK_CENSUS_DESIGN.md`](design/PHASE12_P12B_SPATIAL_NETWORK_CENSUS_DESIGN.md).
Its exact design review passed at `11b4a27`; it fits accepted P12-B/P12-D
scope and does not add a human checkpoint-acceptance gate; see the
[`design review record`](design/PHASE12_P12B_SPATIAL_NETWORK_CENSUS_REVIEW.md).
Implementation remains dependency-gated on releasing/re-integrating the cumulative
`SimulationBootstrapComposition.cs` hotspot against canonical. Neither this
design nor the cumulative census candidate changes P12-B readiness or
authorizes canonical promotion.

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
| P12-B — profile admission and completed-boundary lifecycle | `INCOMPLETE — PARTIAL FOUNDATION PROMOTED` | In addition to the promoted non-admitting kernel, receipt owners, P8-A–D, and RuntimeIdentity witnesses, cumulative stack `b889b4747738d933fe48311ef89fc33a40e3dfa0` adds passive witnesses for record sequence, ActorChoice, RuntimeIdAllocator, ArmedForce/manpower/position, Conflict/War/Battle, Estate/Property, and Institution/Office. The separately promoted P12-D GenealogyStore parentage witness is recorded below. See the promoted-stack section and linked candidate evidence below. The static writer map and partial profile evidence remain in `docs/design/PHASE12_B_BLOCKER_RESOLUTION.md`. No complete profile census, shared-epoch connection, Unity owner-thread/quiescence proof, or capture token is established. |
| P12-C — identity, provenance, deterministic roots | `BLOCKED_ON_P12-B` | The `RuntimeIdAllocator` passive census and record-sequence witness promoted at `b889b47` are inventory evidence only; they do not provide C exports/hydration, deterministic-root state, or provenance. Preserve `codex/phase12/P12CIdentityRuntimeSnapshot` at `531d835` for selective reintegration only after B readiness and revalidation. |
| P12-D — factual roots and Person/population relations | `BLOCKED_ON_P12-B_AND_C` | Owner inventory and accepted scope remain; no complete export/hydration capability is claimed. |
| P12-E — core and official daily-domain owners | `BLOCKED_ON_P12-B_AND_C` | Owner inventory and accepted scope remain; effective-profile provider coverage and exact owner exports are incomplete. |
| P12-F — Knowledge, directives, choices, commitments | `BLOCKED_ON_P12-C_D_E` | Accepted scope remains dependency-gated; no complete export/hydration capability is claimed. |
| P12-G — staged restore, graph validation, publication, parity | `BLOCKED_ON_P12-B_THROUGH_F` | No whole-graph staged restore or continuation-parity capability is claimed. |

Phase 12 remains open. P13 remains dependency-gated. This State does not claim
save/load support, P12-A readiness, P12-B readiness, Phase closure, or a P13
historical fork guarantee.

## Promoted RuntimeIdentityRegistry passive census witnesses

The reviewed candidate `codex/phase12/P12BRuntimeIdentityWitness` at
`033854452c1167e053f57076803819ffe3a16840` was approved and fast-forwarded to
`codex/phase12/canonical` on 2026-09-29 from canonical
`1ada62b031e738e2bdd5d3d623e028a114961d6e`. The independent exact-tip
implementation review record is `codex/phase12/P12BRuntimeIdentityWitnessReviewRecord`
at `c113f52`.

It adds eight schema-v1 passive sections for the existing typed runtime
identity indexes. Each section reports its live index count and shares the
installed registry's identity and monotone registration revision. The normal
bootstrap publishes the fixed provider collection without exposing the raw
registry or a general registration hook. Validation on the candidate passed:
`RuntimeIdentityCensusTests` 6/6, the selected authored bootstrap profile
1/1, ALL EditMode 1963/1963, complete official Smoke 5/5, and
`git diff --check`. XML evidence is recorded in
`docs/design/PHASE12_RUNTIME_IDENTITY_CENSUS_CANDIDATE.md`.

This remains a passive witness for one P12-C causal identity owner. It does
not add the RuntimeIdAllocator counters, shared mutation-epoch wiring,
owner-thread/quiescence enforcement, capture eligibility, or export and
hydration. P12-B remains incomplete and P12-A remains `WAIT_DEPENDENCY`.

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

## Promoted cumulative passive owner-witness extension

The cumulative owner-census candidate was approved and fast-forwarded from
P12 canonical `69f456d5e3c6d6f7e4b85b36e98968ced0549bf3` to
`b889b4747738d933fe48311ef89fc33a40e3dfa0`. Its code-bearing tree is
`65ebc7f7ab6dd834e2326ff426600483a74c162c`; the exact integration review
passed at `35ec9887add812922908d1f402d02d5db3500d44`, and the docs-only final
tip check passed at `b889b47`.

The promoted stack includes passive owner witnesses for:

- `SimulationRecordSequence`, `ActorChoiceStore`, and `RuntimeIdAllocator`;
- `ArmedForceStore`, contingent manpower, and armed-force spatial position;
- persistent Conflict, War, and Battle stores;
- Estate and Property ownership/transfer history; and
- Institution, Office, active incumbency, and retained tenure history.

Focused evidence remains in the linked candidate records under
`docs/design/PHASE12_*_CENSUS_CANDIDATE.md`. The final code tree passed ALL
EditMode `1985/1985`, the complete official `Smoke` filter `5/5`, and
`git diff --check`. This integrates the reviewed slices without closing P12-B
or changing the P12-C/D/E/F/G dependencies.

These are still passive per-owner witnesses. They do not register a complete
effective-profile census, prove every supported committed write reaches the
shared epoch, establish owner-thread/quiescence, issue capture eligibility,
or provide immutable exports and staged hydration. P12-B remains incomplete;
P12-A remains `WAIT_DEPENDENCY`.

## Promoted P12-D Genealogy parentage witness

The reviewed candidate branch `codex/phase12/P12DGenealogyCensusWitness` was
based on canonical `d0c2733994aaf51e417b7c9f49f2b3489c4c49c3` and approved for
promotion at branch tip `bde930477b7614a7fb1baed01497dbd1fe063927`. Canonical
was fast-forwarded to that tip. The code-bearing commit is
`3afbc593fad8648e5a2ae20b7ec1a9d988769fb9`; the intervening and final commits
only record the candidate design and validation evidence.

The promoted schema-v1 `p12d.genealogy.parentage` witness is bound to the
installed runtime `GenealogyStore` and counts direct parentage edges. Ordinary
public add/remove commits advance its local revision once and preflight
revision overflow before changing edges. The internal named-birth compensation
path may remove an edge at saturation without advancing the revision; this
strictly decreases cardinality while ordinary mutations are closed. Clone
construction retains the existing deterministic replay behavior and exposes
only the installed clone through the bootstrap composition.

Independent implementation review passed on the exact code tip and the
docs-only candidate update was reviewed. Validation passed: focused
`GenealogyCensusTests` 3/3, ALL EditMode 1988/1988, and the complete official
`Smoke` filter 5/5. Exact evidence paths and the detailed test boundary are in
`docs/design/PHASE12_P12D_GENEALOGY_CENSUS_CANDIDATE.md`.

This remains one passive, unsynchronized owner witness. It is not registered
in the complete P12-B profile inventory, does not connect writes to the shared
mutation epoch, and proves neither owner-thread/quiescence nor capture
eligibility. It adds no genealogy semantics, export, hydration, or P12-D
completion. P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P12-D
remains blocked on P12-B and P12-C.

The subsequent P12-D multi-owner commit-graph audit found a revision-saturation
rollback defect in the promoted code. The bounded correction's code commit is
`f631de8a9209956cf61d0f901867cca244befcaf` (tree
`ae029655824dd3ec6a73bc3c17a9bb2708014401`); it was independently reviewed
against exact parent `8f04a62bf2e66995efe8f311f5d62cff191acb11` (based on
P12 canonical `543196a`) and validated (focused named-birth 15/15, Genealogy
census 4/4, ALL EditMode 1990/1990, complete official Smoke 5/5, diff-check).
The reviewed correction candidate at `5ef2615bb7d3de6280a2f7a6943a1669ead9002c`
was approved and fast-forwarded to P12 canonical. The promoted Genealogy slice
now includes the saturated named-birth rollback repair. This does not change
P12-B/P12-A readiness or close P12-D.

## Remaining dependency-ordered P12-B blockers

The static successful-writer inventory is recorded; proving every included
commit reaches the shared invalidation epoch remains open. The selected live
day-zero test covers only a subset of owners and does not prove evolved
cardinality, owner-thread identity, or quiescence. Causal C roots, factual D,
official E, and commitment F owners still need exact witness providers and
supported-writer coverage.

P8-A through P8-D owner witnesses and the RuntimeIdentityRegistry witness are
now promoted. This adds positive day-zero P8-A geography cardinalities while
P8-B/C/D and registry witnesses cover their separate sections. Remaining
causal C roots, factual D, official E, and commitment F owners still need exact
witness providers and supported-writer coverage. The remaining committed-write invalidation and
owner-thread/quiescence blockers are unchanged. Phase 12 remains open and no
P12-A implementation authorization is implied.
