# Phase 13 — Historical Reconstruction & Fork

**Authority:** planning entry subordinate to `../SIMULATION_ARCHITECTURE.md`. **Readiness:** `WAIT_DEPENDENCY`; no implementation checkpoint is schedulable.

## Objective and closure

Reconstruct the authoritative state at any actually simulated boundary from the first onward, then independently continue a fork from it under compatible semantics. The guarantee does not extend inside generated pre-simulation backstory.

**Checkpoints:** to be defined at architecture/technical entry; no P13 checkpoint IDs are approved.

## Dependencies and gates

- **Hard semantic contracts:** existing `SAVE != REPLAY != HISTORY`, deterministic execution, first simulated boundary, and runtime mutation semantics.
- **Hard capabilities:** P12 continuation for the included world and recoverable initial/changed domain state; command/input semantics sufficient to preserve causal order.
- **Integration dependency:** reconstructed state and fork use the same authoritative domain stores and compatible execution as normal continuation.
- **Soft ordering:** generated initial worlds help validation, but a manually authored world can also establish the first boundary.
- **Architecture gate:** determine reconstruction/checkpoint retention strategy without assuming event sourcing or treating diagnostics/History as Truth.
- **Product gate:** any narrowed guarantee for simulated boundaries would change the constitution and requires explicit user decision; this Brief does not narrow it.
- **Exclusions:** fork within generated backstory and a mandated universal event log.
- **Replay/fork sensitivity:** initial state, every relevant mutation/input and compatibility boundary, authoritative RNG context and historical state needed to continue.
- **Hotspots/parallelism:** persistence, input capture, domain state evolution, versioning and runtime composition; design can explore early, implementation waits for continuation and causal-input capabilities.
- **Downstream unlocks:** safe historical forks for future runtime construction, material and war consequences; it is not a prerequisite for designing them to be reconstructible.
- **Deferred:** exact storage, checkpoint and replay mechanisms.

## Intraday and installation boundaries — 2026-09-26

The guarantee includes actually simulated intraday boundaries, not just dates.
Supported intraday reconstruction consumes P18's relevant temporal state/input
ordering and P12 continuation. Preserve compatible historical daily semantics
for pre-migration histories; do not infer intraday history that was never run.

Reconstruct authoritative outputs of generation and modules, or recover them
unambiguously under their original compatible inputs/versions. Explicit mod
retrofit during simulation is a historical mutation at its own boundary; a fork
before that boundary must not include its newly created facts. Installation
does not reexecute historical placement/scoring stages. P19 integrations require
their actual state/version/migration contracts, not a blanket loader dependency
for unmodded-world reconstruction. This adds no early replay/storage schema.
