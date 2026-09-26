# Phase 12 — Save & Deterministic Continuation

**Authority:** planning entry subordinate to `../SIMULATION_ARCHITECTURE.md`. **Readiness:** `TECHNICAL_DESIGN_IN_PROGRESS`; P12-A is a bounded proposal and is not ready for implementation.

## Objective and closure

For supported compatible versions/profiles, continuing from boundary T and saving at T, loading, then continuing with the same inputs produce the same future authoritative results. Save is not replay or selective History.

**Checkpoint plan:** P12-A — `UnityBootstrap-Daily-v1` (proposal; durable record at `../design/P12-A_UNITYBOOTSTRAP_DAILY_V1_CHECKPOINT.md`). The ID is assigned for planning traceability only; it is not human acceptance, implementation authorization, delivery, or canonical promotion.

## Dependencies and gates

- **Hard semantic contracts:** deterministic simulation, semantic IDs, effective configuration/calendar/content compatibility and current-world mutation authority are already defined.
- **Hard capabilities:** complete closure needs hydration/continuation for every authoritative domain in the declared supported save scope; domain-specific integrations wait for stable corresponding contracts/capabilities.
- **Integration dependency:** restoration occurs at consistent boundaries and composes all relevant stores, plans, RNG state and command context without a second world authority.
- **Soft ordering:** generation and content may evolve in parallel; this does not excuse a false claim of complete save coverage.
- **Architecture gate:** choose supported scope, compatibility/versioning, numeric execution profile and restoration boundaries before a full technical design; current Phase 7 numeric portability is explicitly limited.
- **Product gate:** if support matrix/compatibility promises exceed existing guarantees, obtain user approval.
- **Exclusions:** historical fork guarantee as a Phase 12 result, universal event sourcing, implicit cross-host numeric portability.
- **P12-A profile assumption:** use the clearly recommended, bounded `UnityBootstrap-Daily-v1` profile: same-build/current-host daily continuation through the normal Unity bootstrap and built-in providers, captured only at a successfully completed daily boundary. This is an orchestrator planning assumption, not an architecture amendment or broader product promise. No P13 history/fork guarantee, cross-host guarantee, loader/module state, generated-world state, or shared-activity state is implied.
- **P12-A causal-input boundary:** pending external `WorldCommand` and P11 actor-choice inputs are unsupported profile state and must reject admission/capture rather than be omitted. This does not add a new security boundary; normal domain/action semantics remain authoritative.
- **Replay/fork sensitivity:** all authoritative truth, plans, Knowledge, IDs/allocators, logical time/calendar, effective config/content and causal randomness needed to continue.
- **Hotspots/parallelism:** `SimulationRuntime`, domain stores, command capture, diagnostics versus actual save state, composition/versioning; state inventory can start alongside P8/P9 work, integration is domain-gated.
- **P12-A implementation gates:** implementation remains `WAIT_DEPENDENCY` until every included owner has exact export and staged hydration support validated for this profile, the live canonical composition and profile inventory are revalidated after P9-A promotion, and any selected P14 capability included in the profile is similarly inventoried and validated. If P9-A genesis-manifest state is admitted, capture its causal identity, ordered contributors, versions, random context, provenance, and generated outputs; never rerun genesis to restore a historical world. P8-E is canonical but is not a blanket daily-profile dependency. Preserve P8-C `PersonId` position identity and P8-D `PersonId`-owned Knowledge/route-plan identity, revisions, evidence, and one-active-plan-per-Person semantics wherever the profile supports those facts; reject unsupported populated spatial state instead of weakening those contracts.
- **Downstream unlocks:** continuation foundation for P13 historical reconstruction/fork.
- **Deferred:** final storage format, migration matrix and replay algorithm until entry design.

## Temporal and extension-state inventory — 2026-09-26

Inventory/design can proceed alongside P18 and generation work. A supported
intraday save profile needs stable P18 state/ordering contracts and promoted
capabilities for hydration/integration, including active activities, commitment,
availability, pending work or reconstruction inputs and same-time causal sequences.
An explicitly bounded daily profile can precede it; it cannot claim complete
intraday coverage. Do not freeze storage around a day-only clock.

Include causally relevant generation stage/contributor identities, effective
versions and outputs. Mod-owned state joins the inventory when supported;
P19 module lifecycle and explicit retrofit compatibility must be available before
claiming those integrations. Do not invent mod schemas or require P19 to save
ordinary official worlds. Missing compatible code/content cannot silently
discard extension state. Revalidate pre-change continuation proposals against
these additions; historical retained state is not regenerated on installation.

## Shared-activity continuation inventory

When P20 is in the supported save scope, hydrate one instance with compatible
definition/version, formation state, participant identities/roles, agreements,
future reservations/intervals, scheduled start and shared lifecycle/context.
Preserve participant-specific effects already applied and pending causal work
without duplicating the activity into separate actor-owned truths. Inventory can
begin on accepted P20 contracts; hydration waits for actual capabilities. This
is conditional coverage, not a blanket P20 dependency for all P12 saves.
