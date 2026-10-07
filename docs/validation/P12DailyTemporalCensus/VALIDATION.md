# P12 Daily-v1 temporal owner/cardinality evidence — validation

**Canonical base:** `d8deeb3f25e4523668e25ff5579c527a5189511e`

**Code candidate:** `cb299879b35dc01d8ec65dec36fd258f3d7f85b1`

**Candidate commit tree:** `5f0201661e5e781597bc884ca7d290ce08a43266`

**Candidate Assets tree:** `4d00528482829fcb3bcde58ff1bcbe4ba69f116b`

**Unity:** `6000.3.9f1`

The code change adds assertions to the existing selected Daily-v1 NPC roster lifecycle test. At 10 → 11 → 10 → 11 roster cardinalities, it checks the live MoneyAccount, Inventory, SpatialKnowledge and ten Knowledge-family census witnesses for exact section IDs, owner object identity, cardinality and owner-local revision. It does not add a runtime provider, mutation, operation, epoch edge, capture path, export or hydration behavior.

The Daily-v1 admission test confirms authored P14-A material flow is rejected before WorldId or runtime owner construction. The existing selected-profile inventory test verifies the authored P9 geography and the selected Daily owner/cardinality baseline. P10-A Ruin/LocalTopology remains a separate proving profile.

| Suite | Result | NUnit XML (saved SHA-256; raw SHA-256) | Unity log.gz (saved SHA-256; raw SHA-256) |
|---|---:|---|---|
| Daily-v1 rejects P14-A before identity/owner construction | 1/1 | [`XML`](Raw/DailyAdmission/EditMode-20261007-132727-29d0b135b2ed4ad08eed5f7774b6e689.xml) `17A85B7C3FD2FB9B2B5591E39AC64A865E07E58414C071E0E97EBB758C699403`; raw `128C5D6F830906B64179AE8DD13EAE3F317B98F53BB1200F67C1AF1A167238DF` | [`log.gz`](Raw/DailyAdmission/EditMode-20261007-132727-29d0b135b2ed4ad08eed5f7774b6e689.log.gz) `FDB59B5AC82C14720CF0529FCA7B925F37FEC59219914E173F9A415B9FF38BC2`; raw `B90940E5E126E657CC37160DC9C7DA53D60A615C04FCEDDB400E80EFE69779D6` |
| Exact selected Daily-v1 geography and owner/cardinality inventory | 1/1 | [`XML`](Raw/DailyInventory/EditMode-20261007-132430-8eb6ba17d1144e9a86955fdca1d10236.xml) `8A193E0CABE15EC69465623E7519A9D4B0472BA0669B6A485F8C0A70DC164D8E`; raw `3CC1B5F0173A953D48321B867C22EA2A1D66F4DD6B00534EF70A28EEAD41C6FC` | [`log.gz`](Raw/DailyInventory/EditMode-20261007-132430-8eb6ba17d1144e9a86955fdca1d10236.log.gz) `D25E6EC40078EE9535E301FB2111E6075313C1B6B9C20A8EC3E7B584B49A524F`; raw `4C5BDBC259C489EABC22F97EBC6E66F67483D0587222785724081B66A3FD4DBF` |
| NPC owner-family census across roster changes | 1/1 | [`XML`](Raw/TemporalMembership/EditMode-20261007-132358-a5938489294b4a3c9f78d680c37c51a2.xml) `5438A4236F1000C0BAB7EFC3F264EDCF01A61A6A033B2D076430F92851C11143`; raw `1F12E5565BAF7A05BCA0FB872E84AA1EE5E75EADC91E349BB6B6038082687AD9` | [`log.gz`](Raw/TemporalMembership/EditMode-20261007-132358-a5938489294b4a3c9f78d680c37c51a2.log.gz) `15D0C7F6EC8638696E494826CC4CFFCFA65233E9D4F46A436DFDDE5984773AC9`; raw `7BAB8941A0DF4DB34D6F2D744B0F3412E893189A5C2570558ABE10C83FC89915` |
| ALL EditMode | 2443/2443 | [`XML`](Raw/AllEditMode/EditMode-20261007-132459-b1b0d70fc4134748b48b1de894252a07.xml) `CB8CD8BE03B4B986609665864DB7FF2E263A0588BEC9156BA609DCCC6D0D2920`; raw `3327C59B04AB88287BE5B6E9E1EE285ECB8181365B2A0DD0D40E4C2B5C2C29EC` | [`log.gz`](Raw/AllEditMode/EditMode-20261007-132459-b1b0d70fc4134748b48b1de894252a07.log.gz) `D1AE2BD136BACD8BFE07AB51F0872FA83FFBF62C725DB71F6C5568F83AC798E5`; raw `46D5665675FB3783099B29CA7C743B84C35DD211C375E376CB5810B59203600D` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 | [`XML`](Raw/Smoke/EditMode-20261007-131942-80057401bc714836855ae0bd62e33cfa.xml) `4DC50AAAB0DB70DC2970D343F1C70C12B2E1EE657A41D0C2877A8313026A9ADE`; raw `887620E7CAED4E93B63FA5D6B0835B8C2EF06D0BFE8A2A8485DDE9D65092794B` | [`log.gz`](Raw/Smoke/EditMode-20261007-131942-80057401bc714836855ae0bd62e33cfa.log.gz) `02E749B350ABAAB67A685F048FF43EDF1844DDEC64FC183B8272E10751D57A59`; raw `DED39E0CFFA3C2265198BD12518F876359C7BB0BC506B680252134D26125EA47` |


## Reproduction commands

```powershell
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter 'SimulationRuntimeAdmissionTests.UnityBootstrapDailyRejectsExogenousMaterialFlowBeforeIdentityOrOwnerConstruction'
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter 'SimulationBootstrapCompositionTests.SelectedDailyV1ProfileBootstrapsItsAuthoredP8GeographyBeforeDayOne'
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter 'SimulationBootstrapCompositionTests.SelectedDailyV1NpcMembershipCommitsIdentityAndKeepsItAfterUnregister'
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -All
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter Smoke
```
All XML artifacts report zero failures, skips and inconclusive cases. Sanitized XML and compressed logs redact workspace/user paths and host/network-discovery output; raw hashes bind the retained source artifacts. `git diff --check` passed. These tests demonstrate the bounded selected-profile/roster witnesses only; they do not prove exhaustive 275-section evolved-state coverage or a complete successful-write/shared-epoch matrix. P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, and P13 remains blocked.