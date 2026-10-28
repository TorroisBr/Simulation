# CORE-F P12-E Justice Records Core Boundary

Date: 2026-10-26

Candidate branch: `codex/core/CORE-F-shared-daily-v1-app-host`

Base: `a2ca8666ce01610d9bede418a761346ee309db4c`

Implementation commit: `bc5b96120d7524249e202ae5c0e21c90dbdab210`

Implementation tree: `3c454cbfbd8f6b01c5b972149d2ebc204a81c79b`

## Scope

The source split commit `f598b7f27a583cb349651e6963dd3bd00f64940a` moves the detached P12-E Justice snapshot value rows and their pure structural validation into `Simulation.Core`. The final candidate adds a narrow exact-zero assertion at the Unity capture boundary: both the witness revision and current Justice receipt revision must be zero. The supported admitted profile already enforces zero upstream; this check makes the snapshot boundary directly enforce the accepted contract. A predicate regression synthesizes a matching nonzero owner/witness pair to ensure it is rejected.

The prior Justice snapshot `.meta` GUID is preserved at its new Core package path. The Unity adapter has its own metadata identity. The Core SDK project explicitly compiles the moved source. Existing tests now invoke the adapter where runtime access is required; assertions and expected failure categories remain unchanged. The one reflection-based receipt predicate test now locates that predicate on the adapter, where it resides.

This source-boundary slice does not add or change a Justice writer, owner registration, profile admission, capture eligibility, receipt persistence, runtime composition/publication, P12-G, P12-A, P13, or CORE-F completion. It does not prove Library-free .NET hosting.

## Validation

| Gate | Result | Evidence identity |
| --- | --- | --- |
| Official CoreValidation cold/warm | 862/862 each; runner PASS | Run `a625a2cfdbc448d099a4d920f009ca46`; stable working-tree input digest `6b4ad142f14ade3c36305164fa0e6afd16b824780ea6eb9f5cb4f111d56fc98c`; protected manifest SHA-256 `c86af2f963fc1df96cb2648456f227a25cceb701a74e68bc9583f2fdafdb04fc`; cold TRX `688e5c049e1f1f276ff9bdbb032c34be28b280f2e5c9ddbdb6bc645f8c88f637`; warm TRX `d200cd811e1016358f2fe243fb273480e6116b75017c3a93a8d1a29cdac67766`. The runner noted this checkout contains Unity Library, so this result is not a Library-independence claim. |
| Official CoreValidation on clean exact candidate, cold/warm | 862/862 each; runner PASS | Run `3ec96ad8b8024960b7e223e02f81d456`; commit `043a7f833f810f583fb9350d06b5dfe784e8dafc`; tree `bd06f1ed520f568e1a13b88697b4607e71c42cd8`; stable input digest `9da61264c08bb3db18d408f65d81352417a9847fbbb25108ffd15facb9b893cc`; protected manifest SHA-256 `da7fd9b57d1366e3efaa55dfb7d54836e4dbe62b4f5e7e90fa6bd9722968dadc`; cold TRX `cfda582a0d89f6a4d7c85fe18fb781ef0b0428fd17c962172a2de6b4984b7c4c`; warm TRX `057125c93ed7e314c89d739e40276beddab52de5d48a73733d59d27953076143`. The official runner reported no Unity Library in this detached exact-tip worktree. Core snapshot source SHA-256 `5f0194445300ed2d76d67a60fead7fb29ebbfcf00671469ff226f22751387317`; Core project SHA-256 `f5ac67887b36708068abb56ac09dd73a82d703c2622a5793e8b263341d8f67b3`. |
| Focused `P12EJusticeRecordsOwnerSnapshotTests` after exact-zero guard | 11/11 PASS; zero failed, skipped, or inconclusive | Run `a542de1793214d03963639ee005196f8`; stable input digest `8da5e4c68aa3195d955dc9d9431ddc657128e9eaa7d535612cfe49d20d8335d8`; protected manifest SHA-256 `a6a916606ecb41146ced8456eaf2286b577842dc355afe487077955589ae6b4e`; XML `a9b511fde217d86bdc5774557825ab196efebcaaf86d81a548406e50e746b4f7`; log `90af5b1dd21c01365857a7b0ba8de6e860265333a938830156ef1b17d12a5e6c`. |
| Full Unity EditMode after exact-zero guard | 3,271/3,271 PASS; zero failed, skipped, or inconclusive | Run `f790fe120f46402dad47ab81fe0c227b`; stable input digest `8da5e4c68aa3195d955dc9d9431ddc657128e9eaa7d535612cfe49d20d8335d8`; protected manifest SHA-256 `2fa381ddb1b4b8c8dba377f3a1e7fe09dd235d1d2bf0b65e89f4e2c6d2a91947`; XML `4055bc470520f8ea40ec06ed42236d9a89b11cac130e59a3a38096a8e3156451`; log `ee2658f81240b78d210d8585913ef734fbda8b7ac0cefee29de2ccde15c56ad6`. The report was packaged and verified below. |
| Official Smoke filter (`EditMode`, `Smoke`) after exact-zero guard | 5/5 PASS; zero failed, skipped, or inconclusive | Run `81ce1d3745c847db8fc487d37799df77`; stable input digest `8da5e4c68aa3195d955dc9d9431ddc657128e9eaa7d535612cfe49d20d8335d8`; protected manifest SHA-256 `98abbad637f93e1a98a240a5f0bb2a0ba5a9437cab54388d0c9d2a4a19fac666`; XML `4c99dfb0238e97abed6b5b65e36a23feace6ff2f148e6ea4da764553b14502be`; log `921fc201d0bdbd14077838806bb59802d526bc4b3e9d545a8d7ca9d577cd21af`. |
| `git diff --check` | PASS | Checked on the exact committed implementation tree `3c454cbfbd8f6b01c5b972149d2ebc204a81c79b`. |

The clean detached CoreValidation run above tested the exact Core source split at `043a7f833f810f583fb9350d06b5dfe784e8dafc`. The later exact-zero change touched only the Unity adapter and its Unity test; no Core source or compile-list input changed, so the CoreValidation result remains applicable. For the three Unity runs after the exact-zero change, `inputStable=true` and the adapter/test file hashes match the committed implementation `bc5b96120d7524249e202ae5c0e21c90dbdab210`; committing the already-tested bytes did not change them. The candidate branch tree at validation launch was `9c18b4b1e97404ae7806238e847d04269861a9c4`, with the same recorded validation-input digest before and after each run.

### Evidence record correction

This addendum corrects two manifest SHA-256 values transcribed incorrectly in the earlier record: the initial CoreValidation manifest is `c86af2f963fc1df96cb2648456f227a25cceb701a74e68bc9583f2fdafdb04fc`; the pre-hardening Full EditMode manifest was `8f7d9d83c45e2e9a18571cd7110cc52fcf2cae1b6376cba70d2e331078c026fb`. Both were recomputed from preserved originals. No original TRX, manifest, log, source, test result, or package was changed. The clean exact-candidate CoreValidation run remains authoritative for the unchanged Core source split.

The official packager rejected the filtered focused and Smoke XML files because their filtered `Simulation` suite counts do not match the full suite descendants. Those original reports remain protected and were not published. The verified full EditMode package below contains the same selected focused and Smoke test cases together with the complete suite.

An initial Unity compile attempt identified a stale adapter reference to the moved schema constant; it was corrected to use `P12EJusticeRecordsOwnerSnapshot.CurrentSchemaVersion`. The first compiled focused run then found a test reflection lookup still targeting the old class; only the lookup target changed, preserving the same receipt-witness assertions. The final focused and full runs above passed.

## Privacy-safe result package

The complete full EditMode XML was packaged with the repository-approved Unity evidence packager and verified against its protected original run manifest. The result-only archive contains one normalized XML entry and no logs. Independent path scan found no workstation-local paths.

- Archive: [`p12e-justice-editmode-exact-zero-20261026.zip`](p12e-justice-editmode-exact-zero-20261026.zip)
- Package manifest: [`p12e-justice-editmode-exact-zero-20261026.manifest.json`](p12e-justice-editmode-exact-zero-20261026.manifest.json)
- Archive SHA-256: `d31e7cf15cd09d8001d110e01ffa9bf9b4ec12692f5dc72745297edfab6080e0`
- Package-manifest SHA-256: `6d0fb079a2cda0fa075ac8439600a951ece26a29bd0ce7e7a5b30ca3dbe64ed6`
- Packager verification: PASS; one normalized full EditMode XML entry, original-to-normalized comparison completed, privacy scan passed.

Original Unity manifests, XML, logs, CoreValidation TRX, logs, and its complete input manifest remain in the protected E: evidence store. They are not copied into this repository. The normalized full EditMode report contains the focused Justice and Smoke test cases; their filtered reports and logs remain protected originals.

## Status boundary

This is a validated candidate checkpoint only. It is not Core canonical promotion or full CORE-F acceptance. The shared authoritative runtime/application, detached restoration host, and separate-process continuation obligations remain open under the approved CORE-F integration contract.
