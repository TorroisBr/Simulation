# Selected NPC-only resident-death integration

## Scope

This candidate moves the selected NPC-only resident-death transaction into Simulation.Core. Core prepares and commits the exact NPC lifecycle, residence, action-slot and City population owners. SimulationRuntime retains the existing P12 operation admission, census-section reservations and completion boundary, and uses a Unity projection callback only after Core has committed. PersonStore-backed NPCs continue through the existing selected Person-death operation. Mixed or unselected roster cases remain on the existing compatibility path.

This is an intermediate CORE-F candidate. It does not complete CORE-F, publish DailyV1Application.TryStart, or advance Core canonical.

## Source identity

The candidate code checkpoint is commit 9a21737174527ecf6f0d15623b8740cb71be82f2, tree 98f7607f05b0a5ac8e59dd40504b88d2294035c4. Validation ran against the same source bytes while the worktree was based on d39cac6ee092c3ff97440f047d12669f440db694, tree e204495c7cde97d03c70bec10f4cc6c37c043782. The official runners recorded stable input snapshots. After the source commit, every changed source and test file matched its captured SHA-256.

| Source or test | SHA-256 |
| --- | --- |
| Assets/_Project/Scripts/NpcRuntime.cs | 532e0ce52fa8882240261ca9aac6dc76eab0751782fe104f8b213dfcad3747b1 |
| Assets/_Project/Scripts/SimulationRuntime.cs | e046680c55a47c55820c47dcd7564728ea026aa891f58c824c6b59954c302ec6 |
| Assets/_Project/Tests/EditMode/Editor/P12PopulationLifecycleInvalidationTests.cs | 368cd92b08cc41ed9c6a3c6fb0511f861d940428705d592ed59e159447cffdfd |
| Packages/com.simulation.core/Runtime/Application/DailyV1SelectedDemographyStage.cs | dbef23a1c8c0a978a80f44fac4dedeffd36a874592399d23f77cde355a56aeaa |
| Packages/com.simulation.core/Runtime/Application/DailyV1SelectedPopulationChangeOperation.cs | 38ddb738ba43caec1aaef41d988f24afcbd9afa05f27a633d9aa1508aecee627 |
| Packages/com.simulation.core/Runtime/Person/DailyV1NpcRuntimeOwners.cs | edb6f072592abcf0274a5172ed2a6df060ab68dfea29b882587be26e5c9b0ea6 |
| tests/Simulation.Core.Tests/DailyV1SelectedDemographyStageTests.cs | 3dbccd2256ae208c6062bd423a69ce265c153bb4fbe4ab34198adb9a9e5e4b0d |

CoreValidation run 4efae0593c47460fb67b6acbbfa2eed0 reported stable input digest 3f82541493b1953e3c84bab4587fab478179857b66c513f75751cde536d88148. The changed Core test file matches that run manifest. The original cold and warm TRX reports and full private runner manifest remain in protected local retention; they are not copied into the repository.

## Validation

| Gate | Result |
| --- | --- |
| Official CoreValidation cold | 1094/1094 passed |
| Official CoreValidation warm | 1094/1094 passed |
| Changed Core operation tests | Included in both complete CoreValidation runs |
| Focused Unity integration test | 1/1 passed |
| P12 population lifecycle suite | 30/30 passed |
| Full Unity EditMode | 3313/3313 passed |
| Smoke filter | 5/5 passed |
| git diff --check | Passed |

The Smoke-filtered five cases and the new P12 integration test are also present and passing in the complete 3313-case EditMode package. An initial category-filter attempt selected zero tests and is excluded from these results. A standalone package of filtered Unity XML was not produced because the approved packager correctly rejected that filtered XML's inconsistent parent-suite descendant counters; the full EditMode package is the published Unity evidence and contains the relevant cases.

## Published Unity evidence

The official evidence packager produced and verified the full EditMode bundle:

- Manifest: selected-npc-resident-death-editmode.manifest.json
- Manifest SHA-256: 3d867c681a7fd0238a40a4a5bc122a3a2349ca23d1f7fa087529066203c90cfc
- Archive: selected-npc-resident-death-editmode.zip
- Archive SHA-256: 4583e37be0a21b2dd40bce884c4928ccb510a6fc6e7405d53c061637a243ff88
- Archive size: 175448 bytes
- Verification: PASS; one run, one normalized XML entry

The package's public manifest contains no absolute workstation drive paths. The complete original Unity XML and raw log remain protected locally; raw logs are not included in the archive. The CoreValidation TRX reports and manifest likewise remain protected locally.

## Review and promotion

Independent exact-tip review is pending for this candidate. No Core canonical promotion is claimed. The selected resident-death change remains only an intermediate CORE-F integration checkpoint.