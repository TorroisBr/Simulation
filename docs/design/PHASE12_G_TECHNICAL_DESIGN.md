# P12-G — Staged Composition, Whole-Graph Validation, and Publication

**Status:** Documentation-only technical proposal for independent exact-tip
review. It defines no implementation readiness and delivers no owner export,
hydrator, envelope, or publication path. P12-A remains `WAIT_DEPENDENCY` and
separately gated.

**Authored base:** P12-F branch tip
`c0849df782fe8a100af7d01083dd08d2a6deb670`. This identifies the source on
which the original G proposal was authored; it is not the current governing
input set.

**Current governing inputs (2026-09-28):** accepted decomposition at
`7585863` with reference refresh `a2ac5d2`; owner inventory `012e04b`
(reviewed evidence map, not live census); P12-B current-evidence design
refresh `d0761ce` (independent review PASS); P12-C refreshed design `a2ac5d2`
(independent review PASS); P12-D `dd81634` and P12-E `ca8e968`
(evidence-reference reviews PASS); and P12-F evidence refresh `ab0393b`
(independent review PASS). Review evidence for these inputs and this
proposal is tracked in independent checkpoint/review records, not inferred
from this traceability ledger. These are design artifacts, not proof that the
corresponding capabilities have been delivered; revalidate hashes and owner
interfaces before implementation.

The accepted architecture baseline is `c285466`; current intraday/extensibility
and multi-participant alignment records remain constraints. The selected daily
profile excludes P18 timeline state, P19 module/loader state, and P20 shared
activities. This bounded profile does not freeze a future Activity to one
actor: any separately admitted profile that includes P20 must preserve
`ActivityInstanceId` independently from definition and participant identities
and honor the architecture's one-or-more participant cardinality.

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
explicit status of every required, empty, excluded, and conditionally included
section. Unknown or incomplete coverage rejects admission before staged
hydration. The current inventory demonstrates gaps; this design is not evidence
that B-F delivery or the inventory gate has passed.

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

For this profile, the required set is exactly the reviewed B-F owner set:

1. B admission, compatibility, completed-boundary, mutation-health and
   quiescence evidence;
2. C identity allocators/registry and shared record sequence, P9 genesis and
   deterministic-random roots, and the selected P8-A one-Hex/one-anchored-
   Location/one-scale facts;
3. D factual roots, Person/population/genealogy relations and legacy
   spatial/site authorities;
4. E City economy projections plus all core and effective-configuration
   selected official daily-domain authorities;
5. F Knowledge, scheduled directives, P11 terminal actor-choice history, and
   only the active commitment authorities proven by the refreshed inventory.

The profile requires P8-B passage, P8-C canonical anchors/Person positions,
P8-D route Knowledge/plans, and P8-E travel sections to be explicitly empty;
the selected P8-A cardinality remains exact. The P10 LocalTopology store is
explicitly empty and rejects populated facts. P14-A material-flow state is
excluded. P18 timeline/work/availability/continuation state, P19 module/loader
state, P20 shared activities, P13 reconstruction/fork state, and
`PlaceContentStore` have no serialized payload in this profile; any composed
owner allowed by B/profile policy must be proven empty by the live inventory,
while populated or unverified state rejects. External `WorldCommand`
service/queue composition and unsupported/injected providers are prohibited
compositions and reject immediately, independent of cardinality. The exact
empty/excluded/prohibited matrix must be refreshed from the live owner
inventory before implementation; these examples do not substitute for that
evidence.

The refreshed B owner census also classifies `NpcDecisionStore` and
`DomainEventStore` rows as `OmittedNonCausalReadModel`: they may be populated
at capture, but have no P12-G payload or staged hydration and are not foreign-
key targets in the authoritative owner graph. `HistoryStore` is a subset of
the event records; `NpcChronicle` is derived. Neither changes this omission
rule. The live inventory must still identify these known owners and their
classification; unknown owner coverage is not treated as an omitted read
model. History retention, event replay, and UI-feed parity are outside this
continuation contract.

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
3. **Establish roots.** Ask B/C contracts to stage the completed logical time,
   exact typed identities, allocator high-water/next values, record sequence,
   deterministic-random provenance/context, P9 historical manifest and P8-A
   facts. Hydration preserves IDs and provenance; it never reruns genesis or
   draws randomness.
4. **Stage factual roots and domain owners.** Invoke reviewed private
   hydrators in dependency order: D root owners; the merged D/E CityRuntime
   projection; the merged D/E/F NpcRuntime projection; other E owner sections;
   then D relations and F Knowledge against available roots. Concrete owner
   ordering follows B-F declared dependencies and owner constructors. Each
   concrete CityRuntime and NpcRuntime is reconstructed once from its
   disjoint projections and a common owner snapshot/revision token. G does not
   recapture, duplicate, or independently hydrate those owners.
5. **Stage deferred causal owners.** Restore F directives only as stored,
   without processing. Restore terminal P11 choices with their exact
   dispositions/idempotency history, rejecting pending/deferred or
   `ConsumedAwaitingTerminalAttempt` state. Restore only inventoried active
   commitments after their roots and target owners exist; do not decide,
   reschedule, replan, retry, charge, move, consume or apply effects.
6. **Resolve cross-section bindings.** Consume each typed unresolved binding
   returned by B-F hydrators and resolve it against the staged owner graph.
   Missing, wrong-kind, ambiguous or multiply-owned targets reject. No
   validation path may consult current live truth to repair a staged record.
7. **Validate globally.** Run every owner-local validator, then the complete
   profile graph checks in §4. Only owner-defined derived indexes,
   registries, caches and projections may be rebuilt, and only from validated
   primary records. Any stage/validation failure abandons the private
   candidate; it does not invoke active-runtime rollback because the active
   runtime has not been mutated.
8. **Bind and publish.** After all validators succeed, bind exactly one fresh
   healthy mutation guard to the complete staged graph. Verify that the
   candidate has one ownership path and no writable staging aliases. The
   bootstrap/session publication owner performs one atomic runtime-reference
   swap. Until that swap succeeds, the prior runtime and its mutation health
   remain unchanged and authoritative. Only after swap may the owner detach or
   dispose the old runtime through its existing lifecycle.

There is one publication owner, not one swap per store, City, NPC, service, or
section. The implementation must identify that concrete owner and its
observation boundary from the live bootstrap; this document does not assume
that the existing genesis publication method already supports replacement.
If a one-reference swap cannot safely publish the complete graph, the track
returns to technical review rather than mutating the live object incrementally.

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

No implementation, test execution, validation result, capability delivery,
or P12-A readiness is claimed by this proposal.

## 7. Dependencies, implementation gate, and exclusions

P12-G implementation depends on exact reviewed B-F export/staged-hydration
contracts and their delivered capabilities, a refreshed live
`UnityBootstrap-Daily-v1` owner/provider inventory proving the included owner
set complete, and stable
cross-owner interfaces for the single CityRuntime and NpcRuntime projections.
The inventory must show each required/empty/excluded/conditional owner,
revision/cardinality source, supported mutation path and publication owner.
P12-C refreshed design `a2ac5d2` passed exact-tip review; incorporate that
reviewed contract and any later interface changes before implementation.
Designs alone do not satisfy capability dependencies. G may not treat unknown
or absent owner coverage as an empty section.

Even after G and B-F capability delivery, P12-A remains `WAIT_DEPENDENCY`
until the complete included owner export/hydration set is demonstrably
implemented, the live profile inventory is validated, and P12-A receives its
separate implementation authorization. Acceptance of this design is not
canonical promotion, save/load approval, or Phase 12 closure.

Explicit exclusions:

* no general-purpose storage backend, serializer, schema migration/version
  upgrade, or cross-build/host compatibility framework;
* no P13 historical boundary reconstruction, arbitrary fork, or replay log;
* no P18 temporal timeline/work/availability state, P19 loader/module state,
  P20 shared activity state, or retroactive future-phase guarantees;
* no new gameplay, generation rerun, planning/decision framework, external
  command queue, or security/authorization boundary;
* no partial runtime publication, per-owner swap, silent omission/default,
  guessed owner state, or claim that a candidate/diagnostic snapshot proves
  complete continuation.

The profile is bounded to its accepted same-build daily boundary. Any future
profile that includes P18/P19/P20 state requires its own reviewed owner
inventory and compatibility contract while retaining the architecture's
temporal, extensibility and participant identity/cardinality constraints.
