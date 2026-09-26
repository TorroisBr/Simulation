# P18-B — Activity Lifecycle Technical Design

**Design base:** `1ac0673d149d684d207c4231d1a91d391fa75880` (`codex/phase18/P18ALogicalTimelineDesign`), descended from canonical architecture `c285466c355103d3637ac165246591b72eb7bda0`.
**Authority:** `docs/SIMULATION_ARCHITECTURE.md` §§11–12, 91–92; `docs/ROADMAP.md`; `docs/EXECUTION_MODEL.md`; `docs/phases/PHASE18_BRIEF.md`; `docs/design/PHASE18_A_TECHNICAL_DESIGN.md`; `docs/architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md`; `docs/architecture/MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md`.
**Status:** Proposed bounded technical design for independent review. This is not implementation authorization or capability promotion. No implementation checkpoint IDs are introduced.

## 1. Purpose and boundary

P18-B defines domain-owned facts and transitions for a timed activity instance: its stable identity, definition/version, lifecycle, interval, participant relations, commitments, and the availability consequences for each participant. P18-A owns logical time and deterministic due-work dispatch. P18-B owns whether a particular activity is pending, active, completed, cancelled, or interrupted and what participant commitment/availability facts follow from that transition. The scheduler queue is only a rebuildable index of P18-B's pending transition facts.

An activity definition describes supported semantics and constraints; an activity instance is one concrete occurrence with its own stable identity and lifecycle. Instance identity is independent of definition identity, `PersonId`, `NpcRuntime`, and materialization. The domain relation from an instance to participants permits zero, one, or many participants only while an instance is an unformed `Proposed` instance; every instance entering `Scheduled` or `Active` has at least one participant. A bounded implementation proof may create one participant, but may not encode that as the permanent cardinality rule. This preserves the P20 extension seam without implementing participant formation, roles, agreement, coordinated reservation, or shared execution.

P18-B is lifecycle infrastructure, not gameplay. It does not decide that an actor should begin an activity, choose an activity from Knowledge, apply consumer-specific effects, introduce an activity superclass, or turn every timed process into an actor activity. Passive production, calendar occurrences, and other owner-domain processes retain their own facts and scheduling descriptors; they do not reserve or change actor availability merely because they are scheduled.

## 2. Authority and data ownership

| Fact | Authority | Derived or prohibited substitute |
|---|---|---|
| Logical `now`, tick precision, calendar projection, due-work ordering/dispatch | P18-A timeline and scheduler contract | P18-B does not keep another clock or dispatch queue authority |
| Definition identity, supported duration/constraint semantics, compatible version | Definition/content owner | Runtime instance or actor type is not the definition |
| Instance identity, lifecycle state/revision, scheduled interval, transition disposition | P18-B activity domain store | Scheduler entries, events, UI state, and object references are not lifecycle truth |
| Participant relation and each participant's commitment | P18-B activity domain store, keyed by stable instance and participant identity | No participant list on `NpcRuntime`; materialized runtime presence is not participation |
| Availability | Individual actor/domain availability facts, maintained atomically with commitments by their authority | Not inferred from active `NpcRuntime`, day roster membership, a queue node, or an activity event |
| Consumer outcome/effect | The existing consuming domain | P18-B completion is not itself proof of a market, travel, need, or other world effect |
| Pending transition index | Rebuildable scheduler projection of current lifecycle revision and due instant | Never independently says an activity is pending |

P18-B may keep a canonical participant relation as domain-owned rows or an equivalent stable relation store, but each relation and commitment must resolve by stable IDs. A single mutable aggregate may be used only if its atomic update semantics are equivalent and its identity/cardinality remain independent. Events and diagnostics may describe transitions after commit, but cannot recreate a lost commitment or lifecycle fact.

## 3. Identities and instance facts

The proposed semantic identity model is:

- `ActivityDefinitionId` identifies the supported definition/content contract; its compatible version is recorded with the instance when version affects meaning.
- `ActivityInstanceId` is a stable semantic ID allocated by the activity domain. It is unique within its declared world scope and is never reused. It is not computed from a participant ID and does not change if the participant relation changes.
- `ParticipantId` is a stable domain identity (for Person-backed actors, `PersonId`). It is not an `NpcRuntime` reference.
- `ActivityRevision` increases on each committed lifecycle/participant/interval mutation relevant to queued work. Old scheduled descriptors cannot act on a later revision.
- A deterministic creation identity is sourced from an accepted, ordered input/owner operation identity or a persisted domain sequence. If creation is retried, the same operation resolves to its already-created instance; distinct creations receive distinct IDs. Runtime hash codes, iteration positions, timestamps alone, and transient object identity are not identity sources.

An instance records the definition/version, stable creation identity, state/revision, scheduled start instant, optional planned end instant or duration, actual start instant when started, terminal instant and disposition when terminal, and the participant relations/commitments that are causally relevant. Any duration conversion uses P18-A's checked tick unit and range. An interval is half-open `[start, end)`: a participant is committed for the activity through but not including the end instant. A finite activity requires `end >= start`; zero duration is permitted only under the explicit instantaneous transition rule below. An absent planned end means no automatic timed completion is scheduled; it is not an infinite arithmetic duration.

## 4. Lifecycle and transition rules

The bounded state machine is:

```text
Proposed ──accept/schedule──> Scheduled ──due start──> Active ──due end──> Completed
    │                              │                       │
    └──cancel──> Cancelled         ├──cancel──> Cancelled ├──cancel──> Cancelled
                                   └──invalidate──> Interrupted <──interrupt──┘
```

`Proposed` is optional domain state and may have no participants or reserved time. `Scheduled` has an exact start instant and at least one participant, with that participant relation and its commitments established atomically. `Active` records the actual start instant, which must equal the due instant at successful start, and retains at least one participant relation. An instance with no participants cannot be scheduled or started; it remains Proposed or takes an explicit terminal disposition. `Completed`, `Cancelled`, and `Interrupted` are terminal for that instance. A retry, new interval, or genuinely new occurrence receives a new instance identity (or an explicitly owner-defined generation that is part of semantic identity); terminal instances are not silently reopened.

Supported transitions are:

1. **Create/propose:** validate a supported definition/version and allocate the stable instance identity. It may be unstaffed and unscheduled. No actor availability changes until a commitment is accepted.
2. **Schedule/commit:** set a non-past start, validated finite duration or explicit no-automatic-end policy, participant relation, and each participant's commitment. Validate all affected participants and interval conflicts under the normal domain semantics. Publish all commitments, availability consequences, lifecycle revision, and P18-A due-work references as one coherent mutation.
3. **Start:** at the scheduled start instant, resolve the authoritative instance and revision. If current domain preconditions still hold, transition `Scheduled → Active` and record actual start. This transition changes lifecycle/availability facts only; it does not perform a consumer's domain outcome. If a required precondition no longer holds, apply the definition/consumer's explicit supported disposition (for this bounded generic contract, cancel or interrupt and release commitments) atomically. It must not start a subset of a future multi-participant instance by accident.
4. **Complete:** at the planned end, transition `Active → Completed`, record terminal instant, and release its commitments/availability constraints atomically. A completion descriptor does not itself grant a domain effect; a consumer that needs an effect owns and validates that separate operation and its exactly-once identity.
5. **Cancel:** an authorized normal domain operation may cancel Proposed, Scheduled, or Active only where the selected consumer supports cancellation. It records a reason/disposition from the consumer's bounded vocabulary, invalidates pending descriptors by revision, and releases outstanding commitments atomically. Cancellation does not roll back already committed effects.
6. **Interrupt:** only a selected consumer that supports interruption may interrupt an Active instance when its defined condition occurs. Record the exact interruption instant and disposition, invalidate end work, and release commitments atomically. No generic rollback, automatic resume, or inferred restart is provided.

The generic P18-B proof should exercise schedule, start, completion, and stale/cancelled due work with a synthetic operation. Cancellation/interruption breadth is implemented only for supported selected semantics; the Brief does not require universal interruption policies. Domain coherence and current-truth checks required by normal gameplay remain. No adversarial authorization layer is introduced.

## 5. Duration, ordering, and due-work consumption

Durations are nonnegative integer P18-A logical ticks. P18-B never measures host elapsed time, frame count, or date-only elapsed days for an intraday interval. Start and completion are explicit typed P18-A due-work references with stable owner ID, instance ID, expected revision, due instant, and transition kind. The authoritative lifecycle record determines whether each descriptor is still current.

At an exact instant, P18-A's reviewed ordering applies: sealed accepted inputs first, the day-boundary operation in its declared position, then stable ordered due work and causal waves. Thus an input that cancels or revises an activity at the same instant is observed before its stale start/end descriptor. A descriptor for a prior revision resolves stale and has no lifecycle or domain effect. A same-instant zero-duration scheduled instance starts and then completes in successive causal transitions only after each preceding transition commits; it cannot recursively invoke either transition or bypass P18-A's deterministic per-instant work bound. If start fails, completion is not run.

Exactly-once lifecycle completion is the committed owner transition, not removal from the queue or successful handler entry. Before dispatch, resolve the current instance and verify identity, expected revision, state, due instant, and transition legality. The lifecycle state/revision change, commitment/availability updates, consumption of the current one-shot due fact, and publication/invalidation of related due facts form one atomic mutation. On any validation or commit failure, publish no partial state, consume no work, and allocate no durable causal order values. Retry resolves current authority first; a transition already committed cannot execute again. If a consumer outcome is needed, it must have its own existing atomic/idempotent domain transaction and is not implied by the lifecycle transition.

Schedule, cancellation, interruption, and participant/interval changes invalidate affected transition references by advancing revision (or a stable generation). Queue deletion is an index optimization only. IDs are never reused; lazy stale nodes cannot consume a newer occurrence. A reschedule is a committed domain transition that publishes a fresh due reference; no handler invents a new later time after failure.

## 6. Commitment and availability semantics

A commitment is a domain fact linking one stable participant identity to one activity instance and a half-open reserved interval, with the lifecycle revision/context required to validate it. It is individual even where a later P20 instance has several participants. A participant's availability is evaluated and updated by the existing individual availability authority against its current commitments and supported conditions. P18-B provides the commitment seam; it does not prescribe utility, daily turns, a universal conflict resolver, or an all-purpose reservation solver.

For the bounded single-participant path, scheduling is accepted only when the selected consumer's normal domain rules accept that participant and interval. If rejected, no commitment, availability change, or due reference is published. For a future shared instance, the transition boundary must validate the complete required set as one write set; P18-B does not decide how that set forms, what roles/counts it needs, or recruit participants. A participant relation may be empty only for an unformed Proposed instance. Every Scheduled or Active instance has at least one participant; zero-participant instances cannot enter either state.

Availability changes caused by start, completion, cancellation, or interruption publish a domain-owned boundary/notification usable by P18-C without exposing hidden world truth. P18-C decides when/how to re-evaluate. Materializing or unloading an `NpcRuntime` does not create, cancel, or complete an activity and does not by itself change availability. Dormant/unloaded actors retain the same stable PersonId commitments and lifecycle relationships.

Passive processes such as market production, resource regeneration, or a calendar occurrence stay in their owning domains with their own due-work descriptors and state. They may happen during an actor commitment, because a participant's unavailability does not pause world processes. They do not create fake activity instances or imply an actor is occupied.

## 7. Queries, cloning, and reconstruction

Read-only queries at an explicit snapshot/instant may return instance state, participant relations, commitments, and availability projections in stable identity order. They cannot drain due work, refresh an index by mutating authority, advance revisions, or make an actor available. A forecast may become stale before execution; it is not an outcome.

Cloning or reconstructing a world preserves the same causal facts and deterministic next transitions for the cloned branch: logical instant/calendar compatibility from P18-A; definition identity/version; instance IDs, creation identities, lifecycle/revision, planned/actual interval and terminal disposition; participant relations and commitments; availability source facts; pending start/end facts or sufficient deterministic inputs to rebuild them; invalidation/generation state; accepted input boundaries and ordering; and any owner sequence/random context actually consumed. The scheduler index may be rebuilt and must yield the same order. Forks preserve the branch's current participant relation and commitments; they do not infer later membership or rerun an old transition. Events, diagnostics, UI, object references, and NPC materialization are not substitutes for these facts. This is a reconstruction inventory, not a save/load format or persistence implementation.

## 8. Validation obligations and implementation hotspots

An implementation candidate and its independent review should cover:

- stable definition/instance/participant identity separation, including zero/one/many relation representation, zero participants only for unformed Proposed instances, and a one-participant proof that does not establish a permanent cardinality law;
- checked duration/range handling, half-open intervals, exact start/end timestamps, zero-duration sequencing, and day-boundary interaction through P18-A;
- atomic scheduling and commitment publication, including rejection with no partial availability or due-work mutation;
- successful start/completion exactly once across retries, stale revisions, cancellation/interruption invalidation, and deterministic queue rebuild;
- input-before-work ordering at equal instants, non-reentrancy, same-instant causal waves, and P18-A dispatch-limit behavior;
- supported interruption/cancellation disposition and commitment release without rollback of earlier domain effects;
- participant identity and commitment survival through dormant/unloaded/materialized runtime states;
- query purity and branch reconstruction producing identical ordered due transitions;
- a passive scheduled process continuing independently while an actor is committed/unavailable;
- no consumer gameplay, actor-choice loop, travel migration, participant solver, mod loader, or persistence implementation introduced by the lifecycle slice.

Hotspots include P18-B activity/commitment authority, P18-A due-work descriptor/atomic dispatch seams, individual availability authority, and any canonical diagnostics/snapshots that must expose new authoritative state. Writers must be isolated and ownership boundaries agreed before implementation. No `SimulationRuntime` daily-loop, `NpcRuntime`, P8 travel, or P11 SellGoods consumer change is authorized by this design.

## 9. Dependencies and explicit exclusions

P18-B technical design consumes the reviewed P18-A contract at the stated base. Implementation/integration remains blocked until P18-A is promoted and the P18-B implementation boundary is independently reviewed. P18-C consumes A/B contracts and promoted capabilities; P18-D selects only actual consumer integrations. P18-B has no dependency on P9 generation, P14 jobs, P17 War, P19 loader, or P20 implementation. P20 later layers participant formation and shared execution on relevant A/B/C capabilities; P18-B must keep identity/cardinality compatible without waiting for P20.

Excluded: Sleep, Dreams, needs, jobs, robbery/theft/gangs, rituals, War, MegaEventos, travel, SellGoods, actor-choice/decision loops, per-frame polling, role catalogs, participant formation/recruitment, coordinated reservation solving, persistent Group membership, shared consumer effects, universal recurrence/activity frameworks, public mod APIs/loaders, and save/replay implementation. No unresolved semantic issue is identified by this proposal; interval, identity, atomicity, and scope choices are explicit proposals for independent review.
