# CORE-F P12-E Justice Records Core Boundary — Independent Review

**Verdict:** `VALIDATED_CANDIDATE` / `PASS`
**Review date:** 2026-10-26
**Reviewer independence:** Two independent read-only reviewers examined the code and evidence. Neither edited the candidate or ran its tests.

## Exact identities

- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Original P12-E source-split base: `a2ca8666ce01610d9bede418a761346ee309db4c`.
- Source-split code commit: `f598b7f27a583cb349651e6963dd3bd00f64940a` (tree `e78944534a7db1ad6eadb730e74ea450c71c1c31`).
- Exact-zero correction code commit: `bc5b96120d7524249e202ae5c0e21c90dbdab210` (tree `3c454cbfbd8f6b01c5b972149d2ebc204a81c79b`).
- Reviewed validation/evidence tip: `c550001b80d6a379defcb49f94cd29214b5738ad` (tree `b4e5866fa93c35bf5d4eb79a4c51658a6217dc80`).
- Candidate branch: `codex/core/CORE-F-shared-daily-v1-app-host`.

This review covers the P12-E Justice snapshot boundary and the exact-zero correction on the existing CORE-F candidate. It is not a review or acceptance of the complete CORE-F application/host package.

## Implementation review

The source split moves detached Justice snapshot value rows and pure structural validation into `Simulation.Core`, preserving the original `.meta` GUID and explicit Core project inclusion. The Unity adapter retains runtime/token/vector checks, exact Justice/City/NPC/PersonStore binding, receipt-owner validation, and private staged `JusticeSystem` projection.

The follow-up change requires both the Justice P18 receipt witness revision and the live owner receipt revision to be zero, as the accepted P12-E design requires. Its regression supplies a matching nonzero owner/witness pair and confirms the direct predicate rejects it. Supported Daily-v1 admission already prevents this state upstream; the boundary assertion is contract enforcement and future-proofing. Existing source/test assertions and failure categories are preserved.

No actionable implementation findings remain. The change does not add a Justice writer, receipt persistence, profile admission, P12-G publication, runtime composition, P12-A readiness, P13 readiness, or CORE-F completion.

## Validation evidence

The complete run identities, private original hashes, corrected historical manifest hashes, and package links are in [the validation record](../validation/CORE_F_P12EJusticeRecordsCoreBoundary_20261026_Final/VALIDATION.md).

| Gate | Result | Evidence |
|---|---:|---|
| CoreValidation cold/warm | 862/862 each | Clean exact Core source split at `043a7f833f810f583fb9350d06b5dfe784e8dafc`; Core source and compile list were unchanged by the later Unity-only guard/test correction. |
| Focused Justice snapshot tests | 11/11 | Stable input digest `8da5e4c68aa3195d955dc9d9431ddc657128e9eaa7d535612cfe49d20d8335d8`; adapter/test file hashes match code commit `bc5b961`. |
| Full Unity EditMode | 3,271/3,271 | Same stable input digest; exact changed source/test bytes match `bc5b961`. |
| Official Smoke filter | 5/5 | EditMode filter `Smoke`; same stable input digest. |
| Full EditMode package verification | PASS | One result-only XML entry; original-to-normalized comparison passed; archive SHA-256 `d31e7cf15cd09d8001d110e01ffa9bf9b4ec12692f5dc72745297edfab6080e0`; package manifest SHA-256 `6d0fb079a2cda0fa075ac8439600a951ece26a29bd0ce7e7a5b30ca3dbe64ed6`. |
| `git diff --check` | PASS | Exact implementation tree `3c454cbfbd8f6b01c5b972149d2ebc204a81c79b`. |

The filtered focused and Smoke XML reports remain in protected evidence storage. The official packager rejected those filtered XML files because retained suite counts do not match their selected descendants. Their test cases also appear in the verified complete EditMode report; no filtered XML or raw log was published. The exact-zero code was committed after the Unity runs without changing the tested bytes; the independent review checked the recorded source hashes against the committed code.

## Disposition

`VALIDATED_CANDIDATE` applies to the bounded P12-E Justice snapshot/Core boundary and exact-zero correction at code commit `bc5b961` with evidence tip `c550001`. The current candidate remains unpromoted; Core canonical remains `a07402f`. Phase 12 and CORE-F closure are not claimed.
