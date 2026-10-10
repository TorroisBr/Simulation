# P12-G populated omitted noncausal read-model restore — exact-tree validation

## Candidate identity

- P12 canonical base: `277020140be373378c04f6e835f35534f95dd8d2`.
- Test code commit: `111c6b43bb6a640dad889dbc3088b5aae1dca1ca`.
- Test-code Git tree: `8d6c72bf7418c8091ea31c5f76c8c06c341b8073`.
- Validated `Assets` tree: `6f6946c4e25bfa876f48c5616e46a5e74892ff35`.
- Changed code path: `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` only.
- Unity Editor: `6000.3.9f1`; runner: `Tools/UnityValidation/Invoke-UnityValidation.ps1`.

## Bounded evidence

`DailyV1RestoreStagesFreshGraphPublishesOnceAndContinuesDeterministically` now requires the completed source boundary to contain populated `NpcDecisionStore` rows. If the authored first day emitted no `DomainEventStore` row, the fixture adds one through the public omitted read-model store after capturing the completed-boundary token. It uses an existing actor/location and a positive event sequence; it does not call `DomainEventRecorder` or allocate from the authoritative `SimulationRecordSequence`.

The test verifies that the completed token remains valid and allocator/shared-sequence/lineage/random continuation roots remain unchanged. After restore, the target receives fresh, empty `NpcDecisionStore` and `DomainEventStore` instances; both populated source stores retain their original rows. The existing assertions still compare the complete included C-F owner projection and continuation roots against an uninterrupted control across two subsequent boundaries. This proves the permitted populated `OmittedNonCausalReadModel` case without adding payload, hydration, production behavior, or scope.

## Validation

Every XML reports `result="Passed"` and zero failures, skips, or inconclusive tests. Compressed logs were decompressed and their raw SHA-256 values rechecked.

| Gate | Result | XML | XML SHA-256 | Raw log SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|---|---|
| Focused `DailyV1RestoreStagesFreshGraphPublishesOnceAndContinuesDeterministically` | 1/1 | `EditMode-20261010-183429-a4345bb9eaca4f7080b04ce7d8ba0649.xml` | `5DBBA4809692F25354152CB938D77FC3147B1A28AC2F020ADB4970A565C97B5D` | `A84E95E9396AF64C07E9DB30271109243F9D8DB0563C322C04FC0EBBCAC6A165` | `FD7394F59FAC7C72A9D23FA87FEEFCD5E7802996F2168B75578BB51E40FA0592` |
| ALL EditMode | 2803/2803 | `EditMode-20261010-183439-061023409d3d4dafb856e14596102207.xml` | `2B3D9813BC5A1ED5A92E160DA59DF0BAE1047EB039DF285FB46478E03CF6CCF8` | `97B30AE906A0F47C3A2B24131580459EDA58FE32F27A4907D04161A2E64A8BCE` | `6EE728A3707BDD8488468F7AD4F0AC5525D0EEDD0B8564A99C37EB95D0D17D59` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 | `EditMode-20261010-183519-79dcb569283845c8a445a5b7f7090747.xml` | `CAB3C2BB213865E1E89E53CF002C6422139C0A1CFE358505CAC09BD6E7A1C0C9` | `01127A337498240743C73BDBBA4AE484CA0B6EFDA3C2732B5A343FE2C25532C7` | `840C03A29E22E0824DF14B4735FEC2F4B00CC2CE731BC768F9B765A69B1FD16C` |
| `git diff --check` | PASS | `2770201..111c6b4` | — | — | — |

Before and after each Unity run, the hashes of the pre-existing user-edited `ProjectSettings/EditorBuildSettings.asset` and `ProjectSettings/ShaderGraphSettings.asset` matched; no unrelated `.meta` file was staged or changed.

## Status boundary

This candidate closes only the §6.1 populated-omitted-read-model evidence row. P12-G remains `WAIT_DEPENDENCY`; P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains `OPEN`. It does not establish complete owner/epoch coverage, P12-G completion, capture eligibility, export/hydration, downstream readiness, or Phase closure.
