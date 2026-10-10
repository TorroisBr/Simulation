# P12-G P8-D target-owner implementation review — 2026-10-10

**Verdict:** `VALIDATED_CANDIDATE` — independent exact-tip implementation review PASS.

- Candidate branch: `codex/phase12/P12GP8DTargetOwnerIdentity`
- Exact reviewed candidate tip: `92659d587fe684b1a884d585d213bb7c956cf4e8`
- P12 canonical base: `1f34fc1458e29569888c6e0f764012eddb7ecd88`
- Code commit: `b95740629b6b7b6c5ef4a2f5076e6352ae09f091`
- Code commit tree: `7971311c5adc409c772c99eea8ed71099e8f30ac`
- Validated `Assets` tree: `fd06e830670acfff5477420da4fe5cfa98c3d2aa`
- Exact reviewed candidate tree: `7971311c5adc409c772c99eea8ed71099e8f30ac`

The reviewer inspected the complete code and documentation diff against the
P12 canonical base, the P8-D exact-zero registration contract, and retained
validation artifacts. The candidate is a clean descendant of canonical. The
candidate tip is the code commit; no post-validation code changes are present.

The coordinator obtains fresh witnesses from the staged runtime's exact
`SpatialRouteKnowledgeStore` and `PersonRoutePlanStore`. For both selected
Daily-v1 target-owner rows, it requires exact owner identity, zero cardinality,
and zero local revision. The corruption matrix substitutes a distinct valid
empty owner while the registered target vector remains bound to the staged
owner. Restore rejects the mismatch before publication, preserves the source
session/token/graph, matches subsequent continuation against a control runtime,
and permits a subsequent valid retry.

Exact-tree validation passed `SimulationRuntimeAdmissionTests` 121/121, ALL
EditMode 2795/2795, official Smoke 5/5, and `git diff --check`; all test runs
reported zero failures, skips, or inconclusive cases. The independent reviewer
verified the XML and compressed-log SHA-256 values and decompressed raw-log
hashes against
[`../validation/P12GGraphRejectionCoverage/P8DTargetOwners-20261010/VALIDATION.md`](../validation/P12GGraphRejectionCoverage/P8DTargetOwners-20261010/VALIDATION.md).

The candidate changes only the restore coordinator, its admission tests, and
P12-G validation/review documentation. It contains no ProjectSettings edits,
protected `.meta` files, or pre-existing unrelated raw XMLs. Scope is limited
to the two selected P8-D target-owner checks. P12-G remains `WAIT_DEPENDENCY`,
P12-A remains `WAIT_DEPENDENCY`, P13 remains `BLOCKED`, and Phase 12 remains
open. No complete graph, capture, export, hydration, or downstream readiness
claim is implied.
