# Simulation roadmap — planning, not world architecture

`SIMULATION_ARCHITECTURE.md` remains the semantic authority. This roadmap names intended phase scopes and likely dependency directions; it neither reports delivered behavior nor authorizes implementation or canonical promotion. The owning `PHASE*_STATE.md` and current canonical code establish delivery. Phase numbers primarily organize planning and closure, not a requirement to execute whole phases serially.

Phases 5–7 are closed within their documented scopes. P8-A/B/C are canonical; `PHASE8_STATE.md` records subsequent candidates and readiness. Phases 9–17 retain their IDs and scopes. New Phases 18–19 introduce intraday execution and the later code-mod platform; their numbers do not put them after strategic War in execution order. See the Phase Briefs and `EXECUTION_MODEL.md` for readiness and scheduling.

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
| 18 — Intraday Temporal Execution v1 | One logical timeline, deterministic due-work scheduling, activity lifecycle and availability-driven actor decisions, with bounded legacy integration. |
| 19 — Code Mods & Public Extension Surface v1 | A later code-mod API/loader and supported extension lifecycle, including new mechanics/state and optional explicit existing-world retrofit. |

Runtime World Expansion remains a future consumer without an assigned dedicated phase. Its absence from this sequence does not forbid a later insertion or assign it to Phase 15 by implication.

## Placement of temporal and extension work

P18 is one phase with dependent checkpoints, rather than separate phases for
every timed consumer. P18-A (timeline/scheduler) precedes P18-B (activity
lifecycle), then P18-C (actor availability/decision integration). P18-D adapts
the relevant existing daily processes, actor input and travel consumers after
their actual contracts/capabilities are available. Entry/technical design can
begin now; implementation is not authorized by this roadmap.

Prioritize P18 foundations before consumers promise work shifts, arbitrary
activity duration, intraday opportunities or complete intraday saves. P8-E's
explicit-operation travel proof and the P11 daily SellGoods slice can proceed
as bounded transitional capabilities after impact review; neither proves P18.
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

generation pipeline contract → P9 → relevant P10 generation stages
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

## Dependency directions, not blanket phase locks

- P8-A establishes factual geography. P8-B (passages) and P8-C (anchors/civil presence) may proceed with isolation after their shared identity/segment contract is stable. P8-D (Knowledge/route) consumes the needed passage and position contracts; P8-E integrates the travel slice. Each implementation dependency must distinguish accepted contract from promoted capability.
- Phase 9 may design against stable Phase 8 spatial contracts before P8-E closes; code that needs working spatial authority waits for the relevant promoted capability. Phase 10 consumes the relevant Phase 9 genesis and P8-C local/anchor contracts.
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
- Before the first durable intraday command: retain payload, authority, exact
  logical boundary and causal ordering. A date alone cannot order intraday effects.
- Before P9 implementation: reviewed bounded profile and stage inputs/outputs,
  dependencies/contribution ordering, identity/provenance, isolated random context
  and complete pre-start publication. P19 implementation is not a prerequisite.
- Before P19 implementation: real supported extension scope and public contracts;
  define module compatibility/state lifecycle, deterministic composition and
  explicit new-world versus optional retrofit behavior. No generic security platform.
- Before claiming save/fork with mods or intraday execution: supported temporal
  and extension-state inventory, recoverable compatible code/content, inputs and
  history; no missing causality may be reconstructed retroactively.

The dated impact record is `architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md`.
Promoted P8-A/B/C and closed Phases 5–7 remain valid in their delivered scopes.
P8-D/E and P11 candidates require targeted revalidation of temporal adapters,
not wholesale redesign. P9 and P12 entry proposals need their new pipeline and
temporal/extension inventories incorporated before further approval. No existing
phase is automatically implementation-ready, cancelled, or retroactively rewritten.

## Changing the roadmap

Phases may be inserted, split, merged, or reordered when the approved product scope changes. Record the revised objective and dependency impact in this roadmap and affected Briefs; preserve existing Phase State history and stable checkpoint identity or explicitly retire/supersede an ID. Re-evaluate active candidates against the new canonical context. Do not convert a soft ordering preference into a hard dependency, or a proposed scope into constitutional architecture, without review.
