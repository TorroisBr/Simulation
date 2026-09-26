# P18-A — Logical Timeline and Due-work Scheduler Technical Design

**Design base:** `c285466c355103d3637ac165246591b72eb7bda0` (`codex/phase8/canonical`)
**Authority:** `docs/SIMULATION_ARCHITECTURE.md` §§11–12, 91–92; `docs/ROADMAP.md`; `docs/EXECUTION_MODEL.md`; `docs/phases/PHASE18_BRIEF.md`.
**Checkpoint:** P18-A — Logical Timeline and Due-work Scheduler.
**Scope:** Proposed technical design only. No executable code, gameplay, architecture, Brief, Roadmap, State, or test changes.
**Status:** The technical design at `6b1edf21128eaa874ecca1b96674287d3c6d1d80` passed review, but its immediate integration of the complete legacy daily pass exceeded the bounded A/D dependency split. This revision narrows that integration boundary and requires independent re-review before it can guide implementation. No implementation authorization or capability promotion is implied.

## 1. Outcome and boundary

P18-A supplies a monotonic logical timeline, a pure mapping from that timeline to the existing date-only calendar, deterministic dispatch of sealed inputs and due work, and read-only timeline queries. It does not own activity lifecycle, actor availability, decisions, domain effects, travel progress, calendar occurrences, or a universal recurring-process model. A calendar occurrence only makes a domain operation due; its owning domain validates and applies any effect.

The scheduler coordinates work through typed, data-only due-work references. Owning domains retain their authoritative pending facts; captured external commands remain owned by the command/input boundary. A scheduler priority queue is a rebuildable index over those facts, never a second copy of their authority. No opaque callback, delegate, scene object, `NpcRuntime` reference, or runtime registration order may be the only representation of causal work.

This proposal is limited to P18-A. P18-B owns activity state and lifecycle; P18-C owns availability-driven decisions and instantaneous-action progress rules; P18-D owns selected consumer migrations. Their contracts may consume A after review, but this design does not implement those consumers. Until a selected consumer is migrated, the existing daily runtime remains the sole time authority for its legacy profile. P18-A does not compose a second active timeline beside that runtime; each supported world profile has one active time/advance authority.

## 2. Logical instant and calendar projection

Represent an instant as a nonnegative signed 64-bit integer `LogicalTick`, with exactly **86,400,000 ticks per logical calendar day** (one millisecond-sized logical quantum). The unit is a simulation convention; it is not elapsed host time, wall-clock time, frame time, or a floating-point duration. Durations are nonnegative integer tick counts. Sub-tick requests are invalid; conversions from authored values must be explicit and checked.

The supported timeline is `[0, long.MaxValue]`. Addition, subtraction, date projection, and conversion use checked arithmetic. An operation beyond the range returns a typed overflow/range failure and performs no partial clock or agenda mutation. Negative instants/durations, NaN/infinity inputs, and silent saturation/wraparound are invalid. The maximum representable day is `long.MaxValue / 86,400,000`; the remaining ticks form that day's valid time-of-day. This deliberately bounds P18-A independently of the date type's wider possible year range.

For an instant `t`, `absoluteDay = t / TicksPerDay` and `tickOfDay = t % TicksPerDay`. Convert `absoluteDay` using the composed run's immutable `SimulationCalendar.GetDate(absoluteDay)`; the calendar remains the authority for year/month/week/day fields. `tickOfDay` is a time-of-day projection only. Calendar definitions do not alter tick scale, and changing the effective calendar during a run is unsupported. A date-only query at an intraday instant returns that instant's containing date. The next day boundary is the checked smallest multiple of `TicksPerDay` strictly greater than `t`; it is not inferred from host time or a fixed Gregorian date.

The runtime's initial logical instant is supplied explicitly at composition and must map through the effective calendar. No implicit reset to zero during materialization, clone, or query is allowed. The chosen millisecond quantum is a proposed contract for review; changing it later is a compatibility change because scheduled instants and reconstructed ordering depend on it.

## 3. Pending work facts and queue ownership

Every dispatchable item has a stable typed `DueWorkId`, owner/domain identity, due `LogicalTick`, and owner-defined payload/version sufficient to resolve and validate the requested operation. IDs are semantic and stable across reconstruction; runtime hash codes, collection positions, and object identity are not keys. One-shot work is consumed only by successful owner dispatch. Recurrences remain owner facts that create a new occurrence after successful processing; the scheduler does not invent recurrence policy.

| Concern | Proposed authority |
|---|---|
| Current logical instant and monotonic advance | World timeline state |
| Domain due time, operation identity, lifecycle/revision, recurrence rule | Owning domain's authoritative store |
| Captured external command, target instant and accepted input sequence | Existing command/input authority |
| Ordered priority queue and due-time lookup | Scheduler-derived index, rebuildable from the above |
| Domain validation and mutation | Existing owning domain/application transaction |
| Calendar fields and date conversion | Effective immutable `SimulationCalendar` |

The scheduler stores references/keys plus the queue ordering data needed to dispatch. It may cache resolved entries, but every dispatch resolves the current owner record and verifies its identity/revision before execution. A cache rebuild must produce the same ordered sequence from the same authoritative inputs. A domain may not keep a second scheduler queue that independently decides whether its operation is pending.

Due work is represented by closed, typed internal descriptors for currently integrated operations, not arbitrary executable payloads. P18-A may define the minimal descriptor/dispatcher seam but does not create a public extension registry, mod loader, universal job/activity superclass, or gameplay operation. Unknown descriptor kind/version is a deterministic validation failure; it is not silently dropped.

## 4. Ordering, dispatch, and reentrancy

The ordering contract is lexicographic and independent of insertion timing, hash iteration, machine, and host clock:

1. Earlier `LogicalTick` first.
2. At one tick, captured external inputs are applied in their persisted accepted input-sequence order. The input authority must reject duplicate/colliding sequence identities. Inputs for an instant must be sealed before that instant's dispatch begins; a late input targeting an already-settled instant is rejected and cannot rewrite prior causality.
3. At an instant that is a calendar-day boundary, the typed day-boundary owner operation is an explicit first due-work class: after all sealed accepted inputs at that instant and before ordinary due-work items at that instant. It runs at most once for each boundary crossed by an advance. This ordering gives boundary inputs effect before that selected owner's boundary operation and ensures ordinary work due at the boundary observes the owner's committed boundary effects. Away from a day boundary there is no such class.
4. Within each due-work class, items are ordered by causal wave, stable owner/domain ID, stable `DueWorkId`, then the owner's persisted occurrence/generation sequence. Initial ordinary work at that instant is wave zero. The boundary operation itself is the sole boundary-class wave-zero operation. Every component is part of the recoverable causal state.
5. Work created by a dispatched item for the current instant is eligible only after the current item commits. It receives a world-persisted causal sequence and enters the next causal wave, after all work in the current wave and after the boundary operation if that operation has already run. It never preempts the currently executing transaction. Work scheduled before `now` is rejected. Work emitted by the daily operation follows the same rule and is therefore processed after that operation, before advance leaves the boundary.

Dispatch is non-reentrant: an owner handler cannot recursively advance or drain the timeline. It returns a result and any newly scheduled typed facts to the scheduler boundary. For a successful one-shot dispatch that publishes due facts, validation, owner mutation, consumption of the current one-shot fact, allocation of causal sequence values, and publication of all returned owner due facts form one atomic owner mutation transaction. Prepare and validate the complete write set first; sequence allocation is provisional until that write set commits. On validation or commit failure, discard provisional sequence values and publish/consume/mutate nothing, so retry observes the same next sequence and no sequence drift. If an owner intentionally records an explicit rejected/cancelled disposition, that disposition and current-item consumption commit atomically as the operation's result. A handler that produces no new facts still commits its one-shot consumption in the same transaction. The scheduler does not report success or consume work on the owner's behalf. This is a required seam for selected owners, not a new generic cross-domain transaction framework; implementations without an atomic owner transaction must stage their mutation so the complete write set is committed by the existing world mutation boundary.

### Typed day-boundary owner seam

A crossed day boundary is represented by a stable typed operation identity containing the world/profile identity and absolute day boundary. It is resolved through a typed boundary-owner contract, never an arbitrary delegate that is the only record of causal work. The owner atomically commits its boundary effects together with the operation's consumed/committed identity. The timeline advances past the boundary only after the owner reports that commit; if dispatch fails, retry resolves the same operation identity, and an already committed identity cannot apply its effects again. Pending/committed boundary status is authoritative owner state or a deterministic projection of it, not a second independently mutable scheduler fact.

P18-A proves this seam with a bounded synthetic owner whose state and boundary effects share one atomic transaction. It does not bind the existing `SimulationRuntime.AdvanceDayAfterClockAdvance` pass to this seam: that pass mutates many independent authorities sequentially, and current mutation guards do not supply a world-wide rollback or prepared write-set. P18-D must define the daily-effect inventory of each supported runtime profile. Every existing daily effect that remains in an intraday profile must be represented by a typed owner operation with a reviewed mutation/idempotency boundary and explicit order; effects cannot be silently dropped or double-run. A profile that retains the complete legacy daily pass uses only the legacy daily authority and does not compose the timeline. A narrower profile that excludes effects must name those exclusions and obtain the product approval required for changing supported behavior before it is treated as a supported profile. P18-A neither adds a world-wide transaction framework nor weakens retry safety for owners that it does admit.

To bound same-instant work and guarantee zero-duration/reentrant chains terminate, a single instant drain has a deterministic, effective-configuration limit `MaxDispatchesPerInstant` (positive integer, proposed default 100,000). Every committed dispatch consumes one unit, including generated work. Reaching the limit stops the advance with a typed `InstantWorkLimitExceeded` at the same logical instant and leaves undispatched work queued in canonical order; it never skips, reorders, or silently completes remaining work. The limit is configuration input and therefore reconstruction-sensitive. The limit is a fail-closed operational guard, not a claim that P18-A owns action-loop semantics. P18-C must separately ensure instantaneous actor actions make progress and do not repeatedly decide without a meaningful state/availability/input boundary.

An item is successfully completed once only when its owner transaction commits its completion/consumption transition. Retrying after an uncertain dispatch must first resolve the current owner record/revision; already-consumed or superseded occurrences are not dispatched a second time. Diagnostics may report retries and rejected stale items but are not the idempotency authority.

## 5. Staleness, cancellation, and pure queries

Cancellation, replacement, and lifecycle transitions are facts of the owning domain. They invalidate the matching stable work identity/revision. The queue can lazily retain an old index node, but resolution discards it as stale without running its payload or consuming a newer occurrence with a reused ID. IDs are not reused; a replacement receives a new identity or owner-defined generation. Scheduler removal is index maintenance, not the cancellation mutation.

Stale status is resolved immediately before dispatch against the owner record, not trusted from a cached queue item. A rejected due operation does not get converted to success, retried at an invented later instant, or used to infer a domain outcome. Retry/reschedule policy belongs to its owner. Atomicity of cross-domain effects is not generalized by P18-A; a selected consumer must supply its existing reviewed transaction seam.

Queries such as `CurrentInstant`, `GetCalendarProjection(instant)`, `NextDueInstant`, `IsDue(instant, workId)`, and ordered read-only due-work previews are pure. They take an explicit instant/snapshot where needed, expose only permitted public descriptors, and cannot drain, register, refresh stale work by mutation, or alter revisions. Query output is deterministically ordered. Previewing future work does not promise that it will still be valid at execution.

## 6. Advancing and `AdvanceDay` compatibility

One central `AdvanceTo(target)` operation is the only clock-advance authority for a profile using the timeline. It rejects a target earlier than `now`; an equal target may perform an explicitly requested drain of work due now but cannot move time backward. For a later target, it walks chronologically through the minimum of the next sealed input instant, ordinary due-work instant, next day boundary strictly after the starting/current instant, and target. At each such instant it moves `now` there, applies sealed inputs, runs the typed boundary-owner operation when this is a newly crossed day boundary, drains ordinary due work and generated same-instant waves under §4, then continues. Thus a direct timeline call crossing several days invokes the synthetic/selected atomic boundary owner once at every intervening boundary, in order, with ordinary work and inputs processed at their exact instants; it does not jump to the target and replay missed daily work. A boundary equal to the call's starting `now` is not crossed and is not run again.

The daily boundary operation is represented by a typed, idempotently identified owner operation for that boundary and profile. Its occurrence identity is derived from stable world/profile identity plus the absolute day boundary; the owner commits the operation identity with its effects atomically. This prevents duplicate execution when `AdvanceTo` is retried at that instant. A failed advance remains at the failing instant; retry resumes pending work there without rerunning already committed owner operations. It sets `now = target` only after all eligible work through the target has completed. Failures leave `now` at the failing logical instant and preserve remaining pending work; no failure is disguised as a successful partial advance.

P18-A designs compatibility with the existing `SimulationRuntime.AdvanceDay` profile but does not replace or wrap its complete legacy pass. That profile retains its existing single daily advance authority and behavior until P18-D defines and validates an intraday profile. P18-A validation proves timeline boundary ordering and retry semantics with the synthetic atomic owner and verifies that the legacy daily path has not acquired a second active clock. Before P18-D replaces the legacy advance authority for a profile, it must inventory every daily effect that remains in that supported composition and route it through reviewed typed owner operations satisfying §4 and the profile order. An effect left outside that inventory cannot simply stop running. If the complete legacy pass remains the supported behavior, that profile continues to use the legacy authority and is not composed with the timeline. No profile runs the old daily pass and then replays the same effects through the timeline.

Repeated `AdvanceDay` calls advance one boundary at a time. A target computed from an input date must be validated against the current instant and effective calendar. There is no implicit “catch up all past daily ticks” when the timeline starts, loads, or changes profile: only boundaries strictly after the current instant and at or before the requested target are invoked. Old daily histories retain their historical interpretation; P18 does not rewrite them as intraday executions.

## 7. Reconstruction inputs and causal state

The P18-A reconstruction inventory is:

- logical instant, tick quantum/version, effective immutable calendar identity and definition/version;
- `MaxDispatchesPerInstant` and other effective settings that change dispatch acceptance;
- stable pending due-work facts, their owner IDs/revisions, occurrence/generation identities, due instants, and recurrence inputs where owned;
- captured external inputs with target instants, accepted sequence identities/order, and the sealed boundary state;
- the causal sequence allocator state used for same-instant scheduling;
- domain state needed to resolve each descriptor and validate/consume it, plus compatible simulation/content versions;
- deterministic random state/context only when a dispatched owner operation consumes it.

The scheduler index itself may be omitted if it is deterministically rebuilt from those facts. If any queue field affects order and is not derivable, it is causal state and must be preserved. Host references/delegates, transient Unity objects, and registration order cannot substitute for stable IDs and inputs. This is an inventory contract only; save/load format, replay engine, history retention, and persistence implementation remain later work under the P12/P13 edges.

For P20 compatibility, the scheduled subject is a stable **activity-instance** identity or another domain-owned due-work identity, never an implicit single actor identity. An activity instance may later relate to zero, one, or several participants while each participant's identity, availability, and commitment remain domain facts. The due-work descriptor must permit that relation to be resolved by its owner without encoding a one-instance-to-one-`PersonId` cardinality. P18-A adds no participant formation, role/count model, reservation solver, shared execution, or participant decision loop.

## 8. Validation plan, hotspots, and readiness

Before implementation review, the candidate should validate at least:

- exact tick/date/time-of-day projection at zero, day boundaries, custom month/year boundaries, the maximum day, and checked overflow;
- monotonic advance, backward-target rejection, equal-target behavior, due-work chronological processing, and failure position/pending-work retention;
- stable same-instant input and due-work ordering across registration permutations and reconstructed queue indexes;
- same-instant scheduling-after-current-item, non-reentrant advance rejection, cancellation/replacement, stale revisions, duplicate IDs/sequences, and exactly-once completion under retry;
- the deterministic dispatch limit, preservation of unprocessed work, and no silent advancement after failure;
- pure queries leave world, owner, scheduler, and diagnostics revisions unchanged;
- the synthetic atomic boundary owner commits once per crossed day, retries at the same instant without repeating committed effects, and preserves pending ordinary work on failure;
- the existing legacy daily runtime remains unchanged and does not compose a second active timeline; later P18-D integration replaces that profile's authority rather than running both;
- P18-D's eventual profile inventory accounts for every daily effect retained in that supported composition, with no silent omissions, duplicate legacy execution, or unreviewed exclusions;
- reconstruction of all ordering inputs from §7, including causal allocator and external input sequence state;
- P20-shaped descriptors can name one stable instance independently of participant count, with no participant solver or per-actor scheduler identity.

Primary implementation hotspots are calendar conversion and timeline state, typed input sealing/dispatch, due-work owner stores, and diagnostics/canonical snapshots when causal state is introduced. P18-A owns no `SimulationRuntime`/legacy `AdvanceDay` migration; reserve that shared daily-loop hotspot for the reviewed P18-D consumer integration. Assign one writer to timeline/input scheduling; other owner adapters must use the reviewed descriptor contract and isolated worktrees. Keep architecture and this design separate from execution changes. No edits to daily domain systems are implied beyond the explicitly selected later P18-D integrations.

P18-A depends on the existing canonical calendar, determinism, and domain mutation/input contracts. It does not acquire blanket prerequisites on P9, P14, or P17; it does not wait for P20. Its reviewed contracts may support P18-B/C technical design and P20 entry/design. Their implementation/integration still waits for promoted upstream capabilities they actually consume. P18-D remains downstream of A/B/C and only the concrete consumer capabilities selected in its reviewed scope. No P12/P13 persistence implementation, P19 loader, gameplay feature, or broad daily-domain migration is authorized here.

**Previous review status:** PASS on `6b1edf21128eaa874ecca1b96674287d3c6d1d80` for the earlier design text. The revised P18-A/P18-D daily integration sequence passed independent review at content commit `aca9cb2d1853fd7bccfd95b6f2229b9d4e8a40c1`. The review approves the bounded design only; do not mint additional checkpoint IDs or infer capability promotion from this document.
