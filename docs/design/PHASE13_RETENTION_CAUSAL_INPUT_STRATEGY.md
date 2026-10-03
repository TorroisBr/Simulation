# Phase 13 Retention and Causal-Input Strategy

**Status:** bounded technical strategy proposal; independent technical review has not occurred. **Design base:** `da34d50bd7831ac3eefab31e925492ede8dded5c` on `codex/phase13/P13RetentionCausalInputDesign`. This document does not amend architecture, assign P13 checkpoint IDs, implement P12/P13, or claim readiness. **Authoritative reconstruction and fork remain `WAIT_DEPENDENCY`.**

## Purpose and boundary

This proposal gives P13 a recoverable-history model compatible with the approved guarantee: reconstruct authoritative state at every actually simulated boundary from the first one onward, then continue an independent fork from that boundary. It does not add pre-simulation backstory boundaries, narrow the guarantee to daily boundaries, require a universal event log, or turn diagnostic Events, History, or snapshots into World Truth.

The proposed combination is:

1. Preserve the exact initial authoritative state and effective execution context needed to reach the first simulated boundary.
2. Preserve immutable causal inputs and compatibility transitions, with stable logical-boundary identity and order.
3. Recover deterministic autonomous evolution by compatible execution from a complete checkpoint; retain an owner-committed semantic transition or a complete state image when an effect cannot be reproduced unambiguously that way.
4. Retain the history archive for every earlier forkable boundary. Full checkpoints accelerate recovery; they do not make older causes disposable.
5. Reconstruct privately, validate the selected profile and all owner relationships, then publish a fork atomically with a new `WorldId` and source-boundary provenance.

Names in this document describe conceptual record roles, not approved DTOs or a new generic domain owner. The authoritative stores remain their domain owners. A P13 history index may locate retained inputs, transitions, versions, and complete snapshots; it must not become another owner of domain facts.

## Authority and evidence at the design base

The strategy follows architecture §§91, 91A, 92, and 92A in [`SIMULATION_ARCHITECTURE.md`](../SIMULATION_ARCHITECTURE.md); the [P13 Brief](../phases/PHASE13_BRIEF.md); and the 2026-10-03 [P12 capability DAG audit](../architecture/P12_CAPABILITY_DAG_AUDIT.md) and [Master handoff](../architecture/P12_CAPABILITY_DAG_MASTER_HANDOFF.md). Those authorities say that save continuation, historical reconstruction, and selective History are different contracts; the project is not event-sourced by default; actual creation, change, and destruction must be recoverable at their real boundaries; and checkpoint recycling must not remove an earlier fork opportunity. They allow either compatible deterministic execution or preserved state, with external causal inputs retained. The guarantee includes supported intraday boundaries and does not reinterpret older daily histories as intraday histories.

The Phase 13 Brief has no implementation checkpoint IDs or Phase 13 State yet. Its design-entry clarification permits this document while reconstruction/fork implementation waits for complete continuation of the chosen world/profile, recoverable causal history, compatible execution, and identity/provenance. The DAG audit and Master handoff make the same distinction: the selected daily profile needs its relevant P12 B–G/A continuation and parity evidence; the administrative `P12 CLOSED` label alone does not unlock P13. The current delivery record is [`PHASE12_STATE.md`](../PHASE12_STATE.md), and the accepted daily-profile contract is [`PHASE12_TECHNICAL_DESIGN.md`](PHASE12_TECHNICAL_DESIGN.md).

Current source/contracts show these limits:

- `SimulationGenesisManifest` records genesis contract/schema, effective configuration, seed and source, fingerprint, calendar, stage/provenance records, output-owner names, and `FirstSimulatedBoundary = "advance-day:1"`. It is useful origin/compatibility evidence, but it is not an exact export of all initial authoritative stores and cannot by itself reconstruct the initial World Truth.
- P12 source contains owner-section census witnesses and partial admission/quiescence wiring. The census protocol is explicitly non-admitting on its own; the current State still reports P12-B incomplete and P12-A `WAIT_DEPENDENCY`. There is no complete selected-profile owner inventory, supported-write invalidation proof, exact immutable owner export, or staged hydration here. A cardinality/revision witness or diagnostic snapshot is not a checkpoint.
- In the daily runtime, `TryAdvanceDay` returns success only after `TryAdvanceDayCore` and the synchronous `AdvanceDayAfterClockAdvance` work complete. The clock advances before that body runs, so a failed or faulted partial advance is not a completed fork boundary. P18's `LogicalTimeline` has logical ticks, sealed inputs ordered by sequence, due-work ordering, causal sequence, resumable boundary commits, and a post-advance signal handoff. Those in-memory seams inform intraday coordinates but do not provide historical storage. The selected `UnityBootstrap-Daily-v1` P12 profile rejects P18 composition and does not claim intraday continuation.
- The [P11 Brief](../phases/PHASE11_BRIEF.md) requires command payload, authority, logical application boundary, ordering, acceptance/rejection semantics, and authoritative effects to be recoverable. In current source, `WorldCommandRecord` retains command ID, absolute day, origin, authority, kind, success, affected/created runtime IDs, event IDs, and diagnostic; it does not retain the command payload or a sub-day application instant. It cannot alone serve as the P13 causal-input ledger. The accepted P12 daily profile also excludes the external `WorldCommand` service/queue composition; a future supported profile must explicitly inventory and admit it.
- The architecture and Roadmap record WI-A as promoted, and [`WORLD_IDENTITY_FOUNDATION_DESIGN.md`](WORLD_IDENTITY_FOUNDATION_DESIGN.md) states the required same-branch/fork semantics. However, at this exact design-base source tree, a source search finds no durable `WorldId` value or world-publication implementation; the only source `WorldId` properties are the injected strings used by the P18 timeline profile. Treat the documented WI-A delivery as an upstream integration/reference that must be verified against the implementation branch before P13 code work. Do not infer P13 identity support from the P18 string.

These are current-base evidence limits, not claims that future P12 or WI-A work cannot close them. The design does not edit the upstream State or resolve the source/document mismatch.

## Proposed history and reconstruction contract

### Initial state and first boundary

At initial world publication, retain a recoverable origin capsule containing the exact authoritative owner state from which simulated execution begins, stable domain identities and allocator roots, effective calendar/configuration/content, compatible genesis-stage identities and versions, and the causal random context required to execute. The capsule may refer to immutable versioned content packages when their exact identity and availability are guaranteed; a mutable asset path, seed, or genesis fingerprint alone is insufficient. Preserve the exact outputs of genesis. Reconstructing a fork must hydrate those outputs and must not rerun generation or retrofit.

The capsule is the input to reaching the first actual simulated boundary, not permission to fork generated prehistory. For the current authored bootstrap, the manifest names `advance-day:1` as the first simulated boundary. P13's boundary catalog starts with that completed boundary and continues only with boundaries the simulation actually committed. This strategy does not add a fork point before the first simulated boundary.

### Actual boundary and mutation capture

Use a stable boundary key with at least branch `WorldId`, admitted profile identity/version, logical time, and a deterministic causal ordinal. A daily profile uses its completed daily boundary and stable order within that boundary. An intraday profile uses its actual `LogicalTick`, sealed-input prefix, same-instant ordering/causal sequence, and pending-work/continuation state. A date or day count cannot stand in for an intraday boundary. A boundary is forkable only after all synchronous work and required continuation/signal handoffs for it have succeeded and the resulting owner state is quiescent and coherent.

For each authoritative creation, change, or destruction, the owning commit path must leave enough durable evidence to recover the post-commit fact at that actual boundary. There are two compatible ways to satisfy this:

- Reproduce the owner commit by executing the pinned compatible simulation from a complete prior checkpoint with all causally relevant inputs, random context, effective definitions, and deterministic order preserved.
- Retain a versioned owner-semantic transition/receipt or a complete boundary image for an effect that compatible replay cannot reproduce unambiguously, such as an external result or a stateful installation/retrofit. The record must be associated with the real successful owner commit, not inferred later from a diagnostic event.

The strategy does not require a row for every autonomous decision if the compatible execution reproduces it exactly. It does require the state changes and their timing to be reproducible. Destroyed facts need enough retained identity/tombstone information to reconstruct earlier boundaries; later absence cannot erase earlier existence. Existing Events, selective History, and world-state diagnostics may help investigate a divergence but cannot substitute for these recovery paths.

Capture evidence at the owning mutation/commit seam and publish it only when that commit succeeds. The implementation must make a crash between a domain commit and its history receipt recoverable through the owner transaction, an idempotent write-ahead/receipt protocol, or an exact snapshot; a detached best-effort log is insufficient. If an operation fails after partial authoritative writes and the runtime cannot prove a complete boundary, mark the history segment faulted/incomplete and reject reconstruction through it. Do not publish a successful boundary marker for partial work or claim an implicit rollback.

### External inputs and authority

Retain each external attempt in an immutable, versioned envelope with the original command/input identity, canonical payload, source/actor and authority mode or decision, target logical boundary, accepted order/sequence, disposition/result, and references to authoritative effects. Store the full payload needed to reapply an accepted input through the same supported command/domain boundary. Record the authority outcome as it was decided; do not later reinterpret a past request using current permissions or hidden state.

Accepted commands are replayed exactly once when reconstructing from a checkpoint before them. Inputs at or before the target boundary are not reapplied after hydrating a checkpoint at that boundary. Rejected, duplicate, deferred, or terminally attempted inputs do not create World Truth effects by themselves, but retain their recorded disposition where command history, idempotency, or continuation state depends on them. Preserve their IDs and sequence state so a restored parent cannot accept a past input twice. A `Suggest` preview that was never admitted as an input is not promoted to a causal command merely because a UI displayed it.

For daily execution, record actual committed order and the accepted P11 terminal-input/idempotency state for any admitted actor-choice owner. For intraday execution, preserve the full sealed input prefix through the target tick; target tick, accepted sequence, pending inputs, due work, causal sequence/waves, same-instant order, dispatch limit, and any pending continuation/commit barrier are part of continuation. Inputs accepted after a fork are appended to the child branch's new segment and cannot rewrite the parent's prefix.

### Compatibility needed for replay

Each origin capsule, snapshot, input segment, and transition record identifies the compatible execution needed to interpret it: profile and owner-section versions; compatible Simulation build/runtime and numeric profile; effective policy/parameters and revisions; calendar; content/definitions and genesis or module stage/contributor identities and versions; random algorithm/state/stream context; and any temporal profile/tick/dispatch contract. Retain immutable content/build references or another exact compatibility mechanism for as long as an earlier boundary can be forked. A mutable preset name or seed alone is not a compatibility package.

Replay may advance only under a compatible implementation and declared content/profile. A missing package, unsupported owner, sequence gap, corrupt segment, or semantic version mismatch fails closed at the last verified compatible point; it must never be repaired by running today's semantics and calling the result historical truth. Explicit migrations or retrofit are themselves versioned causal changes at their actual boundary. They do not rerun past genesis and do not give new facts earlier existence.

## Checkpoint and retention policy

Use complete, immutable world/profile checkpoints as accelerators and verified recovery roots. A checkpoint is eligible only at a completed, quiescent boundary after the selected profile's exact owner inventory and relationship validation succeed. It contains every admitted authoritative owner section, explicit empty/excluded section declarations, stable IDs and references, allocators and causal roots, effective execution context, and all continuation state needed by that profile. It contains no cache only when the cache is proven rebuildable from included truth. Hydration stages privately; validate identity, owner coverage, and ID relationships before publishing the restored composition.

Initial cadence proposal:

- Retain a complete origin capsule and a complete checkpoint at the first supported simulated boundary.
- Add complete checkpoints at a deterministic profile-configured interval of committed boundaries, and at compatibility transitions or before an operation that would make the previous replay segment unavailable. For an intraday profile the interval counts logical committed boundaries, not wall-clock time or just calendar days.
- Reconstruct a target `T` from the nearest verified compatible checkpoint at or before `T`, then execute the exact retained causal-input prefix and compatible deterministic work through `T`. Validate the reconstructed state against the target boundary's retained identity/digest and owner coverage before exposing it.
- If an interval cannot be replayed compatibly, shorten/materialize that interval or retain exact owner-semantic changes/full state for it before advertising its boundaries as forkable. Do not skip an unreconstructible interval.

The interval is an operational tuning value, not a change to which boundaries are promised. The archive is append-only by causal branch. There is no rolling cutoff: retain checkpoints, input/order/transition segments, compatibility artifacts, and index data for every history interval needed to fork every prior actual boundary. Cold storage and compression are compatible if integrity and complete availability are verified before a fork; pruning is compatible only after an equivalent retained representation keeps every earlier boundary reconstructible. A finite retention window would narrow the accepted guarantee and needs an explicit product/architecture decision. It must not be introduced as an implementation default.

## Fork identity, provenance, and publication

`ForkAt(parentWorldId, T)` first resolves a catalog entry for a real committed boundary in that parent branch. It privately reconstructs the exact parent truth and continuation state at `T`; it does not use a later snapshot with fields rewound, replay commands after `T`, or rerun prior effects after hydration. The fork operation then allocates a distinct `WorldId` outside the simulation RNG and atomically publishes the validated composition with provenance `(parent WorldId, actual boundary T)`. Failed hydration, validation, storage, or identity allocation publishes no child.

All inherited pre-`T` domain identities, factual state, allocator state, and accepted command/receipt history preserve their meaning. The child records an immutable parent-history prefix through `T`; its next causal inputs and future branch-local allocations use the child identity/segment so the parent and child can evolve independently without collision. Historical input/effect receipts are imported as already committed provenance, not executed a second time. A display name, save path, genesis seed, or P18 continuation ID is not a replacement for `WorldId` or parent-boundary provenance.

The exact namespace/receipt bridge for P18 occurrences already derived from an injected `worldId` remains an explicit P13 technical gate. Pre-`T` occurrence and effect receipts must stay inherited and must not be re-keyed in a way that repeats effects; future child occurrences must be unambiguous under the new `WorldId`. The approved identity semantics do not yet select that implementation representation.

## Validation seams before any authoritative fork claim

These are required design/implementation proof points; no Unity tests are run or claimed by this document.

| Seam | Required evidence |
|---|---|
| Profile admission and checkpoint completeness | Exact selected-world/profile inventory; each required owner exports and privately hydrates exactly; unsupported or newly composed owners fail closed; ID references validate before publication. P12 census witnesses alone do not pass this seam. |
| Initial-state and mutation reconstruction | At a real boundary, exercise owner creation, change, and destruction; reconstruct just before and just after each commit and compare complete authoritative owner state. Check that failed or partial operations produce no false completed-boundary marker. |
| Causal-input fidelity | Round-trip payload, authority/source, target boundary, accepted order, dispositions, duplicate/idempotency state, and effect receipts. Replay accepted commands once; verify rejected/preview inputs do not mutate truth and historical outcomes are not re-authorized under current policy. |
| Compatible continuation | Restore from origin and each checkpoint under the recorded profile/build/content/calendar/RNG context; replay to every intervening supported boundary; compare exact owner state and deterministic next-step results. Remove or alter a compatibility artifact and require fail-closed behavior. |
| Retention and gaps | Corrupt/remove a required segment, introduce a sequence gap, or mark an owner unsupported; forks through that range must refuse publication. Archive/compaction tests must prove older boundary lookup remains complete. |
| Fork identity and independence | At a chosen `T`, compare child truth and continuation state with parent at `T`; require a new WorldId and exact parent/T provenance; preserve inherited semantic IDs and receipts; prove prior effects are not repeated and later parent/child inputs and allocations diverge independently. |
| Intraday profile, when selected | Reconstruct actual ticks with sealed inputs, same-instant sequence, due-work causal ordering, pending work/continuation state, and P18 owner receipts. Daily P12 parity is not evidence for this profile. |

After implementation, run these against each supported chosen world/profile and every supported boundary class, then compare canonical owner exports/digests and one or more subsequent continuations. A sampled daily-only pass cannot prove an intraday claim. Independent design review must pass before implementation readiness; P12/P13 State records must report actual owner coverage, exact profile, parity and limitations.

## Dependencies, limits, and unresolved questions

The strategy does not identify a new in-scope product or canonical semantic blocker: the approved broad fork guarantee, first-boundary limit, selective-history distinction, compatibility requirement, and new-WorldId fork provenance already answer those questions. No boundary subset, finite retention window, or new product behavior is selected here.

The following are genuine implementation/operational gates, not permission to claim P13 readiness:

- Complete validated continuation for the chosen world/profile, including the selected-profile owner inventory, supported mutation coverage, exact export/private hydration, and continuation parity. For `UnityBootstrap-Daily-v1`, the current P12 B–G/A chain and parity are still incomplete; the partial P12-B witnesses and the `P12 CLOSED` label alone cannot replace this evidence.
- A recoverable initial owner state and every relevant committed mutation/input through the selected boundary, with no history gap and exact compatible execution available.
- A verified durable `WorldId` publication/continuation integration on the code branch used by P13. The architecture tip's documentation and the source-tree mismatch above must be reconciled before code work.
- An independently reviewed design for durable record encoding, crash-atomic owner-commit/history receipts, checkpoint interval and storage budget, compatibility artifact retention, recovery/fork latency, and the P18 inherited-receipt namespace. These details must preserve the semantics above; they may not convert a storage constraint into a silent history cutoff.

Storage format, compression, exact checkpoint cadence, and operational latency/storage budgets remain open technical/product constraints. If the product later needs a finite retention window or a narrowed set of forkable boundaries, obtain an explicit product decision and architecture update before changing this policy. Until the named chosen-world continuation and history prerequisites pass, **P13 authoritative reconstruction/fork remains `WAIT_DEPENDENCY`**.
