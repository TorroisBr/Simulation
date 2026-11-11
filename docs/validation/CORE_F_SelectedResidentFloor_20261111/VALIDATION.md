# CORE-F Selected Resident Floor — Validation

## Scope and source identity

This bounded integration makes the selected Core owner graph authoritative for represented-resident contribution during aggregate demography. Core counts living PersonStore residents and living, selected, unmaterialized NPCs from the exact NPC lifecycle/residence owners. Person-backed NPCs are counted once through PersonStore. The Unity host snapshot excludes selected NPC RuntimeIds and retains PersonStore rows plus unselected compatibility-roster NPCs. The mixed-roster regression proves that an unselected compatibility NPC remains in the remainder.

The candidate was validated over base `914810df3d055dafa1f5e0687ced2f99580a4fd8`. The following source/test file hashes identify the validated working-tree bytes; they must match the eventual candidate commit:

| File | SHA-256 |
|---|---|
| `Assets/_Project/Scripts/Population/DailyDemographicSystem.cs` | `d6e00c5511076ab3227d30388f074743ab46917502f2615117c721db19aa31d7` |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `7e7f368b4e30c5f3ed466ee438986274d1e5c3a0b6be758691b1632a72553617` |
| `Assets/_Project/Tests/EditMode/Editor/Person/PersonResidenceFoundationTests.cs` | `2affddb215730ebd2500f2b51b067283e0fbd9e3f9b2c705218d5e9943a4ce40` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedDemographyStage.cs` | `27c012f626d291d67b9942fec96cc19bc2b3ab73d79fade0f5712b5b02f4aa81` |
| `tests/Simulation.Core.Tests/DailyV1SelectedDemographyStageTests.cs` | `610619e1d2e2012df882d2e6bbd9ce5634de93f558064f169c61b640e09ef9f7` |
| `tests/Simulation.Core.Tests/DailyV1SelectedDailyExecutionStageTests.cs` | `80ac863b19cfbe0133694393c17896fbc967f804dcdef2861d18e0c965047211` |

## Validation results

| Gate | Result | Run |
|---|---:|---|
| Official CoreValidation cold | 1089/1089 passed; 0 failed | `05cde30d65b04a4ba3ce362e99ead869` |
| Official CoreValidation warm | 1089/1089 passed; 0 failed | `05cde30d65b04a4ba3ce362e99ead869` |
| Core selected-demography test class | 3/3 passed in each CoreValidation pass | same run |
| Focused Unity residence snapshot | 13/13 passed; 0 failed | `7d0385bc8dde453f931340bf6ddc85a4` |
| Full Unity EditMode | 3305/3305 passed; 0 failed, skipped, or inconclusive | `bbb6ff43bbfb4cb8b43c215d34249214` |
| Official Unity Smoke filter | 5/5 passed; 0 failed | `dbd08af0221648d6846394f17136da8b` |
| `SimulationRuntimeLongRunTests` | 7/7 passed; 0 failed | `9a9ab57a8fc64968ba0c48d93ca90b1d` |
| `git diff --check` | passed | candidate worktree |

All four Unity runner inventories were stable before and after execution. The full EditMode report contains the Smoke and LongRun test identities. The final full EditMode result was packaged and verified against its protected original report using the approved evidence packager. The manifest SHA-256 is `23b6ac8dc7dcb6739dbc4b7d137d61b0f52a084bae9152c0ba58dbc5cbb1ff31`; the deterministic archive SHA-256 is `c3086c1351d98c5a3072d3d2e403df15268f89fa1a8b81c812cd78ca696a7616`.

CoreValidation's original TRX, raw logs, and owner-only run manifest remain protected in local evidence retention. No public TRX package is claimed. The CoreValidation runner reported `LibraryAbsent=false`; this result is not a claim of .NET host execution independent of the Unity checkout.

Pre-existing local `ProjectSettings/ProjectSettings.asset` and the unrelated prior validation-note edit were left untouched and are excluded from the candidate commit. Unity's before/after input inventory was stable with those existing local edits present.
