# P18-D Demography Owner Execution Plan

**Status:** Bounded technical plan, ready for independent design review. No implementation or validation is claimed here.

**Architecture basis:** P18 canonical `9e790c59e14ca7f7ed195c0e6267e10f3cd039d7`, especially `docs/design/PHASE18_D_TECHNICAL_DESIGN.md` §4.1, and `docs/EXECUTION_MODEL.md`.

## Purpose and scope

This plan makes the accepted P18-D demography due-work fact resumable after an uncertain operation response while retaining existing demographic semantics. The owner remains `DailyDemographicSystem`; death lifecycle effects remain owned by `PersonDeathLifecycleSystem`; population values and aggregate transitions remain owned by `SettlementPopulationRuntime` / `AggregateDemographySystem`.

This plan does not add demographic outcomes, population rules, gameplay systems, a generic transaction framework, coordinator-owned mutations, cross-runtime persistence, or a new daily profile. Receipts are scoped to the live `SimulationRuntime`, consistent with the P18-D design's lifetime boundary.

## Occurrence and target identity

- The parent occurrence is the stable P18 boundary occurrence for the existing semantic demography step. It identifies this execution of the step, not an attempt to execute it.
- Freeze the ordered mortality target roster by `PersonId`, using ordinal comparison, together with each target's applicable exact `NpcRuntimeId` binding and materialization cardinality. Freeze the aggregate target roster by `CityRuntimeId`, also in ordinal order.
- Derive each internal effect identity from the parent occurrence, a semantic owner substep, and the stable domain target (`PersonId` or `CityRuntimeId`). The resident-population decrement belonging to a Person death and that City's aggregate transition must have different semantic identities.
- List indices are ordering cursors only. They are never effect or receipt identities. Do not use attempt count, mutable list position, mortality outcome, or a pre-mortality resident floor in an effect identity.
- The frozen roster does not freeze mutable truth. At each target, recheck current registration/liveness, birth-date and materialization bindings as needed; prepare the effect from current state. Do not freeze whether a Person dies or a City transition/floor at manifest activation.

## Owner contracts

### Person death lifecycle

`PersonDeathLifecycleSystem` owns preparation and application of the existing death lifecycle: resident aggregate decrement when applicable, Person death, optional materialized NPC death, and clearing Person residence. It must accept a stable operation identity and retain an immutable result for the matching fingerprint.

Preparation uses current truth immediately before the effect: boundary day, exact registered Person, living state, Person identity, current residence `CityRuntimeId`, and the applicable materialized `NpcRuntimeId`/cardinality and binding. For a represented resident, capture the settlement population revision and count used for its decrement. Revalidate these facts at install; a changed identity, residence, population revision/count, or other precondition rejects before the corresponding mutation.

The resident decrement is an independently owned `SettlementPopulationRuntime` effect with its own receipt (see below). The lifecycle owner retains progress linking that receipt to the death operation. On retry, it reads/replays the same decrement receipt before continuing the lifecycle projection, so the aggregate count is not decremented twice. Use the existing death preflight/no-fail seam for Person/NPC/residence writes, and commit the matching lifecycle receipt with those writes. Check an existing receipt before the legacy “already dead” rejection. Exact replay returns the retained transition as `Replayed`; only a newly applied death returns `Applied`. `SimulationRuntime` must advance any success-only revision (including the political world revision) only for `Applied`, never for a replay.

A reused identity with a different fingerprint is rejected. Do not compensate or repeat a committed death effect to handle an uncertain response.

### Settlement population effects

`SettlementPopulationRuntime` owns receipt installation together with each change to its population and revision. This includes both a Person-death resident decrement and a City aggregate transition; they use distinct semantic effect identities.

Preparation captures the current City identity, boundary day, population revision and value, and the exact proposed delta/result. For an aggregate transition, the represented-resident floor is obtained only after all mortality effects have completed. Install verifies the prepared revision/value and applicable floor/current-truth constraints, then writes the population, advances the revision once, and retains the immutable receipt/result as one owner commit.

An exact identity/fingerprint replay returns the first retained result without another population or revision change. Conflicting reuse or stale current truth rejects before mutation. A confirmed no-install failure may be retried under the existing contract; an uncertain response is resolved by receipt lookup, not by reapplying the delta.

Keep `AggregateDemographySystem.TryPropose` / `TryApply` semantics and existing provider-failure handling. In particular, preserve the existing guard that detects and restores a provider's attempted mutation of the population during proposal.

### Daily demography progress and report

`DailyDemographicSystem` owns the parent occurrence record, frozen target rosters, phase/cursors, per-target results, ordered diagnostics, and final `DailyDemographyReport`. It retains a sampled/evaluated Person outcome before trying a death effect so a resumed attempt does not ask a potentially stateful sample provider for a different answer. A failed sample/evaluation and its diagnostic are also retained once. After an uncertain effect response, consult the owner receipt and reconstruct the same per-target result instead of repeating the mutation.

Progress is ordered in two phases. First process the frozen Person roster in `PersonId` order. Preserve the existing skip for a Person already dead when reached; increment `NamedPersonsEvaluated` for each living Person before sampling. Retain the sample failure, evaluation failure, non-death outcome, or death result and its existing diagnostic/counter contribution. After all mortality targets complete, build the represented-resident floor snapshot from current truth. Only then process the frozen City roster in `CityRuntimeId` order, preparing each proposal just before its effect. Retain each proposal failure, apply failure, provider exception, or successful transition result. After all targets, retain and assign the exact completed report once.

The P18-D coordinator sees the demographic owner's prepared/committed result, retained report and declared source signals. It does not sequence individual Person or City mutations and does not own demographic truth. The existing demographic operations expose no typed domain signals; return no source signals until such signals are defined by an existing owner contract. Do not invent events for reporting.

## Legacy behavior to preserve

- Natural mortality targets remain ordinally sorted by `PersonId`; aggregate targets remain ordinally sorted by `CityRuntimeId`.
- All natural-mortality work finishes before represented-resident floors are recomputed. City aggregate transitions follow in City order.
- Preserve the current sample/evaluation/apply failure behavior and diagnostic codes, identities, severities, messages, and order, including `NaturalMortalitySampleFailed`, `NaturalMortalityEvaluationRejected`, `NaturalMortalityApplyRejected`, `RepresentedResidentFloorMissing`, `AggregateDemographyProposalRejected`, `AggregateDemographyApplyRejected`, and `AggregateDemographyProviderFailed`.
- Preserve report assignment and exact counter meanings: `NamedPersonsEvaluated`, `NamedDeathsApplied`, `AggregateSettlementsProcessed`, `AggregateBirthsApplied`, and `AggregateDeathsApplied`. A replay contributes the retained original result once; it must not double-count an effect.

## Focused validation plan

After independent design review and implementation, add/run focused EditMode coverage for:

1. Stable Person/City semantic identities, frozen order, and materialization identity/cardinality drift rejection; prove ordinals affect ordering only.
2. Resumption after a retained Person sample/evaluation result without resampling, and exact death replay without a second resident decrement, Person/NPC/residence mutation, or success-only world revision increment.
3. Death receipt fingerprint conflict and stale Person, Npc, residence, day, or population truth rejection before mutation.
4. Settlement population receipt exact replay without another value/revision change; conflicting identity/fingerprint and stale revision/value rejection; distinct receipts for death decrement versus aggregate transition.
5. A mortality effect changing the represented-resident floor before the first City proposal; partial death and City batches resume in the required order.
6. Exact legacy report counters, diagnostic content/order, exception behavior, and completed-report replay after each possible interruption boundary.

Run the focused DailyDemography and relevant PersonNaturalMortality/Population suites plus `git diff --check` for the implementation. Broader P18 integration validation remains with the chronological integrator.

## Readiness and approval boundary

P18-D's architecture/checkpoint scope and technical design were accepted, and the canonical design at `9e790c5` explicitly requires owner-local progress/receipts for this one due-work fact. The owner-local receipt identities, preflight interfaces, retry cursors, and tests in this plan settle implementation mechanics within that accepted semantic contract. Under `docs/EXECUTION_MODEL.md`, independent review of this bounded technical plan is the remaining design-readiness gate; after it passes, the already accepted P18-D scope authorizes isolated implementation. No additional checkpoint acceptance is required. Canonical promotion and Phase closure remain separate gates.
