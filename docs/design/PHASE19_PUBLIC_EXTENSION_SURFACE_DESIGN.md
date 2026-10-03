# P19 — Bounded public extension-surface design entry

**Status:** `P19_PUBLIC_EXTENSION_SURFACE_DESIGN_READY` for bounded architecture work, not loader or API implementation. **Architecture base:** `da34d50bd7831ac3eefab31e925492ede8dded5c`. P19 has no approved implementation checkpoint ID. This record chooses no package, DLL, registration, manifest or save schema.

## First real surfaces worth designing

| Rank | Promoted real consumer | Stable semantic boundary to expose later | Current limit |
|---|---|---|---|
| 1 | P9-A/B deterministic pre-start genesis, with P10-A local topology as downstream authored fact | A contributor has stable identity/version, declared typed inputs/outputs and dependencies, deterministic ordering, isolated causal random context where used, validation and atomic pre-start publication. Its output enters normal World Truth before the first simulated boundary. | This is a **new-world** extension candidate only. P10-A is one authored Ruin; do not invent a generalized local-content catalog or retrofit from it. |
| 2 | P18-A/B/C temporal scheduler/activity lifecycle and P20-A synthetic shared operation | Definition versus instance, stable activity/participant IDs, declared temporal/commitment requirements, deterministic policy contribution and domain-owned outcomes are potential public seams. | P20-A proves generic coordination but no real shared gameplay consumer or universal role policy. Do not freeze a public activity API until a selected real consumer tests it. |

The first bounded public-contract design should focus on the P9 generation-contributor seam because it has a real ordered pipeline and an approved pre-start publication boundary. P10 local topology may be a typed output/consumer of that seam, not a second generator authority. A future official expansion and community module should be able to use the same semantic contract when that contract is actually approved. The public contract must not expose Unity rendering, `NpcRuntime`, private Stores or a polling `Update` loop as mandatory extension points. Prefer lifecycle hooks and explicit commands/events at owned boundaries, with deterministic ordering and rejection of duplicate/conflicting contributions.

The temporal/activity seam is a follow-on design candidate, not a loader requirement for P9 or a license to put new activity logic in `NPCRuntime`. P18's logical timeline remains the only clock. P20's actors remain individual actors; a mod-defined shared activity eventually needs participant policies and effects through supported contracts, not a special base-game manager. Its exact role API waits for a real bounded consumer and review.

## Lifecycle and historical compatibility

New-world contribution is distinct from installation into an already simulated world. Later installation must not rerun old P9/P10 stages, pretend generated backstory is simulated history, or retroactively create facts before the install boundary. An optional retrofit is an explicit runtime mutation at its own logical boundary, with validated domain authority, retained input/version/order and reconstructible results. Module-owned authoritative state enters the applicable future P12 profile only by explicit inventory/export/hydration compatibility; unsupported compositions reject. Modded fork claims additionally need P13 historical reconstruction of the module's causal input and state. Ordinary official worlds and the initial factual World Exchange producer have no blanket P19 loader dependency.

## Readiness and exclusions

This design entry can proceed in parallel with P12-B and P13 design, isolated from `SimulationRuntime`/composition writers. Before a public generation adapter or loader implementation is declared ready, approve a specific real module consumer, contributor failure/ordering/version policy, compatible content resolution, API ownership and independent technical review against current P9/P10 code. The loader remains `DEFERRED`; no universal hooks, arbitrary workflow engine, module persistence schema, transport/IPC design, adversarial mod-security platform, weather/sailing feature or implementation is approved here.
