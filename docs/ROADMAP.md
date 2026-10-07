# Simulation roadmap — planning, not world architecture

`SIMULATION_ARCHITECTURE.md` remains the semantic authority. This roadmap names intended phase scopes and likely dependency directions; it neither reports delivered behavior nor authorizes implementation or canonical promotion. The owning `PHASE*_STATE.md` and current canonical code establish delivery. Phase numbers primarily organize planning and closure, not a requirement to execute whole phases serially.

Phases 5–7 are closed within their documented scopes. P8-A/B/C/D/E are canonical; `PHASE8_STATE.md` records Phase 8 delivery and remaining readiness. Phases 9–17 retain their IDs and scopes. Phases 18–20 introduce intraday execution, the later code-mod platform and multi-participant activities; their numbers do not put them after strategic War in execution order. See the Phase Briefs and `EXECUTION_MODEL.md` for readiness and scheduling.

| Phase | Planning objective |
|---|---|
| 8 — Spatial Truth & Civil Travel v1 | Factual Hex geography and a Knowledge-bounded civil travel vertical slice. |
| 9 — Initial Deterministic Genesis v1 | **Closed within approved scopes.** P9-A: promoted authored-bootstrap genesis. P9-B: promoted at `d9a62d7`; one authored P8-A Hex/Location source for P10 through the same deterministic pre-start pipeline. The approved closure record is canonical at `3bd0eb3`; no procedural generation, Ruin/topology, population/backstory, runtime expansion, or Mod API/loader scope is implied. |
| 10 — Local Generation & Pre-start Authoring | Local content/topology generated or authored into the same initial ontology. |
| 11 — Actor Perspective & Commands v1 | Actor-limited information/decision authority and validated external commands. |
| 12 — Save & Deterministic Continuation | Save/load that continues with the same authoritative future under compatible inputs. |
| 13 — Historical Reconstruction & Fork | Reconstruct and independently continue any actually simulated boundary. |
| 14 — Productive Sources & Material Flow v1 | Bounded productive-source and material-flow truth. |
| 15 — Runtime Construction & Founding v1 | Factual creation of structures/settlements during simulated history. |
| 16 — Military Movement & Logistics v1 | Physical force movement and meaningful logistical constraints. |
| 17 — Strategic War v1 | Strategic War progression without equating Battle result with War result. |
| 18 — Intraday Temporal Execution v1 | One logical timeline, deterministic due-work scheduling, activity lifecycle and availability-driven actor decisions, with bounded legacy integration. |
| 19 — Code Mods & Public Extension Surface v1 | A later code-mod API/loader and supported extension lifecycle, including new mechanics/state and optional explicit existing-world retrofit. |
| 20 — Multi-participant Activities v1 | Temporary shared activity instances, independent participation decisions, future commitments and validated coordinated start/execution over the temporal foundation. |

Runtime World Expansion remains a future consumer without an assigned dedicated phase. Its absence from this sequence does not forbid a later insertion or assign it to Phase 15 by implication.

## P17 strategic War direction and first bounded proof — proposed 2026-10-05

The accepted product direction includes territorial, attrition/capability and
non-territorial/coercive War goals in one persistent strategic model. They are
not competing definitions of War. The [P17 architecture and technical-entry
record](architecture/P17_STRATEGIC_WAR_DIRECTION.md) establishes P17-A —
Explicit Withdrawal Demand and Explicit War Termination — as the smallest
first proof. P17-A is `READY_FOR_TECHNICAL_DESIGN`, not implementation. The
direction was independently reviewed and promoted at `6a1b3ef4d0ea32c109fcfb434d3ee00d05cf589a`.

P17-A starts with an existing two-sided P7 War, two Faction participants and
one ArmedForce bound to each side. Faction A's one actual goal is for B's
selected force to leave a specified Hex through P16-A's one validated passage
crossing. The committed P16 receipt supplies the operational evidence. Goal
satisfaction does not end War; a separate explicit concession input ends this
bounded two-participant War with a recorded reason. No control, occupation,
sovereignty, jurisdiction or ownership changes follow from the crossing or a
Battle. The two participants and one goal are fixture limits only.

```text
P7 persistent War/force bindings + Faction identity + P8 passage truth
  + promoted P16-A one-hop movement/receipt
  → P17-A bounded technical design → reviewed implementation candidate
P17-A War state + §92A owner entry + P12 selected-profile exclusion/rejection
  → P17-A domain promotion gate (no full-P12 prerequisite)
P17-A + explicit military-control/occupation authority
  → later territorial War goal consumer
P17-A + P7/P14/P16 capability facts + participant Knowledge/decisions
  → later bounded attrition/capability consumer
P12 exact chosen-profile continuation + recoverable War/P16 inputs
  → supported P13 historical reconstruction/fork for that profile
P19 loader and P20 small-group Activity → no P17-A dependency
```

P7 `PersistentWarStore` remains the only War authority; Battle, movement,
supply and political/territorial facts remain with their own owners.
P17-A introduces no universal War winner, exhaustion meter, diplomacy,
government, Campaign or War AI. Its demonstrability classification is
`FOLLOW-UP_DEMONSTRATION`: a later Lab scenario should show the goal before and
after the real crossing, War continuing after fulfillment, then ending for an
explicit reason. The Lab path is not a hidden gate on the domain checkpoint;
prefer the human scenario before formal Phase 17 closure when practical.
Implementation must serialize War/runtime/P16/P12 hotspots with active Master
work. The earlier P17 `DEFERRED` entries below describe their dated planning
snapshots and are superseded by this promoted direction.

## Simulation Lab and human demonstrability — approved supporting direction

The [Simulation Lab direction and dependency assessment](architecture/SIMULATION_LAB_DIRECTION.md)
adds a cross-cutting demonstrability check to **future** bounded checkpoint
designs. It establishes one cumulative, Unity-independent scenario host as an
unnumbered supporting workstream, now `READY_FOR_TECHNICAL_DESIGN`, not
`IMPLEMENTATION_READY` or a new numbered Phase. The host consumes existing
Simulation authorities and read capabilities; its first executable technical
scope requires separate design, review and implementation authorization. It
does not make a production UI, P19 loader, whole-World exporter or full P12
save profile a common prerequisite.

The recommended first useful scenario consumes the already promoted P14-B
finite-source capability: run a small world, advance it and observe source
reserve, produced stock and the exhaustion boundary. The Lab scenario still
needs a reviewed non-Unity composition/read path and is not delivered by the
domain promotion. It adds no retroactive requirement to P14-B. A smaller
delivered capability may be used to prove host startup and authority
reuse without falsely demonstrating finite depletion. Subsequent consumers
attach only after their own capabilities and readable facts exist:

| Lab consumer | Human observation | Specific gate; no automatic Phase dependency |
|---|---|---|
| P10 generated topology | Seeded site topology and stable site identity, preferably as a graph. | P10-B is promoted; Lab still needs non-Unity composition and a supported topology read. |
| P14 material flow | Reserve, production, stock and exhaustion. | P14-B is promoted; Lab still needs coherent reserve/stock observation and non-Unity execution. |
| P15 construction | Structure absent before, present after the approved runtime mutation. | P15-A is promoted; Lab still needs a supported non-Unity operation and factual structure read. |
| P16 movement | Force position and carried supply before/after one passage. | P16-A is promoted; Lab still needs a supported non-Unity operation and factual military/spatial reads. |
| P20 shared activity | Independent decisions, coordinated start/lifecycle and individual effects. | P20-A synthetic shared behavior is promoted; Lab still needs a non-Unity execution/observation path. P20-B remains promoted Daily-profile census admission. P20-C joint travel follows the current-base design/handoff gate below. |
| P12 continuation | Save, advance, restore and compare a supported profile. | The specific P12 save/load continuation capability and its exact owner coverage. |
| P13 historical fork | Inspect a simulated boundary and compare source/fork continuations. | Actual P13 reconstruction/fork capability, recoverable causal history and provenance. |

`Simulation-External` World Explorer can display an approved exported factual
artifact, including one produced during a Lab run, but its World Exchange
projection is read-only and does not provide interactive command execution.
The Lab application host therefore remains Simulation-side; sharing a
presentation contract is optional and does not merge execution and projection
ownership. P19 may later consume useful stable contracts, but its general
loader and mod lifecycle remain deferred. Closed phases and active Master
candidates retain their existing gates and status. Demonstration classification
for any subsequent checkpoint follows `EXECUTION_MODEL.md`; a scenario is not
silently added to an in-flight implementation contract.

## P20 checkpoint identity reconciliation — accepted 2026-10-07

The accepted decision resolves the earlier Roadmap/State conflict:
**P20-A — Synthetic Multi-participant Operation** remains promoted;
**P20-B — Daily-profile census admission** retains its already-promoted
scope, evidence and history; **P20-C — Two-Person Joint Civil Travel** names
the real travel consumer. Exactly two Persons is the bounded proving fixture,
not a universal Activity cardinality, role or interval limit.

The dated 2026-10-03 proposal below used P20-B for joint travel. That historical
label is superseded by P20-C for current planning; old decision/design/review
records remain identifiable and are not rewritten. P20-B's admission promotion
neither proves P20-C delivery nor provides a joint-travel Lab demonstration.
See the [identity handoff](architecture/P20_CHECKPOINT_IDENTITY_RECONCILIATION_HANDOFF.md)
and [P20-C Master handoff](architecture/P20C_JOINT_CIVIL_TRAVEL_MASTER_HANDOFF.md)
for the current-base PASS / READY_FOR_IMPLEMENTATION technical verdict and
Master integration/validation gates. Architecture promotion is complete at
`eadfce01f44e7d0d649b2b93103abb10a21c6a37`; P20-C is technically
READY_FOR_IMPLEMENTATION and its Master handoff is current. Existing code is
not P20-C delivery evidence; its FailedStart reconstruction correction remains
required before domain promotion.

```text
P18-A/B/C promoted + P20-A promoted + P8-E promoted
  → P20-C reviewed design promoted → Master implementation/validation workflow
P20-B promoted Daily census boundary + current P12 composition
  → compatibility/rejection gate before P20-C domain promotion
P20-C promoted + supported non-Unity execution/read path
  → joint-travel Lab demonstration (follow-up, no retrospective domain gate)
```

P20-C does not require P18-D, full P12, P13, P19 or persistent Group/Party.
P11 remains conditional on separately selected external commands. P12's
`UnityBootstrap-Daily-v1` still admits only absent/empty P20 travel ownership
and rejects populated state. No implementation or Phase closure is claimed
by this architecture/planning reconciliation.

## P10 / P14 / P20 promoted product direction — 2026-10-03

A bounded checkpoint may prove one case without making it the universal model. The promoted [product-direction and checkpoint sequence](architecture/P10_P14_P20_NEXT_SCOPE_DECISIONS.md) treats varied local topology, finite/multiple sources and shared-activity consumers as compatible capabilities. It preserves promoted P10-A, P14-A and P20-A within their delivered profiles. At this **2026-10-03 planning decision**, the next bounded scopes were `P10-B` deterministic procedural topology for one Ruin instance, `P14-B` finite availability for one source, and `P20-B` joint civil travel by two Persons; all three were then `READY_FOR_TECHNICAL_DESIGN`, not implementation. The status is historical: current owning Phase States record P10-B and P14-B promoted, while the historical P20-B travel label is now superseded by P20-C under the 2026-10-07 reconciliation above. Later P14-C mixed sources waits on the selected P14-B proof; P14-D inter-City transfer still needs a bounded product/transport contract.

```text
P9 ordered genesis + P8 site/Location + P10-A topology owner
  → P10-B procedural topology (site-instance identity, scoped random context)
P14-A source/stock + source-owned finite reserve and coherent stock commit
  → P14-B finite availability
P14-B + P14-A → P14-C selected mixed finite/exogenous multi-source proof
second factual City + applicable transport/transaction contract
  → future P14-D bounded transfer; autonomous trade is later
P18-A/B/C + P20-A + P8-E explicit Person travel
  → P20-C joint-travel technical design (historical 2026-10-03 label: P20-B)
```

P10-B needs a compatible site-instance/definition seam because P10-A's single-instance profile uses a definition ID as its site key. P14-B must keep its reserve and market-stock mutation coherent without changing P14-A's exogenous meaning. P20-C must coordinate two individual travel transitions without making a two-person roster, shared role or common duration a universal Activity invariant. Future content, trade AI and other multi-person work remain separately scoped. New authoritative state or selected-profile composition still passes architecture §92A and the P12 exclusion/rejection rule; P12's accepted profile does not grow automatically. No closed phase is reopened by this promotion.

## Bounded next-slice planning promotion — 2026-10-03

The user selected P15-A, one runtime structure at an existing canonical Location, and P16-A, one existing ArmedForce crossing one valid passage with finite carried supply. Their [P15-A](design/PHASE15A_RUNTIME_STRUCTURE_CHECKPOINT.md) and [P16-A](design/PHASE16A_SINGLE_PASSAGE_MOVEMENT_CHECKPOINT.md) planning/technical-design contracts were independently reviewed and promoted at architecture content SHA `a2788e6400251b5ce3cf8d2269ea8a5b5fdfd4a8`. They are eligible for the Master implementation workflow, not delivered runtime capabilities. They do not expand `UnityBootstrap-Daily-v1`. P15-A uses a synthetic inert proving structure without material debit because P14-A supplies no applicable generic construction-cost transaction; P16-A extends the existing military position owner with carried supply and must fail closed under the P12 profile admission rule.

The [P13 retention/causal-input design](design/PHASE13_RETENTION_CAUSAL_INPUT_DESIGN.md) and [P19 public-extension-surface design](design/PHASE19_PUBLIC_EXTENSION_SURFACE_DESIGN.md) may advance independently. P13 reconstruction/fork implementation still waits on exact continuation of the chosen world/profile and recoverable causal history. P19 loader implementation remains deferred. The updated [P10/P14/P20 product direction](architecture/P10_P14_P20_NEXT_SCOPE_DECISIONS.md) defines small next design scopes without implementing them.

```text
P8-A Location/anchor + runtime guard → P15-A bounded structure creation
  P14 material-cost capability → later cost-bearing construction only
P7 force/position + P8-A/B passage + P14-compatible finite-item semantics
  → P16-A one-hop military movement and carried-supply debit
P18 timed execution → later timed construction/military movement only
P12 selected-profile admission/exclusion → P15-A/P16-A domain promotion gate
P12 exact chosen-profile continuation + recoverable history + compatible execution
  → P13 authoritative reconstruction/fork implementation
P9 ordered genesis seam → bounded P19 public-generation-contract design
  P19 loader and durable mod state remain separate future gates
P10-A / P14-A / P20-A → bounded next-slice technical designs above
```

`SimulationRuntime` and P12-B composition/admission are serial integration hotspots. P15-A owns a new structure store and reads spatial truth. P16-A changes `ArmedForceSpatialStateStore` and reads spatial/passage truth; its P12 census interaction needs a negative admission test. Neither track may edit the active P12-B candidate in parallel. Existing closed phases stay closed; downstream P17 waits for broader military/territorial/War semantics, and the P15/P16 slices make no P18 or P20 capability claim.

## P12 capability-level dependency refresh — 2026-10-03

The [P12 capability DAG audit](architecture/P12_CAPABILITY_DAG_AUDIT.md) separates the accepted `UnityBootstrap-Daily-v1` continuation profile from entry of new authoritative domain capabilities. P12-B is still incomplete on `origin/codex/phase12/canonical` at `54fc23b`; P12-C–G and P12-A retain their accepted chain. Full P12 closure still means exact, validated deterministic continuation for every admitted owner. A newly composed unsupported owner causes profile admission to reject, never silent omission or automatic P12 scope growth. No global `PHASE 12 FOUNDATION READY` milestone is introduced. Each new owner instead passes the architecture §92A continuation-aware entry gate before its bounded checkpoint can be implemented.

The earlier [P12 capability-DAG handoff](architecture/P12_CAPABILITY_DAG_MASTER_HANDOFF.md) records its historical ready set. The [current P15/P16 handoff](architecture/P15_P16_NEXT_SLICES_MASTER_HANDOFF.md) supersedes its P15/P16 product-gate entries; current owning States and code still determine delivery.

While P12-B admission is incomplete, a new owner must either stay outside the selected composition by construction or prove a fail-closed admission hook and negative rejection test before its domain checkpoint is promoted. The latter is a serial P12-B integration, not an independent edit to that hotspot.

| Phase / track | P12 relationship | Next gate |
|---|---|---|
| P10 | Pre-start authoring independent; later saved generated profile is a specific P12 integration. | P10-A promoted; P10-B procedural topology is `READY_FOR_TECHNICAL_DESIGN`. Local content remains unselected. |
| P13 | Strategy and causal-input design independent; actual fork needs complete continuation of its chosen world/profile plus recoverable history. | Retention/causal-input design ready; reconstruction/fork implementation `WAIT_DEPENDENCY`. |
| P14 | Domain work continuation-aware, not blocked by P12. | P14-A promoted; P14-B finite availability is `READY_FOR_TECHNICAL_DESIGN`. Mixed sources and transfer follow separate gates. |
| P15 | Domain work continuation-aware, not blocked by P12 closure. | P15-A reviewed design promoted; `READY_FOR_MASTER_IMPLEMENTATION_HANDOFF` for one inert structure at an existing Location, with profile-safety gate before domain promotion. Broader founding remains unselected. |
| P16 | Domain work continuation-aware, not blocked by P12 closure. | P16-A reviewed design promoted; `READY_FOR_MASTER_IMPLEMENTATION_HANDOFF` for one selected force/one passage/finite supply, with profile-safety gate before domain promotion. Broader routes remain unselected. |
| P17 | Deferred by strategic/domain/product prerequisites, not P12. | P16, territory/political authority and War semantics; no implementation checkpoint. |
| P18 | Bounded A–D scope closed; a future intraday save profile requires specific temporal-state coverage. | No reopening for P12. |
| P19 | Public-surface design may use real consumers; durable mod-state support needs specific P12/P13 integration. | Bounded public-extension-surface design ready; loader scope/implementation remain deferred; no blanket P12 gate. |
| P20 | P20-A synthetic operation and P20-B Daily-profile census admission are promoted; future shared-activity save needs specific state coverage. | P20-C joint travel uses the refreshed design/Master handoff for exact readiness; no universal two-person rule or P12 scope growth. |

```text
canonical semantics + per-owner continuation-aware gate
  → bounded P15/P16 and other runtime designs (after their own product scope)
P12-B → C → D/E → F → G → P12-A parity → P12 CLOSED for accepted profile
complete continuation of chosen world/profile + causal history + compatible execution
  → P13 authoritative reconstruction and independent fork
```

P13 retention/checkpoint and causal-input design is ready as isolated architecture work; P15-A/P16-A have approved bounded scope and reviewed technical design, so the Master may start isolated implementation workflows before P12 closure after checking current prerequisites. At this dated snapshot, P10-B/P14-B and the travel proposal then labeled P20-B were bounded design candidates, not implementation-ready capabilities. Current travel identity/readiness is P20-C as recorded above. The Master may run P15-A/P16-A in separate worktrees while P12-B continues, serializing shared `SimulationRuntime`, spatial/domain owner and persistence-composition hotspots at integration. No closed phase is reopened by this dependency clarification.

WI-A, FR-B and FR-C integrations and the bounded WX-D World Exchange v2 producer have since been promoted/handed off through P12 canonical. Simulation-External `main` at `0ce8403` owns the v2 `collectionCoverage` contract. These delivered cross-cutting slices do not supply P12 capture, save/load or P13 fork. The checkpoint table below records **their planning readiness when its 2026-10-01 design was promoted**, not current delivery; use current owning States and code for delivery.

## World identity and factual projection — approved direction

The approved architecture study is
[`design/WORLD_IDENTITY_AND_PROJECTION_SURFACE_STUDY.md`](design/WORLD_IDENTITY_AND_PROJECTION_SURFACE_STUDY.md).
Architecture §§91A–91B establish durable WorldId semantics for one causal
continuation branch and a bounded read-only factual projection surface. At
the architecture approval, individual capabilities were **DESIGN PROPOSED**;
the subsequent 2026-10-01 design promotion made WI-A and FR-B
**READY_FOR_IMPLEMENTATION**. The bounded implementations have since been
promoted as recorded in the 2026-10-03 refresh above and owning States.
No new numbered phase is assigned: identity belongs at successful world composition, factual
readers are a later bounded cross-domain capability, and the first World
Exchange producer is a later adapter integration. Mod-defined projection
extensions remain deferred until a real P19-compatible consumer exists.

In the approved dependency direction, WorldId aligns with P9's atomic pre-start
publication without reopening closed P9. Identity-preserving P12 save/load and
P13 branch publication consume the approved WorldId semantics at their
respective boundaries. P12-C identity/provenance and future P12-A save-contract
work must revalidate against them; current P12-B work is not invalidated.
A Faction projection port and producer do not require P12-A, P12-B, full P12,
P13 mechanics or P19; selected P12 epoch/quiescence infrastructure may be
reused only if it covers the projected owner set. A real producer requires a
stable WorldId, a coherent approved Faction read and an approved external
collection-coverage contract, with adaptation outside the Simulation domain. P13 still
requires P12 continuation and recoverable causal history independently of
projection. See the study's DAG and explicit remaining gates before scheduling.

### Promoted bounded execution checkpoints (2026-10-01)

The reviewed candidate `bd0d7979d556da12fd69046087d75d0a93b1803d`
was fast-forwarded into this architecture branch. It refines the approved
direction into **WI-A World Identity Foundation**,
**FR-B Factual Read Surface Foundation**, **FR-C Faction Factual Reader**,
and **WX-D First World Exchange Producer**. These are cross-phase
checkpoint labels, not new numbered Phases; design promotion alone did not
deliver capabilities. The
bounded designs are [WI-A](design/WORLD_IDENTITY_FOUNDATION_DESIGN.md),
[FR-B](design/FACTUAL_READ_SURFACE_FOUNDATION_DESIGN.md),
[FR-C](design/FACTION_FACTUAL_READER_DESIGN.md), and
[WX-D](design/WORLD_EXCHANGE_FIRST_PRODUCER_DESIGN.md). Independent review of
the technical design at `9606263` is recorded in
[the review record](design/WORLD_PROJECTION_CHECKPOINTS_DESIGN_REVIEW.md).
The reviewed checkpoint contracts are canonical planning authority. Their
then-current readiness did not assert delivered implementation or authorize a broader scope.

```text
approved architecture f27954a + promoted checkpoint contracts bd0d797
  ├─ WI-A WorldId ────────────────────────────┐
  └─ FR-B read foundation → FR-C Faction ────┼→ WX-D producer
                                               ↑
                         External coverage contract
```

At design promotion, WI-A and FR-B could be implemented independently in
isolated worktrees with serial integration at shared P12-B hotspots. FR-C
required promoted FR-B; WX-D required WI-A, FR-C and an External-owned
collection-coverage contract. World Exchange v1's mandatory arrays could not
distinguish unsupported from known-empty, so an honest bounded producer
awaited that External contract. Simulation-External v2 subsequently supplied
it, and bounded WX-D was promoted; this architecture branch changes no
External files.

| Checkpoint | Owner / planning readiness on 2026-10-01 | Hard prerequisites then | Then-current implementation hotspot |
|---|---|---|---|
| WI-A | Cross-phase world composition / READY_FOR_IMPLEMENTATION | Approved WorldId semantics | `TesteSimulacao` private-draft/publication gate, `SimulationRuntime`, P18 profile handoff; reconcile and serialize integration with active P12-B bootstrap scope. |
| FR-B | Cross-domain factual read infrastructure / READY_FOR_IMPLEMENTATION | Approved projection semantics; explicit `UnityBootstrapDailyV1` owner-thread boundary | `SimulationRuntime` read admission plus Faction/Person Store writer guards; serialize runtime integration with active P12-B writer. |
| FR-C | Faction factual capability / WAIT_DEPENDENCY | FR-B promoted capability | Narrow Faction registration/runtime facade; do not begin implementation before FR-B promotion. |
| WX-D | Dedicated Simulation integration project / WAIT_DEPENDENCY | WI-A, FR-C, External coverage contract | Adapter project; no direct P12-B runtime write planned. |

Relationship classification: P9 → WI-A is **ARCHITECTURAL_ALIGNMENT**
(publication seam, no P9 reopening); P18 → WI-A/FR-B is
**ARCHITECTURAL_ALIGNMENT** (typed identity handoff/completed boundary);
P12-B → FR-B is **OPTIONAL_REUSE**, not a global coherence proof, while
P12-B → WI-A is **NO DEPENDENCY** but shares implementation hotspots;
WI-A → future P12-C/P12-A and P13 is **ARCHITECTURAL_ALIGNMENT** until those
scopes implement continuation/fork identity, at which point WI-A's promoted
contract is a **HARD_DEPENDENCY** for their identity-bearing work. P12-A,
P12-C, full P12 and P13 mechanics → WX-D are **NO DEPENDENCY** for the first
live artifact. P19 → all four is **NO DEPENDENCY**. Simulation-External
coverage contract → WX-D is a **HARD_DEPENDENCY**; its TypeScript packages
remain external authority and are never a Simulation domain dependency.

Reconstruction-sensitive state: the published WorldId is a causal input to
continuation/fork identity; FR-B/FR-C copy current facts and create no new
authoritative state; WX-D is a disposable, potentially stale artifact. Future
P12 preserves WorldId on load, while P13 creates a new one with source-world
and fork-boundary provenance and must reconcile inherited pre-fork history with
new post-fork namespaces. Existing closed P9/P18 delivery remains closed;
active P12-B is not invalidated, and its `SimulationRuntime`/bootstrap
hotspots require serial integration, not a second concurrent writer.

## Placement of temporal and extension work

P18 is one phase with dependent checkpoints, rather than separate phases for
every timed consumer. P18-A (timeline/scheduler) precedes P18-B (activity
lifecycle), then P18-C (actor availability/decision integration). P18-D adapts
the relevant existing daily processes, actor input and travel consumers after
their actual contracts/capabilities are available. Entry/technical design can
begin now; implementation is not authorized by this roadmap.

Prioritize P18 foundations before consumers promise work shifts, arbitrary
activity duration, intraday opportunities or complete intraday saves. P8-E's
explicit-operation travel capability and the P11 daily SellGoods slice are
bounded transitional capabilities after impact review; neither proves P18.
P18-D waits only for the consumers selected for its reviewed integration scope,
not for whole unrelated phases. There is no P18 dependency on worldgen or War.

P19 is dedicated later platform work, after concrete extension consumers and
the contracts it exposes are sufficiently stable. P9 must already use an
ordered dependency-aware generation pipeline; it does not wait for a loader.
P19 generation adapters consume the relevant P9/P10 capabilities, temporal
adapters consume P18, and durable mod-state/retrofit support consumes the
applicable P12/P13 compatibility/continuation boundaries. These are conditional
component edges, not a blanket P19 lock on all those phases.

```text
current calendar + determinism + domain mutation contracts
  → P18-A timeline / due-work scheduler
  → P18-B activity lifecycle
  → P18-C availability-driven actor decisions
  → P18-D bounded legacy / actor-command / travel integration
      ↑ relevant P8-E and P11 capabilities, only when integrated

P18 state/ordering contracts → P12 intraday inventory/design
P18 promoted capability + P12 hydration → supported intraday continuation
P12 continuation + recoverable temporal inputs/state → P13 intraday fork
P18 relevant capability → timed P14/P15/P16 consumers

P8-A geography + P9-A genesis pipeline → P9-B authored geography capability
P9-B canonical LocationId + P8-C Ruin/site anchor → P10-A Ruin profile and bounded LocationId-neutral LocalTopology seam
stable real extension consumers → P19 API/loader
  relevant P9/P10 → generation extensions
  relevant P18 → temporal extensions
  relevant P12/P13 → durable mod state / explicit retrofit compatibility
```

Semantic hooks, presentation independence, player ownership and deterministic
composition are constraints now. A public API/loader, registration protocol,
mod packaging, module migration/storage schemas and alternative-renderer
transport are deferred to dedicated designs. No anti-cheat or adversarial
actor-control/mod-security architecture is introduced for the player's local
world. Optional official expansions should ideally use that same public API.

## Multi-participant activity layer

P18 preserves definition versus instance, stable activity identity independent
of a single actor, and composable availability/commitment boundaries now. Its
first activity slice may involve one actor; it must not turn that slice into a
permanent one-Activity-to-one-Actor contract or NPC-owned lifecycle. P18 does
not implement formation, roles or coordinated multi-participant execution.

**P20** layers that capability on the relevant P18-A/B/C timeline, lifecycle and
availability contracts/capabilities. Entry/decomposition can proceed against
accepted contracts; execution waits for the actual promoted capabilities.
P18-D's entire legacy migration is not a blanket dependency, and P18 never
waits for P20. P20 is prioritized after that temporal foundation, not after
P19 merely because its number is higher. P19's activity extension adapter
consumes the relevant P20 capability when multi-participant support is exposed;
P20's official bounded implementation does not wait for the mod loader.

```text
P18-A/B/C relevant contracts → P20 entry / technical design
P18-A/B/C relevant promoted capabilities → P20 shared activity execution
P20 + relevant spatial/travel capability → future traveling-together consumer
P20 + relevant P14/P15 capability → future multi-worker consumer
P20 state contracts/capability + P12/P13 → supported shared-activity save/fork
P20 + relevant P19 public surface → mod-defined multi-participant activities
```

These are conditional consumer edges, not new closure requirements for P8's
individual traveler, P11 SellGoods or static P14/P15 truth. P16/P17 may use
compatible temporal/commitment concepts for aggregate units/armies without
depending on P20's small-group execution or scheduling all Persons in one
activity. Persistent Group/Organization is not a prerequisite for P20.

P20 entry must delimit definition/instance ownership, required/optional roles
and counts when useful, proposal/formation, independent acceptance, future
reservations, conflict/stale-state checks and atomic start, cancellation or
failure-to-form/abort, and participant-specific effects under normal domain
authorities. No full planner, social negotiation, gang, War or workflow engine
is added. Technical representation and exact role APIs remain design work.

## Dependency directions, not blanket phase locks

- P8-A establishes factual geography. P8-B (passages) and P8-C (anchors/civil presence) may proceed with isolation after their shared identity/segment contract is stable. P8-D (Knowledge/route) consumes the needed passage and position contracts; P8-E integrates the travel slice. Each implementation dependency must distinguish accepted contract from promoted capability.
- Phase 9 design may use stable Phase 8 spatial contracts; code that needs working spatial authority waits for the relevant promoted capability. P9-A remains promoted and scope-closed. P9-B is promoted at `d9a62d7`, consuming P8-A geography plus P9-A's pipeline. P10-A consumes this canonical authored `LocationId` source and P8-C's promoted Ruin/site anchor contract; P10-A implements only the approved bounded LocationId-neutral LocalTopology owner seam and finite profile topology. P10 does not mint regional geography or Location identity. These are separate capability edges, not a blanket P8 completion lock. Phase 9 is formally closed at `3bd0eb3` within the approved P9-A/P9-B scopes.
- P9/P10 generation is an ordered pipeline with explicit stage dependencies and deterministic contributions. New-world participation and existing-world retrofit are separate contracts; later installation never implicitly reruns historical stages. Runtime World Expansion must also obey these distinctions.
- Phase 11 entry architecture and Phase 12 causal-state inventory can progress independently of Phase 8 worldgen. Their code integrations must still wait for whichever concrete command, state, or domain capability they actually consume.
- Phase 13's product guarantee needs Phase 12 continuation plus recoverable causal inputs and initial-world/mutation semantics. Save continuation alone does not fulfill historical forkability.
- Full intraday continuation/fork coverage additionally consumes P18's temporal state/ordering and applicable integrations. Daily-profile coverage may precede it if explicitly scoped; it cannot claim complete intraday support.
- Phase 14 may design localized sources after spatial identity/anchors stabilize; route-dependent material flow waits for the relevant passage/travel capability, not necessarily for every unrelated Phase 8 consumer.
- Phase 15 consumes spatial identity/anchors and normal runtime mutation authority. Material-cost integration waits for the relevant Phase 14 contract/capability; it does not inherit genesis authority.
- Phase 16 uses Phase 7 force/Battle foundations, relevant Phase 8 geography/passages, and Phase 14 logistical/material contracts. Civil travel is not military movement.
- Phase 17 waits for the relevant military movement/logistics and explicit strategic, territorial, and political decisions. A Battle outcome does not automatically resolve War or establish control.

These are planning edges, not fabricated checkpoint IDs for Phases 9–17. Hard contract, promoted capability, integration, validation, architecture/product gates, and soft ordering have different scheduling effects as defined in `EXECUTION_MODEL.md`. A candidate branch never satisfies a canonical capability dependency by default.

## Revised gates and candidate impact

- Before P18 implementation: reviewed logical time precision/range, same-instant
  ordering, reentrant scheduling, zero-duration progress, lifecycle ownership,
  cancellation/stale-work handling, actor availability and daily compatibility.
  Review activity identity/cardinality and definition/instance separation;
  single-actor validation must leave the P20 participant layer possible without
  relocating lifecycle into a synthetic NPC or second scheduler.
- Before the first durable intraday command: retain payload, authority, exact
  logical boundary and causal ordering. A date alone cannot order intraday effects.
- Before P9 implementation: reviewed bounded profile and stage inputs/outputs,
  dependencies/contribution ordering, identity/provenance, isolated random context
  and complete pre-start publication. P19 implementation is not a prerequisite.
- Before P19 implementation: real supported extension scope and public contracts;
  define module compatibility/state lifecycle, deterministic composition and
  explicit new-world versus optional retrofit behavior. No generic security platform.
- Before P20 implementation: reviewed bounded participation/formation contract
  and relevant promoted P18 capabilities; close atomic reservation/start/release,
  independent decision/Knowledge, role validation and loss-of-participant rules
  for the selected proving slice. No per-gameplay manager or NPC rewrite is required.
- Before claiming save/fork with mods or intraday execution: supported temporal
  and extension-state inventory, recoverable compatible code/content, inputs and
  history; no missing causality may be reconstructed retroactively.

The dated impact record is `architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md`.
Promoted P8-A/B/C/D/E and closed Phases 5–7 remain valid in their delivered
scopes. P8-E passed targeted temporal-impact revalidation and was promoted at
`d95b60d`; P11 and P18-A candidates were refreshed against that base. P9 and
P12 entry proposals need their new pipeline and temporal/extension inventories
incorporated before further approval. No existing phase is automatically
implementation-ready, cancelled, or retroactively rewritten.

The additional impact record is `architecture/MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md`
(base `4b6dd1d`). P18-B/C technical work requires a targeted cardinality/identity
review; no P18 implementation existed at this baseline. Delivered P8 behavior
and current P8/P11 single-actor candidates acquire no P20 implementation gate.
P12/P13 inventory and P19 extension design consume the shared-activity contracts
only where that capability is in scope.

## Changing the roadmap

Phases may be inserted, split, merged, or reordered when the approved product scope changes. Record the revised objective and dependency impact in this roadmap and affected Briefs; preserve existing Phase State history and stable checkpoint identity or explicitly retire/supersede an ID. Re-evaluate active candidates against the new canonical context. Do not convert a soft ordering preference into a hard dependency, or a proposed scope into constitutional architecture, without review.
