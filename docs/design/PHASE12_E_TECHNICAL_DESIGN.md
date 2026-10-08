# P12-E — Profile Core and Official Daily-Domain Owner Sections

**Status:** Current-base technical-design candidate for the accepted P12-E capability boundary. It defines exact owner exports and private staged hydration for core and configured official daily-domain authorities. It is not a delivered capability, P12-A profile integration, or Phase 12 closure.

**Current bases:** P12 canonical 0e786db8e6ed5ed937ff62e3f63258d8b73fd93c; architecture canonical 47eff220c7ce00f6e7c759bdc2b76780bb46f628. The architecture documents reviewed are docs/SIMULATION_ARCHITECTURE.md, docs/EXECUTION_MODEL.md, and docs/ROADMAP.md at that architecture tip. The intraday/extensibility and multi-participant alignment records are blobs a231a2a014bf58be5ce382c48a55f3654df89a61 and 4ed6fcc60b348461e3201d4f3d480c21a3154ec3. The accepted P12 Brief, decomposition, selected profile, and P12 owner code are read from current canonical evidence.

## Current canonical revalidation

P12-B and P12-C are COMPLETE/PROMOTED within their bounded contracts. E consumes the P12-B completed-boundary/capture token and P12-C identity/genesis/random roots, including the same WorldId. It does not add a second capture lock, rerun genesis, mint identities, or introduce P13 history semantics. P12-D roots are needed for resolved references; an isolated E owner may use typed unresolved-reference evidence only where its reviewed owner adapter explicitly supports that order.

Daily-v1 is the SampleScene-selected Simulation-DailyV1.asset. Simulation-GeneralTest.asset remains the separate P10-A Ruin/LocalTopology profile and must be rejected by Daily-v1 admission. The P12-D/E owner split follows the accepted capability decomposition: D owns exact City/market/item/account/custody/stock/balance roots and D-owned NPC/person/spatial facts; E owns only distinct core or official-provider state assigned to E. E never exports a second copy of a D-owned root field. A nested shared owner is captured once under the exact P12-B completed-boundary token, with the corresponding owner-section identity/cardinality/revision vector, split into disjoint sections, merged, and hydrated once. Do not invent a synthetic whole-City or whole-NPC revision where current owners expose component revisions. The token and revision vector are transient capture evidence, not serialized continuation state.

P17-A state is not in Daily-v1. Current P17-A composition adds optional strategic state to the existing PersistentWarStore, not a second War owner. The promoted P17-A Daily-profile rejection and its negative test are preserved. E includes the current P12-E War owner facts for this profile, while admission rejects any P17-A state; census count/revision is not serialization and must not be treated as evidence that P17 state is supported. This does not remove or weaken P17-A or establish future P17 persistence.

The previously reviewed E owner-census designs remain valid only as passive census evidence. They do not provide exact exports or staged hydrators. The current P12-B/C promotions and corrected Daily-v1 profile make the prior E technical review stale for implementation readiness. This current-base design candidate requires a fresh independent exact-content review.

P12-A remains WAIT_DEPENDENCY until every included owner has reviewed exact export and staged hydration, the live profile inventory is validated, and its separate implementation authorization is recorded. No P12-A, P12-E, or Phase 12 completion claim is made here.
## 1. Purpose and boundary

P12-E supplies exact immutable owner exports and private staged hydrators for
the supported core authorities and the official daily-domain owners actually
composed by `UnityBootstrap-Daily-v1`. This includes the selected legacy City
economy and the political, institutional, property, justice/crime, military,
conflict, War, and Battle truth reachable in the runtime. Empty-at-bootstrap
stores are still supported authorities: they need explicit empty sections and
must preserve later populated supported state.

The capture is owner truth, not a `WorldStateSnapshot`, event/history stream,
Unity serialization graph, runtime clone, or service-object dump. Every fact
is emitted by the authority that owns it as detached immutable values. A
hydrator validates a private staged candidate; it never mutates the active
runtime. It restores recorded state and does not rerun daily systems, derive
past outcomes from current inputs, replay events, recreate settlements, or
apply consequences a second time.

P12-E owns E-domain facts and provider behavior/state. It does not own City
identity/population roots or NPC values; Knowledge; active merchant plans or
other commitments; identity/genesis roots; global graph validation; or final
publication. The fixed owner boundary and exclusions are defined below.

## 2. Accepted profile and composition admission

The profile is exactly UnityBootstrap-Daily-v1, using the SampleScene-selected Simulation-DailyV1.asset, normal TesteSimulacao.InitializeSimulation composition, exact compatible build/current-host numeric profile, and successful completed-day boundary. Simulation-GeneralTest.asset is the separate P10-A proving profile and is rejected by Daily-v1 admission. Effective configuration and actual
instantiated providers, not serialized module flags alone, decide which
conditional E providers are present.

The current Simulation-DailyV1 asset requests the four declared modules Economy, Merchant, GuardCrime, and Crime. This is bootstrap input; the effective configuration and instantiated service/provider graph remain the semantic authority. The older d01cd62 inventory was built before the profile asset correction and is not accepted as a current provider inventory. Recheck every effective provider, state owner, and mutation path against the current asset and TesteSimulacao composition before its E adapter is implemented. Unknown, injected, unsupported, or ambiguous provider composition rejects this profile.

Each section distinguishes:

- **required populated or empty:** the authority is part of the supported
  composition and exports an explicit value section, including an empty
  collection where there are no facts;
- **not composed/disabled:** the admission manifest records the exact reason
  and provider identity absence; an unexpected live instance or state rejects
  admission;
- **excluded:** the section is outside this profile; populated or unverified
  state rejects admission rather than being ignored.

Do not substitute constructor defaults, an empty diagnostic projection, or an
initial asset count for an owner-issued witness of current state.

## 3. Ownership map and semantic section contracts

Names below are section roles. Implementation uses the actual owner types and
record schemas in the validated composition; it must not create a parallel
world-truth store or force unrelated authorities into a generic serializer.

### 3.1 CityRuntime — one owner; D-only fields in current Daily-v1

CityRuntime and its nested market/economic objects remain one concrete owner boundary. The accepted decomposition assigns City, market, market-item, account/custody, stock/balance, and population root facts to D. The current Daily-v1 source crosswalk finds no separate E-owned CityRuntime fields: economy provider operations change D-owned stock, price, population, and account roots. `CityData` and item definitions are admitted content/configuration identities rather than mutable E owner state. `CityRuntime` daily continuation receipts are called by `P18DDailyBoundaryStepProviders` and belong to excluded P18 temporal composition; P14 finite-source state and `LastMaterialFlow` are also excluded from this profile. A newly discovered populated City field not covered by this map blocks implementation until the owner crosswalk and review are refreshed. A field cannot be duplicated merely because both checkpoints consume it.

P12-D captures the concrete City owner once under the P12-B token and records the exact nested owner-section revision vector for the current profile's root values. P12-E emits no City section in this Daily-v1 profile. If a future supported profile demonstrates an E-owned retained field nested under CityRuntime, the same capture stamp and revision vector must bind disjoint D/E values, which merge into one private candidate and reconstruct one CityRuntime and nested owner graph. No independent City re-read, invented aggregate revision, partial City constructor, or duplicate MarketRuntime hydrator is allowed.

For current Daily-v1, any non-empty E City section is unsupported and must be rejected rather than treated as a second representation of D roots. Providers are rebuilt only where the existing contract proves they are derived; any unclassified retained fact is a precise implementation blocker until the crosswalk resolves it.
### 3.2 Official economy and merchant authorities

Capture each actually composed economy/merchant owner by its own stable
authority identity and revision. P12-D owns the exact market, custody, stock,
balance, item, population, and City-root values assigned by the accepted
decomposition. P12-E owns only distinct provider-owned retained facts
established by the current field crosswalk. Current Daily-v1 has no separate
E CityRuntime value section; provider operation results are captured through
the D roots they mutate. Shared nested values are captured from one CityRuntime
owner snapshot and hydrated once.
`MerchantSystem` owns its own mutable causal state only if the exact code audit
demonstrates such state; otherwise reconstruct its service from admitted
configuration/provider identity and owner references. Do not serialize a
service object to imply state coverage. Any retained cursor, plan index, or
other continuation state must be explicit and owner-issued.

Commercial Knowledge sharing is an E provider behavior, but the observations,
holder/provenance/freshness/revision records it writes are Knowledge and
belong to P12-F. E captures only the provider identity/configuration and any
independent mutable state the exact owner audit proves it retains; it does not
read, export, or hydrate F's Knowledge section. P12-G validates provider-to-
Knowledge bindings after both packages are staged. Active merchant plans,
remaining plan work, and commitment progress belong to P12-F. If an E provider
writes an existing `NpcRuntime` field, the single D/F `NpcRuntime` projection
owns that value; E must not export or hydrate it again.

P12-E does not extend the P18-D keyed sale-receipt capability into a P12 save
protocol, migrate NPC-to-NPC trade, or claim P18 temporal replay semantics.
Any receipt/history that is present in an actually composed P12-E owner must
be inventoried as causal owner state and versioned; it is not replaced by
History or event records.

### 3.3 Justice, crime, appraisal, and guard providers

For the exact effective composition, capture the owner-held justice and crime
facts needed for continuation, including current justice/custody/sentence or
warrant facts where those authorities own them, terminal crime/outcome state,
and `CrimeSocialAppraisalWorldState` only to the extent its owner contract
defines retained truth rather than a rebuildable projection. Identify exact
`JusticeSystem`, `CrimeSystem`, appraisal, and configured crime/guard action
provider identities and compatible versions. Rebuild service references and
derived appraisal indexes only through their domain owner rules.

Crime/guard execution may mutate `NpcRuntime` status, life, or condition.
Those values remain in the single D/F NPC value projection. The E sections
carry only crime/justice owner facts and provider-owned state; cross-section
validation binds outcomes to the existing NPC/person/City roots without
copying or applying the NPC consequence. Events, audit records, and decision
records are not substitutes for the owner facts.

### 3.4 Core political, institutional, property, force, and conflict owners

The runtime constructs the core stores even when empty. For each actual
instance, export an explicit owner section and a private staged candidate for
at least the following current owner families identified by the composition
inventory:

- `InstitutionStore` and `OfficeStore`: institution/office identity and
  definition references, incumbency, tenure and terminal/history facts owned
  by those stores; preserve office-to-institution and Person/actor references.
- `PropertyOwnershipStore` and `EstateStore`: property, ownership and estate
  records, stable IDs, current and historical/terminal dispositions, typed
  Person/organization/property links, and owner revisions.
- `PoliticalClaimStore`: claim truth and recognition/resolution records with
  claimant and target identities, target-kind semantics, status and causal
  dates/revisions. A claim does not establish the asserted fact; recognition
  does not mutate genealogy, property, or office truth.
- `FactionStore` and `PoliticalSupportStore`: faction identity, affiliation
  and support relation records and their lifecycle/revisions. Relations remain
  store-owned rather than redundant collections on `PersonRuntime`.
- `PoliticalDecisionStore`: only its owner-held decision facts and terminal
  dispositions. `PoliticalKnowledgeStore` is Knowledge and belongs to P12-F;
  this E design must not duplicate it. Preserve any distinct E decision record
  sequence through P12-C's shared sequence section, rather than allocating a
  new sequence on hydration.
- `ArmedForceStore`, `ContingentManpowerStateStore`, and
  `ArmedForceSpatialStateStore`: force/contingent identities, current
  manpower state and source bindings, supported spatial positions and owner
  revisions. Validate typed source and location references; provenance is not
  a substitute for an authoritative source binding.
- `PersistentConflictStore`, `PersistentWarStore`, and
  `PersistentBattleStore`: stable identities, lifecycle and terminal outcomes,
  sides/participants, force/conflict/War/Battle references, logical dates,
  causal resolution provenance, and any owner-held consequence facts.
  Preserve `Victory`/`Draw` and winner semantics exactly as the owner stores
  them; do not infer casualties, control, office vacancy, War outcomes, or
  other unrecorded consequences from diagnostics or aggregate values.

Also capture other core authoritative stores actually instantiated by the
current selected `SimulationRuntime` when their facts are in the accepted
profile and not assigned to D, C, or F. The refreshed inventory currently
identifies `PoliticalKnowledgeStore` as a core store; its semantic content is
explicitly assigned to F because it is Knowledge. The exact current composition
and any newly discovered core owner must be re-audited before E implementation;
an unassigned owner is a blocker, not permission to drop it.

### 3.5 Owner revision and causal dependencies

Every mutable authority exports an immutable value snapshot with its stable
owner identity, schema/version, current revision, typed references, and exact
causal values. Each successful supported authoritative mutation advances its
revision exactly once according to its existing atomic owner boundary, including
legacy direct APIs and daily-system writes. A failed preflight with no mutation
does not advance it. The implementation must enumerate every supported writer
and prove the revision cannot be bypassed through a mutable list/object
exposure. If a store lacks revisions, add an owner-controlled revision seam;
do not infer it from event count, absolute day, diagnostics, or a hash alone.

The P12-B capture token binds all included owner-section identities,
cardinalities, schemas, revisions, and the mutation epoch. Current Daily-v1
uses its exact City component revision vector; if a later profile admits an
E-owned City field, it must use the same transient capture stamp/vector.
Capture must be rejected while the runtime is in an active
advance/operation scope or if an owner changes during snapshot. This section
does not create a second lock, thread-safety promise, or capture lifecycle;
P12-B owns eligibility and the promoted P18-D2 lease/handoff contract governs
the runtime hotspot. The active P18-D consumer integration retains the current
`SimulationRuntime` editing window until an explicit handoff. Snapshot operations should use owner atomic-copy methods
or before/after revision equality under the approved completed-boundary
protocol; if neither proves a coherent read, the owner remains unsupported.

E's implementation dependencies are the admitted effective configuration and
provider manifest from P12-B; random provider/seed/stream roots and shared
causal sequence/stable typed IDs from P12-C; and City identity/population,
NPC/Person roots, and legacy spatial references from P12-D. E validates its
owner-local facts and references to those C/D roots. If an E fact has a
cross-section relationship to a P12-F Knowledge or commitment fact, the E
package reports the typed unresolved binding as validation evidence; it does
not depend on, read, restore, or resolve F. P12-G resolves those bindings only
after all B-F packages are staged. Provider code/content, rules, and
configuration must match the admitted profile. Unknown providers, unsupported
schema/version, missing required definitions, unknown random state, or a
populated excluded section rejects before publication.

## 4. Empty, populated, and excluded sections

An empty required core store has a versioned section containing its exact
owner identity/schema/revision and an explicit empty fact set. A populated
store contains the complete supported fact set, never a diagnostic subset.
Absence of an owner section is not interpreted as empty. A configured optional
provider that is disabled has a typed explicit-absent/disabled witness bound
to effective configuration; if it appears at runtime or owns populated state,
profile admission rejects. No default state may conceal missing causal data.

Explicitly excluded state that must remain empty for this profile includes:

- canonical P8-B through P8-E authorities (P8-A geography belongs to C);
- P10 Ruin/LocalTopology state, including the instantiated
  `LocalTopologyStore` which must be witnessed empty;
- P14-A material-flow state. Ordinary City production, population consumption,
  market stock, and merchant economy remain included under their current
  owners;
- P18 intraday timeline, lifecycle, external-input, and continuation state;
- P19 module/loader-owned state and retrofit; P20 shared activities;
- P13 historical reconstruction/fork guarantees; and any generated P9/P10
  world/content.

Knowledge remains assigned to F (including `PoliticalKnowledgeStore` and
commercial/exploration/spatial observations). P11 `ActorChoiceStore`, external
`WorldCommand` queues, scheduled directives, active merchant plans, travel,
expedition and other active commitments also remain assigned to F. NPC facts
written by E providers remain in the one D/F NPC owner projection. E neither
serializes those sections nor silently treats them as empty.

## 5. Staged export, hydration, and dependency order

1. P12-B admits the exact runtime/provider composition and a successful
   completed-day, quiescent boundary. P12-C roots supply typed IDs, shared
   sequence and deterministic random compatibility/state. P12-D supplies the
   referenced City/NPC/Person and legacy spatial roots. These are prerequisites,
   not duplicated E sections.
2. Each E owner exports its own detached immutable value snapshot with exact
   owner identity/schema/revision. For current Daily-v1, D captures
   `CityRuntime` and its nested factual roots once, and E emits no City fields.
   Any future reviewed E City slice must come from that same capture stamp and
   exact component revision vector;
   no independent re-read or second City hydrator is allowed.
3. Before allocating live candidates, validate section presence/absence,
   provider/config/content compatibility, schema, IDs, revisions, enum/range
   values, counter/sequence references, and local owner invariants. Reject
   unknown required sections and unsupported populated state.
4. Build a new private staging graph in dependency order: admit configuration
   and provider identities; instantiate owner shells from C roots; restore D
   factual roots and build each current Daily-v1 CityRuntime once from the D
   projection; if a separately reviewed E City slice exists for a future
   profile, merge it before construction. Bind distinct E-owned provider facts
   to staged D roots without recreating D-owned market/account/economic roots; then resolve only
   E references to already-staged C/D roots. Return typed unresolved binding
   evidence for any relation to F. The concrete factory order must follow
   actual E-to-C/D owner dependencies and not merely this grouping order.
5. Validate E-local invariants and E-to-C/D references: owner identities and
   revisions; City/market/account custody; referenced Person/NPC/City,
   organization/property/force/source existence when those roots are in C/D;
   office/institution, property/estate, claim/recognition,
   faction/affiliation/support, force/manpower/spatial, and
   Conflict/War/Battle owner relations. Record cross-section F bindings as
   unresolved evidence for P12-G. P12-G alone validates global stable-ID
   uniqueness and shared sequence monotonicity across all sections, resolves
   D/E/F bindings, checks admitted providers/random compatibility and required
   exact empty sections, and performs whole-graph validation. Preserve
   recorded terminal facts. Rebuild only indexes/caches explicitly defined as
   owner-derived; do not synthesize facts to repair a broken graph.
6. Return typed validation evidence to P12-G. On any failure, discard the
   private candidate. The active runtime, its mutation guard, owner stores,
   CityRuntime snapshots, IDs, balances, and random state remain unchanged.
   P12-G alone binds the fresh healthy guard after full B-F validation and
   publishes by one owner-controlled runtime reference swap.

P12-E does not own profile serialization/envelope, capture token, whole-graph
publication, or continuation parity coordinator. It provides complete owner
sections and tests required for P12-G.

## 6. Validation evidence required for eventual implementation

Owner suites and composition tests must cover at least:

- Empty and evolved/populated round trips for every E authority, including
  post-bootstrap political, institutional, office/tenure, property/estate,
  claim/recognition, faction/support, force/manpower/position,
  conflict/War/Battle, justice/crime, and economy state. Initial emptiness is
  not a substitute for populated fixtures.
- Exact CityRuntime capture: current Daily-v1 exports only the D-owned City
  roots and no E City slice. Reject an injected non-empty E City section or
  duplicate D field. Any future reviewed D/E split must originate from one
  snapshot revision, merge into one City hydrator, and reject mismatched
  capture IDs/revisions or a City/nested market/account/population mutation
  between capture and validation.
- City economy parity with production, free consumption, price refresh, market
  trade and ordinary merchant effects: preserve exact stock, balance, custody,
  price, item definition, and causal owner state after identical later daily
  inputs. Assert P14-A remains absent and rejects if populated.
- E provider inventory parity: selected effective configuration yields the
  exact instantiation/provider identities, while wrong flags, wrong effective
  policy, injected/unknown providers, missing providers, or unexpected
  disabled-provider state reject admission. Validate provider versions and all
  causally relevant configuration inputs.
- Every supported mutation path advances its owning revision exactly once;
  direct, transactional and daily-system mutation paths are represented.
  Failed preflight leaves both owner facts and revision unchanged. Attempts to
  mutate a detached export cannot affect the source. Mutable collection
  exposure or an untracked writer must be fixed or reported as a blocker.
- Every relation owner rejects missing, duplicate, dangling, wrong-kind,
  contradictory, or cardinality-invalid references before publication. Cover
  office-to-institution/Person, estate/property/owner, claim/recognition target,
  faction affiliation/support Person, force/manpower source and position,
  Conflict/War/Battle parent/participant/outcome links, and City market/account
  custody.
- Empty required sections round-trip explicitly; absent required sections,
  unsupported schemas, unknown populated excluded sections, and non-empty
  P8-B–E/P10/P14-A/P18/P19/P20 sections reject without live mutation.
- Service objects and derived indexes rebuild only from admitted code/provider
  identities plus captured owner facts. Check that E hydration does not
  republish Knowledge, P11 inputs, directives, merchant plans, NPC values,
  history, or events. Verify terminal domain outcomes are not applied twice.
- Private-staging failure at every owner/dependency boundary leaves the active
  runtime byte/value-equivalent for all observable owner truth, revisions,
  balances, owner bindings, and random state. Diagnostics may supplement but
  cannot replace owner-level comparison.
- Continue original and restored runtimes from the same completed daily
  boundary with the same supported inputs and compare each E owner truth and
  its D/F cross-bindings after subsequent daily execution. P12-G owns the full
  profile parity gate; E must supply owner-complete comparison evidence.
- Run the affected economy, population-economy, merchant, justice/crime/guard,
  political, property, armed-force/manpower/spatial, conflict, War/Battle and
  relevant Phase 5–8 regression suites; ALL EditMode; complete official Smoke;
  and `git diff --check` at implementation integration. Add long-run validation
  only if implementation changes long-horizon daily semantics or inventory
  finds a causal reason.

This design itself makes no test-result claim. Passing tests do not establish
P12-A readiness without every included owner's reviewed export/hydrator,
refreshed inventory, P12-G validation/publication/parity, and separate P12-A
implementation authorization.

## 7. Ownership, hotspots, and blockers

| Surface | Owner boundary | Risk / required evidence |
|---|---|---|
| `CityRuntime`, `MarketRuntime`, `MarketCounterpartyRuntime`, population-economy/account and nested item state | One concrete City owner. Current Daily-v1 fields are all in D; E has no City value section. | **Shared hotspot:** D owns the current snapshot/export/hydrator. E provider operations may mutate D-owned facts but may not export them again. Any future distinct E field requires a reviewed one-snapshot split and exclusive integration handoff. |
| `SimulationRuntime.cs`, `TesteSimulacao.cs`, effective composition and daily loop | P12-B/bootstrap owns admission evidence; E consumes the manifest and records E provider sections. | Runtime, bootstrap, and daily-loop hotspot; do not add a second capture lock or change order. Serialize edits with P18-D, P12-B, and D/E owner integration. |
| Economy and merchant services/stores | Existing domain owners, including City-owned state and any proven MerchantSystem-owned causal state. | Inventory exact effects, revisions, custody references, service state, and split from F Knowledge/active plans. Do not mark service stateless without source audit. |
| `JusticeSystem`, `CrimeSystem`, appraisal world-state, guard/crime action providers | Their actual factual owner or configured provider. | NPC consequences go to the sole D/F `NpcRuntime` projection. Separate truth from events, decision records and rebuildable appraisal projections. |
| Institution/Office, property/estate, claims, faction/support/decision, force/manpower/position, Conflict/War/Battle stores | Each current store remains its authority. | Cross-links use typed IDs and D/C roots. Preserve recognition vs fact, relation ownership, terminal outcomes and exact cardinalities. Inventory any missing core store; no aggregate diagnostic substitutes. |
| `PoliticalKnowledgeStore`, commercial sharing observations, all other Knowledge | P12-F. | Explicitly excluded from E despite some being listed as instantiated core runtime services. E may retain provider behavior only; no duplicate observation records. |
| NpcRuntime fields written by E providers; active plans/commitments | One D/E/F NpcRuntime capture under the same P12-B token and component revision vector; active commitment payloads in F. | No duplicate values/hydrators in E. Coordinate one capture and merge disjoint sections before reconstruction; do not invent an aggregate NPC revision. |
| P10 `LocalTopologyStore`, P8-B–E, P14-A, P18, P19, P20, P13 | Explicitly excluded or empty for this profile. | Any populated or unverified excluded state rejects profile admission. No later-phase feature implementation. |

This design refresh does not claim an exhaustive current owner/provider inventory. Census witnesses, diagnostics, and initial asset counts do not establish exact values or hydration. For each E owner slice, record the effective provider identity, exact mutable fields and cross-owner links, owner revision or capture identity, all supported mutation paths, detached export shape, exact staged reconstruction, and rejection fixtures before implementation. If any fact is unowned or a public mutable path bypasses the proposed snapshot boundary, block that slice and name the path; do not broaden P12-E.

## 8. Readiness and exclusions

P12-B and P12-C are COMPLETE/PROMOTED. After this current-base design receives fresh independent exact-tip review, owner-level E implementation may proceed only for a field-complete slice with exact provider evidence and a safe owner handoff. Referenced D roots must be available or represented by a reviewed typed unresolved-reference interface. CityRuntime and NpcRuntime remain serialized shared-owner hotspots. The overall E checkpoint remains open until every selected-profile owner is covered and integrated.
Explicit exclusions: P12-C identity/genesis/random roots; P12-D City identity,
population and other factual roots; P12-F Knowledge, directives, P11
ActorChoiceStore, merchant plans and active commitments; P12-G full graph
validation and publication; P14-A material flow; canonical P8-B through P8-E;
P10 Ruin/LocalTopology; P18 temporal/activity state; P19 code mods/loader;
P20 shared activities; P13 history/fork guarantees; generated-world content;
and new gameplay. Moddability remains a current architecture/review constraint,
but this contract adds no extension API or loader state.
