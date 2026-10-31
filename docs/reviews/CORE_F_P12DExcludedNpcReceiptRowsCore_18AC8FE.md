# Independent candidate review — P12-D excluded NPC receipt rows in Core

**Verdict: VALIDATED_CANDIDATE (PASS).** This is an independent code review, not Core canonical promotion or CORE-F completion.

## Exact identities

- Repository: `https://github.com/TorroisBr/Simulation`.
- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`.
- Exact reviewed candidate tip: `18ac8fede402af3839061e870c82ef15a512402c` (tree `7a9ced483f0ff91392c9eda55345588459c7aad1`).
- Reviewed code commit: `457912c4805fedbfce48718def067fbdb6a280b2` (tree `864ad34cbcf5142da58fde6fc38ba3da347d261f`).
- Candidate base: `32b2fe0d175af04d03878e5ca58ecf8e05beeacf`.
- Current Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- The code commit adds only the two Core application changes and the focused Core test file. The later candidate commit adds only this validation record, the sanitized Unity package manifest, and its result-only archive.

## Review result

The reviewer inspected the full immutable Git-object diff from the base, the accepted P12-D NPC crosswalk and exact-zero receipt-owner design, and the current validation record. The implementation correctly enforces both excluded per-NPC rows with canonical schema-v1 IDs, `Required` role, exact `0/0` cardinality/revision, distinct non-null host identities, selected NPC coverage, and paired mixed-roster compatibility rows. The checks are placed before live capture publication and at both restored-vector validation points. No P18 receipt payload is exported or replayed, and no P18 writer is enabled by this checkpoint. No implementation defect was found.

The reviewer notes that the new malformed-row tests directly exercise the validator rather than injecting malformed rows through each production call site. Existing transaction tests cover general capture/restore failure behavior. This remains a test-coverage limitation, not an actionable finding or a claim of three end-to-end fault-injection cases.

## Validation evidence

The review independently verified the exact candidate’s evidence:

- Official CoreValidation: 984/984 cold and 984/984 warm; stable input digest `644b597a7f6c2444177e0d23c79877053f017291632e77924934a3d409062d5b`; all 537 input hashes match the candidate code/test files.
- Official Unity EditMode: 3284/3284 PASS with stable input inventory.
- Official filtered Unity Smoke: 5/5 PASS on the same Unity input digest.
- `git diff --check`: PASS.
- The normalized Unity result package verifies against the retained protected run manifest and original XML.
- CoreValidation originals remain protected; no public TRX package is claimed because the native runner did not produce the separate approved TRX source-context adapter.

The full evidence and privacy-safe package identities are recorded in [`VALIDATION.md`](../validation/CORE_F_CoreOwnedRuntimeApplication/P12DExcludedNpcReceiptRowsCore_20261031/VALIDATION.md). No validation, review, or source claim extends to P12-D completion, P12-A readiness, full 89-family closure, detached restore, or CORE-F completion.

## Integration constraint

Before any later canonical consideration, refresh current Core canonical and revalidate the candidate against that base as required by the execution model. This review authorizes no canonical promotion.
