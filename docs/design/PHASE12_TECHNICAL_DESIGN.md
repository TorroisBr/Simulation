# Phase 12 — Technical Design Proposal: Daily Continuation

**Status:** Technical design proposal for `UnityBootstrap-Daily-v1`, based on refreshed entry proposal `a257092471607520f7da7f056f465bbb3f5384d3` as carried by base `fd7f39f4a3f6786bab687f35c5eddc43309fdc6b`. This document proposes no checkpoint IDs, changes no Phase State/Brief/Roadmap, and authorizes no implementation. It does not claim the architecture recommendation is canonically approved or that save/load exists.
**Independent technical design review:** PASS at content commit
`8577ba589a0f9b40738fcf8738eee9589563d7b8`. The later lifecycle seam must
invalidate capture eligibility on every supported authoritative write path.
**Targeted architecture-impact revalidation:** PASS at candidate
`01963fc07194b6ef2359cec40a4af01cae9b712b`. The design was rechecked against
Phase 8 canonical docs tip `77f3e1a47a1e007492a794ea777d681a21a36d09`
(including P8-E promotion `d95b60d174cb0b17df09e2775b3cbd134c74b21f`),
architecture baseline `c285466c355103d3637ac165246591b72eb7bda0`, and both
alignment records. The profile remains daily-only; §1 and §3 now explicitly
account for P8-C Person positions and P8-E travel state. The independent
review confirmed the profile rejection boundary and preserved the existing
temporal/cardinality assumptions.

## 1. Contract and supported boundary

For one normal single-player world created by the validated `TesteSimulacao.InitializeSimulation` path from a non-null `SimulationConfigData`, capture is permitted only after `SimulationRuntime.AdvanceDay` has returned successfully and all synchronous work for that daily advance is complete. On restore, the same compatible build/runtime, current-host numeric profile, effective configuration, calendar, official definitions/content, and built-in provider composition must produce the same future authoritative results when given the same subsequent inputs.

This profile is named `UnityBootstrap-Daily-v1`. It covers bootstrap-composed Cities/markets/population aggregates, configured NPCs and their state, authored sites/routes, exploration/expedition/travel-party state and existing supported directives, initial Knowledge, and the official daily systems composed under the resolved effective configuration. It also captures any populated core authorities that belong to the supported `SimulationRuntime` composition, including Person/genealogy, political, force/conflict, and legacy spatial authorities. Empty stores are still represented by the profile's defined empty state. `PersonStore` and genealogy begin empty for configured NPCs; an `NpcRuntime` does not imply a `PersonId`.

The profile excludes arbitrary constructor-composed runtimes, injected providers, non-empty P8 physical geography/terrain/passages, non-empty `PersonSpatialPositionStore` state (`At` or `InTransit`) and P8-E Person travel state, P9/P10 generated worlds, pending external command queues and P11 actor-choice inputs, P19 modules/retrofit, P20 shared activities, `PlaceContentStore` and optional systems not composed by this bootstrap, and intraday save boundaries. It makes no P13 historical reconstruction/fork guarantee. No Sleep, Dreams, robbery/gang, ritual, War gameplay, or MegaEvento behavior is introduced by this design.

Profile admission rejects a presented runtime containing a pending external
WorldCommand or P11 actor-choice input, and load rejects any envelope that
declares one of those excluded causal inputs. The daily profile cannot silently
drop either input while claiming a complete continuation.

P18, P19 and P20 are conditional extensions to the state inventory only if a future explicitly supported profile contains their temporal, module, or shared-activity state. The base daily profile has no blanket dependency on those phases. A date is not a future intraday ordering contract: adding intraday capture requires the relevant promoted P18 identity, exact logical-time, due-work and same-time ordering/hydration capabilities first. Adding extension-owned state requires the applicable P19 lifecycle/state compatibility contracts. Adding shared activities requires the relevant P20 participant and lifecycle contracts. These additions do not justify freezing this proposal's internal time field to a day-only scalar.

The current profile excludes P9/P10 generated-world content and P20 activity
instances. It does include the P9-A authored-genesis manifest identity for the
selected `TesteSimulacao.InitializeSimulation` bootstrap: that path executes
`SimulationGenesisPipeline.ExecuteStages` and publishes a
`SimulationGenesisManifest` with the bootstrapped composition. The continuation
envelope must preserve or validate the manifest's stable profile-contract
identity and schema version, fingerprint, seed source/value, effective
configuration/calendar, ordered stage identities, dependencies, authored
definition identities, output-owner set, canonical provenance records, and
first simulated boundary. Store this genesis-origin identity/provenance with
the saved world's profile compatibility data and cover it together with the
owner sections under the envelope integrity digest. On load, validate it
against the compatible P9-A contract and the saved envelope's world/profile
identity; the manifest describes genesis inputs and outputs, not the later
evolved owner values at the save boundary. Hydration restores the captured
owner state and retained manifest evidence; it never reruns genesis or executes
the stage pipeline. This manifest identity/provenance is causal compatibility
data, not a requirement to carry or regenerate P9/P10 generated-world content.

If a later supported profile includes generated worlds, its causal inventory
must additionally retain stable generation stage/contributor identities,
compatible versions and provenance, selected authored inputs, dependency and
deterministic contribution/conflict order, purpose-scoped random context, and
generated outputs needed to continue; installation must not silently rerun
historical generation. If a later profile includes a P20 activity, preserve the
definition/version and stable instance identity separately from participants;
formation state, semantic participant identities (using `PersonId` for Person
participants without requiring materialized `NpcRuntime`), roles as of the save
boundary, agreements, reservations and intervals, scheduled start, shared
lifecycle/context, effects already applied, and pending causal work including
its relevant input, deterministic order, revision/sequence and random context.
Do not infer a one-Activity to one-actor relationship or duplicate one shared
instance into actor-owned copies. These are conditional inventory/revalidation
gates, not dependencies of `UnityBootstrap-Daily-v1`; relevant P9/P10/P20
capabilities are required only when their state enters a supported profile.
Before implementation, revalidate this envelope against promoted P9-A. If the
authored-genesis manifest/version enters the selected bootstrap composition,
include its compatibility identity and causal inputs or validate them without
rerunning genesis; do not omit them silently.

The alignment records also apply as constraints while their capabilities remain
out of profile: keep domain/application logic independent of Unity presentation
where practical and compose independent semantic contributions deterministically;
do not introduce speculative extension infrastructure. P19's public API,
module loader and module-owned durable state remain deferred. A future
multi-participant profile must preserve stable activity and participant identity,
roles/agreements/reservations and lifecycle independently of a single actor; the
current individual route-plan inventory does not impose such a cardinality rule.

## 2. Continuation envelope

The logical envelope is versioned and typed. Its exact byte encoding and storage medium remain implementation choices; no general serializer framework is proposed.

```text
ContinuationEnvelope
  envelopeKind = "simulation-continuation"
  envelopeSchema = { family, major, minor }
  profile = { id: "UnityBootstrap-Daily-v1", profileRevision }
  compatibility = {
    simulationBuildIdentity, runtimeAndUnityIdentity,
    numericExecutionProfile, officialContentManifest,
    effectiveConfigurationIdentity, calendarIdentity,
    bootstrapProviderSetIdentity
  }
  boundary = { absoluteDay, precision: "day", completedAdvanceSequence }
  world = { semantic entity identities and owner-authored state sections }
  causal = { allocator high-water marks, record sequence, random source state }
  commitments = { active plans, travel/expedition, directives and dispositions }
  knowledge = { holder identities, observations, provenance and freshness }
  integrity = { section identities, required-section set, canonical content digest }
```

All identity and content digests use canonical, length-delimited encoding with explicit null/empty distinctions and stable ordering. Digests detect corruption or accidental mismatch; they do not replace semantic validation or imply authenticity/security guarantees. The exact serialization format, compression, encryption, backup policy, and user-facing save catalog are outside this design.

`effectiveConfigurationIdentity` represents the resolved immutable `EffectiveSimulationConfiguration`, not a preset name or mutable Unity asset reference. `calendarIdentity` identifies the effective calendar rules/version and the calendar values needed to interpret `absoluteDay`. `officialContentManifest` identifies the exact behavior-bearing definitions and rules used by this world (including action/content definitions and enabled built-in policies/providers), using stable IDs plus compatible immutable revisions/content digests. `bootstrapProviderSetIdentity` records the selected built-in provider/system identities and versions for the resolved profile; service presence alone is insufficient. The build/runtime identity pins the simulation code and runtime actually used. `numericExecutionProfile` is `unity-float32-current-host:v1` where applicable and records the host/runtime identity necessary to reject a different numeric environment.

This initial compatibility policy is exact compatible build/runtime, content, effective configuration, calendar and provider set. Unknown schema major, missing required section, unavailable required definition/provider, incompatible content/config/calendar/build/numeric profile, malformed reference graph, or integrity failure rejects load before publication. No best-effort downgrade, migration, default substitution for missing required state, ID regeneration, or silent repair occurs. Compatible additive optional sections may be ignored only when the envelope schema explicitly declares them non-causal for this profile; otherwise a reader must reject an unknown required section. Cross-release migration policy remains deferred.

## 3. Causal state and owner map

The serialized records are value data, but each authoritative fact is emitted and reconstructed by its existing domain owner. This is not a second world store. The implementation inventory must be mechanically checked against the actual `SimulationRuntime` composition and the built-in `TesteSimulacao` composition on every schema revision.

| State group | Owner and required continuation facts | Rebuild / exclusions |
|---|---|---|
| Time and configuration | `SimulationTime` absolute day and completed boundary sequence; effective `SimulationCalendar`; immutable `EffectiveSimulationConfiguration`; exact profile/provider/content compatibility identities. | Recreate service objects and immutable values; do not serialize Unity object references. |
| Identity and sequences | Existing IDs from each domain; `RuntimeIdAllocator` per-kind next/high-water values; `SimulationRecordSequence`; any allocator actually used by a composed owner. Preserve distinct `PersonId`, `NpcRuntimeId`, typed domain IDs and content definition IDs. | Rebuild `RuntimeIdentityRegistry` and lookup indexes from owner facts. Never allocate replacement identities for loaded entities. The WorldCommand allocator/records are out of profile because queued external commands and command service are not composed by the selected bootstrap. |
| Deterministic randomness | Built-in `DeterministicRandomSource` seed and any versioned continuation state needed by that implementation; confirm all bootstrap random consumers resolve through this pinned built-in source. Capture causal demographic fallback inputs/state and random contexts if used. | Reject any profile/runtime whose providers are injected or whose random source is outside the pinned built-in contract. Do not assume seed sufficiency without a stream/draw-context inventory for every supported consumer. |
| Cities and economy | `CityRuntime`, `MarketRuntime`, `MarketItemRuntime`, population economy, inventories/accounts and current authoritative balances/stock; preserve any mutable price or commitment consumed later unless proven derived under the exact profile. | Resolve `ItemDefinitionId`, city IDs and account/custodian references against the exact official manifest. Rebuild price/query caches only when owner contracts define them as projections. |
| NPCs and daily condition | Each `NpcRuntime` stable runtime ID and mutable condition, status/life, current/destination location/city, action/behavior state, hidden-day counters, inventory and money account, and action definition identity. Preserve configured roster membership/order only where it is an explicit semantic tie-break; never use it as an identity substitute. | Resolve definitions and cross-links by stable IDs; recreate transient service references. Person existence is independent. Do not infer `PersonId` from an NPC or roster index. |
| Persons, population and genealogy | `PersonStore`, `GenealogyStore`, settlement population/lifecycle/residence owners, if populated through supported core authorities: `PersonId`, birth/death day, parentage, residence, aggregates, and exact materialization relationship. | Age/maturity are derived from birth date plus calendar/configuration. Preserve Person-backed versus NPC-only representation distinction; materialization must not alter aggregate population. |
| Legacy spatial and sites | Supported bootstrap `SpatialNetworkRuntime` route/location identity and route facts; `ExplorableSiteStore`, exploration state, visited/observed site facts, site links/anchors where present. | Rebuild navigational and registry indexes. Non-empty P8 Hex geometry, terrain, passage and physical-world composition is explicitly excluded. Do not confuse legacy `TravelPartyId` or `NpcRuntime` location links with future spatial identity contracts. |
| P8-C Person position and P8-E travel composition | `PersonSpatialPositionStore` facts keyed by stable `PersonId`, including either `At(StablePositionReference)` or `InTransit` with stable transit identities/progress. P8-E owns no duplicate position store; its travel transaction composes P8-C position with P8-D route-plan state. | The supported profile represents an empty position store explicitly. Any non-empty `At`/`InTransit` state or populated P8-E travel commitment is out of profile and rejects capture/restore rather than being omitted or partially reconstructed. Keep this separate from legacy NPC/TravelParty identities below; never infer position from residence or `NpcRuntime` location. |
| P8-D spatial Knowledge and route plans | `SpatialRouteKnowledgeStore`: holder `PersonId`; each observation's stable identity; typed subject kind and stable subject identity; typed value kind and value (presence/absence, route-option belief, or estimate plus unit); `ConfidencePermille`; `PrecisionIdentity`; `ObservedDay` and `ReceivedDay`; and complete provenance (`SourceKind`, `SourceIdentity`, `OriginIdentity`, optional transmitting `PersonId`). Preserve each actor's observation history and Knowledge revision used by stale checks. `PersonRoutePlanStore`: owner `PersonId`; `SelectionPolicy` identity/version, metric/unit, comparison direction, maximum estimate age, and known-availability requirement; selected candidate identity and ordered route legs/segments with each leg's belief and belief-evidence identity; the exact sorted `KnowledgeBasis` observation-identity set and actor Knowledge revision; decision identity; accepted day; per-Person `PlanRevision`; plan `Status`; complete plan history and store revision. Preserve one-active-plan-per-Person cardinality and same-day replacement semantics. | The daily profile may include spatial Knowledge when its referenced spatial facts are in profile. Route plans are profile-gated when their registered Hex endpoints are outside the supported profile. An empty plan store is represented explicitly; populated out-of-profile route plans or Knowledge referring to unsupported spatial facts reject capture/restore for this profile instead of being silently omitted or partially restored. Do not infer ActivityId, a permanent Activity-to-Actor relation, per-day travel cap, or automatic daily travel execution. |
| Travel and expedition commitments | `TravelSystem` owner facts for each traveling NPC/plan, current progress and destination/route; `TravelPartyStore` instances with `TravelPartyId`, member RuntimeIds, order, costs and lifecycle; `ExpeditionStore` instance/objective/progress, site and party bindings; merchant plan state when enabled. | Validate reciprocal references and active membership using current domain rules; do not recompute an active commitment from current decisions. Legacy `TravelPartyId` is a bounded current domain identity. It is not a P20 ActivityId and does not establish one-Activity/one-Actor cardinality for future activity types. |
| Directives and daily action | `ScheduledDirectiveStore`: stable directive ID, day, mode, operation, actor RuntimeId, selected action definition, state and processed/disposition day. Current NPC action and other future-affecting plan state. | Rebuild actor lookup map. Do not reapply already processed directives. This profile has no pending external command queue and no P11 actor-choice input. |
| Knowledge | NPC commercial/spatial/exploration Knowledge and other Knowledge actually composed/populated by the supported bootstrap; holder identity, observed facts, provenance/source, freshness/observed/received day and stale-check revisions. | Knowledge is not regenerated from current truth. Rebuild indexes, not observations. Excluded extension/module Knowledge remains unsupported until an extension profile exists. |
| Optional official daily domains | Economy/demography/mortality, merchant trade/sharing, crime/justice/social appraisal and guard/crime state when selected by effective configuration and actually composed. Preserve the owner stores' truth, current commitments, revisions, and any provider-derived causal facts. | Resolve the exact selected built-in providers/policies from compatibility manifest. Unsupported injected/custom providers cause profile rejection. Empty/uncomposed optional services are represented by profile composition, not fabricated stores. |
| Core political and military authorities | Persist any populated core authorities included by `SimulationRuntime`: institutions/offices/tenures, property/estate, claims/recognition/factions/support/Knowledge/decisions, armed forces/manpower/positions, persistent Conflict/War/Battle and accepted state/outcome. Preserve each store's owner and revision/fingerprint when future validation reads it. | Empty state is valid where the bootstrap creates an empty authority. No gameplay loop or unsupported provider is inferred. Restore relations only after their referenced owners exist. |
| Mutation health and revisions | Per-domain revisions, stale-plan inputs and causally read sequence values; mutation guard must be healthy at capture. | Recompute derived fingerprints after validation. A captured faulted guard is rejected; restore starts with a fresh guard bound only after staged validation. Do not reset a faulted runtime to healthy. |
| Events, history and diagnostics | Not part of the minimum causal envelope unless an owner fact is independently required above. The profile does not promise event/history/log retention as continuation state. | `WorldStateSnapshot`, canonical writer/digest, log text, Chronicle, decision and event records are not save DTOs or replay logs. Retain them only under separate owner/retention requirements; never use them as primary truth. |

Every stored reference is a typed stable identity and is checked for uniqueness, required target existence, compatible definition, valid relation cardinality and domain invariants. Exact invariant rules come from the domain owner. A detached/unknown target, duplicate identity, impossible reciprocal binding, invalid enum/state transition, or inconsistent allocator high-water mark rejects the entire envelope. References to dormant or non-materialized Persons remain valid by PersonId; they do not require an NpcRuntime.

## 4. Capture and staged hydration

### Capture

1. The bootstrap issues a capture eligibility token only after `AdvanceDay` returns normally. It is bound to the runtime instance and the completed absolute-day/boundary sequence.
2. Any supported authoritative mutation that completes after that `AdvanceDay` return—including a domain/UI command or bootstrap mutation—invalidates the token. The v1 boundary capture therefore occurs before later supported state changes; eligibility is reissued only after a subsequent successful `AdvanceDay`. Diagnostics-only reads do not invalidate it.
3. Capture is single-threaded on the simulation owner thread, between calls, with no daily system, domain transaction, command handler, or bootstrap mutation in progress. Re-entrant advance/capture and concurrent mutation are rejected. The current API does not expose a global transaction, so the implementation must add this lifecycle seam rather than assume snapshot atomicity.
4. A thrown/failed advance, a runtime faulted by the mutation guard, a stale or invalidated token, or an active operation cannot be captured. Do not attempt rollback or infer a successful boundary from the incremented day alone.
5. Each domain owner exports an immutable value section from its own authority. The capture coordinator verifies that all required sections belong to the same runtime and boundary and that no section changes during capture; on any mismatch, discard the candidate envelope.
6. Seal the envelope with deterministic section ordering and integrity digest only after all owner sections pass identity/reference checks. A failed capture leaves the running world untouched and emits no partially usable save.

### Hydration

1. Parse into inert envelope values. Validate envelope schema, required sections, digest, profile ID, build/runtime/numeric identity, official content manifest, exact effective configuration, calendar, and built-in provider set before allocating live domain objects.
2. Resolve immutable definitions and providers from the compatible official catalog. Build a fresh, private staging composition; never hydrate into or clear/mutate the live runtime.
3. Stage logical time/calendar and all semantic identities/ID high-water marks without allocating persisted entity IDs. Validate global uniqueness and allocator monotonicity.
4. Restore factual roots first: cities/markets/accounts, population aggregates, Persons and NPCs; then content/site/spatial legacy facts. Instantiate owner state through explicit domain hydration factories or owner-approved constructors. No general reflection serializer and no bypass of semantic/domain validation.
5. Restore relations and Knowledge by stable references: residence/genealogy; political/institutional relations; optional official domains; then force/conflict relations in their actual constructor dependency order. Restore per-owner revisions/stale inputs required by domain checks.
6. Restore active commitments and pending official work: directives, travel plans/parties, expeditions and merchant plans. Validate reciprocal member/actor/party/site references, progress/cost consistency and disposition state. Do not start, cancel, replay or replan work during hydration.
7. Recreate the built-in deterministic random source and supported causal state; compose the exact official system/provider set; rebuild runtime registries, indexes, caches and read-only projections from authoritative owner values. Bind one new `AuthoritativeMutationGuard` to the fully staged object graph only after all stores report bind compatibility.
8. Run domain-owner validation plus global identity/reference checks, spatial checks applicable to this profile, configuration/provider composition checks, and deterministic canonical diagnostic comparison against the captured semantic projection where that projection covers the relevant section. Diagnostics are evidence, never the input authority.
9. Publish the staged runtime as one reference swap through the owning bootstrap/session boundary only if every check succeeds. Until that swap, the previous runtime remains authoritative. After publication, dispose/detach the previous runtime through its owner lifecycle; no observer receives the staging graph.

Hydration factories must preserve domain mutation authority after publication. They may initialize state before guard binding, but cannot expose a second writable graph. Any owner without a complete, reviewed hydration/export contract blocks claiming that profile's full continuation support; it is not omitted silently.

## 5. Failure and compatibility behavior

Capture failure produces no envelope and leaves the runtime unchanged. Load failure returns a structured incompatibility/corruption/validation result identifying the failed section and stable semantic ID where appropriate, without publishing staged state or mutating the current runtime. Do not substitute defaults for missing required content, drop unknown causal state, regenerate entities, recompute commitments, or silently clear invalid references.

The initial profile is intentionally same-build/current-host/exact-content and exact effective-configuration/calendar compatibility. A different numeric profile, Unity/runtime identity, provider set or behavior-bearing content revision is incompatible even if display names match. No cross-host float equivalence, arbitrary injected providers, cross-release migration, P19 compatibility, or future P18/P20 schema support is implied. A future compatible schema migration must be explicitly designed and versioned; this proposal does not supply one.

## 6. Continuation parity evidence

The implementation review should establish evidence in layers:

1. **Owner round-trip:** for each included owner, capture→hydrate preserves stable IDs, owner truth, Knowledge, active commitment, relevant revisions and allocator constraints. Include empty and populated instances and references to non-materialized Persons where those authorities are supported.
2. **Boundary admission:** successful completed day can capture; before/inside advance, re-entrant/concurrent capture, exception/faulted guard, unsupported composition and unsupported provider are rejected.
3. **Compatibility rejection:** mutate one envelope dimension at a time (schema major, build/runtime, numeric profile, content/provider manifest, effective configuration, calendar, missing required section, digest/reference corruption); prove rejection occurs before publication and the previous runtime is unchanged.
4. **Identity/cardinality checks:** duplicate IDs, dangling and wrong-kind references, out-of-range allocator marks, inconsistent NPC/Person/materialization links, party/member bindings, and relation-specific multiplicity violations reject. Confirm NPC-only bootstrap actors do not acquire PersonIds. Confirm legacy `TravelPartyId` preserves its current members without making a general ActivityId↔single-actor assumption. A future shared-activity profile must separately round-trip one stable instance with multiple Person participants and its as-of-boundary roster/reservations; that test belongs behind relevant P20 capability, not this profile.
5. **Uninterrupted versus restored continuation:** from identical captured daily boundaries, run an original and restored world with the same next-day inputs. Compare every included authoritative owner section after each day, including Knowledge, plans/commitments, identities and subsequent allocator/RNG-dependent outcomes. Diagnostic snapshots/digests may be the comparator only after their coverage is audited against the causal inventory; they are not the persistence format.
6. **Required regression gates:** run relevant domain suites for every implemented owner, affected Phase 5–8 regressions, ALL EditMode and the complete official Smoke suite as specified by current execution instructions before promotion. Long-run validation is required if the implementation changes the daily loop or long-horizon behavior. `git diff --check` is also a gate.

Phase 13 parity is separate: it must reconstruct every actually simulated boundary, include causal input/history and generation/module mutations at their original boundaries, support compatible independent forks, and include intraday boundaries when those capabilities exist. Passing daily save/load parity does not close or narrow P13.

## 7. Sequencing and unresolved gates

The technical choices above resolve the bounded design questions using reviewed defaults. The remaining gates are process/capability gates, not unresolved product questions:

- The entry proposal and this technical design require independent technical review and acceptance under the execution model. That review may reject or request changes; this proposal cannot self-approve.
- No P12 checkpoint IDs or implementation authorization exist. A reviewed, accepted technical design still needs the Phase's checkpoint scope and status record established through the normal architecture/planning workflow before code implementation.
- Complete implementation remains capability-gated by owner-export/hydration support for every domain included in `UnityBootstrap-Daily-v1`; each owner must be inventoried against the refreshed canonical composition at implementation time.
- P18/P19/P20 support remains conditional as specified in §1; P9/P10 generated state and external command/P11 input queues remain excluded from this initial profile.

No genuinely unresolved product or canonical semantic decision is identified within the reviewed profile. Any request to expand compatibility guarantees or include an excluded state source requires a new scoped decision/design rather than an implicit change to this proposal.

## 8. Sources rechecked

### Promotion impact revalidation — 2026-09-26

This design remains limited to `UnityBootstrap-Daily-v1`; this refresh creates
no checkpoint IDs and grants no implementation authorization. The post-promotion
dependency check uses P8 canonical State `470667d`, P9-A authored-bootstrap
promotion `988b6f5`, P11 Actor Choice State promotion `0803670`, P18-A State
promotion `0b52898`, and architecture baseline
`c285466c355103d3637ac165246591b72eb7bda0`, including the current
intraday/extensibility and multi-participant alignment records.

- **P9-A manifest/version:** the selected bootstrap executes P9-A and publishes
  a `SimulationGenesisManifest`. Preserve or validate its stable profile/schema/
  contract identity, ordered stage identities/versions/dependencies, fingerprint,
  selected-input and output provenance, configuration/calendar, and seed source/
  value against the saved world. Hydration restores saved owner state and never
  reruns genesis. This compatibility evidence does not bring generated P9/P10
  content into the daily profile.
- **P11 Actor Choice ingress:** pending WorldCommand and actor-choice inputs
  remain outside this profile. Capture admission and load validation reject
  their presence; the promoted ingress does not authorize dropping them from a
  continuation envelope. Reconfirm this rejection against current command and
  actor-choice authorities before implementation.
- **P18-A:** upstream-irrelevant to this daily-only profile. No intraday time,
  activity or due-work state is claimed. Any intraday profile requires the
  relevant promoted P18 state, ordering and hydration capabilities first.
- **P20:** conditional only if a future supported profile includes shared
  activities; no blanket dependency is introduced.

The refreshed alignment records preserve explicit temporal and participant
identity boundaries. This review changes no supported scope, serialization
choice, or compatibility promise.

- `docs/SIMULATION_ARCHITECTURE.md` §§8, 11–13, 91–93.
- `docs/ROADMAP.md`, `docs/EXECUTION_MODEL.md`, `docs/phases/PHASE12_BRIEF.md`, and `docs/phases/PHASE13_BRIEF.md`.
- Refreshed `docs/design/PHASE12_ENTRY_ARCHITECTURE.md`, current `docs/PHASE8_STATE.md`, and formal `docs/PHASE5_STATE.md`, `docs/PHASE6_STATE.md`, and `docs/PHASE7_STATE.md` records.
- `docs/architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md` and `docs/architecture/MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md`.
- `Assets/_Project/Scripts/TesteSimulacao.cs` (`InitializeSimulation`, `RebuildSystems`), `SimulationRuntime.cs` (`AdvanceDay` and composition), `SimulationModuleSet.cs`, and owner implementations for `SimulationTime`, `SimulationCalendar`, `EffectiveSimulationConfiguration`, runtime identities, random source, Person/population, city/market, travel parties, expeditions, directives, Knowledge, spatial and political/military stores.
