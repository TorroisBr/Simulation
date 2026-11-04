# Validation — exact admission for initial restore staging

Implementation commit: `4fff61b27a3cafb6badb7423e7dc434d5559408e`

Implementation tree: `7db93bccc24a92baa646b24583d9fd28f21aef1a`

Base: `51c658f3e047b735226bcfbfdd48bff724bf574e`

Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

The independent review of `51c658f3` found that application-owned initial image staging accepted a missing `DailyV1ApplicationStartAdmission`. This fix makes the exact `RestoreRequired` token mandatory before initial image decoding or stage callbacks. A supplied token is rejected on replacement staging; the expected-session/replacement-operation path and the application-free compatibility path remain unchanged. Core coverage proves a missing token returns `InvalidOwnerValue` in `p12.initial-start-admission` before decoding, staging, or publication. Unity integration fixtures now obtain the token by loading the exact bytes through `TryAdmitInitialStart`; the split-alias case separately retains its detached owner-census rejection proof.

## Validation

All official runs completed with stable input snapshots. The source table below records the exact UTF-8 CRLF bytes captured for the runs. The files were committed afterward and their Git blobs use UTF-8 LF bytes. The run inputs were verified to equal the committed text after line-ending normalization; both byte hashes are recorded below. CoreValidation ran in the Unity worktree with its `Library` present (`worktreeLibraryAbsent=false`); it is not claimed as a Library-free .NET run.

| Gate | Result | Run ID | Private manifest SHA-256 |
| --- | --- | --- | --- |
| CoreValidation cold and warm | 1007/1007 each; all build, demo, and lab gates passed | `461635f6e1f84c54b8596e88c7cd5b54` | `822fbf93cb161a275176679ccf4cb63d85a127c681fb106e74f6dbaca5ceeef8` |
| Focused app-owned restore fixture | 16/16 | `237987e60fc74c9181817778a4cdacfd` | `8abe6f061aa08fc5d2ac211ce2559d5ea61b0ac8121a5f1cf44f289c501ca114` |
| P12-G EditMode | 33/33 | `76c859f215694ad49200ca497c5f0148` | `db897ced25cbfb0958613adfe8d1a5e72ee1c914fd53bef92ca785167c7ebdd6` |
| All EditMode | 3299/3299 | `2ba45723b54848a6b832b54f34de9cfd` | `9882a3c1a735d32099d1f8bc7f3033fcde58e7e1a14f5b453b20e67ed97fd8df` |
| Official Smoke filter | 5/5 | `11c73a2739164efabb835859bca42cd9` | `dae538f9ca1392a2e9acce12253d38ba12d1fd606d78757806233c36b39bba43` |

The CoreValidation manifest input digest was `6be84a823ec51701da5947dde7563f3e5d63958c71302fd73de103e52c2293af`. The full Unity and Smoke runs recorded stable inputs; their Git baseline was `51c658f3` with tree `3a9aea55de713ed4984614137c6c418a96ec3f1a`. Run-input hashes were compared with committed text after LF line-ending normalization:

| Source | Run-input SHA-256 (UTF-8 CRLF) | Committed Git-blob SHA-256 (UTF-8 LF) |
| --- | --- | --- |
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | `693776ad45c49c7da21151d64fb7dc2e1c22ee5e64e4583ce7607b751444c797` | `e21284f5e201295aa8a8e79f3f2159953ed2fe50e624dc0bac6190595f060f95` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationHostTests.cs` | `a251597190519d43e4998a79f886127cdf4dbee52c84548c9eec77722861db00` | `7a1ac4243621a7a3941278a9c3c82cdb5b44e3f2cd0e294622c734ef73db034a` |
| `Assets/_Project/Tests/EditMode/Editor/P12GOwnerEntryQualificationTests.cs` | `c59982310181d738a68b1ab85e3a1fc3e3eb311fde21d6876625fa13d104aca7` | `cc7dedaa75302af3b3b05137b68829d5b8131b563381293105baf5ad93df937e` |

`git diff --check HEAD^ HEAD` passed. Evidence manifests and original XML/log artifacts remain in the protected local validation storage and are not copied into this repository. A separate PlayMode filter experiment returned a zero-test report and is excluded; the five required Smoke cases passed under the documented EditMode `Smoke` filter and appear in the full EditMode report.

This is a candidate checkpoint only. It does not complete `DailyV1Application.TryStart`, detached .NET hosting, or CORE-F acceptance, and it does not promote Core canonical.

## Public normalized Unity evidence package

The complete EditMode result was packaged with the repository's official validation evidence packager. The package contains the normalized complete EditMode report; it records 3299 passed tests and zero failed, skipped, or inconclusive results. Smoke's required five cases are included in that complete run.

- Bundle: `coref-initial-restore-editmode-20261103`
- Manifest: `UnityEvidence/coref-initial-restore-editmode-20261103.manifest.json`
- Published manifest SHA-256 (committed UTF-8 LF bytes): `6087994f501b15fd24b45847374b1a3521a2a2d5cefce9b02279dd6ef7caae6f`
- Archive: `UnityEvidence/coref-initial-restore-editmode-20261103.zip` (174984 bytes)
- Archive SHA-256: `6b0b9c5ccaf3a58455d99b1fce3853e1c0c8114898288313eb1134415d1dcef3`
- The official package verifier passed against the retained private run manifest. The archive and public manifest contain no local account or protected-store path matches.

Original Unity XML, logs, and private manifests remain unchanged in protected local evidence storage and are not part of this repository package. The .NET TRX files and CoreValidation manifest also remain protected and unchanged; this validation record does not claim a public normalized .NET TRX package or an independent inspection of those private originals.

## Independent review boundary

The independent exact-tip review passed the implementation and tests at code commit `4fff61b27a3cafb6badb7423e7dc434d5559408e` / tree `7db93bccc24a92baa646b24583d9fd28f21aef1a`. The reviewer did not inspect the protected private manifests or this validation record. The evidence statements above are therefore the Master Orchestrator's validation/package verification, not part of the reviewer's PASS. CORE-F remains an incomplete candidate and Core canonical is unchanged.

## Atomic initial-restore publication checkpoint — 2026-11-04

| Identity | SHA |
| --- | --- |
| Implementation commit | `c4d1eec69952e117146c3710080e498f2d885baa` |
| Implementation tree | `4a1c1963baae848dd9dcab2c18296921fb71a6a8` |
| Base | `48ae83045e4ecff298d516ecfb8763793b9e050b` |
| Core canonical | `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea` |

This change binds the exact app-owned initial-restore admission token and admitted image check to the same Core session gate that atomically publishes the restored Core session and Unity host projection. Initial publication rejects a missing token or a separately decoded byte-equivalent image before either side is published. Replacement restore continues to use its existing expected-session/replacement operation, and application-free compatibility paths are unchanged. Coverage proves rejection preserves the unpublished session, Unity projection, and candidate runtime boundary, followed by publication with the exact admitted token/image. No change was made to production owners, persisted content, or the Daily-v1 profile.

### Validation for the exact implementation input

The approved runners recorded stable input snapshots. The code was committed after execution; for the three changed sources, exact run-input CRLF hashes and committed LF Git-blob hashes are crosswalked below. The test-run source content matches after line-ending normalization; no source/test content changed between execution and commit.

| Gate | Result | Run ID | Private manifest SHA-256 |
| --- | --- | --- | --- |
| CoreValidation cold and warm | 1007/1007 each; all build, demo, and lab gates passed | `78d505f408e145feac7805b3bc17f799` | `7b8a5d15443af0ca9ffbfcb6e7238ef574946f6bfe4d9ac57e0b4a2ab4862906` |
| Focused initial-restore publication test | 1/1 passed | `fa4521b6d2da449b896411e78e13bb12` | `6de4e613f30eac87d275e3235eeab6ea300eda39b901e9f76966a742ee2e6ff2` |
| P12 regression suite | 566/566 passed | `d2db3745445d4508aebcd4d93f7e0c55` | `3df905c83c4694d8efd3a7be42f0e3361c2d6e2fdf6bd22878589acd920c8eb1` |
| All Unity EditMode | 3299/3299 passed | `27149e88d83840d3b1896a80f550c24a` | `421fb83129a9e42c1bb2fbdf443f3bfca29f539b5fa104dda7d727d880740ad0` |
| Official Smoke filter | 5/5 passed | `c949c0d6ca56436a963fee1be353f25c` | `1d4dacc26723f918351595205e019c42f677df2bc165420c482e5caecb571692` |

All listed Unity results have zero failures, skips, and inconclusive cases. The focused run's stable result XML contains one passed test. The initial focused invocation timed out while Unity held its log open and produced no run manifest; it is excluded. After the Unity Library was warm, the official focused rerun completed and passed. `git diff --check HEAD^ HEAD` passed for the implementation commit.

| Source | Run-input SHA-256 (UTF-8 CRLF) | Committed Git-blob SHA-256 (UTF-8 LF) |
| --- | --- | --- |
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | `11a8a6cea8c895f36bcf7cbbddb6d7f08c22130c84baf21840cb475144a31157` | `bac17f2417a36902e9c91a416973177db0f6de3c0cd6230a10d84553409033fc` |
| `Assets/_Project/Scripts/TesteSimulacao.cs` | `b135371a3b66f00c1e7dc770677d30b598bda5a361b4b89e1e32ba577c7552eb` | `038b51003df4deaea4249a5e0d2b0643afcf37d8bf41dbdff8aea5e3b51d3b9c` |
| `Assets/_Project/Tests/EditMode/Editor/P12GOwnerEntryQualificationTests.cs` | `1d1ea0f1a92b58ea23f04f17874b0f0d80c753c04760425f4b96d802b3a81c1c` | `53f13b58380ca7b51f3ea35ea1f48fcc1892b0d56499566aed2bd24ab43360f7` |

CoreValidation's cold and warm raw TRX files remain private and unchanged; their SHA-256 values are `ff4b9cf27ae4e3cedbaa40b410274983c1cec597001507d844090c504a21050a` and `501a2e04b6f837cb4d48eae17b1df25cadd022a035118fb69f85f692f245c159`. No public normalized .NET TRX package is claimed. Unity original XML/logs and private manifests remain in protected validation storage and are not included here.

The complete EditMode XML was packaged by the official evidence packager and verified against the retained private full-run manifest. Public package locator:

- Manifest: `UnityEvidence/coref-initial-restore-publication-editmode-20261103.manifest.json`
- Published manifest SHA-256 (committed UTF-8 LF bytes): `c528d190b9387231d50134fad1a32b39318c73a03f1370222b43d704f6d65bb9`
- Archive: `UnityEvidence/coref-initial-restore-publication-editmode-20261103.zip` (174385 bytes)
- Archive SHA-256: `d9192b5108f4ae7a6e59ef27642bcc1dbe4fbeb69465e73c7c4e5b0977a29c24`
- Manifest binds full EditMode run `27149e88d83840d3b1896a80f550c24a`, 3299 passed cases, original XML SHA-256 `1b7ed9d581ee931e07505a2d6f3c88844393028f1731c5ee911dd426e531f8fa`, and normalized XML SHA-256 `066447bed2cd56ca9aff96f1f38d256ce5e1204897d73ddcb67d23346262aafc`.
- Official package verification passed: one run and one entry, archive size/hash consistent, all 3299 results passed, and the public manifest/XML passed the configured local-path/account and credential-pattern scans.

The package contains normalized result XML only. Original XML/logs and private manifests remain unmodified in protected local storage. This evidence establishes validation for the bounded atomic initial-restore publication change only; it does not establish full CORE-F completion or Core canonical promotion.
