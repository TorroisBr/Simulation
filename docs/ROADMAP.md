# Simulation roadmap — planning, not world architecture

`SIMULATION_ARCHITECTURE.md` remains the semantic authority. This roadmap names intended phase scopes and likely dependency directions; it neither reports delivered behavior nor authorizes implementation or canonical promotion. The owning `PHASE*_STATE.md` and current canonical code establish delivery. Phase numbers primarily organize planning and closure, not a requirement to execute whole phases serially.

Phases 5–7 are closed within their documented scopes. Phase 8 has consolidated architecture and approved macro checkpoints but no implementation. Phases 9–17 are planning entries whose checkpoint decomposition still needs entry design. See the Phase Briefs for scope and readiness, and `EXECUTION_MODEL.md` for the scheduling vocabulary.

| Phase | Planning objective |
|---|---|
| 8 — Spatial Truth & Civil Travel v1 | Factual Hex geography and a Knowledge-bounded civil travel vertical slice. |
| 9 — Initial Deterministic Genesis v1 | Deterministic, complete initial World Truth before the first simulated boundary. |
| 10 — Local Generation & Pre-start Authoring | Local content/topology generated or authored into the same initial ontology. |
| 11 — Actor Perspective & Commands v1 | Actor-limited information/decision authority and validated external commands. |
| 12 — Save & Deterministic Continuation | Save/load that continues with the same authoritative future under compatible inputs. |
| 13 — Historical Reconstruction & Fork | Reconstruct and independently continue any actually simulated boundary. |
| 14 — Productive Sources & Material Flow v1 | Bounded productive-source and material-flow truth. |
| 15 — Runtime Construction & Founding v1 | Factual creation of structures/settlements during simulated history. |
| 16 — Military Movement & Logistics v1 | Physical force movement and meaningful logistical constraints. |
| 17 — Strategic War v1 | Strategic War progression without equating Battle result with War result. |

Runtime World Expansion remains a future consumer without an assigned dedicated phase. Its absence from this sequence does not forbid a later insertion or assign it to Phase 15 by implication.

## Dependency directions, not blanket phase locks

- P8-A establishes factual geography. P8-B (passages) and P8-C (anchors/civil presence) may proceed with isolation after their shared identity/segment contract is stable. P8-D (Knowledge/route) consumes the needed passage and position contracts; P8-E integrates the travel slice. Each implementation dependency must distinguish accepted contract from promoted capability.
- Phase 9 may design against stable Phase 8 spatial contracts before P8-E closes; code that needs working spatial authority waits for the relevant promoted capability. Phase 10 consumes the relevant Phase 9 genesis and P8-C local/anchor contracts.
- Phase 11 entry architecture and Phase 12 causal-state inventory can progress independently of Phase 8 worldgen. Their code integrations must still wait for whichever concrete command, state, or domain capability they actually consume.
- Phase 13's product guarantee needs Phase 12 continuation plus recoverable causal inputs and initial-world/mutation semantics. Save continuation alone does not fulfill historical forkability.
- Phase 14 may design localized sources after spatial identity/anchors stabilize; route-dependent material flow waits for the relevant passage/travel capability, not necessarily for every unrelated Phase 8 consumer.
- Phase 15 consumes spatial identity/anchors and normal runtime mutation authority. Material-cost integration waits for the relevant Phase 14 contract/capability; it does not inherit genesis authority.
- Phase 16 uses Phase 7 force/Battle foundations, relevant Phase 8 geography/passages, and Phase 14 logistical/material contracts. Civil travel is not military movement.
- Phase 17 waits for the relevant military movement/logistics and explicit strategic, territorial, and political decisions. A Battle outcome does not automatically resolve War or establish control.

These are planning edges, not fabricated checkpoint IDs for Phases 9–17. Hard contract, promoted capability, integration, validation, architecture/product gates, and soft ordering have different scheduling effects as defined in `EXECUTION_MODEL.md`. A candidate branch never satisfies a canonical capability dependency by default.

## Changing the roadmap

Phases may be inserted, split, merged, or reordered when the approved product scope changes. Record the revised objective and dependency impact in this roadmap and affected Briefs; preserve existing Phase State history and stable checkpoint identity or explicitly retire/supersede an ID. Re-evaluate active candidates against the new canonical context. Do not convert a soft ordering preference into a hard dependency, or a proposed scope into constitutional architecture, without review.
