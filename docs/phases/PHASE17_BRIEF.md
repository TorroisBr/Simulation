# Phase 17 — Strategic War v1

**Authority:** planning entry subordinate to `../SIMULATION_ARCHITECTURE.md`. **Readiness:** `DEFERRED`; no implementation checkpoint is schedulable.

## Objective and closure

War progresses through explicit strategic decisions, pressure and consequences without collapsing Battle result, hostility, control, occupation and War termination into one event. Its concrete v1 closure requires later entry design.

**Checkpoints:** to be defined at architecture/technical entry; no P17 checkpoint IDs are approved.

## Dependencies and gates

- **Hard semantic contracts:** Phase 7 Conflict/War/Battle distinctions, P16 operational movement/logistics, and explicit relevant territorial/political authority.
- **Hard capabilities:** promoted Battle foundations already exist; applicable P16 and other strategic input capabilities must exist before integrated War behavior.
- **Integration dependency:** War outcome consumes validated domain facts and writes only its own authorized state/consequences; Battle victory does not imply War victory or control.
- **Soft ordering:** runtime construction and richer economy may be consumers/suppliers, not automatic blanket prerequisites.
- **Architecture gate:** War goals, strategic pressure, control/occupation, ceasefire/surrender/peace and relation to future Polity need consumer-specific decisions.
- **Product gate:** the first strategic-war scope and political authority require explicit human choice.
- **Exclusions:** automatic Peace from Battle, universal Polity/government, complete diplomacy and a second combat engine.
- **Replay/fork sensitivity:** War identity/goals, pressure and control facts, orders, supply, decisions, outcomes and external interventions must be recoverable when authoritative.
- **Hotspots/parallelism:** War/Battle stores, political authority, territory, military position/logistics, runtime and diagnostics; distant implementation must wait despite isolated file opportunities.
- **Downstream unlocks:** future diplomacy, government and broader strategic consumers, without declaring them delivered.
- **Deferred:** exact strategic model, polity relationship, occupation/treaty semantics and additional future consumers.

P20's multi-participant activity capability does not redefine War or add a hard
dependency for strategic War. Large-scale execution may use aggregate units/
armies with its own participation, temporal and outcome semantics; compatible
shared mechanisms are reusable without identical small-group scheduling.
