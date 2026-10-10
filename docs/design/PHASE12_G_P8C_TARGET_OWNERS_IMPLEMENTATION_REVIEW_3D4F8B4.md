# P12-G P8-C target-owner implementation review — 2026-10-10

**Verdict:** `VALIDATED_CANDIDATE` — independent exact-tip code review PASS.

- Candidate branch: `codex/phase12/P12GP8CExactEmptyTargetEvidence`
- Exact reviewed candidate tip: `edea5a3a177fdfa06f91864e31eef6daaeb02aa5`
- P12 canonical base: `c57fd22bb058e43672c01d8f5755d5d29eaa40b0`
- Code commit: `3d4f8b403dd93c8872699455c3494105f73096a9`
- Code commit tree: `014a0b7e3755fda0e9eb4bcd557a7a627c175de2`
- Validated `Assets` tree: `c4d1b78a92e95f10d83726bcc5f98f791c68122d`
- Exact reviewed candidate tree: `1fba524e020c153bc80e0f3ef444b5a4596f50b9`

The reviewer inspected the complete candidate diff against the stated base,
the P8-C target-owner audit, its independent review, the prior-candidate
supersession note, and all retained validation evidence. The candidate is a
clean descendant of canonical. The review and validation commits after the
code commit are documentation/artifact-only; the `Assets` tree stayed equal
to the tested tree.

The coordinator reads fresh census witnesses from the staged runtime's exact
`LegacySpatialAnchorBindingStore` and `PersonSpatialPositionStore`. It
requires zero cardinality and matches the observed owner revision against
the target vector revision; it does not impose an independent revision-zero
requirement, consistent with the P8-C audit. The corruption matrix replaces
each owner with a distinct valid empty owner and verifies rejection before
publication, preservation of the source session/token/graph/health,
continuation parity against a control runtime, and a successful valid retry.

Exact-tree validation passed `SimulationRuntimeAdmissionTests` 119/119, ALL
EditMode 2793/2793, official Smoke 5/5, and `git diff --check`; all XML runs
reported zero failures, skips, or inconclusive cases. The independent reviewer
verified the XML and compressed-log SHA-256 values against the validation
manifest and confirmed decompressed raw-log hashes. Full artifact hashes and
paths are recorded in
[`../validation/P12GGraphRejectionCoverage/P8CTargetOwners-20261010/VALIDATION.md`](../validation/P12GGraphRejectionCoverage/P8CTargetOwners-20261010/VALIDATION.md).

The diff contains only the restore coordinator, its admission tests, and
P12-G validation/review documentation. It contains no ProjectSettings edits,
protected `.meta` files, or pre-existing unrelated raw XMLs. Scope is limited
to the two selected Daily-v1 P8-C target-owner checks. P12-G remains
`WAIT_DEPENDENCY`, P12-A remains `WAIT_DEPENDENCY`, P13 remains `BLOCKED`, and
Phase 12 remains open. No complete graph, capture, export, hydration, or
downstream readiness claim is implied.
