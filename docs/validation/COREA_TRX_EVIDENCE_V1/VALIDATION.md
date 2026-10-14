# CORE-A retained .NET result evidence

## Identity and source continuity

- CORE-A parser-corrected source commit: `f725fa87c6d1259d822180b84856b747c9e02980`.
- Source tree: `bdb6b9b63532e780661477430099a835d2b4f086`.
- Promoted TRX tool checkout: `codex/core/canonical` at
  `c68ca7a9a51df178ba0b889096c762c3fd7f4034` (tree
  `7fba07ba75e8790f5b8a7791a3846003b1b9fe2a`). Its packager source and test
  files are unchanged from the independently reviewed implementation
  `b7535041a8aeb6e341fe35c6fcc465da48ce313e` (tree
  `67b595da4c1337f4da29116cb4d3e791ada54d91`).
- Original source input digest: `174a3de69f00b1cb370f6b75749b14f5da72a3c8c911d675bc921270e5cbffee`.
- Captured source inventory: 27 files and 3 generated dependencies; the
  retained context verified the inventory and its stability.

The original cold and warm .NET test reports were retained unchanged. Their
source manifest and all retained report copies match their recorded hashes.
This package is a normalized, privacy-safe projection; it does not replace or
modify the protected originals.

## Results and package integrity

- Cold attempt: `Completed`, 45 results, 45 passed, gate passed.
- Warm attempt: `Completed`, 45 results, 45 passed, gate passed.
- Attempt order is cold then warm.
- Normalized cold report SHA-256:
  `5dba6f63c1c00775e2ab23d8d61de78e2b764e12a1578b28265eba4798c70ce5`.
- Normalized warm report SHA-256:
  `979e61b5d1b41aa052b63447d659fe2faa7ae6b296284f41ce501b4a2074134a`.
- Public manifest SHA-256 (committed UTF-8/LF Git blob bytes, 34,173 bytes):
  `d4583bbb8a5626bf020f4319046d66f822125b3b05e34b25d38a833537d039c4`.
- Windows checkout representation: CRLF, 35,054 bytes; SHA-256 `4ddc5555af7f9259ba59aa7e904603a47a4cf015ba93b9aac738c87f6056420f` (not the committed artifact identity).
- Archive: 13,867 bytes; SHA-256
  `540026a1fe878a927bd2ba70c51dc5e03b87526f43f474467f60eb68cb5a43ed`.

The promoted tool successfully completed all of the following without running
the tests again:

- Package with the retained private source context: PASS,
  `originalComparisonVerified`.
- Archive-only Verify: PASS,
  `originalComparisonNotPerformed` (the expected boundary without private
  context).
- Read: PASS; one run and the ordered cold/warm results were reported.
- Privacy inspection of the public manifest and archive: no absolute local
  paths, user-home paths, or credential-token/private-key markers found.

The earlier execution evidence remains authoritative for the 45/45 cold and
warm test results. Packaging did not rerun .NET, Unity EditMode, or Smoke.

## Scope and remaining review

Package generation, archive verification, and private original comparison completed using the promoted `c68ca7a9` tool. The old-base candidate exact-tip review is recorded as `NEEDS_CHANGES` at `451ba62b1419a3936abcc28bc60f14eaa17eecfc` because the validation record used an incorrect manifest SHA-256. The current-base integration corrects the label to the committed UTF-8/LF blob hash above while preserving the package bytes and source/test implementation identity.

The verifier import-order correction is now promoted to `codex/core/canonical` at `570b9f7c2718b79519b9a84b18e55748cc95ee35`; its implementation `2bda6341f3c5e599064e10ccda569237b9aea445` and exact-tip review record `7f0aeed1a036fe07dc6cd87f91b1c1652c099e8a` are recorded in `docs/CORE_STATE.md`. Under that corrected CLI, archive-only verification passed for this TRX package (one run, two entries, `originalComparisonNotPerformed`) and the retained Unity package. The source-context comparison was not repeated under the newer tool because this package explicitly records tool identity `c68ca7a9`; the verified `originalComparisonVerified` result under `c68ca7a9` remains authoritative and is not replaced by archive-only verification.

The current-base CORE-A candidate preserves the complete evidence results and report bytes. Its updated hash label and applicability are awaiting an exact-tip integration review. This evidence makes no P12-A, P13, cross-build compatibility, or broader .NET hosting claim.
