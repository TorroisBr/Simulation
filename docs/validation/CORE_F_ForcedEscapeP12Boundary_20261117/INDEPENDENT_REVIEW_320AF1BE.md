# Independent review — CORE-F forced Escape boundary

## Reviewed source

- Candidate: `320af1be97d79b35cb7d12559320c79028850af1`
- Parent: `5b4c3ee17515a47bf1873cd61e4f196afe38342b`
- Tree: `804bddcc40da199a219e3e542007e7db0aa81b65`
- Scope: hostless Core execution of the existing scheduled `ForceOutcome/EscapePrison` action and its exact P12 Crime/Justice owner operation.

## Verdict

**VALIDATED_CANDIDATE.** Independent exact-tip review found no actionable defects.

The review confirmed that the hostless path admits only the exact supported targetless Escape action, uses the admitted selected owner graph and sealed census identities, and reuses the already-open Crime/Justice operation scope for nested status mutation. Unsupported dispatch inputs fail closed. The Justice census keeps its owner identity while the admitted sentence removal changes row cardinality and publishes the committed owner revision. Terminal directive mutation updates its census baseline and the shared mutation epoch. `ForceOutcome` does not allocate an autonomous decision for its selected actor. The added tests strengthen the hostless Escape and P12 assertions; existing assertions were not weakened.

## Validation evidence

- Official CoreValidation: **1,144/1,144 PASS**, cold and warm. Run ID: `cdecabf04a0649debd03f8f889f7cebb`.
- Official Unity EditMode: **3,321/3,321 PASS**. Run ID: `0ba417641a064d4194e2f53ee6405d88`.
- Official Unity Smoke: **5/5 PASS**. Run ID: `805f00dbb27746889d5e7a9e0470b51b`.
- The committed bytes of all six changed production/test files match their CoreValidation manifest input hashes. The five changed production files match the Unity run input manifest. The code/test tree is unchanged from the validated inputs.
- Verified full EditMode evidence package: SHA-256 `d25b2e8d6e1f2eafc2eff8f8eded8f621b7e5062d6c8a207c6f4b25a37d2fe16`.
- `git diff --check`: PASS.

The separate raw Smoke run passed, but its filtered packaging attempt was rejected because the aggregate XML suite count did not match its descendants. The original report was preserved unchanged; the same Smoke tests are included in the verified full EditMode run/package. CoreValidation records `worktreeLibraryAbsent=false`; this review makes no Library-independence claim.

## Boundary

This record reviews the candidate only. It does not claim remote publication, Core canonical promotion, or CORE-F completion. The local candidate and unrelated protected/user changes remain separate; no unrelated files are part of the reviewed commit.
