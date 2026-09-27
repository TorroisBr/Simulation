# Phase 12 — Save & Deterministic Continuation

**Authority:** planning entry subordinate to `../SIMULATION_ARCHITECTURE.md`. **Readiness:** `TECHNICAL_DESIGN_IN_PROGRESS`; full design/current-base review passed at `9fde12a` and the scoped reference-refresh review passed at `d1a8414`. Proposed P12-A remains unaccepted and `WAIT_DEPENDENCY` for implementation until complete included-owner exact export and staged hydration, a live profile inventory, formal checkpoint acceptance, and separate implementation authorization are established.

## Objective and closure

For supported compatible versions/profiles, continuing from boundary T and saving at T, loading, then continuing with the same inputs produce the same future authoritative results. Save is not replay or selective History.

**Checkpoint plan:** P12-A — `UnityBootstrap-Daily-v1` (proposal; contract proposal at `../design/PHASE12_CHECKPOINT_CONTRACT_PROPOSAL.md`). The ID is assigned for planning traceability only; it is not human acceptance, implementation authorization, delivery, or canonical promotion.

## Dependencies and gates

- **Hard semantic contracts:** deterministic simulation, semantic IDs, effective configuration/calendar/content compatibility and current-world mutation authority are already defined.
- **Hard capabilities:** complete closure needs hydration/continuation for every authoritative domain in the declared supported save scope; domain-specific integrations wait for stable corresponding contracts/capabilities.
- **Integration dependency:** restoration occurs at consistent boundaries and composes all relevant stores, plans, RNG state and command context without a second world authority.
- **Soft ordering:** generation and content may evolve in parallel; this does not excuse a false claim of complete save coverage.
- **Resolved bounded profile:** `UnityBootstrap-Daily-v1` uses the SampleScene-selected `Simulation-GeneralTest.asset` through the validated Unity bootstrap, exact compatible build/runtime and current-host numeric profile, and completed-day capture boundaries. The selected asset enables the P9-B authored-geography profile and publishes exactly one P8-A Hex and one anchored Location before day one. P9-B is included on `codex/phase9/canonical` at code tip `d9a62d7c6bea242653c2d68cc0a70911bb5ed1bf` (implementation tip `00395ef80cfa2364d34ed2170e0735d3a4b1513d`); its current promotion-record State/status tip is `14a2e8ee01e49f1ffdcecf8fd64d274c728ead44`. This P12 profile is incompatible with P9-A-only configs. Any retained P9-A-only profile requires a distinct profile identity and admission that explicitly rejects authored geography. P12 excludes intraday state, P9/P10 generated worlds, P19 module state, P20 shared activities, and P13 historical fork guarantees.
- **Architecture/checkpoint gate:** P12-A's candidate contract must pass independent review against current canonical architecture, Roadmap, Execution Model, P8 State, P9-A/P9-B profile-manifest contracts, P11 input authorities, current P18 State, reviewed P20 technical design and proposed P20-A checkpoint, and both alignment records. The selected P9-B geography capability is present on current P9 canonical at `d9a62d7`; this P12 design does not substitute for P12's independent review or acceptance gates. P10-A is independently reviewed and ready for implementation at candidate `345dcbc`, but has no delivered runtime; its bounded Ruin/LocalTopology profile does not change P12's exclusion of generated P10 state.
- **Exclusions:** historical fork guarantee as a Phase 12 result, universal event sourcing, implicit cross-host numeric portability.
- **P12-A profile assumption:** use the clearly recommended, bounded `UnityBootstrap-Daily-v1` profile: same-build/current-host daily continuation through the normal Unity bootstrap and built-in providers, captured only at a successfully completed daily boundary. This is an orchestrator planning assumption, not an architecture amendment or broader product promise. No P13 history/fork guarantee, cross-host guarantee, loader/module state, generated-world state, or shared-activity state is implied.
- **P12-A causal-input boundary:** the selected bootstrap has no external `WorldCommand` service/queue composition; adding it is unsupported and must reject profile admission. When the promoted P11 `ActorChoiceStore` is composed, preserve full terminal input/disposition and idempotency history plus sequence; reject `Pending` (including deferred) and `ConsumedAwaitingTerminalAttempt` records at capture/load. This does not add a new security boundary; normal domain/action semantics remain authoritative.
- **Replay/fork sensitivity:** all authoritative truth, plans, Knowledge, IDs/allocators, logical time/calendar, effective config/content and causal randomness needed to continue.
- **Hotspots/parallelism:** `SimulationRuntime`, domain stores, command capture, diagnostics versus actual save state, composition/versioning; state inventory can start alongside P8/P9 work, integration is domain-gated.
- **P12-A implementation gates:** implementation remains `WAIT_DEPENDENCY` until every included owner has exact export and staged hydration support validated for this profile, the live canonical composition and profile inventory are revalidated, and formal checkpoint acceptance plus separate implementation authorization are complete. The full-design and scoped current-base review evidence is recorded above; no P12 implementation checkpoint has been accepted. P9-B is present on current P9 canonical at code tip `d9a62d7` with State/status tip `14a2e8e`. Preserve the selected P9-B geography profile identity/schema and fingerprint/provenance, seed/config/calendar, inherited P9-A stage identities, P9-B stage/dependencies, authored inputs/outputs and first boundary without rerunning genesis. P8-A's selected Hex, terrain/revision provenance, anchored Location and scale context are populated required state; P8-B through P8-E passage, City/Site presence anchors, Person positions, route Knowledge/plans, and travel remain empty for this profile. Unsupported or partially populated state must reject admission. P8-E is not a blanket daily-profile dependency. P14-A is not configured by the selected GeneralTest City assets; a future profile that composes its authored material flow must include its owner state or reject admission. Complete included-owner exact export and staged hydration coverage plus a live profile inventory are not yet demonstrated.
- **Read-only owner evidence:** [`../design/PHASE12_OWNER_COVERAGE_INVENTORY.md`](../design/PHASE12_OWNER_COVERAGE_INVENTORY.md) records per-authority export/hydration gaps. It is evidence only, not checkpoint acceptance or implementation authorization; P12-A remains `WAIT_DEPENDENCY`.
- **Downstream unlocks:** continuation foundation for P13 historical reconstruction/fork.
- **Deferred:** final storage format, migration matrix and replay algorithm until entry design.

## Temporal and extension-state inventory — 2026-09-27

Inventory/design can proceed alongside P18 and generation work. This daily
profile does not compose the P18 timeline and makes no intraday continuation
claim. If a later profile includes P18 state, reconstruct its boundary identity
`(worldId, profileId, absoluteDay)`, tick quantum/version, effective
`MaxDispatchesPerInstant` (or equivalent dispatch limit), work identity
`(ownerId, workId, revision, occurrence)`, exact logical instant, causal wave and
same-instant order, pending boundary/work, persisted occurrence/sequence and
owner idempotency/effect state. Preserve sealed external inputs with target
`LogicalTick`, accepted sequence/order, and boundary state. Include promoted P18-B activity instance,
lifecycle/revision/receipt, participant commitments and availability facts, and
P18-C PersonId-keyed decision/attempt state only when the selected consumer
composes them. P18-A/B/C are promoted; P18-D is still blocked and legacy daily
execution remains authoritative until a selected consumer migrates. A day value
alone cannot stand in for these facts, and the bounded daily profile does not
depend on P18-D.

Include causally relevant generation stage/contributor identities, effective
versions and outputs. Mod-owned state joins the inventory when supported;
P19 module lifecycle and explicit retrofit compatibility must be available before
claiming those integrations. Do not invent mod schemas or require P19 to save
ordinary official worlds. Missing compatible code/content cannot silently
discard extension state. Revalidate pre-change continuation proposals against
these additions; historical retained state is not regenerated on installation.

## Shared-activity continuation inventory

When P20 is in the supported save scope, hydrate one stable `ActivityInstanceId`
and compatible definition/version separately from participant identities. Keep
the architecture's one-or-more participant cardinality; the reviewed two-Person
P20 fixture does not define a universal count or role policy. Include partial
formation and independent PersonId decisions, roles where applicable, agreements,
reservation intents and committed intervals, scheduled start, shared lifecycle/
revision/context, already-applied participant effects, and pending due work with
its causal input, order, sequence and idempotency state. Never infer one
Activity-to-one-actor cardinality or duplicate a shared instance into actor-owned
truth. P20 remains conditional and outside this profile; its reviewed design is
not an implementation capability or blanket P12 dependency.
