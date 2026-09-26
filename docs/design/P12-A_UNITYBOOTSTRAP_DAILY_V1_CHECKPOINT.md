# P12-A — UnityBootstrap-Daily-v1

**Record type:** planning checkpoint proposal; not accepted or authorized for implementation.  
**Checkpoint ID:** `P12-A` (stable planning identifier).  
**Candidate base:** `codex/phase8/canonical` at `77f3e1a47a1e007492a794ea777d681a21a36d09`.  
**Current candidate planning ref:** `codex/phase12/UnityBootstrapDailyPlan`, based on refreshed P12 docs at `e08708fd159011b0a97d44422ddd189067ebb1a8`.  
**Planning status:** `TECHNICAL_DESIGN_IN_PROGRESS`; current-base independent review is required before deriving `READY_FOR_IMPLEMENTATION`.  
**Implementation status:** `WAIT_DEPENDENCY`.

The checkpoint ID gives the Master a durable reference for this bounded plan. It does not mean the contract has human acceptance, does not create a P12 State, and does not claim save/load delivery or canonical promotion. The recommended profile below is recorded as an orchestrator assumption in response to the user's direction to choose clearly preferred repository-supported designs autonomously. It does not amend architecture or add a cross-host/product guarantee.

## Objective and closure boundary

Implement save and deterministic continuation for one normal single-player world created by the validated `TesteSimulacao.InitializeSimulation` path, using the repository's built-in providers and one `SimulationRuntime`. Capture is allowed only after `AdvanceDay` returns successfully and synchronous work for that daily advance is complete. Restore and subsequent execution use the same compatible build/runtime, current-host numeric profile, effective configuration, calendar, official definitions/content, and built-in provider composition. With identical subsequent inputs, the restored world must produce the same future authoritative results as uninterrupted execution.

This profile makes no Phase 13 historical reconstruction/fork claim and no cross-host numeric portability claim. It adds no loader, mod/module lifecycle, module-owned state, generated-world consumer contract, or P20 shared-activity coverage. It does not introduce gameplay such as Sleep, Dreams, robbery/gangs, rituals, War, or MegaEventos.

## Supported causal-state contract

The implementation inventory is the refreshed `docs/design/PHASE12_TECHNICAL_DESIGN.md` and `docs/design/PHASE12_CHECKPOINT_CONTRACT_PROPOSAL.md`, applied to the exact live composition when implementation becomes eligible. Preserve all causal owner facts, commitments, stable identities, revisions/allocator state, time/calendar/configuration/content/provider compatibility and random context required by that composition. Export and hydration must be complete for every supported populated owner; indexes and service references may be rebuilt only where the owner contract permits. Hydration is staged and publishes one coherent restored composition; partial owner publication is not an accepted result.

The selected profile excludes pending external `WorldCommand` and P11 actor-choice inputs. If either is pending in a presented runtime, profile admission/capture rejects it; an envelope declaring either input is rejected before publication. They are not silently omitted. This is a supported-profile boundary, not an adversarial security feature.

Preserve P8-C `PersonId` position identity and P8-D `PersonId`-owned spatial Knowledge and route-plan semantics when their referenced facts are in profile, including observation/provenance and Knowledge revisions, route evidence/basis, plan status/history/revisions, and one-active-plan-per-Person cardinality. Unsupported populated P8 spatial facts or route endpoints reject the profile rather than being dropped. Keep `PersonId`, `NpcRuntimeId`, and other typed identities distinct; do not infer a Person from an NPC. P8-E is canonical but is not a blanket dependency of this daily profile; the first profile does not claim P8-E in-transit travel support merely by relying on P8-C/D semantics.

## Conditional capabilities and revalidation

- **P9-A genesis manifest:** Before implementation, revalidate this inventory against the exact promoted P9-A candidate and refreshed bootstrap composition. If authored-genesis manifest facts/outputs are part of the selected profile, capture the actual generated outputs and their causal stage/contributor identities, deterministic order, versions, random context, and provenance. Never rerun genesis to reconstruct a historical save. P9-A promotion alone does not silently expand this profile.
- **P14 material-flow capability:** If a selected P14 capability is actually composed and included, inventory its exact source/sink owner facts and causal commitments and validate export/hydration before admitting those states. Do not infer P14 inclusion from the phase number or a planning candidate.
- **P18 intraday execution:** No blanket dependency for a daily boundary. Any later intraday profile requires the relevant promoted temporal identity, exact logical-time, ordering, pending-work, and hydration capabilities.
- **P19 extension surface:** No loader/module scope is included. Any later extension-owned state requires applicable promoted module lifecycle, compatibility, and state capabilities.
- **P20 multi-participant activities:** No shared-activity scope is included. If later supported, hydrate stable activity and participant identities, roles/formation, agreements, reservations, scheduled start, lifecycle/context, applied participant-specific effects, and causal ordering under the relevant promoted P20 contracts.

These conditional gates preserve extensibility requirements as current review constraints without implementing a speculative loader or requiring unrelated future phases for the bounded daily profile.

## Implementation readiness gates

P12-A remains `WAIT_DEPENDENCY` until all of the following are satisfied:

1. An independent reviewer confirms that the checkpoint contract and technical design apply to this current-base candidate and the current canonical architecture, roadmap, Phase 8 State, and both alignment records.
2. The exact included owner set is inventoried against the current canonical `SimulationRuntime` composition, including any selected P14 capability and any P9-A genesis-manifest state admitted by the supported profile.
3. Every included owner has a reviewed exact export and staged hydration path, with stable-ID/cardinality/reference validation and rejection-before-publication behavior; unsupported populated state is rejected rather than omitted.
4. The P9-A conditional genesis-manifest gate is revalidated after P9-A is promoted, if that capability is available before implementation. Historical generated outputs are captured; genesis is not rerun.
5. The candidate receives the required implementation, integration, and validation reviews/gates defined by current repository instructions and execution model.

Passing these gates can make the checkpoint implementation-ready; implementation and canonical promotion remain separate lifecycle steps. No human checkpoint approval is claimed here.

## Review evidence and source baseline

The inherited daily-profile contract review and technical-design review predate this current-base planning record; their earlier PASS does not by itself satisfy gate 1. The refreshed P12 technical design records targeted revalidation against canonical `77f3e1a47a1e007492a794ea777d681a21a36d09`, including P8-E promotion `d95b60d174cb0b17df09e2775b3cbd134c74b21f`, architecture baseline `c285466c355103d3637ac165246591b72eb7bda0`, and both alignment records. A fresh independent review of this checkpoint record and its Brief update must identify its exact content commit/ref before readiness may be derived.

Reconstruction-sensitive constraints are governed by `docs/EXECUTION_MODEL.md`. Semantic limits are governed by `docs/SIMULATION_ARCHITECTURE.md`, `docs/PHASE8_STATE.md`, `docs/architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md`, and `docs/architecture/MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md`. This record does not create a Phase 12 State or alter the Phase 12 objective.
