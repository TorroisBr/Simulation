# P12-G Crime/Social runtime-operation evidence

**Status:** targeted selected-profile ingress evidence passed; P12-G remains `WAIT_DEPENDENCY`.

**Canonical base:** `bc9ab6a27d5bb5c1e2de0987aa6377db6a96c37e`

**Scope:** test-only proof that the existing authored Daily-v1 Steal path publishes the Crime/Social composite while the registered `runtime.advance-day` operation is active. No production source, operation ID, or gameplay behavior changed.

Unity Editor: `6000.3.9f1`. All result XMLs report coherent counts with zero failures, skipped tests, or inconclusive tests.

| Gate | Result | XML SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|
| `P12CrimeSocialAppraisalInvalidationTests` | 12/12 PASS | `57C12D127A8AFB1BE057E122D87EADECDA2BE2BE9C6067C7CB7B9125429F430B` | `7B15CA7841A47F5B49F70EC0483B64A723FDDC66B69455EF75085B82F4799978` |
| ALL EditMode | 2733/2733 PASS | `254BCBD79B8B9BE42030637D5E83457F46F241D53824ABD7758BBC60A3C5A66F` | `DFEA6B85F6455D1C775ED45938F2281D1B7A8FD75472FB5782E9AE07068B3561` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 PASS | `C45197858ED1D4FF06E246F69D2A6242D1B05A2686CEACB2F59C4C7C826C2D49` | `7F4672B5281D516C4773CD2FCB31E40FC30F8DEC7706BD12282BD8C76B2B6CA2` |

The exact XML and `.log.gz` artifacts are retained beside this manifest under `Focused`, `AllEditMode`, and `OfficialSmoke`. Commands used:

```powershell
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter P12CrimeSocialAppraisalInvalidationTests -ResultsDirectory docs/validation/P12GCrimeSocialRuntimeOperation/Focused
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -All -ResultsDirectory docs/validation/P12GCrimeSocialRuntimeOperation/AllEditMode
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter Smoke -ResultsDirectory docs/validation/P12GCrimeSocialRuntimeOperation/OfficialSmoke
git diff --check
```

Intermediate diagnostic failures in `Focused/` are not counted as passing evidence; the table identifies the final successful runs. The unrelated `ProjectSettings/EditorBuildSettings.asset`, `ProjectSettings/ShaderGraphSettings.asset`, and three untracked `.meta` files were not staged or changed.

This evidence closes one runtime-ingress witness only. It does not complete the P12-G owner/commit matrix, prove all direct writers or shared-epoch coverage, establish global quiescence, make P12-G implementation-ready, make P12-A ready, unblock P13, or close Phase 12.

## Exact composite epoch follow-up — 2026-10-09

**Code candidate:** `33ce3026297a372363624065d4f3240c9b49b44c`
**Base:** P12 canonical `01e1008f204ca96bb21c799f1ae8b8e46a5f85f0`
**Assets tree:** `cffa2ef1f4e6958cda4bc00f918b77b246678095`

The selected Daily-v1 Steal ingress probe now reads the shared mutation epoch immediately before and after the complete `TryAcceptTheftOutcome` composite returns. It asserts that the Crime/Social composite advances the shared epoch exactly once while the `runtime.advance-day` operation is active. This isolates the composite's contribution from other valid owner changes during the full day advance. The test-only change adds no production behavior or gameplay semantics.

Unity Editor `6000.3.9f1` validation for this exact Assets tree:

| Gate | Result | XML SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|
| `P12CrimeSocialAppraisalInvalidationTests` | 12/12 PASS | `8E4B7941A2C9B77DE19DCEC78EFF862A3C466C253FB039F33441016E4D85CDCF` | `693374891BA7E6B0B6A68F8E48F9A7D98887F326EE9EB219DD2D39B1003B8425` |
| ALL EditMode | 2733/2733 PASS | `EEAE31737B6BFC9081C73BD88D92ED7749C9797AB5946A9B6AFE5E403CEFEC8D` | `41601601E87E5F31F3ADAC51BDCDB49E5379380F2D4CC55D0A2C90AECF2233AC` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 PASS | `D28412B671D3626BAC7FEDBA14009E4AEC4A6E98F9153C51EB93D60360A04420` | `18E6C13B8EF1D4E8F44E6067B3D457B1B90B254DE82FE7A5333AEE4B37855C36` |

The exact XML and `.log.gz` files are stored in the corresponding `Focused`, `AllEditMode`, and `OfficialSmoke` directories. `git diff --check 01e1008..33ce302` passed. The existing ProjectSettings edits and three untracked `.meta` files were present but not staged or changed.

This closes the exact Crime/Social composite epoch witness for the selected Daily-v1 Steal ingress. It does not prove the full `runtime.advance-day` epoch delta, exhaustive owner/shared-epoch coverage, P12-G readiness, P12-A readiness, P13 readiness, or Phase 12 closure.
