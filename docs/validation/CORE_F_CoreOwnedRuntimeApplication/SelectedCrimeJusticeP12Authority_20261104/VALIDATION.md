# Selected Crime/Justice P12 Core boundary — validation

Status: independently reviewed implementation checkpoint; not canonical promotion or CORE-F completion.

## Candidate identity

- Branch: codex/core/CORE-F-try-start-transaction
- Candidate commit: 3314fe41e68f196e4259778eed4f4d4f1418d40c
- Candidate tree: e22b251b3b1d6713f7a7835dda92f2c73cf05726
- Parent: 6cf0af2937d16eaaf36b0a7134feb8e37e895cc8
- Core canonical: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea

This bounded change routes the admitted selected Crime/Justice day-start phases through the existing Core actor-turn execution boundary. Core checks the selected session, owner thread, exact graph and host-supplied census identities, then performs the existing four P12 reservations in order. Unity still supplies the exact census protocol and opaque receipt identities, and retains projection and non-admitted compatibility responsibilities. Mixed-roster execution still takes the whole-stage legacy fallback.

The change does not complete the Core-owned daily runtime, DailyV1Application.TryStart, detached restoration, or the .NET host. It does not close P12 or CORE-F and makes no new capture, owner-coverage, or cross-host compatibility claim.

## Validated source identities

The five committed implementation/test files are the complete code delta. Their SHA-256 values match the official runner inventories for the files each runner consumes.

| File | Git blob | SHA-256 |
|---|---|---|
| Assets/_Project/Scripts/SimulationRuntime.cs | 1833e0ef530d95b54db440f74111a18662cdd406 | 9e923d99e7f837fcfb2a5c5e11413531e5b5900e5f7e586c1c155f268d62e296 |
| Packages/com.simulation.core/Runtime/Application/DailyV1ActorTurnExecutionStage.cs | 192e1ba30627f11bbe007e3af1f574d4bcf47bce | 4f4af49550b40d1c4c0eb4d96d8d00d4074182f9f850921be33319a62fd9a70b |
| Packages/com.simulation.core/Runtime/Application/DailyV1SelectedCrimeJusticeDailyStage.cs | 87f06faf38fd728f5d11251c786f3e8c8365d5fa | 247b60d652584c77d19781b0c44de06fd5876ac0c96833fc22a7c0804c2d8bc2 |
| Packages/com.simulation.core/Runtime/Application/DailyV1SelectedDailyExecutionStage.cs | dea69a08bc2839fa1f94ef47412711c9737074d3 | 04d360ab7bf7baf3a4f4886a4bc532e90261f1077a489a2e0d850de9c3f431a7 |
| tests/Simulation.Core.Tests/DailyV1SelectedDailyExecutionStageTests.cs | 0e2554f8976b870b596e56b2a8d713f7250b8ec2 | 5218196b353d1d55915343c501b7a75f90bd5812493a7207b193945951cb5b5a |

The test file is outside the Unity project inventory; its identity is bound by the CoreValidation manifest. Three pre-existing ProjectSettings edits remain local and unstaged; none is part of the candidate or package.

## Results

| Gate | Result | Evidence |
|---|---:|---|
| CoreValidation cold | 1,007/1,007 passed; zero failed, aborted, skipped, or inconclusive | Run 9ea22eb788984a6ea2cf0a30d376c4ce; stable input digest c5cf099b9cf93067452d98aa0db372046f1ae589e22ede37a2f496e6ff3cc9ad; private manifest SHA-256 5a464911c1fb574dfe2c685aac82aba28386049d2ccdb3104fa75afe994626c6; cold TRX SHA-256 5aeecd3908b8f53d2a36b6cbedc22804e194d9f20a090b525796bb553556d97d |
| CoreValidation warm | 1,007/1,007 passed; zero failed, aborted, skipped, or inconclusive | Same run; warm TRX SHA-256 b1faf68582a05347e71b22ef039f8fa0eb4d5649228574aee3a45feaf0f91f07 |
| Focused Unity SimulationRuntimeAdmissionTests | 243/243 passed | Run 40dd8765fc6e4d108e13e0fc54e4bf63; its filtered XML was not accepted as a standalone package because the ancestor suite count does not equal the selected descendants. The final full EditMode report contains the changed regression cases. |
| Full Unity EditMode | 3,299/3,299 passed; zero failed, skipped, or inconclusive | Run f86b4f9a67124b8ba7b699451332ec65; stable input snapshot SHA-256 462060ba04beabe4d30dbc84b94888510fdd9155a2ae91cd77930ee671c90cc9; seven LongRun identities and all five Smoke identities passed in this full report. |
| Official standalone Smoke filter | No result XML; not counted as a separate pass | The five Smoke cases are individually Passed in the complete EditMode report. |
| git diff --check | PASS | Candidate commit |

The result-only Unity package is in UnityEvidence/:

- Archive: coref-crimejustice-p12-6cf0af29-full-editmode.zip, SHA-256 5d1ad4c2247d2ac3c840b4610ab17194545fc1a6fe3f6279e32a89fb5e8280b7.
- Public manifest: coref-crimejustice-p12-6cf0af29-full-editmode.manifest.json, SHA-256 0c52e45f5758176bbee812c55368825d8de4a2fbcce08f0c3f6224f9993891a9.

The official Unity packager verified the archive against the protected run manifest. The archive contains normalized result XML only; privacy checks found no absolute workstation paths and no NUnit output elements. The five Smoke case identities in the packaged full run are:

- GMConsolePlayModeSmokeTests.GmConsoleCanPreviewApplyAndRefreshObserver
- GMConsolePlayModeSmokeTests.GmConsoleNaturalLanguageCanTranslatePreviewApplyAndRefreshObserver
- WorldObserverPlayModeSmokeTests.ObserverSceneCanRenderAndSelectNode
- WorldObserverReadModelTests.ObserverPrefabSmokeInitializesWithoutMissingReference
- WorldObserverVisualTests.ObserverPrefabSmokeHasNoMissingSerializedReferences

The CoreValidation manifest and original cold/warm TRX files remain in protected local evidence retention and are unchanged. No public .NET TRX package is claimed: the retained runner manifest is simulation-core-validation/v1, while the approved TRX packager requires a matching private dotnet-trx-source-context-v1 record. Do not treat the private hashes alone as permanent per-case evidence for a later canonical promotion. No raw TRX or logs are committed.

## Independent review

The exact-tip review is recorded in INDEPENDENT_REVIEW.md. The reviewer found no actionable implementation findings. The evidence limitations above remain separate from that code-review verdict.

## Continuation boundary

The next integrated work is the Core-owned P12 restore transaction: compose the existing P12-C/D/E/F stages into the application, bind a Core-native execution boundary without requiring Unity SimulationRuntime to remain the advance/census authority, and publish the restored session once through the application gate. This checkpoint is not that completion.