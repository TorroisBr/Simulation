# P12-G — Staged Composition, Whole-Graph Validation, and Publication

**Status:** Documentation-only technical proposal for independent exact-tip
review. It defines no implementation readiness and delivers no owner export,
hydrator, envelope, or publication path. P12-A remains `WAIT_DEPENDENCY` and
separately gated.

**Authored base:** P12-F branch tip
`c0849df782fe8a100af7d01083dd08d2a6deb670`. This identifies the source on
which the original G proposal was authored; it is not the current governing
input set.

**Current governing inputs (2026-10-09):** P12 canonical is
02009f9063dd252bd4b177fd6aef1e74dcd947f5 (tree
e42ef56abd6780a565c68f5b8887d397518eafe8; Assets tree
a9a7c1015a5fa3cacfdb6219b18f2f593c863174). Its current PHASE12_STATE records
P12-B through P12-F promoted within their reviewed scopes, P12-G
WAIT_DEPENDENCY on complete B-F composition and a validated live-profile
inventory, P12-A WAIT_DEPENDENCY, P13 BLOCKED, and Phase 12 OPEN. This State
supersedes dated implementation-status statements in this historical design
and older inventory snapshots.

The latest Architecture General canonical is
codex/architecture/world-identity-projection at
47eff220c7ce00f6e7c759bdc2b76780bb46f628. Its SIMULATION_ARCHITECTURE.md
blob is 25843842688239cdc3b80988b2e28dbaa16b4987, ROADMAP.md blob is
d03e144544ab25371b71db64538c0de47ae8381c, and ARCHITECTURE_STATE.md blob is
06a2ac9a3d38188c064e4b2290cc5d81108d3f66. The promoted P12 capability DAG is
ee8cca1010c8f5f37928e81849b6489bffd6a318. The same architecture lineage
contains the P12-B bounded-completion contract and Master handoff at blobs
2338be53b9bc710acd03fc43858e040ee55fdae0 and
e3ac52d9b3af9f4d4661782f78a60b39f9853d8d. Their dated INCOMPLETE status is
superseded by current P12 State; their bounded completed-boundary and fresh
admission meaning remains applicable.

The alignment records remain current: INTRADAY_EXTENSIBILITY_ALIGNMENT.md,
promoted commit 4b6dd1d38cffeaf3cc1ac3effea0f8ede8771194 (blob
a231a2a014bf58be5ce382c48a55f3654df89a61), and
MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md, promoted commit
c285466c355103d3637ac165246591b72eb7bda0 (blob
4ed6fcc60b348461e3201d4f3d480c21a3154ec3). Daily-v1 remains bounded to its
accepted P9-B authored geography profile. P10 LocalTopology, P14 material
flow, P18 timeline state, P19 module state, and P20 shared activities remain
outside its serialized payload. Current constraints for temporal identity,
player-owned mod extensibility, and one-or-more activity participants apply
to review; deferred loader work and future-profile guarantees remain deferred.

The current 299-section census, temporal cardinality, owner-family mapping,
role evidence, payload distinctions, and retained validation are reconciled
in PHASE12_OWNER_COVERAGE_INVENTORY.md under “P12-G current canonical
Daily-v1 census”. This census reconciliation is not whole-graph composition,
restoration parity, or publication capability.
## 1. Purpose and boundary

P12-G composes the exact profile sections produced by reviewed P12-B through
P12-F owner contracts into a fresh private candidate, validates their complete
typed reference graph and compatibility, proves reconstruction parity, and
hands a fully validated candidate to the one owner that publishes the active
runtime. It owns orchestration and cross-owner validation, not the semantic
schema of individual stores. B-F remain the authorities for their exact owner
sections, immutable exports, revisions, local invariants, and staged
hydrators.

G does not decide whether an owner exists or is empty. An absent owner is not
an empty owner. It consumes B's current live-composition inventory and the
explicit status of every required, explicitly empty, excluded, conditional, or
omitted noncausal owner. The current 299-section census reconciles the selected
Daily-v1 expected vector and its source identities/cardinalities; it is not by
itself proof that every evolved live provider, export, staged hydrator, or
mutation path is covered. Unknown or incomplete coverage rejects admission
before staged hydration. Current Phase State and exact implementation evidence,
not this proposal, determine which B-F capabilities have been delivered.

G supplies the bounded final restore/publication capability in the accepted
decomposition. It does not implement general storage, schema migration,
serialization, reflection, persistence providers, P13 historical/fork
guarantees, or new domain semantics. It does not rerun P9 genesis, dispatch
directives, retry actor choices, replan commitments, or reapply domain effects.

## 2. Admission manifest and section classification

Before constructing domain runtime objects, the coordinator validates an inert
envelope/value graph against a current profile admission manifest. The manifest
binds the selected `UnityBootstrap-Daily-v1` bootstrap, exact compatible
build/runtime and current-host numeric profile, selected content and provider
identities/versions, effective configuration, calendar, P9-A/P9-B lineage and
P8-A geography contract, and a successful completed-day boundary from B. It
also names each owner section's schema, expected owner identity, revision
source, cardinality, and one of these composition states:

| State | G behavior |
|---|---|
| Required | Section must be present with its exact schema and owner identity. Zero records are valid only when represented as an explicit empty section. |
| Explicitly empty | Section must be represented as empty in the profile contract, and every composed owner covered by that section must report an authoritative zero cardinality. A nonempty or unverified owner rejects. |
| Excluded | No serialized payload/section for this state is admitted. A composed owner that B and the profile matrix allow may exist only when the live inventory authoritatively proves exact zero cardinality; populated or unverified state rejects. G never drops state. |
| `OmittedNonCausalReadModel` | The live census identifies a non-authoritative read-model owner whose rows are not continuation inputs or graph-reference targets. Its known populated rows may be omitted without serialization or hydration; absence of this payload does not imply the owner is empty. |
| Conditional | B's current provider/composition inventory resolves it to required, explicitly empty, or excluded before allocation. Unresolved conditional coverage rejects admission. |

The 299 expected provider sections for the authored Daily-v1 proving
composition are reconciled in PHASE12_OWNER_COVERAGE_INVENTORY.md. The 61
fixed sections contain 48 Required and 13 Explicitly empty roles, with no
section carrying an Excluded role. All dynamic per-NPC, per-Person, and
per-City sections are Required, including sections whose current cardinality
is zero. A Required role does not mean its record count must be positive.

The fixed explicitly-empty rows are four zero-cardinality
RuntimeIdentityRegistry subsections, the ExplorableSiteStore section, six
P8-B/C/D spatial sections, and two global keyed-receipt caches. P8-A has three
Required facts: one Hex, one anchored Location, and one scale context. The
P8-B/C/D zero rows are passage-option barrier state, crossings, city/site
Location bindings, Person positions, spatial-route observations, and Person
route-plan history. These are not P8-E travel sections.

SampleScene selects the separate P9-B authored-geography Daily-v1 profile,
without the P10-A Ruin. Simulation-GeneralTest remains the P10-A
Ruin/LocalTopology proving profile. LocalTopology has no expected section in
the Daily-v1 provider vector; its absence is neither an Explicitly empty nor
Excluded witness and does not prove a composed owner is empty. P14 material
flow and P18/P19/P20 state are outside this accepted payload; absence from the
vector is not evidence of an empty owner.

The per-NPC ActorChoice temporal census provider exists, but its temporal input
section is not registered in the current expected vector. The P12-F
actor-choice input section remains a separate Required section. Do not infer
that the unregistered section is empty or excluded. The promoted P12-F source
capture already reads the same `ActorChoiceStore`: it matches the required P11
owner identity/revision witness and rejects `TemporalInputCount != 0`. G can
reuse that exact-zero exclusion proof without adding a serializable section or
a new B census API, provided it invokes F source capture before allocating any
staged domain objects. The current G composition does not yet demonstrate that
ordering. Keep the temporal section outside the 299 serializable sections
unless a separately reviewed profile contract admits it.

Provider/content identity is a compatibility input, not a security boundary.
The profile may reject incompatible schema, provider, content, or effective
configuration, or incomplete continuity coverage. It must not label
player-owned/local extension state hostile or add anti-cheat, anti-tamper, or
command-forgery validation. P19 loader work remains deferred; the manifest
describes only the selected profile and its reviewed compatibility contract.
Future deliberately composed profiles may extend that contract.

Every serialized section carries explicit owner identity, schema/version,
revision, and cardinality, including zero. The live admission inventory
separately identifies every composed owner and its classification. Known
omitted noncausal read models report observed cardinality without becoming
payload sections. Unknown, omitted, duplicate, or mismatched declarations
are not interpreted as empty. Required and Explicitly empty sections cannot
be synthesized from defaults; no state may be silently discarded.

The receipt inventory distinguishes three shapes. The two global keyed
receipt caches for NPC decision occurrences and economy keyed sales are
Explicitly empty. Crime and justice receipt IDs are Required sentinel witnesses
with one owner and local revision zero; these sentinels carry no receipt
payload. Per-NPC local-observation and Merchant-trade-state receipt rows are
Required exact-zero witnesses with no payload. City population operation
receipts are Required captured owner state, not exact-zero sentinels.

NpcDecisionStore and DomainEventStore are known OmittedNonCausalReadModel
authorities: they may have populated rows, but are not serialized or hydrated
and are not authoritative graph targets. HistoryStore is a subset of event
records and NpcChronicle is derived. Unknown owner coverage is never assigned
this classification by default. History retention, event replay, and UI-feed
parity are outside this continuation contract.

Every serialized owner section must include explicit owner identity,
schema/version, revision, and cardinality, including zero. The admission
census separately identifies every composed owner and its classification;
`OmittedNonCausalReadModel` entries also report their observed cardinality,
which may be nonzero, without becoming serialized sections. Unknown, omitted,
duplicate, or mismatched owner declarations are not interpreted as empty.
Required and explicitly empty sections cannot be synthesized from defaults. A
known-empty composed owner is not itself incompatible when B and the profile
matrix permit it, but G emits no excluded-state payload. No section may be
silently discarded because the current bootstrap happens not to populate it.

## 3. Staged composition protocol

G uses a parse/admit/stage/validate/publish protocol. Parsing may allocate
ordinary inert values, but must not allocate or register live domain runtime
objects. The active composition is read only until the final publication swap.

1. **Parse inert input.** Decode only the already selected supported envelope
   representation into inert bounded values. Check structural limits, format
   identity, section ordering/uniqueness and integrity digest. Do not construct
   domain services, invoke owner factories, or mutate the active runtime.
2. **Admit compatibility.** Compare profile/build/numeric/content/provider
   manifest, effective configuration, calendar, P9 lineage, P8-A contract,
   successful boundary and required/empty/excluded section matrix with the
   current validated bootstrap composition. Reject unsupported or incomplete
   owner inventory before domain-object allocation.
3. **Establish B/C roots.** B supplies the completed-boundary and inventory
   evidence. C stages exact typed-ID allocation/high-water roots, the shared
   record sequence, deterministic-random provenance/context, P9 historical
   manifest, and P8-A facts. D then exports/stages the RuntimeIdentityRegistry
   section from those exact IDs and its D-owned runtime entities. Preserve IDs
   and provenance; never allocate replacements, rerun genesis, or draw
   replacement randomness.
4. **Stage D factual and spatial owners.** D supplies RuntimeIdentityRegistry,
legacy SpatialNetwork, CityRuntime and NpcRuntime factual roots,
Person/materialization/population/genealogy/site owners, and detached per-NPC
factual sections. C stages the P8-A SpatialAuthority roots. Construct each
CityRuntime and NpcRuntime once from its D-owned projection.
5. **Stage E owners.** Stage E authorities and typed unresolved bindings from
their own exports. E supplies no CityRuntime or NpcRuntime factual projection.
6. **Stage F owners and merge detached NPC sections once.** Stage the five
fixed F authorities—PoliticalKnowledge, ScheduledDirective, ActorChoice,
TravelParty, and Expedition—plus inventoried Knowledge, SpatialKnowledge,
travel, active commitment, and action sections. Merge D-exported detached
NPC sections into each staged NPC exactly once through the reviewed F
boundary. F does not recreate D factual owners. The exact owner/package
mapping is reconciled in PHASE12_OWNER_COVERAGE_INVENTORY.md.

7. **Validate globally.** Run every owner-local validator, then the complete
   profile graph checks in §4. Only owner-defined derived indexes,
   registries, caches and projections may be rebuilt, and only from validated
   primary records. Any stage/validation failure abandons the private
   candidate; it does not invoke active-runtime rollback because the active
   runtime has not been mutated.
8. **Bind the candidate mutation guard.** Bind one fresh healthy mutation
guard to the complete staged graph. Confirm that the candidate has one
ownership path and no writable staging aliases. This does not make it active.
9. **Admit the restored completed boundary through B.** A
DailyCaptureEligibilityToken is in-memory and bound to its source runtime
identity, completed sequence, mutation epoch, owner vector, profile,
configuration, calendar, and day. It cannot be copied to the reconstructed
runtime, which has a fresh identity and mutation epoch. Preserve the semantic
completed boundary in the admitted roots, then call a narrowly scoped P12-B
restored-boundary admission API only after the complete graph is staged,
validated, and healthy. The API issues fresh in-memory admission bound to the
candidate runtime and preserved boundary. It must not call a current-day
getter, advance time, increment the successful-advance sequence, or claim
restore was a gameplay advance. The current runtime has no such restore
admission API; its exact contract is a required B/G design and review
dependency before implementation.
10. **Publish one coherent active composition.** TesteSimulacao is the
current bootstrap/session owner. Genesis publication is one-shot, while
Simulate reads a private SimulationRuntime field and reporting retains cached
logger/City/NPC references. Changing publishedComposition alone would leave
stale aliases active. G must route simulation and reporting through one
current-composition holder/snapshot acquired at a defined operation boundary,
then perform one atomic reference swap after restored-boundary admission.
Before that swap the old graph and health remain authoritative. Preserve the
existing lifecycle; do not assume a disposal API or add per-owner swaps or
compensating mutations.

## 4. Whole-graph invariants

G invokes exact B-F local validators and adds only cross-owner/profile
invariants:

* Every stable identity is typed; no IDs are inferred or replaced. Check
  profile-wide uniqueness across registered identity kinds and verify each
  allocator's next/high-water state can continue without collision. Preserve
  shared sequence values and causal owner revisions; reject regressions,
  disagreement between split projections, or inconsistent snapshot tokens.
* Every required reference resolves to exactly one compatible staged target
  of the declared type. Validate reciprocal links, owner-declared
  cardinalities, relation membership, definition/provider compatibility and
  cross-section bindings. Never infer a universal one-to-one relation from a
  fixture. In particular, if a separately supported profile later includes a
  P20 activity, preserve its `ActivityInstanceId` separately from activity
  definition, `PersonId`/other participant identity, and honor one-or-more
  participant cardinality; the P20 two-Person proving fixture is not a global
  cardinality constraint. P20 activity facts remain excluded from this daily
  profile.
* `ActorChoiceStore`'s `DecisionRecordId` and active
  `OriginDecisionId` string values are preserved as opaque owner values. They
  are not graph foreign keys: G does not require a matching
  `NpcDecisionStore`/decision-history row, resolve them to a target, or reject
  them because such a read-model row is omitted.
* Required owners must be present. Explicitly empty owners must report zero;
  excluded state has no serialized payload and any permitted composed owner
  must report authoritative zero cardinality. Prohibited compositions reject
  regardless of reported cardinality. Conditional owners must be resolved by
  a current composition inventory. Missing evidence is an admission failure,
  never proof of emptiness.
* P8-A has exactly one Hex, one anchored Location and one scale context for
  the selected P9-B profile. P9 outputs/manifests are retained historical
  facts; generation is not rerun. Validate selected P8/P9 lineage and owner
  references without substituting legacy spatial IDs.
* Validate each section's capture boundary, owner revision and cardinality
  against B's successful completed daily-boundary witness. Reject mixed
  boundaries, mutation during export, mismatched City/NPC projections,
  active operations, unhealthy captured state or incompatible providers.
* Validate F's causal state without execution: no external command queue, no
  in-flight ActorChoice, no processed directive replay, and no commitment
  effect reapplication. The restored graph contains the exact existing work
  and terminal outcomes for later normal domain authority.

G does not re-encode B-F domain rules or impose new gameplay cardinalities.
Any apparent conflict between owner schemas is a design/integration blocker;
G must not normalize, repair, default, or choose a winning section.

## 5. Failure atomicity and runtime health

All parsing, admission, owner hydration and global checks occur away from the
live graph. A malformed envelope, compatibility mismatch, absent owner,
invalid explicit-empty/excluded section, bad revision, unresolved reference,
allocator/cardinality violation, thrown owner hydrator, failed validation or
failed guard bind discards the staged candidate. The previous runtime
reference, owner values, revision state, guard health and ability to continue
must remain exactly as before the attempt. A rejected candidate cannot fault,
invalidate, partially reset or publish into the live runtime.

If the final publication operation fails, its implementation contract must
leave the old reference installed and the staged graph unreachable; no
per-owner commits or compensating mutations are permitted. Post-swap disposal
failure is reported through the publication owner's lifecycle and must not
retroactively publish a second graph or replay effects. Exact resource/disposal
behavior follows the identified owner API and is a required implementation
review point.

Failure-path tests compare the pre/post active reference, all included owner
truth/revisions, mutation health, and ability to advance using identical
normal input. These checks establish failure atomicity; logging/digests alone
do not.

## 6. Evidence and validation contract

P12-G implementation evidence must demonstrate:

1. **Section and compatibility matrix:** successful admission for the exact
   supported profile and explicit empty sections; rejection for each missing
   required owner, absent-as-empty substitution, unknown or duplicate section,
   populated or unverified excluded authority, unresolved conditional owner,
   and the permitted presence of populated `OmittedNonCausalReadModel` rows
   without payload; unsupported build/numeric/content/provider/config/calendar/P9/P8 identity, invalid
   digest, or incomplete live inventory. Admission rejection occurs before
   runtime-domain object allocation.
2. **Ordered staged round trip:** empty and evolved/populated profile fixtures
   export through B-F and hydrate into a different private composition while
   preserving every included owner value, typed identity, sequence/allocator,
   revision, relationship, provenance and commitment. Verify P9 genesis is
   not called and no gameplay operation executes during staging.
3. **Graph rejection:** independently corrupt duplicate/cross-kind IDs,
   allocator marks, shared sequence, owner revisions, snapshot tokens,
   definition/provider bindings, reciprocal references, relation membership,
   required owner cardinality, P8-A exact geography cardinality and B-F
   cross-section bindings. Verify rejection before publication and no repair.
4. **Atomic failure:** inject failure at each parse/admission/root/owner/
   relation/commitment/global-validation/guard-bind/publication boundary. The
   live runtime identity, every included owner value/revision, mutation guard
   health and subsequent behavior remain unchanged. No partial section is
   observable; a later valid restore remains possible.
5. **Causal no-replay:** terminal ActorChoice idempotency history and
   dispositions are retained; pending states reject. Directives and active
   commitments hydrate as data with no dispatch, retry, planning, charge,
   movement, consumption, or effect. Verify first subsequent normal domain
   operation produces the same result as an uninterrupted runtime. Preserve
   `DecisionRecordId` and `OriginDecisionId` strings opaquely without lookup
   into omitted decision/history rows.
6. **Continuation parity:** from the same successful daily boundary, run the
   uninterrupted and restored runtime with identical future inputs. Compare
   each included authoritative owner section, relations, commitments,
   allocator/sequence, random-dependent outcomes and owner revisions over
   multiple subsequent boundaries. Compare the included authoritative graph
   and future authoritative results, not `NpcDecisionStore`, `DomainEventStore`,
   `HistoryStore`, or `NpcChronicle` rows and not history/UI-feed parity.
   Diagnostic snapshots/digests are supplemental comparators only after their
   coverage of included truth is mapped; they are not export or hydration
   evidence.
7. **Regression gates:** run all domain suites affected by B-F/G, relevant
   Phase 5–8 regressions, ALL EditMode, complete official Smoke, and
   `git diff --check` before any implementation candidate promotion. Run
   long-run validation if implementation changes daily-loop or long-horizon
   behavior, per execution policy.

No runtime implementation, Unity test/regression result, capability delivery,
or P12-A readiness is claimed by this proposal. Documentation diff checks are
reported separately with the exact candidate evidence.

## 7. Dependencies, implementation gate, and exclusions

Current P12 canonical State records B-F delivered within their approved
scopes, P12-G WAIT_DEPENDENCY, P12-A WAIT_DEPENDENCY, P13 BLOCKED, and Phase
12 OPEN. The exact State governs checkpoint and closure status.

Before implementation, the refreshed design and inventory require exact-tip
independent review and the Execution Model's checkpoint acceptance. Remaining
evidence is concrete:

1. **Live inventory validation:** reconcile all 299 expected sections to every
   owner/provider composed by the normal selected bootstrap; validate the
   effective profile and dynamic roster/cardinality changes, conditional
   owners, and known omitted noncausal read models. Record objects with no
   expected section and whether they are composed. Absence is not zero.
2. **Package interface verification:** verify each current B-F export/staged
   hydrator, owner identity, schema, revision/cardinality, unresolved bindings,
   failure semantics, and dependency order at exact canonical tips. The old
   merged D/E CityRuntime and D/E/F NpcRuntime projections are not current:
   D owns factual City/NPC roots and detached NPC facts, E owns its stores and
   bindings, and F merges D-exported detached sections once.
3. **Restored-boundary admission:** independently review the bounded P12-B
   API that binds fresh admission to a healthy reconstructed runtime while
   preserving the source completed logical boundary. The source token cannot
   transfer. The API does not advance time or successful-advance sequence.
4. **Single publication boundary:** audit all TesteSimulacao consumers and
   define one authoritative holder/snapshot for simulation and reporting.
   Demonstrate one atomic swap and failure behavior against the old graph.
5. **Whole-graph evidence:** satisfy the admission, graph rejection,
   failure-atomicity, no-replay, and continuation-parity obligations in §6.

The current census, temporal roster test, Person materialization test,
source crosswalks, and retained validations are useful evidence; they do not
close these requirements by themselves. Design review, checkpoint acceptance,
canonical promotion, P12-A authorization, and Phase closure are separate gates.
P12-A remains WAIT_DEPENDENCY until the complete included owner
export/hydration set and live profile inventory are demonstrated and its
separate implementation authorization is granted. P13 remains blocked on its
own prerequisites.

Explicit exclusions:

* no general storage backend, serializer, schema migration, or cross-build/
  host compatibility framework;
* no P13 historical boundary reconstruction, arbitrary fork, or replay log;
* no P18 temporal timeline/work/availability, P19 loader/module, P20 shared
  activity payload, or retroactive future-phase guarantee;
* no new gameplay, genesis rerun, planning framework, external command queue,
  or security/authorization boundary;
* no partial publication, per-owner swap, silent omission/default, guessed
  state, or claim that diagnostics prove complete continuation.

The profile remains the accepted same-build P9-B authored-geography
Daily-v1 boundary. Any future profile that includes P10 LocalTopology, P14,
P18, P19, or P20 state needs deliberate owner admission, validated inventory,
and a reviewed compatibility contract. Future admitted activities must
preserve ActivityInstanceId independently from definition and participant
identities and permit one or more participants; exactly two is only the P20
proving fixture.

## Historical dependency refresh — 2026-09-29 (superseded by the current-base refresh below)

P18 is formally closed by marker `a49de9d`; current P18 canonical State tip is
`8ac2d78`, and its P12-B `SimulationRuntime` hotspot handoff is effective.
The P9-B/P11 composition refresh at `36e3064` passed independent exact-tip
revalidation and is promoted to P12 canonical; the prior reviewed and validated
executable tree is unchanged. The P12 live-profile, owner-coverage,
mutation-invalidation, and prerequisite checkpoint deliveries remain
outstanding. P12-A remains `WAIT_DEPENDENCY`.


## Current dependency refresh — 2026-10-09

The current P12 canonical ref observed for this refresh is
02009f9063dd252bd4b177fd6aef1e74dcd947f5. Its Assets tree is
a9a7c1015a5fa3cacfdb6219b18f2f593c863174. The Architecture General canonical
is codex/architecture/world-identity-projection at
47eff220c7ce00f6e7c759bdc2b76780bb46f628. The current Roadmap, architecture
state, promoted P12 capability DAG, P12-B bounded-completion contract, and
Master handoff are identified in the governing-input block above.

The 299-section census establishes the current section vector,
role/cardinality facts, and source ownership mapping. It does not establish
whole-graph staging, restored-boundary admission, one coherent publication
boundary, complete evolved-state inventory, P12-A readiness, or P13
readiness. This is a design refresh, not a reviewed design, accepted new
checkpoint, implementation, validation run, canonical promotion, or Phase
closure.

TesteSimulacao has cached runtime and reporting references that must move
behind one active-composition snapshot. The current capture token is bound to
its source runtime and cannot cross to a reconstructed runtime. The
restore-specific P12-B admission seam remains a design/review dependency. Do
not use an extra day advance or current-day read as a substitute.

The intraday/extensibility alignment promoted at 4b6dd1d and
multi-participant activity alignment promoted at c285466 remain review
constraints. Daily-v1 excludes P18 timeline, P19 loader/module, and P20 shared
activity payload. Any future admitted activity preserves ActivityInstanceId
separately from its definition and participant identities and allows one or
more participants; exactly two is only the P20 proving fixture. P19 loader
implementation remains deferred.
