# P20-C Master handoff — Two-Person Joint Civil Travel

**Status:** Scope accepted by the user on 2026-10-07. Current-base design revalidation is preliminary and awaits independent General Architect review.
**Architecture baseline:** codex/architecture/world-identity-projection at e16796014d348e3b59da7ed848101c4c03926ba5.
**P20 canonical baseline:** codex/phase20/canonical at fe4909a0fc371a2fedb55cb9cef086e5dbf63526.
**Phase 20 candidate:** codex/phase20/P20CIdentityReconciliation at 7461d9b9e9b3fd8d5593dd8bd58c0a19dfb56924; see docs/design/PHASE20C_JOINT_CIVIL_TRAVEL_REVALIDATION.md.

## Checkpoint identity

P20-A remains Synthetic Multi-participant Operation. P20-B remains Daily-profile census admission and retains its promoted identity, review, scope, and history. P20-C is the bounded Two-Person Joint Civil Travel consumer.

Exactly two Persons is a proving fixture, not a universal participant limit. The consumer uses distinct PersonIds, independent assent, one supported civil leg, P18-owned activity lifecycle/commitments, and P8-owned travel state for each Person. Start and required terminal transitions must be coherent across both individual travel authorities and the P18 activity. Each Person retains separate identity, Knowledge, position, and outcomes.

No persistent Group/Party membership, shared Person identity, universal role catalog, recruitment AI, common whole-interval assumption, P18-D dependency, War/gang/robbery gameplay, save/load, P13 fork, or P19 loader is included.

## Reused design and current code

The historical P20-B joint-travel design and its independent review remain immutable source artifacts. The Phase 20 current-canonical revalidation finds no semantic incompatibility with current P8-E/P18-A/B/C/P20-A contracts, but this remains a preliminary Master assessment until independent review against current refs.

Joint-travel source code is already present in current P20 canonical history. The latest P20 executable Assets tree is identical to reviewed tree 62f8f3f3ad803e3f8eca832f7e39cff8196d5b85. Preserve that code and exact-tree evidence. Do not count it as a P20-C promoted result solely from the P20-B promotion: P20-B's State records only Daily-v1 census/admission. A P20-C delivery claim needs a current-base review that names P20-C, validates the consumer contract and P20-B/P12 Daily rejection behavior, and records any required new code delta without duplicating or deleting existing behavior.

## Dependency and profile boundary

P20-C consumes promoted P18-A/B/C, P20-A, and P8-E. P20-B is retained as the admission boundary for the travel owner in Daily-v1: that profile remains explicitly empty and rejects populated P20 travel state. Do not widen P12 Daily-v1 or infer P12-B/P12-A/P13 readiness.

P20-C does not wait for P18-D, P12 save capability, P13, P19, P14, or construction/military phases. P11 is conditional only if a separate typed external command input is selected; the reused design does not require it.

## Required next review

General Architect: update the architecture-owned Roadmap identity/dependency record and independently review the current-base P20-C design revalidation. Preserve the historical P20-B handoff and design files. Return a specific current-base verdict and any required findings.

Master: after that review, re-inspect the current P8/P18/P20/P12 refs and existing P20 code before deciding whether a P20-C code delta is needed. Any changed code tree requires its own validation and exact-tip implementation review. This handoff makes no implementation-readiness, promotion, or Phase 20 closure claim.
