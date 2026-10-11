# P12-G successful directive effect parity — independent review

Status: **VALIDATED_CANDIDATE**

Date: 2026-10-11

## Exact review target

- Current P12 canonical/base: `b30a89686752777ad8c7e5f9019e3166136055eb`
- Code candidate: `3c20b93b084777d2fe57709efebb66e2f83cd084`
- Code commit parent: `b30a89686752777ad8c7e5f9019e3166136055eb`
- Reviewed code Git tree: `f6a24951b2aca4df57121a6f9cd75ce939a4d737`
- Reviewed Assets tree: `ebad02a9bf540a8ca16e12c5cc614c2a226fa903`
- Candidate evidence tip reviewed: `96a44252545a28464b723d10165916bddd080304`
- Evidence commit parent: code candidate `3c20b93b084777d2fe57709efebb66e2f83cd084`
- Evidence-only delta: five XML reports, five compressed logs, and `VALIDATION.md`; no executable file changed after code validation.
- Pushed candidate branch: `codex/phase12/P12GSuccessfulDirectiveEffectParity`

## Review result

The independent reviewer inspected the complete two-file code diff, current P12-G Brief/decomposition/technical design, and exact validation reports and hashes. No actionable findings.

The Census provider preserves exact-empty cardinality/revision for bootstrap by default. Restored continuation opts out only of the empty-state condition and still rejects negative cardinality/revision while enforcing the exact section IDs, schema versions, owner-instance identities, and other registered census contracts.

The test uses normal selected-profile action execution: successful Steal populates the Crime/Social owners and creates a warrant; the selected Guard performs Arrest; restore occurs at the healthy completed day-2 boundary; the due scheduled EscapePrison action succeeds on day 3; and the terminal directive is not replayed on day 4. The test compares included-owner projections and continuation roots across source/restored paths and checks the one-time directive revision, sentence clearing, release, and exact bounty change. It injects no gameplay outcome or state.

The implementation is compatible with the accepted P12-E populated-owner snapshot contract and P12-G's requirement for deterministic normal continuation after successful restore. Reflection narrows the test action catalog; actions and effects still use normal domain APIs. No profile scope, owner, operation, epoch, persistence format, readiness label, or closure requirement is broadened.

The reviewer independently verified the five final XML counts (1/1, 12/12, 6/6, 2866/2866, 5/5), zero failures/skips, all XML/compressed/decompressed log hashes against `VALIDATION.md`, and `git diff --check` for both code and evidence deltas. Exact artifacts: [P12GSuccessfulDirectiveEffectParity/Final/VALIDATION.md](../validation/P12GSuccessfulDirectiveEffectParity/Final/VALIDATION.md).

## Limits and integration

This case proves only the named successful scheduled EscapePrison effect parity. It is not a separate invalid-restored-census failure-atomicity test, and it does not close the remaining P12-G compatibility, target-owner, failure-injection, causal no-replay, or broad included-owner continuation obligations.

No readiness changes are approved or implied. P12-G and P12-A remain `WAIT_DEPENDENCY`; P12-B through P12-F remain promoted within their recorded bounds; P13 remains `BLOCKED`; Phase 12 remains open. Canonical promotion remains a separate step under the repository's autonomous bounded-promotion preflight.
