# Daily-v1 start preparation on the existing Core application

**Status:** validated candidate milestone; not Core-canonical and not CORE-F completion.

## Candidate identity

- Candidate branch: codex/core/CORE-F-restore-stage-transaction
- Implementation commit: 40b72a384ee824b7e9a9e1f4aad508e54f838eff
- Implementation tree: ed16029699f384bf10e6c8061473ea4de6d1711d
- Parent candidate: fd1e5c1af544bb856e1f0d042e40a4d2e642361e
- Core canonical: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea

## Change

DailyV1Application.TryPrepareStartOnExistingApplication performs initial persistence admission and prepares genesis or restoration on the already-existing application instance. The preparation carries the exact admission token and persistence result.

The Unity bootstrap now builds the selected effective-definition graph once, asks that Core application to admit the start, and retains the exact preparation. For the NoSave path, InitializeSimulation projects the already-prepared Core genesis and publishes its exact session through that same application; it does not rebuild a second Core genesis or create a second logger. For the restore path, the same application's exact initial admission is passed to detached restoration and the resulting session is published through that application.

The new Core regression checks that preparation retains the same application, admission and unpublished Core session. The Unity regression exercises NoSave preparation followed by projection/publication and asserts that the published Core session is the exact prepared session. Existing fail-closed P12 rejection messages were preserved.

This is an internal start-preparation seam. It does not add or claim the final public DailyV1Application.TryStart API.

## Validation

| Gate | Result | Run |
|---|---:|---|
| Official CoreValidation cold | 1,014/1,014 PASS | e301a6e356ba473b98dd613bc877628d |
| Official CoreValidation warm | 1,014/1,014 PASS | e301a6e356ba473b98dd613bc877628d |
| Focused Core-prepared Unity bootstrap regression | 1/1 PASS | 5055dc2c52ba45c399ebb52576a76585 |
| Full Unity EditMode | 3,299/3,299 PASS | e2767bc4bba44072a4295df054125ea7 |
| Official Unity Smoke filter | 5/5 PASS | 253c3e6c0c5b4b45a970a78f7b28b975 |
| LongRun tests present in full EditMode | 7 cases discovered | e2767bc4bba44072a4295df054125ea7 |
| git diff --check | PASS | implementation commit |

CoreValidation's exact committed source tree was ed16029699f384bf10e6c8061473ea4de6d1711d. Its stable working input digest is 8d2d26564c3fe5735a9b4e7541daf8d9f458dd61da562afaa774a5a4562470aa. The private native runner manifest SHA-256 is 94987e39c0d036199fea1d41c1fef865f4b872ee65cd3b2b56751acaeff78a89; cold/warm TRX SHA-256 values are 92587a6bf3bf5a2d8f491362e20af0ae78de322d8f8a8f896c37b0145b93dd2d and 2325b678d806c8b4d4c855ca25ab971e3c041246073eb49af17017b7b6b38783. The runner reported that a Unity Library was present, so this run makes no Library-independent .NET claim. Original reports and the native manifest remain in protected local evidence retention. No public DotNetTrx package is claimed because this runner's manifest is not the separate dotnet-trx-source-context-v1 adapter context required by the approved packager.

Unity ran with 6000.3.9f1. The full EditMode run reports stable before/after input digest 40cfed25eb6e8ac2b866a53d34b21b6276e1d35b86e1b3ea6c92daae602991b3. Its protected original XML SHA-256 is 47f59beba7520411f18301e0aafba40111937002de8b98684871d1173220c73b; private run-manifest SHA-256 is 08e0e13960d9c7265caab3466bb081180057b35785f546bc4e575c33f07d8b8d. The official privacy-safe package is [the EditMode manifest](Unity/coref-start-preparation-editmode.manifest.json) and [the result archive](Unity/coref-start-preparation-editmode.zip). The verifier passed with archive SHA-256 767da0b452a7ca484c58def5bb5fb3d32e6dcc469da1d8ef0740b2326aaed0cd; package-manifest SHA-256 is 758fca3eb03b73c54f89f87324c90b01721224ff8756afbbe4ed85be9ea2f745.

The full Unity snapshot's hashes for every changed Unity source and test file match the committed candidate. CoreValidation's input manifest likewise matches the committed DailyV1Application.cs and Core test file. The validation runs were captured before the implementation commit; no validated source or test bytes changed before that commit.

The first full Unity attempt found two failures caused only by appended diagnostic text changing the established P12 rejection messages. That diagnostic append was removed; the fresh full suite above passed. The failed original run remains protected locally and is not counted as passing evidence. A separate initial .NET invocation from the orchestrator directory also stopped before compilation because the runner's relative project path was unresolved; it is excluded.

## Changed-source identities

| File | SHA-256 |
|---|---|
| Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs | 5a8f6f35461927ddaaa2dc8a20dcccd54ea47fe0eb430c57d50da902ed751d06 |
| Assets/_Project/Scripts/TesteSimulacao.cs | c18399e1c2540ec6d57cca46f8e1ef5699c5c0068fff09632cefafb5a9a406d3 |
| Assets/_Project/Scripts/UnitySimulationLoggerAdapter.cs | 136cc3613d331926e5d005f9b04201deb09c22dfa1bec6872249323a967ae5c6 |
| Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs | 4d4fc2fb5400fbf8298cc66a886f54124eff27350f88e134fb45a83540810f44 |
| tests/Simulation.Core.Tests/DailyV1ApplicationHostTests.cs | 2fd12485e58d51adb8156dd58e19a18b4494dcba1aa15813e030f240975d6f09 |

## Limits

This milestone establishes same-application admission, genesis preparation, Unity projection and initial publication. It does not complete the public TryStart lifecycle, Core-owned Unity-independent daily execution, detached .NET host, or separate-process T/T+1/T+2 continuation. CORE-F remains open and unpromoted.

The unrelated ProjectSettings worktree entry and pre-existing untracked protected evidence were not staged or changed.