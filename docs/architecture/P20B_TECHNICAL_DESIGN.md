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

P8-E currently owns an individual Person's accepted/active route plan, current position/transit, passage revalidation, and prepared atomic updates across that Person's position/route/Knowledge authorities (`P8ETravelTransactionCoordinator`). P8-E was promoted on the P8 canonical line at `d95b60d` (Phase 8 State closure at `0ae5055`); the older P8-E technical-design document still contains historical pre-promotion candidate wording. Its explicit operation does not provide group travel, shared start, or automatic intraday advancement. `ActivityLifecycleStore` already owns stable activity instances, participants, temporal commitments, due-work references, and a start validator. It does not presently compose a lifecycle start with two P8-E participant transitions in one transaction. P20-B must add that narrow composition seam; two sequential calls to today's individual start method cannot provide the required all-or-nothing result.

Relevant implementation evidence read includes `ActivityLifecycle.cs`, `LogicalTimeline.cs`, `ActorAvailabilityDecision.cs`, `P8ETravelTransactionCoordinator.cs`, `TravelSystem.cs`, `TravelParty.cs`, and tests `ActivityLifecycleTests.cs`, `LogicalTimelineTests.cs`, `ActorAvailabilityDecisionTests.cs`, `GeneralizedSpatialTravelTests.cs`, `GroupTravelTests.cs`, and `TravelPartyCensusTests.cs`. Existing `TravelParty` behavior is not the semantic model for this Person-based proof and must not be reused as a persistent P20 group.

## 3. Ownership and proposed interfaces

Keep the solution consumer-specific and small:

| Concern | Owner | Proposed shape |
|---|---|---|
| Activity definition/version, stable instance ID, lifecycle, participants, interval commitments, cancellation/interruption | P18 `ActivityLifecycleStore` | Existing authority; add a narrow prepared consumer-start transaction hook so a start can atomically include prepared domain effects. |
| Proposal, per-Person consent, selected common civil leg and linkage to the P18 instance | P20-B `JointCivilTravelStore` (small new owner) | Stable `JointTravelId`; references `ActivityInstanceId`, two distinct `PersonId`s, shared segment identity, each Person's explicit assent/revision, and coordination revision/state. No copied location, transit, or lifecycle truth. |
| Individual position/transit, accepted route, passage, current travel state | P8 spatial/travel authorities | Existing `PersonSpatialPositionStore`, `PersonRoutePlanStore`, `SpatialPassageAuthority`, and `P8ETravelTransactionCoordinator` prepared mutation patterns. Extend those owners with batch preparation/install for exactly the two selected Persons. |
| Logical time, accepted-input / boundary / due-work order | P18-A `SimulationTimeline` | Existing authority; no second clock or scheduler. |
| Per-Person Knowledge | Individual Knowledge/spatial route-knowledge owner | Each proposer/acceptor sees only their own supported Knowledge snapshot; execution checks current truth. |
| Domain outcomes and event/diagnostic reporting | P8/P20 consumer and normal event/diagnostic surfaces | Events are emitted only after owner facts commit; they cannot substitute for committed state. |

Conceptual call boundaries (names illustrative, not a frozen public API):

```text
TryPropose(sharedLeg, inviterPersonId, inviteePersonId, interval, creationIdentity)
TryAccept(jointTravelId, acceptingPersonId, consentIdentity)
TrySchedule(jointTravelId, logicalStart, logicalEnd)
TryPrepareJointStart(jointTravelId, dueReference, out PreparedJointTravelStart)
TryAdvanceParticipant(jointTravelId, personId, progressTicks)
TryArriveParticipant(jointTravelId, personId)
TryCancel(jointTravelId, actor/owner disposition)
TryAbort(jointTravelId, reason)
```

Each transition validates stable IDs, expected P20 coordination revision, expected P18 activity revision, expected timeline instant, route-plan and position revisions, and the identity of each affected participant. The implementation should use prepared owner mutations and a no-fail install phase; it should not expose internal store root swaps to callers.

`JointCivilTravelStore` must not duplicate participant commitments or activity lifecycle. It may retain the assent/proposal fact and shared-leg reference needed to explain why P18 instance participants committed. P18 participant references resolve as `PersonId` strings, never as `NpcRuntime` instances. Definition identity/version is fixed for this selected P20-B activity definition and retained if it can affect interpretation.

## 4. Formation, consent, and reservation

1. **Propose:** an inviter creates a stable joint-travel identity and P18 Proposed instance. The proposal contains a supported common segment identity (`fromHexId`, `toHexId`, traversal option/boundary) and a requested logical interval. It references individual route plans rather than copying them. Proposal is non-executing and may remain incomplete.
2. **Independent assent:** each named Person accepts or declines through that Person's decision boundary and own Knowledge. An invite or inviter's choice never implies the other Person's choice. A refusal is a recorded decision if that is part of the selected command surface; absent assent, no joint start is possible. For the controlled proof, these decisions may be explicit test/owner calls; P11 is not a dependency unless external Actor/GM command capture is added.
3. **Reservations:** each accepted assent may establish that Person's commitment to the requested half-open interval `[start,end)` through P18. The commitment mutation and assent revision commit together. The second assent is a separate coherent operation. A conflict leaves that Person's prior state unchanged. Incomplete formation is valid pending state, not a scheduled executable activity.
4. **Schedule:** only after both distinct required Persons have assented and both reservations are current and nonconflicting can one operation transition the P18 instance to Scheduled and publish the start due-work reference. Validate both role/count requirements and both intervals as one write set; do not infer that consent alone guarantees availability at start. Duplicate Person IDs, a mismatch between P20 and P18 participant sets, overlapping commitment, incompatible interval, or stale revision rejects without a partial schedule.

P20-B's consumer definition can declare two required traveler slots for the proof. Generic requirement semantics stay factored: count/role rules answer whether an instance is valid to start; domain contribution is evaluated independently and may later be nonlinear or absent.

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

Preparation must produce two independent P8-owned travel mutations, two route-status updates where needed, the P20 shared execution/lifecycle linkage, and the P18 `Scheduled → Active` transition. The `SimulationTimeline` due-work commit validates all captured revisions and lifecycle/coordination guards before installing any prepared roots. It then installs the complete fixed write set in deterministic `PersonId` order and publishes one committed P18 receipt/shared-start event after the no-fail install point. There must be no callback, validation, allocation, or fallible operation after the first root installation.

If either participant fails any check, or any expected revision changes before commit, install none of the travel, route, lifecycle, commitment, or coordination changes. The stale start takes the explicit failed-start path, records a stable failure disposition, invalidates its due reference, and releases both reservations through one terminal lifecycle mutation. The implementation must provide a coherent outcome under the timeline owner-facts boundary; a validator that merely returns true and then calls two existing P8-E starts sequentially is insufficient.

A person-ordering rule is required for deterministic preparation and diagnostics. It must not let sort order choose who travels if the other cannot; both are required by this proof definition. Stable operation / creation identities and P18 causal sequence govern retries and records. No RNG is required for this travel transition. If a later selected domain effect consumes randomness, its effective configuration and random context become explicit retained inputs.

## 6. Individual travel, effects, completion, cancellation, and abort

After successful shared start, the P20 instance remains a single common activity, but each Person has their own P8 transit and route state. Individual progress and arrival use the existing P8 travel owner rules and may commit at separate timeline instants. A shared context does not imply identical travel effects, a shared position, or forced synchronization of every later transition. The selected proof must state its supported end condition: complete the P18 instance only when both Persons have individually arrived at the common destination; record each arrival separately under the P8 owner and then complete/release the P18 commitments through an ordered lifecycle transition. If an explicit travel outcome/effect is selected, each effect is evaluated and committed by its owning domain per Person; P20 coordinates the shared timing and references the two results but does not apply a generic effect.

Cancellation before start cancels the P18 instance, invalidates scheduled work, and releases every participant commitment in the same coherent owner mutation; no route enters transit. If the supported slice permits abort after start, interruption must not roll back already committed travel progress or effects. Mark the activity interrupted, invalidate remaining lifecycle work, release outstanding commitments, and interrupt each still-active individual route/position through a prepared P8 batch. Preserve each Person's factual current position/transit according to the P8 owner policy. No automatic retry, teleport, rewind, or rollback is implied. If the implementation chooses not to support active abort, it must explicitly reject abort after start and retain only pre-start cancellation; it may not silently cancel lifecycle while leaving active P8 travel unaccounted for. Loss of either required participant before start fails the whole start; loss during execution follows the same explicit interruption rule, not partial success.

Failure/cancellation receipts are triggers and history only. Lifecycle state, reservations, and each Person's factual position/route remain authoritative. Individual outcomes may differ, and no shared Knowledge update is created by the coordinated activity.

## 7. Timeline and stale-work behavior

Use P18-A `LogicalTick` and the bound authoritative timeline. `now`, accepted-input ordering, day-boundary ordering, due-work order, causal waves, dispatch bounds, and pending-reference invalidation remain P18-A/P18-B-owned. The joint start is scheduled work with stable owner/instance/revision/due identity. The timeline's documented ordering applies at equal instants; same-instant accepted cancellation/revision makes a queued start stale before it can execute. A stale due reference must be a no-op or a committed failed-start result according to the P18-B terminal contract, never a partial participant transition.

No frame clock, date-only travel estimate, private scheduler, or daily-loop participant logic is introduced. P8-E remains explicit-operation travel: P20-B's due-start consumer invokes the joint start only through the supported logical timeline operation, and each later progress/arrival fact uses a declared supported P18/P8 logical transition path. Design review must confirm the proposed P8 progress bridge against current promoted code; this design does not claim that P8-E already advances itself intraday.

## 8. State inventory, profile exclusion, and reconstruction

The P20-B state contract is required even though save/fork implementation is excluded. Retain enough authoritative facts to reproduce causality:

- stable world identity and compatible calendar/logical-time profile;
- P20 definition identity/version if behaviorally relevant; stable `JointTravelId`, creation identity, coordination state/revision, shared-segment semantic identity, proposal, and each independent assent/decline fact and causal input identity;
- stable P18 `ActivityInstanceId`, current lifecycle state/revision, participant PersonIds, planned/actual interval, commitments and their revisions, terminal disposition, transition sequence, and pending due-work facts or sufficient owner state to rebuild them identically;
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
- compatible reservations commit per consent, conflicting intervals reject atomically, and failed formation/cancellation releases every affected commitment exactly once;
- P18 schedules one deterministic due start with two `PersonId` participants; same-instant cancellation or changed activity revision makes old work stale;
- successful joint start prepares both P8 transitions and P18 lifecycle update; each failpoint (first/second position validation, first/second route plan, either passage, coordination/P18 revision, commit guard) leaves both Persons and all coordination/lifecycle/commitment roots unchanged or takes the single documented failed-start terminal path;
- current passage truth is revalidated separately for both Persons even if their Knowledge/route plans match; one newly unavailable passage prevents either from starting;
- one Person's route/position can never substitute for the other's, there is no shared position, and effects/Knowledge remain per Person;
- individual transit/arrival can differ in order; P18 completion occurs only after both supported arrivals; cancellation/abort preserves already committed individual facts and releases remaining reservations without rollback;
- retries after committed and uncommitted operations use stable operation IDs and do not double-apply either travel start, arrival, or individual effect;
- clone/reconstruction fixtures restore stable links, partial proposals, reservations, scheduled starts, in-transit persons, staggered arrivals and causal ordering; indexes/queue rebuild deterministically;
- P12 `UnityBootstrap-Daily-v1` admission positively rejects a world containing this P20 owner state and continues to accept an empty/unrepresented-free state according to the explicit inventory contract;
- relevant `ActivityLifecycleTests`, `LogicalTimelineTests`, `ActorAvailabilityDecisionTests`, P8 generalized spatial travel/route planning, and existing TravelParty census behavior remain within their original profiles; no persistent `TravelParty` authority leaks into P20.

Candidate review must inspect full diff against this exact base, including semantic boundaries, prepared atomicity across timeline/P18/P20/two P8 Persons, deterministic ordering, stale/cancellation handling, exact P12 admission failure, and reconstruction state. Focused integration validation and the repository's risk-appropriate regression gate are required before promotion. This document is not self-review evidence.

## 10. Dependencies, exclusions, files, and sequence

**Hard promoted capability dependencies for implementation:** P18-A/B/C; P20-A coordination precedent; applicable P8-E explicit Person travel/position/passage. The architecture and contract decisions are accepted. Current `PHASE8_STATE.md` records P8-A through P8-E promoted; P8-E is an individual explicit-operation capability only. P11 is conditional on choosing typed external commands. There is no dependency on P18-D, P8 route planning beyond the one selected supported segment, P12 Save completion, P13 fork, P14 production, P15 construction, P19 loader, Group membership, or War.

**Likely implementation file boundary (subject to current exact-tip review):**

- New `JointCivilTravelContracts.cs`, `JointCivilTravelStore.cs`, `JointCivilTravelSystem.cs` (or a single tightly bounded owner file), and `JointCivilTravelCensusProvider.cs` if the P12 owner inventory convention requires it.
- A narrow prepared composite-start extension in `ActivityLifecycle.cs` / timeline integration, owned with the P18 hotspot owner; do not expand generic public lifecycle APIs beyond the real consumer requirement.
- Batch preparation/install seams in `P8ETravelTransactionCoordinator.cs` and its existing P8 spatial position/route authorities so two participants commit together under one runtime mutation boundary.
- Composition wiring in `SimulationRuntime.cs` only if needed to provide authoritative owners/timeline/guard; do not add participant loops to `AdvanceDay`.
- Focused tests under `Assets/_Project/Tests/EditMode/Editor/` for joint travel formation/start, abort/arrival, profile rejection, plus only necessary adjustments to existing owner tests.

**Integration sequence:**

1. Reconfirm current canonical P8/P18 State and code, capability promotion SHAs, approved P20-A details and clean integration base; reconcile missing owning P20 State evidence if the authoritative worktree still lacks it.
2. Implement/test isolated P20 coordination facts and consent/reservation transitions against P18 authorities.
3. In a separate serialized hotspot window, add the P18 due-start prepared commit seam and P8 two-Person batch preparation/install; review no partial-write paths before composition.
4. Compose and test the shared start, individual travel continuation/arrival, cancellation/abort and per-Person Knowledge/effects.
5. Add exact §92A export/hydration seams only if required by owner contract; add the fail-closed P12 daily-profile exclusion hook and negative rejection regression before candidate promotion. Do not implement save/load.
6. Run focused and required integration regressions, `git diff --check`, and independent exact-tip candidate review. Stop at normal candidate/promotion gates; technical-design completion alone is not authorization to implement or promote.

**Exclusions:** persistent Party/Group; generic activity framework or role/contribution solver; broad activity consumers; autonomous recruitment/social negotiation; multi-leg group routes; military travel/War; route-choice AI or route replanning; new travel economy/needs effects; automatic world-time advancement invented by P8; P11 external command ingress unless separately selected; save/load/fork implementation; enlargement of `UnityBootstrap-Daily-v1`; broad diagnostics/UI project; architecture or Phase-State amendment in this design branch.

**Freshness and review:** architecture and roadmap are read at base `f6924e63d8e5731da1d33021d0361e7defe6dad7`. Re-evaluate this design if architecture, P8/P18 promoted State/code, P20 owning State, or P12 admission inventory advances before implementation. Independent technical review remains required; current verdict: `DESIGN DRAFT — NOT REVIEWED`, so implementation readiness is not yet claimed.
