# Phase 8 — Spatial Truth & Civil Travel v1

**Authority:** approved phase scope under `../SIMULATION_ARCHITECTURE.md` §69; planning, not delivered State. **Readiness:** `READY_FOR_TECHNICAL_DESIGN`, not `READY_FOR_IMPLEMENTATION`. No Phase 8 implementation has started.

## Objective and closure

A finite, manually authored validation slice represents factual Hex/Location/passages. A civil individual backed by persistent `PersonId` occupies factual position, plans among known alternatives from Knowledge, traverses multiple segments, meets changed passage truth, stops without hidden-truth leakage, can decide to replan, and arrives through one spatial/travel authority. The small slice is not lazy regional World Truth.

## Approved macro checkpoints

| ID | Closure boundary | Dependencies |
|---|---|---|
| P8-A — Factual Geography | Finite Hex geography, stable semantic identity/coordinates, regional scale, terrain, and anchored Locations are factual and deterministic. | Consolidated §69; bounded technical design/review before implementation. |
| P8-B — Factual Passages | Factual traversal distinguishes terrain, road, barrier/crossing and a passage can change at runtime; contextual passage/cost can be queried. | P8-A relevant contract/capability; shared segment/identity boundary with P8-C. |
| P8-C — Legacy Anchors and Civil Presence | City/Site bridge has one authority; Person-backed traveler retains factual position including in transit. | P8-A relevant contract/capability; shared segment/identity boundary with P8-B. |
| P8-D — Knowledge and Route Plan | Stale/false observations and at least two known route candidates are evaluated without hidden truth, with deterministic selection. | Stable P8-B/C passage, position, identity contracts and needed capabilities. |
| P8-E — Civil Travel Vertical Slice | Departure, progress, changed crossing, factual rejection, appropriate observation, stop/replan and arrival use the same authority. | Relevant promoted P8-B/C/D capabilities and integration validation. |

Dependency direction is `P8-A → {P8-B, P8-C} → P8-D → P8-E`; B/C may proceed in isolated tracks only after their shared semantic contract is stable. An accepted upstream contract can enable technical design; downstream integration requiring behavior waits for promotion. Checkpoint-specific technical designs and reviews are still required where boundaries are nontrivial.

## Gates and boundaries

- **Hard semantic contract:** Hex/Location, passage versus adjacency, World Truth versus Knowledge, one authority per trip, Person identity versus loaded representation, determinism and reconstruction safety are consolidated in §69 and related sections.
- **Hard capabilities/integration:** as stated per checkpoint above; fixed-day legacy routes can remain only for explicit legacy consumers, not as a second authority in the new trip.
- **Soft ordering:** read-only Knowledge/test discovery can precede passage implementation; it cannot certify P8-D readiness.
- **Architecture/product gates:** technical design must surface any unaddressed semantic or product choice; no additional product decision is currently required to enter technical design.
- **Exclusions:** worldgen, runtime expansion/construction, military movement, encounter resolution, renderer, save/replay, universal Group, full terrain catalog, final pathfinding and speed formulas.
- **Replay/fork sensitivity:** Hex/structure IDs and topology, mutable crossing/passage truth, factual position/progress, travel commitment, actor Knowledge, effective scale/configuration, randomness and external travel inputs must remain reconstructible in principle. This phase does not implement persistence.
- **Hotspots/parallelism:** `SpatialAuthority`, `SpatialRuntime`, `TravelSystem`, `SimulationRuntime`, Knowledge, diagnostics, City/Site anchors and configuration; B/C require isolation and explicit integration ownership. P8-D/E must wait for their shared contracts/capabilities.
- **Downstream unlocks:** stable geography/identity for genesis and localized sources; passage/civil position for later travel/material and military consumers. They unlock only at the dependency level actually promoted.
- **Intentionally deferred:** exact Hex distance, coordinate representation, pathfinding, speed formulas, worldgen IDs, multi-grid, groups/encounters, military movement and save format.
