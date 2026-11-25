# CORE-F P12-G save-scope exception fix — independent review

Verdict: **PASS** for the bounded exception-safety correction.

## Reviewed identity

- Exact commit/tree: `2f3892973c59e41fb4c08ef4f00a2c40ea82c316` / `4a6de7c150dce6e983d9d4b579f928c0c4e6c673`
- Reviewed against parent: `33e0667209f4cb8528c6e92c18f9020bd82ab2ce`

## Review result

The review confirmed that if source-witness capture or save-operation construction throws after registering the continuation-save scope, cleanup is attempted. A cleanup exception is retained diagnostically without replacing the original failure; `ExceptionDispatchInfo` rethrows the original exception with its stack. The focused regression asserts the same exception instance is observed and that `ActiveOperationCount` returns to zero. The normal false-return path continues to close the scope.

The witness retains exact token/list identity, snapshots census values, matches owner identity references, and checks mutation epoch under the single continuation-save scope. The implementation remains only a witness/admission seam.

No code findings remain within this diff. The review did not rerun tests; validation evidence is recorded separately in `VALIDATION.md`.

## Explicit exclusions

This review does not establish P12-G source payload capture, detached owner staging, target admission, image composition, atomic application storage, full save/load integration, P12-G completion, CORE-F completion, or Core canonical promotion.