# CORE-F selected travel owner/P12 boundary — incremental validation

This record binds the bounded selected travel integration to its exact source inputs. It is incremental candidate evidence, not full CORE-F package validation or canonical approval.

## Source identity

- Candidate branch: `codex/core/CORE-F-runtime-boundary-3ec0aa`
- Base commit: `3ec0aa126f95a034f219c4dca3accc82ac3c8dc9`
- Unity validation source snapshot SHA-256 (before and after every listed Unity run): `400cefffdf72a886a7cfd4f8f5951372b24e9764a74549ef6cd0d9c7a3607c8a`
- The following selected source/test files were the implementation inputs. Their SHA-256 hashes identify the candidate files used for this record:

| SHA-256 | Repository path |
| --- | --- |
| `5a1d3842e1d83007180e2c77b42e328db1aec97fd8763daa155415d10558baeb` | `Assets/_Project/Scripts/CityRuntime.cs` |
| `2c25909df96542e1e582230d8c12af7f2855b0089cb43c84cfc6147314cd8bd2` | `Assets/_Project/Scripts/NpcRuntime.cs` |
| `a6a260dac0ccf728648e797f4c5e794c56bf2cb1008104c5741a2d50e967829e` | `Assets/_Project/Scripts/SimulationRuntime.cs` |
| `c40e37053d789983a48178f8907e7ae8349c6adb1513082fcaec5c980df21149` | `Assets/_Project/Scripts/TravelSystem.cs` |
| `8a0b36718a09f19ec573ba7cae8f85511088e2552026f2d4324a740462999a94` | `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` |
| `a3e0458f17363d1cc16df1cc568e87a8def28235e968c996a63be0cb48e4da96` | `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedRuntimeOwnerGraph.cs` |
| `63e4d36d1877ff6c5c8e9c9292f5a947cfcb51678fb132c66177a1dfdbdecfa2` | `Packages/com.simulation.core/Runtime/City/DailyV1CityRuntimeOwners.cs` |
| `940cb9d5ea695b7d4a19356941a5e31f56576a83e36f75c6ea49350cab52008f` | `Packages/com.simulation.core/Runtime/City/DailyV1SelectedTravelTransitionPlan.cs` |
| `21a9baf4a9109138a3df53def65c0687ee2c5b628c5ac1311d6833275dfe2bf3` | `tests/Simulation.Core.Tests/DailyV1SelectedRuntimeOwnerGraphTests.cs` |

## Results

Unity used Unity `6000.3.9f1`, sequentially in the isolated candidate worktree with its local package cache. `inputStable=True` for each listed Unity invocation. Raw original XML and logs are retained unchanged in the private validation evidence store; they are not included in this repository record or candidate payload.

| Suite | Result | Run ID | XML SHA-256 | Log SHA-256 |
| --- | ---: | --- | --- | --- |
| Selected travel arrival regression | 1/1 PASS | `d3934becd8a74ea698951df93c9f9643` | `11e9c13440a35ad4a9ed33a29e67aaa2c2fd5cd396a4c14e5ed3cd9e6b20480b` | `ee5e828267a99d313f3894e13e694401b4ece2939985dcb34b343627c3640c52` |
| `SimulationRuntimeAdmissionTests` | 208/208 PASS | `03e3c65b90064cf889968629e320c329` | `92206ec3f0743765f87dc1043a279affaca81225e29afc5e68693b91e4d3d0b1` | `097ec5a6e0613725ec387d341a6a261172b7518f203ec8b0637f2bdc5a782a73` |
| `SimulationBootstrapCompositionTests` | 38/38 PASS | `2e06ff66fa0a448aa071f3a7acf4c8ba` | `7e2b0170c53540c296daf3c123a3949f49aa129b0d05e314048d00a09ffbb960` | `a07c6caffbffd82b906ea2299357277adb5cd6e249ce0639e8adc1d19f745eb0` |
| P12 solo travel start | 11/11 PASS | `becf3b717a1c40059bd8e83515bb7e8c` | `c8cda3252d893a4950c02cfb8dc0362b2223ec5e81a0f73bae436a329bde1a01` | `13229f80f8210d0217b3ae63270644553e0c2b3493253a216e8a1a22dac733fa` |
| P12 TravelParty advance | 10/10 PASS | `e75cf0f4cc1b4528bb3dc3f4ad472093` | `f0f713bf8ba1c42942af74f0bdfba0fd6a22cff1fcf18e56ac9fc0c0f07168ae` | `41ad8c1f9e342953149284f30756c4bad95cee351d40f69db13cafb2059d92e3` |
| Runtime orchestration | 12/12 PASS | `c1e0c1be02884c138f34e0ba1334e562` | `8bcc4eafcb64574751ecf09189324cf07bbe80b582d43185a0b137844b4c359e` | `36e6b434fba0cbdcbfb78460856802134a1e2341fd0823cfc16e08fad78db29e` |
| Generalized spatial travel | 19/19 PASS | `875a00001cb04a109d2b9a5024e34485` | `21a4a3e4b00523f25c3750d664842c003e66ad3e863897b04e9b40c57cf101f7` | `49251e1e08ddd32bf773ad298b7accb10fa47e43e550b8aa951f718fbd869701` |
| GroupTravel | 32/32 PASS | `77aa9d939d0b4663a230f2a4822daa14` | `0012a3b5f45606248788103c0b86542ceb8606bf972812f0ac3dff08dc75d3ce` | `b8a308f93b2ff5ea24fc70d1ec226fa98c1722d27322a213872dcbcba52677ff` |

The focused .NET command was `dotnet test tests/Simulation.Core.Tests/Simulation.Core.Tests.csproj --no-restore --filter FullyQualifiedName~DailyV1SelectedRuntimeOwnerGraphTests`; result: 7/7 PASS. Unity filters are partial-suite reports and do not constitute the complete XML package required by the evidence packager. They remain preserved as private originals and were not edited, normalized, or represented as a complete evidence package.

## Failure classification and limits

An earlier admission run reported 207/208 because the newly added test fixture selected two travelers whose destinations differed. This was a fixture-selection defect, not a production failure. The original failing XML/log remain preserved unchanged. The fixture now deterministically selects two eligible NPCs from one source City and uses the other authored City as their common destination; the exact-source rerun is 208/208.

`git diff --check` passed. These results validate the selected action/travel owner boundary and its focused regressions only. They do not validate the full EditMode suite, official Smoke, the shared ordered daily runner, complete admission/lease/token ownership, the detached host, P12 restore integration, or CORE-F completion. Canonical promotion was not performed.
