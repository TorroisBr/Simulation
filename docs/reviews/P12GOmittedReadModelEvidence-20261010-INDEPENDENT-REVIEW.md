# Independent exact-tip review — P12-G populated omitted read models

- Verdict: **VALIDATED_CANDIDATE**
- Candidate branch: `codex/phase12/P12GOmittedReadModelEvidence`
- Candidate tip: `a672b5cbe2a705da50ae1e586014cd56a8f36bfd`
- Base / canonical at review: `277020140be373378c04f6e835f35534f95dd8d2`
- Code commit: `111c6b43bb6a640dad889dbc3088b5aae1dca1ca`
- Validated Assets tree: `6f6946c4e25bfa876f48c5616e46a5e74892ff35`

## Findings

The candidate is exactly two commits ahead of the stated base; the remote branch resolves to the requested tip. Its only code change is 43 added lines in `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`. The remaining changes are the validation manifest and its focused, ALL EditMode, and Smoke XML/log artifacts. There are no production code, ProjectSettings, or .meta changes.

The added proof extends `DailyV1RestoreStagesFreshGraphPublishesOnceAndContinuesDeterministically`:

- Confirms the completed source has populated `NpcDecisionStore` rows.
- If the source `DomainEventStore` is empty, adds a concrete `NpcArrivedEvent` directly to that omitted read model using an existing actor/location and positive sequence. It avoids `DomainEventRecorder` and the authoritative `SimulationRecordSequence`.
- Checks the completed-boundary token remains valid and allocator/shared-sequence/lineage/random continuation-root facts are unchanged after seeding.
- Confirms restoration publishes fresh, empty target decision/event stores while source rows/counts are retained.
- Confirms restored day and fresh restored-boundary token, complete included C–F owner projection, selected facts and continuation roots. The existing test then continues source, restored target and uninterrupted control across two more boundaries and compares deterministic facts/graphs.

This addresses the bounded §6.1 populated-omitted-noncausal-read-model proof described as outstanding in the base State. It does not claim that staging invokes no P9 genesis/gameplay callbacks beyond the assertions described, nor does it broaden payload, hydration or production behavior.

## Validation

The exact-tree manifest is `docs/validation/P12GGraphRejectionCoverage/OmittedReadModels-20261010-111c6b4/P12GOmittedReadModels-20261010-VALIDATION.md`. It binds code commit `111c6b43bb6a640dad889dbc3088b5aae1dca1ca` and Assets tree `6f6946c4e25bfa876f48c5616e46a5e74892ff35`.

| Gate | Manifest result | XML SHA-256 | Raw log SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|---|
| Focused restore test | 1/1 | `5DBBA4809692F25354152CB938D77FC3147B1A28AC2F020ADB4970A565C97B5D` | `A84E95E9396AF64C07E9DB30271109243F9D8DB0563C322C04FC0EBBCAC6A165` | `FD7394F59FAC7C72A9D23FA87FEEFCD5E7802996F2168B75578BB51E40FA0592` |
| ALL EditMode | 2803/2803 | `2B3D9813BC5A1ED5A92E160DA59DF0BAE1047EB039DF285FB46478E03CF6CCF8` | `97B30AE906A0F47C3A2B24131580459EDA58FE32F27A4907D04161A2E64A8BCE` | `6EE728A3707BDD8488468F7AD4F0AC5525D0EEDD0B8564A99C37EB95D0D17D59` |
| Official Smoke | 5/5 | `CAB3C2BB213865E1E89E53CF002C6422139C0A1CFE358505CAC09BD6E7A1C0C9` | `01127A337498240743C73BDBBA4AE484CA0B6EFDA3C2732B5A343FE2C25532C7` | `840C03A29E22E0824DF14B4735FEC2F4B00CC2CE731BC768F9B765A69B1FD16C` |
| `git diff --check` | PASS | `2770201..111c6b4` | — | — |

Focused and Smoke XML headers were inspected and report the listed passed counts with zero failed, skipped, or inconclusive tests. The connector returned no content for the large ALL EditMode XML range; its SHA-256 and outcome are therefore retained as manifest-reported evidence rather than independently recomputed in this review. The manifest records protected ProjectSettings hashes unchanged before/after Unity validation and no unrelated .meta files staged or changed.

## Status boundary

This increment closes only the populated omitted noncausal read-model evidence row. P12-G remains `WAIT_DEPENDENCY`; P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains `OPEN`. No complete owner/epoch coverage, P12-G completion, capture eligibility, export/hydration, downstream readiness, or Phase closure is implied.
