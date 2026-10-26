# CORE-F P12-E Justice Records Core Boundary

Date: 2026-10-26

Candidate branch: `codex/core/CORE-F-shared-daily-v1-app-host`

Base: `a2ca8666ce01610d9bede418a761346ee309db4c`

Implementation commit: `f598b7f27a583cb349651e6963dd3bd00f64940a`

Implementation tree: `e78944534a7db1ad6eadb730e74ea450c71c1c31`

## Scope

This candidate moves the detached P12-E Justice snapshot value rows and their pure structural validation into `Simulation.Core`. The Unity adapter retains access to `SimulationRuntime`, the exact completed-boundary token and owner-section vector, live Justice/City/NPC/PersonStore identity checks, receipt-witness validation, and private staged `JusticeSystem` construction.

The prior Justice snapshot `.meta` GUID is preserved at its new Core package path. The Unity adapter has its own metadata identity. The Core SDK project explicitly compiles the moved source. Existing tests now invoke the adapter where runtime access is required; assertions and expected failure categories remain unchanged. The one reflection-based receipt predicate test now locates that predicate on the adapter, where it resides.

This source-boundary slice does not add or change a Justice writer, owner registration, profile admission, capture eligibility, receipt persistence, runtime composition/publication, P12-G, P12-A, P13, or CORE-F completion. It does not prove Library-free .NET hosting.

## Validation

| Gate | Result | Evidence identity |
| --- | --- | --- |
| Official CoreValidation cold/warm | 862/862 each; runner PASS | Run `a625a2cfdbc448d099a4d920f009ca46`; stable working-tree input digest `6b4ad142f14ade3c36305164fa0e6afd16b824780ea6eb9f5cb4f111d56fc98c`; protected manifest SHA-256 `c86af2f963fc1df96cb2648456f227a25ccbeb701a74e68bc9583f2dfadb04fc`; cold TRX `688e5c049e1f1f276ff9bdbb032c34be28b280f2e5c9ddbdb6bc645f8c88f637`; warm TRX `d200cd811e1016358f2fe243fb273480e6116b75017c3a93a8d1a29cdac67766`. The runner noted this checkout contains Unity Library, so this result is not a Library-independence claim. |
| Focused `P12EJusticeRecordsOwnerSnapshotTests` | 11/11 PASS | Run `977bb6d11cf04b268cbc99ecfd44d166`; stable inputs; protected manifest SHA-256 `b360f16aae7c7e2d58bcbc13cc9a92f273344f6b9502acc31c7486877cee2d9b`; XML `7d16c517baeafdcdfc1e7e58df94febebdc6023a57465c20661d8963d344b5d1`; log `2916ae59631a4ee8b0d607c80a6a829a29813a4954a51ec8e3d5878a2e4b61f4`. |
| Full Unity EditMode | 3,271/3,271 PASS; zero failed, skipped, or inconclusive | Run `65c1955fa7b0446d82cc3a20e46e473c`; stable inputs; protected manifest SHA-256 `8f7d9d83c45e2e9a18571cd7110cc52fc2cae1b6376cba70d2e331078c026fb`; XML `24656699a3fffbd9cfa41609d1b2f4feed9dee067219da93971301f73ffe51ea`; log `24d315628205ea130654c264a3d8aab22a8bee067d00238c8f43f630ffc9b0c7`. |
| Official Smoke subset | 5/5 PASS; zero failed, skipped, or inconclusive | Run `3607aa45a1c942ffa288f9dc04886c92`; stable inputs; protected manifest SHA-256 `7788ac570a3db8bd4b1a66a195e4e2ecd263824629fa12391558b42557eee7c5`; XML `ddd750e829d020226d990d557d356b65e0c90492b6f6b53851a5c8113caf9057`; log `5fe8762827eda854b110819835945f6e7592c21129a69d1161fee2bb2f518ce7`. |
| `git diff --check` | PASS | Checked after staging the final candidate source and metadata. |

CoreValidation ran before only Unity-adapter/test reflection-target and new Unity metadata whitespace corrections. The Core snapshot, Core compile list, and CoreValidation inputs did not change afterward. Final Unity focused, full, and Smoke runs were performed after those corrections. For each final Unity run, the runner recorded `inputStable=true`; hashes for the moved Core source, Unity adapter, and Justice snapshot test matched the current candidate files.

An initial Unity compile attempt identified a stale adapter reference to the moved schema constant; it was corrected to use `P12EJusticeRecordsOwnerSnapshot.CurrentSchemaVersion`. The first compiled focused run then found a test reflection lookup still targeting the old class; only the lookup target changed, preserving the same receipt-witness assertions. The final focused and full runs above passed.

## Privacy-safe result package

The complete full EditMode XML was packaged with the repository-approved Unity evidence packager and verified against its protected original run manifest. The result-only archive contains one normalized XML entry and no logs. Independent path scan found no workstation-local paths.

- Archive: [`coref-p12e-justice-core-boundary-final-20261026.zip`](coref-p12e-justice-core-boundary-final-20261026.zip)
- Package manifest: [`coref-p12e-justice-core-boundary-final-20261026.manifest.json`](coref-p12e-justice-core-boundary-final-20261026.manifest.json)
- Archive SHA-256: `896e2cf007c42c7241a9e60167cf33a534d80c9afd1d813a0574c0b2e00ffc99`
- Package-manifest SHA-256: `b8bd737d00209e1709a25bcedb014eed56e2bec317e135f12d560553245bc988`
- Packager verification: PASS; original-to-normalized comparison completed.

Original Unity manifests, XML, logs, CoreValidation TRX, logs, and its complete input manifest remain in the protected E: evidence store. They are not copied into this repository. The normalized full EditMode report contains the focused Justice and Smoke test cases; their filtered reports and logs remain protected originals.

## Status boundary

This is a validated candidate checkpoint only. It is not Core canonical promotion or full CORE-F acceptance. The shared authoritative runtime/application, detached restoration host, and separate-process continuation obligations remain open under the approved CORE-F integration contract.
