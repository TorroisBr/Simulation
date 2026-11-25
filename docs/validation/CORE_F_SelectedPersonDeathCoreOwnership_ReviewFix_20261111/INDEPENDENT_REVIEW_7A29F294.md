# Independent exact-tip review — selected Person-death runtime boundary

## Verdict

**PASS — VALIDATED_CANDIDATE**, no actionable findings.

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Reviewed candidate: `7a29f29404695b1de911abff5085aa414f2c305b`
- Implementation base: `347bcda8f17aa9ad5c15ae539621dd21522f738c`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Local HEAD and remote candidate ref matched at review.
- The reviewed diff adds the Unity integration regression and evidence only; production source is unchanged from the already reviewed base.

## Review scope and findings

The regression calls `SimulationRuntime.TryExecuteCoreSelectedPersonDeath` with the real selected Core owner graph. It checks the exact Person/City/NPC owner references, P12 census integrity, one mutation-epoch increment, Person/City/NPC/action revision changes, and the resulting Unity death/action projections. Natural Mortality remains disabled. No assertions were weakened.

The earlier focused failure was from a superseded test-source hash and is distinguished in the validation record. The focused rerun and full report both match the committed test source.

## Validation and evidence verified

- Focused EditMode: 23/23 passed; run `0d2a8e00ebf84a78821b5387617a2ef7`.
- Full EditMode: 3,306/3,306 passed; run `2aad036bf67943c8a9e36bf48af6ae5f`.
- Official Smoke: 5/5 passed; the five Smoke identities are also in the full report.
- CoreValidation: 1,091/1,091 cold and warm on unchanged production and .NET test sources.
- `git diff --check`: pass.
- Evidence package verification: pass; archive SHA-256 `46d65000ecbd67637a5c1484e70e1cf722336a8ad037549ed53b91876d5e861e`.
- Current test-source SHA-256: `049d9848e755a231227555c893cd9343e768b3910357b23279a818c91fb3b159`.

The package contains 3,306 cases, including this regression as Passed, and all five Smoke cases as Passed. It has no absolute local paths.

No tests were rerun by the reviewer. No canonical promotion was performed. This review validates only the bounded selected Person-death Unity boundary regression; it does not claim broader P12 or CORE-F completion.
