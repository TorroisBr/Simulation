# Phase 12 — Save & Deterministic Continuation

**Authority:** planning entry subordinate to `../SIMULATION_ARCHITECTURE.md`. **Readiness:** `ENTRY_ARCHITECTURE_READY`; no implementation checkpoint is schedulable.

## Objective and closure

For supported compatible versions/profiles, continuing from boundary T and saving at T, loading, then continuing with the same inputs produce the same future authoritative results. Save is not replay or selective History.

**Checkpoints:** to be defined at architecture/technical entry; no P12 checkpoint IDs are approved.

## Dependencies and gates

- **Hard semantic contracts:** deterministic simulation, semantic IDs, effective configuration/calendar/content compatibility and current-world mutation authority are already defined.
- **Hard capabilities:** complete closure needs hydration/continuation for every authoritative domain in the declared supported save scope; domain-specific integrations wait for stable corresponding contracts/capabilities.
- **Integration dependency:** restoration occurs at consistent boundaries and composes all relevant stores, plans, RNG state and command context without a second world authority.
- **Soft ordering:** generation and content may evolve in parallel; this does not excuse a false claim of complete save coverage.
- **Architecture gate:** choose supported scope, compatibility/versioning, numeric execution profile and restoration boundaries before a full technical design; current Phase 7 numeric portability is explicitly limited.
- **Product gate:** if support matrix/compatibility promises exceed existing guarantees, obtain user approval.
- **Exclusions:** historical fork guarantee as a Phase 12 result, universal event sourcing, implicit cross-host numeric portability.
- **Replay/fork sensitivity:** all authoritative truth, plans, Knowledge, IDs/allocators, logical time/calendar, effective config/content and causal randomness needed to continue.
- **Hotspots/parallelism:** `SimulationRuntime`, domain stores, command capture, diagnostics versus actual save state, composition/versioning; state inventory can start alongside P8/P9 work, integration is domain-gated.
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
