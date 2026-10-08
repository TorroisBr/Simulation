# P12-D private owner-package assembly validation

## Scope and exact candidate

This validates only the private unpublished P12-D Daily-v1 D-owner package assembly and D-graph validation seam. P12-D reuses the promoted City, NPC, Person, Genealogy, SpatialNetwork, and site/receipt evidence owners. P18 LocalObservation and MerchantTradeState remain exact-zero census evidence only; no receipt data is copied or replayed. The package is not published to `SimulationRuntime` and does not implement P12-G whole-profile validation, envelope creation, or final runtime publication. This does not claim P12-D completion, P12-A readiness, P13 readiness, capture eligibility, profile-wide export/hydration, or Phase 12 closure.

- P12 canonical base: `a4ce0abcf261226f4b52fbacc8df9ef8f67a0de2`.
- Reviewed current-base design tip: `d1fef9acf95f40c918784b471e7b0c7191f89905`; independent exact-tip design review PASS `8a68a003601d9197e63fe4fe5fc567eab6a4b58e`.
- Effective architecture authority: `47eff22c7ce00f6e7c759bdc2b76780bb46f628`; `docs/SIMULATION_ARCHITECTURE.md` blob `25843842688239cdc3b80988b2e28dbaa16b4987`.
- Implementation branch: `codex/phase12/P12DOwnerPackageAssemblyImplementation`.
- Code-only implementation commit: `aaf727b0f32cedf93aa895aa93461ae6a6a2d9e9`.
- Full Git tree at the code commit: `57194a3800f1a14015541c1a8ba382f78e168f28`.
- Exact `Assets` subtree at the code commit: `012791f5ea4dfd6bd53aae9751ca7ba6d9d950a3`.
- Code source blob / worktree SHA-256: `e2bd14470d5953c35cb38c127e6edd09c25101f1` / `FE4104862717068F0CD05EEE0D5D2D8C2071800D0479CD86CBF5532B6E6BB3E9`.
- New code `.meta` blob / worktree SHA-256: `4d7d95c8c106f6ea7930be9265e444db52f55e36` / `65D4E2485A13ABB1286FA6239AA8B146409F66EB9F3B6864FE5CFE7FC7F08C8F`.
- Test source blob / worktree SHA-256: `334a0519720110b831e3cbe4182a47148aa25af5` / `390DA09265845C9743B61873C5080EE42F43CDBEEE19A9DA2357EBE36CFEF291`.

Architecture §85A demonstrability assessment: `NOT_MEANINGFUL_FOR_THIS_CHECKPOINT`. This candidate returns only a private, unpublished reconstruction package; it has no standalone world behavior for a person to observe. The useful evidence is the automated value, identity, relation, and failure-atomicity coverage. A later published runtime behavior or usable scenario may have a meaningful demonstration assessment; this finding does not foreclose that.

## Validation results

All runs used `Tools/UnityValidation/Invoke-UnityValidation.ps1` on the exact code-bearing candidate above. XML files and gzip-compressed logs are retained under `Raw/`. Each compressed log was decompressed and its SHA-256 verified against the raw-log SHA-256 below.

| Gate | Filter | Result | XML file / SHA-256 | `.log.gz` SHA-256 / decompressed raw-log SHA-256 |
|---|---|---:|---|---|
| P12-D NPC D/F package and snapshot | `EditMode -TestFilter P12DNpcRootOwnerSnapshotTests` | 24/24 | `Raw/EditMode-20261008-223428-7a2a020e58cf42cd8f0c0ea6815d641c.xml` / `315845D85D408E7ABA9796D2453EB1697226AE43102CCA2BE4E97C4441C7338E` | `Raw/EditMode-20261008-223428-7a2a020e58cf42cd8f0c0ea6815d641c.log.gz` / `FDC1C78CD37B2F07E7BDA9DC275E447765F789BEEC4628A3FE0B114669B5F350` |
| City root and membership | `EditMode -TestFilter P12DCityRootOwnerSnapshotTests` | 17/17 | `Raw/EditMode-20261008-223452-ab72153fb9554f03ac90b614901273bf.xml` / `C2BA53794AF2471826858188C58648C6FED3A62B1136650D8CBAA208FBC54DAB` | `Raw/EditMode-20261008-223452-ab72153fb9554f03ac90b614901273bf.log.gz` / `CCAFA44F80059A26A8FBCDA030FC4132DE965F535EEE159413F5CB48071AA7AB` |
| Exact-zero NPC receipts | `EditMode -TestFilter P12DNpcReceiptOwnerCensusTests` | 13/13 | `Raw/EditMode-20261008-223510-950297eb2a11446399ddce526ed834cd.xml` / `17A9DB2A1915E95E4888AEF876EA1DADF3EBBB25F4923E154EF2249B61422E9B` | `Raw/EditMode-20261008-223510-950297eb2a11446399ddce526ed834cd.log.gz` / `9DDD71EB28AF712E8378F6218F07205EFE6166AC4C11DBC10B702EF142197A67` |
| Person owner staging | `EditMode -TestFilter PersonOwnerSnapshotTests` | 5/5 | `Raw/EditMode-20261008-223535-a67ebb2c1f0b4c149ba6e8856a7034c5.xml` / `08D46299645118D1CC43293E0440B8B303463F606350C48F7E7F5224AD949C78` | `Raw/EditMode-20261008-223535-a67ebb2c1f0b4c149ba6e8856a7034c5.log.gz` / `6F8E8E2033186ACD2932B6E0FED4680C1E6157C0FF679CD819CD82A9068FD417` |
| Genealogy owner census | `EditMode -TestFilter GenealogyCensusTests` | 7/7 | `Raw/EditMode-20261008-223553-b9419c5007d846498a262993ef546f22.xml` / `0B91A7C85CAB61A721E62AB2534A21B99FF8CA6D2354301DE2362B1EF84F3E7D` | `Raw/EditMode-20261008-223553-b9419c5007d846498a262993ef546f22.log.gz` / `6C090E50FB4550366F7959A1242CE16B7044968BCB4F4568051C54D9366F1E2E` |
| Genealogy world integration | `EditMode -TestFilter GenealogyWorldIntegrationTests` | 18/18 | `Raw/EditMode-20261008-223611-dcb06cc91bb84481a1dcba4da4e652f5.xml` / `413DD16DBC58A619D8C27DC385C37C3ACC028D000B27706626FB991105BBF948` | `Raw/EditMode-20261008-223611-dcb06cc91bb84481a1dcba4da4e652f5.log.gz` / `503CD5A306499E5CEB767291024D5983C1C4551A461D91EA9BB349328B23B2A1` |
| Legacy SpatialNetwork census | `EditMode -TestFilter SpatialNetworkCensusTests` | 11/11 | `Raw/EditMode-20261008-223631-42534cb054bb41bc9f431589df47ecdd.xml` / `97E3FA9020AF2811466FD194639EB9E00F529E1CDA45B918225C933C2C3730D3` | `Raw/EditMode-20261008-223631-42534cb054bb41bc9f431589df47ecdd.log.gz` / `828E79C93F28D1B05F1DFEFD339B50CF6509D4D1CCA071B586B6B54A1DCCFE5C` |
| ALL EditMode | `EditMode -All` | 2626/2626 | `Raw/EditMode-20261008-223649-e7a5403bba4b4e38b5c457b9b7620ffe.xml` / `C8866CEA3EEA54532D17A22945E9BF3AAEAA386A1BFBADD01BD221AEC3D38710` | `Raw/EditMode-20261008-223649-e7a5403bba4b4e38b5c457b9b7620ffe.log.gz` / `E3CD0D7F827E950F5B66FAF015A0DBE470287DC96EB71E30E556EBD3C6D613AE` |
| Official Smoke | `EditMode -TestFilter Smoke` | 5/5 | `Raw/EditMode-20261008-223814-ccf18e995091432d80ec25b0888c3620.xml` / `159B0585AF348956FE7CE824BB1390F94BF1F353728603B92207067A017B1731` | `Raw/EditMode-20261008-223814-ccf18e995091432d80ec25b0888c3620.log.gz` / `848E0FDAF10C3FCAE6BCDC727B690E659CA9A53E3960588E5E94B1907CEC6F86` |
| `git diff --check` | worktree and staged owned paths | PASS | n/a | n/a |

The smoke gate is the repository's EditMode `-TestFilter Smoke`. A preliminary PlayMode invocation using the similarly named `GMConsolePlayModeSmokeTests` returned a valid zero-test XML and is not counted; its XML and gzip log are retained for transparency.

## Corrected fixture and superseded attempts

`DailyV1Package_PreflightsCityLocationReciprocityBeforeAnyMembershipFill` captures a valid completed-boundary token and detached City/NPC snapshots, confirms the original graph passes relation preflight, then changes only a detached NPC D-row so its current City remains the origin while its current legacy Location is another City’s captured Location. The package relation preflight rejects that inconsistent staged input before membership fill. Live City membership/revision and NPC City/Location references remain unchanged.

Earlier attempts are retained but excluded from passing evidence: a compile/setup failure log `Raw/EditMode-20261008-222630-0b75f7235a0e41cd808e8cca4775e996.log.gz` (raw SHA-256 `5582F84DCDEC37E600FEFEAB52C5DB11DA28F9F3CD460B91A0D92EA1BCFA2A6D`; gzip SHA-256 `162375E01FA514E8B9EEF097557DC33139348D57B3C0CDF67DC3354F3EF83654`); a 23/24 run `Raw/EditMode-20261008-222757-51f179b4d73743b483eb4ceacc5d5a1e.xml` (XML SHA-256 `D1F68A6D6E9B76267A5BC411EBCB69C9EEE7F021D88D13BF414CFA0AAB23CA63`, raw log `5BBBA3BB4B2B6D480A76E0130C3419E621A76E0CEBBD89C9A5831D34375DBCB7`, gzip `0BB830B62893B08173018A8CC92F7BEEE8C6A9651DF0C63FC744D1B097E45BA8`); and a 0/1 malformed-live-state attempt `Raw/EditMode-20261008-222922-79ab640f5c6e4b98b8cbcd8b9bfffac8.xml` (XML SHA-256 `A9FA9C09CF393E48CA3104AF294AE11379555453D3809F9A09FF959EEA13078E`, raw log `0C0B3BC2A6905476B7067E7F60C867E15AFCBA2FC0780F345F393E1385AB8503`, gzip `9BC90613D66E2CA4544F60975C21F132205917C68BD8C0FFA2FB0D6880EBDE32`). The final fixture now mutates only detached snapshot input and passed in the 24/24 focused run above.

The zero-test PlayMode probe is `Raw/PlayMode-20261008-223732-b5fb79d62b19446e92161affd48dd4a4.xml` (XML SHA-256 `B5D2C13953CF3AF6B9C20E945C8D3B0B8E1F6A3BF55D0F03E465037F790E6483`, raw log `D60C0C3EECBD0626170B83A01081C9C0D7B2B1B395A9C54B4D855F23B0F360BA`, gzip `2E1F139F61A99281E919180C0F68150B58065535C9C11A0C1988D29B05998E3E`).

## Worktree hygiene

The code commit contains only the D package source, its required `.meta`, and the D snapshot test update. The following environment/Unity-generated or unrelated worktree paths were left unstaged and untouched during candidate staging; current SHA-256 values are recorded for recheck:

| Preserved path | Current SHA-256 |
|---|---|
| `ProjectSettings/EditorBuildSettings.asset` | `58BCBFB23DA7AAB5ACD696E7D83E9D75A86442ED39A582159D2493049361BA28` |
| `ProjectSettings/ProjectSettings.asset` | `F0771A23C3EBCB50D651C13515EE66E6E56D321C6115F54A2193BF35DD994633` |
| `ProjectSettings/ShaderGraphSettings.asset` | `5C432C9C73FDF73FEE91C8823EA02AB98E3B7A8EDF341B5AE6901D699CCE52A7` |
| `Assets/_Project/Scripts/ArmedForceSpatialPosition.cs.meta` | `DA5EB6A7C27BCF3459ED9CF2748323F1B0D64BD9C3979927F2EC7868933C98AE` |
| `Assets/_Project/Tests/EditMode/Editor/ArmedForceSpatialPositionTests.cs.meta` | `2BC7BB66D77B905A4C46C3F4043C85B98921183E87D514177224916285142ACD` |
| `Assets/_Project/Tests/EditMode/Editor/P17ARuntimeTests.cs.meta` | `B76E18E84D18E7A3931570FC8505FC7CF4708016BCB5C66A7D774C3ED24CC860` |

All result files are confined to this checkpoint's `docs/validation/P12DOwnerPackageAssembly/Raw/` directory. No source or project settings outside this bounded D slice were staged.
