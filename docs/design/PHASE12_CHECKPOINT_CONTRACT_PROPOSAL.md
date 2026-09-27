# Phase 12 `UnityBootstrap-Daily-v1` Checkpoint Contract Proposal — UNAPPROVED

**Status:** proposed checkpoint contract for planning identifier `P12-A`; it
is not accepted or authorized for implementation. The earlier independent
review at `a9699e4` and targeted review against P8 `77f3e1a` predate the current
P9 manifest mapping. The full design/current-base review passed at candidate
`9fde12a`. The scoped current-base refresh review passed at `d1a8414`, covering
the refreshed P9/P10/P20/alignment references and confirming consistency with
the bounded profile; it did not repeat a full-design review. Formal P12-A
checkpoint acceptance and implementation authorization remain pending.
This proposal changes no canonical capability and makes no claim that capture,
hydration, or save/load parity currently exists.

**Historical targeted revalidation:** the earlier review used P8 canonical
`77f3e1a47a1e007492a794ea777d681a21a36d09` and predates the current P9
manifest mapping. It remains historical evidence only; it is not a current-base
review of this candidate.

**Profile:** `UnityBootstrap-Daily-v1`, the bounded daily profile recommended
by the refreshed entry and technical proposals. Its scope is the SampleScene-
selected `Simulation-GeneralTest.asset`, the validated Unity
`TesteSimulacao.InitializeSimulation` bootstrap, repository built-in providers,
and one `SimulationRuntime`, captured only after a successful daily advance.
The selected asset enables P9-B authored geography and publishes exactly one
P8-A Hex and one Location anchored to it before day one. P9-B is present on
`codex/phase9/canonical` at `d9a62d7c6bea242653c2d68cc0a70911bb5ed1bf`,
including implementation tip `00395ef80cfa2364d34ed2170e0735d3a4b1513d`.
P9-A-only configs are incompatible with this
profile. Any separately retained P9-A-only profile requires its own identity
and explicit admission that rejects P9-B/geography. This proposal adds no
cross-host guarantee, P13 history/fork behavior, generated P9/P10 worlds, P11
pending commands, P19 modules, or P20 shared activities.

**Revalidated references:** P8 State `470667d37863384edadb3d93ef64d8004aff46a3`;
architecture `c285466c355103d3637ac165246591b72eb7bda0`; P9-A code promotion
`43f08b3` / current P9-B promotion-record State/status `14a2e8e`; P9-B canonical integration
`d9a62d7c6bea242653c2d68cc0a70911bb5ed1bf` (implementation tip
`00395ef80cfa2364d34ed2170e0735d3a4b1513d`); P11 code promotion `0cd4281` /
closure State `308e24d`; current P18 State `311baa9` (P18-A/B/C promoted;
P18-D implementation blocked); P14 State `f8a61fe` (P14-A daily and promoted);
P20 Entry Architecture `2f9c93b`, Technical Design `6a0d164`, and proposed
P20-A checkpoint `1a6ad537` (design and proposal reviewed; checkpoint
acceptance/implementation remain pending). Its two-Person fixture is bounded
proof evidence, not universal participant cardinality or a P12 dependency.
Both architecture alignment records remain current. P10-A is the user-approved
Ruin/LocalTopology profile; design and checkpoint review passed at `345dcbc`,
and it is `READY_FOR_IMPLEMENTATION`, but no P10 runtime capability is
delivered. The P9-B promotion record is current at State/status tip `14a2e8e`;
use it alongside canonical code tip `d9a62d7` for P9-B capability and profile
evidence.
Refreshed P12 entry proposal:
`a257092471607520f7da7f056f465bbb3f5384d3`; current P12 technical proposal
refresh: `e0023d2` on `codex/phase12/ContinuationTechnicalDesign`.

**Authority:** `docs/SIMULATION_ARCHITECTURE.md` controls semantic boundaries.
The Phase 12 Brief, refreshed P12 entry/technical proposals, Phase 8 State,
and `INTRADAY_EXTENSIBILITY_ALIGNMENT.md` plus
`MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md` constrain this proposed contract.

## 1. Contract objective and compatibility

For one compatible supported profile, the same causal state at a completed
daily boundary and the same subsequent inputs must yield the same future
authoritative results whether execution continues uninterrupted or continues
from a captured and restored runtime. Save is a continuation-state transfer,
not replay, a selective History export, or an event-sourced reconstruction.

The proposed compatibility envelope is exact: profile/schema version,
simulation build and Unity/runtime identity, the supported current-host numeric
profile (`unity-float32-current-host:v1` where applicable), official
behavior-bearing content/definitions, resolved effective configuration and
calendar, and the built-in provider/system set. Unknown required sections,
missing required definitions/providers, changed causal content or
configuration, invalid references, integrity failure, and incompatible build
or runtime identity reject capture/load as applicable. No best-effort defaults,
cross-host equivalence, migration, or silent repair is promised. Exact payload
encoding and storage medium remain implementation choices; the envelope must
have a versioned schema and deterministic canonical digest encoding.

The profile boundary is the return of a successful `SimulationRuntime.AdvanceDay`.
Capture is allowed only while the owning simulation thread is quiescent and
before any later authoritative write. The eligibility token is bound to that
runtime and completed day/advance sequence; any supported authoritative
mutation after the boundary invalidates it. Capture during an advance or
transaction, after an exception/fault, during reentrant/concurrent work, or
from a stale token is rejected. A day increment alone is not evidence of a
successful boundary.

This profile excludes between-day commands after the eligible boundary,
external queued WorldCommands, pending or mid-dispatch P11 actor-choice inputs,
P9/P10 generated content, P19 module-owned state or retrofit, P20 shared
activities, arbitrary constructor-composed runtimes and injected providers,
P8 facts beyond the selected P8-A Hex/anchored-Location/scale set, all P8-B
passage, P8-C City/Site anchors and Person positions, P8-D route Knowledge/plans,
P8-E travel, and intraday capture. `PlaceContentStore` and other optional stores
not composed by this bootstrap are out of scope. The profile includes explicit
empty sections for its declared empty owners and the exact populated P8-A
authority specified here; unsupported populated state is rejected.
Unsupported populated state must reject capture/load; it cannot be omitted
silently.

In particular, the selected Unity bootstrap has no external WorldCommand
service/queue composition; adding that service or any queued external command
input is outside this profile and must reject admission rather than be omitted.
Where the promoted P11 `ActorChoiceStore` is composed, preserve its complete
input/disposition history, duplicate-command identity history, and next input
sequence so terminal idempotency remains intact. `Rejected`, `AttemptReturned`,
and `AttemptThrew` are terminal and may be captured; `Pending` (including
deferred inputs) and `ConsumedAwaitingTerminalAttempt` are in-flight causal
inputs and reject capture. Loading an envelope with an in-flight input also
rejects before publication. The daily profile never drops causal input state
silently or claims its effects were captured when they were not.

## 2. Causal state and owner coverage

Every section is exported from and hydrated through its authoritative owner.
The envelope is inert value data; it is never a second mutable world. Empty
required sections are explicit. If any included populated owner lacks a
complete export and hydration path, the profile cannot claim closure.

| State group | Required v1 continuation state | Scope rule / rebuild boundary |
|---|---|---|
| Profile and execution context | Profile/schema identity; immutable build/runtime/numeric identity; exact official content and built-in provider manifest; effective configuration and calendar identity/values. | Resolve all compatibility inputs before publishing a staged runtime. Asset/display names alone do not establish compatibility. |
| Time and boundary | Absolute day, effective calendar, completed `AdvanceDay` sequence and daily boundary precision. | No intraday instant or same-instant scheduler state is claimed. Recreate time/calendar services from validated values. |
| Semantic and runtime identity | Each existing typed semantic/runtime ID; per-kind `RuntimeIdAllocator` next/high-water state; `SimulationRecordSequence`; any allocator used by an included owner. | Never allocate replacement IDs for persisted entities. Rebuild `RuntimeIdentityRegistry` and lookup indexes from owner sections. Definition IDs, Person IDs, NPC runtime IDs, and domain IDs remain distinct. |
| Causal randomness | Built-in `DeterministicRandomSource` seed and any required versioned stream/draw context used by supported consumers; deterministic demographic fallback inputs/state when enabled. | Pin built-in source/providers. Reject injected or unversioned random implementations. A seed alone is insufficient unless the included consumers' draw behavior is covered. |
| City, market, population economy | City and market identities; mutable balances, inventory/stock, population aggregates and other future-affecting owner state; relevant revisions and references. | Resolve item/city/account definitions against exact content. Rebuild only owner-defined derived price/query projections. |
| NPC roster and condition | Configured `NpcRuntime` IDs and membership; condition/life/status, current action/behavior state, current/destination legacy location/city, hidden-day counters, inventories, money and future-affecting plans. | Resolve actor/action/content references by stable IDs. Preserve configured roster order only when the existing domain contract uses it as a causal tie-break; never use order as identity. |
| Persons and genealogy | Any populated `PersonStore`, `GenealogyStore`, residence/lifecycle/population owner facts, including PersonId, birth/death dates, parentage, residence, aggregate counts and actual materialization links. | The bootstrap creates no Person records for configured NPCs: those rows remain NPC-only and do not receive PersonIds. Never infer a PersonId from an NpcData definition or NpcRuntime. Age/maturity are derived; materialization cannot change aggregate population. |
| Legacy spatial, sites, and exploration | Bootstrap `SpatialNetworkRuntime` locations/routes; `ExplorableSiteStore`; site/exploration facts, supported City/Site anchors and legacy position links. | Preserve current legacy IDs/links. The P8-A Hex/Location described below is separately owned canonical geography; do not infer or replace it from legacy routes or runtime locations. |
| P8-owned geography and spatial authorities | Selected P9-B output: exactly one P8-A `SpatialAuthorityStore` Hex `hex/sample-origin` at `(0,0)` under `axial-hex-v1`, terrain `terrain/sample-plains`/`sample-world-v1`, one Location `location/sample-origin` anchored to that Hex, and one world scale context (convention `world-scale/Simulation-GeneralTest/v1`, source `profile/Simulation-GeneralTest` version `1`, distance `1 km` per neighbor step). | Export and hydrate all P8-A facts through the owning authority, including coordinate convention, terrain and revision pair, anchor, scale identity/source/version/value/unit, and P8 invariants. Preserve exactly one Hex and one Location; reject missing, duplicate, partial, malformed, or extra geography. P8-B passage, P8-C City/Site anchors and `PersonSpatialPositionStore`, P8-D `SpatialRouteKnowledgeStore` and `PersonRoutePlanStore`, and P8-E travel are explicit empty sections and populated state rejects. Do not infer P8 identity from legacy `SpatialNetworkRuntime`. P8-E is not a blanket dependency. |
| Travel and expeditions | Supported active travel state/progress; `TravelPartyStore` identity, ordered members, costs and lifecycle; `ExpeditionStore` objective/progress/site/party links; merchant plan state when composed. | Preserve reciprocal bindings and current domain progress. `TravelPartyId` is a current domain identity, not a general ActivityId. It does not define Activity-to-actor cardinality. |
| Directives, daily actions, and actor-choice ingress | `ScheduledDirectiveStore` stable ID, scheduled day, mode/operation, actor, action definition, status and disposition day; actor's current action/plan state. Where composed, full P11 `ActorChoiceStore` records/dispositions, input sequence, and duplicate WorldCommand-ID history. | Rebuild actor indexes. Do not reapply processed directives. Preserve terminal actor-choice idempotency history; reject `Pending` or `ConsumedAwaitingTerminalAttempt` inputs at capture/load. External WorldCommand queue/service composition is unsupported by this bootstrap and must be rejected if introduced. |
| Knowledge | Supported NPC commercial, spatial and exploration Knowledge and any populated supported political/crime Knowledge: holder, observations, provenance, freshness/observed/received day and stale-check revisions. | Knowledge is authoritative perspective state, not derivable from current truth. Rebuild indexes, never regenerate observations on load. |
| Optional official daily domains | Included owner truth, active commitments, revisions, and causal provider facts for effective-configuration-selected economy/demography/mortality, merchant/trade/Knowledge sharing, crime/justice/social appraisal and guard-crime systems actually composed. P14-A material flow is included only if the selected authored City/source is configured and composed; the current GeneralTest City assets have no P14-A source configuration. | Resolve exact built-in provider/policy versions from the manifest. An uncomposed service is represented by the profile/provider set, not a fabricated store. Injected providers reject profile admission. Any future P14-A config must add source/settlement/store/item identities, calendar/config/policy, source application and consumption facts/order, stock and reconstruction coverage, or be rejected by this profile. |
| Populated core political and military authorities | Any supported populated `InstitutionStore`, `OfficeStore`, property/estate, claims/recognition, faction/support, political Knowledge/decision, armed force/manpower/position, local topology, Conflict, War or Battle state and owner revisions. | Empty authorities are valid where composition creates them. Preserve each domain owner and restore relations only after referenced owners. This adds no political/military gameplay loop or unsupported provider. |
| Revisions, sequences and mutation health | Causally read owner revisions, stale-plan inputs, allocator/sequence values and guard health. | Capture rejects a faulted mutation guard. Validate allocator monotonicity. Restore uses a fresh guard bound only after staging validates; derived fingerprints may be recomputed only by their owner. |
| History, event records and diagnostics | No general History/event/log preservation as continuation state. | `WorldStateSnapshot`, canonical output/digests, logs, Chronicles, and `NpcDecisionStore`/event history records are not save DTOs or replay logs. The populated `PoliticalDecisionStore` remains included as owner state above. Use diagnostic projections only as parity comparators after auditing their coverage against this inventory. |

All persisted references are typed identities validated for uniqueness, target
existence, definition compatibility, relation cardinality, reciprocal bindings
and owner invariants. Dangling/wrong-kind IDs, invalid states, duplicate
identities, inconsistent progress or allocator marks, and malformed references
reject the whole envelope. Non-materialized or dormant Persons remain
resolvable by PersonId without requiring an `NpcRuntime`.

## 3. Capture eligibility and complete owner export

The capture contract has these gates:

1. **Supported composition admission:** the runtime matches the validated
   `UnityBootstrap-Daily-v1` build, providers, content, effective config and
   calendar. Unsupported composition, excluded populated owners or providers
   reject eligibility.
2. **Completed-boundary token:** the bootstrap issues a token only after
   `AdvanceDay` returns successfully. It names the runtime identity, absolute
   day and completed advance sequence. Any supported authoritative mutation
   afterward invalidates the token; a later successful advance issues a fresh
   one.
3. **Quiescent owner export:** capture occurs on the simulation owner thread,
   between operations. Each owner emits immutable values tagged with the same
   runtime/boundary. The coordinator requires every profile section, including
   explicit empty sections, verifies no section changes during capture, and
   discards the candidate on mismatch.
4. **Complete causal coverage:** all included owner truth, Knowledge, active
   commitments, provider context, identities, relevant revisions, sequences
   and randomness are represented. No partial owner omission, regeneration,
   substitution, or conversion of event/history records into truth is allowed.
5. **Seal:** after identity/reference/owner validation, canonicalize sections
   in stable identity order, compute the integrity digest, and return one sealed
   envelope. Capture failure emits no partially usable save and leaves the
   running world unchanged.

Capture before/during `AdvanceDay`, within a transaction/handler, after a
throw/fault, under reentrant/concurrent work, or after a stale token is
rejected. Diagnostics-only reads do not invalidate eligibility. The existing
daily path is not a global all-domain transaction, so the capture lifecycle
token and owner export protocol are required capabilities; current snapshot
construction is not proof of atomic capture.

## 4. Staged restore and atomic publication

Restore follows this dependency sequence:

1. Parse into inert values. Verify schema/required sections/digest, profile,
   build/runtime/numeric identity, content/provider manifest, effective config,
   calendar and the boundary before allocating live objects.
2. Resolve immutable definitions/providers. Create a fresh private staging
   composition; do not clear, mutate, or reuse the currently published
   runtime.
3. Stage time and all existing identities/high-water marks. Validate global
   uniqueness and allocator monotonicity without allocating replacement IDs.
4. Restore factual roots through owner hydration factories: Cities/markets/
   accounts, aggregates, Persons and NPCs, then sites and supported legacy
   spatial facts. Preserve NPC-only rows without synthesizing Person identity.
5. Restore owner relations and Knowledge by stable references: residence and
   genealogy; spatial/domain anchors; political/institutional relations;
   optional official domains; then armed-force/conflict/war/battle relations
   in actual owner constructor dependency order.
6. Restore directives and active travel/expedition/merchant commitments.
   Validate reciprocal actor/member/site bindings, progress, costs and
   disposition. Hydration does not run, replan, cancel or replay work.
7. Recreate the exact built-in random/provider/system composition and causal
   state. Rebuild runtime registries, indexes, caches and read-only
   projections. Bind one fresh `AuthoritativeMutationGuard` only after all
   included stores pass bind and validation checks.
8. Run owner invariants, cross-owner identity/reference checks, profile
   compatibility checks, applicable spatial checks and canonical diagnostic
   comparison for fields diagnostics actually cover. Diagnostics are evidence,
   never restore authority.
9. Publish the validated staged runtime with one owner-controlled reference
   swap. Until the swap, the current runtime remains authoritative; a failure
   leaves it untouched and the candidate unreachable. Detach/dispose the old
   runtime only through its owner lifecycle after publication.

Direct field injection, reflection serialization, silent repair, default
substitution for missing causal state, reallocated persisted IDs, second
writable worlds and partial publication are disallowed. An owner lacking a
reviewed complete export/hydration path blocks full-profile closure.

## 5. Continuation parity and closure gates

The proposed checkpoint closure sequence is dependency-ordered. These are
descriptive gates only, not accepted checkpoint IDs:

1. **Profile and boundary contract accepted:** record this support matrix,
   exact compatibility policy, daily capture boundary, admission/rejection
   behavior and excluded-state handling. This does not authorize code.
2. **Owner export closure:** mechanically enumerate every owner reachable
   from the validated bootstrap and `SimulationRuntime` composition. For each
   included owner, demonstrate complete immutable export, stable IDs,
   references/cardinality and owner revisions; any unsupported populated
   owner prevents the profile from claiming capture completeness.
3. **Staged hydration closure:** each included owner has a reviewed hydration
   factory and dependency order; malformed, missing, incompatible or dangling
   state rejects before publication; the live runtime is unchanged on failure.
4. **Atomic capture/restore integration:** enforce lifecycle eligibility,
   consistent owner snapshots, staged candidate isolation, validation, fresh
   mutation-guard binding and one runtime reference swap. No gate passes by
   reusing `WorldStateSnapshot` as a save DTO.
5. **Owner round-trip and admission evidence:** for every included owner,
   cover empty/populated data, stable IDs, Knowledge, active commitments,
   revisions, allocator constraints and relevant relationships. Reject capture
   before/during/after failed advances, after invalidating mutations, on a
   faulted guard, unsupported providers/compositions and out-of-profile state.
6. **Compatibility and failure evidence:** independently vary schema, build/
   runtime/numeric identity, provider/content manifest, config/calendar,
   required sections, digest and references. Prove rejection before publish
   and preservation of the currently live runtime.
7. **Continuation parity and repository gates:** from the same captured daily
   boundary, run original and restored worlds with identical next inputs and
   compare every included owner after each day, including Knowledge,
   commitments, identities, revisions and RNG/allocator-dependent results.
   Run affected domain/Phase 5–8 suites, ALL EditMode and complete official
   Smoke; add long-run validation if daily-loop or long-horizon semantics
   change; require `git diff --check` before promotion.

Phase 12 closure is not achieved until every supported owner has both export
and hydration coverage and parity passes for the declared profile. A clean
diagnostic digest alone is not closure evidence when the diagnostic projection
omits causal fields.

## 6. Conditional dependencies and exclusions

- **P8:** P8-A through P8-E are canonical at the current impact-refresh
  baseline. The selected profile requires exact P8-A geography output from the
  P9-B profile on current canonical and complete owner export/hydration. Preserve
  one Hex, one anchored Location, terrain/revision provenance and world-scale context.
  P8-B/C/D/E remain explicitly empty and reject populated state. Legacy routes
  do not substitute for P8 identity. P8-E is not a blanket continuation
  prerequisite. P9-B is on current P9 canonical at `d9a62d7`; this P12
  proposal does not establish P12 export/hydration coverage or checkpoint gates.
- **P18:** the daily profile does not compose P18 temporal state and does not
  claim intraday continuation. If a future supported profile composes it,
  preserve `(worldId, profileId, absoluteDay)` boundary identity, tick
  quantum/version, effective `MaxDispatchesPerInstant` or equivalent dispatch
  limit, `(ownerId, workId, revision, occurrence)` work identity, exact logical
  time, causal wave and same-instant order, pending boundary/work,
  occurrence/sequence, and owner idempotency/effect state. Preserve sealed
  external inputs with target `LogicalTick`, accepted sequence/order, and
  boundary state. Include P18-B ActivityInstanceId/lifecycle
  revision/receipts and participant commitments/availability, plus P18-C
  PersonId-keyed decision/attempt state only where composed. P18-A/B/C are
  promoted; P18-D remains blocked and the legacy daily path remains authoritative.
- **P20:** shared activities are outside this profile. Any later profile that
  includes them must preserve stable `ActivityInstanceId` separately from its
  definition and participants (using `PersonId` for Person participants without
  requiring materialized `NpcRuntime`), one-or-more participant cardinality,
  partial formation/decisions, roles where applicable, agreements, reservation
  intents and committed intervals, scheduled start, shared lifecycle/context,
  applied participant effects and pending causal work/order/idempotency. The
  reviewed two-Person fixture in the reviewed P20 design does not establish a
  universal count or role policy. Do not infer one-Activity-to-one-actor
  cardinality or split a shared instance into actor-owned copies. The reviewed
  P20 design is not implementation authorization or a blanket dependency.
- **P9-A/P9-B/P10:** the selected bootstrap retains P9-A's historical
  `unity-authored-bootstrap/genesis-v1` lineage and publishes the distinct P9-B
  geography contract `unity-authored-bootstrap/authored-geography-v1` with
  stage `p9.genesis.authored-geography/v1`. Preserve the selected P9-B
  profile/schema identity and fingerprint, inherited P9-A stage identities,
  stage graph, seed/config/calendar, selected inputs, outputs and provenance,
  including all P8-A facts, without rerunning genesis. P9-B is on current P9
  canonical at `d9a62d7`. P9/P10 generated-world content remains excluded; P10
  is not a dependency of P12-A.
- **P11:** the selected bootstrap has no external WorldCommand
  service/queue composition; adding it is unsupported and must be rejected.
  When `ActorChoiceStore` is composed, capture its full input/disposition and
  idempotency history plus sequence. Reject pending/deferred and
  `ConsumedAwaitingTerminalAttempt` records; preserve terminal rejected,
  returned, and threw records. Applied effects remain represented by owner
  truth and are never replayed from record summaries.
- **P19:** modules, module-owned state and retrofit are deferred. Official
  configuration-selected built-in providers remain distinct from code mods.
- **P13:** historical reconstruction/fork of every actually simulated
  boundary, replayable input/history and independent forks remains a separate
  guarantee. Passing daily save/load parity does not close or narrow P13.

## 7. Proposal boundary and review gates

This document is the bounded P12-A contract proposal and its planning
identifier record. It does not amend `SIMULATION_ARCHITECTURE.md`, establish
an accepted checkpoint, or authorize implementation.

The full design/current-base review passed at `9fde12a`; the scoped current-base
refresh review passed at `d1a8414` and did not repeat the full-design review.
Remaining gates are formal acceptance of the checkpoint contract and
scope/status through the repository workflow, separate implementation
authorization, and demonstrated complete staged owner export/hydration plus
the live profile inventory. The read-only gap ledger at
`PHASE12_OWNER_COVERAGE_INVENTORY.md` records the current per-owner evidence;
it does not satisfy those capability gates or authorize implementation.
Preserve the selected P9-B profile/stage identity and manifest provenance for
the `TesteSimulacao` path; generated P9/P10 world content remains excluded. Owner
export/hydration and parity requirements remain capability gates for closure,
not permission to claim completion before implementation evidence exists.

No unresolved product or canonical semantic choice is required for this
bounded profile by the reviewed documents. Expanding to cross-host/runtime
compatibility, migration, intraday capture, generated/mod-owned content,
pending P11 inputs or P20 activities requires a separately scoped and accepted
profile contract.

## 8. Sources rechecked

- `docs/SIMULATION_ARCHITECTURE.md`, `docs/EXECUTION_MODEL.md`,
  `docs/phases/PHASE12_BRIEF.md`, and refreshed P12 entry/technical proposals.
- Current canonical `docs/PHASE8_STATE.md` at `470667d`, P9-A promotion
  `43f08b3`, P9-B promotion-record State/status `14a2e8e`, P9-B canonical
  integration `d9a62d7c6bea242653c2d68cc0a70911bb5ed1bf` (implementation tip
  `00395ef80cfa2364d34ed2170e0735d3a4b1513d`),
  P11 promotion `0cd4281` and
  closure State `308e24d`, current P18 State `311baa9`, P14 State `f8a61fe`,
  P20 Entry Architecture `2f9c93b`, Technical Design `6a0d164`, and proposed
  P20-A checkpoint `1a6ad537`, P10-A reviewed design/checkpoint candidate
  `345dcbc` (ready, runtime not delivered), architecture
  `c285466`, `docs/architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md`, and
  `docs/architecture/MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md`.
- `Assets/_Project/Scripts/TesteSimulacao.cs`, `SimulationConfigData.cs`,
  `SimulationRuntime.cs` composition and `AdvanceDay`/`TryAdvanceDay`,
  `RuntimeIdentity.cs`, `DeterministicRandom.cs`, and
  `Diagnostics/WorldStateSnapshot.cs`.
- Current owner implementations for Person/population/genealogy; City/market;
  travel parties/expeditions/directives; Knowledge; spatial/P8 presence and
  route plans; political, crime, armed-force, conflict, war and battle stores;
  plus owner-specific revisions and mutation guard composition.
