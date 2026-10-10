# P12-G restored-boundary coordinator — deferred guard and transition validation

## Candidate identity

- Current P12 canonical base: `3c1575530a1d44c3932767b6e8879be2e6672dc8`.
- Candidate code commit: `15b7822d74188bba62598d49967e3ec7417adfc1`.
- Candidate code Git tree: `556513a868639e1d3abd739dd8715dc745b120b3`.
- Candidate code `Assets` tree: `229edf0c6303ff52219bbb47ef5bd127355ae6a4`.
- Unity Editor: `6000.3.9f1`.
- Working copy uses `core.autocrlf=true`; review and validation identity is the committed Git/Assets tree, not raw worktree line endings.

The implementation defers mutation-guard binding for the private restored-continuation runtime until after the integrated graph checks. It then binds the guard, recaptures the complete owner vector and mutation epoch, compares that exact vector with the one checked, and admits the completed boundary only when unchanged. Normal bootstrap construction retains eager guard binding. Restored continuation admission retains the exact authored ten-NPC bootstrap cardinality check at initial admission while using the source completed-boundary owner vector for later supported NPC roster cardinalities.

The new focused cases cover dynamic NPC roster restore/continuation parity; populated source Expedition rejection; target Expedition exact-zero rejection after the initial target census; roster mutation between graph validation and guard binding; guard-bind conflict atomicity and valid retry; and failure injection after guard binding and after target-vector recapture.

## Validation results

All XML files report `Passed`, with zero failed, skipped, or inconclusive tests. XML SHA-256 and raw log SHA-256 are recorded below. The three Unity logs are retained in [`GapFix4-UnityLogs.zip`](GapFix4-UnityLogs.zip), SHA-256 `1D1071F28EED77FEC9083E76F0A36AD0941A4BF60BF14C84A26518D1CAF5783B`.

| Gate | Result | XML | XML SHA-256 | Raw log SHA-256 |
|---|---:|---|---|---|
| Focused `SimulationRuntimeAdmissionTests` | 105/105 | [`GapFix4-Focused-20261010/EditMode-20261010-151039-3065a6822b1b493a8b594e03308a7ade.xml`](GapFix4-Focused-20261010/EditMode-20261010-151039-3065a6822b1b493a8b594e03308a7ade.xml) | `5DD24C235F66A531A8AAC33A07BCDE247C84D6DDBE535D2FD06CC3C1040A7280` | `8F559400939404C3C0832E7B558E282F0017EAB9B9CE38506461ABDABAD9DD12` |
| ALL EditMode | 2779/2779 | [`GapFix4-AllEditMode-20261010/EditMode-20261010-151231-19724892ca9c464cb46f5711f760ee42.xml`](GapFix4-AllEditMode-20261010/EditMode-20261010-151231-19724892ca9c464cb46f5711f760ee42.xml) | `C980F9B706DE1EF49744CB23F3ECF2B33757674E519EED94EDE682A82EEF809D` | `3DF9B3A0712CC2D260443647AB657347BBF804B69A844012FAF193DF89E0BA25` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 | [`GapFix4-OfficialSmoke-20261010/EditMode-20261010-151314-44fcd33e93e14ecb87a6e619a8361654.xml`](GapFix4-OfficialSmoke-20261010/EditMode-20261010-151314-44fcd33e93e14ecb87a6e619a8361654.xml) | `F64FA734C6B61BC3379BE0E46ABA4ACDE854A93817EED91864BFC786F62905C2` | `7451C8D25E77192BEC19E8DE19FED0E21FCEA7F21ADBE6099F66F249561528CF` |
| `git diff --check` | PASS | `3c157553..15b7822d` | — | — |

Commands used the repository wrapper:

```powershell
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter SimulationRuntimeAdmissionTests -ResultsDirectory docs/validation/P12GRestoredBoundaryCoordinator/GapFix4-Focused-20261010
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -ResultsDirectory docs/validation/P12GRestoredBoundaryCoordinator/GapFix4-AllEditMode-20261010
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter Smoke -ResultsDirectory docs/validation/P12GRestoredBoundaryCoordinator/GapFix4-OfficialSmoke-20261010
```

## Remaining evidence limits

This validation does not complete P12-G. The exact-tip independent review of this code commit is pending; the earlier review applies to the prior code tree only. The broader §6 graph-rejection matrix and all-boundary atomicity evidence remain open, including integrated corruption cases for cross-kind identity collisions, allocator/sequence/token/revision drift, exact P8 geography cardinality/lineage, additional P12-E and definition/provider bindings, and failure injection at owner hydrator and final publication boundaries. The current restore API is in-memory and has no envelope parser, so serialized-envelope parse rejection is not part of this slice.

P12-G remains `WAIT_DEPENDENCY`; P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains `OPEN`. No P12-A export/hydration readiness, capture eligibility, or P13 readiness is claimed. The unrelated P12-E working-copy change, ProjectSettings edits, untracked `.meta` files, and earlier validation artifacts are excluded from this candidate.
