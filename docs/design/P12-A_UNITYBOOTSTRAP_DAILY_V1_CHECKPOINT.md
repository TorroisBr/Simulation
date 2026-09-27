# P12-A — UnityBootstrap-Daily-v1

**Record type:** planning checkpoint proposal; not accepted or authorized for implementation.
**Checkpoint ID:** `P12-A` (stable planning identifier).
**Current canonical references:** P8 `codex/phase8/canonical` at `470667d37863384edadb3d93ef64d8004aff46a3`; architecture `c285466c355103d3637ac165246591b72eb7bda0`; P9-A code promotion `43f08b3dfbf042380c2f8a8b037bbf3ebd309ccb` and closure State `96f2c1aaf742f313bbb9643e5f5b3d844c402c78`; P11 code promotion `0cd4281804ecc6a2d110352d1a238959e93867f0` and closure State `308e24d0744112e8f2b741521b8b3e4acb51ebbf`.
**Current candidate refs:** technical design `codex/phase12/ContinuationTechnicalDesign` at `e0023d2`; planning contract/Brief on `codex/phase12/UnityBootstrapDailyPlan` in this commit. Fresh independent review at both exact resulting tips remains pending.
**Planning status:** `TECHNICAL_DESIGN_IN_PROGRESS`; current-base independent review is required before deriving `READY_FOR_IMPLEMENTATION`.
**Implementation status:** `WAIT_DEPENDENCY`.

The checkpoint ID gives the Master a durable reference for this bounded proposal. It does not mean the contract has human acceptance, does not create a delivery State, and does not claim save/load delivery or canonical promotion. The bounded repository-supported profile is the selected planning recommendation; it does not amend architecture or add a cross-host/product guarantee. No separate product choice remains open inside this profile.

## Objective and closure boundary

Implement save and deterministic continuation for one normal single-player world created by the validated `TesteSimulacao.InitializeSimulation` path, using the repository's built-in providers and one `SimulationRuntime`. Capture is allowed only after `AdvanceDay` returns successfully and synchronous work for that daily advance is complete. Restore and subsequent execution use the same compatible build/runtime, current-host numeric profile, effective configuration, calendar, official definitions/content, and built-in provider composition. With identical subsequent inputs, the restored world must produce the same future authoritative results as uninterrupted execution.

This profile makes no Phase 13 historical reconstruction/fork claim and no cross-host numeric portability claim. It adds no loader, mod/module lifecycle, module-owned state, generated-world consumer contract, or P20 shared-activity coverage. It does not introduce gameplay such as Sleep, Dreams, robbery/gangs, rituals, War, or MegaEventos.

## Supported causal-state contract

The implementation inventory is the refreshed `docs/design/PHASE12_TECHNICAL_DESIGN.md` and `docs/design/PHASE12_CHECKPOINT_CONTRACT_PROPOSAL.md`, applied to the exact live composition when implementation becomes eligible. Preserve all causal owner facts, commitments, stable identities, revisions/allocator state, time/calendar/configuration/content/provider compatibility and random context required by that composition. Export and hydration must be complete for every supported populated owner; indexes and service references may be rebuilt only where the owner contract permits. Hydration is staged and publishes one coherent restored composition; partial owner publication is not an accepted result.

The selected profile excludes pending external `WorldCommand` and P11 actor-choice inputs. If either is pending in a presented runtime, profile admission/capture rejects it; an envelope declaring either input is rejected before publication. The implementation must query the promoted input authorities to enforce this boundary; it may not silently omit their state. This is a supported-profile boundary, not an adversarial security feature.

P8 dependencies are state-specific. Use P8-A stable `LocationId` and promoted P8-C City/Site anchor composition where those facts are part of the selected bootstrap. The profile requires the P8-C Person-position store to be empty; reject non-empty `At` or `InTransit` state. Preserve P8-D `PersonId`-owned spatial Knowledge/route-plan semantics only where all referenced facts are in profile, including provenance/revisions, route evidence/basis, plan status/history/revisions, and one-active-plan-per-Person cardinality. Reject P8-D Knowledge/plans that refer to excluded spatial facts, along with unsupported non-empty P8 geometry, terrain, passage or P8-E travel state. P8-B passage and P8-E travel capabilities are not blanket dependencies for this daily profile. Keep `PersonId`, `NpcRuntimeId`, and other typed identities distinct; do not infer a Person from an NPC.

## Conditional capabilities and revalidation

- **P9-A authored-genesis manifest:** The selected bootstrap executes promoted P9-A and publishes `SimulationGenesisManifest`; its stable `unity-authored-bootstrap/genesis-v1` profile/contract identity, schema, fingerprint, seed source/value, effective configuration/calendar, ordered stage identities and dependency edges, authored input/output provenance, and first simulated boundary are required compatibility evidence. Preserve or validate that evidence under the envelope integrity digest. The manifest records genesis provenance, not evolved owner state. Hydration restores saved owner sections and never reruns genesis. P9/P10 generated worlds and generated-content consumers remain excluded; P10 is not a dependency.
- **P14 material-flow capability:** If a selected P14 capability is actually composed and included, inventory its exact source/sink owner facts and causal commitments and validate export/hydration before admitting those states. Do not infer P14 inclusion from the phase number or a planning candidate.
- **P18 intraday execution:** No blanket dependency for a daily boundary. Any later intraday profile requires the relevant promoted temporal identity, exact logical-time, ordering, pending-work, and hydration capabilities.
- **P19 extension surface:** No loader/module scope is included. Any later extension-owned state requires applicable promoted module lifecycle, compatibility, and state capabilities.
- **P20 multi-participant activities:** No shared-activity scope is included. If later supported, hydrate stable activity and participant identities, roles/formation, agreements, reservations, scheduled start, lifecycle/context, applied participant-specific effects, and causal ordering under the relevant promoted P20 contracts.

These conditional gates preserve extensibility requirements as current review constraints without implementing a speculative loader or requiring unrelated future phases for the bounded daily profile.

## Implementation readiness gates

P12-A remains `WAIT_DEPENDENCY` and is not ready for implementation until all of the following are satisfied:

1. An independent reviewer confirms that the checkpoint contract and technical design apply to their exact current-base tips and the current canonical architecture, roadmap, P8 State, promoted P9-A manifest, promoted P11 input authorities, and both alignment records. This exact review is pending and cannot be inferred from earlier review passes.
2. The exact included owner set is inventoried against the current canonical `SimulationRuntime` composition, including any selected P14 capability and any P9-A genesis-manifest state admitted by the supported profile.
3. Every included owner has a reviewed exact export and staged hydration path, with stable-ID/cardinality/reference validation and rejection-before-publication behavior; unsupported populated state is rejected rather than omitted.
4. The required P9-A genesis-manifest compatibility mapping is confirmed against the current bootstrap composition. Historical genesis is never rerun during hydration; generated P9/P10 worlds remain out of scope.
5. The candidate receives the required implementation, integration, and validation reviews/gates defined by current repository instructions and execution model.

Passing these gates can make the checkpoint implementation-ready; implementation and canonical promotion remain separate lifecycle steps. No human checkpoint approval is claimed here.

## Review evidence and source baseline

The inherited daily-profile contract review and technical-design review predate this current-base planning record; their earlier PASS does not by itself satisfy gate 1. The refreshed P12 technical design records targeted revalidation against canonical `77f3e1a47a1e007492a794ea777d681a21a36d09`, including P8-E promotion `d95b60d174cb0b17df09e2775b3cbd134c74b21f`, architecture baseline `c285466c355103d3637ac165246591b72eb7bda0`, and both alignment records. A fresh independent review of this checkpoint record and its Brief update must identify its exact content commit/ref before readiness may be derived.

Reconstruction-sensitive constraints are governed by `docs/EXECUTION_MODEL.md`. Semantic limits are governed by `docs/SIMULATION_ARCHITECTURE.md`, `docs/PHASE8_STATE.md`, `docs/architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md`, and `docs/architecture/MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md`. This record does not create a Phase 12 State or alter the Phase 12 objective.
