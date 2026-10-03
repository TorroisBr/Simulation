# Phase 13 — Historical Reconstruction & Fork

**Authority:** planning entry subordinate to `../SIMULATION_ARCHITECTURE.md`. **Readiness:** `WAIT_DEPENDENCY`; no implementation checkpoint is schedulable.

**Design-entry clarification (2026-10-03):** retention/checkpoint strategy and causal-input model work may proceed before P12 closure. `WAIT_DEPENDENCY` above applies to authoritative reconstruction/fork implementation, not to isolated design.

## Objective and closure

Reconstruct the authoritative state at any actually simulated boundary from the first onward, then independently continue a fork from it under compatible semantics. The guarantee does not extend inside generated pre-simulation backstory.

**Checkpoints:** to be defined at architecture/technical entry; no P13 checkpoint IDs are approved.

## Dependencies and gates

- **Hard semantic contracts:** existing `SAVE != REPLAY != HISTORY`, deterministic execution, first simulated boundary, and runtime mutation semantics.
- **World identity alignment:** architecture §91A requires a reconstructed fork to publish a new `WorldId` with parent WorldId and actual simulated boundary provenance after recovering the exact inherited truth/state. Pre-fork domain identities remain inherited; future branch-local allocations and P18 occurrences must be unambiguous without reapplying old effects. The precise namespace/receipt mechanism is a P13 technical design gate, not permission to narrow historical forkability. P13 mechanics are not a prerequisite for present-time factual projection.
- **Hard capabilities:** P12 continuation for the included world and recoverable initial/changed domain state; command/input semantics sufficient to preserve causal order.
- **Capability-level gate:** a fork of a chosen world/profile needs complete validated continuation of that world/profile, recoverable initial state and every simulated mutation/input through T, compatible execution, and new WorldId/provenance publication. For the accepted P12 daily profile this consumes its relevant B–G/A parity capability; administrative `P12 CLOSED` alone is neither a replacement for causal history nor a universal gate for design. No P13 implementation checkpoint is unlocked by this clarification.
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

## Shared-activity historical boundaries

Supported P20 history includes formation, independent agreements/withdrawals,
reservations, validated start, cancellation/abort and individual/shared effects
at their actual boundaries. A fork preserves the corresponding pending or
executing instance and participants, not a later final roster imposed on earlier
history. It consumes relevant P20 state/input semantics and P12 continuation
only when that activity capability is supported; no early replay implementation.
