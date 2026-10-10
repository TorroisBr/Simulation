# P12-G selected-P9 fingerprint consistency — exact-tip implementation review

**Verdict:** `VALIDATED_CANDIDATE` — independent exact-tip review PASS.

- Candidate branch: `codex/phase12/P12GP8DTargetOwnerIdentity`
- Exact reviewed candidate tip: `2240f38419a6fc07fc3e78a1eba180fe218c1fee`
- P12 canonical base: `f8fea5b603fdfb42dbbf11ddc447ce1e4931f9ce`
- Code commit: `3e1e1eea9aa94bc0c2bbb509faaccc45c35292a4`
- Code Git tree: `60ab88b969f7b626f9e2eb88340fba24c5a94750`
- Tested `Assets` tree: `d188470c2cf8a07ea0cd4c2de98773ed514b8798`
- Independent reviewer: `/root/p12g_p9_lineage_review`

The reviewer verified the exact candidate diff, the current P9 snapshot predicate,
and all retained validation artifacts. The test changes only the existing P12-G
P9 lineage rejection test. It first rejects malformed fingerprint text, then
changes one hexadecimal digit of the original selected-P9 fingerprint to make
a different 64-character lowercase SHA-256 value. It verifies the exact
fingerprint-equality diagnostic for the valid-format mismatch. Both cases fail
after source capture and before candidate-root staging. Each `finally` block
restores the original manifest field; the test verifies active-session identity,
completed-boundary token, health, and owner graph preservation, then proves
continuation parity and a successful valid retry.

The tested `Assets` tree is unchanged through the documentation/evidence-only
candidate descendants. Focused `SimulationRuntimeAdmissionTests` passed 121/121,
ALL EditMode 2795/2795, official Smoke 5/5, and `git diff --check` passed. XML
counts, XML SHA-256, compressed-log SHA-256, and decompressed raw-log SHA-256
were independently checked against
[`../validation/P12GGraphRejectionCoverage/P9FingerprintConsistency-20261010-VALIDATION.md`](../validation/P12GGraphRejectionCoverage/P9FingerprintConsistency-20261010-VALIDATION.md).

This closes only the selected-P9 well-formed-but-inconsistent fingerprint
rejection row, alongside the already covered malformed case. It does not close
the complete P9 lineage matrix, other P12-G graph/failure/no-replay/parity
gates, P12-G itself, P12-A, or P13. P12-G and P12-A remain
`WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains open. No capture,
export, hydration, or downstream readiness is implied.
