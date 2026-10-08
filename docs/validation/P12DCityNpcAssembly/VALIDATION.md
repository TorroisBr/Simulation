# P12-D City/NPC relation-order assembly validation

## Provenance and scope

- Canonical base: `1c7b906c172d9e47020996888db64bd2516b451a`.
- Architecture baseline: `47eff220c7ce00f6e7c759bdc2b76780bb46f628`.
- Current-base independent design revalidation: PASS, record commit `c0da9fa01aa6aab6db58b34a314ad98c45aef362`, retained additively as candidate ancestor `28318d584a497dd4d0fbab48181884ef4d2a964a`.
- Implementation candidate: branch `codex/phase12/P12DCityNpcAssemblyImplementation`, code commit `f1d9003ac112391501249c6cda55d2dbbc06a64d`.
- Candidate root tree: `f92420e4f33aae8b8d322b7811e9c0aaa29c9e37`.
- Candidate `Assets` tree: `0412f8455bfe3616da328379a5eacdc7edaaa7ac`.

This bounded slice adds a transient D/F capture-identity proof, exact-zero validation of both excluded NPC receipt-owner families, direct-reference staging for one NPC instance, and whole-graph City/NPC relation validation before ordered private City membership lists are filled. It preserves captured `ImportantNpcs` order and revision without gameplay mutators. It does not add P18 export/replay, new P12-B mutation semantics, P12-A/P13 behavior, profile-wide export/hydration, runtime publication, or Phase completion claims.

## Validation results

All Unity runs used `Tools/UnityValidation/UnityValidation.psm1` from this exact worktree after the final code/test edits. Each focused filter passed; ALL EditMode and the official Smoke filter passed. Each `.log.gz` file was decompressed and verified against its raw log before recording these hashes. XML and compressed logs are committed below; the raw logs remain in the implementation worktree and are not committed (the ALL EditMode raw log is 118 MB).

| Suite | Result | XML SHA-256 | Local raw log SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|---|
| `P12DCityRootOwnerSnapshotTests` | 13/13 PASS | `4DCC5080E117CF8E5081FF755C4CA3AFA3685DF9C84EA104DABB08A100332F2D` | `BAA331F0C372990D275D0B03E550AC1F0F18458FD8686CDAF87BF98906173905` | `19F642916CB9B5CD6831B285005704F4002B94D5D79C00C217963A0F2CCC86EF` |
| `P12DNpcReceiptOwnerCensusTests` | 13/13 PASS | `7286BE9FEF17DA6053529143C7F3413D68DED3EC56B35AE008A2F5C9F2764E6A` | `0213CB31DF518A9EAAA3EB3078E81D9DFDF80EF9E566855BD890306DD64E396B` | `D04996AA3AD288A2651DD38CA0F9C8A4D0F3417FF4F0F7F1E6E85704B15E94D2` |
| ALL EditMode | 2588/2588 PASS | `7F6114F3A5B88E09C79D4ECE901C258E16B024412F6F387C732509C2C16BB2E7` | `AB2BD00905AE7178F1EC5F386527481688460B38906D16250ABDA11E02E31598` | `B4D97DA9ACBC80E436D3E43507FB0347400A60D666BB96C5BF5911DF8E7AC512` |
| Official Smoke | 5/5 PASS | `EED85676F25E8C41A181D58F9B6FD6AEC2C9239C932FF473682DB4E3DD78AA67` | `0767A816D70F272DF64454B749FE6A7369018EC8FA38BD6D5AA5878B165BE94E` | `3B47A5E6B7444DC0E58EA74CA3B1BBE4CAE436B38642018A0A19C5F94AD63286` |

Artifacts are retained under `docs/validation/P12DCityNpcAssembly/Raw/`:

- `Focused/P12DCityRootOwnerSnapshotTests/EditMode-20261008-184503-1087cf8c81fa42dd96a6319290f67e8e.{xml,log.gz}`
- `Focused/P12DNpcReceiptOwnerCensusTests/EditMode-20261008-184710-3f2ef4d0786c4de7ac261ec874ad5ef8.{xml,log.gz}`
- `AllEditMode/EditMode-20261008-184728-9ad51fd3e99d47568b36d941b1dbb812.{xml,log.gz}`
- `OfficialSmoke/EditMode-20261008-184810-ccbef075602240e398731382276769e4.{xml,log.gz}`

`git diff --check` and the staged diff check passed.

## Source identity

The SHA-256 column is the source byte hash from the validated worktree. Git blob IDs identify the committed source objects in code commit `f1d9003`; the checkout is clean for these paths after commit.

| Source file | Worktree SHA-256 | Git blob ID |
|---|---|---|
| `Assets/_Project/Scripts/NpcRuntime.cs` | `F54638DBB7934DA2A54C216F2B7A46D2556539E7A53FC65C81256DD956549634` | `4d6302d2f5769c59ff685e195de9b137e1189c05` |
| `Assets/_Project/Scripts/P12DCityRootOwnerSnapshot.cs` | `9589FE07A5F58E510773B49332E7F414C7A8259C45F466D9D379804A1ABF4047` | `235dc10c9f543cadfcaee098e4b3e7be939c161f` |
| `Assets/_Project/Tests/EditMode/Editor/P12DCityRootOwnerSnapshotTests.cs` | `EFBA306F1627268C4729B27DE6DB1513FFA1560452B2EF2AD227A5F1006FBCA7` | `286d21506406538c2cea07ffe61342a0cd57429c` |

Unity also left unrelated `ProjectSettings/EditorBuildSettings.asset`, `ProjectSettings/ShaderGraphSettings.asset`, and untracked ArmedForce/P17 `.meta` files in this worktree. They were not staged or included in the candidate.


## First review correction (superseded by capture envelope)

The initial City-to-D/F capture identity binding was validated on implementation commit `4d93a4aa38ed66712c7c8b0951a61e0303d6c7af` (Assets tree `60ba6768fd1d9c01bfe365fc29ffca4eed1e9a6c`): focused City assembly 16/16, receipt-owner 13/13, ALL EditMode 2591/2591, official Smoke 5/5, and cumulative `git diff --check` PASS. Exact source, XML, and log hashes are recorded in [VALIDATION-FOLLOWUP-CITY-IDENTITY-4D93A4A.md](VALIDATION-FOLLOWUP-CITY-IDENTITY-4D93A4A.md). This follow-up preserves the earlier candidate's evidence and does not imply P12-D or P12-B completion.


## Swapped-pair review addendum follow-up

Review addendum `d36d63a91f5440527a35d1bea724ef89ac4b1623` identified that City snapshot values and capture identity could be passed separately. Candidate `5aceb2b7ce49ffe009489627731cd6689fe2d200` replaces that path with an unpairable capture envelope and passes 17/17 City assembly, 13/13 receipt-owner, ALL EditMode 2592/2592, official Smoke 5/5, and cumulative `git diff --check`. Exact hashes and evidence are recorded in [VALIDATION-FOLLOWUP-CAPTURE-ENVELOPE-5ACEB2B.md](VALIDATION-FOLLOWUP-CAPTURE-ENVELOPE-5ACEB2B.md). This remains a bounded P12-D slice and does not close P12-D or P12-B.
