# CORE-F Runtime/Host Integration candidate validation

This is a recoverable candidate checkpoint on `codex/core/CORE-F-runtime-host-integration`. It is not a CORE-F completion claim or canonical promotion. Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

## .NET CoreValidation

- Run: `682a2a03461d454e85fcd216f156c70f`.
- Result: cold 640/640 and warm 640/640 passed; the runner's default demo and lab gates passed.
- The protected local run manifest is retained outside Git. SHA-256: `57f2cde830a9cb0781b9cfc1da5827f1c2f53d5289232f5ffbe1e4dd83125dc8`.
- Source commit/tree captured by the runner: `7053361e5efad5fe97fa1ea52d7a694e33e4ceab` / `23fadc0786f94cfe6d2620fb17f9a7cc2356bcc4`. Its working-input digest was `b5e488b1d5eec5733eef4677e26c949924f4d7db394652852f0e9b1c20925cb0`.
- The raw .NET reports, logs, and runner traces remain in protected local evidence; they are not copied into this candidate.

## Unity validation

- Unity: `6000.3.9f1`.
- Focused restored-session exchange: 1/1 passed (`aeca89697d0f472c97ca3e03e4432781`).
- Focused persisted-image restore and continuation: 1/1 passed (`a0965fad67d3424bbc1eabab95378c7b`).
- Full runtime admission suite: 220/220 passed (`5787aa05897e4bc7973c2b3a9d17ac40`).
- Full EditMode suite: 3,254/3,254 passed (`1aaa884d286b42c18e6b493f73cdbd00`). The exact-input manifest records base commit `7053361e5efad5fe97fa1ea52d7a694e33e4ceab`, tree `23fadc0786f94cfe6d2620fb17f9a7cc2356bcc4`, and 1,846 input files. Every inventoried file hash was rechecked against the current worktree and matched.
- The official `EditMode -TestFilter Smoke` run on the same input passed 5/5 (`8303b4dc897b424dbab4b24d3346a8f6`). Its filtered raw XML remains protected because the approved packager rejects its ancestor suite counts. All five Smoke cases also appear as passed cases in the packaged full EditMode report.
- Result package: `coref-runtime-host-editmode-final-20261022.zip`; archive SHA-256 `025d6a05469c5690a70bda70c4039344241127de510626d8fb74cdd8dc8a1779`. Manifest SHA-256 `7bc9b8ca0425d005ae59a71166d6c015866375f246bf4f849fe7f5c35575be37`. The package contains normalized result XML only; package verification confirmed 3,254 passed, zero failed, including five passed Smoke cases. The earlier package is retained locally but is not used as the final candidate evidence.
- An earlier full EditMode attempt was retained privately and failed 17 cases on stale test assumptions after source ownership moved. The final run above followed corrections to those test assumptions and passed. The candidate review should assess those test edits against the unchanged runtime and persistence contracts.

## Source identity and exclusions

The .NET validation inputs did not change after CoreValidation. The final Unity package's 1,846 inventoried inputs match the current worktree byte-for-byte, including the whitespace-normalized new folder metadata. The Unity run captured three pre-existing local ProjectSettings edits; they are unrelated user changes, remain untouched, and are deliberately excluded from the candidate commit. Protected local evidence and generated .NET `bin`/`obj` outputs are also excluded. The reviewer should account for the recorded ProjectSettings inputs when assessing validation applicability.

`git diff --check` passed after the final implementation/test edits. It reported only Git's normal LF-to-CRLF working-copy notices.
