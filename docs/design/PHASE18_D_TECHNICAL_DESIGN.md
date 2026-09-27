# P18-D — Intraday SellGoods Actor Choice and Daily Compatibility Design

**Candidate base:** refreshed P18-C design commit `39bd42e9e78f80c1e40b35b099e980ee8bc44a43` (`codex/phase18/P18CAvailabilityDecisionDesign`). P18-A/B are promoted at P18 canonical `3d4fe829f4be41fc9e9bb11052a320c3eb00d94d`; architecture/alignment baseline `c285466c355103d3637ac165246591b72eb7bda0`. P11 Actor Choice code originated at `0cd4281` and its closure review is `308e24d`.

**Status:** technical design candidate; implementation is gated on P18-C promotion and a serialized `SimulationRuntime.cs` ownership window after P14 integration/promotion. This document does not promote a capability or change Phase State/Brief.

**Authority:** `docs/SIMULATION_ARCHITECTURE.md` §§2, 11–12, 91–92; `docs/ROADMAP.md`; `docs/EXECUTION_MODEL.md`; `docs/PHASE18_STATE.md`; `docs/phases/PHASE18_BRIEF.md`; `docs/phases/PHASE11_BRIEF.md`; `docs/design/PHASE18_A_TECHNICAL_DESIGN.md`; `docs/design/PHASE18_B_TECHNICAL_DESIGN.md`; `docs/design/PHASE18_C_TECHNICAL_DESIGN.md`; `docs/architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md`; `docs/architecture/MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md`.

## 1. Purpose and bounded consumer

Integrate the existing trusted local P11 actor-choice input for the bounded `SellGoods` action with P18's logical timeline and P18-C's post-advance decision boundary. In an intraday profile, an input is retained and attempted at its exact logical instant in causal input order. The actor's stable identity is `PersonId`; an activity instance, if a future action ever needs one, remains a separate identity. This action remains instantaneous and uses the current SellGoods provider and economy transaction path.

`SimulationRuntime.AdvanceDay` / `TryAdvanceDay` become advance-to-next-day-boundary adapters only when the runtime uses the intraday profile. The adapter advances chronologically through due work and day boundaries; it does not jump the clock and catch up retroactively. Explicit legacy daily-profile behavior remains available and retains its existing day-turn ordering and historical input semantics.

This is the narrowest P18-D consumer because P11's trusted typed input and SellGoods domain path are already promoted. P8-E travel is optional and separate; this design neither migrates travel nor makes P8-E a P18-D-wide dependency. P8-C local position eligibility already used by SellGoods remains current-truth input to the existing action path.

## 2. Existing contract and adaptation

P11's `ActorChoiceStore` retains a stable input ID, originating WorldCommand ID, monotonic input sequence, `PersonId`, action definition ID, command origin/authority, captured day, pending/terminal state, and ordered dispositions. A pending choice replaces that actor's autonomous choice slot. Current P11 runtime validates the actor, local position/city and SellGoods eligibility, constructs the requested action, records an ActorChoice decision, executes through the ordinary action provider, and records returned/thrown terminal attempt status. A failed/rejected/throwing actor choice has no same-turn autonomous fallback. These lifecycle and diagnostic properties remain in force.

The P18-D adaptation adds an optional exact target `LogicalTick` (or equivalent immutable logical instant) to intraday-retained commands and the timeline input envelope. Daily-profile records continue to use their existing captured day and roster ordinal; they are not backfilled with a fabricated tick. Intraday capture retains:

- stable command/input ID and originating command ID;
- stable actor `PersonId`;
- semantic action definition ID/version and typed SellGoods payload (item definition ID, requested amount, and any already-supported target choice encoded as IDs/values; no `NpcRuntime`, `CityRuntime`, `MarketRuntime`, or `ScriptableObject` reference is causal authority);
- trusted origin and authority values under the existing P11 local-input contract;
- exact target `LogicalTick`, input sequence, and accepted/pending/dispatched/terminal disposition history.

The accepted command's payload is immutable after capture. Timeline input ordering uses the P18-A sequence at a given instant. The actor-choice store remains the retained command/disposition owner; any timeline envelope or indexed input reference is a deterministic dispatch projection tied to that retained ID. Capture/retention and publication to the timeline must either commit together through a narrow owner operation or leave a recoverable pending record that can be indexed exactly once; never acknowledge acceptance while silently losing the command from both owner state and timeline agenda. Duplicate command IDs remain rejected using P11 semantics.

For compatibility, legacy P11 records continue to mean “apply at the actor's ordinary daily slot,” with captured-day/roster disposition history preserved. An intraday command must provide its exact logical instant; date-only capture is not silently assigned an arbitrary time. If a legacy pending record is explicitly carried into an intraday profile, an adapter must define a one-time migration boundary (the next eligible day-boundary decision slot) and preserve its original capture record; it must not invent a historical tick. This migration is not required to claim ordinary operation in either profile.

## 3. Chronological ordering and C handoff

Use the same `SimulationTimeline` composed with `ActivityLifecycleStore`; do not add a second scheduler. The existing P18-A order at one logical instant is authoritative:

1. accepted external inputs at the instant, in stable input sequence;
2. the idempotent daily-boundary operation, if this instant is a day boundary;
3. ordinary due work, in P18-A causal ordering/waves, including activity starts/completions;
4. after the outer `TryAdvanceTo` succeeds, the P18-C coordinator drains committed receipts and decision requests, in stable actor `PersonId` and request-sequence order.

An input owner's commit records the accepted command boundary and enqueues a typed P18-C request/receipt; it does not call actor decision, schedule an activity, or execute SellGoods from inside `TryAdvanceTo`. Timeline owner callbacks cannot reenter timeline publication, and P18-C design explicitly requires its coordinator to run after the outer advance returns. At the post-advance handoff, the coordinator consumes the input trigger in sequence. For the addressed actor, a pending trusted ActorChoice takes precedence over autonomous choice, and it consumes only one eligible command according to P11 one-shot semantics. Multiple accepted commands remain ordered and individually retained; a terminal outcome is not collapsed into a later command.

If an input targets an instant before the requested advance target, it is dispatched at its own instant and its decision attempt occurs at that instant's completed causal boundary before advancing further. This requires a timeline advance driver that returns/yields at each next causal boundary for the C handoff, or an equivalent bounded post-boundary callback outside owner dispatch. It must not let `TryAdvanceTo(target)` run past a command decision boundary and then execute that command retroactively. The adapter's advancement loop is therefore: find/advance to next timeline boundary, run C handoff, continue toward requested target; inputs and due work at a timestamp are drained before its handoff. At a timestamp with several inputs, their retained request sequence is honored and the P11 actor-choice no-fallback rule is preserved per attempt. A command scheduled for a time already sealed/past is rejected at capture with a typed stale-boundary disposition; it is not moved forward silently.

P18-C's post-advance scheduling rule continues to apply: work selected at `t` may not be backdated or published into the sealed prefix. A timed proposal's earliest start is strictly greater than `max(CurrentInstant, InputsSealedThrough)`. SellGoods remains immediate; its attempt executes after the handoff at `t`, under current domain validation. A failed timeline advance does not consume its not-yet-handed-off command request; committed inputs and due-work transitions remain recoverable, with timeline idempotency/retry semantics applied by their owner.

## 4. Day-boundary adapter and exact profile compatibility

Profile selection is explicit composition/configuration, not inferred from whether a timeline happens to exist.

**Legacy daily profile:** `AdvanceDay` retains the current `TryAdvanceDay` behavior and complete daily pass: advance `SimulationTime` by one day, run daily demography and `BeginSimulationDay`, prepare directives, economy production/consumption/prices, local knowledge sharing, expedition daily work, existing actor roster order (directive handling, actor choice, autonomous decision/action), then travel progression/arrival reconciliation. Keep existing day and roster ordinal disposition semantics. No P18 timeline boundary also invokes that legacy pass. Existing saved daily worlds and test fixtures stay on this profile unless explicitly migrated.

**Intraday profile:** `AdvanceDay` computes the next absolute day boundary from the current `SimulationTimeline.CurrentInstant` using checked calendar/tick arithmetic and calls the same chronological advance driver as any other request. At each crossed boundary, one idempotent boundary owner runs the daily domain operations once for `(worldId, profileId, absoluteDay)` in their established dependency order. The pass includes daily demographic/consequence timers, daily economy cadences, knowledge refresh/sharing and other already-owned daily processes. It emits any typed availability/condition receipts needed by P18-C after those owner mutations commit. Actor decision/action dispatch is excluded from this pass and is performed only by the post-advance P18-C handoff. The legacy roster loop, including its autonomous actor choices and `TryExecuteCurrentAction`, is not called in this profile. Thus each daily cadence runs once, while actor decisions run only at meaningful P18-C boundaries; a day boundary is one such boundary where applicable, not an additional hidden daily actor loop.

Travel, including P8-E, remains under its existing explicit owner/API and is not pulled into the intraday day pass by this adapter. Any later travel migration must separately define exact timing and ordering. P14 material-flow daily operations remain within P14's normal domain owner and serialized integration; P18-D does not take ownership of or reinterpret them.

Failure is surfaced through the existing `TryAdvanceDay` failure shape (extended only with specific timeline failure mapping where required). The adapter does not partially advance `SimulationTime` separately from the timeline; `CurrentDay`/calendar projection in intraday mode derives from the timeline. Calling `AdvanceDay` at a day boundary targets the next boundary, not the current one, preventing a duplicate pass. Repeated boundary dispatch is idempotent by the P18-A boundary identity. Calling it multiple times advances one full day each call, even if an earlier `AdvanceTo` ended mid-day.

## 5. Current-truth execution, outcome, and failure atomicity

P11's decision proposal may use only the actor's permitted Knowledge and typed input. The execution adapter resolves the current materialized actor by `PersonId`, current action definition/version, item, location/city/market and applicable target through current owners at the requested logical instant. It rechecks alive/eligible state, local position and city-location consistency, enabled SellGoods configuration, merchant-plan restrictions, inventory/item availability and all existing provider/domain preconditions. Stale known facts or command payload do not authorize a sale.

Build an ephemeral runtime action from stable IDs/values only after validation; pass it to the current `MerchantSystem` and `EconomyTransactionService` execution path. Transaction owners remain responsible for atomic inventory, money, market/merchant, plan and history effects under their current contracts. P18-D adds no parallel sale mutation. A rejection or failed transaction commits the supported ActorChoice terminal disposition and no sale effects; a thrown attempt records the existing thrown terminal state and cannot fall through to autonomous action. The input dispatch-start marker, decision record, action result, terminal disposition and authoritative sale must not create a state where a retry can apply a second sale. Where existing sale outcome and ActorChoice disposition cannot share one transaction, use stable command/input idempotency at the sale boundary or a recoverable “attempt started/terminal pending” protocol before claiming exactly-once effects; do not add a generic cross-domain transaction framework. The implementation must demonstrate which existing sale transaction receipt/history field can carry this idempotency correlation, or flag a focused architecture review if none can.

World truth mutation stays with existing economy/merchant authorities. P18-C decides when a retained actor request reaches the consumer, not whether its stale proposal succeeds. No input can force a market outcome or bypass present eligibility.

## 6. Reconstruction and diagnostics inventory

Any later continuation/fork claim covering this path must recover or deterministically rebuild:

- profile ID, `SimulationTimeline.CurrentInstant`, calendar/config version, causal sequence, sealed input prefix, pending boundary identity/day and deterministic agenda;
- retained ActorChoice command ID, input ID, actor `PersonId`, semantic action/version, full typed SellGoods payload, origin/authority, exact logical instant, sequence, lifecycle/dispositions, and any pending post-advance C request/cursor;
- P18-C decision request/receipt identity, stable actor/request ordering state, one-attempt-per-boundary identity and decision/random context;
- P18-B activity IDs, participant commitments, lifecycle transition receipts and pending due-work identity needed for actor availability, without treating receipt history as current availability truth;
- day-boundary idempotency state, P14/P8/E or economy owner state only to the extent their own capabilities are in the selected world, and action transaction correlation needed to prevent duplicate sale effects;
- materialization-independent `PersonId` ownership and stable action/item semantic identity/version.

Derived indexes, timeline agenda entries, and runtime object references are rebuildable and cannot be the sole record of accepted causality. Diagnostics and invariant snapshots must show retained input state alongside timeline reference/handoff state and must detect orphaned inputs, duplicate dispatch, nonmonotonic sequence, impossible terminal/dispatched transitions, wrong-profile ticks, duplicate daily boundary execution, and sale correlation without matching terminal disposition.

No P19 loader/API, durable save implementation, replay system, travel migration, or universal daily-domain conversion is delivered by this design.

## 7. Focused validation plan

Required implementation suites should cover:

- exact-tick capture and stable sequence ordering for two inputs at one instant; a command between due-work times is attempted at its own timestamp, not at the final target;
- same-instant order: input commit/retention, one day-boundary pass, due work/start/completion, then P18-C post-advance handoff; receipts produced during timeline dispatch are not handled reentrantly;
- P18-C actor request coalescing/ordering, ActorChoice precedence, sequential retained commands, one-shot terminal behavior and no autonomous fallback after rejection, provider failure, or throw;
- stale/past/sealed input rejection, actor unavailable/dead/dormant or missing materialization disposition, stale action/item/configuration, moved location, unavailable goods, and current market/merchant plan constraints with no unauthorized or partial sale;
- economy transaction atomicity/idempotency and retry after a failure between dispatch start and terminal recording;
- repeated `AdvanceDay` in each profile, mid-day-to-next-boundary advance, multiple crossed boundaries, same-boundary idempotency, overflow/fault behavior, and proof that intraday actor action runs once through P18-C while each daily cadence runs once;
- daily-profile golden compatibility for existing actor-choice roster ordinal, disposition sequence, action ordering, daily system order and travel progression; no synthesized logical tick or changed history;
- runtime composition isolation, repeated equivalent run determinism, and current diagnostics/invariant parity.

Because this changes the daily loop and chronological behavior, run targeted P18 timeline/lifecycle/decision tests, P11 ActorChoice and ActorActionChoice command regressions, affected P14/economy and existing daily regression suites, ALL EditMode, complete official Smoke, and a justified long-run suite. Run `git diff --check` on the integration candidate. Promotion also needs independent review against the exact P18-C/P11/P14 base composition.

## 8. Hotspots, ownership, and integration order

`SimulationRuntime.cs` is currently owned by active P14-A implementation. P18-D must not edit it concurrently or use a shared checkout as a workaround. After P14 integration and promotion, establish a serialized `SimulationRuntime.cs` ownership window for the P18-D adapter. The implementation branch also coordinates on `ActorChoiceStore`/contracts and ActorChoice diagnostics, `LogicalTimeline`/input owner composition, P18-C decision coordinator and `MerchantSystem`/economy transaction interfaces. Assign one writer per hotspot; keep nonoverlapping diagnostics/test work isolated only where it does not change shared contracts.

Recommended sequence after prerequisites:

1. Rebase/refresh this design candidate against promoted P18-C, P14 canonical integration, and current P11 canonical; verify no relevant contract drift.
2. Review the command-to-timeline atomic retention seam and the SellGoods transaction idempotency/correlation field. If the existing owner path cannot guarantee retry safety, resolve that narrow architecture question before coding.
3. Implement the intraday input envelope/retention projection and boundary-yielding advance driver against the promoted P18-A/C APIs; keep the existing P11 store as command/disposition owner.
4. Implement profile-selected `AdvanceDay` adapter and split day-boundary cadence from actor decision traversal, with exact legacy daily path preserved.
5. Connect the post-advance coordinator to the existing SellGoods action provider and current economy authority; add diagnostics/invariants and focused tests.
6. Run independent architecture/diff review, required focused and full gates, then integrate serially. Only after validated promotion may the Phase 18 State record P18-D readiness/delivery changes; this design branch intentionally edits no State or Brief.

Excluded: broader SellGoods gameplay, new security/grant or anti-cheat mechanisms, P14 material-flow ownership, Sleep/dreams/needs/jobs/theft/war/rituals, P8-E travel integration, P19 loading/API, save/replay, and universal daily-domain rewrite. P20 is not a prerequisite for this one-actor command; no shared activity behavior is introduced.
