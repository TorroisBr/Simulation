# P12-G registered source-owner witness — exact-tip review

**Result:** `VALIDATED_CANDIDATE`

**Repository:** `TorroisBr/Simulation` (`origin`)
**Current P12 canonical/base:** `7ad325b7fe5f9003be0947d18798182496eace31`
**Candidate branch:** `codex/phase12/P12GRegisteredOwnerSourceWitness`
**Reviewed candidate tip:** `7910a2383b8e1bd46649357067982c5eac0a48fb`
**Code commit:** `a5af39f3323be8a8de4e01eb81161d200b7f0e53`
**Code Git tree:** `24237c5d00a7f2929cfcb753564150f23b2a0563`
**Reviewed Assets tree:** `480136d37053888910a54a134154ce9a4c105256`
**Candidate Git tree:** `2d4404d4ad69e44959be50b590e817dc38f58e42`

The candidate is a clean two-commit fast-forward from the exact P12 canonical base. Its code-bearing commit changes only the selected Daily-v1 composition test. The later candidate commit adds State and validation evidence; it leaves the reviewed Assets tree unchanged.

## Review findings

The test binds four registered protocol rows to their independently installed source owners, using the existing exact-owner helper:

- Justice records resolve to the installed `JusticeSystem`; expected cardinality is wanted records plus sentences, and revision is read from that same owner.
- Justice P18 receipts resolve to the installed `JusticeSystem`; the required sentinel has cardinality one and uses the owner's current receipt revision.
- NPC decision-occurrence receipts resolve to the installed `NpcDecisionRecorder`; the row is explicitly empty and uses the owner's direct witness identity and revision.
- Economy keyed-sale receipts resolve to the installed `EconomyTransactionService`; the row is explicitly empty and uses the owner's direct witness identity and revision.

The assertions preserve source cardinality, role, schema, and revision semantics. They add no runtime behavior, owner, profile row, or operation. Existing Crime receipt registration evidence remains unchanged. No correctness, scope, temporal-identity, or mutation-authority finding blocks this candidate.

## Validation evidence

The validation manifest and exact artifacts are in `docs/validation/P12GRegisteredOwnerSourceWitness/VALIDATION.md`. The independent review verified all XML test counts/statuses, all compressed-log exit codes, and the recorded SHA-256 values. XML hashes match the recorded Windows CRLF-normalized bytes; compressed-log hashes match the stored bytes.

| Run | Result | XML SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|
| Focused `SimulationBootstrapCompositionTests` | 27/27 PASS | `E6046EA05ACB337C4D86010741DAA2DBC94D6C437AC49692F1A8E6C003EA5FF6` | `5DF1F5D904F600B16D1F0FE7864189F40EDC4A85EEC6C6B8ADF21EDD3449302E` |
| ALL EditMode | 2740/2740 PASS | `F67B27ADC1DEC19BBC8C6557076A5E0B42BD110CE70C8BAAB9060FE11A747EFA` | `BFEC3873E0D914C5B9CFF91BBD7AFEB33AC06C9FE49928D23226E712A56C6194` |
| Official Smoke | 5/5 PASS | `0742C6AE7E3F976192739AB5D1464D0B0BA5990EF2844A115D1C9E773B0870A7` | `F50EAC6F0056797249E26A1E22B659641B6AADB127EC0240E6F9FC3C9A222551` |

`git diff --check` from the base through the candidate passed with exit code 0. The Assets tree is unchanged after validation artifacts were recorded. No tests were rerun during review.

## Baseline and retained limits

The current Architecture General baseline is `47eff220c7ce00f6e7c759bdc2b76780bb46f628` (architecture blob `25843842688239cdc3b80988b2e28dbaa16b4987`). The P12 candidate branch contains an older architecture-file blob `4a3c73c4428ba7bc43c28f617e243e4cd54078fa`; this candidate does not modify that file, and the test-only assertions were reviewed against the current Architecture General baseline. The branch-local architecture-file drift has no semantic conflict with this bounded source-owner test slice.

This closes the four registered source-owner identity checks only. It does not close the full 299-row live owner/cardinality/transition and writer/epoch join, same-attempt target census, restored-graph coordinator, whole-graph rejection, failure atomicity, no-replay, deterministic continuation parity, or any capture/export/hydration readiness. P12-B through P12-F remain promoted within scope; P12-G and P12-A remain `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains `OPEN`.

**Reviewer:** independent P12-G exact-tip reviewer (`/root/p12g_source_witness_exact_review`). The reviewer did not modify the candidate or rerun tests.

