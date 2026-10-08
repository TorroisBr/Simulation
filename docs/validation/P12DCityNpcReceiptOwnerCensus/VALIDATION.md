# P12-D City NPC receipt owner census validation

Implementation candidate for the reviewed exact-zero NPC receipt-owner witness design.

## Provenance and scope

- Canonical base: `04e7c3f49a7c9690ebc091fcfc50f05ef67009d6`.
- Architecture: `47eff220c7ce00f6e7c759bdc2b76780bb46f628`.
- Reviewed design: `5b10a58a680af9adcf56a585f6f2ac50f8e6b172`.
- Independent design PASS: `d4d2e343d3297d1dfc61b9ff52c9db25aba464e3` on `codex/review/phase12/P12DCityNpcReceiptZeroWitnessDesignR2`.
- Implementation branch: `codex/phase12/P12DCityNpcReceiptWitnessImplementation`.

This slice contributes only the two D-owned nested P18 receipt-owner exact-zero census families, including dynamic NPC roster reconciliation. Every census read rejects nonzero cardinality or revision; access remains raw and non-lazy. Rows reconcile atomically with the existing roster families under the existing shared epoch decision. Same-object re-add is preserved; a new object with a retained RuntimeId is rejected. No P18 receipt export, P18 behavior changes, new P12-B mutation semantics, P12-A/P13 expansion, capture/export/hydration claim, or unrelated ProjectSettings/metadata change is included.

## Validation

All runs used `Tools/UnityValidation/Invoke-UnityValidation.ps1` from this worktree after the final code/test edits. The 12 focused suites total 137/137; ALL EditMode is 2578/2578; official EditMode Smoke is 5/5. Each XML and its matching compressed Unity log are retained below. Every `.log.gz` was decompressed and its SHA-256 matched the original raw log before removing the uncompressed duplicate.

| Suite | Result | XML artifact | XML SHA-256 | Unity log artifact | Compressed log SHA-256 |
|---|---:|---|---|---|---|
| `Raw/AllEditMode` | Passed; 2578/2578 passed; failed=0; skipped=0 | `docs/validation/P12DCityNpcReceiptOwnerCensus/Raw/AllEditMode/EditMode-20261008-174528-4e4d0280be434a438bf874ad20929a53.xml` | `08D6E86533B7FF607B9652BAEAF542783F7BA69DF8AC6695DA310587F615010D` | `docs/validation/P12DCityNpcReceiptOwnerCensus/Raw/AllEditMode/EditMode-20261008-174528-4e4d0280be434a438bf874ad20929a53.log.gz` | `9EC6CB220FBE345BE4FA34CA5433245352BAE16CA6A32AEFB822074DCEFF5F12` |
| `Focused/ContinuationCensusProtocolTests` | Passed; 24/24 passed; failed=0; skipped=0 | `docs/validation/P12DCityNpcReceiptOwnerCensus/Raw/Focused/ContinuationCensusProtocolTests/EditMode-20261008-174339-20d6d8f4f2a745d78848b0061b99a53f.xml` | `0044C133B4D9D83E2135E4821923766DB83EB66630516DA7F9D0794B31288169` | `docs/validation/P12DCityNpcReceiptOwnerCensus/Raw/Focused/ContinuationCensusProtocolTests/EditMode-20261008-174339-20d6d8f4f2a745d78848b0061b99a53f.log.gz` | `D45404440CEFA2DB0F7B8ED842D9C9ED3489895320A78D47FC62054B7662702D` |
| `Focused/GenealogyCensusTests` | Passed; 7/7 passed; failed=0; skipped=0 | `docs/validation/P12DCityNpcReceiptOwnerCensus/Raw/Focused/GenealogyCensusTests/EditMode-20261008-174420-829fda367bed4b7282f20db89fee91f5.xml` | `8DFAC303F46367608ECE4B8902646BB7E86A033DCBC79DA595FAF0FEC7708EDD` | `docs/validation/P12DCityNpcReceiptOwnerCensus/Raw/Focused/GenealogyCensusTests/EditMode-20261008-174420-829fda367bed4b7282f20db89fee91f5.log.gz` | `AAD0318DA73A1BCC862681BBDB3D3E6426F18D9C20B3AA1D755D8C08A1159D6A` |
| `Focused/NpcInventoryCensusTests` | Passed; 7/7 passed; failed=0; skipped=0 | `docs/validation/P12DCityNpcReceiptOwnerCensus/Raw/Focused/NpcInventoryCensusTests/EditMode-20261008-174441-37d6452fe6f846bfbb06e86bdc1ff16c.xml` | `C1EDF2ADDA441E7F98E515E9009E0A0ECD8CD0364A8680BCCE683A292D007F0C` | `docs/validation/P12DCityNpcReceiptOwnerCensus/Raw/Focused/NpcInventoryCensusTests/EditMode-20261008-174441-37d6452fe6f846bfbb06e86bdc1ff16c.log.gz` | `A89181ADA2D5F0C768F98C09DD9B6BA5E161077B32D366EC9B6BC66F737254F4` |
| `Focused/NpcPlanCensusTests` | Passed; 5/5 passed; failed=0; skipped=0 | `docs/validation/P12DCityNpcReceiptOwnerCensus/Raw/Focused/NpcPlanCensusTests/EditMode-20261008-174454-4a465df6bf6641ce80b0cb1938c43b05.xml` | `F4E8D396B96D9664ED6760801CD431BBA807A8FC21F504F8580767D400AC2F62` | `docs/validation/P12DCityNpcReceiptOwnerCensus/Raw/Focused/NpcPlanCensusTests/EditMode-20261008-174454-4a465df6bf6641ce80b0cb1938c43b05.log.gz` | `C6EAB0C9C0A04E23E4C2B9E9EE429431B98C00DED38BDB4E3A6435ED16248A4B` |
| `Focused/P12DCityRootOwnerSnapshotTests` | Passed; 7/7 passed; failed=0; skipped=0 | `docs/validation/P12DCityNpcReceiptOwnerCensus/Raw/Focused/P12DCityRootOwnerSnapshotTests/EditMode-20261008-174319-b24d0ef8d60244669d4364322bae9563.xml` | `FB477FC5AD9222777C472BAC487D06F3C90C2B3F2B6959F806E074714AB8A04D` | `docs/validation/P12DCityNpcReceiptOwnerCensus/Raw/Focused/P12DCityRootOwnerSnapshotTests/EditMode-20261008-174319-b24d0ef8d60244669d4364322bae9563.log.gz` | `6A86D81CF8EB974F2F547D6376C9E470F90D8A351552A7162DF28EA71EABACF6` |
| `Focused/P12DNpcReceiptOwnerCensusTests` | Passed; 9/9 passed; failed=0; skipped=0 | `docs/validation/P12DCityNpcReceiptOwnerCensus/Raw/Focused/P12DNpcReceiptOwnerCensusTests/EditMode-20261008-174250-3be6c97fe4bc40b9af13339ecf13ce12.xml` | `74B0BBA87881B28EA6825DAA01F74B0225B3C158A643FAD11E3B73FCB2DD7D0A` | `docs/validation/P12DCityNpcReceiptOwnerCensus/Raw/Focused/P12DNpcReceiptOwnerCensusTests/EditMode-20261008-174250-3be6c97fe4bc40b9af13339ecf13ce12.log.gz` | `E2BD737D1BAFB13752A20F4886BFB5E453757E1B4FAD1A01701F179C5CE32109` |
| `Focused/P12PopulationLifecycleInvalidationTests` | Passed; 20/20 passed; failed=0; skipped=0 | `docs/validation/P12DCityNpcReceiptOwnerCensus/Raw/Focused/P12PopulationLifecycleInvalidationTests/EditMode-20261008-174349-cf5fba80d43a4227932f59b70da6efb6.xml` | `46DD076E6BBDE071F2C7C5B660ACBD83B6F2FD4908393E044DA4BD9B3D241323` | `docs/validation/P12DCityNpcReceiptOwnerCensus/Raw/Focused/P12PopulationLifecycleInvalidationTests/EditMode-20261008-174349-cf5fba80d43a4227932f59b70da6efb6.log.gz` | `1DDC05F3E8D62D2E75A9CBDC0E26113B23132194C2CC5EC2F8DACC299AE9AB82` |
| `Focused/PersonOwnerSnapshotTests` | Passed; 5/5 passed; failed=0; skipped=0 | `docs/validation/P12DCityNpcReceiptOwnerCensus/Raw/Focused/PersonOwnerSnapshotTests/EditMode-20261008-174329-96e15f5f22cb4fa399b9d29481e3de84.xml` | `E59CB7EB0D9EAB3B00FC33512D318DEA675E6C74C52B8E076F2175AF49A0382C` | `docs/validation/P12DCityNpcReceiptOwnerCensus/Raw/Focused/PersonOwnerSnapshotTests/EditMode-20261008-174329-96e15f5f22cb4fa399b9d29481e3de84.log.gz` | `5F8D0FB7D8734A3DF141513A3019FB5184B6BE44D94D81B66E2B7CB807E3AA2C` |
| `Focused/PropertyEstateMutationEpochTests` | Passed; 5/5 passed; failed=0; skipped=0 | `docs/validation/P12DCityNpcReceiptOwnerCensus/Raw/Focused/PropertyEstateMutationEpochTests/EditMode-20261008-174430-903eb9ee593c438192c40eb59da38fa2.xml` | `2715DF7C41FE704DEF7270F081B9D5ABAFA19CFFC140326DABD25729553D5838` | `docs/validation/P12DCityNpcReceiptOwnerCensus/Raw/Focused/PropertyEstateMutationEpochTests/EditMode-20261008-174430-903eb9ee593c438192c40eb59da38fa2.log.gz` | `093B5E1E41E9F3210A2A116C08F139BB82FF8FF4EC37775DEBEC516B8BC11F3B` |
| `Focused/RuntimeIdentityCensusTests` | Passed; 6/6 passed; failed=0; skipped=0 | `docs/validation/P12DCityNpcReceiptOwnerCensus/Raw/Focused/RuntimeIdentityCensusTests/EditMode-20261008-174503-42c8905910594fc881eb484bfafac580.xml` | `76D771C7AC211DF293E730969116D1B32D49AEDCDA4FB6CF43B4ECAC291642E5` | `docs/validation/P12DCityNpcReceiptOwnerCensus/Raw/Focused/RuntimeIdentityCensusTests/EditMode-20261008-174503-42c8905910594fc881eb484bfafac580.log.gz` | `B589BED354961CF59E20B0C74F4A8088BA1487995E64AD5340DE180AB3D081B7` |
| `Focused/SimulationBootstrapCompositionTests` | Passed; 26/26 passed; failed=0; skipped=0 | `docs/validation/P12DCityNpcReceiptOwnerCensus/Raw/Focused/SimulationBootstrapCompositionTests/EditMode-20261008-174409-d6eb2748f8bb411bbf66348345f9b8d5.xml` | `8A92F895853D593926932A8D6CE5058E2B9718CAD8CC343BC4332CFD733BC61B` | `docs/validation/P12DCityNpcReceiptOwnerCensus/Raw/Focused/SimulationBootstrapCompositionTests/EditMode-20261008-174409-d6eb2748f8bb411bbf66348345f9b8d5.log.gz` | `1E14B7D639C2EA38617AF5E709D5659E42E4F362E8D366A4CBB9157BFE4BA31A` |
| `Focused/SpatialKnowledgeCensusTests` | Passed; 16/16 passed; failed=0; skipped=0 | `docs/validation/P12DCityNpcReceiptOwnerCensus/Raw/Focused/SpatialKnowledgeCensusTests/EditMode-20261008-174513-db625880c38e4c5b962b206bd529a16e.xml` | `50567DBC21357F3ED14A7B5C8FE7AD86E81143C3F24E59AA8D05F290EAC751B9` | `docs/validation/P12DCityNpcReceiptOwnerCensus/Raw/Focused/SpatialKnowledgeCensusTests/EditMode-20261008-174513-db625880c38e4c5b962b206bd529a16e.log.gz` | `616C601882FCADC584B0CDB361881FE9172C12958E99E0D50176BD1D7C1C678B` |
| `Raw/OfficialSmoke` | Passed; 5/5 passed; failed=0; skipped=0 | `docs/validation/P12DCityNpcReceiptOwnerCensus/Raw/OfficialSmoke/EditMode-20261008-174603-cf4eda0260ac427da49ed86b14b65c9f.xml` | `66CE875F035AB8070F471B1FCB565DA62CDCDD0ED3D8029F09FE466F9071BCD5` | `docs/validation/P12DCityNpcReceiptOwnerCensus/Raw/OfficialSmoke/EditMode-20261008-174603-cf4eda0260ac427da49ed86b14b65c9f.log.gz` | `A1B2F4AE8351526FE5AB9201DCD61C063DCB7E449FF868F073EBED342EB9A03C` |

The earlier implementation-time constructor-order compile failure and stale 278-row fixture expectation were corrected before the final validation series; all results above are from the corrected current tree.

## Source SHA-256

| File | SHA-256 |
|---|---|
| `Assets/_Project/Scripts/ContinuationCensusProtocol.cs` | `587684B3E6C010CC9D86685B7C00416B115BB57D464B4FC1F15B0D4769CD8913` |
| `Assets/_Project/Scripts/NpcRuntime.cs` | `3AD69412ED5CBB737FA8667A54FE7A1322DA6EFBDDE489637604A6E05CCE53DB` |
| `Assets/_Project/Scripts/P18DLocalKnowledgeObservation.cs` | `7531F65CD928F9EE8C458C2FC13FB572563CB1DF0ED4BF35DE6C8AF5AFFF2D63` |
| `Assets/_Project/Scripts/P18DMerchantTradeStateOwner.cs` | `20E64C427199DF78162AA816CE0ADC8413B38047072185211B15F47BE163760E` |
| `Assets/_Project/Scripts/P12DNpcReceiptOwnerCensusProviders.cs` | `53B469C98852E1957E184075551380CAF303DBFB8E94F50539EEDD1BF0EB72F4` |
| `Assets/_Project/Scripts/P12DNpcReceiptOwnerCensusProviders.cs.meta` | `9FB465069B60EF7B5CD35C12E0549ABD346F713C1CCB3F79B728E82BFD89AD9F` |
| `Assets/_Project/Scripts/SimulationBootstrapComposition.cs` | `A58192A4C05E3FDF72744653BA8D18F07E8FE168EC882C08B55FEACC2F144DB5` |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `37C2B0A5BFC7D7EF367A0474765F9AE669555B2521DFCCC9E4427C14ED323505` |
| `Assets/_Project/Tests/EditMode/Editor/P12DNpcReceiptOwnerCensusTests.cs` | `50E4990966DB9CCE12809505C749A0CEDAD95CD729DA0A953FD034054491626C` |
| `Assets/_Project/Tests/EditMode/Editor/P12DNpcReceiptOwnerCensusTests.cs.meta` | `D1AA77BB25AF2B41C9A921FE9EDEECE6AF7A01043C732B5F71E2FAEE8C5F96A7` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs` | `3345E9A181422A37849C7D8ED1DE77C38C6247049200023B258FAC8A9907304C` |
| `Assets/_Project/Tests/EditMode/Editor/Genealogy/GenealogyCensusTests.cs` | `084E18ABDCDF97E2FD3777A935D74D2EB8017714F731CE975BB98712101BF240` |
| `Assets/_Project/Tests/EditMode/Editor/PropertyEstateMutationEpochTests.cs` | `03D7899F1878A59CFDB3A05F61A79310FC9F22F582F70B62D035647AC73667DA` |

`git diff --check` passed after the evidence was generated. Incidental Unity-generated ProjectSettings changes and unrelated `.meta` files remain outside this candidate.

## Current-tip test follow-up — 2026-10-08

The review-requested exact-tip test additions and their current-tip results are documented in [`VALIDATION-FOLLOWUP-95A5466.md`](VALIDATION-FOLLOWUP-95A5466.md). That follow-up validates the new receipt-owner cases on the updated candidate and preserves the original implementation validation above unchanged.

The subsequent saturated-epoch roster reconciliation case and its 13/13 focused, 2582/2582 ALL EditMode, and 5/5 official Smoke results are documented in [`VALIDATION-FOLLOWUP-50C9206.md`](VALIDATION-FOLLOWUP-50C9206.md). This follow-up also preserves the earlier exact-tip evidence.
