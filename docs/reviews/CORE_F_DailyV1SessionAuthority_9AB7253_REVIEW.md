# CORE-F Daily-v1 session authority and demography/travel context review

Date: 2026-10-30

## Verdict

VALIDATED_CANDIDATE — PASS for the bounded ten-path integration slice below. No actionable code or test findings remain. This review does not approve Core canonical promotion and does not claim completion of the CORE-F application/host package.

## Reviewed identity

- Candidate branch: codex/core/CORE-F-core-owned-runtime-application
- Exact candidate tip: 9ab7253ac16bb075fbb1286c5e15f75bde778e4f
- Implementation commit: 8414de899e24a04ff6139cee5570f65bdfb9325f
- Implementation tree: 0cb45c804e2627769a50b11f743d8c1b8c3b074c
- Exact implementation base: 3d1d5f660467c98c4622d03a9659685c59530eea
- Current Core canonical at review: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- The candidate tip adds only the validation record and two evidence package pairs after the implementation commit. The candidate and canonical remote refs matched these identities at review preflight. The isolated review checkout was clean at the exact candidate tip.
- Independent reviewer: read-only review agent, separate from the candidate author.

## Scope reviewed

The full implementation diff against the stated base changes ten implementation/test paths:

- Assets/_Project/Scripts/SimulationRuntime.cs
- Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeOrchestrationTests.cs
- Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs
- Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs
- Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs
- Packages/com.simulation.core/Runtime/Application/DailyV1SelectedDailyExecutionStage.cs
- Packages/com.simulation.core/Runtime/Application/DailyV1SelectedTravelDayStage.cs
- tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs
- tests/Simulation.Core.Tests/DailyV1SelectedDailyExecutionStageTests.cs
- tests/Simulation.Core.Tests/DailyV1SelectedRuntimeOwnerGraphTests.cs

The Core execution state now owns the one-time application/session advance routing pair behind a dedicated lock. Unity runtime forwarding reads that exact pair and then enters the existing application session gate; the state lock is not held while that gate is entered. The app binds only the exact session/state pair and retains the existing host-boundary and publication checks.

For admitted demography, Core creates the execution context from the admitted execution state and exact owner-graph reference. The context takes its calendar from that state and retains only the existing sample-provider and writer-boundary host ports. The host boundary factory verifies the exact selected city/population bindings. Legacy unadmitted execution retains its existing host context route.

For admitted travel, the host-port interface no longer supplies stage availability. Core derives solo-travel availability from the sealed runtime operation inventory and rejects an admitted state without that inventory. The non-admitted compatibility path retains its host-reported fallback. This resolves the prior finding that Unity could independently decide whether the admitted Core travel stage ran.

The tests add positive and negative inventory/context-binding cases, exact application/session routing assertions, and preserve the prior failure assertions. No test assertion was weakened. The change does not modify the owner census, its 89-family contract, owner capture/hydration, section identities, revisions, epochs, mutation order, or P12 profile.

## Validation and evidence review

The evidence records bind the implementation to commit 8414de899e24a04ff6139cee5570f65bdfb9325f and tree 0cb45c804e2627769a50b11f743d8c1b8c3b074c.

| Gate | Result | Evidence identity |
|---|---:|---|
| Official CoreValidation cold | 960/960 PASS | Runner e0b38a3fdd214c55a8572d92d36ac568; native report fca352e8-b912-4e65-a2d5-6cf6e824d3b7 |
| Official CoreValidation warm | 960/960 PASS | Same runner; native report 8407681b-b551-4767-9dff-5cef4868de1f |
| Unity full EditMode | 3,283/3,283 PASS | Run 5c5ece02e37544edac9806042bd91b7f; stable input snapshot; Unity 6000.3.9f1 |
| Filtered Smoke check | 5/5 PASS | Run b94dc63aa9844e0ab160046d28aee867; stable input snapshot |
| git diff --check | PASS | Implementation commit against its exact parent |

The CoreValidation package records the exact source commit and tree, stable source input digest 586e90ceeecfe60688b7b936f48125e34236063d541a6626eaa1add1f989b743, and a Core-only checkout without a Unity Library. It records .NET SDK 9.0.315 and CLR 8.0.28. The cold and warm original TRX hashes are 4b58bea6fb20208dda2a3be2a42e492dfb27d9706cae9ecfac90f7cf56c33620 and 2d9c245053bffff0e0067344c516cc3cfe9422167668060f00a60d8671c44d72.

Public .NET package:

- Manifest docs/validation/CORE_F_CoreOwnedRuntimeApplication/CoreSessionAndDemographyTravel_20261030/DotNetTrx/coref-session-demography-cold-warm-20261030.manifest.json; SHA-256 8ca99239a5a24d30e39d4b2e3e73442e05b5706ec9377a7bec31fe8b27a3399c
- Archive docs/validation/CORE_F_CoreOwnedRuntimeApplication/CoreSessionAndDemographyTravel_20261030/DotNetTrx/coref-session-demography-cold-warm-20261030.zip; SHA-256 a6f748acc103efd93d73ecdf834650d9110b9b9edce922607ca4431ebc513057
- The package contains normalized cold and warm TRX only. Its recorded source-projection comparison passes for both attempts.

Public Unity full EditMode package:

- Manifest docs/validation/CORE_F_CoreOwnedRuntimeApplication/CoreSessionAndDemographyTravel_20261030/Unity/coref-session-demography-travel-editmode-20261030.manifest.json; SHA-256 579b1caad3b9f58896860c815b970c041002d6af20ea57c2a477e028c96796e1
- Archive docs/validation/CORE_F_CoreOwnedRuntimeApplication/CoreSessionAndDemographyTravel_20261030/Unity/coref-session-demography-travel-editmode-20261030.zip; SHA-256 b89e5262110f43751a893b56dcbc5a5740216be1cfa406e2fa8a3d2e91c0e47a
- The archive contains the privacy-normalized full EditMode XML. The protected run manifest reports stable inputs; all seven Unity-side changed paths match its post-run source snapshot. The two C# files checked across Unity and .NET worktrees have different raw line endings but identical Git blob IDs.

The official filtered Smoke run is preserved unchanged but has no separate public package. The packager correctly fails closed because that filtered XML contains five result cases while suite testcasecount attributes retain full-assembly counts, including 3,283 at the top suite. No original XML was edited or repackaged by another route. All five filtered Smoke case identities are present in the passing full EditMode result. This evidence limitation is recorded in the candidate validation record.

The public manifests and normalized XML archive entries were checked against their recorded hashes; no workstation-path or credential-shaped strings were found. Private original reports, logs, contexts, and metadata remain outside the public candidate.

## Review limitations

This verdict applies only to the exact ten-path implementation slice and its evidence. It does not establish a complete Core-owned Daily-v1 runtime, complete DailyV1Application.TryStart startup, detached P12 restoration, a fixed executable .NET host, or separate-process T/T+1/T+2 parity. CORE-F remains incomplete and Core canonical remains unchanged.

The reviewer did not rerun tests and made no implementation edits. This record is written on the separate review branch, not on the candidate branch.