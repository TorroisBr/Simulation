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
