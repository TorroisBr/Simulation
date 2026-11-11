# Independent exact-tip review — CORE-F Selected Resident Floor

**Verdict:** `VALIDATED_CANDIDATE` — PASS, no actionable findings.

## Identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Exact reviewed candidate tip: `4fa97347fdfac4e8dcf5433c66b29251a44cd90a`
- Candidate tree: `37364a8f1d21442f74b4fbaae24e6724f0c39cec`
- Implementation commit: `65df8dee` (tree `a253432909eaeeab45136e5aa7b0779145c79417`)
- Implementation base: `914810df3d055dafa1f5e0687ced2f99580a4fd8`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Candidate contains the current Core canonical base.
- Commits after the implementation are evidence/documentation only; executable and test source are unchanged.

## Scope reviewed

Core now counts represented residents from the exact selected owner graph: living PersonStore residents plus living selected NPCs that are not materialized into PersonStore. Materialized NPCs count once through PersonStore. Unity provides only compatibility-roster remainder, and the mixed-roster test verifies that an unselected NPC remains represented. The Core order test asserts the unsorted input received by the boundary directly against the canonical owner order.

The review checked all six implementation/test file hashes against the corrected manifest in `VALIDATION.md`, the exact candidate diff, retained Unity package and manifest hashes, and the final cold/warm CoreValidation run identity. No implementation or test-integrity defect remains. No code or tests were changed during review.

## Validation evidence checked

- CoreValidation cold/warm: 1089/1089 each; final run `dd7c39d9969b4a409037a213a48f774e`.
- Focused Unity residence snapshot: 13/13; run `7d0385bc8dde453f931340bf6ddc85a4`.
- Full Unity EditMode: 3305/3305; run `bbb6ff43bbfb4cb8b43c215d34249214`.
- Official Smoke: 5/5; run `dbd08af0221648d6846394f17136da8b`.
- LongRun: 7/7; run `9a9ab57a8fc64968ba0c48d93ca90b1d`.
- Approved full EditMode evidence package verifies; archive SHA-256 `c3086c1351d98c5a3072d3d2e403df15268f89fa1a8b81c812cd78ca696a7616`.
- `git diff --check`: PASS.

The raw CoreValidation TRX/logs and owner-only manifest remain private; no public TRX package or independent raw-report comparison is claimed here. The Unity package and its source/test identity are recorded in the linked validation note.

## Limits

This validates only the selected resident-floor contribution and its mixed-roster remainder. It does not claim complete P12 census ownership, a Unity-independent DailyV1Application, detached .NET runtime construction, or CORE-F completion. Core canonical remains unchanged.