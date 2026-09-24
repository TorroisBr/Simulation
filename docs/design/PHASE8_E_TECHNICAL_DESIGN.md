# P8-E — Civil Travel Vertical Slice Technical Design

**Design base:** `01eebe0f11ca145e04e7c4cd065c124777edc8fd` (`codex/phase8/canonical`)
**Upstream semantic contract:** `5faa5817a11b0ae7412ec3ed98240fb1d633de11` (P8-B/C shared segment and identity contract)
**Reviewed P8-D design:** `8299bc080962e92f6324d570548a87b3b9586945`
**Candidate branch:** `codex/phase8/P8ETechnicalDesign`
**Scope:** technical design for the approved P8-E civil travel slice. This document changes no executable code, architecture, Brief, Roadmap, State, or tests.
**Readiness:** ready for independent design review; implementation is blocked until the relevant P8-B/C/D capabilities are promoted and their exact published APIs are available. At this design base, only P8-A is promoted.

## 1. Authority and bounded outcome

This design is subordinate to `docs/SIMULATION_ARCHITECTURE.md` §§5–6 and 69, `docs/phases/PHASE8_BRIEF.md`, `docs/PHASE8_STATE.md`, the accepted P8-B/C contract, and the reviewed P8-D design. It defines the application transaction boundary for one deterministic civil journey. It does not claim that P8-B, P8-C, P8-D, or this design is implemented.

The slice proves one PersonId-backed traveler can accept an actor-Knowledge-based route, depart, make deterministic segment progress, encounter a passage condition that changed after planning, receive only an observation supported by the attempt, stop at the last reached stable position, explicitly replan, and arrive. Position/progress remain solely P8-C facts; passage remains solely P8-B truth; route Knowledge and intent remain solely P8-D state. P8-E adds no competing position, passage, travel-plan, or legacy `TravelParty` authority.

The route graph and endpoints for this slice are Hex-only, following P8-D. The authored fixture may anchor Locations to the origin and destination Hexes, but arrival at a Hex does not enter, contain, or grant access to its Location. Location/Crossing connectors require an explicit later capability. The slice does not migrate or repurpose legacy fixed-`TravelDays` trips.

## 2. State ownership and application seam

| Fact or operation | Existing owner | P8-E responsibility |
|---|---|---|
| Registered Hexes, anchors, and scale | P8-A spatial authority | Read validated stable identities; do not create geography. |
| Current passage option and condition | P8-B passage authority | Revalidate the selected option against current truth at the attempt boundary. P8-E does not cache or change passage truth. |
| Person position and in-transit segment progress | P8-C position authority | Request validated position transitions/progress through its published API. Never mirror progress in a plan or NPC. |
| Actor observations, route candidates, accepted plan and plan status | P8-D Knowledge/plan authorities | Supply explicit decision inputs and transact the supported interruption/arrival status updates. Do not author observations from hidden truth. |
| Cross-authority travel command | P8-E application/transaction seam | Validate all participants before committing a logically indivisible operation; record one deterministic outcome. This seam is orchestration, not a second domain store. |

Use one world-authorized P8-E command/application boundary for starting travel, attempting the next segment, stopping after a rejected attempt, replanning, and arrival. Names below are conceptual; implementation binds to the exact APIs published by promoted B/C/D and does not recreate their types or authority. If those APIs do not provide the required atomic commit boundary, implementation waits for an explicit integration design rather than exposing partially committed state.

Each transaction captures the relevant world/store revisions and validates the PersonId, plan identity/revision, expected stable position, next typed boundary/option, and any explicitly supplied knowledge basis before mutation. It obtains a current B evaluation using the promoted movement-profile/world-context inputs. It then either commits every owned mutation or leaves all participant state and revisions unchanged. Read-only B evaluation is not itself a mutation. A B passage change is a separate world mutation and is never rolled back by a travel command.

The atomicity matrix is:

| Operation | Validation | Commit |
|---|---|---|
| Accept/replan | D candidate, policy, source Knowledge basis, origin/destination and PersonId are valid | D plan only. A rejected plan or preview changes nothing. |
| Depart/start a segment | D plan is current and explicitly accepted; C is `At(from)`; selected stable segment matches the plan; B currently permits the typed option for the explicit civil profile/context | C moves to the promoted in-transit representation and D marks the plan active in one commit. No travel progress is copied into D. |
| Progress current segment | C has matching transit identity/direction and D still has the matching active plan; deterministic progress input is valid | C progress only, unless this operation reaches an endpoint; endpoint position and any D completion/status update commit together. |
| Reject next segment | C is at the last fully reached stable position; D plan identifies the attempted next option; B current evaluation rejects it; observation evidence, if any, is explicitly actor-perceivable | Keep C at that position; atomically mark the plan interrupted and record only the supported D observation. If no observation is supported, record none. Do not advance, substitute an option, or expose the hidden cause. |
| Arrive | C is at the planned destination after the final valid segment and D still identifies the same active plan | C becomes `At(destination)` and D marks that plan complete in one commit. Arrival does not imply Location entry or topology discovery. |

Segment progress is owned by P8-C. At a stable Hex, the next segment is derived from the active D plan and C's current position; D does not acquire a mutable route cursor that could disagree with C. A successful endpoint transition uses P8-C's exact published transition rules. If those rules cannot represent the required segment boundary deterministically, that is an upstream integration blocker, not a reason for P8-E to add a parallel position record.

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

The actor then makes an explicit new route decision from factual origin `hex:hub` to `hex:destination` on day 21. Under the same scenario policy, the remaining estimates received day 20 are fresh (age one day), and D selects `hub → west → destination` (6 estimated days). This expected result is explicitly dependent on P8-D's pending KnownUnavailable eligibility rule: the day-21 observation must make the attempted `hub → east` option ineligible for route candidate selection. P8-E does not impose that filtering itself. If the promoted D contract does not exclude a currently KnownUnavailable option (or define equivalent selection behavior), this proving replan is blocked for D-owner resolution rather than silently assuming the alternate wins. P8-E accepts the new plan, starts each validated segment, advances deterministically using the exact promoted C progress API, and commits `At(hex:destination)` plus plan completion through the same travel transaction boundary. No route is switched automatically as a side effect of failure.

The fixture's travel-step schedule and B contextual evaluations are explicit input data in focused tests. They must not be derived from the actor-known estimates. Commands carry an explicit simulation day: plan acceptance/departure is day 20; the progress input that reaches `hex:hub`, passage mutation, rejected next-segment attempt, supported generic observation, and explicit replan are ordered on day 21 as described above. The supported progress unit, movement profile, contextual evaluation inputs, and progress-per-step values must use the exact promoted B/C contracts; this design does not invent a universal formula. The proving runner invokes the travel command with an explicit simulation day/input. P8-E does not require `SimulationRuntime.AdvanceDay` integration or autonomous daily travel processing; add that only if an accepted temporal semantic cannot be met by the explicit operation, and then obtain a separate review of ordering and long-run consequences.

## 4. Execution ordering and failure behavior

For any requested next segment, order work as follows:

1. Read/validate the exact active plan revision, PersonId, stable plan segment and candidate, source Knowledge basis where required, and current C factual position/progress.
2. Resolve the segment's stable typed boundary and `TraversalOptionRef` through promoted B; query the current factual condition with explicit movement profile/context. Never ask Knowledge to refresh itself from B.
3. On success, request the single C position/progress transition. If the operation reaches a stable destination or completes the plan, commit its D status change in the same transaction.
4. On rejection, keep C at the last fully reached stable position. Only an explicit supported-observation value can request a D Knowledge update. Commit interruption and that optional observation together. A rejected or stale multi-authority mutation leaves all participating state/revisions unchanged.
5. Replanning is a separate explicit decision against actor Knowledge and the current stable origin. It can select a route only from known candidates and known estimates under a named policy. It cannot inspect B to remove a stale path or choose an alternate.

No transaction uses collection/discovery order, RuntimeIds, loaded NPC instances, rendering coordinates, current truth as a route score, or authoritative RNG to resolve a tie. Equal causal inputs and commands produce equal outcomes independent of registration order and materialization state. Failure messages and D observations expose no cause absent explicit evidence. Diagnostics must expose enough stable plan/position/knowledge state and mutation outcome to reconstruct this slice without becoming a save/replay implementation.

## 5. Identity, reconstruction, and ownership

All traveler state and plan/Knowledge association use stable `PersonId`. The position and transit values use only the B/C contract's stable `HexId`, `LocationId`, `CrossingId`, boundary, and typed traversal-option identities. No `NpcRuntime`, `RuntimeId`, `SubLocation`, anchor inference, or renderer coordinate may define route identity, progress, or persisted causality. Cloning and stable diagnostics for authoritative state are requirements of the owning B/C/D stores and their integration; P8-E adds no duplicate clone projection.

P8-E's future implementation branch owns only the travel application transaction and its focused command-level tests, using exact published B/C/D APIs. A single named integrator owns runtime composition and cross-authority diagnostics/invariant changes. No parallel writer edits `SimulationRuntime`, the daily loop, shared diagnostics, B/C passage/position stores, or D Knowledge/plan stores. If the P8-E operation requires a new B/C/D primitive, publish that gap and return it to the owning checkpoint/API review; do not fork a second authority locally.

## 6. Focused acceptance evidence

Before integration, focused P8-E tests should prove:

- accepting or previewing a route does not change C position or B passage state;
- the explicit policy chooses the 4-day actor-known candidate over the 7-day candidate, independently of insertion order, and does not use B truth or actual contextual costs to plan;
- missing/stale estimates and stale plan/position revisions fail without mutation;
- departure couples plan activation and C transit atomically; progress changes only C; rejection after `At(hub)` preserves that exact position and does not advance progress;
- changing the Stone Bridge after planning causes current B revalidation to reject it even though D still held the earlier belief;
- generic rejection does not reveal or store a cause; supported evidence records only that the attempted option was unusable at the boundary on the attempt day;
- interrupted-plan status and the optional observation commit atomically, while an unsupported observation leaves Knowledge unchanged;
- replan on day 21 is a new explicit decision from `hex:hub`, selects only the alternate known candidate under fresh supplied estimates if and only if P8-D's promoted KnownUnavailable eligibility rule excludes the failed option, and never auto-switches during rejection;
- day-20 estimates are fresh at planning day 20 and day 21 under the exact inclusive rule; replacing a required estimate with one received day 19 makes day-21 route selection fail with a typed insufficient/stale-estimate result and no selected route;
- final C arrival and D plan completion commit together through the one P8-E transaction boundary;
- clones, dormant/unmaterialized Person representation, stable projections, invariants, and command outcomes retain identical semantic identities and deterministic results;
- legacy `TravelSystem`/`TravelParty` tests remain unchanged and those authorities never report position/progress for this P8-E Person trip.

Run focused P8-E EditMode coverage and relevant P8-A geography, promoted P8-B passage, P8-C position, P8-D Knowledge/plan, Phase 7 spatial/Battle/ArmedForce, and legacy Travel regressions during implementation/integration. Follow the then-current Phase 8 promotion gate, including required full EditMode and official complete Smoke, and `git diff --check`. Long-run testing is not implied by this design because it adds no autonomous daily-loop behavior; reassess if implementation adds that behavior.

## 7. Dependencies, unresolved choices, and verdict

**Hard implementation gate:** the relevant P8-B, P8-C, and P8-D capabilities must be promoted and independently validated; the implementation must bind to their exact published stable identities, transaction/mutation conventions, route-plan status semantics, actor-observation shape, B contextual evaluation, and C progress/arrival APIs. The accepted B/C contract and reviewed D design enable design work but do not satisfy this gate. P8-E must not treat candidate branches as capabilities.

**Bounded by this design:** the proving policy and estimates above are scenario data; the route endpoints are Hexes; the changed crossing is rejected from the stable adjacent Hex; observation is generic and evidence-bounded; replan is explicit; no local Location entry is inferred; and no automatic daily execution is added.

**Unresolved for upstream publication or later product direction:**

- P8-C's final deterministic progress representation and rate/context semantics, plus P8-B's exact movement-profile and contextual evaluation API, are not promoted at this base. P8-E must use those published capabilities rather than pick an incompatible parallel formula.
- The exact P8-D plan status/interruption/replacement and observation value APIs are not yet published. P8-E may not assume names or write those states independently.
- What sensory evidence justifies a cause-specific explanation (for example, distinguishing an unusable crossing from a visibly destroyed bridge) remains a product/perception choice. This slice needs only explicit evidence for generic unusability; it does not decide broader perception rules.
- Whether players/actors may choose another route objective or request route comparison outside this fixture remains open. The scenario-local minimum known estimated-days policy is not a product default.
- Entry from/to a Location or Crossing, local connectors, and whether arrival automatically begins site-level observation are outside this Hex-only P8-E scenario; the design does not settle those later consumers.
- If product intent requires travel to advance without explicit travel commands, the temporal invocation/order with `AdvanceDay` needs a separate semantic decision and review. This design leaves it out.

**Verdict:** the bounded P8-E scenario is ready for independent technical-design review and does not require an unresolved product decision to review. It is **not implementation-ready** at this base: P8-B/C/D are not promoted in the current Phase 8 State, and their exact published APIs are required before code can begin. Preserve that dependency gate in any implementation planning.
