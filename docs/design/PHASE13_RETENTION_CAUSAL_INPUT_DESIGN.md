# P13 — Retention and causal-input design entry

**Status:** `P13_RETENTION_CAUSAL_INPUT_DESIGN_READY` for architecture/design review only; authoritative reconstruction and fork remain `WAIT_DEPENDENCY`. **Architecture base:** `da34d50bd7831ac3eefab31e925492ede8dded5c`. No P13 implementation checkpoint ID or storage schema is approved here.

## Recovery obligation

For every boundary T that was actually simulated, beginning with the first boundary after complete initial World Truth publication, the project must recover the authoritative World Truth and all continuation-relevant state exactly as it stood at T, then support independent continuation from T under compatible semantics. Generated pre-start backstory has no internal simulated boundaries. The logical identity of T includes whatever day/tick, same-instant causal order and profile/version were actually active; a date alone is not sufficient for a world that ran intraday work.

The recovery proof for T may use an exact compatible state at T, or an exact compatible earlier checkpoint plus a complete deterministic path of retained external causal inputs and effective content/configuration/random context through T. A checkpoint is an acceleration, not a replacement for earlier or intermediate boundaries. `History`, selective events and diagnostics cannot be treated as the authoritative state. Universal event sourcing is not required: autonomous decisions can be re-executed when their starting state and causal inputs deterministically reproduce their effects. If a domain mutation cannot be reproduced that way, its authoritative result or sufficient causal data must be retained at its actual boundary. Intent-only logs that omit created, changed or destroyed World Truth are insufficient.

## Causal inventory to preserve or recover

- Complete initial committed World Truth before first simulation, including authored/generated outputs, stage/contributor identity and compatible versions, effective configuration, calendar and content. Seed or preset name alone does not recover it.
- Stable world and domain IDs, allocator/sequence state, authoritative owner records, relationships and invariants, plans, commitments, Knowledge and random context required for the same future.
- External Actor/GM/editor/mod/retrofit input payload, authority, accepted logical boundary and deterministic order from the first durable input onward. A later save or P13 adapter cannot reconstruct discarded input causality.
- Creation, alteration and destruction of factual structures, settlements, roads, resources, organizations, employment, property and every other authoritative runtime fact at its real simulated boundary. A fork before creation excludes it; a fork afterward includes it.
- Effective policy/parameter/content revisions and compatible execution semantics for the period replayed. Missing incompatible contributors fail closed; installation/retrofit never reruns genesis retroactively.
- For an intraday supported profile: P18 logical instant, pending due work and lifecycle, commitments/availability, accepted input order, same-instant wave/sequence, occurrence receipts and idempotency/effect state. Historic daily worlds retain their original daily boundary semantics.
- For a chosen P20 or modded profile, the actual included participant/module state and input contracts; these do not silently join the first P12 daily profile.

Retention design must prove that no allowed compaction/pruning removes the only recovery path to any boundary within the product guarantee. Checkpoint cadence, physical format, storage backend, old binary/content retention and retention-cost policy remain later design/product decisions. Do not narrow the all-simulated-boundaries promise silently to make storage easy.

## Reconstruction and fork boundary

Recover into a private composition of normal authoritative owners. Validate compatible content/profile, all ID links, owner invariants and exact logical boundary before one publication. A failed recovery publishes neither partial World Truth nor a fork identity. Save/load on the same continuation preserves `WorldId`; a P13 fork publishes a new `WorldId` with provenance `(source WorldId, T)` only after exact recovery. Pre-T domain identities are inherited; post-T allocations, causal occurrences and P18 receipts must be unambiguous in the new branch without reapplying old effects. No separate `LineageId`/`BranchId` or universal event log is mandated. The concrete namespace/receipt reconciliation is a later bounded P13 technical-design gate.

## Capability DAG and possible first implementation

Design of retention adequacy, causal input capture and checkpoint recovery contracts can proceed now in an isolated worktree. No P13 reconstruction code is authorized by this record. Actual implementation for a chosen world/profile requires: (1) complete validated P12 continuation of every included owner, exact export and private staged hydration, whole-graph validation and parity (for `UnityBootstrap-Daily-v1`, relevant P12-B–G/A capability, currently blocked at B); (2) recoverable initial state and every causal input/mutation through T; (3) compatible execution/content/calendar/random semantics; (4) reviewed boundary/order, retention and failure policy; and (5) WI-A/P13 identity/provenance reconciliation. P18 temporal state is an additional capability edge for an intraday profile. Administrative `P12 CLOSED` alone proves neither history retention nor P13 forkability.

A future **candidate** first checkpoint could prove recovery and independent continuation at every simulated boundary of one explicitly supported daily world/profile, using a bounded history with at least one authoritative mutation before and after a chosen T. It cannot claim general intraday, modded or all-profile coverage. Its ID, storage approach, test fixture and technical design must be separately approved once the capability gates exist. Until then P13 implementation stays `WAIT_DEPENDENCY`.

## Review questions still open

Choose a retention/checkpoint strategy that satisfies all boundaries without assuming one file per boundary or a universal event stream. Decide old compatible code/content availability versus explicit unsupported-version rejection without retroactively claiming a fork where the guarantee was lost. Define precise same-instant T and branch-local allocation/receipt representation in the relevant future technical design. Product choices about deleting/pruning a world's history or branching copied saves need explicit approval if they affect the guarantee; this design entry does not choose them.
