# CORE-F selected NPC-only resident death — review corrections

## Scope

This candidate corrects two findings from the independent review of the selected NPC-only resident-death transaction. The bounded operation remains limited to a resident NPC that is not backed by a `PersonStore` Person. It does not change P12 census coverage, operation identity, mixed-roster fallback, or CORE-F completion status.

- A persisted `PersonId` on an NPC is rejected, even when `BoundPersonRuntime` is null. This prevents an unbound NPC from aliasing a live `PersonStore` record or carrying an unresolved persisted identity through an NPC-only death path.
- The Unity lifecycle notification now runs only after the combined NPC-owner and City population transaction succeeds. The callback observes the committed population and revision.
- Failed Unity admission clears the proposed transition and leaves the P12 epoch and authoritative owners unchanged.

## Source identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`.
- Previous candidate: `facfc86984153f3da3a1cdb0b37f5415e31d509e`.
- Implementation commit: `7271fe39d0a06171e3e9876f39bb19cadb9d5202`.
- Implementation tree: `bd58ccf5eef299460e9e5cf0d52b2d8051dfb318`.
- Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

| File | SHA-256 |
| --- | --- |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `a57fe87ce1341a0cd1a0ff1c4d1a185c01f4e1134e51e38e9f6d6fde7068c19f` |
| `Assets/_Project/Tests/EditMode/Editor/P12PopulationLifecycleInvalidationTests.cs` | `e2fa88a0d5a4fc732094eae2430849c4663729cc48fb98b5838ccb22658fdd4d` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedDemographyStage.cs` | `9eba6c3fef27b0dde4f77a918913208cc065e3d7fe5e6fecb7200b84ab3f8a57` |
| `tests/Simulation.Core.Tests/DailyV1SelectedDemographyStageTests.cs` | `ad3c9a42094f633fe940242a429850ed1a78df418653b65d0f22ab24723a0c73` |

The three Unity source/test hashes match the full EditMode run's stable source snapshot. The .NET source/test input was covered by the official CoreValidation run below. The code commit contains exactly these four files.

## Validation

- Official CoreValidation run `093be806a32e433abe467ad55a59ef7b`: cold 1,095/1,095 and warm 1,095/1,095 passed. The official manifest reports stable inputs and digest `fefbde2b9834ff5af3b9b2ab8add0ba32b7b98a8119b3ab9db2d75266d31b775`. Original .NET reports and the runner manifest remain in protected private evidence storage and are not copied into this public record.
- Focused Unity alias regression `CoreSelectedNpcOnlyResidentDeathRejectsUnboundPersonStoreAliasWithoutMutation`: 1/1 passed, run `4d7f09fefac04ed584a6e214ac646bea`.
- Official Unity `P12PopulationLifecycleInvalidationTests`: 31/31 passed, run `e2540e900518467594942a2af1293ec7`.
- Full official Unity EditMode: 3,314/3,314 passed, run `977a21a5f4bc4232a598b4e375883e02`; stable input snapshot.
- Official Smoke filter: 5/5 passed, run `edd4fb6f16404d69a4d78c0c74836e82`. This is the `Smoke` test filter; a separate category-filter attempt that selected zero tests is not counted. The five Smoke cases also occur in the full EditMode result.
- `git diff --check`: PASS.

The verified full EditMode result package is in `Unity/selected-npc-resident-death-review-fix-editmode.zip`. Its SHA-256 is `3f51ccc4087e2bb136077c513baa4410598c57a360f9bcb3aa215902fc144c09` (175,850 bytes). The package manifest is `Unity/selected-npc-resident-death-review-fix-editmode.manifest.json`, SHA-256 `e2999986b530b1a03cc11a7f91cfddf9c81dc5702689f5dd21eee75e7864d561`. The package contains the complete normalized full-suite XML; no filtered subset was packaged.

## Review and status

The corrected candidate requires a new independent exact-tip review. No Core-canonical promotion is performed here. CORE-F remains incomplete.
