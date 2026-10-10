# P12-G restored graph rejection coverage — validation record

## Candidate identity

- P12 canonical base: `3c1575530a1d44c3932767b6e8879be2e6672dc8`.
- Prior integrated restore candidate: `33f61e571ac0eaf5740fd9beb282e5d179c47a4f`.
- Test-only code commit: `92206b206db15001f4abdcf4531d57cde227078e`.
- Candidate Git tree: `d2892fdd214a875559574918115ad3b8a571d7f7`.
- Candidate `Assets` tree: `494e93d25a95b5415e7a7881cd7c8af5ba3a7bf8`.
- Changed executable path: `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` (blob `089ad370dee7609e130c46fa6d473bae9f03bf1f`). Production code is unchanged from the prior candidate.
- Unity Editor: `6000.3.9f1`; validation used `Tools/UnityValidation/Invoke-UnityValidation.ps1`.

## Added rejection evidence

Four integrated corruption cases use the private restored target and verify rejection before publication for:

- allocator NPC next/high-water regression;
- shared `SimulationRecordSequence` regression;
- per-NPC `MoneyAccountRuntime` revision drift;
- removal of the selected P8-A Location from the target identity registry.

Each case confirms the original active-session reference remains installed, its completed-boundary token and mutation health remain valid, its owner projection is unchanged, a subsequent daily advance matches an uninterrupted control, and a later valid restore retry reproduces the same owner projection.

## Validation results

All result XMLs report `Passed`, with zero failed, skipped, or inconclusive tests. SHA-256 hashes are recorded for both XML and raw log.

| Gate | Result | XML | XML SHA-256 | Raw log SHA-256 |
|---|---:|---|---|---|
| Focused `SimulationRuntimeAdmissionTests` | 109/109 | [`Focused/EditMode-20261010-152310-28a3547c263b4bceb5960a5b56321870.xml`](Focused/EditMode-20261010-152310-28a3547c263b4bceb5960a5b56321870.xml) | `3335E5145A34AB22A1E4ECF47679D0D2B24B7CAD16430AF3DC925240EBC80504` | `6DE77FCA6B930D74CEF8079B7196074DF949C32A9EA5E09E928A7875511B0847` |
| ALL EditMode | 2783/2783 | [`AllEditMode/EditMode-20261010-152514-74238a59356b47cfb1aa421a135f7f3d.xml`](AllEditMode/EditMode-20261010-152514-74238a59356b47cfb1aa421a135f7f3d.xml) | `B0BF8018A20184B9944BE1A4D6C172DD5B50B193ED2A2532ECC966D2876E7214` | `B649D845F04380A343371BE86023B16C6D29FE66B275C0F2D25B7A66592ADF62` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 | [`OfficialSmoke/EditMode-20261010-152556-e1794b84a0694f56a7b61edcf6c6d5d5.xml`](OfficialSmoke/EditMode-20261010-152556-e1794b84a0694f56a7b61edcf6c6d5d5.xml) | `526A254545C7A37C41ABE31C118381B619EFDD8C10B079529B617E744526E544` | `D3FC5AB42C533B244BF0DAD8C52C856CE59D54438F60B9017C2A69CD6841063E` |
| `git diff --check` | PASS | `33f61e5..92206b2` | — | — |

Commands:

```powershell
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter SimulationRuntimeAdmissionTests -ResultsDirectory docs/validation/P12GGraphRejectionCoverage/Focused
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -All -ResultsDirectory docs/validation/P12GGraphRejectionCoverage/AllEditMode
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter Smoke -ResultsDirectory docs/validation/P12GGraphRejectionCoverage/OfficialSmoke
```

## Evidence limits and status

This test-only slice does not complete P12-G. It does not yet prove the full §6 rejection matrix: cross-kind identity collisions, token/snapshot inconsistencies, all definition/provider/cross-section bindings, and exact P8 lineage/cardinality combinations remain to be covered. The current in-memory coordinator has no serialized-envelope parser. Owner-hydrator and final publication failure injection, broader staged-graph atomicity, and the full multi-boundary deterministic parity matrix also remain outstanding. Existing tests cover the bounded cases recorded in the prior coordinator validation manifest; this record does not enlarge those claims.

The new exact code tip still needs independent review. P12-G remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains `OPEN`. This does not claim capture eligibility, persistence-envelope support, full P12-A export/hydration readiness, or P13 readiness. Protected ProjectSettings changes, unrelated `.meta` files, the P12-E working-copy change, and the earlier candidate’s untracked validation artifacts are excluded.