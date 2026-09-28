# Phase 12 — Technical Design Proposal: Daily Continuation

**Status:** Technical design for the user-accepted scope of checkpoint `P12-A — UnityBootstrap Daily Continuation v1`, based on the bounded `UnityBootstrap-Daily-v1` profile. The profile covers the SampleScene-selected `Simulation-GeneralTest.asset` through the validated `TesteSimulacao.InitializeSimulation` bootstrap, exact compatible build/runtime and current-host numeric profile, and completed-day capture boundaries. The selected asset enables P9-B authored geography. P9 canonical closure is `82396ae7ffaf407fda278928da456b06dc5394d`; P9-B code integration is `d9a62d7c6bea242653c2d68cc0a70911bb5ed1bf` (implementation `00395ef80cfa2364d34ed2170e0735d3a4b1513d`), with promotion State/status record `14a2e8ee01e49f1ffdcecf8fd64d274c728ead44`. P9-A-only configurations are incompatible with this profile and require separate profile identity/admission if retained. Scope acceptance does not authorize implementation or establish Phase State delivery, owner export/hydration, or save/load. Independent full-design/current-base review passed at `9fde12a`; the later current-base refresh review passed at `d1a8414` for the refreshed P9/P10/P20/alignment references and their consistency with the bounded profile. That scoped refresh review is not a new full-design review, and neither review demonstrates owner export/hydration coverage.
**Historical independent technical design review:** PASS at content commit
`8577ba589a0f9b40738fcf8738eee9589563d7b8`. The later lifecycle seam must
invalidate capture eligibility on every supported authoritative write path.
**Historical targeted architecture-impact revalidation:** PASS at candidate
`01963fc07194b6ef2359cec40a4af01cae9b712b`. The design was rechecked against
Phase 8 canonical docs tip `77f3e1a47a1e007492a794ea777d681a21a36d09`
(including P8-E promotion `d95b60d174cb0b17df09e2775b3cbd134c74b21f`),
architecture baseline `c285466c355103d3637ac165246591b72eb7bda0`, and both
alignment records. That review predates later P9 manifest and current-base
clarifications and does not review this exact revision. Current references for
this refresh are P8 canonical State `470667d37863384edadb3d93ef64d8004aff46a3`,
P9 canonical closure `82396ae7ffaf407fda278928da456b06dc5394d`, P9-A code promotion `43f08b3dfbf042380c2f8a8b037bbf3ebd309ccb` / P9-B
promotion-record State `14a2e8ee01e49f1ffdcecf8fd64d274c728ead44`, P9-B canonical integration
`d9a62d7c6bea242653c2d68cc0a70911bb5ed1bf` (implementation tip
`00395ef80cfa2364d34ed2170e0735d3a4b1513d`), P11 code promotion
`0803670cfa2c39163b54ff46a21daa06df5a16f6` / closure State
`308e24d0744112e8f2b741521b8b3e4acb51ebbf`, and architecture baseline
`c285466c355103d3637ac165246591b72eb7bda0`. P8's `77f3e1a`→`470667d`
advance is State-only. Both alignment records remain current.
The P9-B promotion record is canonical in State/status tip `14a2e8e`; use that
record alongside P9-B code integration `d9a62d7` and current P9 closure
`82396ae` when checking capability and profile provenance. P10 canonical
State/Brief tip is
`252ad6b9a507f1c001c05a1e19c2546ebd0707a2` (P10-A code
`9501bf076d506fb64d6ee3e6d178574fff36e153`, State record
`9e79b58397dc9a89ddcc562be139b79987cb55b9`). P10-A's current reviewed profile is one Ruin at an existing
P8 Location with finite LocalTopology. Its runtime is promoted at code
`9501bf076d506fb64d6ee3e6d178574fff36e153`, with P10 State `9e79b58397dc9a89ddcc562be139b79987cb55b9`
and current canonical State/Brief tip
`252ad6b9a507f1c001c05a1e19c2546ebd0707a2`, but
its Ruin/LocalTopology output is outside `UnityBootstrap-Daily-v1` composition
and remains excluded from this profile. P14-A code promotion is
`c44904bb4b0a066eced1d7e8a773b7dc1eea76c0` with historical State
`f8a61fe9634ba9ab56ee31d50b57b45fef292a6f`; current P14 canonical State/Brief
`4caecbb` is a docs-only update. The selected GeneralTest City assets do not
configure P14-A's material-flow source. P20-A is a reviewed proposed
synthetic two-Person proof at checkpoint proposal
`2a03edadcb411c7ca7aa2d0c67ee8d36f972779e`, not an accepted
checkpoint or implementation capability; its exact-two fixture does not narrow
the architecture's one-or-more participant cardinality.

## 1. Contract and supported boundary

For one normal single-player world created by the validated `TesteSimulacao.InitializeSimulation` path from a non-null `SimulationConfigData`, capture is permitted only after `SimulationRuntime.AdvanceDay` has returned successfully and all synchronous work for that daily advance is complete. On restore, the same compatible build/runtime, current-host numeric profile, effective configuration, calendar, official definitions/content, and built-in provider composition must produce the same future authoritative results when given the same subsequent inputs.

This profile is named `UnityBootstrap-Daily-v1`. It covers the SampleScene-selected `Simulation-GeneralTest.asset` and its bootstrap-composed Cities/markets/population aggregates, configured NPCs and their state, authored sites/routes, exploration/expedition/travel-party state and existing supported directives, initial Knowledge, and official daily systems composed under the resolved effective configuration. It also captures supported core authorities in the selected `SimulationRuntime` composition, including Person/genealogy, political, force/conflict, legacy spatial authorities, and the selected P9-B P8-A geography facts. The P9-B profile composes exactly one authored Hex, one Location anchored to that Hex, and one scale context before day one. `PersonStore` and genealogy begin empty for configured NPCs; an `NpcRuntime` does not imply a `PersonId`.

The profile excludes arbitrary constructor-composed runtimes, injected providers, any P8-owned facts beyond the selected P8-A Hex/Location/scale set (including P8-B passage, P8-C City/Site anchors and Person positions, P8-D spatial Knowledge/route plans, and P8-E travel), P9/P10 generated worlds, pending or mid-dispatch P11 actor-choice inputs, external WorldCommand service/queue composition, P19 modules/retrofit, P20 shared activities, `PlaceContentStore` and optional systems not composed by this bootstrap, and intraday save boundaries. P9-A-only configs and legacy manifests are not admitted by this v1 profile. A separately versioned P9-A-only profile may retain an explicit admission check requiring authored geography disabled and the P9-A-only contract identity. This profile makes no P13 historical reconstruction/fork guarantee. No Sleep, Dreams, robbery/gang, ritual, War gameplay, or MegaEvento behavior is introduced by this design.

The selected bootstrap does not compose an external WorldCommand service or
queue; adding that composition or queued external commands is unsupported and
must reject profile admission rather than omit them. When the promoted P11
`ActorChoiceStore` is composed, preserve its complete input/disposition history,
duplicate-command identity history, and next sequence for terminal
idempotency. `Rejected`, `AttemptReturned`, and `AttemptThrew` inputs are
terminal and may be captured; `Pending` (including deferred choices) and
`ConsumedAwaitingTerminalAttempt` are in-flight and reject capture/load. The
profile cannot silently drop causal input state while claiming a complete
continuation.

P18, P19 and P20 are conditional extensions to the state inventory only if a future explicitly supported profile contains their temporal, module, or shared-activity state. The base daily profile has no blanket dependency on those phases. P18-A/B/C and the P18-D receipt/serialized-runtime prerequisites are promoted in code; the current P18 canonical code tip is `9e790c5`, while its recorded canonical State still needs a docs-only reconciliation for those prerequisites. The P18-D consumer remains undelivered, and P18-A is not integrated into the legacy `SimulationRuntime.AdvanceDay` path. For a future profile that explicitly composes the accepted P18-A continuation extension contract `2175bf2`, preserve its frozen activation manifest, distinct `ContinuationId` and step identities, descriptor order/version, committed receipts and next unresolved position, and completion/barrier state alongside boundary identity `(worldId, profileId, absoluteDay)`, tick quantum/version, effective `MaxDispatchesPerInstant` or equivalent dispatch limit, work identity `(ownerId, workId, revision, occurrence)`, exact logical instant, causal wave and same-instant order, pending boundary/work, persisted occurrence/sequence, and owner idempotency/effect state. Preserve sealed external inputs with target `LogicalTick`, accepted sequence/order, and boundary state. Include P18-B activity lifecycle/revision/receipts and participant commitments/availability plus P18-C PersonId-keyed decision/attempt state when used by its consumer. The P18-A implementation candidate/acceptance record `9de70ae` is not evidence of delivered or promoted capability. A date is not an intraday ordering contract. Extension-owned state requires applicable P19 lifecycle/state compatibility contracts. P20 remains conditional: a profile that includes it must preserve stable activity identity separately from definition and one-or-more participants; the reviewed two-Person fixture does not define universal cardinality or role policy. These additions do not justify freezing this proposal's internal time field to a day-only scalar.

The current profile excludes P9/P10 generated-world content and P20 activity
instances. It includes the selected P9-B authored-geography profile identity for
the `TesteSimulacao.InitializeSimulation` bootstrap, distinct from P9-A's
historical `unity-authored-bootstrap/genesis-v1` identity. The promoted P9-B
profile uses
`unity-authored-bootstrap/authored-geography-v1` and stage
`p9.genesis.authored-geography/v1`. Its manifest/fingerprint must be versioned
separately so the additional P8 facts cannot be represented as P9-A-only output.
The continuation envelope preserves the selected P9-B contract/schema identity
and fingerprint, inherited P9-A stage identities plus P9-B stage/version/
dependencies, seed source/value, effective configuration/calendar, selected
authored definition IDs, output-owner set, canonical provenance and first
simulated boundary. For the
current selected asset, that provenance includes Hex `hex/sample-origin` at
axial `(0,0)`, terrain `terrain/sample-plains` with revision
`sample-world-v1`, Location `location/sample-origin` anchored to that Hex, and
scale convention `world-scale/Simulation-GeneralTest/v1`, source
`profile/Simulation-GeneralTest` version `1`, distance `1 km` per neighbor
step. Cover the genesis evidence and owner sections together under the envelope
integrity digest. On load, validate evidence against the selected P9-B contract,
its inherited P9-A stage lineage, and saved world/profile identity; manifest evidence describes genesis
inputs/outputs, not evolved owner values. Hydration restores captured owner
state and retained manifest evidence, never reruns genesis. This does not bring
P9/P10 generated-world content into scope.

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
The selected bootstrap publishes the P9-B profile manifest through the existing
P9 genesis handoff. Independent review of the current integrated docs package
must verify the P9-A stage lineage/P9-B profile mapping; implementation must
validate retained profile-manifest evidence without rerunning genesis or
replacing evolved owner state.

The alignment records also apply as constraints while their capabilities remain
out of profile: keep domain/application logic independent of Unity presentation
where practical and compose independent semantic contributions deterministically;
do not introduce speculative extension infrastructure. P19's public API,
module loader and module-owned durable state remain deferred. A future
multi-participant profile must preserve stable `ActivityInstanceId` independently
of activity definition, PersonId/NpcRuntimeId and participant identity, plus
formation/decision state, roles where applicable, agreements/reservations,
lifecycle/context, effects and pending causal work. The architecture's one-or-more
participant cardinality applies; P20's exactly-two fixture is not a global rule.
The current individual route-plan inventory imposes no activity cardinality rule.

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
| P8-owned geography and spatial authorities | The selected P9-B profile has exactly one populated P8-A Hex, one P8-A Location anchored to that Hex, and one `SpatialWorldScaleContext`: `hex/sample-origin`, coordinate `(0,0)` under `axial-hex-v1`, terrain `terrain/sample-plains`/`sample-world-v1`, `location/sample-origin` with its `AnchorHexId`, and the authored scale convention/source/version/distance/unit recorded in the manifest. | Export and hydrate complete `SpatialAuthorityStore` facts through P8-A ownership, including stable IDs, terrain reference and revision, anchor relation, coordinate convention, scale context and owner invariants. Preserve exact one-Hex/one-Location cardinality for this selected profile; reject missing/duplicate/malformed/extra facts. P8-B passage/route, P8-C City/Site anchors and `PersonSpatialPositionStore`, P8-D `SpatialRouteKnowledgeStore` and `PersonRoutePlanStore`, and P8-E travel remain explicitly empty and reject populated state. Never infer P8 identity from legacy `SpatialNetworkRuntime`. P8-A is included; P8-E is not a blanket dependency. |
| Travel and expedition commitments | `TravelSystem` owner facts for each traveling NPC/plan, current progress and destination/route; `TravelPartyStore` instances with `TravelPartyId`, member RuntimeIds, order, costs and lifecycle; `ExpeditionStore` instance/objective/progress, site and party bindings; merchant plan state when enabled. | Validate reciprocal references and active membership using current domain rules; do not recompute an active commitment from current decisions. Legacy `TravelPartyId` is a bounded current domain identity. It is not a P20 ActivityId and does not establish one-Activity/one-Actor cardinality for future activity types. |
| Directives, daily action, and actor-choice ingress | `ScheduledDirectiveStore`: stable directive ID, day, mode, operation, actor RuntimeId, selected action definition, state and processed/disposition day. Current NPC action and other future-affecting plan state. Where composed, full P11 `ActorChoiceStore` inputs/dispositions, next sequence, and duplicate WorldCommand-ID history. | Rebuild actor lookup map. Do not reapply processed directives. Preserve terminal actor-choice records (`Rejected`, `AttemptReturned`, `AttemptThrew`) for idempotency; reject `Pending` or `ConsumedAwaitingTerminalAttempt` at capture/load. The bootstrap does not compose an external WorldCommand service/queue; reject a profile that adds one. |
| Knowledge | NPC commercial/spatial/exploration Knowledge and other Knowledge actually composed/populated by the supported bootstrap; holder identity, observed facts, provenance/source, freshness/observed/received day and stale-check revisions. | Knowledge is not regenerated from current truth. Rebuild indexes, not observations. Excluded extension/module Knowledge remains unsupported until an extension profile exists. |
| Optional official daily domains | Economy/demography/mortality, merchant trade/sharing, crime/justice/social appraisal and guard/crime state when selected by effective configuration and actually composed. Preserve the owner stores' truth, current commitments, revisions, and any provider-derived causal facts. P14-A material flow is included only if its authored settlement/source is selected and composed; the selected `Simulation-GeneralTest.asset` and its referenced City assets currently contain no P14-A source configuration. | Resolve exact selected built-in providers/policies from compatibility manifest. Unsupported injected/custom providers cause profile rejection. Empty/uncomposed optional services are represented by profile composition, not fabricated stores. If a later admitted config activates P14-A, revise the profile to include its settlement/source/store/item identities, effective policy/config/calendar, daily source application and consumption state/order, stock/revisions and reconstruction facts; otherwise reject it under this profile. |
| Core political and military authorities | Persist any populated core authorities included by `SimulationRuntime`: institutions/offices/tenures, property/estate, claims/recognition/factions/support/Knowledge/decisions, armed forces/manpower/positions, persistent Conflict/War/Battle and accepted state/outcome. Preserve each store's owner and revision/fingerprint when future validation reads it. | Empty state is valid where the bootstrap creates an empty authority. No gameplay loop or unsupported provider is inferred. Restore relations only after their referenced owners exist. |
| Mutation health and revisions | Per-domain revisions, stale-plan inputs and causally read sequence values; mutation guard must be healthy at capture. | Recompute derived fingerprints after validation. A captured faulted guard is rejected; restore starts with a fresh guard bound only after staged validation. Do not reset a faulted runtime to healthy. |
| Events, history and diagnostics | Not part of the minimum causal envelope unless an owner fact is independently required above. The profile does not promise event/history/log retention as continuation state. | `WorldStateSnapshot`, canonical writer/digest, log text, Chronicle, decision and event records are not save DTOs or replay logs. Retain them only under separate owner/retention requirements; never use them as primary truth. |

Every stored reference is a typed stable identity and is checked for uniqueness, required target existence, compatible definition, valid relation cardinality and domain invariants. Exact invariant rules come from the domain owner. A detached/unknown target, duplicate identity, impossible reciprocal binding, invalid enum/state transition, or inconsistent allocator high-water mark rejects the entire envelope. References to dormant or non-materialized Persons remain valid by PersonId; they do not require an NpcRuntime.

## 4. Capture and staged hydration

### Capture

1. The bootstrap issues a capture eligibility token only after `AdvanceDay` returns normally. It is bound to the runtime instance and the completed absolute-day/boundary sequence. Admission verifies the selected `Simulation-GeneralTest.asset`, `useAuthoredGeographyProfile`, and P9-B contract identity/schema; a P9-A-only manifest or a different geography inventory is rejected for this profile.
2. Any supported authoritative mutation that completes after that `AdvanceDay` return—including a domain/UI command or bootstrap mutation—invalidates the token. The v1 boundary capture therefore occurs before later supported state changes; eligibility is reissued only after a subsequent successful `AdvanceDay`. Diagnostics-only reads do not invalidate it.
3. Capture is single-threaded on the simulation owner thread, between calls, with no daily system, domain transaction, command handler, or bootstrap mutation in progress. Re-entrant advance/capture and concurrent mutation are rejected. The current API does not expose a global transaction, so the implementation must add this lifecycle seam rather than assume snapshot atomicity.
4. A thrown/failed advance, a runtime faulted by the mutation guard, a stale or invalidated token, or an active operation cannot be captured. Do not attempt rollback or infer a successful boundary from the incremented day alone.
5. Each domain owner exports an immutable value section from its own authority. The capture coordinator verifies that all required sections belong to the same runtime and boundary and that no section changes during capture; on any mismatch, discard the candidate envelope.
6. Seal the envelope with deterministic section ordering and integrity digest only after all owner sections pass identity/reference checks. A failed capture leaves the running world untouched and emits no partially usable save.

### Hydration

1. Parse into inert envelope values. Validate envelope schema, required sections, digest, profile ID, build/runtime/numeric identity, official content manifest, exact effective configuration, calendar, and built-in provider set before allocating live domain objects.
2. Resolve immutable definitions and providers from the compatible official catalog. Build a fresh, private staging composition; never hydrate into or clear/mutate the live runtime.
3. Stage logical time/calendar and all semantic identities/ID high-water marks without allocating persisted entity IDs. Validate global uniqueness and allocator monotonicity.
4. Restore factual roots first: cities/markets/accounts, population aggregates, Persons and NPCs; then the complete P8-A geography authority, content/site/spatial legacy facts. Validate the selected Hex, terrain/revision provenance, anchored Location and scale through the P8 owner before restoring any dependent spatial relation. Instantiate owner state through explicit domain hydration factories or owner-approved constructors. No general reflection serializer and no bypass of semantic/domain validation.
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

- The full design/current-base review passed at `9fde12a`. The scoped exact-base refresh review passed at `d1a8414`, covering the P9/P10/P20/alignment-reference refresh and confirming it preserves this bounded profile; it did not repeat a full-design review. P12-A scope/checkpoint acceptance is recorded at `4a1d364` against reviewed contract `5264f0c`; neither record authorizes implementation.
- `P12-A` is the accepted bounded daily continuation checkpoint scope, but implementation remains `WAIT_DEPENDENCY` and unauthorized until every included owner has complete exact export and staged hydration, the live profile inventory is validated, and separate implementation authorization is recorded.
- Each owner must be inventoried against the refreshed canonical composition at implementation time; complete owner export/hydration and the live profile inventory have not yet been established.
- P18/P19/P20 support remains conditional as specified in §1; P9/P10 generated state and external command/P11 input queues remain excluded from this initial profile.
- P8 dependencies are state-specific, not a blanket A–E execution edge. The selected profile requires complete P8-A export/hydration for its one authored Hex, anchored Location and scale. P8-B/C/D/E sections remain explicitly empty and populated state rejects; legacy routes do not substitute for P8 identity. P8-E is not required merely for daily continuation.
- P9-A's historical authored-genesis contract and the selected P9-B geography profile contract are distinct required compatibility evidence. The P9-B implementation at `00395ef80cfa2364d34ed2170e0735d3a4b1513d` is included on current `codex/phase9/canonical` at `d9a62d7c6bea242653c2d68cc0a70911bb5ed1bf`. P12 implementation remains blocked on complete included-owner export/hydration, the live profile inventory, and separate implementation authorization; accepted scope and contract review do not satisfy these gates. The bootstrap has no external WorldCommand service/queue composition. Where P11 ActorChoiceStore is composed, preserve complete terminal history/idempotency state and reject `Pending`/`ConsumedAwaitingTerminalAttempt` inputs.

No genuinely unresolved product or canonical semantic decision is identified within the reviewed profile. Any request to expand compatibility guarantees or include an excluded state source requires a new scoped decision/design rather than an implicit change to this proposal.

## 8. Sources rechecked

### Promotion impact revalidation — 2026-09-27

This design remains limited to `UnityBootstrap-Daily-v1`. P12-A scope is
accepted; this refresh grants no implementation authorization and records no
review verdict. The current-base dependency check uses P8
canonical State `470667d`, P9 canonical closure `82396ae`, P9-A code promotion
`43f08b3` and P9-B promotion-record State `14a2e8e`, P9-B code integration
`d9a62d7c6bea242653c2d68cc0a70911bb5ed1bf`
(implementation tip `00395ef80cfa2364d34ed2170e0735d3a4b1513d`), P11 code promotion `0803670` and
closure State `308e24d`, current P18 canonical code tip
`9e790c59e14ca7f7ed195c0e6267e10f3cd039d7` (P18-A/B/C and the P18-D
sale-receipt/serialized-runtime prerequisites promoted; P18 State text at the
time of this design refresh still carries stale pending wording), accepted
P18-A continuation extension contract
`2175bf2` (the implementation candidate/acceptance record `9de70ae` is not
delivered/promoted capability), P14 historical promotion State `f8a61fe` and
current docs-only canonical State/Brief `4caecbb`,
P20 Entry Architecture `2f9c93b`, Technical Design `6a0d164`, and proposed
P20-A proposed checkpoint `2a03eda` (the user accepted its bounded scope on
2026-09-27; this accepts scope only, delivers no P20 capability, and implies
no P12 dependency),
P10-A's approved bounded P8-C LocationId-neutral seam with design/checkpoint
review PASS at `345dcbc` and `READY_FOR_IMPLEMENTATION` status; the P10 runtime
is promoted at code `9501bf0` / State `9e79b58` / current canonical State/Brief
`252ad6b`, but
its Ruin/LocalTopology output is outside this profile's composition/scope, and
architecture baseline `c285466`, including both current alignment records.

- **P9-A/P9-B manifest and version:** the selected bootstrap executes P9-A and
  the P9-B authored-geography stage/profile. Preserve the historical P9-A
  `unity-authored-bootstrap/genesis-v1` identity separately from P9-B's
  `unity-authored-bootstrap/authored-geography-v1` contract/schema and
  `p9.genesis.authored-geography/v1` stage identity. Validate ordered stage
  identities/versions/dependencies, fingerprint, seed source/value, selected
  authored inputs/outputs, P8-A output facts, configuration/calendar and
  provenance against the saved world. Hydration restores saved owner state and
  never reruns genesis. Generated P9/P10 content remains excluded. The P9-B
  implementation is present on current P9 canonical at `d9a62d7`; this P12
  record does not certify P12 export/hydration or checkpoint gates.
- **P11 Actor Choice ingress:** the selected bootstrap has no external
  WorldCommand service/queue composition. When `ActorChoiceStore` is composed,
  preserve its full terminal history and idempotency sequence; reject pending
  (including deferred) or `ConsumedAwaitingTerminalAttempt` records. P11 is not
  a gameplay dependency; its current authority is queried for this boundary.
- **P8 spatial capability:** require exactly the selected P9-B P8-A geography
  authority (one Hex, one anchored Location, one world-scale context) and export/
  hydrate every authored fact and provenance field. Preserve P8-B passage, P8-C
  City/Site anchor and Person-position, P8-D route Knowledge/plan and P8-E travel
  as empty sections; reject populated state. Do not infer stable Location
  identity or anchors from legacy routes. P8-E is not a blanket dependency.
- **P18:** the selected daily profile does not compose P18 temporal state and
  does not claim intraday continuation. If a future supported profile composes
  P18, inventory `(worldId, profileId, absoluteDay)` boundary identity,
  tick quantum/version, effective `MaxDispatchesPerInstant` or equivalent
  dispatch limit, `(ownerId, workId, revision, occurrence)` work identity, exact
  logical time, causal wave and same-instant order, pending boundary/work,
  occurrence/sequence and owner idempotency/effect state. Preserve sealed
  external inputs with target `LogicalTick`, accepted sequence/order, and
  boundary state. Preserve ActivityInstanceId separately from
  PersonId/NpcRuntimeId and participant identity, plus P18-B lifecycle/revision/
  receipts, commitments/availability and P18-C PersonId-keyed decision/attempt
  state when the consumer composes them. P18-A/B/C are promoted; P18-D is not
  implemented and the daily runtime has not migrated to that timeline.
- **P20:** shared activities remain outside this profile. If later included,
  preserve stable ActivityInstanceId separately from definition and participant
  identities, one-or-more participant cardinality, partial formation/decisions,
  roles where applicable, agreements, reservation intent and committed
  intervals, scheduled start, lifecycle/context, participant effects and pending
  causal work/order/idempotency. P20's reviewed exactly-two fixture does not
  establish universal cardinality; its reviewed design is not implementation
  authorization or a blanket dependency.

The refreshed alignment records preserve explicit temporal and participant
identity boundaries. The P8 `77f3e1a`→`470667d` advance changes only Phase 8
State wording; P9's current promotion-record State/status tip is `14a2e8e`, for
code at `d9a62d7`. This refresh records P9-B's canonical integration and
required P8-A state, records P10-A runtime promotion while keeping its
Ruin/LocalTopology output outside the selected profile's composition/scope,
and leaves P18 intraday, P19 modules and P20 shared activities conditional and
outside this profile. Independent review of the
current integrated package must confirm these current-base mappings and the
remaining capability/acceptance gates.

- `docs/SIMULATION_ARCHITECTURE.md` §§8, 11–13, 91–93.
- `docs/ROADMAP.md`, `docs/EXECUTION_MODEL.md`, `docs/phases/PHASE12_BRIEF.md`, and `docs/phases/PHASE13_BRIEF.md`.
- Refreshed `docs/design/PHASE12_ENTRY_ARCHITECTURE.md`, current `docs/PHASE8_STATE.md`, and formal `docs/PHASE5_STATE.md`, `docs/PHASE6_STATE.md`, and `docs/PHASE7_STATE.md` records.
- `docs/architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md` and `docs/architecture/MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md`.
- `Assets/_Project/Scripts/TesteSimulacao.cs` (`InitializeSimulation`, `RebuildSystems`), `SimulationRuntime.cs` (`AdvanceDay` and composition), `SimulationModuleSet.cs`, and owner implementations for `SimulationTime`, `SimulationCalendar`, `EffectiveSimulationConfiguration`, runtime identities, random source, Person/population, city/market, travel parties, expeditions, directives, Knowledge, spatial and political/military stores.
