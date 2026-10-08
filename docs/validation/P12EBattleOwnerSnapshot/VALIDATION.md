# P12-E Battle Owner Snapshot — Validation

**Base:** `codex/phase12/canonical` at `ef0cafb5848cadcf0ac91a3e1af7ff3faaab1367`
**Code commit:** `2f78244c5a4dc96d3ac45e87335b341912b81947`
**Validated Assets tree:** `7a92f17b81dea315ef9cf4330421749a41f08767`
**Design:** `9b1b1a5603056c57d89cd048b434993aeb927397`; exact-content design review PASS `4b008f818469070a4b12e8a7b1c599ffe3015ef1`
**Unity:** `6000.3.9f1`

## Scope

This bounded P12-E owner slice adds detached schema-v1 export and unpublished exact-value staging for the existing `PersistentBattleStore` in `UnityBootstrap-Daily-v1`. Capture binds the snapshot to the exact successful P12-B completed-boundary token and owner-section vector; staging reconstructs Battle rows against staged Force, Conflict, War, and spatial parents. It rejects composed `LocalTopology` for this profile and never retains the ephemeral capture token or live object identity.

This does not wire runtime/bootstrap publication or P12-G atomic graph publication. It does not claim complete owner coverage, shared-epoch coverage, quiescence, capture eligibility, P12-A readiness, P13 readiness, or Phase 12 closure. P12-B remains complete only within its recorded bounded contract; P12-C remains complete only within its promoted bounded scope.

## Validation results

| Suite | Result | XML |
|---|---:|---|
| `BattleOutcomeApplicationTests` | 20/20 passed | [XML](Raw/Focused/BattleOutcomeApplicationTests/EditMode-20261008-155312-cb48ea299ab0429989f103f7b05a5211.xml) `56F4C02778B6E13DA814E4B31116FF5AE5D45DF4A15F995AE989F4BAFA60BF97` |
| `BattleSpatialBindingTests` | 7/7 passed | [XML](Raw/Focused/BattleSpatialBindingTests/EditMode-20261008-155322-cd870faaa3884670a1f342140af118e8.xml) `B11B9EC6A4E57C7889998029594F0ABABF63CC8056E10A1F08D48FE206B5C8A4` |
| `P17ARuntimeTests` | 10/10 passed | [XML](Raw/Focused/P17ARuntimeTests/EditMode-20261008-155344-8eb5394849514c07be87443e1b2a42ff.xml) `96321C168D56C6A85DCFECC9E0A4EF172DA115ABAC8E754FDBD7BE41B03CC473` |
| `PersistentBattleOwnerSnapshotTests` | 6/6 passed | [XML](Raw/Focused/PersistentBattleOwnerSnapshot/EditMode-20261008-155254-56ab18d54bc9452485dd493e75e70173.xml) `0977C1D3DF56473A489467A288F81EEC7A0D65A3D712E1D09CF0D325C2753F0A` |
| `PersistentConflictWarBattleStateTests` | 9/9 passed | [XML](Raw/Focused/PersistentConflictWarBattleStateTests/EditMode-20261008-155333-542ff84d8bfa427ba48ecced46eea3b9.xml) `7BD77D5209DDCE3131E3721A923870DA10A7D80665BAC5AFB107770F433C0AB9` |
| `ALL EditMode` | 2569/2569 passed | [XML](Raw/Full/EditMode-20261008-155359-47ffd3d254814b91b99ab8032f2655db.xml) `F659DFDCEE88488220D9F64B0BD1E4D178819654FB89B253C78998C5D48F355B` |
| `Official EditMode Smoke filter` | 5/5 passed | [XML](Raw/Smoke/EditMode-20261008-155439-b344d941d43d4ca8844bbc7d3c15030d.xml) `8F76B5FA7244B546040E9DDA7D88FAD0625FF40571434371C0764E84D0CA5F9A` |

Focused total: 52/52. ALL EditMode: 2569/2569. Official Smoke: 5/5. Every XML reports zero failed, skipped, and inconclusive tests.

## Compressed raw logs

| Log | Compressed SHA-256 | Decompressed raw SHA-256 |
|---|---|---|
| [EditMode-20261008-155312-cb48ea299ab0429989f103f7b05a5211.log.gz](Raw/Focused/BattleOutcomeApplicationTests/EditMode-20261008-155312-cb48ea299ab0429989f103f7b05a5211.log.gz) | `7A16A9E14051C1352D752EB3F9ABD7F5FD541AF1C638000C5BB8669C364CAD23` | `29ED5C6D6D8842046F28C9DBB5F844C5640B028322F957EDAD0FB2BC81BF7B62` |
| [EditMode-20261008-155322-cd870faaa3884670a1f342140af118e8.log.gz](Raw/Focused/BattleSpatialBindingTests/EditMode-20261008-155322-cd870faaa3884670a1f342140af118e8.log.gz) | `F36F9DBC904C536C4935FD5D3CEE8EF6272D6282B0609A37E575978306092950` | `8A561F7A5E8C4A0CF05130169F98CD208B5C6457CC25B3B41DFF84EFD51D61CD` |
| [EditMode-20261008-155344-8eb5394849514c07be87443e1b2a42ff.log.gz](Raw/Focused/P17ARuntimeTests/EditMode-20261008-155344-8eb5394849514c07be87443e1b2a42ff.log.gz) | `B2B0B20FA3EA215232764226CCCC0517EDEE1675C92962E723946451261C8C3F` | `DFD1B9D497EBACE6B68A4FA515DC7D89FCB833CBB0273018665099DB4F7491FB` |
| [EditMode-20261008-155254-56ab18d54bc9452485dd493e75e70173.log.gz](Raw/Focused/PersistentBattleOwnerSnapshot/EditMode-20261008-155254-56ab18d54bc9452485dd493e75e70173.log.gz) | `8FE2E39641127D5D3ADF619CAE32A831F10BFF340ABF17A5671C8ACD76BD9B9C` | `D2F445F388549001A36BC2551CCB72DBBD4AAF71CC165FB9529F94B983DA5C85` |
| [EditMode-20261008-155333-542ff84d8bfa427ba48ecced46eea3b9.log.gz](Raw/Focused/PersistentConflictWarBattleStateTests/EditMode-20261008-155333-542ff84d8bfa427ba48ecced46eea3b9.log.gz) | `C711DF0CA97C848F0EEAB909BFCB90D4EFA4CF1703888C43236F831439DCABA7` | `4E01643802CC25BF26B7AD958764B976E3027FA5EB526A9B1B6C6916EC1ADF5F` |
| [EditMode-20261008-155359-47ffd3d254814b91b99ab8032f2655db.log.gz](Raw/Full/EditMode-20261008-155359-47ffd3d254814b91b99ab8032f2655db.log.gz) | `F2718E726DDC617B47EBBA51FC0F46D9BE3A87EBE5E1F35D5F173C6B050F6D79` | `419E516C68343C5C8200290CB445A27A73A0229ECEAA5755F7206427A37FCEC6` |
| [EditMode-20261008-155439-b344d941d43d4ca8844bbc7d3c15030d.log.gz](Raw/Smoke/EditMode-20261008-155439-b344d941d43d4ca8844bbc7d3c15030d.log.gz) | `F51011DFCF8B6E9B82BE1145AA9A1C81AF19A9055782A08C8564A1E8187D6221` | `D027A4C2203383558F504A6C7613FA9B8F938C7816F90F2B66DF24894622A27E` |

## Source identity and diff check

Source blob IDs are from the committed code tree, so they identify the exact tested repository bytes.

| Source | Git blob |
|---|---|
| `Assets/_Project/Scripts/PersistentBattleOwnerSnapshot.cs` | `179e0bd7d3a8a9f4ee05b1d9c9ced10cf5c7646e` |
| `Assets/_Project/Scripts/PersistentConflictWarBattleStores.cs` | `34beed712544d30c85f9844defc933b059831548` |
| `Assets/_Project/Tests/EditMode/Editor/PersistentBattleOwnerSnapshotTests.cs` | `c9b55d5edb8cba7e7e9f69276b07b606e9466405` |
| `Assets/_Project/Tests/EditMode/Editor/BattleOutcomeApplicationTests.cs` | `9414f49805247fa8043fbf8aac764bd8fbfa99ce` |

`git diff --check <base> <code> -- <candidate source and test files>` passed.

Commands used: focused suites `PersistentBattleOwnerSnapshotTests`, `BattleOutcomeApplicationTests`, `BattleSpatialBindingTests`, `PersistentConflictWarBattleStateTests`, and `P17ARuntimeTests`; full `-All`; official `-TestFilter Smoke`.
