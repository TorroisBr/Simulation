# P8-E — Civil Travel Vertical Slice Technical Design

**Design base:** `c5b2e06b534f4b2af38f10e6510b10800aa8b28c` (`codex/phase8/canonical`)
**Upstream semantic contract:** `5faa5817a11b0ae7412ec3ed98240fb1d633de11` (P8-B/C shared segment and identity contract)
**Reviewed P8-D design:** `6800d3d289e2f8be730f082ee7457c518ed22050`
**Prior reviewed design:** `4b7127f57d354c851e4d8ddaaeb27e8fbd51686c` (`codex/phase8/P8ETechnicalDesign`)
**Refresh branch:** `codex/phase8/P8EApiLifecycleRefresh`
**Scope:** reconcile the approved P8-E civil travel design to canonical P8-B/C/D APIs after P8-D promotion and the 2026-09-26 intraday/extensibility and multi-participant alignments. This document changes no executable code, architecture, Brief, Roadmap, State, or tests.
**Readiness:** design refresh for independent review. The required B/C/D capabilities are promoted, but P8-E implementation remains blocked on the prepared-mutation API contract specified below.

## 1. Authority and bounded outcome

This design is subordinate to `docs/SIMULATION_ARCHITECTURE.md` §§5–6, 11, 12 and 69, `docs/phases/PHASE8_BRIEF.md`, `docs/PHASE8_STATE.md`, the accepted P8-B/C contract, and the reviewed/promoted P8-D design. It defines the application transaction boundary for one deterministic civil journey. P8-A through P8-D are promoted capabilities; this design does not claim P8-E is implemented.

The slice proves one PersonId-backed traveler can accept an actor-Knowledge-based route, depart, make deterministic segment progress, encounter a passage condition that changed after planning, receive only an observation supported by the attempt, stop at the last reached stable position, explicitly replan, and arrive. Position/progress remain solely P8-C facts; passage remains solely P8-B truth; route Knowledge and intent remain solely P8-D state. P8-E adds no competing position, passage, travel-plan, or legacy `TravelParty` authority.

The route graph and endpoints for this slice are Hex-only, following P8-D. The authored fixture may anchor Locations to the origin and destination Hexes, but arrival at a Hex does not enter, contain, or grant access to its Location. Location/Crossing connectors require an explicit later capability. The slice does not migrate or repurpose legacy fixed-`TravelDays` trips. Its one-Person operation is a bounded consumer profile, not a universal Activity cardinality or a permanent Activity-to-Actor binding. P18-D may later consume the promoted explicit travel capability for automatic intraday execution; this slice adds no daily progression, one-action-per-day rule, or blanket P18 prerequisite. P20 adds no dependency to this individual traveler. Extensibility remains a current review constraint, but no speculative hook, registry, loader, or mod API is introduced here.

## 2. State ownership and application seam

| Fact or operation | Existing owner | P8-E responsibility |
|---|---|---|
| Registered Hexes, anchors, and scale | P8-A spatial authority | Read validated stable identities; do not create geography. |
| Current passage option and condition | P8-B passage authority | Revalidate the selected option against current truth at the attempt boundary. P8-E does not cache or change passage truth. |
| Person position and in-transit segment progress | P8-C position authority | Request validated position transitions/progress through its published API. Never mirror progress in a plan or NPC. |
| Actor observations, route candidates, accepted plan and plan status | P8-D Knowledge/plan authorities | Supply explicit decision inputs and transact the supported observation and lifecycle updates. Do not author observations from hidden truth. |
| Cross-authority travel command | P8-E application transaction coordinator | Sole owner of the logical commit spanning prepared C/D changes; it creates no parallel domain store and records one deterministic outcome. |

### Current API reconciliation and required commit seam

Promoted P8-B exposes factual passage evaluation; P8-C exposes individually guarded `TryBeginTransit`, `TryAdvanceTransit`, and `TryArrive`; P8-D exposes actor-scoped observations/Knowledge basis and a `PersonRoutePlanStore.TryAcceptPlan`. The current D enum is only `Active`, `Superseded`, and `Interrupted`; acceptance immediately writes `Active`, and D has no transition/completion or atomic interruption-plus-observation API. C and D calls each mutate their own store and revision. Therefore the earlier design's assumed coupled calls cannot be made all-or-nothing by simply invoking these public methods in sequence.

P8-E should own one `P8ETravelTransactionCoordinator` (name is conceptual) as the only application writer for the cross-authority travel commands. It does not own the underlying facts. The coordinator captures C, D-plan, and D-Knowledge revisions plus the exact PersonId, plan key/revision, expected position/transit, selected segment and explicit evidence. It obtains a read-only current B evaluation and asks each owning authority to prepare an immutable change. Preparation validates all current preconditions and precomputes the next store snapshots and revisions without mutating any store. Commit runs synchronously under the runtime's authoritative mutation boundary, rechecks every captured revision, then installs the already-prepared C and D snapshots using internal, no-fail state-root/snapshot swaps. These commit primitives must perform no validation, callbacks, allocation, or other operation that can reject after the first swap. If preparation or revision recheck fails, nothing changes. All supported mutation entrypoints and reads of participating state are serialized against this boundary, so no supported observer can see an intermediate subset; if concurrent readers are later supported, publish the participating values through one immutable transaction-state root instead. This is a narrow domain transaction, not a general transaction framework.

The currently published APIs do not provide these prepared changes. Before P8-E implementation, the owning C/D contracts must expose narrowly scoped prepare/apply primitives (or one equivalent P8-E-owned internal transaction port): C preparation for begin/arrival, D plan preparation for accept/activate/complete/interruption/supersession, and D Knowledge preparation for the optional observation. They must return immutable expected-revision-bound changes; applying a complete prepared set must be guaranteed no-fail after the coordinator's final revision check. The D lifecycle/API change belongs to P8-D ownership; C's existing individual operations remain authoritative and should gain transaction preparation without changing their semantics. If the integrated runtime cannot provide serialized no-fail snapshot installation, P8-E is blocked for that exact missing commit contract; sequential public `Try*` calls, compensating writes, rollback-after-failure, or clone-and-replace of whole runtime stores are not acceptable substitutes.

A B passage change is a separate world mutation and is never rolled back by a travel command. Passage evaluation occurs before transaction preparation and is revalidated at the attempt boundary. The transaction does not mutate passage truth.

### Plan lifecycle

P8-D plan history must support `Accepted`, `Active`, `Completed`, `Interrupted`, and `Superseded`. These values describe intent lifecycle, not C position. The current `TryAcceptPlan` behavior that immediately creates an `Active` plan must be replaced by acceptance as `Accepted`; acceptance/replanning does not mean physical departure.

| Transition | Meaning and owner |
|---|---|
| none → `Accepted` | P8-D accepts a selected, current Knowledge-based plan. C position is unchanged. If an earlier `Accepted` plan is explicitly replaced, its terminal status becomes `Superseded` in the same D prepared change. |
| `Accepted` → `Active` | P8-E departure transaction couples C `At(from)` → transit with plan activation. |
| `Active` → `Completed` | P8-E final-arrival transaction couples C transit → `At(destination)` with completion. Completion is never represented as supersession. |
| `Active` → `Interrupted` | P8-E commits a rejected next-segment attempt at the last reached stable position, with an optional supported observation. |
| `Accepted` or stable-position `Active` → `Superseded` | An explicit replacement decision supersedes a still-viable old intent. For `Active`, C must be `At` a stable Hex; while C is in transit, replacement is rejected until that segment reaches a stable endpoint or a separately designed C stop operation exists. |

Terminal plans (`Completed`, `Interrupted`, `Superseded`) never become active again. A new accepted decision creates a new revision/history item. Rejection does not silently create a replacement route or update an estimate. Plan history remains keyed by persistent `PersonId`; no NpcRuntime or Activity instance owns it.

The atomicity matrix is:

| Operation | Validation | Commit |
|---|---|---|
| Accept/replan | D candidate, policy, source Knowledge basis, origin/destination and PersonId are valid; any replaced active plan is at a stable position | D writes new `Accepted` and any prior-plan `Superseded` transition as one prepared D change. C is unchanged. Preview/rejection changes nothing. |
| Depart/start a segment | D plan is `Accepted` at origin or `Active` at the reached segment origin; C is `At(from)`; selected stable segment matches the plan; B currently permits the typed option for explicit civil profile/context | C begins transit and D changes `Accepted` → `Active` (or keeps `Active`) in one prepared transaction. No progress is copied into D. |
| Progress current segment | C has matching transit identity/direction and D has the matching `Active` plan; explicit deterministic progress input is valid | C progress only. Reaching an intermediate Hex is a single C mutation; D remains `Active`. |
| Reject next segment | C is `At` the last fully reached stable Hex; D plan is `Active` and identifies the attempted option; current B evaluation rejects it; optional evidence is explicit and actor-perceivable | Keep C position unchanged; D changes `Active` → `Interrupted` and optionally records the supported observation in one prepared transaction. No hidden cause, progress, substitute option, or automatic replan. |
| Arrive | C transit is complete at the planned final destination and D still identifies the matching `Active` plan | C becomes `At(destination)` and D changes `Active` → `Completed` in one prepared transaction. Arrival does not imply Location entry or topology discovery. |

Segment progress is owned by P8-C. At a stable Hex, the next segment is derived from the D plan and C's current position; D does not acquire a mutable route cursor that could disagree with C. A successful endpoint transition uses P8-C's exact published transition rules. Current C progress accepts explicit ticks up to its fixed 0..1000 range; P8-E supplies those values as command input and invents no speed/time law. Reaching 1000 is not arrival by itself: P8-C `TryArrive` must validate the endpoint and is prepared with P8-D completion. If current C/D prepared operations cannot represent this deterministically, it is the precise API blocker above, not a reason for P8-E to add a parallel position record.

## 3. Deterministic proving scenario

Use one manually authored finite scenario fixture with a persistent civil Person `person:traveler_01`, origin `hex:origin`, destination `hex:destination`, and five registered Hexes with stable identities and semantic coordinates:

| Fixture Hex | Axial coordinate | Role |
|---|---:|---|
| `hex:origin` | `(0, 0)` | Initial factual position. |
| `hex:hub` | `(1, 0)` | First reached intermediate Hex and stop location. |
| `hex:east` | `(2, 0)` | Intermediate Hex on the initially selected route. |
| `hex:west` | `(1, -1)` | Intermediate Hex on the alternate route. |
| `hex:destination` | `(2, -1)` | Planned Hex destination. |

The actor knows exactly these two complete candidates initially:

1. `origin → hub → east → destination`, whose `hub → east` segment uses the stable Crossing option `crossing:stone-bridge` on its typed Hex boundary.
2. `origin → hub → west → destination`, using the explicitly authored non-Crossing traversal options on its two alternate boundaries.

Other geometrically neighboring boundaries may exist in the fixture, but they are not silently added to actor Knowledge or route candidates. Any factual traversal option on them is separately authored and is not known to this actor. The selected option on each route is a full accepted typed `TraversalOptionRef`; the labels above are readable fixture names, not substitute runtime identities.

For this scenario only, provide the stable P8-D selection policy `scenario:minimum-known-estimated-days/v1`. It minimizes the sum of the actor's current known segment estimates expressed in the fixture's `estimatedMovementDays` unit; missing, invalid, or stale required estimates fail planning. An estimate is fresh exactly when `0 <= planningDay - ReceivedDay <= 1`; the maximum age is one day inclusive, measured from `ReceivedDay` (not `ObservedDay`). An estimate received in the future relative to `planningDay` is invalid. The initial plan is accepted on day 20 and the replan is evaluated on day 21, so estimates received on day 20 are fresh for both. Exact knowledge inputs received on day 20 are:

| Directed segment and option | Actor-known estimate | Observed day | Received day | Stable source / provenance |
|---|---:|---:|---:|---|
| `origin → hub` / authored option `option:origin-hub` | 1 day | 18 | 20 | `source:guide-01` / `prov:guide-01-origin-hub` |
| `hub → east` / `crossing:stone-bridge` | 2 days | 18 | 20 | `source:guide-01` / `prov:guide-01-stone-bridge` |
| `east → destination` / authored option `option:east-destination` | 1 day | 18 | 20 | `source:guide-01` / `prov:guide-01-east-destination` |
| `hub → west` / authored option `option:hub-west` | 3 days | 18 | 20 | `source:guide-01` / `prov:guide-01-hub-west` |
| `west → destination` / authored option `option:west-destination` | 3 days | 18 | 20 | `source:guide-01` / `prov:guide-01-west-destination` |

The policy therefore ranks the first candidate at 4 known estimated days and the alternate at 7; it selects the first on day 20. If equal, P8-D's accepted stable candidate-sequence tie-break applies. As a stale-estimate control, replace the `hub → west` estimate's `ReceivedDay` with day 19 and evaluate on day 21: its age is two days, so route selection fails with P8-D's typed `InsufficientKnownEstimate`/`PolicyUnavailable` result and selects no route. This control proves the inclusive freshness boundary independently of passage truth. The policy and control are fixture content, not a universal route objective, factual travel duration, speed law, safety/cost policy, or claim about actual traversal cost. Execution uses the current P8-B factual evaluation and P8-C's promoted progress contract; actual contextual cost/progress is never copied from these estimates.

The passage fixture initially reports the Stone Bridge option usable for the explicit civil movement profile. On day 20, the traveler accepts the selected plan and departs from `hex:origin`. On day 21, a deterministic P8-C progress input completes `origin → hub`; once C reports `At(hex:hub)`, and before any attempt of `hub → east`, a separate authorized world mutation changes the bridge condition so B rejects that option. The next-segment attempt, generic observation, and explicit replan all occur on day 21. This ordering ensures the current position is stably `At(hub)` when the changed crossing is attempted; it does not require inventing what happens to someone already traversing a crossing whose truth changes mid-segment.

The attempt returns only a generic factual rejection for the selected option at that boundary. The scenario supplies explicit observation evidence that this traveler can perceive that the attempted crossing is currently unusable; it supplies no evidence that the actor can identify why. D records the supported attempt observation with the current day/provenance. It must not record “bridge destroyed” or any cause-specific explanation from the rejection alone. The route Knowledge now excludes or marks unavailable that known traversal according to the exact promoted D observation semantics; it does not mutate P8-B.

The actor then makes an explicit new route decision from factual origin `hex:hub` to `hex:destination` on day 21. Under the same scenario policy, the remaining estimates received day 20 are fresh (age one day), and D selects `hub → west → destination` (6 estimated days). Promoted P8-D implements the `KnownUnavailable` eligibility rule: the day-21 observation makes the attempted `hub → east` option ineligible for route candidate selection. P8-E relies on that owner behavior and does not duplicate the filtering. P8-E accepts the new plan, starts each validated segment, advances deterministically using the exact promoted C progress API, and commits `At(hex:destination)` plus plan completion through the same travel transaction boundary. No route is switched automatically as a side effect of failure.

The fixture's travel-step schedule and B contextual evaluations are explicit input data in focused tests. They must not be derived from the actor-known estimates. Commands carry an explicit simulation day: plan acceptance/departure is day 20; the progress input that reaches `hex:hub`, passage mutation, rejected next-segment attempt, supported generic observation, and explicit replan are ordered on day 21 as described above. The supported progress unit, movement profile, contextual evaluation inputs, and progress-per-step values must use the exact promoted B/C contracts; this design does not invent a universal formula. The proving runner invokes the travel command with an explicit simulation day/input. P8-E does not require `SimulationRuntime.AdvanceDay` integration or autonomous daily travel processing; add that only if an accepted temporal semantic cannot be met by the explicit operation, and then obtain a separate review of ordering and long-run consequences.

## 4. Execution ordering and failure behavior

For any requested next segment, order work as follows:

1. Read/validate the exact `Accepted` or `Active` plan revision, PersonId, stable plan segment and candidate, source Knowledge basis where required, and current C factual position/progress. `Accepted` may depart only from its route origin; `Active` may continue only from its last reached segment boundary.
2. Resolve the segment's stable typed boundary and `TraversalOptionRef` through promoted B; query the current factual condition with explicit movement profile/context. Never ask Knowledge to refresh itself from B.
3. On success, prepare the C begin/progress/arrival transition. A departure prepares `Accepted` → `Active`; final arrival prepares `Active` → `Completed`. Commit the complete prepared set through the P8-E coordinator.
4. On rejection, C must already be at the last fully reached stable position. Prepare `Active` → `Interrupted` and only an explicitly supported D observation, then commit them together. A rejected or stale multi-authority mutation leaves every participating state and revision unchanged.
5. Replanning is a separate explicit decision against actor Knowledge and the current stable origin. It can select a route only from known candidates and known estimates under a named policy. It cannot inspect B to remove a stale path or choose an alternate.

No transaction uses collection/discovery order, RuntimeIds, loaded NPC instances, rendering coordinates, current truth as a route score, or authoritative RNG to resolve a tie. Equal causal inputs and commands produce equal outcomes independent of registration order and materialization state. Failure messages and D observations expose no cause absent explicit evidence. Diagnostics must expose enough stable plan/position/knowledge state and mutation outcome to reconstruct this slice without becoming a save/replay implementation.

## 5. Identity, reconstruction, and ownership

All traveler state and plan/Knowledge association use stable `PersonId`. The position and transit values use only the B/C contract's stable `HexId`, `LocationId`, `CrossingId`, boundary, and typed traversal-option identities. No `NpcRuntime`, `RuntimeId`, `SubLocation`, anchor inference, or renderer coordinate may define route identity, progress, or persisted causality. Cloning and stable diagnostics for authoritative state are requirements of the owning B/C/D stores and their integration; P8-E adds no duplicate clone projection.

P8-E's future implementation branch owns only the travel application transaction and its focused command-level tests, using exact published B/C/D APIs. A single named integrator owns runtime composition and cross-authority diagnostics/invariant changes. No parallel writer edits `SimulationRuntime`, the daily loop, shared diagnostics, B/C passage/position stores, or D Knowledge/plan stores. If the P8-E operation requires a new B/C/D primitive, publish that gap and return it to the owning checkpoint/API review; do not fork a second authority locally.

## 6. Focused acceptance evidence

Before integration, focused P8-E tests should prove:

- accepting creates `Accepted` without changing C position or B passage state; preview changes nothing; explicit replacement produces `Superseded` only for the old plan and a new `Accepted` plan;
- the explicit policy chooses the 4-day actor-known candidate over the 7-day candidate, independently of insertion order, and does not use B truth or actual contextual costs to plan;
- missing/stale estimates and stale plan/position revisions fail without mutation;
- lifecycle transitions are exact: `Accepted` → `Active` at departure, `Active` → `Completed` only with final C arrival, `Active` → `Interrupted` on rejected next segment, and explicit replacement → `Superseded`; completion is never encoded as supersession;
- departure couples plan activation and C transit atomically; progress changes only C; rejection after `At(hub)` preserves that exact position and does not advance progress;
- changing the Stone Bridge after planning causes current B revalidation to reject it even though D still held the earlier belief;
- generic rejection does not reveal or store a cause; supported evidence records only that the attempted option was unusable at the boundary on the attempt day;
- interrupted-plan status and the optional observation commit atomically, while unsupported observation evidence leaves Knowledge unchanged;
- stale C/D/Knowledge revisions or any failed prepare leave every participating value and revision unchanged; no test relies on rollback after one participant has already mutated;
- replan on day 21 is a new explicit decision from `hex:hub`, selects only the alternate known candidate under fresh supplied estimates using promoted P8-D `KnownUnavailable` filtering, and never auto-switches during rejection;
- day-20 estimates are fresh at planning day 20 and day 21 under the exact inclusive rule; replacing a required estimate with one received day 19 makes day-21 route selection fail with a typed insufficient/stale-estimate result and no selected route;
- final C arrival and D plan completion commit together through the one P8-E transaction boundary;
- clones, dormant/unmaterialized Person representation, stable projections, invariants, and command outcomes retain identical semantic identities and deterministic results;
- legacy `TravelSystem`/`TravelParty` tests remain unchanged and those authorities never report position/progress for this P8-E Person trip.

Run focused P8-E EditMode coverage and relevant P8-A geography, promoted P8-B passage, P8-C position, P8-D Knowledge/plan, Phase 7 spatial/Battle/ArmedForce, and legacy Travel regressions during implementation/integration. Follow the then-current Phase 8 promotion gate, including required full EditMode and official complete Smoke, and `git diff --check`. Long-run testing is not implied by this design because it adds no autonomous daily-loop behavior; reassess if implementation adds that behavior.

## 7. Dependencies, unresolved choices, and verdict

**Hard implementation gate:** P8-B/C/D are promoted and their current stable identities, contextual evaluation, observation, Knowledge basis, and position/progress APIs are available. Before P8-E code starts, independently review the D plan lifecycle expansion and prepared-mutation contract required from C, D plan, and D Knowledge owners. The coordinator must commit prepared immutable changes under one serialized authoritative mutation boundary with a final expected-revision check and no-fail snapshot installation. Existing direct public `Try*` calls do not satisfy that requirement. Do not treat design or candidate branches as implemented capability.

**Bounded by this design:** the proving policy and estimates above are scenario data; the route endpoints are Hexes; the changed crossing is rejected from the stable adjacent Hex; observation is generic and evidence-bounded; replan is explicit; no local Location entry is inferred; and no automatic daily execution is added.

**Unresolved for upstream publication or later product direction:**

- Current C progress is a fixed 0..1000 explicit tick value; it defines no speed/time law. P8-E must use explicit progress command input and exact B contextual evaluation, without deriving progress from Knowledge estimates.
- Current P8-D has no `Accepted`/`Completed` lifecycle, transition methods, or atomic plan-plus-observation operation. The owning P8-D API must add the designed lifecycle and prepare methods. Current C and D public mutations apply immediately and independently; the owning APIs must provide expected-revision-bound prepared changes and no-fail installation so P8-E can guarantee all-or-nothing. Until reviewed, P8-E is not implementation-ready.
- What sensory evidence justifies a cause-specific explanation (for example, distinguishing an unusable crossing from a visibly destroyed bridge) remains a product/perception choice. This slice needs only explicit evidence for generic unusability; it does not decide broader perception rules.
- Whether players/actors may choose another route objective or request route comparison outside this fixture remains open. The scenario-local minimum known estimated-days policy is not a product default.
- Entry from/to a Location or Crossing, local connectors, and whether arrival automatically begins site-level observation are outside this Hex-only P8-E scenario; the design does not settle those later consumers.
- Automatic intraday execution/availability remains a P18-D consumer of the completed P8-E capability; the bounded P8-E explicit-operation slice has no `AdvanceDay` integration. No P18-A/B/C or P20 prerequisite is added to this slice.

**Verdict:** the existing one-Person scenario remains compatible with the latest temporal and multi-participant alignments. P8-B/C/D are promoted and the scenario's route, passage, Knowledge, identity, and progress capabilities exist. This refresh is **not implementation-ready** until the P8-D lifecycle expansion and expected-revision-bound prepared-mutation/no-fail commit port are specified, implemented by the owning authorities, and independently reviewed. That is a bounded API/integration contract gap, not an unresolved product decision. The explicit-operation scope adds no P18/P20 gate; automatic intraday behavior remains downstream of P18-D.
