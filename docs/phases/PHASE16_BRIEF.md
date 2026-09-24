# Phase 16 — Military Movement & Logistics v1

**Authority:** planning entry subordinate to `../SIMULATION_ARCHITECTURE.md`. **Readiness:** `WAIT_DEPENDENCY`; no implementation checkpoint is schedulable.

## Objective and closure

Armed Forces can occupy and change factual position under military movement semantics with meaningful logistical constraints. Force position, movement plan and execution remain distinct.

**Checkpoints:** to be defined at architecture/technical entry; no P16 checkpoint IDs are approved.

## Dependencies and gates

- **Hard semantic contracts:** Phase 7 ArmedForce identity/composition/position and Battle boundaries; P8 geography/passages and P14 material/supply meanings needed by the chosen slice.
- **Hard capabilities:** relevant promoted P8 traversal/spatial authority and P14 logistical/material foundation; P8 civil-travel execution is not itself military movement.
- **Integration dependency:** military movement updates force position via its domain authority and revalidates physical passage, without granting a Battle or War outcome automatically.
- **Soft ordering:** civil route planning may offer reusable observations/queries, but military knowledge, force size and supply remain distinct.
- **Architecture gate:** define initial movement/supply/permission/scouting semantics and operational temporal boundary.
- **Product gate:** scope of military logistics and allowable command authority requires approval at entry.
- **Exclusions:** strategic War completion, automatic territorial control, full occupation and treating forces as ordinary Persons/travel parties.
- **Replay/fork sensitivity:** orders, force position/progress, passage/knowledge, supply state, casualties/consumption and authoritative commands.
- **Hotspots/parallelism:** `ArmedForceSpatialPosition`, spatial traversal, military stores, Battle validation, runtime/diagnostics; movement and material work may be isolated after shared contracts stabilize.
- **Downstream unlocks:** operational facts and pressure inputs for P17 strategic War.
- **Deferred:** exact pathfinding, movement formula, weather/scouting catalog and full occupation policy.
