# Exact-Tip Review — CORE-F Selected Daily Preflight

**Verdict:** VALIDATED_CANDIDATE — PASS for this commit. No actionable findings.

## Identity and scope

- Branch: `codex/core/CORE-F-restore-stage-transaction`
- Candidate: `a1b16a1deac1e7695592cca5f0159d1843b8d021`
- Tree: `9657f269086757492e86e1bbea5a30d23a448819`
- Parent/base: `ef6f5011da1301f4c6e10bbd9dce841b4cde04fd`
- Core canonical observed: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Scope: the four changed production/test files and the committed validation record, Unity package, and package manifest.

## Review result

Core binds preflight to the admitted execution state and exact owner graph. It checks the admitted calendar when local material flow requires it, validates Core owner membership, and derives Merchant enabled state from admitted effective configuration. Unity callbacks check City/NPC projections and host capability only; Merchant capability checks remain conditional on the Core-supplied enabled flag. Null configuration is rejected before stage execution, and immutable definition/owner graph construction rejects null required inputs and owners.

The preflight remains before logger day start and daily writers. Mixed-roster compatibility behavior and the disabled-Merchant path remain covered by existing tests. No defect found.

## Validation evidence

The record reports CoreValidation **1,103/1,103 cold and warm** (run `ac6ba77a22da4cb296a84f7168c5969c`), focused Unity admission tests **246/246** (`979553d0894a46548bd0cf13897d64e5`), full EditMode **3,318/3,318** (`da2a6dc7edff4dd6aab9307fd14c3f18`), filtered Smoke **5/5** (`b6d012ca59ca400a875108bfbe8f51e8`), and `git diff --check` PASS. No validation was rerun for this review.

The full EditMode archive hash matches its manifest and record: **175,875 bytes**, SHA-256 `d59d2e53ce3168ff580c29ba9544b67c454e100fb207ebad497ffbac1c5bb1b2`. The manifest SHA-256 is `6403934ecdedd0a64e6f4d974c035180f46932942365b6e4c225692071f7b184`. Its single normalized XML entry reports Passed, 3,318 total, zero failures/skips/inconclusive. The package contains no absolute Windows paths. The two changed production-source hashes match the Unity input manifest.

## Evidence limits

The Core TRX/logs remain private; the recorded Core run and input digest were relied on without opening private evidence. No public Core TRX package is claimed because the runner manifest lacks the source-context schema required by the approved packager. The filtered Smoke XML also remains private: the packager rejects its suite count mismatch, so no standalone Smoke archive is claimed. The record says all five Smoke identities occur in the full report.

The Unity run manifest identifies the parent as HEAD and records the tested working-tree inputs; both changed production-file hashes match those inputs. No ProjectSettings or private evidence was accessed. This verdict covers only this commit and does not promote code or claim CORE-F completion.
