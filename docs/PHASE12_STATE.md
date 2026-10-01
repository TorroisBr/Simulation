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

## Cumulative census promotion and current readiness — 2026-09-30

The approved cumulative P12-B passive owner-census candidate
`codex/phase12/P12BCensusOwnersCumulativeIntegration` was fast-forwarded to
P12 canonical at `81435f9f17816a3fb35b59d8ab374ed1cd719444`, from previous
canonical `676196bcd807603deb9d01bd2855342a7d47a01e`. This promoted code tree
is `b0be75370d32679d0745ed15d29ce359dada0bb6`; exact implementation review
passed, required focused suites passed, ALL EditMode passed `2008/2008`, the
complete official Smoke filter passed `5/5`, and `git diff --check` passed.
The following State-only commit records the promotion. The promoted candidate
includes the approved P12-D Genealogy saturated-rollback correction at
`5ef2615bb7d3de6280a2f7a6943a1669ead9002c`; the correction remains limited to
named-birth compensation at revision saturation. It does not complete P12-D.

Its code-bearing tip is `44fc3ab94c9666f656149f346fb2cc553d3cb689` and tree
`b0be75370d32679d0745ed15d29ce359dada0bb6`, based on prior canonical
`676196b`. It adds published PersonStore, ExplorableSite, and per-City
SettlementPopulation census providers while retaining the promoted Genealogy
provider. Exact-tip implementation and integration review passed; final
documentation revalidation passed at candidate branch tip `81435f9`.

This remains a partial passive owner-census foundation. P12-B is incomplete
and P12-A remains `WAIT_DEPENDENCY`. The promotion does not provide a complete
effective-profile inventory, shared mutation-epoch coverage,
owner-thread/quiescence proof, capture eligibility, immutable exports, or
staged hydration. P12-D remains blocked on P12-B and P12-C. The reviewed
legacy `SpatialNetworkRuntime` location/route census is now promoted as
recorded below. Continue with the remaining owner and invalidation gaps in
[`PHASE12_B_BLOCKER_RESOLUTION.md`](design/PHASE12_B_BLOCKER_RESOLUTION.md);
do not treat this witness as complete profile coverage or a P12-B readiness
change.

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
| P12-B — profile admission and completed-boundary lifecycle | `INCOMPLETE — PARTIAL FOUNDATION PROMOTED` | In addition to the promoted non-admitting kernel, receipt owners, P8-A–D, and RuntimeIdentity witnesses, cumulative stack `b889b4747738d933fe48311ef89fc33a40e3dfa0` adds passive witnesses for record sequence, ActorChoice, RuntimeIdAllocator, ArmedForce/manpower/position, Conflict/War/Battle, Estate/Property, and Institution/Office. Follow-up promotions add P12-D Genealogy parentage, legacy `SpatialNetworkRuntime` location/route witnesses, day-zero per-NPC `SpatialKnowledgeRuntime` witnesses, roster-following SpatialKnowledge census at `0021b0aa13fb6ae6d5f27c129c67ba452dbacb4e` (code `2c782a7`), the per-City NPC-presence projection witness at `10fb58d088e515d76bb86de2d7381c9ea9cb7483` (code `aafa81e`), roster-following per-NPC Inventory witnesses at `15e5a543f3896c02f9dbd9c36c9ba75bc90dac2b`, and the passive TravelParty owner witness at `b78a271f9c552b40bade1a45168388eafa670f59` (code/test `260a688`, review `e891058`). See the promoted-stack sections and linked candidate evidence. The static writer map and partial profile evidence remain in `docs/design/PHASE12_B_BLOCKER_RESOLUTION.md`. There is no complete profile census or shared-epoch coverage for all supported writes, no global Unity owner-thread/quiescence proof, and no capture token. |
| P12-C — identity, provenance, deterministic roots | `BLOCKED_ON_P12-B` | The `RuntimeIdAllocator` passive census and record-sequence witness promoted at `b889b47` are inventory evidence only; they do not provide C exports/hydration, deterministic-root state, or provenance. Preserve `codex/phase12/P12CIdentityRuntimeSnapshot` at `531d835` for selective reintegration only after B readiness and revalidation. |
| P12-D — factual roots and Person/population relations | `BLOCKED_ON_P12-B_AND_C` | Owner inventory and accepted scope remain; no complete export/hydration capability is claimed. |
| P12-E — core and official daily-domain owners | `BLOCKED_ON_P12-B_AND_C` | Owner inventory and accepted scope remain; effective-profile provider coverage and exact owner exports are incomplete. |
| P12-F — Knowledge, directives, choices, commitments | `BLOCKED_ON_P12-C_D_E` | Accepted scope remains dependency-gated; no complete export/hydration capability is claimed. |
| P12-G — staged restore, graph validation, publication, parity | `BLOCKED_ON_P12-B_THROUGH_F` | No whole-graph staged restore or continuation-parity capability is claimed. |

Phase 12 remains open. P13 remains dependency-gated. This State does not claim
save/load support, P12-A readiness, P12-B readiness, Phase closure, or a P13
historical fork guarantee.

## P12-B SpatialNetwork census promotion — 2026-09-30

With explicit approval, candidate
`codex/phase12/P12BSpatialNetworkCensus` at
`46c457fe12d2b287d55553e606fea471255d292d` was fast-forwarded to
`codex/phase12/canonical` from `eec3fbeecffb52c633ffe0945b3dfa39743ae064`.
The code-bearing commit is `555594ed899f2e191640f758599058a198c53ee7`,
tree `da7edc43dcbe7c56fedf52730d5fb808ff7db6d3`. Independent exact-tip
implementation review passed; the durable review record is
`codex/phase12/P12BSpatialNetworkCensusReviewRecord` at
`dc10d7376e22af1a7027ea8bdb238f2e56dfe390`. The candidate and review records
are linked in
[`PHASE12_P12B_SPATIAL_NETWORK_CENSUS_CANDIDATE.md`](design/PHASE12_P12B_SPATIAL_NETWORK_CENSUS_CANDIDATE.md)
and
[`PHASE12_P12B_SPATIAL_NETWORK_CENSUS_IMPLEMENTATION_REVIEW.md`](design/PHASE12_P12B_SPATIAL_NETWORK_CENSUS_IMPLEMENTATION_REVIEW.md).

It publishes passive schema-v1 owner witnesses for legacy runtime network
locations and routes. Both bind to the exact installed `SpatialNetworkRuntime`
and share its monotone revision. The selected authored bootstrap profile
reports two locations, two routes, and revision four. Validation on the code
tree passed `SpatialNetworkCensusTests` 7/7, ALL EditMode 2015/2015, complete
official Smoke 5/5, and `git diff --check`; retained XML evidence is under
`Library/ValidationResults/P12BSpatialNetwork` in the candidate worktree.

This remains unsynchronized passive census evidence. It does not connect
network or direct `RuntimeIdentityRegistry` writes to the shared P12-B epoch,
complete the selected-profile owner inventory, prove owner-thread/quiescence,
grant capture eligibility, or provide export/hydration. P12-B remains
incomplete; P12-A remains `WAIT_DEPENDENCY`; P12-C through P12-G remain
blocked on their documented prerequisites; Phase 12 remains open.

## P12-B SpatialKnowledge census promotion — 2026-09-30

With explicit approval, candidate
`codex/phase12/P12BSpatialKnowledgeCensus` at
`55e2f232b02cd5c6df76014d325a8caeb7020408` was fast-forwarded to
`codex/phase12/canonical` from `81ddfe4bd0620b1b61a0a52aa074ef2ed57c2833`.
The code-bearing commit is `de49358cd8b0baa5df2c12206d3e405dbf31261a`.
Independent exact-tip implementation review passed after the retained result
paths were verified; the reviewed candidate tip changes only the evidence
record. The selected bootstrap profile reports 20 per-NPC sections: ten
installed NPC owners, each with two known locations and one known route at
shared revision three. Providers bind to each exact installed
`SpatialKnowledgeRuntime` and are ordered by ordinal `RuntimeId`. The runtime
now returns live read-only views and rejects new discoveries before mutation
at revision saturation.

Validation on the code-bearing tip passed `SpatialKnowledgeCensusTests` 3/3,
`SimulationBootstrapCompositionTests` 14/14, ALL EditMode 2018/2018, and the
complete official Smoke filter 5/5. Exact XML evidence is retained under
`Library/ValidationResults/P12BSpatialKnowledgeReview` in the candidate
worktree and linked from
[`PHASE12_P12B_SPATIAL_KNOWLEDGE_CENSUS_CANDIDATE.md`](design/PHASE12_P12B_SPATIAL_KNOWLEDGE_CENSUS_CANDIDATE.md).

This is a fixed day-zero per-NPC witness set. Roster-driven dynamic
SpatialKnowledge section membership is now covered by the separate promotion
record below. Other dynamic NPC owner coverage, discovery-write invalidation,
shared mutation-epoch coverage for all supported writes, global
owner-thread/quiescence, complete profile owner coverage, capture eligibility,
and export/hydration remain unresolved. P12-B remains incomplete; P12-A
remains `WAIT_DEPENDENCY`; Phase 12 remains open.

## P12-B dynamic NPC SpatialKnowledge census promotion — 2026-09-30

With explicit approval, candidate branch
`codex/phase12/P12BDynamicNpcCensusImplementation` at
`0021b0aa13fb6ae6d5f27c129c67ba452dbacb4e` was fast-forwarded to the local
`codex/phase12/canonical` branch from
`0a37e9f053b482d80d0815c95352e3d96b56ed8f`.
The code-bearing commit is `2c782a7a08abfc1c8b2ba9201efad12f4ac279ae`.
Independent exact-tip implementation review passed against that base; the
durable record is
[`PHASE12_P12B_DYNAMIC_NPC_CENSUS_IMPLEMENTATION_REVIEW.md`](design/PHASE12_P12B_DYNAMIC_NPC_CENSUS_IMPLEMENTATION_REVIEW.md)
at the promoted candidate tip.

The partial protocol now reconciles two schema-v1 SpatialKnowledge sections
for every currently registered NPC, ordered by ordinal RuntimeId and bound to
the exact NPC and SpatialKnowledge owner objects. NPC register/unregister and
Person materialization/adoption use a runtime-owned outer membership context;
affected PersonStore sections and the dynamic family publish as one delta.
The local context records its owning thread; a mismatched nested entry faults
this partial census and cannot join its active context or publish its family
or epoch. This does not prove global runtime thread affinity or quiescence.

Validation on the code-bearing tip passed `SpatialKnowledgeCensusTests` 16/16,
`ContinuationCensusProtocolTests` 22/22, ALL EditMode 2035/2035, the complete
official Smoke filter 5/5, and `git diff --check`. Exact results are retained
under `Library/ValidationResults/P12BDynamicNpcCensus/` and linked from
[`PHASE12_P12B_DYNAMIC_NPC_CENSUS_IMPLEMENTATION_CANDIDATE.md`](design/PHASE12_P12B_DYNAMIC_NPC_CENSUS_IMPLEMENTATION_CANDIDATE.md).

This promotes only the roster-following SpatialKnowledge/Person partial
census. The passive City `ImportantNpcs` projection witness is promoted
separately below; broader City/NPC composite coverage, shared-epoch wiring,
direct Inventory write invalidation, unrelated PersonStore writers,
SpatialKnowledge discovery invalidation, complete profile inventory, global owner-thread/quiescence,
capture eligibility, and export/hydration remain open. P12-B remains
incomplete and P12-A remains `WAIT_DEPENDENCY`.

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

## P12-B City NPC-presence projection census promotion — 2026-09-30

With explicit approval, candidate
`codex/phase12/P12BNpcOwnerCensusImplementation` was fast-forwarded from prior
canonical `f7a66963ff6f44ee6116c0b37fd7374bf34ace5a` to candidate tip
`10fb58d088e515d76bb86de2d7381c9ea9cb7483`. The code-bearing implementation
is `aafa81ea6a2cf6b8963d15ee9ed8d26577b368a2`. Independent exact-tip
implementation and candidate-document reviews passed.

The promoted slice publishes one schema-v1 passive witness per installed City,
bound to the exact `CityRuntime` and reporting the City-owned
`ImportantNpcs` cardinality/revision. Before issuing evidence, it validates
unique projection membership and reciprocal exact City/Location references
against the live installed NPC roster. City-owned mutations, the live
read-only view, and revision-capacity preflights cover cross-City presence,
single travel, party start/rollback, and final-day party arrival.

Validation on the code-bearing tree passed `GeneralizedSpatialTravelTests`
18/18, `GroupTravelTests` 32/32, `SimulationBootstrapCompositionTests` 14/14,
ALL EditMode 2042/2042, the complete official `Smoke` filter 5/5, and
`git diff --check`. Persistent XML evidence and the precise scope are recorded
in
[`PHASE12_P12B_CITY_NPC_PRESENCE_CENSUS_CANDIDATE.md`](design/PHASE12_P12B_CITY_NPC_PRESENCE_CENSUS_CANDIDATE.md).

This remains a passive City projection witness. It does not wire City writes
to the shared mutation epoch, prove owner-thread/quiescence, provide
export/hydration, or grant capture eligibility. It does not supply global
owner coverage. It continues to rely on the
existing `SimulationRuntime.Cities` boundary; City composition sealing and
drift detection remain unsolved. P12-B remains incomplete and P12-A remains
`WAIT_DEPENDENCY`.

## P12-B dynamic per-NPC Inventory census promotion — 2026-09-30

With explicit approval, candidate
`codex/phase12/P12BNpcInventoryCensus` was fast-forwarded from canonical
`f961477380508647d2975272da72d96892944ed9` to `15e5a543f3896c02f9dbd9c36c9ba75bc90dac2b`.
The code-bearing changes add one schema-v1 `p12f.inventory/<RuntimeId>`
witness per currently rostered NPC, ordered by ordinal RuntimeId and bound to
the exact installed `NpcRuntime` and `InventoryRuntime`. The witness reports
the item-row count and existing local Inventory revision without triggering
lazy owner or item-storage creation. Inventory family reconciliation joins
the existing NPC membership operation with the dynamic SpatialKnowledge and
fixed PersonStore sections. Same-roster owner replacement fails census closed
and retains the previously published provider snapshot.

Independent exact-tip implementation review passed against the base/current
canonical tip `f961477`; the durable review record is
`codex/phase12/P12BNpcInventoryCensusReviewRecord` at
`f98c5d4f3ddc3721b4b31de1a40f063e2b333831`. Validation on candidate tip
`15e5a54` passed `NpcInventoryCensusTests` 7/7, bootstrap composition 14/14,
ALL EditMode 2049/2049, complete official Smoke 5/5, and `git diff --check`.
Exact XML evidence is retained in
`Library/ValidationResults/P12BNpcInventoryCensus` in the candidate worktree
and linked from
[`PHASE12_P12B_NPC_INVENTORY_CENSUS_CANDIDATE.md`](design/PHASE12_P12B_NPC_INVENTORY_CENSUS_CANDIDATE.md).

This remains a passive Inventory owner witness. Direct Inventory writes still
do not notify the shared P12-B epoch. The witness does not seal City
composition or cover an NPC `StartingCity` outside the composed City list; that
is a separate full-profile admission blocker. This promotion adds no global
owner-thread/quiescence proof, capture eligibility, export, or hydration.
P12-B remains incomplete and P12-A remains `WAIT_DEPENDENCY`.

## P12-B TravelParty census promotion — 2026-09-30

With explicit approval, `codex/phase12/P12BTravelPartyCensusImplementation`
was fast-forwarded to `codex/phase12/canonical` at
`b78a271f9c552b40bade1a45168388eafa670f59` from canonical
`e9ced8e451f42e80ed2132ce494cd5c26e439894`. Its code/test tip is
`260a688f7ea1a9f86e9b589788cda2c32357e170` (tree
`34571165d55af36a60f64650dce1968802cfd500`). Independent exact-tip review
passed; the durable PASS record is
`codex/phase12/P12BTravelPartyCensusImplementationReview2` at
`e8910587fdd894a5090c4208b30c5e834acb528f`. The previous HOLD record is
preserved separately.

The candidate publishes the exact installed `TravelPartyStore` through a
schema-v1 passive census provider reporting active party-instance count and
owner-local revision. Party identity remains distinct from member NPC
identities; cardinality counts party instances, not participants. The bounded
compensation test uses a deterministic stepwise sequence through real store
and lifecycle operations; it does not claim to pause the complete
`ExpeditionSystem.TryBeginReturn` coordinator. The reviewer verified the
production coordinator's matching mutation-window boundary and found this
proof sufficient without a production-only test hook.

Validation on the code/test tip passed `TravelPartyCensus` 10/10,
`GroupTravel` 32/32, `Expedition` 14/14, bootstrap composition 14/14, ALL
EditMode 2061/2061, complete official Smoke 5/5, and `git diff --check`.
All six XML/log pairs were independently inspected against their recorded
SHA-256 values in
[`PHASE12_P12B_TRAVEL_PARTY_CENSUS_CANDIDATE.md`](design/PHASE12_P12B_TRAVEL_PARTY_CENSUS_CANDIDATE.md).

This is a passive owner witness only. It does not prove member-level census
coverage, global committed-write invalidation, complete profile inventory,
owner-thread/quiescence, capture eligibility, export, or hydration. It adds
no P18 timeline/handoff behavior and no permanent Activity-to-Actor or
NPC-owned authority rule. P12-B remains incomplete; P12-A remains
`WAIT_DEPENDENCY`.

## P12-B ScheduledDirective, Expedition, and NPC Knowledge composition promotion — 2026-09-30

With explicit approval, candidate
`codex/phase12/P12BCensusOwnersCompositionIntegration` was fast-forwarded
from canonical `19d0373d6a71b63536248ecc9091e66c9b3a708b` to code-bearing tip
`72239ad1013dad5d507bad9737c358b1cadfe752`, tree
`c9fc250195bb5fda11935c687294f8ae5738bd9a`. The source owner candidates
remain in the integration history. The candidate composes the reviewed
ScheduledDirective, Expedition, and per-NPC Knowledge census providers through
the normal runtime/bootstrap boundary. Knowledge roster reconciliation is
staged before publication and preserves the Inventory family on success and
failed reconciliation.

Independent exact-tip integration review passed against canonical
`19d0373`; the durable record is
`codex/phase12/P12BCensusOwnersCompositionIntegrationReviewRecord` at
`b4fe15f0b32acea82c7ca4da608efde9346e8221`. The candidate contract and
evidence record is
`codex/phase12/P12BCensusOwnersCompositionCandidateRecord` at
`3b60dd61c7d84e08cfda587e83fb18a69decb957`.

The exact code tree's completed validation record reports Knowledge census
17/17, bootstrap composition 14/14, ScheduledDirective census 6/6, ALL
EditMode 2099/2099, official Smoke 5/5, and clean `git diff --check`. The
reviewer independently verified the retained Smoke XML/log hashes recorded in
the candidate file; the focused-suite and full EditMode XML/log files were
absent from the migrated checkout. The Smoke XML records 5/5 and its assembly
metadata lists 2,099 test cases; it is not itself evidence that all 2,099 ran.

This promotes only partial passive census composition. It does not complete
the effective-profile owner inventory or P12-B, connect every supported owner
write to the shared epoch, prove runtime-wide owner-thread/quiescence, grant
capture eligibility, or provide immutable exports, staged hydration, or
restoration. P12-B remains incomplete; P12-A remains
`WAIT_DEPENDENCY`; downstream P12 checkpoints remain blocked as recorded;
Phase 12 remains open.

## Remaining dependency-ordered P12-B blockers

The static successful-writer inventory is recorded; proving every included
commit reaches the shared invalidation epoch remains open. The selected live
day-zero test covers only a subset of owners and does not prove evolved
cardinality, owner-thread identity, or quiescence. Causal C roots, factual D,
official E, and commitment F owners still need exact witness providers and
supported-writer coverage.

P8-A through P8-D owner witnesses and the RuntimeIdentityRegistry witness are
now promoted. This adds positive day-zero P8-A geography cardinalities while
P8-B/C/D and registry witnesses cover their separate sections. The promoted
TravelParty witness covers only party-instance count/revision. Remaining
causal C roots, factual D, official E, and commitment F owners still need exact
witness providers and supported-writer coverage. The remaining committed-write
invalidation and owner-thread/quiescence blockers are unchanged. Phase 12
remains open and no P12-A implementation authorization is implied.

## P12-B bounded runtime admission/quiescence adapter promotion — 2026-10-01

With explicit approval, candidate `codex/phase12/P12BRuntimeQuiescenceAdapterIntegration`
was fast-forwarded from P12 canonical `f538a096bf4b2558566518483bc60f0129718a3b`
to `f60d8e65bea5f0b3f8964eef85957cf84451d6ba`. Its runtime implementation
commit is `9d4b035bc484286cfb58d66cca07809844c30254` (tree
`911d7cc9ff4e9c0205ae3305df4757099a461b0a`). The accepted technical design
`e5a32b8ec226204e751e1da41dfcf2546a760718` and its independent design PASS
remain linked from the implementation candidate record.

The selected `UnityBootstrap-Daily-v1` profile now carries the Unity `Start`
thread identity into the runtime and its partial census protocol. Its named
bootstrap-publication and daily-advance scopes are admitted on that captured
thread; direct runtime-owned daily clock calls route through the same daily
operation. The adapter rejects combination with a P18 timeline profile and
preserves P18 timeline clock ownership. Failure in an admitted bootstrap or
daily operation faults partial admission; selected-profile publication is
revoked and failed Start is latched. The candidate adds a selected-profile
ExplorableSite witness assertion for exact installed-owner identity and stable
day-zero count/revision zero.

Independent exact-tip implementation and final-delta review passed. The
review record is `codex/phase12/P12BRuntimeAdmissionAdapterReviewRecord` at
`bd62a1e7e1d5a4da0b5778fec99dee12bf62f9d9`. Validation passed runtime
admission 8/8, P18D compatibility 26/26, selected-profile census 1/1, ALL
EditMode 2107/2107, the complete official Smoke filter 5/5, and
`git diff --check`. Exact validation artifact hashes are retained in
`docs/design/PHASE12_P12B_RUNTIME_ADMISSION_ADAPTER_CANDIDATE.md` and the
review record.

This promotion supplies only the bounded admission/quiescence adapter and
related census evidence. It does not establish a complete effective-profile
census, full supported-write coverage of the shared mutation epoch, global
owner-thread/quiescence coverage, capture eligibility, immutable export, or
staged hydration. P12-B remains incomplete; P12-A remains
`WAIT_DEPENDENCY`; Phase 12 remains open.

## P12-B operation-footprint refresh candidate — 2026-10-01

The docs-only operation-footprint refresh was prepared on this canonical base
`70bc1e50a7107a1489614a62f5f34694b6b52498` as
`codex/phase12/P12BOperationFootprintRefresh`. Audit commit
`abf7246cd472e522139dd15867c38f6b6e7afd2a` corrects stale statements from the
older `81ddfe4` audit and separates the selected daily, membership, Travel,
TravelParty, and Expedition outer paths. Independent exact-tip review passed;
the review record is `docs/design/PHASE12_P12B_OPERATION_FOOTPRINT_REFRESH_REVIEW.md`.

The refreshed evidence confirms that the promoted adapter registers only the
partial owner and operation inventories described above. It does not supply
the complete effective-profile owner/cardinality set or map every supported
commit to the shared epoch. In particular, the Expedition start, daily
autonomy, direct exploration/effects, return, and travel-reconciliation paths
have different owner commits and bypasses; this evidence does not authorize a
new runtime operation or callback. This candidate changes no executable code
and records no new checkpoint acceptance.

P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, P13 remains
dependency-gated, and Phase 12 remains open. No complete census, shared-epoch
coverage, capture eligibility, export, or hydration is inferred.

## P12-B operation-footprint refresh promotion — 2026-10-01

With explicit approval, `codex/phase12/canonical` was fast-forwarded from
`70bc1e50a7107a1489614a62f5f34694b6b52498` to the reviewed, documentation-only
candidate `4f125864290113a257bc4f926e5e19d9a3b48da6`. This promotes the
operation-footprint revalidation and its State/review evidence only; it changes
no executable code and adds no implementation authorization.

The audit confirms that the selected profile has partial bootstrap, daily, and
NPC-membership scopes. It does not establish a complete owner/cardinality
inventory, supported-write coverage of the mutation epoch, or a single
Expedition operation boundary. The complete owner-section and supported-
operation matrix remains the prerequisite to further runtime wiring.

P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13 remains
dependency-gated; Phase 12 remains open. Capture eligibility and
export/hydration are not provided by this promotion.

## P12-B per-City Market stock census promotion — 2026-10-01

With explicit approval, `codex/phase12/canonical` was fast-forwarded from
`7be88ca7816b14c934729bffec56178ce3eb7e5a` through the exact-tip-reviewed
candidate `533c1e54f362218f222bf567dc1cacb8fdf68600` and its durable review
record commit `9dbae5b6e20caccfabfabf63e27ac5d75dc15008`. The implementation
commit is `3b2a9c2807ac95c6c929fe1104fcb078f364b96f` (tree
`1bb0a2b9e271d8070fe1012a0fecf7b37b0763e5`); the exact candidate tree is
`014ada28ba228be3e6faa104a876f521dd2a5835`. The review record is
`docs/design/PHASE12_P12B_MARKET_STOCK_CENSUS_IMPLEMENTATION_REVIEW.md`.

The promoted addition is only a passive per-City Market stock-row census
witness: each installed Market reports its row count and that same owner's
existing local revision. Exact-tip review verified the full candidate diff,
reviewed candidate SHA, and validation artifacts. The recorded validation
passed Market census 1/1, bootstrap composition 14/14, ALL EditMode
2108/2108, official EditMode Smoke 5/5, and `git diff --check`; detailed
artifact hashes remain in
`docs/design/PHASE12_P12B_MARKET_STOCK_CENSUS_CANDIDATE.md`.

This does not complete P12-B or establish runtime operation wiring, shared
mutation-epoch coverage, global owner-thread/quiescence, capture eligibility,
export, or hydration. P12-B remains incomplete, P12-A remains
`WAIT_DEPENDENCY`, P13 remains blocked, and Phase 12 remains open.
