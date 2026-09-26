# Phase 12 `UnityBootstrap-Daily-v1` Checkpoint Contract Proposal — UNAPPROVED

**Status:** proposed checkpoint contract; independent review **PASS** for
content commit `a9699e46aaffd12616353975b447caed12ff3d27`. The review used
`codex/phase8/canonical` at `c5b2e06b534f4b2af38f10e6510b10800aa8b28c`,
including architecture refresh `c285466c355103d3637ac165246591b72eb7bda0`,
and both alignment records: intraday/extensibility at `c285466c355103d3637ac165246591b72eb7bda0`
and multi-participant activity at `4b6dd1d38cffeaf3cc1ac3effea0f8ede8771194`.
No blockers were found. If P9 genesis outputs enter this supported profile,
recheck their stage/contributor identity, deterministic order, random context,
provenance and output-capture coverage; capture generated outputs rather than
rerunning historical genesis. Formal human acceptance remains pending. This
document assigns no accepted Phase 12 checkpoint IDs, changes no Phase
Brief/State/Roadmap, and authorizes no code implementation. It makes no claim
that capture, hydration, or save/load parity currently exists.

**Profile:** `UnityBootstrap-Daily-v1`, the bounded daily profile recommended
by the refreshed entry and technical proposals. Its scope is the validated
Unity `TesteSimulacao.InitializeSimulation` bootstrap, the repository's built-in
providers, and one `SimulationRuntime`, captured only after a successful daily
advance. This proposal adds no cross-host guarantee, P13 history/fork behavior,
generated P9/P10 worlds, P11 pending commands, P19 modules, or P20 shared
activities.

**Baseline:** current `codex/phase8/canonical` at
`c5b2e06b534f4b2af38f10e6510b10800aa8b28c`, containing architecture/roadmap
refresh `c285466c355103d3637ac165246591b72eb7bda0`. P8-A through P8-D are
canonical. P8-E remains a design-approved candidate with implementation and
promotion pending in the current Phase 8 State. Refreshed P12 entry proposal:
`a257092471607520f7da7f056f465bbb3f5384d3`; current P12 technical proposal
base: `f62e4fa16fca9de274515a693c7d71a1d605b129`.

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
external queued WorldCommands, P11 actor-choice inputs, P9/P10 generated
content, P19 module-owned state or retrofit, P20 shared activities, arbitrary
constructor-composed runtimes and injected providers, non-empty P8 physical
geography/terrain/passage worlds, and intraday capture. `PlaceContentStore` and
other optional stores not composed by this bootstrap are out of scope. The
profile includes empty/default state for its declared core owners and populated
core-authority state only when that state is within the listed profile
contracts. Unsupported populated state must reject capture/load; it cannot be
omitted silently.

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
| Legacy spatial, sites, and exploration | Bootstrap `SpatialNetworkRuntime` locations/routes; `ExplorableSiteStore`; site/exploration facts, supported City/Site anchors and legacy position links. | Preserve current legacy IDs/links. Non-empty P8 Hex geography, terrain and passage worlds are excluded. Do not infer canonical physical geography from legacy routes or runtime locations. |
| P8-owned spatial presence and route Knowledge | If present and supported by the selected profile: P8-C City/Site anchor bindings and Person positions; P8-D `SpatialRouteKnowledgeStore` observations (holder, typed subject/value, confidence/precision, observed/received days, full provenance and Knowledge revision); and `PersonRoutePlanStore` owner, policy/version, route legs/evidence, knowledge basis, decision/day/status/history and plan/store revisions. | These stores are not populated by the ordinary NPC bootstrap. Include only state whose referenced P8 facts and identities are supported; non-empty P8 physical geography/passages or plans requiring out-of-profile Hex endpoints reject this profile rather than being dropped. The ordinary legacy route/knowledge model remains separate. P8-D is canonical; P8-E is not a blanket dependency. |
| Travel and expeditions | Supported active travel state/progress; `TravelPartyStore` identity, ordered members, costs and lifecycle; `ExpeditionStore` objective/progress/site/party links; merchant plan state when composed. | Preserve reciprocal bindings and current domain progress. `TravelPartyId` is a current domain identity, not a general ActivityId. It does not define Activity-to-actor cardinality. |
| Directives and daily actions | `ScheduledDirectiveStore` stable ID, scheduled day, mode/operation, actor, action definition, status and disposition day; actor's current action/plan state. | Rebuild actor indexes. Do not reapply processed directives. External pending commands and P11 input state are excluded. |
| Knowledge | Supported NPC commercial, spatial and exploration Knowledge and any populated supported political/crime Knowledge: holder, observations, provenance, freshness/observed/received day and stale-check revisions. | Knowledge is authoritative perspective state, not derivable from current truth. Rebuild indexes, never regenerate observations on load. |
| Optional official daily domains | Included owner truth, active commitments, revisions, and causal provider facts for effective-configuration-selected economy/demography/mortality, merchant/trade/Knowledge sharing, crime/justice/social appraisal and guard-crime systems actually composed. | Resolve exact built-in provider/policy versions from the manifest. An uncomposed service is represented by the profile/provider set, not a fabricated store. Injected providers reject profile admission. |
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

- **P8:** P8-A through P8-D are canonical at this baseline. The profile
  preserves supported P8-C/P8-D state only when its owner facts and references
  fit the support matrix; non-empty physical P8 Hex/terrain/passage worlds are
  excluded. P8-E is a design-approved candidate, with implementation and
  promotion pending, and is not a blanket continuation prerequisite.
- **P18:** daily-boundary `UnityBootstrap-Daily-v1` has no blanket P18
  dependency. Capturing intraday boundaries or P18 activities/availability/
  pending due-work requires the relevant promoted P18 temporal identity,
  ordering, state and hydration/integration capabilities. A daily-only save
  cannot claim intraday continuation.
- **P20:** shared activities are outside this profile. Any later profile that
  includes them must preserve one stable Activity identity and definition /
  version separately from participant identities (using `PersonId` for Person
  participants without requiring materialized `NpcRuntime`), roles, agreements,
  reservations, scheduled start, shared lifecycle/context, effects already
  applied and pending causal work. Do not infer an Activity-to-single-actor
  relationship or split a shared instance into actor-owned copies. P20 is not
  a blanket dependency for this daily profile.
- **P9/P10:** generated worlds/outputs and generation provenance are excluded.
  Historical generation output must be captured rather than regenerated if a
  future explicitly supported profile includes it.
- **P11:** external command queues and pending actor-choice inputs are not
  composed by the selected bootstrap and are excluded. Already-applied effects
  remain represented by their owner truth, not replayed from record summaries.
- **P19:** modules, module-owned state and retrofit are deferred. Official
  configuration-selected built-in providers remain distinct from code mods.
- **P13:** historical reconstruction/fork of every actually simulated
  boundary, replayable input/history and independent forks remains a separate
  guarantee. Passing daily save/load parity does not close or narrow P13.

## 7. Proposal boundary and review gates

This document is a proposed checkpoint-contract input. It does not amend
`SIMULATION_ARCHITECTURE.md`, Phase 12 Brief/State, Roadmap, or Phase 8 State.
It assigns no accepted P12 checkpoint IDs and authorizes no implementation.

Remaining gates are formal human acceptance of this contract, then
establishment of actual checkpoint scope/status and separate implementation
authorization through the repository workflow. The owner export/hydration and
parity requirements are capability gates for closure, not permission to claim
completion before implementation evidence exists. The independent review's
conditional P9 recheck applies only if genesis outputs enter the supported
profile; they remain excluded from this proposal.

No unresolved product or canonical semantic choice is required for this
bounded profile by the reviewed documents. Expanding to cross-host/runtime
compatibility, migration, intraday capture, generated/mod-owned content,
pending P11 inputs or P20 activities requires a separately scoped and accepted
profile contract.

## 8. Sources rechecked

- `docs/SIMULATION_ARCHITECTURE.md`, `docs/EXECUTION_MODEL.md`,
  `docs/phases/PHASE12_BRIEF.md`, and refreshed P12 entry/technical proposals.
- Canonical `docs/PHASE8_STATE.md` at `c5b2e06`, including P8-D promotion and
  the pending P8-E candidate; `INTRADAY_EXTENSIBILITY_ALIGNMENT.md` at
  `c285466`; and `MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md` at `4b6dd1`.
- `Assets/_Project/Scripts/TesteSimulacao.cs`, `SimulationConfigData.cs`,
  `SimulationRuntime.cs` composition and `AdvanceDay`/`TryAdvanceDay`,
  `RuntimeIdentity.cs`, `DeterministicRandom.cs`, and
  `Diagnostics/WorldStateSnapshot.cs`.
- Current owner implementations for Person/population/genealogy; City/market;
  travel parties/expeditions/directives; Knowledge; spatial/P8 presence and
  route plans; political, crime, armed-force, conflict, war and battle stores;
  plus owner-specific revisions and mutation guard composition.
