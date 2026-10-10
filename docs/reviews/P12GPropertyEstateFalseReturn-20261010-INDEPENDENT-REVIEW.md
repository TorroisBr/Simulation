# P12-G Property/Estate partial-stage false return — exact-tip independent review

**Verdict:** PASS — bounded implementation candidate
**Review date:** 2026-10-10
**Candidate branch:** `codex/phase12/P12GPropertyEstateFalseReturn`
**Candidate tip:** `4205799847b6a5fe6c1c78e0bcb8a33f40513015`
**Base and current P12 canonical at review:** `f2a6ff357d739b58ae1757f4870b4b75aef1a7e9`
**Implementation commit:** `4f9de0ca84b4d2518e0c19405b48609e014b839b`
**Implementation Git tree:** `7348f6ef113d98bb4648875b4ae186a776f556dc`
**Validated Assets tree:** `47d26eca4ed1d7e3e4f319153aa5297f225dbbc0`

## Review findings

The candidate was reviewed as a clean two-commit fast-forward from the stated canonical base. Its first commit contains the implementation; the descendant commit adds only validation artifacts and their manifest. The candidate tip, implementation commit, and remote ref were checked, and no implementation code changed after validation or review.

The restore-only snapshot override is null by default and is threaded solely through the explicit Property/Estate restore-for-test path. It runs after the E owner snapshots have been captured and after Institution/Office owners have staged, immediately before Property/Estate hydration. The test preserves the snapshot schema, row count, revision, and all other owner rows, changing only the Property owner PersonId to an ID absent from the staged P12-D Person root. Normal Property/Estate hydration rejects through its typed unresolved-reference path before the Property/Estate stage is reached.

The integrated assertions establish that the earlier E siblings were privately staged, the Property/Estate pair was not staged, and the restore returned `OwnerStageFailed` before publication. The old active session, completed-boundary token, health, selected facts, continuation roots, and full included C–F owner projection remain unchanged. The original runtime continues in parity with an uninterrupted control; a valid retry restores the populated Property row; one further boundary remains in parity. This is one bounded P12-E→P12-G false-return case, not complete P12-G failure or graph coverage.

The runtime census change allows populated Property/Estate owners only when the runtime admission context is a restored continuation. Fresh bootstrap retains its exact-empty requirement. This is necessary for the accepted populated-owner restore/retry path and does not admit P10-A into the selected Daily-v1 bootstrap profile.

The focused XML reports 178/178, ALL EditMode 2852/2852, and official Smoke 5/5, each with zero failures, skips, or inconclusive tests. The focused and Smoke XML outcomes were inspected in review. The review connector could not independently retrieve the large ALL EditMode XML or raw compressed-log bytes. A local checkout verification separately recomputed all six committed artifact hashes and all three decompressed-log hashes against the committed manifest; the XML counts and zero-nonpass totals also match. `git diff --check` passes for the candidate.

This candidate closes only the Property/Estate partial-stage false-return case and its valid retry/continuation proof. The live owner/provider/cardinality/revision/writer/epoch join, remaining target-owner and graph rejection cases, other restore-boundary failure paths, no-replay, and full included-owner multi-boundary parity remain open. P12-G remains `WAIT_DEPENDENCY`; P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains `OPEN`. No complete owner/epoch coverage, global quiescence, capture eligibility, export/hydration readiness, downstream readiness, or Phase closure is implied. Canonical promotion is not performed by this review record.
