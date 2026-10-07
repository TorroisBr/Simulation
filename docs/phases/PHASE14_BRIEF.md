# Phase 14 — Productive Sources & Material Flow v1

**Authority:** planning entry subordinate to `../SIMULATION_ARCHITECTURE.md`. **Readiness:** P14-A is `PROMOTED`; Phase 14 remains open for any separately scoped follow-on checkpoint. The approved scope remains limited to the profile below. The overlapping P9-A bootstrap/genesis integration is promoted.

## Objective and closure

Establish a bounded, factual account of productive sources and material movement with explicit ownership/custody and source/sink semantics. The selected first slice is one authored City, one configured exogenous daily source for one item, free same-City population consumption of that item, and closure at the closing aggregate market balance. Its approved implementation contract is `../design/PHASE14A_LOCAL_DAILY_MATERIAL_FLOW_CHECKPOINT.md`.

**Checkpoints:** P14-A — Local Daily Material Flow v1 (promoted to `codex/phase14/canonical` at `c44904bb4b0a066eced1d7e8a773b7dc1eea76c0`; State promotion record `f8a61fe9634ba9ab56ee31d50b57b45fef292a6f`). This delivers only the approved bounded local source/material-flow capability.

## Dependencies and gates

- **P14-A promoted-capability edges:** P8-A stable factual `LocationId` identity and the promoted P8-B/C City-anchor composition used to bind the authored City instance to that location. The profile verifies that the City anchor resolves to the same stable location. This slice does not consume P8-D passage or P8-E travel.
- **P14-A semantic contract:** preserve the architecture distinction among ownership, custody, control and economic flows. Settlement title over the configured source/material remains distinct from the market store's custody of aggregate stock.
- **Route-dependent flow:** a later transport/route checkpoint additionally requires the relevant promoted passage/travel capability. Do not impose all of P8-E on a local source without need.
- **Integration dependency:** material movement that uses travel must integrate with the single spatial/travel authority, not parallel city-to-city fixed durations.
- **P9 ordering and ownership:** P9 is not a semantic prerequisite for this manually authored profile. P9-A was promoted at code tip `43f08b3`; its State/Brief closure was recorded at canonical tip `96f2c1a`. The former serial integration/ownership edge to overlapping P9-A startup/composition work is satisfied, without creating a P9 generation capability dependency. P10 content is likewise optional for the authored profile.
- **Checkpoint gate:** current-base independent review passed for P14-A candidate checkpoint content `7a9e8d7` and technical design `565a3c0` against P8 canonical `470667d`, architecture `c285466`, and both alignment records.
- **Resolved profile/product choice:** the user approved the one-City, one-exogenous-source, one-item daily profile stated above. This does not authorize added sources, items, Cities, or gameplay.
- **Deferred product scope:** changes that add sources, inputs, finite reserves, transformations, paid consumption, transport, multi-worker production, or a different closure boundary require a separately scoped checkpoint; they are not implicit extensions of P14-A.
- **Exclusions:** universal macroeconomy, automatic trade network, general taxation and full supply simulation.
- **Replay/fork sensitivity:** source identities, stock/ownership/custody, transformations, transfers, constraints and commands must be recoverable when authoritative.
- **Hotspots/parallelism:** economy/merchant, property, spatial anchors, travel, runtime composition and diagnostics; localized truth design can advance before route-flow integration.
- **Downstream unlocks:** material costs for P15 and logistics/supply for P16 where those consumers need them.
- **Deferred:** exact goods catalog, production formulas, market macro-policy and global material optimization.

P14-A's daily passive source/sink profile has no P18 or P20 dependency. P18 is
conditional on a later approved consumer promising duration-based or intraday
production/transport; P20 is conditional on a later consumer coordinating
multiple participants. P19 loader/public API work remains deferred, while the
current semantic-extension and deterministic-composition constraints apply to
review now. See the P14-A contract and both current architecture alignment
records for the corresponding causal and identity boundaries.

## Temporal consumer and extension boundary — 2026-09-26

Localized source/stock/ownership truth remains independent of P18. A slice that
promises duration, work shifts or intraday production/transport consumes the
relevant P18 scheduler/lifecycle contracts and capabilities; passive production
does not become a mandatory actor-selected action. No new work/rest/theft
behavior is required for infrastructure validation. Use existing semantic
source/transfer seams and composable policy contributions only when actually
needed; no mod loader or universal production extension engine in P14.

If a future chosen production consumer requires several actors, its shared
participation/commitment coordination consumes relevant P20 capability on P18.
Static source/stock truth and passive production remain independent; no crew,
job, hunting or meal system is introduced for this requirement.

## Architecture-ordered P14-C checkpoint — 2026-10-07

The current architecture product-direction record (`P10_P14_P20_NEXT_SCOPE_DECISIONS.md` at architecture tip `e16796014d348e3b59da7ed848101c4c03926ba5`) sequences **P14-C — Multiple Identifiable Sources v1** after promoted P14-B. The bounded proving case is one City/item with two distinct source IDs, deterministic source ordering, and one closing material balance; the recommended proof combines one P14-A exogenous source and one P14-B finite source. P14-A and P14-B remain unchanged. This is a separate P14 profile and does not combine the P10 Ruin profile, add a City/Location, or widen P12 Daily-v1.

P14-B is promoted at State `06e9c30101a74bd618d3651885c489c79fe866bb`; its dependency for the recommended P14-C proof is satisfied. The bounded design candidate is [`PHASE14C_MULTIPLE_IDENTIFIABLE_SOURCES_CHECKPOINT.md`](../design/PHASE14C_MULTIPLE_IDENTIFIABLE_SOURCES_CHECKPOINT.md). Its bounded technical design passed independent review; see [`PHASE14C_DESIGN_REVIEW.md`](../PHASE14C_DESIGN_REVIEW.md). No P14-C implementation has started.
