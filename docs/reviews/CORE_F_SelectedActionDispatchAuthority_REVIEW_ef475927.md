# CORE-F selected action dispatch authority — independent review

**Verdict: NEEDS_CHANGES**

## Reviewed identity

- Candidate branch: `codex/core/CORE-F-shared-daily-v1-app-host`
- Candidate tip: `ef475927a2070c371e283bd5fe61949ec64dba02`
- Source/test implementation commit: `a32baba3fd4ecc6bb38363dfe93eae87f20c4780`
- Reviewed implementation tree: `f1cbe04dcc95866707eddd2675e3aed877a94a26`
- Review base: `7ee230428ec7880b5631bce9b66690d9a061a370`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

## Finding

The new Core resolver covers ten dispatch kinds, but the added direct tests cover only Normal success/failure and malformed Normal, FleeCity, and unknown-kind inputs. They do not directly pin successful and failed nested-result mapping for every dispatch kind, failed-roll mapping across those variants, or the host-completion API boundary that prevents Unity from returning action success/result type. Add focused coverage before treating the checkpoint as accepted.

## Review conclusions

- The Core outcome is resolved before Unity presentation completion.
- Production dispatch constructors consistently pair the top-level success with the action roll and the kind-specific nested operation result. No production path was found that produces successful detail with top-level failure.
- The change preserves existing admission/commit ordering and the planned-sale partial-commit fault check.
- No P12 census, reservation, exact identity, revision, or epoch implementation changed; no existing assertion was removed.
- This is a bounded dispatch-authority slice only. It does not deliver the shared runtime, `DailyV1Application`, full P12 restore, detached host, or CORE-F acceptance.
- The candidate has a long ancestry shared with Core canonical at `a07402f`; it is not a standalone fast-forward promotion candidate and requires dependency/ancestry refresh before any promotion consideration.

## Evidence checked

- CoreValidation cold/warm: 867/867 each; private manifest and cold/warm TRX hashes matched the validation record.
- Focused Unity `EconomyTransactionTests`: 59/59, with stable input inventory.
- Full Unity EditMode: 3271/3271, with stable before/after input digest. The full suite includes all five Smoke test cases, all passing.
- The normalized full EditMode package verified against the protected original run manifest.
- `git diff --check` passed.

The independent reviewer inspected the exact candidate diff and retained evidence. This record preserves the actionable coverage finding; it is not an approval or promotion.
