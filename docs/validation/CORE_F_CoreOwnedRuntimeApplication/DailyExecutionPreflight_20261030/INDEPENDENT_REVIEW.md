# CORE-F Daily Execution Preflight Review

## Verdict

**VALIDATED_CANDIDATE — PASS**, bounded to the pre-clock selected daily-execution preflight. This review does not establish complete CORE-F runtime/application acceptance and does not authorize canonical promotion.

## Exact identities

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Reviewed code commit: `2a07c6a63d979c9840f14a4c6305782e453ffe4c`
- Reviewed code tree: `a502b72e74f12e8cd4b68651243a5284f2216782`
- Exact parent/base: `771395e09aebcc1639df18a0b0a7be2e65e3ca5f`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Review outcome: independent exact-tip applicability PASS; candidate branch remote matched the code commit.
- Diff: exactly six implementation/test files, 421 insertions and 15 deletions.

## Reviewed behavior

The selected runtime now prepares a one-use per-day execution context while the existing advance lease and DailyAdvance operation scope are held, before advancing the clock. It binds the selected owner graph and execution roots, preflights the required selected actor, day-start, travel/P12, enabled demography, directive, Merchant, Economy and Knowledge contexts, and then reuses those prepared contexts after the clock transition.

For selected Unity Crime/Justice participation, the adapter verifies that each non-null Justice/Crime projection is bound to the exact Justice owner in the admitted graph. A stale projection is rejected before clock advancement. The established mixed-roster Unity fallback remains in place and does not require the selected Crime/Justice binding. Late, effectful day operations retain their prior order.

The existing P12 mutation scopes, 89-family census rules, owner-vector publication, completed-boundary sequence and capture-token issuance were not changed. The new Core and Unity regressions assert pre-clock rejection, unchanged day/Justice revision, no completed sequence/token, no Core owner/logger callbacks on failed preflight, and lease release. Existing assertions were strengthened rather than weakened.

## Validation evidence

- Official CoreValidation: cold 940/940 and warm 940/940; zero failed, skipped, inconclusive or unexecuted cases. Runner inputs and generated dependencies were stable. Original TRX files and runner manifest remain in protected local retention.
  - Cold TRX SHA-256: `92f2b6da4ec809cc39ca74ac362b619f95e336951f143b56a3d77aff1e77a84f`
  - Warm TRX SHA-256: `29d97daf13f820968482623da2e1e3558ee1a379ca0ba364489dd554815205b5`
  - Private runner manifest SHA-256: `9ff64e574c012aeaeb29940bfcdaeb8279260afde7938b36f4143b55c65a764f`
- Official Unity 6000.3.9f1 unfiltered EditMode: 3282/3282 passed; zero failed, skipped or inconclusive; validation inputs stable. The complete result includes all five Smoke cases and seven LongRun cases, all passed.
  - Original XML SHA-256: `72f2d42ba3d466f5bc7a2ab863be174e45a97bc7fd254b9c0d3999c5035cd8bd`
  - Normalized result-only package: `coref-daily-preflight-771395e-editmode.zip`
  - Package SHA-256: `1dc0f08ceb9eea23fd38b70c7d05a69d05298423c254f050afb85ea3b0c336ca`
  - Package manifest SHA-256: `e76208b66e1172951c6ea399afad2e589692f2eaa4cdba718199d1defb23b748`
  - The package contains normalized result XML only, removes output fields, passed the approved package verifier, and is retained beside this record.
- `git diff --check`: PASS.

The Unity input inventory also records unrelated local ProjectSettings values that were present during this run. They were not included in the candidate commit and remain untouched. This is a recovery checkpoint, not a promotion package for the completed CORE-F contract.

## Review limitation

The independent reviewer noted that the new stale-binding cases do not directly assert the shared mutation epoch. The reviewed control flow rejects before any P12 owner mutation, so this was non-blocking for this bounded preflight slice. Add a direct epoch assertion when a focused regression can exercise it without widening the current contract.

## Remaining CORE-F work

Core-owned live selected-owner census/capture, detached restoration, the operational DailyV1Application/TryStart, executable .NET host, and separate-process T/T+1/T+2 continuation remain incomplete. No full CORE-F acceptance or canonical promotion is claimed.
