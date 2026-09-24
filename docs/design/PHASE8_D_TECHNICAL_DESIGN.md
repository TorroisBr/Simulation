# P8-D — Knowledge and Route Plan Technical Design

**Design base:** `01eebe0f11ca145e04e7c4cd065c124777edc8fd` (`codex/phase8/canonical`)
**Accepted upstream contract:** `5faa5817a11b0ae7412ec3ed98240fb1d633de11` (`PHASE8_BC_SHARED_SEGMENT_CONTRACT.md`)
**Candidate branch:** `codex/phase8/P8DTechnicalDesign`
**Scope:** P8-D technical design only. No executable code, architecture, Brief, Roadmap, or State edits.
**Readiness:** The P8-D design is approved, and its P8-B/C capability dependencies are now canonical through implementation commit `1c84519740db8a245e103678b38f692e13522383`; P8-D implementation is READY from the current `codex/phase8/canonical` head. No product choice is hidden in the selector: route comparison requires an explicit stable selection policy and estimates available to the actor; absence of either is a typed planning failure. This checkpoint's regional route endpoints are Hexes; Location/Crossing local connectors are outside its supported routing graph.

## 1. Authority and scope

This design is subordinate to `docs/SIMULATION_ARCHITECTURE.md` §§5–6, 16, 69; `docs/phases/PHASE8_BRIEF.md`; `docs/PHASE8_STATE.md`; the P8-A technical design; and the accepted P8-B/C shared segment contract. It defines the Knowledge and route-plan seams needed for P8-D. The P8-B/C capabilities it consumes are now canonical; this design does not claim P8-D itself is implemented.

P8-D establishes actor-owned spatial observations, known route alternatives, a deterministic route-selection operation over those observations, and an accepted route plan keyed to persistent `PersonId`. The plan is intent/decision state. P8-C remains the only authority for factual position and transit progress; P8-B remains the authority for current passage truth. Planning reads actor Knowledge and explicit request inputs only. Execution, owned by the later P8-E vertical slice, revalidates current truth.

The current `SpatialKnowledgeRuntime` stores `RuntimeId`-based known Locations and Routes, and the existing `TravelSystem`/`TravelParty` implement direct legacy trips with fixed `TravelDays`. They remain compatibility code for those consumers. Neither is suitable as the P8-D actor Knowledge, stable route identity, Person position, or new trip-plan authority.

## 2. Semantic model

P8-D keeps these facts separate:

| Concept | Owner | Meaning |
|---|---|---|
| Current passage, terrain, crossing condition | P8-B spatial passage authority | Factual present world state. |
| Current position and in-transit segment progress | P8-C Person spatial position authority | Factual location/progress keyed by `PersonId`. |
| Spatial observation | P8-D actor Knowledge store | What a `PersonId` observed or received about a typed spatial subject, at a time and with provenance. It can be stale or false. |
| Route candidate | Pure P8-D evaluation value | An ordered sequence of stable directed traversal segments assembled from known route/segment information. It is not world truth. |
| Selected route plan | P8-D plan authority | Destination, chosen candidate, assumptions/knowledge basis, selection policy, and decision identity. It is intent, not physical position or progress. |
| Execution result | P8-E | Outcome after checking the next traversal segment against current P8-B truth and the P8-C position. |

Route Knowledge is not a mirror of the passage store. Registering a Hex, Connection, Crossing, or new passage condition does not reveal it to an actor. A route may be unknown, known as usable when it is now unavailable, or known as unavailable when it is now open. `Unknown` is distinct from `KnownUnavailable`, and absence of a known route is not evidence of physical impossibility.

An observation record should contain a typed subject, observed value/status or estimate, stable source identity, observation day, receipt day when information was relayed, confidence/precision as supported by the shared Knowledge conventions, and stable provenance sufficient to identify whether observations share an origin. It must not conflate observed value, current truth, interpretation, or memory. Repeated copies with the same origin do not count as independent confirmation. Preserve received observations and resolve the actor's current belief for a subject with this deterministic rule: greatest `ReceivedDay` wins; if received on the same day, the ordinal-greatest stable provenance key wins. Two conflicting values with the same provenance key are an invalid/colliding observation input and are rejected atomically. This rule determines which information the actor currently uses; it does not erase older evidence or claim that the winning observation is true. It never edits World Truth.

## 3. Knowledge API and update authority

Introduce a `PersonId`-keyed spatial Knowledge store (proposed source: `SpatialRouteKnowledgeStore.cs`) composed into the world runtime through one integration owner. The store owns actor observations, deterministic lookup/order, clone, mutation guard, invariant validation, and stable diagnostic projection. It does not own passages, positions, routing decisions, or runtime NPC references.

Conceptual seams; exact names may follow the published upstream APIs:

```text
TryRecordObservation(PersonId actor, SpatialObservation observation)
GetObservations(PersonId actor, SpatialSubject subject)
TryGetKnownTraversal( ... typed boundary + TraversalOptionRef ... )
TryBuildKnownCandidates(actor, origin, destination, explicit inputs)
```

`SpatialSubject` is a closed typed union over the stable spatial identities supplied by P8-A/B: Hex, Location, Crossing, boundary/traversal option, and route-estimate subject as needed. It carries no `RuntimeId`. A known route is a semantic sequence of directed boundary and `TraversalOptionRef` values, not the legacy `SpatialRouteRuntime.RuntimeId` and not an opaque string. The same traversal option may be observed in more than one direction while retaining the accepted B/C stable option identity.

Only an explicit observation input may change Knowledge. The input declares what was observed, by whom/source, and when; the store does not query P8-B for current status to fill in missing fields. An execution failure can produce a limited observation such as “attempted option was unavailable at this boundary on day N” only when the actor could observe that outcome. The mutation must not infer “bridge collapsed,” “barrier raised,” or another cause from a generic false revalidation result. A cause-specific observation requires an explicit evidence input from the observation/execution context that says the actor perceived that cause. If there is no such evidence, the stored update is limited to the observed failed attempt (or no update when even that is not observable).

Observation time/provenance must remain available to freshness and confidence rules. Staleness is evaluated as a read against an explicit planning day and policy; it does not silently erase or refresh the stored observation. A false observation remains the actor's belief until a new observation or information-transfer operation changes it. P8-D adds no broadcast, automatic scouting, information propagation, or periodic truth synchronization.

## 4. Candidate generation and deterministic selection

Planning input is an explicit immutable request containing `PersonId`, a known factual origin Hex supplied from P8-C or an explicit command, destination `HexId`, planning day, actor Knowledge view, and a stable route-selection policy identity/version. P8-D's route graph and candidate endpoints are Hex-only. If P8-C reports `At(LocationId)` or `At(CrossingId)`, P8-D returns a typed `UnsupportedRouteEndpoint` unless a later approved capability supplies an explicit connector transition; it must not treat a Location/Crossing anchor Hex as a traversable connection. A caller cannot silently substitute the anchor Hex for the actor's position. If a Hex identity or the policy cannot be resolved, planning returns a typed failure without mutation. The planner must not inspect current passage truth to repair a candidate or estimate.

Candidate generation uses only the actor's known spatial structure and the Hex request endpoints. It returns zero or more simple ordered paths whose every segment is represented in the actor's Knowledge. For each traversal option, resolve its current actor-belief value using the explicit planning day/freshness rules; `KnownUnavailable` makes that option ineligible for a new candidate and therefore for selection. This is only a belief-based planning constraint: the planner neither asserts that the option is factually unavailable nor queries P8-B's passage authority. A false `KnownUnavailable` belief can therefore exclude an actually open option until new information changes the resolved belief. `Unknown` remains distinct from `KnownUnavailable`: it does not itself assert unavailability, and the explicit selection policy handles any missing status or estimate it requires. Candidate generation does not enumerate hidden Hexes, discover Connections, infer passage from geometric adjacency, or infer local access because two things share a Hex. A `LocationId` resolving to its anchor Hex and a `CrossingId` resolving to physical boundary facts do not create a route edge or connector. Known graph traversal is deterministic: expand and return segments by canonical boundary endpoint order, direction, then the accepted `TraversalOptionRef` ordering; candidate paths have a stable sequence key derived from their typed segment sequence. Cycles are excluded from a candidate path. If no candidate is known, return `NoKnownRoute`; do not claim `NoPhysicalRoute`.

When two or more known candidates exist, selection is deterministic only under an explicit policy. The policy is an input with a stable semantic identity/version and defines which *actor-known estimates* are comparable and their ordering. The selector evaluates the candidate set against those estimates, then breaks a complete tie by ordinal stable candidate sequence key. It must never substitute current factual passage status or an execution-time cost for missing/stale observations. If the policy's required estimate is absent, invalid, or stale under that policy, return a typed `InsufficientKnownEstimate`/`PolicyUnavailable` result rather than falling back to hidden truth or an undocumented preference. The design intentionally does not establish a universal shortest, fastest, safest, or cheapest route objective or formula; those remain distinct and open in §69. The first P8-E scenario must supply a concrete policy and the observations/estimates needed for it. This Hex-only endpoint constraint is deliberate scope control for P8-D, not a statement that Locations or Crossings are unreachable; supporting them requires an explicit known connector transition and corresponding execution-time factual validation in a later approved seam.

Candidate generation and preview are pure: no store revision, allocator, plan, event, or authoritative RNG changes. Given equal actor Knowledge, request, and policy version, they return equal candidates and selection regardless of insertion order or loaded NPC representation. The accepted route result carries a knowledge basis (the observation identities/revisions or deterministic observation fingerprint actually consulted), not a claim that those observations are true.

## 5. Plan authority and factual state boundary

Introduce a route-plan authority keyed by persistent `PersonId` (proposed source: `PersonRoutePlanStore.cs`) and a pure planning/selection system (proposed source: `SpatialRoutePlanningSystem.cs`). A plan contains at least:

- actor `PersonId`;
- destination `HexId` (the only route-plan endpoint type supported by P8-D);
- selected ordered route candidate and its typed segment identities;
- explicit route-selection policy ID/version and the estimate/observation basis used;
- stable decision identity supplied by the existing decision authority (or a typed external decision reference if that seam is not available yet);
- plan creation/acceptance day and a plan revision/status sufficient to recognize supersession or interruption.

Plan replacement is an explicit mutation after selection. Preview and rejected/stale acceptance do not mutate the plan. A new selection/replan is a new decision and supersedes the prior plan only after validating the Person, destination, candidate identity, and source Knowledge basis under P8-D's accepted store mutation rules. P8-D does not automatically replan when Knowledge or World Truth changes.

P8-C remains the sole owner of `At(StablePositionReference)` / `InTransit(TraversalProgress)`. The route plan may identify the next intended segment, but stores no current position, completed distance, elapsed travel, progress fraction, or “arrived” flag that could disagree with P8-C. Starting/advancing/stopping/arriving a trip and coordinating plan status with factual position are P8-E transaction work. If P8-E later commits multiple authorities, it must validate every participant before making any visible mutation, as required by the B/C contract.

## 6. Execution-time revalidation and knowledge feedback

At each P8-E traversal boundary, execution resolves the plan's next stable segment/option against P8-B's current factual passage authority and validates the actor's factual `PersonId` position/progress through P8-C. The plan is evidence of intent only; it does not authorize passage and is never accepted as proof that its old observation still matches reality. Current contextual passage/cost belongs to execution, not route-planning estimates.

If the next option is now unavailable or otherwise invalid, execution rejects/stops at the last factual position/progress P8-C has committed. It does not advance position, consume an alternate route, or rewrite the plan as though arrival/progress occurred. The result distinguishes stale/infeasible execution from planning failure but reveals only facts supported by the actor's observation context. A generic stale failure cannot identify its hidden cause. The actor may later use an explicitly recorded observation and make a new route decision; P8-D does not select it automatically.

An accepted plan's observation basis can be reported as stale relative to new Knowledge, and execution can report that current World Truth rejected the next segment. These are separate predicates. Stale plan in truth does not imply that the actor knows why it is stale.

## 7. Identity, reconstruction, and authority composition

All actor ownership is by stable `PersonId`, independent of `NpcRuntime` materialization, dormancy, death, or loading. Spatial subject and route segment identity use the accepted stable Hex/Location/Crossing/boundary/`TraversalOptionRef` values. Route candidate identity is reconstructible from endpoints and its ordered typed segment sequence. Observation ordering is semantic (actor ID, subject type/identity, observed day, stable source/provenance identity); plan ordering is ordinal `PersonId` with stable decision identity. Neither dictionaries, registration order, `RuntimeIdAllocator`, nor rendering data may affect output.

Knowledge and plan state that changes later decisions must be cloned with the runtime and exposed through stable diagnostics/invariants like other authoritative causal inputs; this is not a save/load or replay implementation. Every new mutation is mutation-guarded, validates all identities and revisions before changing state, and leaves state/revision unchanged on rejection. Query/preview methods are read-only and do not consume randomness.

Single-owner integration seams:

- P8-D worker owns new spatial Knowledge value/store, route candidate and policy input/result values, plan store, planner, and focused tests. It must not edit P8-B passage structures or P8-C position/progress structures.
- One named integrator owns `SimulationRuntime` composition and shared `WorldStateSnapshot`, canonical writer, diff, formatter, invariant, and runtime-facade additions after B/C are promoted. The P8-D worker provides the exact projection/invariant requirements without concurrently editing those hotspots.
- P8-E owns execution transaction wiring and any daily-loop integration only if the accepted vertical slice explicitly requires autonomous progression; P8-D adds no `AdvanceDay` behavior.
- Existing legacy `SpatialKnowledgeRuntime`, `SpatialRouteRuntime`, `TravelSystem`, and `TravelParty` are not migrated or repurposed by P8-D. Any adapter would require explicit stable identity and a separate approved seam; this design defines none.

## 8. Integration order, tests, and regression gates

1. Accept this design against its stated base and the accepted B/C contract. Implementations wait until B and C are promoted and their published APIs are available; P8-D types bind to those exact stable identities rather than duplicate them.
2. Implement P8-D Knowledge and planning stores in an isolated feature branch based on the then-current Phase 8 canonical plus promoted B/C. Keep runtime/diagnostics composition for one assigned integrator. Independently review the complete P8-D diff before integration.
3. Integrate the P8-D-owned stores and pure planner, then the runtime/diagnostics composition patch. P8-E starts only after the relevant P8-D capability is promoted and reviewed.
4. Focused P8-D coverage must prove: stale and deliberately false observations remain distinct from current truth; unknown does not equal unavailable; resolved `KnownUnavailable` belief excludes that option from a new candidate even when factual passage is open; same-subject conflicts resolve by latest receipt day then stable provenance key across insertion orders; conflicting values with one provenance key reject atomically; two or more eligible known candidates are generated and selected deterministically; tie-breaking is stable across insertion orders; missing/stale policy inputs fail without hidden-truth fallback; non-Hex route endpoints reject even when their Location/Crossing resolves to an anchor Hex; preview has no mutation/revision/RNG effects; knowledge updates preserve source/observed/received provenance and do not infer hidden causes; generic failed revalidation does not reveal a cause; plan mutation/replacement is atomic; plan and C position/progress have one owner each; stable `PersonId` and route reconstruction work across clones/materialization states; clone, diagnostics, canonical projection, diff and invariants include causal inputs; rejected references/revisions leave state unchanged.
5. Run the relevant P8-D EditMode suite and affected P8-A geography, P8-B passage, P8-C position, Phase 7 spatial/Battle/ArmedForce, and legacy Travel regressions at integration. Follow the then-current Brief/State promotion gate, including full required EditMode and official Smoke when promoting. `git diff --check` is required. P8-D alone adds no daily loop behavior, so long-run validation is not implied; reassess if integration introduces autonomous travel progression.

## 9. Exclusions and blockers

P8-D does not implement passage or position authority, travel execution/progress, automatic replan, route discovery/scouting or information propagation, shared/group Knowledge, universal knowledge-holder abstractions, shortest/fastest/safest/cheapest universal scoring or speed formula, local topology/access links, pathfinding over unknown geography, renderer/UI, persistence/replay, world generation, military movement, or changes to the legacy direct-route system. It does not update architecture, Brief, Roadmap, State, or checkpoint IDs.

**Blockers:** none for this technical design. Endpoint scope is bounded to Hexes until an explicit known local connector and factual validation seam is approved; the design does not infer connectivity from a shared Hex or anchor resolution. Conflicting observations resolve by latest receipt day, then stable provenance key, with collisions rejected. A resolved `KnownUnavailable` belief excludes an option from new plans without asserting current truth. The stable explicit route policy input keeps selection deterministic while leaving product-specific route preference and formulas open. P8-E cannot ship its first scenario until it supplies a concrete policy and adequate actor-known estimates; that is a downstream content/consumer requirement, not a choice made implicitly by P8-D.
