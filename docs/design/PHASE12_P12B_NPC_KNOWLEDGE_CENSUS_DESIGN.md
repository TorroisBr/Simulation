# P12-B Per-NPC Knowledge Census Design

**Status:** reviewed-design proposal; no implementation or readiness claim

**Canonical design base:** `e9ced8e451f42e80ed2132ce494cd5c26e439894`

**Scope:** passive live census of selected per-`NpcRuntime` Knowledge owners for P12-B.

## Decision

Add one explicitly typed `NpcKnowledgeCensusProvider` and one `Knowledge` dynamic roster family to `ContinuationCensusProtocol`. It should publish separate required sections for each independently counted collection, with all sections for a given NPC tied to the exact owner runtime and its owner revision. This is a P12-B census seam only. It is not an export format, a shared epoch, a mutation notification mechanism, a capture-eligibility proof, P12-F export/hydration, or P12-A readiness.

The family is warranted by the source: the selected roster contains four composed per-NPC runtime owners with different write paths and cardinalities. Reuse the existing typed `SpatialKnowledge` and `Inventory` family patterns for staging, validation, dynamic roster reconciliation, and fail-closed behavior. Do not merge their owner data into a generic family abstraction or alter either existing family contract.

## Source inventory and current revision state

`NpcRuntime` owns lazy `ExplorableSiteKnowledgeRuntime`, `LocalTopologyKnowledgeRuntime`, and `AdventureSiteIntelKnowledgeRuntime` properties, plus a serialized `CommercialKnowledgeRuntime`. Census construction must not invoke a property that can materialize missing state. Add non-materializing internal accessors for the four included owner references; a null existing owner is an owner-coverage failure, not an empty owner. Although `NpcLocalKnowledgeObservationRuntime` exists on `NpcRuntime`, the canonical P12-B owner inventory says the P18 temporal/local-observation owner is not composed in the selected profile. Do not register that owner as a required section and do not label it an exact-zero owner.

| Owner | Required cardinalities | Owner identity | Revision at base | Existing mutation authority |
|---|---|---|---|---|
| `ExplorableSiteKnowledgeRuntime` | observations | exact runtime instance | absent | direct runtime `RecordObservation`; `ExplorableSiteKnowledgeSystem`; authored grant handler |
| `LocalTopologyKnowledgeRuntime` | place observations; connection observations | exact runtime instance | absent | direct runtime record methods; `LocalTopologyKnowledgeSystem`; adventure-intel observation fanout |
| `AdventureSiteIntelKnowledgeRuntime` | opposition; notable-item; common-resource; access observations | exact runtime instance | absent | direct runtime record methods; `AdventureSiteIntelKnowledgeSystem`; authored grant handler; expedition observation |
| `CommercialKnowledgeRuntime` | market observations; liquidity observations; share receipts | exact runtime instance | present (`Revision`) | direct record methods; `MerchantSystem`; `CommercialKnowledgeSharingSystem`; P18 prepared observation/share installs |

The first three included owner runtimes have no owner-local revision. Their collection getters are read-only views but the runtime record methods are public, so census revisions must be maintained at those owners, not inferred from call sites. `CommercialKnowledgeRuntime.Revision` already advances for successful direct insert/replacement, accepted liquidity insert/replacement, successful prepared direct batches by the number of changed Knowledge rows, and a new committed share receipt (one revision for that commit); an idempotent replay does not advance it. Keep those existing semantics. The current `CommercialKnowledge` property uses `??=` and can materialize state during a read, so add a non-materializing `ExistingCommercialKnowledge` accessor and fail closed when it returns null.

P8 `SpatialKnowledgeRuntime` is already separately witnessed by `p12f.spatial-knowledge.locations/{npcRuntimeId}` and `...routes/{npcRuntimeId}`. Its current bootstrap evidence is 2 known locations, 1 route, revision 3 for the relevant authored profile witness. Do not fold it into these sections. P8-D world `SpatialRouteKnowledgeStore` knowledge is PersonId keyed and separate from NPC-local spatial Knowledge. `ExplorableSiteStore` is an independent P12-D owner with its own existing fixed witness; it is not the per-NPC knowledge below.

## Required section schema v1

Publish the following required section for each installed NPC, sorted by ordinal `RuntimeId`. Each section uses schema version 1. Cardinality means the exact backing list count, not distinct domain concepts or a read-model count.

| Section prefix | Cardinality |
|---|---|
| `p12f.explorable-site-knowledge/` | `ExplorableSiteKnowledgeRuntime.Observations.Count` |
| `p12f.local-topology-knowledge.places/` | `LocalTopologyKnowledgeRuntime.PlaceObservations.Count` |
| `p12f.local-topology-knowledge.connections/` | `LocalTopologyKnowledgeRuntime.ConnectionObservations.Count` |
| `p12f.adventure-intel.opposition/` | `OppositionObservations.Count` |
| `p12f.adventure-intel.notable-items/` | `NotableItemObservations.Count` |
| `p12f.adventure-intel.common-resources/` | `CommonResourceObservations.Count` |
| `p12f.adventure-intel.access/` | `AccessObservations.Count` |
| `p12f.commercial-knowledge.markets/` | `CommercialKnowledgeRuntime.Observations.Count` |
| `p12f.commercial-knowledge.liquidity/` | `LiquidityObservations.Count` |
| `p12f.commercial-knowledge.share-receipts/` | retained `ShareReceipts.Count` |

Every witness reports the exact corresponding owner object as `OwnerInstanceIdentity`. The two sections for `LocalTopologyKnowledgeRuntime` share that exact identity and revision; four Adventure sections share their exact runtime identity and revision; three Commercial sections share that owner and its existing `Revision`. Expose a narrow non-materializing owner census accessor for Commercial share-receipt count; do not expose or copy receipt payloads.

For owner runtimes whose revision is absent, add a private monotone `long` owner-local revision and a read-only accessor. Increment once for every successful collection insertion or replacement. Return the existing false/no-op outcome without increment for null, duplicate/stale/equal-priority observation, invalid precondition, or rejected mutation. Before a mutation that would change stored state, check that revision can advance; if at `long.MaxValue`, reject before that owner collection changes. No-op paths remain available at saturation. Do not claim rollback of preceding mutations to a different owner during a multi-owner fanout.

Adventure's generic base and four typed lists remain the existing data model. The schema intentionally counts four typed lists separately; it does not invent a sum that could conceal movement between sections. For `LocalTopologyKnowledgeRuntime.RecordConnectionObservation`, count only the connection list in its section; the explicitly recorded origin/destination place observations are independently reflected in the places section.

## Live roster, identity, and replacement rules

Build a typed grouped roster-family provider for the four included runtime owner types. It creates ten section witnesses per NPC, using a shared internal NPC-family provider identity plus typed owner references, not a reflection-driven or generic owner registry. Validate that:

1. The source roster is non-null and every `NpcRuntime` is non-null with a non-empty unique `RuntimeId`.
2. The four owners already exist; all are accessed only through non-materializing `Existing...` accessors. A null Commercial backing field is missing coverage and must not be repaired by invoking the lazy getter.
3. Each runtime's declared owner ID (where defined) equals the owning NPC `RuntimeId`.
4. Every section's witness identity is reference-equal to the captured specific runtime owner. For all sections sharing a runtime owner, their owner identities and revisions agree.
5. There are exactly ten unique section IDs per NPC and exactly ten times the roster count total, with no collision against fixed or existing Inventory/SpatialKnowledge family sections. P18 local-observation receipt owners are not part of this composed selected-profile family; absence is not a zero witness.

At each already-authorized owner-thread/quiescent roster reconciliation boundary, construct all new candidates and witnesses in temporary structures first. Reconcile additions, removals, and rematerialized/replaced `NpcRuntime` or owner instances as one typed Knowledge-family replacement; publish new provider/section/owner maps only after all candidates validate. Removed family sections are retired according to the existing roster-family protocol. A same-ID but different NPC/runtime-owner reference is replacement, never continuation of the old identity. Missing, duplicated, invalid, lazily absent, or replaced owners that fail the exact binding checks fault/fail closed; no empty fallback, implicit owner creation, partially published provider set, or guessed revision. Provider reads remain passive and unsynchronized and make no owner-thread/quiescence claim by themselves.

Extend the existing protocol using a specifically named `NpcKnowledge` family and typed candidate/provider records alongside `SpatialKnowledge` and `Inventory`. Reuse the current protocol's atomic staged dictionaries, required owner contracts, exact identity checks, fail-closed validation, and roster refresh path. The refreshed protocol must rebuild Knowledge candidates in the same outer roster/materialization boundary that refreshes its installed NPC roster. Do not make the new API a post-seal arbitrary registration hook, alter the P8 SpatialKnowledge family, or infer global mutation-epoch notification from revisions.

## Mutation paths and semantics to preserve

### Explorable-site Knowledge

Writes occur through `ExplorableSiteKnowledgeRuntime.RecordObservation`, `ExplorableSiteKnowledgeSystem.RecordInitialScenarioKnowledge`/`RecordDirectObservation`, the authored `GrantSiteKnowledge` command handler, and bootstrap initial-Knowledge publication. The system first records the site observation and may then discover its location in the separate SpatialKnowledge owner. Count only the ExplorableSite observation; leave spatial discovery to the existing P8 sections. Preserve source/day-priority replacement and false return for unchanged/stale records.

### Local-topology Knowledge

Writes occur through `LocalTopologyKnowledgeRuntime.RecordPlaceObservation` and `RecordConnectionObservation`, its direct/initial/shared `LocalTopologyKnowledgeSystem` paths, and `AdventureSiteIntelKnowledgeSystem.RecordDirectObservation`. A connection observation explicitly records origin and destination places before recording the connection. Preserve the sequence and existing return behavior. Owner-local revision changes track successful local list changes; they do not promise one transaction across the places and connection lists.

### Adventure/site intel

Writes occur through all four typed `AdventureSiteIntelKnowledgeRuntime.RecordObservation` overloads; direct site/place observations in `AdventureSiteIntelKnowledgeSystem`; authored `GrantAdventureIntel` handlers; and `AdventureExpeditionAutonomySystem` observations during expedition progress. The site/place observation path may first write LocalTopology Knowledge and then update Access, Opposition, NotableItem, and CommonResource sections from content. Preserve existing typed identity/replacement rules and source priorities. A later child rejection, including owner-revision saturation, must not be described as rolling back earlier LocalTopology or Adventure writes; tests must pin the actual bounded sequence and ensure no section reports a changed count without its own revision advancing.

### Commercial Knowledge

Writes occur through direct `RecordObservation` and `RecordLiquidityObservation`, `MerchantSystem` bootstrap/market observations, `CommercialKnowledgeSharingSystem` (including one recipient-owned prepared batch plus edge receipt), and P18 prepared installs when that path is composed. The market and liquidity sections count their two existing lists; the share-receipts section counts retained committed `CommercialKnowledgeShareReceipt` entries. A successful prepared P18 install can change several market/liquidity rows and increments Commercial's existing revision by the prepared number of applied changes. A new share operation increments Commercial revision once even when its committed receipt records zero successful updates, because the receipt is still durable owner state. Replays change neither count nor revision. Do not reinterpret `Revision` as an item counter.

P18 local-observation receipt state remains explicitly out of this selected-profile family. Its owner is not composed for the profile in the canonical P12-B owner map, so this design neither adds the receipt owner to the roster contract nor asserts that it is empty. P18 prepared-install effects on Commercial Knowledge are counted through the already composed Commercial owner when that execution path is present; no P18 receipt census is inferred from that fact.

## Selected-profile baseline and evidence

For the selected authored profile, the installed live NPC roster is expected to contain 10 NPCs. The census must derive this from the live runtime roster, not a literal expected count. At bootstrap baseline:

- P8 SpatialKnowledge has its separately established current witness (for the reviewed proving profile, locations 2, routes 1, revision 3); it remains out of this family.
- Per-NPC ExplorableSite, LocalTopology, and Adventure/site-intel collections are initially zero unless authored profile inputs explicitly seed them; compare actual counts against the selected inputs and current owner state rather than hard-code zero in provider code.
- P18 local-observation receipt owner is not included in this profile's required family; do not assert an exact-zero receipt count.
- Commercial market/liquidity rows depend on actual authored merchant inputs and `BootstrapInitialCommercialKnowledge`. Census must enumerate actual merchant owners and query the live lists. Do not hard-code market or liquidity totals; the exact receipt count starts at zero absent a committed share operation.

Tests should assert selected-profile owner coverage equals `liveNpcRoster.Count × 10`, exact per-NPC section identity/count/revision, and independently verify profile-specific actual values against the selected bootstrap inputs. A merchant-count mismatch is a profile-evidence failure to diagnose, not a reason to change the census cardinality definition. Also assert that reading the census with a null `CommercialKnowledge` backing field does not call the materializing getter and fails closed without creating an owner.

## Focused implementation and verification requirements

The implementation checkpoint should add focused tests without changing domain behavior:

1. **Provider baseline and identity:** each section has the expected schema/ID, exact runtime owner identity, common identity/revision across sibling sections, and list count. Census must not materialize any missing owner, including a null `CommercialKnowledge` backing field; include a test that asserts the field remains null after failed census construction.
2. **Per-owner mutation/revision matrix:** insertion, valid replacement, duplicate, stale/equal-priority no-op, null/invalid guarded request, and revision saturation for Explorable, LocalTopology, each Adventure typed list, and Commercial. Confirm a rejected saturated write leaves that owner's list and revision unchanged.
3. **Fanout boundaries:** place/connection observation counts across `LocalTopologyKnowledgeSystem`; LocalTopology→Adventure observation fanout; Commercial direct/sharing/prepared batch cardinalities and revisions; P18 prepared Commercial child-install identity when that path is included. At saturation, assert only the documented individual-owner behavior and make no global atomicity assertion.
4. **Typed dynamic roster reconciliation:** additions/removals/materialization, stable same-NPC identity, same-ID NPC replacement, each of the four included owner replacements, duplicate/empty runtime IDs, missing owners, null Commercial field, and failed mid-build candidate proving no partial family publication. Keep existing SpatialKnowledge and Inventory family coverage intact.
5. **Protocol exact-zero/current witness:** while the runtime owner thread is bound and registered operations are quiescent, reconcile the Knowledge family and verify all ten sections per NPC; exact-zero witnesses must stay zero at owner revision `long.MaxValue` when no mutation occurs. Verify unnotified changed cardinality/revision is still detected by the protocol's existing validation; do not add global epoch wiring in this checkpoint.
6. **Selected profile:** compare live NPC count, owner identities and every section against actual authored bootstrap inputs, including actual merchant Knowledge. Cross-check the existing P8 SpatialKnowledge baseline separately.

Run the focused Knowledge census tests, the existing `ExplorableSiteKnowledgeTests`, `LocalTopologyKnowledgeTests`, `AdventureAutonomyFoundationTests`, `AdventureExpeditionAutonomyTests`, `CoreWorldCommandHandlerTests`, `CommercialKnowledgeTests`, `CommercialKnowledgeSharingTests`, `P18DLocalKnowledgeObservationTests`, `SpatialKnowledgeCensusTests`, and `SimulationBootstrapCompositionTests`, then all EditMode and the complete official Smoke suite under the normal P12 validation gate. Validation must run against the candidate's exact tip. This design work itself makes no implementation or test claim.

## P12-B blockers that remain

This design resolves only the per-NPC Knowledge census contract. It does not complete P12-B. Remaining evidence/capabilities include:

- all other owner/cardinality census areas still listed in the current P12 blocker map, including the in-flight TravelParty design/implementation and directive/expedition/military/composition gaps;
- committed-write invalidation mapping and evidence, including all owner paths that can mutate without notifying a common epoch;
- owner-thread and quiescence proof for the complete registered operation set, beyond the specific already-serialized P18 runtime advance window;
- runtime exact-zero/current-owner witnesses for every required included profile owner and complete live selected-profile owner inventory;
- export and staged-hydration capabilities plus actual profile owner-set closure for P12-F/P12-A, under their separate gates.

No result here authorizes capture, declares mutation visibility complete, creates a common epoch, grants export/hydration, makes P12-A READY, closes P12-B, or promotes this candidate to canonical.
