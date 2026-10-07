# Architecture handoff — P20 checkpoint identity reconciliation

**To:** General Architect
**Status:** User decision recorded; canonical Roadmap change requested. This branch is an additive handoff only and does not modify canonical architecture.
**Canonical architecture baseline:** codex/architecture/world-identity-projection at e16796014d348e3b59da7ed848101c4c03926ba5.
**Owning phase baseline:** codex/phase20/canonical at fe4909a0fc371a2fedb55cb9cef086e5dbf63526.
**P20 scope candidate:** codex/phase20/P20CIdentityReconciliation at 22be1e07ef7a6dc40a82b7ebf490c103a4a293d7.

## Decision received

On 2026-10-07, the user resolved the P20 checkpoint identity conflict:

- P20-A remains the promoted Synthetic Multi-participant Operation.
- P20-B remains the promoted Daily-profile census admission checkpoint. Do not rename, reinterpret, or retroactively reassign its promoted identity or rewrite its history.
- P20-C is the preferred new identity for the bounded Two-Person Joint Civil Travel consumer. No P20-C reservation was found in current P20 canonical content or retained P20 checkpoint history.
- Exactly two Persons is a proving fixture only, not a universal P18/P20 participant limit.
- The prior joint-travel technical design may be reused/revalidated under P20-C if current contracts remain compatible.

The user also directed the Master to create this Architecture handoff if the canonical Roadmap requires General Architect ownership, without stopping unrelated executable work.

## Canonical mismatch to reconcile

The current architecture Roadmap at this baseline still says P20-B is joint civil travel and says the checkpoint identity must be reconciled. Current P20 canonical State instead records the promoted P20-B as Daily-v1 empty-owner census/admission. The owning Phase 20 Brief and State candidate at 7461d9b now records the user decision and P20-C scope while preserving the P20-B row and history.

The historical Roadmap wording is preserved as dated planning history. Please update the current Roadmap status and dependency view to state P20-A/P20-B/P20-C consistently, rather than rewriting the old promotion record.

## Recommended canonical Roadmap edits

1. Replace the current unresolved-conflict note with the decision above.
2. Update the current P20 consumer row and checkpoint sequence so that P20-B denotes Daily-profile census/admission and P20-C denotes Two-Person Joint Civil Travel.
3. Keep the 2026-10-03 planning entry as historical and annotate that its P20-B travel identifier was superseded by the 2026-10-07 identity decision. Do not alter P20-B's canonical State or prior review/promotion commits.
4. Show P20-C's semantic capability edges as promoted P18-A/B/C, P20-A, and P8-E. Preserve the P20-B Daily-v1 admission/rejection boundary as a composition constraint; it is not a new shared-activity behavior dependency.
5. Record that current P20-C design review is still required against the current architecture and owning Phase States. Do not mark implementation READY from the old P20-B label or review alone.
6. Preserve the broader activity rules: the two-Person setup is only a proving fixture; no universal two-person cap, Party/Group model, common interval, or fixed role policy is introduced.

## Design and code lineage to preserve

The historical design is docs/architecture/P20B_TECHNICAL_DESIGN.md at content tip 8afc463fb71112a0c7b8902e7e5673aee9e31bd9, with an independent PASS in docs/architecture/P20B_TECHNICAL_REVIEW.md against architecture f6924e63d8e5731da1d33021d0361e7defe6dad7. Keep those records immutable as history; the P20-C current-base revalidation is recorded in the Phase 20 candidate.

The P20 canonical Assets tree at fe4909a is 8b579d9f61ad3145b535b27cdd71a37d512a32b1, unchanged from the reviewed de24dff implementation tree. Its pre-promotion history includes joint-travel code commits 0a3e0a2, e93731c, 6e44c25, d14d235, and a234f20. The P20-B promotion State describes B's checkpoint scope as the Daily-v1 empty-owner admission seam. Preserve both facts: do not change B's recorded scope or discard/rewrite the existing code. The P20-C candidate must explicitly revalidate the current code and decide any remaining implementation delta before claiming P20-C delivery.

P20-B's admission remains important to P12: Daily-v1 accepts only absent/empty P20 travel state and fails closed on populated state. P20-C must not change that accepted P12 profile. No P12-B completion, P12-A readiness, persistence, P13 fork, P19 loader, or broader gameplay scope is implied.

## Requested General Architect action

Review the user's decision and the Phase 20 candidate/revalidation record. Then update canonical Roadmap and current P20 technical-design handoff references on the appropriate Architecture-owned branch. Record an exact current-base P20-C design verdict or findings. Keep the Phase 20 candidate on its separate owning branch until it receives the appropriate current-base review and normal P20 integration.

This handoff changes no canonical Roadmap, architecture contract, code, or checkpoint status.
