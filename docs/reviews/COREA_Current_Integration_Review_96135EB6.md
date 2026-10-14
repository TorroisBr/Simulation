# Independent review: CORE-A current-base integration

Date: 2026-10-14

## Exact target and authorities

- Candidate branch: `codex/core/COREA-calendar-current-integration`
- Candidate commit: `96135eb6a87e5217d169a56414a20d21e6649af6`
- Candidate tree: `51d0f26e90804fedf6e9dae7379342ac4e5c2405`
- Merge parents: prior CORE-A candidate `7441f1e4251613c98f514b7bca257e0bece2a1be`; current Core canonical `570b9f7c2718b79519b9a84b18e55748cc95ee35`
- Current canonical remote ref verified at review: `origin/codex/core/canonical` = `570b9f7c2718b79519b9a84b18e55748cc95ee35`
- Production source baseline: `codex/phase12/canonical` at `4cc31073f0ec6ba5c718084e4cebd6e914931d5d`
- Prior parser-corrected CORE-A implementation: `f725fa87c6d1259d822180b84856b747c9e02980` (tree `bdb6b9b63532e780661477430099a835d2b4f086`)
- TRX tool provenance in the retained package: `c68ca7a9a51df178ba0b889096c762c3fd7f4034` (tree `7fba07ba75e8790f5b8a7791a3846003b1b9fe2a`)
- Promoted CLI import correction: `2bda6341f3c5e599064e10ccda569237b9aea445`, independently reviewed at `7f0aeed1a036fe07dc6cd87f91b1c1652c099e8a`
- Reviewer: independent Codex subagent; the candidate branch and its files were not edited.

The full diff from current Core canonical and the merge delta from the prior candidate were inspected. The candidate combines the previously reviewed CORE-A extraction and current canonical. The merge delta from `7441f1e` is limited to the shared CLI and its regression test plus `docs/CORE_STATE.md` and the two CORE-A validation records.

## Verdict

**VALIDATED_CANDIDATE — PASS for this exact integration tip.** No actionable finding remains in the assigned scope. This review does not promote CORE-A or close a Phase.

## Source and input continuity

The candidate's production and validation input tree is identical to reviewed commit `f725fa87c6d1259d822180b84856b747c9e02980`, tree `bdb6b9b63532e780661477430099a835d2b4f086`. The exact diff from `f725fa8` to this candidate has no changes under `Assets`, `Packages`, `src`, `tests`, `tools`, `Tools/CoreValidation`, or `global.json`. The retained manifest continues to bind the original source commit/tree and input digest, 27 source inputs, and 3 generated dependencies. Unity and .NET implementation/test inputs therefore remain applicable without rerunning those suites for this integration refresh.

The candidate's `Tools/UnityValidation` tree is unchanged from the separately reviewed `2bda6341` CLI correction. The import-order fix and subprocess regression remain present. No adapter or simulation source change was introduced by this integration.

## Evidence and hash correction

The committed public manifest blob is 34,173 bytes with SHA-256 `d4583bbb8a5626bf020f4319046d66f822125b3b05e34b25d38a833537d039c4`; the updated validation record names this committed UTF-8/LF blob hash. Its Git blob is unchanged from the prior candidate. The retained archive blob is also unchanged; it is 13,867 bytes with SHA-256 `540026a1fe878a927bd2ba70c51dc5e03b87526f43f474467f60eb68cb5a43ed`.

The archive contains the same cold and warm normalized reports: cold SHA-256 `5dba6f63c1c00775e2ab23d8d61de78e2b764e12a1578b28265eba4798c70ce5`; warm SHA-256 `979e61b5d1b41aa052b63447d659fe2faa7ae6b296284f41ce501b4a2074134a`. The manifest preserves cold-then-warm order, `Completed` outcomes, and 45/45 passed results for each run. The report and archive bytes were verified against the committed artifacts.

Both retained archive-only CLI verification commands passed on this exact checkout:

- TRX package: one run, two entries, archive SHA above, `originalComparisonNotPerformed`.
- Unity/NUnit package: one run, one entry, archive SHA-256 `ad99dea856aac26d94fec35c970ab8e9142dc77f9f7e95a3bcc0a89ffdd0c614`.

The private source-context comparison remains the recorded `originalComparisonVerified` result from tool `c68ca7a9`. It is not attributed to `2bda6341` or this integration; no source-context comparison under the newer CLI is claimed. The exact-tip validation text preserves this distinction, the privacy boundary, the original result scope, and the pending integration/promotion status. This review did not access protected original reports or rerun .NET/Unity suites.

## Validation and checkout notes

- `git diff --check 570b9f7c2718b79519b9a84b18e55748cc95ee35 96135eb6a87e5217d169a56414a20d21e6649af6`: PASS.
- Exact-tip archive-only TRX and Unity CLI Verify: PASS, as recorded above.
- Unity EditMode, Smoke, and .NET test suites were not rerun because their source/test/input tree is unchanged from the reviewed and validated `f725fa8` implementation.
- No tracked worktree modifications or staged candidate changes were present. Three unrelated untracked `.meta` files were left untouched.

## Scope and integration boundary

This is an exact-tip integration applicability review. It establishes that the manifest-hash correction is representation-correct and that the already-reviewed CLI fix is integrated without changing CORE-A production/test inputs or evidence bytes. It does not replace the prior private source comparison, claim a new execution, broaden CORE-A, promote code, or establish P12-A/P13, cross-build compatibility, or broader .NET hosting readiness. CORE-A remains unpromoted pending the normal canonical integration workflow.