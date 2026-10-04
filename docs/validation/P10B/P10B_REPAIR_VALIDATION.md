# P10-B exact-tip repair validation

**Code commit:** `51d0f88ff204ef0e192353fa6e1f477d8d8b5b3a`
**Code tree:** `471130a97dcb099a74d6eafde79656f44166c213`
**Candidate base:** P12 canonical `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e`
**Validation date:** 2026-10-04. Unity runs were serialized in the isolated P10-B worktree using `Tools/UnityValidation/Invoke-UnityValidation.ps1`.

## Repair scope

- Pinned fixed v1 request fingerprints, complete generated graph vectors, and composed profile fingerprints for seeds 412 and 17.
- Pinned P10-A compatibility outputs: manifest contract identity and stage order, provenance count/fingerprint, profile fingerprint, site and Location runtime IDs, legacy SiteInstanceId, and DefinitionId-based semantic-topology lookup.
- Made each P10 publication store operation internally atomic across its owner writes and revision update. The complete outer undo array is allocated before publication starts.
- Added failure injection after every internal store mutation and verified that each failure restores all store contents, topology publication state, and revisions.
- Kept P10 scope and P12 Daily rejection behavior unchanged.

## Gates

| Suite | Result |
|---|---:|
| `P10BGeneratedRuinGenesisTests` | 10/10 |
| `P10RuinLocalTopologyGenesisTests` | 6/6 |
| `RuinLocalTopologyGenerationTests` | 5/5 |
| `LocalTopology` | 98/98 |
| `SpatialAuthorityTests` | 8/8 |
| `SimulationBootstrapCompositionTests` | 21/21 |
| `SimulationRuntimeAdmissionTests` | 31/31 |
| `ExplorableSiteFoundationTests` | 17/17 |
| `LocalTopologyPublicationAtomicityTests` | 13/13 |
| `RuntimeIdentityCensusTests` | 6/6 |
| `SpatialNetworkCensusTests` | 7/7 |
| `ExplorableSiteCensusTests` | 5/5 |
| **ALL EditMode** | **2262/2262** |
| **Official Smoke** (`-TestFilter Smoke`) | **5/5** |
| `git diff --check` | PASS |

The P10-B focused suite includes ten tests, including the P10-A golden compatibility assertions and partial-store failure rollback matrix. Each retained XML reports `Passed`, zero failures, and zero inconclusive tests.

## Retained exact-tree artifacts

`P10B-repair-validation-20261004-51d0f88.zip` contains the fourteen exact XML/log pairs below (twelve focused suites, ALL EditMode, and official Smoke). The archive SHA-256 is:

`D093DDD5DA8D8F3A1A305BA9C4CFA00E8D1F2795345C57737EA9EFC96E93EBD4`

| Suite | XML | Tests | XML SHA-256 | Log SHA-256 |
|---|---|---:|---|---|
| `P10BGeneratedRuinGenesisTests` | `EditMode-20261004-154445-bbd5f996229144afbcc0b04108deb661` | 10 | `0FE0989262221A09EB6769D7452F3B98B7692E313049D3FFA9F6CBE34449BC72` | `1C888C862674E2F5051FA8ECC455DEB78B1CF5F516844A0A30D961B91660B1A0` |
| `P10RuinLocalTopologyGenesisTests` | `EditMode-20261004-154455-2afcee5dc62045a2ac8dcb4b9e5f5ce5` | 6 | `F8A64E18B393ABD5578C2D6E26D6F5694FEEE3C1B126A6294D0991CABEECC761` | `78A9CA51F9DD04DA8421735C2CCE929CDEEDEC42556CAFC1EF3EC7B8D03C5B87` |
| `RuinLocalTopologyGenerationTests` | `EditMode-20261004-154505-89f10fe31e3e462ebce134298286f12f` | 5 | `F805E4991578F2E4668CC574721C5507EB598083F2B0480824E35B4F781D39D3` | `5605943C35708AE712925834DEA77D3966BC75C6950B0908A66DAF8A58322484` |
| `LocalTopology` | `EditMode-20261004-154514-c5550931e40646a5a465fcce9e64adfb` | 98 | `FC708C21C4380B0A81B8F6A4D27BEA60ADD15967D4863A52D985200146C2E532` | `8AC1695C3E80845C14B2A63A52D7DEA8D0ECFAD7B30CB8DC55AB413702DF48F2` |
| `SpatialAuthorityTests` | `EditMode-20261004-154524-157cd9665ae245f3a8c6581c4b1641e7` | 8 | `BFA5818AEA2327FEEED6431A674486590D1553C8F561E3004DBBC1E8EDBE124F` | `C3F71802543F9890A5236AC76FAFC0980CD6C04086F57C755EDAD89A8E200F38` |
| `SimulationBootstrapCompositionTests` | `EditMode-20261004-154534-3e0cf800e960445b90a267c74744f2f9` | 21 | `22E4EB9E587466D05A1DABBAECC8CC29EA1CE2A52EDC570D279115C5AA44BAC7` | `DD822F0A21D2F5E0DF3FDFB15245DD66F5CD71C24D49A50B349406E45F07FDE9` |
| `SimulationRuntimeAdmissionTests` | `EditMode-20261004-154543-39b859d085c54d869a319a8d046d0654` | 31 | `61F4119FE628B35E83242C457FBAF7E4F39DEABAB798E663DEE0AD9CAB4776B2` | `590C5016093D2D26C4C529142F95AD44EBDF4A7F04478BDB86F1339F2E6B8F5A` |
| `ExplorableSiteFoundationTests` | `EditMode-20261004-154553-efed201b64224055a34181435208f4d1` | 17 | `4245A18B4CD90FEF3726EE069BCB647FF885BE34E76B21A644672141AB90B0F2` | `566E335729632C08B35CAA51122F7D4A28BECF3E154BA2B534BDF2B3763AD6EB` |
| `LocalTopologyPublicationAtomicityTests` | `EditMode-20261004-154602-85d1ab4191c742d1b031aa5cd5a350e1` | 13 | `BBC50B2467F3F5732E6DA7E3303C6C485A1CBD1AD92957D17F943008DBA719E1` | `DA6E31356647A37E946A9E047F4A8998F2A4EE1F8D01C34A0E20993FF1008B9B` |
| `RuntimeIdentityCensusTests` | `EditMode-20261004-154612-2d8d0e6aca3c4883bbee02bb394bbfa8` | 6 | `C0303388CC62B5305144AD9E2082F925BD2F0D0278EA49DD116E3EF723145632` | `89617D7B3548304CC9FCD7810FF2836A8349EAEFB316F6B90AC902E476D33E94` |
| `SpatialNetworkCensusTests` | `EditMode-20261004-154621-409a452246ac481ca2fc73aa79221166` | 7 | `0590D4703067AE77ED2D91218EDAC90AFC89BBFC20C51A668A84ABA17246DF20` | `BA0E54D987DC33A027C605A1C8AF1B784B569AC54455A94B859C148DFF911873` |
| `ExplorableSiteCensusTests` | `EditMode-20261004-154631-23243cf778de433b94d389bb489b1ad6` | 5 | `B6357D42815968DD94E708A1E59237E2EC9190B05B07BC22BAEE2DB1DCAA5CE8` | `4D4F2A1B43CFA091D5FEB0698BD38A5DB2AE775F8CAAF57335BE7BA2FC073D47` |
| ALL EditMode | `EditMode-20261004-154644-8704f87036fe4e3c89f7286fc471d08d` | 2262 | `08BB046127875FBD45AC505FF15FF29B3C84DFA7362B3B48BB3559284DBF292B` | `968B38BA1F78879A62ACD4DE6278D3F5F0197C31843F0BE271009711C5AF0BD6` |
| Official Smoke | `EditMode-20261004-154719-94d5178be60b459ba503a415d8faa3d2` | 5 | `CB852A419E84630AE19F4B1570FB25AA301CB7DFC638849AA48FF7DF1A758BD2` | `6D6C28FA6E80CEEF0256A0B0CD33932F5BE543780245F8BA53BC13BA9C48D571` |

The earlier `P10B-validation-20261004.zip` remains the original `845d267` evidence; this repair archive is for code tree `471130a` only. These validation results do not constitute independent exact-tip review or canonical promotion.
