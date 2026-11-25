# Selected-runtime advance authority forwarding

**Status:** validated bounded candidate milestone; not Core-canonical and not CORE-F completion.

## Candidate identity

- Code candidate: `e6edb70d6fdc04d5254a973d20bec7f8cc378e8b`
- Code tree: `a53e5747d39b5568a5d0d7cff233daa6af11c45b`
- Parent candidate: `42e50f4c98428e449a5c247d3e914b56f569ddc4`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Candidate branch: `codex/core/CORE-F-shared-daily-v1-app-host`

## Change

A selected runtime now binds its compatibility advance entry points to the exact `DailyV1Application` and `DailyV1ActiveRuntimeSession` that own its admitted execution state. Calls through `SimulationRuntime.TryAdvanceDay` and `TryAdvanceDays` forward through that application/session gate. An unbound standalone runtime retains its existing compatibility route. Binding checks the exact execution-state instance and the admitted `UnityBootstrapDailyV1` profile.

The regression holds an exclusive Core application operation, then verifies both runtime advance APIs reject with `AdvanceAlreadyInProgress`, multi-day advancement reports zero completed days, and the runtime day remains unchanged. No P12 owner, capture, hydration, token or persistence semantics changed.

## Validation

| Gate | Result | Evidence run |
|---|---:|---|
| Official CoreValidation cold | 891/891 PASS | `ce572c8a38f8431e9c62233e96fc80ad` |
| Official CoreValidation warm | 891/891 PASS | `ce572c8a38f8431e9c62233e96fc80ad` |
| Focused Unity owner-closure regression | 1/1 PASS | `81c1c30d662749598ca0f212faf8e298` |
| Unity ALL EditMode | 3274/3274 PASS | `0814bb4a478d4a3e87754cd6a8ba8af8` |
| Official Unity Smoke filter | 5/5 PASS | `5de6b9573b3a4e39a10f7e51ea22872e` |
| LongRun cases present in ALL EditMode | 8 cases discovered | `0814bb4a478d4a3e87754cd6a8ba8af8` |
| `git diff --check` | PASS | code candidate commit |

Unity validation used `6000.3.9f1`. The focused, full and Smoke run input digest is `aa2799abddc779bb8a16972ad4421734ac34165cef2fc709c7b24996a348e004`; each run reports stable before/after inputs. The full EditMode result package is the official privacy-safe package in [Evidence](Evidence/coref-app-advance-authority-editmode-final-20261027.manifest.json) and [archive](Evidence/coref-app-advance-authority-editmode-final-20261027.zip). It was independently verified by the official verifier. Manifest SHA-256: `C8B32D8E41E8329662EE74F92C70A326369D5881CF35E84D9B7859FFA45BE0F7`; archive SHA-256: `E1B7CBE0141AF4198603151F9C9FC9A4F92748393A56646A9ED75513F402A8DF`.

The CoreValidation input digest is `270e50f0e8c2793a4ee3bae8f2f7997a829d1c2201439745e8b6c77af6b513b2` and was stable. Its private manifest SHA-256 is `800E4E4F9314AF75D0D4C78A8C710B9493DE07878CFD04E67892993CCAD1A33E`; original cold/warm TRX SHA-256 values are `7129b3023f86bb05ccfe836689ec87988ad6f57212295a2e7c5fc2e38ca9b19e` and `fc09c4385cf6650be6900fd157dc86d5cd9ee29aeec2b53eedc28a111f3ece78`. The originals and private runner manifest remain in owner-only local evidence storage. No public DotNetTrx projection is claimed because this runner did not emit the source-context schema required by the approved packager. This incremental run observed a Unity `Library`; it makes no Library-independent .NET-host claim.

The tests ran before the source commit, from parent `42e50f4` with the candidate files in the working tree. All six committed files were then hash-checked against the exact captured input records; no tested bytes changed before commit:

| Committed source/test file | SHA-256 |
|---|---|
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `99ffdbe04d281a06415e612f2f22979bec713425d4dc30faaf8fc794efc697b8` |
| `Assets/_Project/Tests/EditMode/Editor/Core/CoreFDailyV1OwnerClosureTests.cs` | `d35d65bc3427558acc24dd16becd98459d8e62a9a130ef78b1e3221c2ff99124` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeOrchestrationTests.cs` | `34a01aa635472a91877133f8660547f9d84e0d48b2390954ed7b9eae389b1264` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | `d3ebe35c1e5e0fb312b56e7f6ea79d4f48973b2ed4c22cd0d79f1156ef903936` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs` | `cce023d5b4b606b0877a32ee3f77bd3feb9e9ae334dc8b62939873e8c269ce22` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs` | `aba48efe06ec47d3023bdfd9bbf5f919a58b962767df1621db4b60efadd93cf3` |

## Limits

This closes an application-gate bypass through the Unity runtime projection. It does not complete the Core-owned runtime/session migration, `DailyV1Application.TryStart`, detached P12 restoration, the fixed .NET host, or separate-process T/T+1/T+2 continuation. CORE-F remains open and unpromoted. The pre-existing ProjectSettings edits, unrelated untracked files and protected evidence were not staged.