# Validation — exact admission for initial restore staging

Implementation commit: `4fff61b27a3cafb6badb7423e7dc434d5559408e`

Implementation tree: `7db93bccc24a92baa646b24583d9fd28f21aef1a`

Base: `51c658f3e047b735226bcfbfdd48bff724bf574e`

Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

The independent review of `51c658f3` found that application-owned initial image staging accepted a missing `DailyV1ApplicationStartAdmission`. This fix makes the exact `RestoreRequired` token mandatory before initial image decoding or stage callbacks. A supplied token is rejected on replacement staging; the expected-session/replacement-operation path and the application-free compatibility path remain unchanged. Core coverage proves a missing token returns `InvalidOwnerValue` in `p12.initial-start-admission` before decoding, staging, or publication. Unity integration fixtures now obtain the token by loading the exact bytes through `TryAdmitInitialStart`; the split-alias case separately retains its detached owner-census rejection proof.

## Validation

All official runs completed with stable input snapshots. The source hashes below match the committed file bytes. CoreValidation ran in the Unity worktree with its `Library` present (`worktreeLibraryAbsent=false`); it is not claimed as a Library-free .NET run.

| Gate | Result | Run ID | Private manifest SHA-256 |
| --- | --- | --- | --- |
| CoreValidation cold and warm | 1007/1007 each; all build, demo, and lab gates passed | `461635f6e1f84c54b8596e88c7cd5b54` | `822fbf93cb161a275176679ccf4cb63d85a127c681fb106e74f6dbaca5ceeef8` |
| Focused app-owned restore fixture | 16/16 | `237987e60fc74c9181817778a4cdacfd` | `8abe6f061aa08fc5d2ac211ce2559d5ea61b0ac8121a5f1cf44f289c501ca114` |
| P12-G EditMode | 33/33 | `76c859f215694ad49200ca497c5f0148` | `db897ced25cbfb0958613adfe8d1a5e72ee1c914fd53bef92ca785167c7ebdd6` |
| All EditMode | 3299/3299 | `2ba45723b54848a6b832b54f34de9cfd` | `9882a3c1a735d32099d1f8bc7f3033fcde58e7e1a14f5b453b20e67ed97fd8df` |
| Official Smoke filter | 5/5 | `11c73a2739164efabb835859bca42cd9` | `dae538f9ca1392a2e9acce12253d38ba12d1fd606d78757806233c36b39bba43` |

The CoreValidation manifest input digest was `6be84a823ec51701da5947dde7563f3e5d63958c71302fd73de103e52c2293af`. The full Unity and Smoke runs recorded stable inputs; their Git baseline was `51c658f3` with tree `3a9aea55de713ed4984614137c6c418a96ec3f1a`. The pre-commit source hashes were compared with the committed files:

| Source | SHA-256 |
| --- | --- |
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | `693776ad45c49c7da21151d64fb7dc2e1c22ee5e64e4583ce7607b751444c797` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationHostTests.cs` | `a251597190519d43e4998a79f886127cdf4dbee52c84548c9eec77722861db00` |
| `Assets/_Project/Tests/EditMode/Editor/P12GOwnerEntryQualificationTests.cs` | `c59982310181d738a68b1ab85e3a1fc3e3eb311fde21d6876625fa13d104aca7` |

`git diff --check HEAD^ HEAD` passed. Evidence manifests and original XML/log artifacts remain in the protected local validation storage and are not copied into this repository. A separate PlayMode filter experiment returned a zero-test report and is excluded; the five required Smoke cases passed under the documented EditMode `Smoke` filter and appear in the full EditMode report.

This is a candidate checkpoint only. It does not complete `DailyV1Application.TryStart`, detached .NET hosting, or CORE-F acceptance, and it does not promote Core canonical.

## Public normalized Unity evidence package

The complete EditMode result was packaged with the repository's official validation evidence packager. The package contains the normalized complete EditMode report; it records 3299 passed tests and zero failed, skipped, or inconclusive results. Smoke's required five cases are included in that complete run.

- Bundle: `coref-initial-restore-editmode-20261103`
- Manifest: `UnityEvidence/coref-initial-restore-editmode-20261103.manifest.json`
- Manifest SHA-256: `5a5a7c33d07e5398b2aa08d83cd07331e8e6226d348f675d201cf19e9bca6f32`
- Archive: `UnityEvidence/coref-initial-restore-editmode-20261103.zip` (174984 bytes)
- Archive SHA-256: `6b0b9c5ccaf3a58455d99b1fce3853e1c0c8114898288313eb1134415d1dcef3`
- The official package verifier passed against the retained private run manifest. The archive and public manifest contain no local account or protected-store path matches.

Original Unity XML, logs, and private manifests remain unchanged in protected local evidence storage and are not part of this repository package. The .NET TRX files and CoreValidation manifest also remain protected and unchanged; this validation record does not claim a public normalized .NET TRX package or an independent inspection of those private originals.

## Independent review boundary

The independent exact-tip review passed the implementation and tests at code commit `4fff61b27a3cafb6badb7423e7dc434d5559408e` / tree `7db93bccc24a92baa646b24583d9fd28f21aef1a`. The reviewer did not inspect the protected private manifests or this validation record. The evidence statements above are therefore the Master Orchestrator's validation/package verification, not part of the reviewer's PASS. CORE-F remains an incomplete candidate and Core canonical is unchanged.
