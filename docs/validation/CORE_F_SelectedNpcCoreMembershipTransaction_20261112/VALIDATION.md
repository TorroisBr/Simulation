# CORE-F Selected NPC Core Membership Transaction

## Candidate identity and scope

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`.
- Implementation commit: `caee8293945384b31c72167a98f90ae414351be6`.
- Core canonical baseline: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- This bounded implementation moves selected NPC owner-graph and ordered actor-roster preparation, paired publication, and rollback into `Simulation.Core`. The Unity adapter continues to own host registry/projection changes, runtime-identity census registration, and the enclosing `runtime.npc-membership` P12 scope.
- Selected add/remove use the existing exact owner graph, selected roster validation, and execution-boundary compare-and-swap. A late Unity host-binding failure can request the matching Core rollback. Unselected NPC compatibility registration retains its existing path. P12 runtime identity cardinality/revision remains retained after active roster removal.

Changed code/test SHA-256 values:

| File | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs` | `a79f334557d71895d979aab856e295ac8399564aefe968017c97354ffc92c9d5` |
| `tests/Simulation.Core.Tests/DailyV1ActorTurnExecutionStageTests.cs` | `46d4a47aa75616c30911ab1f4087e343cbfd96ca0e583793f6c96abc6b0719a2` |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `8b7f2ad2acfc71b8cc5e1ac1dedff8ac7f0bf49db6344d59ecfc708f52c2df66` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs` | `21af7faf67687b991979c5094d3361de9456b7e8df4fccb1ab6c852c8ec39e59` |

## Validation results

- Official CoreValidation run `16402a9b9eb94b31bbaf6448c2978a6e`: **1,101/1,101 cold and 1,101/1,101 warm PASS**; complete approved runner including Lab build/cold/warm gates passed. Input digest `44c4b53a96dd718a2de4a51fd926cacf775674b592ccfab50f026c6733aabba4`, stable. Cold/warm TRX SHA-256: `8593b4246c9a3e4d7dc440aa0f1888805a535e52c7f25ed4624e0283b10f1513` / `b6d12acc79954537e5804270a2e12aac89ed548b8be64c962eb95734b467f4c3`. Private run-manifest SHA-256: `a17b967d9404a5400dd150b125c65a471fbbf49e50a5486d1d0cfd0f3709b0c4`.
- Focused Unity EditMode `SimulationBootstrapCompositionTests.SelectedDailyV1MembershipPublishesCoreGraphRosterAndDynamicCensus`: **1/1 PASS**, run `0322f00989834654b4f51cc53f560be4`; private run-manifest SHA-256 `82844e7490de033ef9ff6931b1ba577c91d6ac52f42ce50810d5f1c61c78b8c9`.
- Full official Unity EditMode: **3,318/3,318 PASS**, zero failed/skipped/inconclusive. Run `9267c47b4f954c38a70100ce3a7f7282`; stable input digest `e51af85783aaa00b5b1ef40b095e9b9fb819b6c22166413fe19cfa020ba2f346`; original XML SHA-256 `7e53f966c95bb76ced9fcb47c5184c5fe14ac4f9a134e403662d3d54cf3d61f3`; private run-manifest SHA-256 `43b181b92b7a1bc8fcc240ea71ace0e0a8ed26d72d9a5d4d71337902f64a8525`.
- Official filtered Smoke: **5/5 PASS**, zero failed/skipped/inconclusive. Run `9612e681e5164df59308f9146741d8f6`; input digest matches full EditMode; private run-manifest SHA-256 `2683b3130493f3ba12b041227f1fd0659cc96d0ed7a095f3cb52ca3c0a73a8cd`. All five filtered Smoke case identities/outcomes were compared with the full report; all match. The new selected-membership case occurs exactly once in the full report and passed.
- Privacy-safe full EditMode result package: [`coref-selected-npc-membership-editmode.zip`](Unity/coref-selected-npc-membership-editmode.zip), 175,990 bytes, SHA-256 `06d3e7876635587926dd975cd4357cafb7bcc822bbf4607ae3dfc33c63e9cabb`; package-manifest SHA-256 `171b314a923018297c05d2d37c0ab6c5938cd2d9c3b4d34a45adb70e612fbffd`. The official verifier passed (`runs=1`, `entries=1`) against the private original run manifest.
- `git diff --check`: PASS.

## Exact behavior covered

The new Core test verifies that preparation does not publish either half of membership, that graph and roster publish together, that selected actor-turn roots rebind to the resulting graph, that a failed later host step can roll back to the prior graph/roster, and that selected removal leaves neither the exact owner nor actor identity in the active graph/roster. The Unity integration regression verifies selected NPC registration/unregistration through the admitted profile, host lookup, dynamic per-NPC P12 owner census, exactly one membership epoch per successful add/remove, and retention of the exact runtime identity census owner and cardinality after unregister. The pre-existing legacy generic re-registration case remains unchanged.

An initial exploratory version temporarily added a selected-object re-registration expectation to that existing generic test and failed because that behavior was outside this checkpoint's accepted contract. That draft was removed; the original test is unchanged, and the selected add/remove proof is isolated in the new test above. No production behavior or assertion was changed to accommodate that out-of-scope expectation.

The CoreValidation runner emits `simulation-core-validation/v1`, while the separate public TRX packager requires `dotnet-trx-source-context-v1`. Raw Core TRX/logs and the original run manifest remain protected in E: private evidence; no public Core TRX package or Library-independent host claim is made. The Core runner reported that this checkout has a Unity Library. The Unity result package is the privacy-safe public artifact; its original XML/log and full runner manifest remain protected.

The validation snapshot included the existing local `ProjectSettings/ProjectSettings.asset` edit. Its SHA-256 remained `f0771a23c3ebcb50d651c13515ee66e6e56d321c6115f54a2193bf35dd994633` before and after the full Unity run. That edit, the unrelated `CORE_F_SelectedTravelPartyCoreCharge` validation edit, all other untracked files, and all private evidence were excluded from the candidate commit and left untouched.

This is a validated incremental CORE-F candidate only. It does not complete the shared runtime/session, public `DailyV1Application.TryStart`, complete detached P12 restoration, .NET hosting, separate-process T/T+1/T+2 continuation, or CORE-F acceptance. No Core canonical promotion is implied.
