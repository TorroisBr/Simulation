# P12-D NPC D/F owner snapshot validation

## Scope and exact tree

This record validates the bounded P12-D NPC D/F value snapshot, projection merge, and private staged reconstruction slice. It does not claim full D integration, capture eligibility, P12-A/P12-B/P13 readiness, export/hydration completion, or Phase 12 closure. P18 LocalObservation and MerchantTradeState remain exact-empty census evidence only; no receipt data is exported or replayed.

- Refreshed canonical branch: `codex/phase12/canonical`
- Canonical base: `5acc3fff94f74fcb718610825caadf645423d672`
- Integration commit: `4bf23109a7b62f7c0d21a08672c98cc9b3d7b983` (merge parent 2 is the exact canonical base above)
- Staged implementation index tree before adding this validation record: `96294afc88ce085762fe67616586a4abedcaa2de`
- Exact `Assets` tree: `d7e95170c31947fb611a7461133b60ce75ad1e4d`
- New snapshot source blob: `58c44f1b1a0ff1e3ec2a6b7eef3e7ba108067a32`
- Focused test source blob: `bb3243f68bca7e7c49c8568cd6b3ac49023cda94`
- New snapshot source worktree SHA-256: `8C3B3A56D2CCDFBEEB8F7B9CA861A962BF8EDD7C4C3F10C1786228A07DD51AA2`
- Focused test source worktree SHA-256: `4978D5AB00776D4167056754CB6F54413D8BE74A03FFDD8122D96C998D8ECC27`

The P12-E canonical delta was composed additively. Its changed implementation paths are ArmedForce/manpower/position and do not overlap the NPC-owned source/test paths listed by this checkpoint. The final validation below was run after that composition. The exact `Assets` subtree and source/test blobs were the same as the staged implementation tree above; validation documentation and logs were added afterward.

## Validation

All runs used `Tools/UnityValidation/Invoke-UnityValidation.ps1` against this worktree. Raw XML and logs are retained under this directory. Each result was copied immediately after its run.

| Gate | Command filter | Result | XML SHA-256 | Compressed `.log.gz` SHA-256 / decompressed raw-log SHA-256 |
|---|---|---:|---|---|
| NPC D/F snapshot | `EditMode -TestFilter P12DNpcRootOwnerSnapshotTests` | 22/22 | `BB2EA322EE71E299B8C03B2562B7A6FEE282CED5140915B9CA6EC6D72304A261` | `DC24BC767EF19B20871BEF6006FDC67D8A843E53D29C0C8F5B21A1C729D7EBE9` / `0AE61CD7DFC16556D4C5F03FC592B17543594A945894CB38D4774C784D6326F7` |
| City-root regression | `EditMode -TestFilter P12DCityRootOwnerSnapshotTests` | 17/17 | `4C69F376F0E4EDD708BB1529C165EE82C0429E2DC2C6C194673AC1CC6A0E13D2` | `26D28A1C57582C9EE3A883172B4FCDDFBD142273A067AD608084F00E35F5EB4E` / `87D23669DB2E750DFB8ED6E29BEEF7934E151A41B419158C74640875F652E6A6` |
| NPC receipt-owner regression | `EditMode -TestFilter P12DNpcReceiptOwnerCensusTests` | 13/13 | `9152FB459CD358465189F674CEE9ABBA9F0421E05D8DC2F39EFE9D5283EEA459` | `E577D6FC9120F7F2938822BD0FC30E3ECD7891C8DAA0A49E7FB8074E0506B2A0` / `0ABE4FFF4A817F28F8477456A49202A352A0C018E87703B98583BE98A5EF64CE` |
| ALL EditMode | `EditMode` | 2624/2624 | `3703BA8DF426990BFCFBDCC88750B9FF0C75AAF58735DB4B153A7FDBB52B7B4E` | `9973B6BBC44F201BB7A864CA940887129FC730AADB62C39755E7219D6EDBC764` / `C39EEE5F50588C301D51D9ADE41694153677D222FA41BCDF94315C9DD6750AE9` |
| Official Smoke | `EditMode -TestFilter Smoke` | 5/5 | `E882FEE3622D0A5DA9C7429F87675419E22F03308A93A4DF886A74ED26A4A800` | `41938DD6BC242886AE0FAE7DA42D02F3F0E1BF03616AC3EC11F53F7CB1A22486` / `DC019FC8A3CE0F77A4035A5D8D6E469018235E8423726CCEAF077439074A8F76` |
| `git diff --check` | exact composed worktree and staged checkpoint diff | PASS | n/a | n/a |

Retained result filenames:

- `Raw/NpcSnapshot/EditMode-20261008-204245-785596d2d73141ee9b6eaf40d560ac5c.xml` and `.log.gz`
- `Raw/CityRoot/EditMode-20261008-204301-00f426d8172341028972ce51c1095892.xml` and `.log.gz`
- `Raw/ReceiptOwner/EditMode-20261008-204316-ad6cedf980574fcbacf66c56384a1cec.xml` and `.log.gz`
- `Raw/AllEditMode/EditMode-20261008-204331-d321445f671f4f38844397e415376837.xml` and `.log.gz`
- `Raw/OfficialSmoke/EditMode-20261008-204407-1091354d506b42d790a14a7e0393229f.xml` and `.log.gz`

Each compressed log was decompressed and its bytes matched the raw-log SHA-256 in the table. Validation documentation and archived artifacts were added after the test runs; they do not change the tested `Assets` tree.

## Worktree hygiene

Only the twelve P12-D implementation/test paths and this validation record plus its raw results are checkpoint-owned. Unity-generated changes to `ProjectSettings/EditorBuildSettings.asset`, `ProjectSettings/ShaderGraphSettings.asset`, `Assets/_Project/Scripts/ArmedForceSpatialPosition.cs.meta`, `Assets/_Project/Tests/EditMode/Editor/ArmedForceSpatialPositionTests.cs.meta`, and `Assets/_Project/Tests/EditMode/Editor/P17ARuntimeTests.cs.meta` were preserved and excluded from staging. The ProjectSettings files were present as local environment changes during validation, were not staged or modified by this checkpoint, and remain outside the committed candidate.
