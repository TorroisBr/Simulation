# Independent review: CORE-A retained TRX evidence

Date: 2026-10-14

## Exact target and authorities

- Candidate branch: `codex/core/CORE-A-calendar`
- Candidate commit: `f0bcb3e873fcf32df92c9dc3bc1a1ad20dae77d4`
- Candidate tree: `57d24a9d00c94e506d5f01fb75997f7844f46d66`
- Candidate parent: `74677a0b5b74c7814874dd2d2b11aead2891bc42`
- Current canonical tool tip: `codex/core/canonical` `c68ca7a9a51df178ba0b889096c762c3fd7f4034`, tree `7fba07ba75e8790f5b8a7791a3846003b1b9fe2a`
- Tool implementation independently reviewed at `b7535041a8aeb6e341fe35c6fcc465da48ce313e`, tree `67b595da4c1337f4da29116cb4d3e791ada54d91`
- CORE-A source identity: `f725fa87c6d1259d822180b84856b747c9e02980`, tree `bdb6b9b63532e780661477430099a835d2b4f086`
- Reviewer: independent Codex subagent. Candidate files were not edited; this record is on a separate review branch.

## Verdict

**NEEDS_CHANGES — evidence record discrepancy.** The committed ZIP and its reports validate, but the `VALIDATION.md` manifest SHA-256 does not match the committed manifest artifact or its Windows checkout representation.

## Verified evidence

- The manifest binds the promoted tool commit/tree and CORE-A source commit/tree listed above. The source inventory has 27 files and 3 generated dependencies, is marked stable, and reports 45 discovered cases.
- Attempts are ordered `cold`, `warm`. Each is `Completed`, has 45 results, 45 passed, zero failed, exit code 0, and a passing gate. The two normalized report byte lengths and SHA-256 values match their corresponding ZIP entries.
- The ZIP is 13,867 bytes with SHA-256 `540026a1fe878a927bd2ba70c51dc5e03b87526f43f474467f60eb68cb5a43ed`; that matches the manifest’s archive binding and `VALIDATION.md`. It contains exactly the two normalized cold/warm TRX entries.
- A read-only scan of the committed public manifest and normalized reports found no Windows/UNC or common Unix absolute paths, user-home paths, credential tokens, or private-key markers.
- The candidate diff from the stated source commit changes documentation/evidence only; code and test source remain unchanged. Tool implementation and tests are unchanged between the independently reviewed tool candidate and promoted tool tip.
- The candidate worktree contains three unrelated untracked Unity `.meta` files; the index was empty for them and they were not staged or modified.
- `VALIDATION.md` distinctly reports exact-context Verify as `originalComparisonVerified` and archive-only Verify as `originalComparisonNotPerformed`. I checked the distinction in the public record but did not access protected originals or rerun .NET/Unity, as requested.

## Finding: committed manifest hash mismatch

`docs/validation/COREA_TRX_EVIDENCE_V1/VALIDATION.md` labels `278b63a1d41d4d8de9748680fb4133335b0f85bb3c22b8c517aa190425bd1366` as the “Public manifest SHA-256.” It does not match the committed manifest file:

- Git blob `f1cdf698962063c274f9167065ca7ef692fee36a`, UTF-8/LF, 34,173 bytes: SHA-256 `d4583bbb8a5626bf020f4319046d66f822125b3b05e34b25d38a833537d039c4`.
- Windows checkout, UTF-8/CRLF, 35,054 bytes: SHA-256 `4ddc5555af7f9259ba59aa7e904603a47a4cf015ba93b9aac738c87f6056420f`. Converting the committed LF bytes to CRLF reproduces this checkout hash.
- Neither representation hashes to the value recorded in `VALIDATION.md`.

Minimal correction: update the record to the committed manifest blob hash and identify it as the committed UTF-8/LF representation, or provide and commit the exact artifact representation whose hash is `278b63…bd1366`. Reverify the corrected record against the committed bytes. No candidate file was changed during this review.

## Scope

This review covers the old-base evidence candidate only. It does not review the later current-canonical recomposition, promote CORE-A, or establish P12-A readiness or Phase closure.
