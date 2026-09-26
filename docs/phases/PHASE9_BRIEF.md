# Phase 9 — Initial Deterministic Genesis v1

**Authority:** planning entry subordinate to `../SIMULATION_ARCHITECTURE.md`. **Readiness:** P9-A is `TECHNICAL_DESIGN_IN_PROGRESS` while its bounded checkpoint record receives independent review; implementation begins only after a pass. Any readiness update here is a candidate-branch planning update until promotion.

## Objective and closure

Produce a deterministic, semantically complete initial World Truth before the first actually simulated boundary, using the same world ontology as authored worlds. Generated backstory may explain that initial state but is not itself simulated history.

**Checkpoint:** `P9-A — Authored Bootstrap Genesis v1`. It establishes the dependency-aware, deterministic pre-start pipeline using the existing authored Unity bootstrap profile. Its reviewed scope and acceptance evidence are recorded in `../design/PHASE9A_AUTHORED_BOOTSTRAP_CHECKPOINT.md`. Later generation/content algorithms are separate future scope.

## Dependencies and gates

- **Hard semantic contract:** the existing initial-world and determinism guarantees. Stable P8 factual geography, identity and anchors apply only when a selected profile output explicitly consumes P8-owned facts; P9-A's authored profile selects none.
- **Hard capability:** a profile output that consumes P8-owned facts needs the relevant promoted spatial authority; P9-A's current authored outputs use the legacy bootstrap spatial model and do not need a P8 capability edge. No P9 profile needs the whole P8-E civil travel slice by default.
- **Integration dependency:** generated state must enter the same world authorities and compatibility/diagnostic boundary as manual state.
- **Soft ordering:** P8 travel completion may supply end-to-end validation but is not a blanket prerequisite.
- **Architecture gate:** deterministic generated identity, provenance, compatible content/configuration and the pre-start boundary need entry design; do not invent an ID algorithm here.
- **Product gate:** none established; surface one if world scope or generation guarantees require a user choice.
- **Exclusions:** runtime expansion/construction, lazy existence from observation, and a claim that generated backstory is simulated history.
- **Replay/fork sensitivity:** effective seed/random context, generated IDs, content/configuration provenance and full initial authoritative state must be recoverable as required by the constitutional fork guarantee.
- **Hotspots/parallelism:** spatial authority, composition/configuration, initialization, diagnostics and identity; entry architecture may proceed beside P8 work, implementation that consumes spatial code waits for relevant promotion.
- **Downstream unlocks:** local/pre-start generation (P10) and a consistent initial state for save and historical reconstruction.
- **Deferred:** exact generation passes/APIs, ID algorithm, mod schema, and exact content catalog.

## P9-A dependency and scope record — 2026-09-26

P9-A is scoped to the existing `TesteSimulacao.InitializeSimulation` authored
bootstrap profile. The implementation uses only the promoted capabilities
selected by this profile. It does not initialize P8 spatial facts where the
authored inputs provide no explicit P8 identities/provenance, and it adds no
P8-E route-plan, travel-progress, P18 temporal-activity, P20 participation, or
P19 extension-loader state. P8-A through P8-E are nevertheless canonical at
the implementation baseline.

P9-A has no dependency on P18, P19, or P20. Existing scheduled directives
retain their present domain semantics. The profile creates no P18 Activity
instances and no P20 shared participants. Current extensibility constraints
apply to stage seams, declared inputs/outputs, compatibility, ordering and
domain ownership; the P19 loader/API remains deferred.

The selected product scope is the user's approved authored-bootstrap-only
first delivery. No generated terrain, settlements, population additions,
local topology, pre-simulation backstory, runtime expansion, or new gameplay is
promised. `CityData.initialPopulation`, authored markets/liquidity, and other
existing authored inputs remain part of the profile.

## Extensible generation contract — 2026-09-26

P9 must design an ordered dependency-aware pipeline, with explicit stage
inputs/outputs, stable causal identities/version/provenance, deterministic
contribution/conflict order and appropriate random context. Validate the stage
graph and complete initial state before publication. Stage outputs enter their
owning domain authorities; no monolithic opaque `GenerateEverything()` or
separate generator truth. The first supported profile and algorithms remain
entry/technical decisions, not implied new biome/climate/settlement scope.

Future contributors can consume previous outputs, add persistent domain data,
contribute scoring/policies and insert stages with declared dependencies. P9
implements only its concrete generation needs; it does not wait for or implement
P19's public loader/API. Pipeline provenance joins the reconstruction inventory.
P18 is not a blanket prerequisite for genesis; a profile initializing temporal
activities consumes the relevant accepted/promoted temporal contracts.

New-world generation and optional explicit existing-world retrofit are distinct.
Installing a mod does not rerun historical stages or change past settlement
placement. Retrofit support belongs to the later module/domain migration scope;
runtime expansion retains its own post-start mutation boundary. Revalidate
pre-change P9 proposals before technical approval against this pipeline requirement.
