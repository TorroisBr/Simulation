# Phase 11 — Actor Perspective & Commands v1

**Authority:** planning entry subordinate to `../SIMULATION_ARCHITECTURE.md`. **Readiness:** `ENTRY_ARCHITECTURE_READY`; no implementation checkpoint is schedulable.

## Objective and closure

Establish a bounded actor-control and external-command slice in which a human choice replaces the actor's autonomous choice without granting hidden knowledge or outcome authority, while GM/external commands pass through explicit validation and domain execution.

**Checkpoints:** to be defined at architecture/technical entry; no P11 checkpoint IDs are approved.

## Dependencies and gates

- **Hard semantic contracts:** existing Person/Knowledge/decision-execution and `Request`/`Declare`/supported `ForceOutcome` distinctions in the architecture.
- **Hard capabilities:** a chosen command consumer needs its actual domain authority, not merely a registered provider; no blanket P8/P9 capability dependency is established.
- **Integration dependency:** input capture must enter the supported WorldCommand/domain boundary and coexist with autonomous processing.
- **Soft ordering:** civil travel may later be a consumer but is not required for the entry design.
- **Architecture gate:** define the first bounded actor/command consumer, perspective boundary and logical command ordering before implementation.
- **Product gate:** any choice about which user interventions are allowed or exposed requires explicit user intent; no broad permission is inferred here.
- **Exclusions:** omniscient player mode by default, arbitrary client mutation, universal `ForceOutcome`, UI-heavy clients and full networking.
- **Replay/fork sensitivity:** command payload, authority, logical application boundary, order, acceptance/rejection semantics and any authoritative effects must be recoverable.
- **Hotspots/parallelism:** `WorldCommandFoundation`, command handlers, Knowledge, `SimulationRuntime` and diagnostics; entry design may proceed independently of worldgen, but shared command/runtime edits require ownership.
- **Downstream unlocks:** durable external-input semantics useful to continuation and historical reconstruction.
- **Deferred:** client UI/API transports, multiplayer and a universal actor-control framework.

## Actor availability and local-player alignment — 2026-09-26

The first separately reviewed proposal chooses local SellGoods through trusted
single-player input, without actor-control grants. Ordinary gameplay eligibility,
Knowledge and execution validation remain; no adversarial authorization or
anti-cheat layer is implied. The proposal/technical design remains candidate
evidence until explicitly accepted/promoted; this paragraph does not promote it.

A bounded existing daily-turn choice adapter may proceed after targeted review.
It must not make one action per day the durable actor contract. P18-C/D supplies
availability-driven decision/application boundaries and the later adapter
migration; P11's basic typed input/capture need not wait for the whole of P18.
Capture payload, authority, logical application boundary and ordering when
introduced. In an intraday profile, a day/roster slot alone is insufficient.
Deferred/pending inputs and dispatched attempts retain their causal lifecycle;
future integration must not reinterpret old daily inputs or lose those records.

Revalidate pre-change runtime/turn adapters against this boundary before further
approval. Preserve the input store and existing SellGoods domain seam where
compatible; public mod API/loader and UI transport remain later work.

## Participation input boundary

The bounded SellGoods actor-choice slice does not wait for P20. A future
participation/proposal/withdrawal consumer needs P20's actual contracts, retaining
each actor's decision and stable input causality. Choosing for one actor does
not automatically choose for all activity participants or reveal their Knowledge.
No recruitment UI, group-control grants or negotiation engine is added to P11.
