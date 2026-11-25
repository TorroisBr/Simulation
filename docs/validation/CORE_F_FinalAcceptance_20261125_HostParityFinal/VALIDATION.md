# CORE-F Final Acceptance Evidence Reconciliation

Date: 2026-11-25

This record closes the two final evidence gates for the accepted CORE-F scope: a packaged, independently verified cold/warm .NET result set and a durable candidate source identity. It does not claim Core canonical promotion or expand the Daily-v1 profile.

## Immutable implementation input

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Validated implementation commit: `a5d5504ab4c5fb1e8b26b61a6d3c6ca1ef533e4f`
- Validated implementation tree: `44ae90057f501fd154d7cf3d86cf20361e2a3191`
- Core canonical remains: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- The candidate input was clean and immutable during the successful .NET run. The public validation record and result packages are evidence-only additions; original reports and private source context are retained separately.

## .NET CoreValidation

The official pinned CoreValidation runner completed from the sparse candidate worktree. Its manifest binds commit/tree above, 564 stable inputs, input digest `42b630bc8d218b0912671915b3bfd3b174d7cfac3ca2b52fa1867360212468da`, zero worktree changes, and `worktreeLibraryAbsent=true`.

| Run | TestRun ID | Result | Original TRX SHA-256 | Normalized package entry SHA-256 |
| --- | --- | --- | --- | --- |
| Cold | `fad8f86c-8b4e-43fa-8bc7-ddfc6202a01d` | 1,212/1,212 passed | `0a66cbc3198bb9f2f2e1af2c9e2a90f14cef57b78fbdbc29782a890947107e64` | `f609a7e5867018078febbc7715aa646a854735bb144e43a52c2eb9f00189f21b` |
| Warm | `3689ee7e-9b04-478a-b016-740d19741ad9` | 1,212/1,212 passed | `8af33e0b50a74f71ea22e23b4d88e1583450f806bc42b18af6f93551a91b0054` | `732de21a4801ec9b9cc9c253ca6e927de72082dcb5a91d2f5fc309515645f938` |

- Runner ID: `83f462f42de1457e9369af3905b21859`; runner status: `Passed`.
- Captured runner manifest SHA-256: `9d2c97147654ac766bf17c1b6a9385e30db114c4f9242099807116c5e0b96650`.
- Verified private source-context record SHA-256: `3cbf84b856db11118b7e6c57e1f1491834e4db8c1d193b057f42d7711b5d5c7e`. It remains in owner-only protected storage; its local paths and private metadata are not published.
- Official VSTest/TRX package: [manifest](DotNet/coref-dotnet-a5d5504a.manifest.json), SHA-256 `a5b960dc6fc5fbb477bd152b5910ed99d07eea581075483708491896c8d0c5e8`; [normalized result archive](DotNet/coref-dotnet-a5d5504a.zip), 579,089 bytes, SHA-256 `9fbbd73acf197aa3aea4987d60765076b890702b41cff3228f91b60d5dbbc0e0`.
- The archive contains only the two normalized cold/warm TRX result entries. The official standalone verifier returned `IsValid=true`, `GatePassed=true`, `OriginalComparison=originalComparisonVerified`, and deterministic archive integrity PASS. The originals were not modified.
- Producer: .NET SDK `9.0.315`, VSTest `17.14.1`, selected CLR `8.0.28`.
- The cold and warm runs include `ProcessSeparatedDailyV1ContinuationMatchesThreeDayGenesisControl` as Passed. The separately recorded continuation check restored in a fresh process and continued T+1/T+2; mutation-epoch parity was `87 + 92 + 80 = 259`.

## Unity and Smoke evidence

- Unity Editor: `6000.3.9f1`.
- Final ALL EditMode run: `98e12e92c1004cc7b450f88c69ea98d9`, **3,327/3,327 passed**. The final packaged report is [manifest](Unity/coref-final-editmode-2463d171-final.manifest.json), SHA-256 `dc17c05b69e1ab69374f06e8a93431a9966cb07d115eeb68ba466bc9cdbe9c2f`; [archive](Unity/coref-final-editmode-2463d171-final.zip), SHA-256 `63376e575dfddbf7ba71effa9b9ed9c797396273b2235d819b0e612763bccc53`.
- Unity input snapshot: 1,994 files, SHA-256 `586f23ae8ab68ad8622e46f7c7a3d4db9169a95e92f3cbfb1ebb0bd11916ae67`, captured at parent commit `2463d17149b8b39ee8caeb2a3322dc08a66e3e2d` / tree `f1144aa0fe0e593ecd46904b0d62d8f28e7b607d`. The candidate's changed Unity test source and helper `.cs`/`.meta` identities match that snapshot exactly. The remaining candidate changes are the .NET host/tests and its narrowly scoped generated-output ignore rule; they are covered by the exact-commit CoreValidation evidence above.
- LongRun: 7/7 passed within the ALL EditMode evidence.
- Official Smoke: 5/5 passed. The original filtered Smoke report and raw log remain unchanged in protected storage. Its aggregate suite-count anomaly was reconciled under the already accepted CORE-B/CORE-C evidence treatment: the same five identified Smoke cases have individual Passed results in the final ALL EditMode report above. No original XML was normalized or synthesized for this reconciliation; this Smoke item is closed.

## Acceptance and disposition

The previously completed functional acceptance matrix remains PASS within the accepted CORE-F scope, including selected owner/ingress coverage, exact identity and persistence compatibility, detached restore, deterministic continuation, negative admission and failure atomicity. Existing evidence and regression results were reused only where their recorded inputs match the immutable candidate; this reconciliation did not rerun Unity or modify protected reports.

Independent source review passed for implementation commit `a5d5504ab4c5fb1e8b26b61a6d3c6ca1ef533e4f`; the final exact-tip evidence/applicability review is performed against the candidate commit containing this record and the public evidence packages. Core canonical is unchanged. This record does not itself authorize or perform Core canonical promotion.