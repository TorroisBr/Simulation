# P12-D SpatialNetwork Owner Capture Validation

## Scope and source identity

This bounded checkpoint moves the P12-D SpatialNetwork snapshot source to the exact Core `SpatialNetworkOwner` retained by the active selected runtime graph. The continuation-save operation captures one immutable snapshot under its existing lease and validates the Location/Route census pair against the same token. The app-backed Unity package binds to that exact Core owner and consumes its snapshot; the no-operation legacy path remains unchanged. The P12 image sections and format are unchanged.

- Candidate base commit: `24b629c6fcdd6918d1dd1ca402a665b17e363559`
- Base tree: `e0f1222014ede8c6e7594ef614c1170c976e7c2e`
- Source/test working diff object: `8a07fc31f5dce41b33ca0ea9d08e03a0274f692c`
- CoreValidation source input digest: `788b79a79692460aeeaa1cbdba225147f6650b36d6afb7b264918cbfbf832f62`
- Unity input snapshot digest: `835eaf112d4a12b1996e5d04f966da86be610142f681dca7b59f005ece38323a`
- Core and Unity input inventories reported stable for their successful final runs.

| Changed implementation/test input | Bytes | SHA-256 |
| --- | ---: | --- |
| `Assets/_Project/Scripts/P12DDailyV1OwnerPackage.cs` | 55058 | `2e0a260fad8858af75fbb9103c5d1a3b7ceb36218275a43ae96ddf82fb3bcb42` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | 828746 | `fd43fcce79c28cc7a2acbe8c8d3ee7468d052e42d9d63c7f6f7a8fe2b0f50b32` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | 92322 | `3935d6df09fb4e1a99ee16f5db04c75b50b8193c200aa861801a104f08c49e31` |
| `Packages/com.simulation.core/Runtime/Geography/SpatialNetworkOwner.cs` | 10021 | `2a9a8971575e8802eaeb29b9d4acf929f5c91d0aeb43a9158354bd97fbdc60ab` |
| `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GContinuationSourceWitness.cs` | 8594 | `fe58a2b279e27fc7a313cda2ec8a0aba811c6607a459d114215b6d1a4dc2ebcf` |
| `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GCoreBoundOwnerSources.cs` | 28662 | `86e57ebf5625568559fab264cd823cb544e6b60c33f249b502b2155542ef1b8a` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs` | 155461 | `2d865cbbbec645b16ebfd40c59b6c0dfa78051d896d2814dccd6d7fc8de02ef1` |

## Validation results

### CoreValidation

The official CoreValidation cold and warm runs passed 935/935 each. The pinned SDK, restore, Release build, discovery, both test runs, demo, and Lab gates passed.

- CoreValidation run ID: `b78c4c14c74d4266a02d0bfae612a121`
- Cold VSTest run ID: `c962e590-a84c-43df-b070-5bf741e3b526`
- Warm VSTest run ID: `b9fd4a6a-3984-4db8-9556-22c41d5103ee`
- Protected native runner manifest SHA-256: `e99c1108190f1230494a3a41d6b99719e9ce93cb41081a051d50dc11ff7efb11`
- Cold TRX SHA-256: `e948dd58f6014e68ca5fe099181ea425724aa48e58c07d58ab513abcb13ff006`
- Warm TRX SHA-256: `be53df44d4b8027d14c155e6dc3eab44c20aec512c158f86671640d402eedadc`
- Cold test log SHA-256: `ebe909522f5ef305a8485d773b5e153faa990571c1c5993c66fd6d486935fbc4`
- Warm test log SHA-256: `076594d84f8fe549a8d7db4e940ef2b5ccc6ccfe9dc5c7b404c93dcbc5aed116`

The official CoreValidation manifest and original TRX/logs remain in protected E:-local evidence storage. The separate `DotNetTrx` adapter rejects the native `simulation-core-validation/v1` manifest as an unsupported source-context key; no synthetic adapter context was created. This uses the repository's established native CoreValidation evidence form. Existing Unity Library presence was recorded by the Core runner; this run does not establish Library independence.

### Focused Unity restore regression and full EditMode

The focused nonempty Person/Genealogy detached-restore continuation regression passed 1/1. It also opens a Core continuation-save operation, verifies its exact SpatialNetwork owner reference and Location/Route census pair against the Unity projection, then confirms that the captured image uses the same snapshot cardinalities and revision. Full EditMode passed 3279/3279; all failures, skips, and inconclusives were zero.

- Focused run ID: `8d613c1560bf40b7b699678f79dd201d`
- Focused protected manifest SHA-256: `bc1df725fad1d1e3e2f6b595f3cb683d1eee027c6fe6f5926af742bd46386bce`
- Focused XML SHA-256: `86762b2cab85bf8f80c4cbd1344e0795f95256e129d9f69c29443d44d6c750f1`
- Focused raw log SHA-256: `de33ab44f84d3511e4b3a29248a87c52abd19c21b38372721575094c710f7648`
- Full EditMode run ID: `f3e2e881b1ff48509f04b05c57a953c9`
- Full protected manifest SHA-256: `c3512ab367c1128b980545c4730f15b654e349eb25efdafd0855faecd5e5cf7b`
- Full original XML SHA-256: `cd8d52e2c0d8ead101ff91e17b166f317f2491948bbf9fee265a34c5eed67589`
- Full raw log SHA-256: `2c4a06408aa2b10cbe8b44ec5c02abb669b8c893e3039cb1b3990f5233b8c51b`

The result-only full EditMode package is the privacy-safe public evidence in this directory. The official package verifier passed (`runs=1`, `entries=1`):

- `coref-p12d-spatial-editmode-all-20261029.zip` SHA-256: `b5381427ae69dc146210027387a571393ffe1107e6947eb5b28b4770b074dce5`
- `coref-p12d-spatial-editmode-all-20261029.manifest.json` SHA-256: `9daa162329194ef1ec673c3aef1261c7886b2cfbffac650eae7ede7deb4f40ad`

### Official Smoke

Official EditMode Smoke passed 5/5 with zero failures, skips, or inconclusives. The input snapshot matches the full EditMode source digest above.

- Run ID: `64258787fbfa4a1294e7b5f07f390b6c`
- Protected manifest SHA-256: `0fcca46265dc7abb6b11887abdf2ce9990be4930fc7c37d783f1af879e7b932c`
- Original XML SHA-256: `cebfd934634e2fa409d211dc2b8d9daaddb268d9b33e852dfb4b9e75fa2f67fa`
- Raw log SHA-256: `7c2fa1435cc440427ed2424e0fbd5dfaca5eae36d8894c88ab3c42a1bfa90fac`

The result packager refused the filtered Smoke XML because the `Simulation` NUnit fixture hierarchy's declared `testcasecount` does not match its filtered descendants. The filtered XML was not edited or repackaged. All five Smoke test identities are present with `Passed` outcomes in the full EditMode original and the verified result-only package:

- `GMConsolePlayModeSmokeTests.GmConsoleCanPreviewApplyAndRefreshObserver`
- `GMConsolePlayModeSmokeTests.GmConsoleNaturalLanguageCanTranslatePreviewApplyAndRefreshObserver`
- `WorldObserverPlayModeSmokeTests.ObserverSceneCanRenderAndSelectNode`
- `WorldObserverReadModelTests.ObserverPrefabSmokeInitializesWithoutMissingReference`
- `WorldObserverVisualTests.ObserverPrefabSmokeHasNoMissingSerializedReferences`

The earlier focused Unity attempts `78d1dae0b88d4f1e81311c5a5bc068e4` and `e91340d6de3f4e26b715ba09c1dfd7c1` produced no test XML: the first found a nonexistent `SimulationRuntime.CoreRuntimeSession` accessor in the package guard; the second found the Unity projection was read from `SimulationRuntime` instead of its session. Those compile errors were corrected before the passing focused run. Their protected originals remain outside Git; no output from the failed attempts is used as acceptance evidence.

## Integrity and limitations

- `git diff --check` passed on the source/test implementation snapshot.
- Only the seven implementation/test inputs listed above are in the implementation diff. The three pre-existing ProjectSettings modifications, unrelated untracked files, `.meta` files, and private evidence remain excluded.
- This is a bounded P12-D capture-source checkpoint only. It does not close P12-D, P12-B, or CORE-F and does not promote Core canonical.
