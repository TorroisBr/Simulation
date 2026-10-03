# Phase 16 — Military Movement & Logistics v1

**Authority:** planning entry subordinate to `../SIMULATION_ARCHITECTURE.md`. **Readiness:** first product scope approved; P16-A technical design is a reviewed candidate pending canonical promotion. No implementation is authorized by this Brief alone.

## Objective and closure

Armed Forces can occupy and change factual position under military movement semantics with meaningful logistical constraints. Force position, movement plan and execution remain distinct.

**Checkpoints:** P16-A — One Passage Military Movement with Finite Supply. The user approved this bounded scope on 2026-10-03. The candidate military owner, atomic movement, profile safety and validation contract are in [`PHASE16A_SINGLE_PASSAGE_MOVEMENT_CHECKPOINT.md`](../design/PHASE16A_SINGLE_PASSAGE_MOVEMENT_CHECKPOINT.md). No further P16 checkpoint is approved.

## Dependencies and gates

- **Hard semantic contracts:** Phase 7 ArmedForce identity/composition/position and Battle boundaries; P8 geography/passages and P14 material/supply meanings needed by the chosen slice.
- **Hard capabilities:** relevant promoted P8 traversal/spatial authority and P14 logistical/material foundation; P8 civil-travel execution is not itself military movement.
- **Integration dependency:** military movement updates force position via its domain authority and revalidates physical passage, without granting a Battle or War outcome automatically.
- **Soft ordering:** civil route planning may offer reusable observations/queries, but military knowledge, force size and supply remain distinct.
- **Architecture gate:** define initial movement/supply/permission/scouting semantics and operational temporal boundary.
- **Product gate:** resolved for P16-A only: one existing ArmedForce crosses one valid passage and consumes finite carried supply through a controlled operation. Broader logistics, timed routes and player command authority require separate scope.
- **Exclusions:** strategic War completion, automatic territorial control, full occupation and treating forces as ordinary Persons/travel parties.
- **Replay/fork sensitivity:** orders, force position/progress, passage/knowledge, supply state, casualties/consumption and authoritative commands.
- **Continuation-aware entry:** the bounded checkpoint must declare stable force/order identities, force-owned committed position/progress and supply state, logical movement boundary and deterministic ordering, causal commands/randomness, retained versus derived state, ID relationships, exact-state export and staged-hydration/validation seams, and profile admission or rejection. Before domain promotion, prove exclusion from selected P12 composition or integrate a fail-closed admission hook with negative validation serially with P12-B. This does not require P12 serialization code or full P12 closure; duration-based execution still consumes relevant promoted P18 capability.
- **Hotspots/parallelism:** `ArmedForceSpatialPosition`, spatial traversal, military stores, Battle validation, runtime/diagnostics; movement and material work may be isolated after shared contracts stabilize.
- **Downstream unlocks:** operational facts and pressure inputs for P17 strategic War.
- **Deferred:** exact pathfinding, movement formula, weather/scouting catalog and full occupation policy.

## Operational time integration — 2026-09-26

Movement/logistics with duration consumes the relevant P18 timeline/scheduler
and lifecycle contracts/capabilities. It keeps force-owned position/progress and
military execution semantics; it does not reuse Person actor availability by
assumption. Design can precede P18 promotion on stable contracts, but integrated
intraday movement cannot use an independent daily/host clock. Supply/passive
processes remain domain-owned. No Wind & Sail/weather mod, loader or alternative
renderer is required for P16 closure.

P20's small-group individual activity model is not mandatory for military
execution. Units/armies may consume compatible time/commitment concepts under
their aggregate domain authority without scheduling every Person in one shared
activity or converting an army into a synthetic NPC. No blanket P20 dependency.
