# CORE-F runtime boundary recovery validation — 2026-10-19

Status: **intermediate recovery checkpoint only**. This record does not claim independent review, Unity validation, package completion, or Core canonical promotion.

## Source identity

- Candidate base: `645103573363895db353092ed72aa0ac9a5d1683`.
- Validator working-tree input digest: `9b4d42f5af16ae97b5efbbb242bbd064f06d4747d94fc6c1fcf1b630b42465c3`.
- Executable/test/meta manifest SHA-256: `c8876c3bf2da864408121d6ef91008a8dc6ff84848610d223e513b8280bc0397`.
- Manifest is the UTF-8 SHA-256 of newline-terminated lowercase SHA-256/file-path rows for the twelve files listed below.

| SHA-256 | Repository path |
|---|---|
| `1036f0e4f95cb64f46f7e72e4b8192d7421aec338a9d25702bbe42148d0f89ca` | `Assets/_Project/Scripts/SimulationRuntime.P18D.cs` |
| `ff96173c58a88fbde349c93549987183f5a0a1811f58fede91bddbb774c2670e` | `Assets/_Project/Scripts/SimulationRuntime.cs` |
| `43e400898bc20e239df977f10da4dec983ca19f9e3a688457e76f4e1eb498c6e` | `Assets/_Project/Scripts/TravelParty.cs` |
| `4390ac53a9abd0f49fee78d767d71cb6b0398a4a08b86b365e55b33504446a15` | `Assets/_Project/Scripts/TravelSystem.cs` |
| `de3a26083369748411cc27516f8e8551ca9dee6c6d246922cbb47bd12c86aed3` | `Assets/_Project/Scripts/SimulationRuntime.DailyV1TravelStage.cs` |
| `2bb5ae323940277dd83891e3a9d757d8d3422bfbb1e06bc02aaef4c146c6e309` | `Assets/_Project/Scripts/SimulationRuntime.DailyV1TravelStage.cs.meta` |
| `d0a9f8ee981191f7d369c9dd4c1dcc8a175899adcefdedf0b2704686a4002530` | `Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs` |
| `262cae8c857005894b096e56ecbf9797435f7a2af5a14f1669a17363fb3e6597` | `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedTravelDayStage.cs` |
| `0777f9c417aac5ee30dde7e6df46018b426d14bdff20729226d6c50ee15494a8` | `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedTravelDayStage.cs.meta` |
| `83b7ab55a28824ffb42f81f6af278f1a321417b053c22407c6af81a1b1fe599b` | `src/Simulation.Core/Simulation.Core.csproj` |
| `1bf22afe4506fdd3ea268434600123b3ed259bdfb13c9ee09f1cc31e772294a9` | `tests/Simulation.Core.Tests/DailyV1ExecutionBoundaryStateTests.cs` |
| `8447a52a077524e6327c5df0bbda78867e7baee3fbb32a3b68b5211f1afa35fb` | `tests/Simulation.Core.Tests/DailyV1SelectedRuntimeOwnerGraphTests.cs` |

## Validation

- Focused `SelectedTravelDayStageRunsPartyOperationBeforeSoloTravel`: 1/1 PASS after correcting its fixture to model the following day by clearing the party member's existing `TravelStartedToday` state before stage execution.
- Supported `Tools/CoreValidation/Invoke-CoreValidation.ps1 -SkipLab`: PASS; cold 372/372 and warm 372/372; SDK 9.0.315; exit code 0; run identity `cc2bbc3a04e342f29097b8e144564320`.
- The preceding CoreValidation run `8f8f182995a9469cb4a189a7482e4913` is retained unchanged and classified as a test-fixture failure (371/372); it exposed the same-day travel-start rule described above.
- The validator reported that this checkout contains a Unity `Library`; this is not evidence of Library-independent .NET execution.
- `git diff --check`: PASS apart from pre-existing line-ending warnings on protected `ProjectSettings` files, which are excluded from this candidate.
- Unity EditMode/Smoke were not run in this recovery checkpoint.
- Original TRX/log artifacts remain in the protected local evidence store pending privacy review; they are not included in this repository record or candidate commit.

## Scope boundary

This checkpoint contains the selected Core travel-stage and advance-lease work plus the P18-D lease type compatibility update. It is a recovery milestone only. It does not deliver the ordered Daily-v1 runtime, actor/action stage, `DailyV1Application`, detached restoration, or .NET host, and carries no review or promotion verdict.
