# P20-B — Two-Person Joint Civil Travel Technical Design

**Checkpoint:** P20-B, bounded joint civil travel proving slice.
**Design base:** `f6924e63d8e5731da1d33021d0361e7defe6dad7` on `codex/architecture/p20b-technical-design`.
**Authority:** `docs/SIMULATION_ARCHITECTURE.md` §§11–12, 91–93; `docs/ROADMAP.md`; `docs/EXECUTION_MODEL.md`; `docs/phases/PHASE20_BRIEF.md`; `docs/PHASE18_STATE.md`; `docs/PHASE8_STATE.md`; `docs/design/PHASE8_E_TECHNICAL_DESIGN.md`; `docs/design/PHASE18_A_TECHNICAL_DESIGN.md`; `docs/design/PHASE18_B_TECHNICAL_DESIGN.md`; `docs/design/PHASE18_C_TECHNICAL_DESIGN.md`; `docs/architecture/MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md`; `docs/architecture/P10_P14_P20_NEXT_SCOPE_DECISIONS.md`; `docs/architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md`.
**Status:** bounded technical design proposal; it has not received independent technical review and does not authorize implementation or promotion.

## 1. Decision and boundary

P20-B proves one real shared activity: exactly two independent Persons can separately assent to one civil-travel activity, hold compatible future commitments, and begin one common supported Hex-to-Hex passage at one logical instant. A single coordination instance owns shared intent, agreements, and lifecycle linkage. P18 continues to own activity lifecycle and commitments; P8 continues to own each Person's route plan, position, passage, and travel facts. The start boundary coordinates one prepared transition in each Person-owned travel domain with the P18 activity transition.

The two-Person/two-equivalent-traveler-slot rule is a fixture and checkpoint constraint only. Start validity (required participant/role counts, current capability and availability, route agreement, and current passage) is distinct from contribution/effect. Neither this design nor its interface encodes exactly two, travel-only activities, one role, a one-participant-per-role maximum, universal common intervals, or a universal contribution formula. Future definitions may require four people, include optional helpers, contain several participants in a role, or use multiple roles. A later consumer separately defines each person's contribution and outcome.

No persistent Group, Party, Organization membership, shared Person/Actor identity, common Knowledge, generic workflow engine, universal role catalog, recruitment AI, or military/War semantics are introduced. The shared activity is temporary and does not become a group entity.

## 2. Evidence and present seams

Promoted P18-A/B/C supply logical ticks and deterministic due-work dispatch, lifecycle/participant commitments, and availability-driven actor decision seams. Promoted P20-A supplies a synthetic coordination proof, not a real consumer or universal activity policy. The owning `PHASE20_STATE.md` is not present in this checkout; P20-A's promoted status, integration/code SHAs, and bounded exactly-two fixture rule are corroborated by `PHASE18_STATE.md`, the P20 Brief, and promoted architecture/alignment records. This omission is recorded rather than silently treating a missing state file as code evidence.

P8-E currently owns an individual Person's accepted/active route plan, current position/transit, passage revalidation, and prepared atomic updates across that Person's position/route/Knowledge authorities (`P8ETravelTransactionCoordinator`). P8-E was promoted on the P8 canonical line at `d95b60d` (Phase 8 State closure at `0ae5055`); the older P8-E technical-design document still contains historical pre-promotion candidate wording. Its explicit operation does not provide group travel, shared start, or automatic intraday advancement. `ActivityLifecycleStore` already owns stable activity instances, participants, temporal commitments, due-work references, and a start validator. It does not presently compose a lifecycle start with two P8-E participant transitions in one transaction. P20-B must add the narrow composition seams specified in §§5–6; two sequential calls to today's individual start/arrival methods cannot provide the required all-or-nothing result. P18 TrySchedule can already atomically commit a full participant set, so P20 will defer all availability reservation to that existing transition rather than add a per-assent P18 reservation API.

Relevant implementation evidence read includes `ActivityLifecycle.cs`, `LogicalTimeline.cs`, `ActorAvailabilityDecision.cs`, `P8ETravelTransactionCoordinator.cs`, `TravelSystem.cs`, `TravelParty.cs`, and tests `ActivityLifecycleTests.cs`, `LogicalTimelineTests.cs`, `ActorAvailabilityDecisionTests.cs`, `GeneralizedSpatialTravelTests.cs`, `GroupTravelTests.cs`, and `TravelPartyCensusTests.cs`. Existing `TravelParty` behavior is not the semantic model for this Person-based proof and must not be reused as a persistent P20 group.

## 3. Ownership and proposed interfaces

Keep the solution consumer-specific and small:

| Concern | Owner | Proposed shape |
|---|---|---|
| Activity definition/version, stable instance ID, lifecycle, scheduled participant relation/commitments, terminal state | P18 `ActivityLifecycleStore` | Existing authority; P20 does not create reservations or lifecycle shadows. Add only narrow prepared consumer-start and consumer-terminal hooks for this real consumer. |
| Proposal, independent per-Person assent, selected common civil leg and linkage to P18 | P20-B `JointCivilTravelStore` (small new owner) | Stable `JointTravelId`; references `ActivityInstanceId`, two distinct `PersonId`s, shared segment identity and each Person's assent/input identity. No availability, commitment, position, transit, or lifecycle authority. |
| Individual position/transit, accepted route, passage, current travel state | P8 spatial/travel authorities | Existing position, route-plan, and passage authorities. Extend P8-E preparation to combine actor-specific prepared changes into one next-root per affected store and one install set for the selected participants. |
| Logical time, accepted-input / boundary / due-work order | P18-A `SimulationTimeline` | Existing authority; no second clock or scheduler. |
| Per-Person Knowledge | Individual Knowledge/spatial route-knowledge owner | Each proposer/acceptor sees only their own supported Knowledge snapshot; execution checks current truth. |
| Domain outcomes and event/diagnostic reporting | P8/P20 consumer and normal event/diagnostic surfaces | Events are emitted only after owner facts commit; they cannot substitute for committed state. |

Conceptual call boundaries (names illustrative, not a frozen public API). Proposal creation itself must stage the P18 Proposed instance and P20 proposal row together through a narrow prepared P18 proposal seam; a half-created orphan is not accepted. Exact commit: preallocate the stable IDs/creation identity and both private replacement roots; under the serialized runtime mutation boundary, recheck both expected owner revisions, call `SimulationTimeline.TryCommitOwnerFacts` with no due-work references, then install the P18 Proposed row and P20 proposal row with no-fail swaps. Consent mutations then affect only P20.

```text
TryPropose(sharedLeg, inviterPersonId, inviteePersonId, logicalStart, creationIdentity)
TryAccept(jointTravelId, acceptingPersonId, consentIdentity)
TrySchedule(jointTravelId, logicalStart)
TryPrepareJointStart(jointTravelId, dueReference, out PreparedJointTravelStart)
TryAdvanceParticipant(jointTravelId, personId, progressTicks)
TryArriveParticipant(jointTravelId, personId)
TryCancel(jointTravelId, actor/owner disposition)
TryAbort(jointTravelId, reason)
```

Each transition validates stable IDs, expected P20 coordination revision, expected P18 activity revision, expected timeline instant, route-plan and position revisions, and the identity of each affected participant. The implementation should use prepared owner mutations and a no-fail install phase; it should not expose internal store root swaps to callers.

`JointCivilTravelStore` stores invitation/proposal facts and independent consent only. The exact two Person IDs and requested logical start are proposal data until schedule. P20 stores no tentative time lock. P18 is the sole authority for accepted participant relations, commitments, availability consequences, lifecycle, and terminal dispositions. P18 participant references resolve as `PersonId` strings, never as `NpcRuntime` instances. P20 derives scheduled/active/completed/cancelled/interrupted state from the referenced P18 instance rather than caching a second lifecycle status. Definition identity/version is fixed for this selected P20-B activity definition and retained if it can affect interpretation.

## 4. Formation, consent, and reservation

P20-B uses one concrete rule for the pre-schedule gap: individual assent is recorded by P20, but assent alone does **not** reserve the Person's time. P18 `ActivityLifecycleStore.TrySchedule` is the sole reservation authority and commits both required Persons' commitments together only after both consent facts exist. This avoids a second reservation store and avoids adding a new per-participant reservation mode to P18 that its promoted API does not currently support. While the proposal is partially accepted, a Person may still accept another commitment; the later joint schedule then revalidates and either reserves both or changes neither. Because the single leg has no fixed duration and completion is arrival-triggered, the P18 commitment is `[scheduledStart, ∞)` and remains active until the explicit P18 terminal transition releases it; this affects only this bounded consumer instance.

1. **Propose:** create a stable `JointTravelId` and one P18 Proposed instance. P20 records inviter/invitee PersonIds, the requested logical start instant `start`, common segment identity (`fromHexId`, `toHexId`, traversal option/boundary), inviter's assent identity, and proposal revision. P18 has no scheduled participants/commitments yet. The proposal is non-executing and may remain partial.
2. **Independent assent:** each named Person accepts or declines through that Person's decision boundary and own Knowledge. The P20 store atomically updates only that Person's assent fact/revision and causal input identity. An invite or inviter's choice never implies the other Person's choice. Refusal closes the offer as declined; no P18 commitment is created. P18 Proposed may remain as an inert, participant-free historical instance; P20 refusal prevents any TrySchedule call, and ordinary P18 cancellation may terminalize it without travel facts. For the controlled proof, calls may be explicit test/owner operations; P11 is not needed unless external command capture is separately selected.
3. **Joint schedule and reservation:** once both distinct required Persons have accepted, call the existing P18 `TrySchedule(timeline, activityInstanceId, now, start, duration: null, participantIds)` with exactly those two stable PersonIds. P18 creates open-ended commitments beginning at `start`; it releases them only on failed start, pre-start cancellation, completion, or active abort settlement. P18 validates start/state/conflicts and stages participant relation, both open-ended commitments `[start,∞)`, start due-work descriptor, revision, and Schedule receipt as one timeline owner-facts commit. It either commits the complete two-person reservation or leaves P18 unchanged. P20 consent facts are immutable through this call, so there is no second store mutation to reconcile. A stale P20 proposal/assent revision or changed proposal start/segment is checked immediately before the P18 call; serialize this check and `TrySchedule` under the runtime's existing authoritative mutation boundary. A failed attempt remains an agreed but unscheduled proposal and may be cancelled; it cannot start.
4. **Start-time validity:** reservation is not a start guarantee. P18's due-start validation rechecks the complete two-person requirement, each current commitment, availability/capability, and each P8 current-truth passage/position condition. Failure uses P18's failed-start terminal transition and releases both commitments together.

The selected definition declares two required equivalent traveler slots for this proof. Its requirement check asks whether the selected instance has the required participants and both can start; it does not calculate their travel contributions. Generic future definitions remain able to require other counts/roles, optional participants, or multiple same-role participants.
## 5. Shared start, current truth, and atomicity

At the scheduled due instant, a P20-B start validator prepares the complete start transaction. It reads both Persons in stable ordinal `PersonId` order and validates, for each:

- the stable Person exists as a domain identity; no NPC materialization is required;
- an accepted route is at the correct origin (or an already active route is at the same next segment, if the selected implementation explicitly supports this case);
- the next planned segment exactly matches the joint shared segment;
- the current stable position is the shared `fromHex` and the Person is not already in transit;
- the current commitment and activity revision still match the scheduled instance;
- individual capability and availability conditions still permit the start;
- the current authoritative passage evaluator permits the same traversal option for that Person's applicable movement context.

Each Person's Knowledge may have informed their plan and assent. The validator does not merge Knowledge or use hidden truth to rewrite prior beliefs; it does revalidate factual passage/position conditions needed for execution. Any materialized NPC position is a projection, not the travel authority.

Preparation is owned by a batch operation on `P8ETravelTransactionCoordinator`, called from a P20-specific prepared-start participant attached to the P18 due-start transition. The batch validates the two Persons in ordinal PersonId order, evaluates passage separately per movement context, and asks `PersonSpatialPositionStore` for one prepared replacement root containing both begin-transit changes at one expected store revision. It similarly asks `PersonRoutePlanStore` for one prepared replacement root containing both Accepted→Active changes. If either route is already Active under an explicitly supported fixture, no plan change is prepared for it; otherwise the route must be Accepted at the shared origin. Do not create two single-Person prepared replacements against the same store revision and install them sequentially: the second would overwrite the first root or fail its revision guard.\n\nA narrow P18 extension lets the start validator prepare this P8 batch and hand it to the existing `ActivityLifecycleStore` due-work commit. On the P18/timeline owner-facts callback, first recheck the due reference, lifecycle revision, P20 assent/proposal revision, both P8 store revisions and each prepared change's install guard. Then install the single P8 position root, single P8 plan root, P18 Scheduled→Active/lifecycle/commitment/pending-work roots, and committed receipt in deterministic owner order. The P20 proposal/assent store is read-only at start and needs no lifecycle mirror write. No callback, validation, allocation, or fallible operation may run after the first install. P18 and P8 remain the sole authorities for their respective facts.

If either participant fails preparation, the start validator returns a failed disposition; existing P18 FailedStart semantics then atomically mark the activity terminal and release both P18 commitments, with no P8 root installed. If preparation succeeds but a captured revision is stale at due-work commit, the composite commit fails without installing any root or consuming the work; retry re-resolves the still-current due reference and prepares from current truth. A current-truth rejection is terminal FailedStart; a transient revision race is retryable. The P18 prepared-start extension must distinguish these cases and preserve P20-A and all ordinary P18 validators/transitions. The P20 assent/proposal remains historical formation input; the authoritative scheduled/failed state remains P18.

A person-ordering rule is required for deterministic preparation and diagnostics. It must not let sort order choose who travels if the other cannot; both are required by this proof definition. Stable operation / creation identities and P18 causal sequence govern retries and records. No RNG is required for this travel transition. If a later selected domain effect consumes randomness, its effective configuration and random context become explicit retained inputs.

## 6. Individual travel, effects, completion, cancellation, and abort

P20-B schedules the P18 activity with `duration: null`. P18 therefore schedules the due start but no automatic `Complete` due reference. This is intentional: completion is owned by a narrow P20 consumer-triggered P18 transition, not by a guessed fixed duration or by one Person's early arrival.

After shared start, each Person has a distinct P8 transit and route state. Progress is explicit and per Person through the existing P8 travel operations under the runtime mutation boundary; P20 adds no auto-progress loop. Each Person's P8 arrival is also prepared and committed by that Person's route/position owners. The common activity completes on the exact operation that commits the second required Person's arrival at the agreed common destination. That operation uses a new narrow P18 `TryPrepareConsumerTerminalTransition` seam (conceptual name): it checks the Active instance/revision/participant set/current `LogicalTick`, prepares `Active → Completed`, releases both P18 commitments, consumes/invalidate relevant work, and publishes the terminal receipt. The prepared second-Person P8 arrival and the P18 transition are installed in the same timeline owner-facts commit. If the first arrival already committed, it remains factual and the second arrival is the only remaining P8 transition in that write set. There is no P20 copied arrival flag: whether one or both are at destination is read from P8 position authority at preparation time and guarded by its revision.

P20's narrow P18 integration must preserve existing P18 behavior: ordinary duration-bearing activities keep their current scheduled completion path, P20-A is untouched, and only an explicitly marked consumer-managed activity with no `PlannedEnd` may use this terminal API. P18 continues to own lifecycle, participant commitments and receipts. Consumer-managed completion must not be reachable through an unguarded generic call that can complete unrelated activities.

If an explicit domain travel effect is selected, each Person's effect is planned and committed by its owning domain per Person; shared timing does not merge results or Knowledge. P20 records no duplicate effect truth. This bounded fixture can use no additional outcome effect beyond P8's person-specific travel state.

**Cancellation and active abort are supported with fixed semantics.** Before the scheduled start, cancellation delegates to P18 `TryCancel`, which atomically invalidates the start reference and releases both commitments. P20 proposal/assent remains immutable historical formation evidence; lifecycle disposition is read from P18. No route enters transit.

After start, abort is a request to finish the already-started single civil leg and terminate the shared activity as Interrupted; it is not an immediate reversal or cancellation of physical travel. P20 records a stable `AbortAfterLeg` request under expected coordination revision. This is an explicit P20-only state mutation and does not change P18/P8 facts or release commitments yet. Both Persons continue only their already-started P8 segment to the agreed destination. No next segment is permitted. An individual arrival commits normally and remains factual. When the second Person arrives, the same prepared arrival transaction described above chooses `Active → Interrupted` instead of Completed, releases P18 commitments and writes the interruption receipt atomically with that P8 arrival. Until both reach the destination, the P18 activity remains Active and both commitments remain in force. This rule prevents a fictitious mid-segment location, teleport, rollback, or stranded in-transit Person. The fixed single segment cannot be aborted mid-leg; this is a deliberate bounded rule, not an unresolved implementation choice.

Loss/refusal before schedule means no schedule. A start-time loss/stale participant fails the whole start. Dormancy or absence of an `NpcRuntime` is not participant loss because PersonId-backed identity and P8 truth remain authoritative. Post-start participant deletion is outside the proof; no death/removal path may silently delete a required Person while this instance is Active.

Receipts/events describe committed facts but never substitute for P18 lifecycle, P8 position/route, or per-Person outcomes. No common Knowledge update is produced.
## 7. Timeline and stale-work behavior

Use P18-A `LogicalTick` and the bound authoritative timeline. `now`, accepted-input ordering, day-boundary ordering, due-work order, causal waves, dispatch bounds, and pending-reference invalidation remain P18-A/P18-B-owned. The joint start is scheduled through the existing P18 `TrySchedule(..., duration: null, participantIds)` path with stable owner/instance/revision/due identity. It creates no P18 completion due reference. The timeline ordering applies at equal instants; same-instant accepted cancellation/revision makes queued start work stale before execution. A stale due reference must be a no-op or a committed FailedStart per P18 contract, never a partial participant transition. Completion/interruption is the explicit P20 consumer terminal operation defined in §6, at the exact `LogicalTick` of the second participant's committed arrival.

No frame clock, date-only travel estimate, private scheduler, or daily-loop participant logic is introduced. P8-E remains explicit-operation travel: P20-B's due-start consumer invokes the joint start only through the supported logical timeline operation; later per-Person progress/arrival is explicit and occurs under the runtime mutation boundary at the current logical instant. It does not claim that P8-E advances itself intraday.

## 8. State inventory, profile exclusion, and reconstruction

The P20-B state contract is required even though save/fork implementation is excluded. Retain enough authoritative facts to reproduce causality:

- stable world identity and compatible calendar/logical-time profile;
- P20 definition identity/version if behaviorally relevant; stable `JointTravelId`, creation identity, coordination revision, shared-segment semantic identity, proposal, each independent assent/decline fact and causal input identity, and an accepted `AbortAfterLeg` request until the terminal P18 transition;
- stable P18 `ActivityInstanceId`, current lifecycle state/revision, participant PersonIds, planned start, open-ended commitments and their revisions, terminal disposition, transition sequence, and pending due-work facts or sufficient owner state to rebuild them identically;
- each Person's authoritative stable position/transit progress, route-plan identity/revision/status and accepted segment facts, passage option/boundary identity, current P8 authority revisions, and any individual domain outcome already committed;
- current truth/configuration/authority inputs relevant to route execution, effective movement context, captured accepted inputs and deterministic causal order; random context only if actually consumed;
- stable references between the P20 coordination instance, P18 activity instance, both Persons, and their separate P8 owner facts.

Derived route/commitment lookup indexes, timeline queue nodes, diagnostic events, UI state, caches, and `NpcRuntime` materialization may be rebuilt only from those sufficient retained facts. Query/preview/validation is pure and consumes neither RNG nor causal sequence.

The currently accepted `UnityBootstrap-Daily-v1` P12 profile excludes P18 intraday state, P20 shared-activity state, and this new coordination owner. P20-B must not expand that profile. Before P20-B domain promotion, satisfy architecture §92A by either proving the new owner cannot enter selected P12 composition or integrating a fail-closed admission/inventory hook with a negative rejection test for any composed P20-B joint-travel state. This design chooses the latter: enumerate the P20 coordination section (including empty/absent coverage semantics) and shared P18/P8 dependencies at admission; reject a nonempty or unhandled owner state before publication/hydration into the daily profile. Mere omission from serialization is not proof. A future explicitly supported intraday profile needs exact export, private staged hydration, referential validation, and reconstruction tests for the complete P18/P20/P8 write set. A later P12/P13 adapter cannot recreate consent, reservations, route revisions, or prior individual transitions discarded here.

No save schema, serializer, loader, fork mechanics, or daily-profile enlargement are implemented by P20-B.

## 9. Test and review plan

The implementation candidate should add focused owner and integration tests proving:

- each Person consents independently; inviter assent cannot stand in for invitee; refusal/unanswered proposal never starts;
- a proposal can remain partially formed; duplicate Person, duplicate identity, count mismatch, same-role semantics, and repeated participants are handled under the selected narrow definition without hardcoding an extensibility ceiling into generic P18;
- each consent commits only the consenting Person's P20 assent fact and creates no availability reservation; TrySchedule after both assents atomically reserves both through P18, and conflict leaves both P18 and P20 authority unchanged; cancellation releases the P18 pair exactly once;
- P18 TrySchedule atomically commits exactly two PersonId participant links and both commitments with one due start and no planned end; same-instant cancellation or changed activity revision makes start work stale;
- successful joint start produces one prepared position-store root for both Persons and one prepared route-plan-store root for both Persons; each failpoint (either Person position/route/passage, P18/P20 revision, either P8 store revision, or commit guard) leaves all P8/P18 roots unchanged or follows P18 FailedStart; validate stale commit retry separately from terminal current-truth rejection;
- current passage truth is revalidated separately for both Persons even if their Knowledge/route plans match; one newly unavailable passage prevents either from starting;
- one Person's route/position can never substitute for the other's, there is no shared position, and effects/Knowledge remain per Person;
- individual transit/arrival can differ in order; the second committed arrival and P18 terminal transition share one owner-facts commit; AbortAfterLeg never reverses a started leg and retains commitments until both arrivals;
- retries after committed and uncommitted operations use stable operation IDs and do not double-apply either travel start, arrival, or individual effect;
- clone/reconstruction fixtures restore stable links, partial proposals, open-ended scheduled commitments, scheduled starts, in-transit Persons, staggered arrivals, pending AbortAfterLeg intent and causal ordering; indexes/queue rebuild deterministically;
- P12 `UnityBootstrap-Daily-v1` admission positively rejects a world containing this P20 owner state and continues to accept an empty/unrepresented-free state according to the explicit inventory contract;
- relevant `ActivityLifecycleTests`, `LogicalTimelineTests`, `ActorAvailabilityDecisionTests`, P8 generalized spatial travel/route planning, and existing TravelParty census behavior remain within their original profiles; no persistent `TravelParty` authority leaks into P20.

Candidate review must inspect full diff against this exact base, including semantic boundaries, prepared atomicity across timeline/P18/P20/two P8 Persons, deterministic ordering, stale/cancellation handling, exact P12 admission failure, and reconstruction state. Focused integration validation and the repository's risk-appropriate regression gate are required before promotion. This document is not self-review evidence.

## 10. Dependencies, exclusions, files, and sequence

**Hard promoted capability dependencies for implementation:** P18-A/B/C; P20-A coordination precedent; applicable P8-E explicit Person travel/position/passage. The architecture and contract decisions are accepted. Current `PHASE8_STATE.md` records P8-A through P8-E promoted; P8-E is an individual explicit-operation capability only. P11 is conditional on choosing typed external commands. There is no dependency on P18-D, P8 route planning beyond the one selected supported segment, P12 Save completion, P13 fork, P14 production, P15 construction, P19 loader, Group membership, or War.

**Likely implementation file boundary (subject to current exact-tip review):**

- New `JointCivilTravelContracts.cs`, `JointCivilTravelStore.cs`, and a tightly bounded `JointCivilTravelSystem.cs`; store only proposal/assent/abort-request facts and inventory the owner for P12 fail-closed exclusion. Add a prepared P18 Proposed-instance insertion seam used only by the composite proposal-creation operation so P18/P20 proposal creation is all-or-nothing: prebuild both owner roots, then install them together under `TryCommitOwnerFacts` after all revision checks.
- A narrow P18 prepared consumer-start and consumer-terminal integration in `ActivityLifecycle.cs`: start accepts one prepared domain participant; terminal supports consumer-triggered completion/interruption only for explicitly marked duration-null activities. Existing P18 scheduled completion and P20-A behavior remain unchanged.
- Batch prep/install seams in `P8ETravelTransactionCoordinator.cs`, `PersonSpatialPositionStore.cs`, and `PersonRoutePlanStore.cs`: aggregate both Person changes into one replacement root per store, with one CanInstall and one no-fail InstallPrepared per root. Reuse the same pattern for one participant's final arrival when P18 consumer-terminal transition is in the same commit.
- Composition wiring in `SimulationRuntime.cs` only if needed to provide authoritative owners/timeline/guard; do not add participant loops to `AdvanceDay`.
- Focused tests under `Assets/_Project/Tests/EditMode/Editor/` for joint travel formation/start, abort/arrival, profile rejection, plus only necessary adjustments to existing owner tests.

**Integration sequence:**

1. Reconfirm current canonical P8/P18 State and code, capability promotion SHAs, approved P20-A details and clean integration base; reconcile missing owning P20 State evidence if the authoritative worktree still lacks it.
2. Implement/test isolated P20 coordination facts and consent/reservation transitions against P18 authorities.
3. In a separate serialized hotspot window, add the P18 due-start prepared commit seam and P8 two-Person batch preparation/install; review no partial-write paths before composition.
4. Compose and test the shared start, individual travel continuation/arrival, explicit second-arrival terminal trigger, pre-start cancellation, AbortAfterLeg request/settlement, and per-Person Knowledge/effects.
5. Add exact §92A export/hydration seams only if required by owner contract; add the fail-closed P12 daily-profile exclusion hook and negative rejection regression before candidate promotion. Do not implement save/load.
6. Run focused and required integration regressions, `git diff --check`, and independent exact-tip candidate review. Stop at normal candidate/promotion gates; technical-design completion alone is not authorization to implement or promote.

**Exclusions:** persistent Party/Group; generic activity framework or role/contribution solver; broad activity consumers; autonomous recruitment/social negotiation; multi-leg group routes; military travel/War; route-choice AI or route replanning; new travel economy/needs effects; automatic world-time advancement invented by P8; P11 external command ingress unless separately selected; save/load/fork implementation; enlargement of `UnityBootstrap-Daily-v1`; broad diagnostics/UI project; architecture or Phase-State amendment in this design branch.

**Freshness and review:** architecture and roadmap are read at base `f6924e63d8e5731da1d33021d0361e7defe6dad7`. Re-evaluate this design if architecture, P8/P18 promoted State/code, P20 owning State, or P12 admission inventory advances before implementation. Independent technical review remains required; current verdict: `DESIGN DRAFT — NOT REVIEWED`, so implementation readiness is not yet claimed.
