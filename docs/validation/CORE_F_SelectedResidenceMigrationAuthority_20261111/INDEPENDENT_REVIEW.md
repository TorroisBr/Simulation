# Independent Review — Selected Residence Migration Authority

## Verdict

**VALIDATED_CANDIDATE** — independent exact-tip review passed with no actionable findings.

## Reviewed identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Reviewed commit: `82bd5207d70be44fd280ecc7f53182295b732e85`
- Reviewed tree: `f16dbba0c0ca3b518e8df3f50200c40e60c338f4`
- Parent: `551d977151352ef8fe0df4a95aeac071b48fe579`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

## Review coverage

The review checked the selected Core plan and commit path, exact owner graph membership, City and residence-owner identities, expected values/revisions, writer admission, P12 section IDs, epoch reservation, mixed-roster fallback, and additive regression coverage.

The operation binds the exact selected NPC and origin/destination City owners. It uses exactly one residence authority: the exact PersonStore-backed Person or the selected NPC-only residence owner. The paired City operation completes its validation and mutation admission before invoking the residence commit callback; after that callback succeeds, only the paired City value/revision writes and existing mutation notifications remain. No ordinary failure-capable validation follows the residence write.

The existing `runtime.population.residence-migration` contract and its two City aggregate rows plus one residence row are preserved. The P12 scope completes one reserved epoch. Mixed or unselected rosters retain the scoped compatibility route; failure to construct the admitted operation scope faults closed. Tests cover both residence-owner routes, revisions, epoch/census consistency, and rejection of a forged transition before writes. Assertions were not weakened.

## Validation evidence checked

The reviewer verified the recorded exact source hashes against the candidate, the public Unity package and manifest hashes, and the archive contents/privacy scan. The validation record reports CoreValidation 1,092/1,092 cold and warm, focused migration tests 3/3, full Unity EditMode 3,311/3,311, Smoke 5/5, and `git diff --check` PASS. No tests were rerun during this independent review because the reviewed source tree and evidence were unchanged.

## Scope

This is a validated intermediate CORE-F candidate only. It does not promote Core canonical or complete the shared runtime/application/restore/host contract.
