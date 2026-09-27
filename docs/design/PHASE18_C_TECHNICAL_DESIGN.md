# P18-C — Availability-driven Actor Decisions Technical Design

**Prior design candidate:** `97b97c5c7523f39f3645bc018c82dbbab633648f` (`codex/phase18/P18CAvailabilityDecisionDesign`), now refreshed against promoted P18-A/B canonical tip `3d4fe829f4be41fc9e9bb11052a320c3eb00d94d`. P18-A source is `985c56c40fc01dc6a4d392120e2d32151a558d03`; P18-B source is `97918cbbe4238a65a216b1a1f0ef84c70b4d080c`. Both consume architecture baseline `c285466c355103d3637ac165246591b72eb7bda0` and the current intraday/extensibility and multi-participant alignment records.
**Authority:** `docs/SIMULATION_ARCHITECTURE.md` §§2, 11–12, 91–92; `docs/ROADMAP.md`; `docs/EXECUTION_MODEL.md`; `docs/phases/PHASE18_BRIEF.md`; `docs/design/PHASE18_A_TECHNICAL_DESIGN.md`; `docs/design/PHASE18_B_TECHNICAL_DESIGN.md`; `docs/architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md`; `docs/architecture/MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md`.
**Status:** Design revalidation required after P18-A/B promotion. This remains a bounded technical-design proposal only. No implementation checkpoint IDs or capability promotion are introduced. The promoted P18-B API does not yet expose committed transition signals, and its timeline-bound scheduling constraints require a post-advance decision handoff described below. Implementation remains blocked until this refreshed exact design passes independent review and the implementation boundary is recorded.

## 1. Purpose and boundary

P18-C connects individual availability boundaries to actor decision and action execution. When an actor is eligible, it makes one decision using only currently permitted Knowledge and a stable decision context. The selected action is then revalidated against current world truth by its owning execution domain. Decision evidence does not authorize or guarantee the outcome.

The current `SimulationRuntime` daily path calls `EvaluateAction` and immediately attempts execution for each eligible runtime during the daily pass. `NpcDecisionSystem` computes deterministic weighted utility from configured actions, status, job/default-action preferences, and action providers; action providers create runtime action objects and execute through `TryExecuteAction`. `NpcActionRuntime` may hold transient `NpcRuntime`/`CityRuntime` references alongside serialized action parameters. P18-C must adapt these current seams behind stable semantic identities and explicit causal inputs; it must not elevate the existing one-action-per-day traversal, Unity object references, provider iteration order, or a synchronous call stack into intraday authority.

P18-C does not own the timeline or due-work queue (P18-A), activity lifecycle/commitments/availability authority (P18-B), consumer outcomes, travel, player command capture, persistence, or mod loading. It adds neither concrete gameplay nor a universal actor workflow. An actor may choose again after a meaningful decision boundary; this is not a promise of one decision at every tick.

## 2. Identity, decision request, and inputs

Use stable `PersonId` for Person-backed actor decision ownership. A decision request identifies the world/run, actor, exact `LogicalTick`, stable triggering boundary/input identity, relevant availability/condition revision, and deterministic decision sequence. Runtime materialization is a resolver for execution context, not the actor's identity or the source of eligibility.

For a bounded initial implementation, retain the existing action definition and provider seams, but resolve each definition/provider through a stable semantic ID and compatible version. Build candidates in stable ID order before utility evaluation or random selection; never use dictionary order, Unity asset discovery order, runtime IDs, or provider registration order as tie-break authority. Existing deterministic random selection must be driven by an explicit decision random context derived from stable world/actor/decision identity and recorded random state as required by the authoritative random-source contract. Candidate enumeration and random consumption must be unchanged by materialization order.

The decision snapshot is a read-only, Knowledge-bounded view. It may include known facts, available supported actions, actor-owned preferences/status inputs, and disclosed availability/condition notifications. A hidden current-world fact cannot enter utility or candidate filtering merely because the execution subsystem can observe it. Decision output is a proposal carrying stable actor/action/target identifiers, semantic parameters, evidence/provenance permitted by the action contract, and an origin decision identity. It must not carry authoritative object references as its only target identity.

## 3. Event-driven reevaluation and promoted P18-B seam

The actor decision coordinator consumes typed, committed domain boundary signals and queues eligible decision requests; it does not poll per frame or scan all actors at every tick. A signal identifies its stable source, affected `PersonId` or bounded affected set, exact logical instant, and committed source revision/sequence. Relevant signals are:

- a participant commitment/availability transition from P18-B (activity scheduled, starts, completes, or supported cancellation/interruption releases or changes the commitment);
- a supported condition boundary that changes decision eligibility or a configured action's known utility inputs;
- a newly accepted external command/input explicitly addressed to the actor or decision boundary;
- an owning domain's completion/failure/disposition signal that makes the actor eligible to decide again.

**Promoted-API gap:** P18-B at `3d4fe82` provides lifecycle snapshots, participant commitments, `IsAvailable`, scheduling/termination, and due-work dispatch, but has no committed-transition signal API or transition history. P18-C implementation must add a narrow, append-only lifecycle-transition receipt seam owned by `ActivityLifecycleStore`, atomically recorded with each committed scheduling/eligibility transition (schedule, start, complete, cancellation, interruption, or failed start). Each receipt carries a stable transition sequence/identity, activity-instance ID and revision, transition kind, exact logical instant, affected stable participant IDs, and committed disposition. It is an event/causal trigger only; current lifecycle state and `IsAvailable` remain the authority. No callback may mutate the lifecycle store or timeline reentrantly. The coordinator drains newly committed receipts by sequence at the explicit post-advance handoff; this is incremental event delivery, not status polling. The seam must preserve receipts or enough deterministic reconstruction input to avoid losing an unconsumed transition. This is an additive P18-C implementation seam, not a claim that the promoted P18-B API already supplies it.

Other signals follow the same post-commit rule. Their ordering follows P18-A: captured inputs are sealed and applied in input sequence, day-boundary work occurs in its declared class, and other due work is dispatched in deterministic causal order. The decision coordinator consumes signal identity/revision, coalesces redundant reevaluation requests for the same actor at the same instant only where doing so preserves accepted-input semantics, and orders distinct actors by stable `PersonId` plus persisted request sequence. It does not infer hidden truth or create a second availability store.

**Timeline handoff:** transition receipts can be produced while `SimulationTimeline.TryAdvanceTo` is dispatching. The coordinator must not call `ActivityLifecycleStore.TrySchedule` from a timeline owner callback: the promoted `TryCommitOwnerFacts` rejects publication while advancing/dispatching or inside its owner-commit window. Drain and evaluate queued requests only after the outer `TryAdvanceTo` returns successfully, using the timeline and lifecycle store bound by the same composition. At that point the input prefix is sealed through at least the returned `CurrentInstant = t` and may already extend beyond it. Any timed activity selected from that request must therefore use a due instant strictly greater than `max(t, InputsSealedThrough)`; its earliest representable start is one logical tick after that maximum (one millisecond under P18-A), with checked overflow. Never backdate work to `t` or mutate the timeline's sealed prefix. Instantaneous actions may execute at `t` only after advancement returns and through their owning domain's current-truth transaction. Their own disposition does not recursively trigger another decision at `t`; another attempt requires a distinct later logical boundary. If advance fails, queued requests remain pending and are not partially consumed.

At an instant, one decision attempt is permitted for an actor and a given causal decision boundary. A failed/stale proposal does not itself cause an immediate unbounded retry. A subsequent attempt requires a new meaningful boundary: changed availability/condition revision, a new accepted input, a committed action disposition, or a later explicitly scheduled domain due-work boundary. Deduplicating a repeated signal is based on its stable identity/revision, not object identity.

## 4. Decision, execution, and progress

The operation is separated into three steps:

1. **Plan:** read a stable Knowledge-bounded snapshot and disclosed individual availability; enumerate supported actions in stable identity order; calculate utilities and choose at most one proposal. Planning is pure with respect to authoritative world state. Any decision sequence/random consumption is committed as explicit causal decision state, not hidden in a transient provider or runtime object.
2. **Validate and execute:** resolve actor and target identities through current domain authorities, then revalidate current truth, action preconditions, and any relevant commitments at the exact execution instant. Apply effects only through the existing owning domain transaction. A stale or invalid proposal produces the domain's supported rejected/stale disposition and no partial world mutation. No Knowledge evidence is treated as current truth.
3. **Publish disposition:** commit the action decision/disposition identity and resulting domain changes atomically at the owning mutation boundary. Publish any resulting availability/condition signal only after commit. A failed transaction leaves the proposal retryable or explicitly rejected according to the domain's existing semantics, without duplicate effects or consumption of causal IDs.

An instantaneous action that succeeds, fails, or is rejected terminates that decision attempt. The coordinator does not immediately repeat choose/execute at the same actor and same boundary just because the action completed synchronously. A new committed disposition may authorize one subsequent decision in a later causal wave only if it represents a meaningful changed condition and the policy explicitly permits it; each actor/boundary pair is still processed at most once. If zero-duration effects generate another relevant signal, it is sequenced as new causal work under P18-A and cannot recurse. The P18-A `MaxDispatchesPerInstant` limit remains the outer operational bound; it does not replace this actor-level progress rule.

For a timed action, P18-C requests/schedules an activity through the composition-bound P18-B store using stable activity-instance identity, separate from actor identity, subject to the post-advance admissibility rule above. P18-B owns the resulting participant commitment and lifecycle. P18-C waits for relevant start/end/interruption/cancellation/completion receipts to become eligible again; it does not simulate elapsed duration in an actor loop. A shared activity may later have multiple participant relations, each with its own `PersonId` availability/decision state. No P18-C API may encode the activity as an actor-owned child or require exactly one participant.

## 5. Unavailable, dormant, and materialized actors

An actor known to be unavailable due to an active commitment does not receive ordinary autonomous decision requests. The source domain can retain/coalesce a pending reevaluation marker only if the trigger is semantically relevant after the commitment ends; otherwise the committed availability transition itself generates the next request. Decision requests are not silently discarded if a deterministic supported input must remain pending; owner semantics decide whether an input is retained, rejected, or consumed.

Dormant/unloaded actors keep stable identity, Knowledge, decision-relevant state, and commitments in domain-owned stores. They need not have an `NpcRuntime` materialized to preserve eligibility facts or boundary history. When a supported decision actually needs an execution adapter, resolve/materialize by `PersonId` without changing availability, starting an activity, creating a decision, consuming random state, or producing outcomes merely due to loading. The same causal inputs must yield the same decision whether materialization occurred earlier or later. If current action execution requires an unavailable adapter, return a typed owner disposition and wait for the relevant supported boundary; do not invent an alternate dormant simulation rule here.

Dead, deleted, or otherwise ineligible actors are filtered by current authoritative lifecycle/domain facts at decision and execution. This is ordinary domain coherence, not adversarial authorization. The existing trusted local game/UI input contract remains; no player-to-actor grants, anti-cheat, anti-tamper, or forged-command security layer is introduced.

## 6. Atomicity, stale work, and mutation authority

Decision evaluation and candidate ranking are read-only. The committed decision record, consumed decision request, stable decision/random sequence allocation, accepted activity proposal (when present), and publication of any emitted typed due work/signals must share a coherent mutation boundary. Existing consumer execution owns its own outcome transaction; P18-C does not create a generic cross-domain transaction framework. Where selection and execution are separated by scheduled work, persist the proposal and expected source revisions so that it can be revalidated immediately before execution.

Before executing, resolve the current actor, action definition/version, target, and relevant domain revision by stable IDs. If the source changed, treat the proposal as stale and do not apply effects. Stale or duplicate requests cannot consume a newer request with a reused identity; semantic IDs are never reused. For failure before commit, no partial action effect, durable sequence allocation, request consumption, or success signal occurs. An explicit rejected/cancelled disposition may commit atomically when that is the supported domain result. Activity completion alone is not an action effect, and an action result does not independently rewrite P18-B commitments.

Signals are facts about committed source transitions, not direct mutation commands. P18-C may enqueue/consume its own stable request descriptors as coordinator state, but may not become a second authority for activity lifecycle, individual availability, Knowledge, or consumer outcomes. The additive transition-receipt seam is stored by the lifecycle owner in the same commit as the transition; its unread cursor/request queue is coordinator state, not a second lifecycle store. Queue/index nodes are rebuildable projections under P18-A; owner records decide current validity.

## 7. Reconstruction inventory

Any future save/fork/reconstruction claim that includes P18-C must recover or deterministically rebuild:

- P18-A logical instant, calendar/profile version, sealed input boundary and accepted input sequence, due-work causal wave/order, and dispatch configuration;
- stable actor `PersonId`, lifecycle and decision-relevant authoritative state; Knowledge facts/sources/versions and visibility policy needed by candidate selection;
- supported action/provider semantic IDs and compatible versions, stable candidate ordering, configuration/content and effective utility inputs;
- current P18-B activity-instance IDs/revisions/states, participant relations, individual commitments/availability, and the source revisions that trigger reevaluation;
- pending decision requests, triggering signal/input identity and sequence, deduplication/coalescing state, actor decision sequence, and any separated action proposal with expected revisions and stable targets/parameters;
- deterministic random state/context actually consumed by a decision or action, plus any domain-owned outcome idempotency identity;
- materialization-independent state necessary to resolve actors/targets and reproduce eligibility, without relying on `NpcRuntime`, Unity asset discovery, delegates, or object references.

Events/history/diagnostics may explain decisions and transitions after commit, but are not the primary state needed to reproduce them. Queue indices may be rebuilt only when their complete order is derivable from the listed facts. This is a causal inventory, not a serialization schema, replay implementation, or save integration.

## 8. Validation obligations and hotspots

Before implementation review, validate at least:

- Knowledge-bounded planning followed by current-truth execution revalidation, including stale target and changed availability/condition cases;
- no decision while committed unavailable, and exactly one eligible reevaluation at relevant completion/cancel/interruption/condition/input boundaries;
- same logical instant input/due-work ordering, stable per-actor request order, deduplication, and deterministic results across provider/registration/materialization permutations;
- successful, failed, rejected, and stale instantaneous action termination without infinite same-instant choose/execute loops or random/sequence drift;
- timed proposal handoff to P18-B with activity-instance identity separate from `PersonId`, one-participant proof without one-to-one cardinality, and a P20-shaped instance relation that remains valid without implementing formation/roles/coordination;
- dormant/unloaded versus materialized equivalence, including no side effects from materialization and stable identity through dormancy/death semantics;
- planning/query purity, atomic failure/no partial mutation, retry/idempotency behavior, and stable causal reconstruction inputs;
- no per-frame polling, blanket per-actor scan, permanent one-action-per-day law, consumer gameplay, P8 travel migration, P11 SellGoods integration, P19 loader, or persistence implementation.

Hotspots include `NpcDecisionSystem`, action/provider interfaces and adapters, `SimulationRuntime` daily action orchestration, command capture, Knowledge lookup, actor lifecycle/materialization, P18-B availability notifications, and P18-A due-work/input ordering. Assign exclusive ownership for the actor decision/execution boundary and serially integrate any `SimulationRuntime` change. No broad rewrite of the daily simulation loop is implied; P18-D selects and validates concrete compatibility/migrations.

## 9. Dependencies and explicit exclusions

P18-C consumes the promoted P18-A/B contracts. P18-A/B are now canonical; the existing B transition signal/history gap and timeline handoff limits are addressed here as explicit additive implementation requirements, not presumed existing APIs. **P18-C implementation remains blocked until this refreshed exact design passes independent review and its bounded checkpoint/shared-hotspot ownership is recorded.** No P18-D consumer is needed to implement this seam, and this proposal does not authorize a P18-D migration.

No P11 SellGoods integration, P8 travel migration, sleep/dreams/needs/jobs/theft/robbery/gangs/rituals/War/MegaEventos, participant formation or solver, public extension API/loader, save/replay, generic security boundary, or universal actor decision framework is included. P19 mechanics remain deferred; current extensibility constraints still apply to semantic identity, stable ordering, and avoiding speculative Unity-only authority. No unresolved product or architectural decision is identified by this proposal; exact APIs and implementation sequencing remain reviewable technical details within these contracts.

**Prior review:** PASS on `2dd4941cc26271b5faddd5ca7bb5db5883063cfc` before P18-B promotion. Current-base revalidation is pending; this document records no capability promotion.
