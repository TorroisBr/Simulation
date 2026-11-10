# CORE-F Core-owned census composition — validation

Status: bounded implementation checkpoint; this does not complete CORE-F or authorize Core-canonical promotion.

## Source and test identity

The tested implementation was the four-file working-tree change based on candidate 8781d650e830648440ebfbf9a29e2e99506497c1, before its recovery commit. The Unity runner recorded source-inventory digest 20997abb216d44280dc523807c2586f8bc037e2a2b346a07b9070321f44f3c72 and snapshot tree 730b3fc936d164b01992fb349fe25c12b1144bce; focused, full, and Smoke runs have the same digest and report inputStable=true. SHA-256 comparison confirms that all four changed implementation/test paths matched the Unity snapshot at run time:

- Assets/_Project/Scripts/SimulationRuntime.cs
- Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs
- Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs
- Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs

The pre-existing ProjectSettings/ProjectSettings.asset edit was present in the validation input snapshot and remains unstaged and unchanged. The test and production source identities above are bound by the official input inventory and public package; no claim is made that the local ProjectSettings edit is part of the candidate.

## Results

| Gate | Result | Run |
|---|---:|---|
| Official CoreValidation cold and warm | 1,089/1,089 each | 6e8b702c9f414d26a2cf8a066f18c756 |
| Focused census composition EditMode | 1/1 | 2e0b9d4e6b8d458f96389b6c3d9ec8a5 |
| Full Unity EditMode | 3,305/3,305 | 6588e8473afa4a31a3c81dd960b681ee |
| Official filtered Smoke | 5/5 | 9100bb6294874b7195336caf76a4ffc9 |
| git diff --check | PASS | candidate worktree |

All Unity runs have zero failures, skips, or inconclusive tests and share the same stable input digest. The five filtered Smoke identities are present and passed in the full EditMode report.

CoreValidation recorded inputStable=true, 1,089 discovered cases, working-tree input digest 69d1dc6db61f8f388cb1c1be4dffc84a8db0396dc8c2570bf6b37432d214c7e5, and matching current hashes for both changed Core files. Its cold/warm original TRX reports and manifest remain in protected local evidence; they are not copied into Git. The run observed a Unity Library in this checkout, so this is not a Library-independent .NET-host claim.

## Unity evidence package

The approved packager produced the normalized full EditMode package in UnityExact/. Its public manifest SHA-256 is 541007a4b6762c064800a2b28abfa54440ccf9d47e4e0dc08e3e3b98eac78447; its archive SHA-256 is 7012cc36c9cc5448b702b757d0bc641e44e9a9f6e234f0c6be4905aeb8b7bf56. Official verification against the protected run manifest passed. The original full XML remains unchanged in protected retention.

A public package attempt for the separate filtered Smoke XML was rejected by the approved packager because the Simulation suite's declared testcasecount does not match its descendants. The 5/5 test result is retained privately and all five identities are included in the verified full EditMode package. The original Smoke XML was not changed and no alternate packaging path was used.

No public Core TRX package is claimed; original cold/warm TRX and the official source manifest remain protected locally for independent review.
