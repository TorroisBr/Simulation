# P12 Daily-v1 dynamic owner census — validation

**Canonical base:** `89b2368e9756069b2f52cd7cf17c26735f7c103f`

**Candidate code:** recorded by the candidate commit that adds the test-only census coverage.

**Unity:** `6000.3.9f1`

This candidate changes tests only. It does not alter runtime behavior, profile configuration, owner registration, mutation admission, operation scopes, epochs, capture, export, or hydration.

The selected Daily-v1 inventory checks exact dynamic section IDs, owner object identity, cardinality, and owner-local revisions for NPC MoneyAccount, Inventory, SpatialKnowledge, Knowledge, TravelState, merchant/travel plans, NPC life/residence, CrimeJustice status, and current action. It also checks Person life/residence after normal Person registration and NPC materialization. The sealed count assertion is `65 + 20*N + U + P`, where `N` is the NPC roster size, `U` is the count of NPCs without a bound Person, and `P` is the registered Person count.

The existing roster test exercises `10 → 11 → 10 → 11` membership changes. The new materialization test uses `TryRegisterPerson` and `TryMaterializePerson`, checking the owner census after registration and after materialization. Daily-v1 admission still rejects authored P14-A material flow before identity or runtime-owner construction. The selected-profile test verifies that SampleScene uses Daily-v1, authored P9 geography is present, and P10-A Ruin/LocalTopology remains a separate GeneralTest profile.

| Suite | Result | Sanitized NUnit XML | SHA-256 |
|---|---:|---|---|
| Daily-v1 dynamic owner census through roster changes | 1/1 | [`DailyRosterTemporal.xml`](Raw/DailyRosterTemporal.xml) | `52D2EF9804A6BAD166E135BDDEF122E0F3CF1CA7026461744AF3242698F4AFAD` |
| Daily-v1 owner census after Person registration and materialization | 1/1 | [`PersonRegistrationMaterialization.xml`](Raw/PersonRegistrationMaterialization.xml) | `86F6187EA23A7FE3F22CFA945DC6FA96BCFE04EEDAACA7A1707694BAEAC1B6FB` |
| Daily-v1 rejects P14-A before identity or owner construction | 1/1 | [`DailyAdmission.xml`](Raw/DailyAdmission.xml) | `F4DBE93E567948429E73A1C4A5EBDE002A84304A446D0562B84DE2A777813C03` |
| Selected Daily-v1 profile and owner inventory | 1/1 | [`DailyProfileInventory.xml`](Raw/DailyProfileInventory.xml) | `19EFFD936918876C8F4079B26C04B21BE490ACB60EAAA7115A1E6F16E9E490C6` |
| ALL EditMode | 2444/2444 | [`AllEditMode.xml`](Raw/AllEditMode.xml) | `524216151A4224A870F92CC60C9CA8883A967D7CA13C26C8636B18125B1AFEAB` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 | [`OfficialSmoke.xml`](Raw/OfficialSmoke.xml) | `507CCAE1E761335E967135FF7BC22CB1999F7DE0BA9AEA0CC5F23F75EDCEB9FE` |

All reported XML files parse successfully with zero failures and skips. Workspace and user paths are replaced with safe placeholders in the saved XML. `git diff --check` passed.

This evidence covers the selected profile's current owner and cardinality inventory, roster changes, and one supported Person registration/materialization path. It does not establish exhaustive successful-write coverage, complete shared-epoch coverage, global quiescence, capture eligibility, export, or hydration. P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, and P13 remains blocked.

## Reproduction

```powershell
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter 'SimulationBootstrapCompositionTests.SelectedDailyV1NpcMembershipCommitsIdentityAndKeepsItAfterUnregister'
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter 'SimulationBootstrapCompositionTests.SelectedDailyV1PersonMaterializationReconcilesDynamicOwnerCardinality'
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter 'SimulationRuntimeAdmissionTests.UnityBootstrapDailyRejectsExogenousMaterialFlowBeforeIdentityOrOwnerConstruction'
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter 'SimulationBootstrapCompositionTests.SelectedDailyV1ProfileBootstrapsItsAuthoredP8GeographyBeforeDayOne'
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -All
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter Smoke
```
