# P16-A current-base revalidation — 2026-10-04

This validation covers the recomposed P16-A candidate commit `98f80648a226212cd13c37bce34d0e2d6c68574a`, tree `1abb2f83817659fa9bce8e66826f79fde34765b8`, using Unity `6000.3.9f1`.

## Recomposition classification

The P16-A candidate was based on P15 canonical `5054211ad883d14fc6727416c313f1f1824679f4` and P12 canonical `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e`. P14 canonical advanced to `06e9c30101a74bd618d3651885c489c79fe866bb`, which includes P14-B and P10-B. The branches shared merge base `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e` and merged without conflict. P14/P10 and P16 production changes are disjoint; their shared `SimulationBootstrapCompositionTests.cs` changes merged automatically.

Classification: **BASE_DRIFT_ONLY**. The combined P14 City/P10 Ruin proving profile remains explicitly deferred and rejects during resolve-profile before world identity, owner construction, or publication. No City Location is created by P10, and P8's one-owner-per-Location rule remains intact.

## Results

Every suite passed with zero failed, skipped, or inconclusive tests:

| Suite | Result | XML SHA-256 | Unity log SHA-256 |
|---|---:|---|---|
| `P16AMilitaryMovementTests` | 20/20 | `FAE202CD024AA1FC1C5AA1B9BD29411AB28C60E9D8FA12232EAF62F2993EDDD5` | `596B9CF3A62FE8D310AB51DB498314DCD5BB58BD0E4291D0F4A9ED48BE6A5A06` |
| `SimulationRuntimeAdmissionTests` | 37/37 | `9120F5C3D274AE9B12448F109A406F1827F957CE943DF7523B3EAEDD480CD956` | `0E3471B0DD2FBD97BA87DE1818408EEDB0182053A32C926E150BE6C2612D3AD1` |
| `SimulationBootstrapCompositionTests` | 21/21 | `B23C2C02DA2482FFB96F3CE169E457B441A179E59F6633089C577BCE50BE0E6E` | `4774E8392419C80DA285EFA72019487FB164D4003420FAA480AD645D73C13339` |
| ALL EditMode | 2323/2323 | `2D94F1DFA9DF04D2B25A6052991C9F851DD4C1900033FAA88593D09E7CCC3356` | `C75AEFE15906FEC9AAF3018FDA6CC48D34CEC30619A264A89F3C5FD98142353D` |
| Official Smoke | 5/5 | `9CBECA1E567B5A88A72202D38D7F43E9EB27CB63019439002FF41F7EF459C8FB` | `151ED0349B1C0E618069089AFA5D2FB888C0E8B2768FCE246E492785BCC1C4E5` |

`git diff --check` passed for the recomposition merge. Raw XML and Unity logs are archived byte-for-byte in `P16A-current-base-revalidation-1abb2f8.zip`; archive SHA-256 is `5C7C6D878EB880A5879A558AD52370144C782413F05E8F3904097E6FCDD09BFE`.

The existing `ProjectSettings` edits remain unstaged. The two untracked `.meta` files remain untracked and are excluded from the validation archive and candidate commit. This validation does not claim P12-B completion, P12-A readiness, P13 readiness, complete owner/shared-epoch coverage, capture eligibility, export/hydration, or Phase 16 closure.
