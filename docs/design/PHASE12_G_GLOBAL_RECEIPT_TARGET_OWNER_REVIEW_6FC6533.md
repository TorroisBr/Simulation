# P12-G global receipt target-owner candidate review

## Verdict

**VALIDATED_CANDIDATE** — independent exact-tip code review passed. This review
record does not itself claim P12-G completion or Phase 12 closure.

## Reviewed identity

- Canonical base: `78a90d4558b8bffd80fa9cfa1eda7297e2f909ef`.
- Exact reviewed candidate: `c4d0faab0a705e6b14cf9754fb7f7dab1bf4fa1f`.
- Code commit: `6fc6533d5a0de8bd3bf72b79caac72257a055b3f`.
- Code Git tree: `0cc7156e74d37e77b9277f79c6e97951058dddfa`.
- `Assets` tree: `34da0c22f63cdd0f46cb3b2c3759392526821d31`.
- The remote candidate ref matched the reviewed tip, is a clean descendant of
  the canonical base, and contains only the test change plus validation
  documentation/evidence. Production scripts, ProjectSettings, and `.meta`
  files are unchanged.

## Findings

The two new parameter cases substitute the source composition's valid empty
`NpcDecisionRecorder` and `EconomyTransactionService` receipt owners into the
private staged compositions. Each test confirms source and candidate owners
are distinct and each receipt owner is cardinality/revision `0/0`. The
candidate runtime's target owner vector continues to identify its own installed
owner, while the composition now reports the source owner's identity. The
restore sentinel rejects each mismatch as `TargetOwnerVectorFailed` before
publication.

The shared failure-atomicity harness verifies the active source session,
token, health, and graph remain unchanged; subsequent normal advance matches an
uninterrupted control; and a later valid retry publishes the expected graph.
No runtime wiring or domain behavior changes.

No defect was found within the bounded test-only scope.

## Validation evidence reviewed

The exact-tree manifest is
`docs/validation/P12GGraphRejectionCoverage/GlobalReceiptTargetOwner-20261010/P12GGlobalReceiptTargetOwners-20261010-VALIDATION.md`.
It records focused `SimulationRuntimeAdmissionTests` 124/124, ALL EditMode
2798/2798, official Smoke 5/5, and `git diff --check` PASS. The two new cases
are explicitly passed in the focused XML. The review independently verified
all XML outcomes and hashes, all compressed-log hashes and decompressed raw-log
hashes, and that the validation used the reviewed `Assets` tree. No validation
rerun was needed because only review documentation follows the code commit.

## Scope boundary

This closes only two global receipt-cache target-owner substitution cases in
the P12-G rejection evidence. It does not claim complete target census,
complete §6 compatibility/rejection or failure-injection coverage, causal
no-replay, multi-boundary parity, P12-G completion, P12-A readiness, P13
readiness, or Phase 12 closure. P12-G and P12-A remain `WAIT_DEPENDENCY`; P13
remains `BLOCKED`; Phase 12 remains `OPEN`.