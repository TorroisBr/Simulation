# P12-B PoliticalSupport owner census and invalidation validation

**Canonical base:** `codex/phase12/canonical` at `2760fe199909708f22ea61d3eb2dd929b542521b`.
**Reviewed design:** `d29c186417e5587cf7310a15913e5968c0e20d14`; independent PASS record `34283400d0ad7aab0f57752aa31fb57844c7fa9e`.
**Code candidate:** `3b0e73824e600b5754cfd32d5bcf1ec37d78142c`; exact code tree `d4c3d901f4cd4a7b23da11edbe5c8a05f215136a`.
**Profile:** selected `UnityBootstrap-Daily-v1`; required inventory increases from 257 to 258 sections.

## Results

Focused suites: **109/109 passed** across eight affected suites.
ALL EditMode: **2434/2434 passed**. Official Smoke: **5/5 passed**.
The Unity validation harness was `Tools/UnityValidation/Invoke-UnityValidation.ps1`.

| Suite | Result | XML | XML SHA-256 | Log member | Log SHA-256 |
|---|---:|---|---|---|---|
| P12PoliticalSupportCensusTests | 6/6 | `Raw/Focused/EditMode-20261007-001750-4db144fdb9854602becfb59397b72c9c.xml` | `46AA0B7A0AB1F495FC7FA64E49EFC38D9D6E8EED42B71F1DE05AB2B3E866091D` | `Unity-logs.zip:Raw/Focused/EditMode-20261007-001750-4db144fdb9854602becfb59397b72c9c.log` | `D10F46276AC37BEB9D9EE32CB76FDA3B6DBEB4629401FD7BC8C7563822E8A6E5` |
| PoliticalSupportFoundationTests | 6/6 | `Raw/PoliticalSupportFoundationTests/EditMode-20261007-001837-ac50cb88c2894362bff802aa6a2c39a7.xml` | `F2B9BF0BC498E766A3DDE9D9B49E91BBDA08964153952BA558746D3C92351AE0` | `Unity-logs.zip:Raw/PoliticalSupportFoundationTests/EditMode-20261007-001837-ac50cb88c2894362bff802aa6a2c39a7.log` | `10F4EE9744BFAACAEAEF6D9CB3482EF6FA4B6BB5588965F6301998D6E2F9BC4B` |
| PoliticalKnowledgeSupportWorldIntegrationTests | 7/7 | `Raw/PoliticalKnowledgeSupportWorldIntegrationTests/EditMode-20261007-001846-d82ff7eed44f438fac5cd68d1da5f14f.xml` | `36AFF47AD07C9CDDA294A1A161D4B248AFD51055E39A9DEA0C85D000D1B3CE52` | `Unity-logs.zip:Raw/PoliticalKnowledgeSupportWorldIntegrationTests/EditMode-20261007-001846-d82ff7eed44f438fac5cd68d1da5f14f.log` | `B41D617CD4552750C9BBA08066D7E48008D216F066C92B5EC2894BF94183843D` |
| SimulationBootstrapCompositionTests | 24/24 | `Raw/SimulationBootstrapCompositionTests/EditMode-20261007-001908-b97ea7f890564144bf3a56e572bcfeef.xml` | `E14F362210B4078B66B5E6766410D8EA14BC0AD18FD335E4E40A257563EA1653` | `Unity-logs.zip:Raw/SimulationBootstrapCompositionTests/EditMode-20261007-001908-b97ea7f890564144bf3a56e572bcfeef.log` | `F551B75C7F7ECBCAFBF500958D51BDC472DF518B46C926368ACD8653E4A77594` |
| SimulationRuntimeAdmissionTests | 50/50 | `Raw/SimulationRuntimeAdmissionTests/EditMode-20261007-001918-f4b44869c8654aeb9e1c184c31e649e7.xml` | `30A7880924B778F9A32F64FF670EE56871F8F0BA68248BD6F25CC6E330AEFBCD` | `Unity-logs.zip:Raw/SimulationRuntimeAdmissionTests/EditMode-20261007-001918-f4b44869c8654aeb9e1c184c31e649e7.log` | `9E60A2379246A4222BB8EBC5AD7023441BD0715D6255CFE5D98A7769EA01310A` |
| P12FactionStoreCensusTests | 5/5 | `Raw/P12FactionStoreCensusTests/EditMode-20261007-001941-2155e2bca0ea4456842c95db644253fe.xml` | `5DFA467D45BC58C58A3A7E9A051D9C95C1639A6A2E0063CB6934D8F9BDA11B04` | `Unity-logs.zip:Raw/P12FactionStoreCensusTests/EditMode-20261007-001941-2155e2bca0ea4456842c95db644253fe.log` | `063BBAE41DC14DAC87BFC2539504C7283C6C7BD45D89191F3454B743864FFC4D` |
| P12PoliticalClaimCensusTests | 6/6 | `Raw/P12PoliticalClaimCensusTests/EditMode-20261007-001951-b025aaa05d4341d09b1bf67d1223f026.xml` | `05B6C2CCF58AAF319060A1DE61D33D7DA92888EC77E72D3590B0E197B4539BEE` | `Unity-logs.zip:Raw/P12PoliticalClaimCensusTests/EditMode-20261007-001951-b025aaa05d4341d09b1bf67d1223f026.log` | `A707A24FC67E234D7557F0A610651CF080E832851BB503181ECC3FAB92F5DCFD` |
| PropertyEstateMutationEpochTests | 5/5 | `Raw/PropertyEstateMutationEpochTests/EditMode-20261007-002000-e245d2ad423f48d3bee032c5ff18acc1.xml` | `21A521568500FC2C00268EE911D2459493F5CF922AECFE6F3FB59D6224190DED` | `Unity-logs.zip:Raw/PropertyEstateMutationEpochTests/EditMode-20261007-002000-e245d2ad423f48d3bee032c5ff18acc1.log` | `A242D1F47F4BDDAA89F9CA4BAEE9EB653B9AE9DA6FC4908EEA27265304271D40` |
| ALL EditMode | 2434/2434 | `Raw/AllEditMode/EditMode-20261007-002033-e72399bdd53844668d39292065b65c9f.xml` | `FA2692E70F9D134E040F20BA1303DD74D6A4E79FC5A75F4EA2EE1318F1960253` | `Unity-logs.zip:Raw/AllEditMode/EditMode-20261007-002033-e72399bdd53844668d39292065b65c9f.log` | `FAA34EA21E70F3BB7E69B6C2CCD8B6D51E10947761FEE259469CFA2DE3582A56` |
| Official Smoke | 5/5 | `Raw/OfficialSmoke/EditMode-20261007-002108-e0f23e742b104b5eb5e63c479ce0c641.xml` | `7ED01DF0CCBC7FBA871A5C4F9EE1BF10DE71B60F588E8B39704D7C3A4A8A77E7` | `Unity-logs.zip:Raw/OfficialSmoke/EditMode-20261007-002108-e0f23e742b104b5eb5e63c479ce0c641.log` | `BAACD96E5360433D08B55D655A5B6DF854E5AD3024A197558FE7EDBB5328AF16` |

Archived Unity logs: `Unity-logs.zip` (SHA-256 `EED4F2C83933B9E8759DD07CF50821388F725F93F50AFE97A9D9FA0CF26A943D`).

## Boundary and integrity

The exact selected profile composition test binds the new Required section to the installed runtime clone at its current 0-count/0-revision bootstrap boundary. The focused suite covers registration, add, end (same cardinality with a changed revision), proposal no-ops, stale/duplicate/future-day/wrong-store/revision-overflow failures, owner-thread admission, exhausted epoch, and stale baseline.

`git diff --check 2760fe199909708f22ea61d3eb2dd929b542521b 3b0e73824e600b5754cfd32d5bcf1ec37d78142c` passed on the candidate diff.
This candidate adds only one passive relation cardinality/revision witness and P12 invalidation for the three existing runtime commit facades. It does not implement export/hydration or claim complete owner/writer/shared-epoch coverage, global quiescence, capture eligibility, P12-A/P13 readiness, P12-B completion, or Phase 12 closure.
