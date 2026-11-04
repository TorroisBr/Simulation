# Independent Review — CORE-F Restored Selected-Owner Mutation-Guard Binding

## Verdict

**VALIDATED_CANDIDATE.** The independent review found no actionable implementation or test-integrity findings. This verdict applies only to the bounded candidate and exact source identity below. It is not canonical promotion or CORE-F completion.

## Exact source identity

- Candidate branch: codex/core/CORE-F-restore-stage-transaction
- Reviewed tip: 1e3cbc8dbb4c0cfc4ca10a3762d158ec3170ff53
- Reviewed tree: d4be057eb1446d4b2d6279b1270c0717f472e4bf
- Production/test commit: a30193d2eb1bf2b5e7d02bb774b5ce9f1c0aa94a
- Production/test tree: 77cb07ff1da2ff3102bca88288ab9817d4da4e97
- Exact base: 6bac9d0bd26464372840c5f9807f4a5fc2e77944
- Current Core canonical at review: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Independent reviewer: separate Codex review agent, /root/review_guard_arrest_p12

The final commit before review changed only the two corrected SHA-256 labels in VALIDATION.md. The production and test files, normalized Unity evidence archive, and package manifest are unchanged from the reviewed source commit.

## Reviewed scope

- Assets/_Project/Scripts/SimulationRuntime.cs
- Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs
- Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs
- Packages/com.simulation.core/Runtime/Application/DailyV1SelectedRuntimeOwnerGraph.cs
- tests/Simulation.Core.Tests/DailyV1GenesisComposerTests.cs
- docs/validation/CORE_F_RestoredCoreOwnerMutationGuard_20261104/

Core preflights the exact selected restored owner graph and binds its mutation guard before the Unity host binds remaining authorities and projections. Unity verifies required projection references against the Core-bound owner instances. The tests cover exact owner binding and rejection without partial binding. The reviewed scope retains the distinction between Core-bound owner graph authorities and host-owned non-graph authorities/projections.

## Validation evidence inspected

- Official CoreValidation: 1,012/1,012 cold and 1,012/1,012 warm, stable inputs. Protected manifest SHA-256: 9d14582008c531928452bf58ecdf3250e18a8a63eed8c64ba30296b46a6a8455. Cold TRX SHA-256: fe7b606994eade7d181e9c5abdb78820d061e4b1c0c6245f13090460e829575e. Warm TRX SHA-256: 863e7e71f559dcbbdb68bdbe61d04fdd9063fb64f64a9545d5b6cd073f28d563.
- Focused SimulationRuntimeAdmissionTests: 243/243 PASS.
- Full Unity EditMode: 3,299/3,299 PASS; stable input snapshot c88e939396a2a4e728710ebe66028b68cf1f78282a6f2eb49a6bf2807d57b5f0.
- Official EditMode Smoke: 5/5 PASS, same source snapshot as full EditMode.
- The focused and Smoke protected manifests do not contain independent input snapshots; their counts and artifacts were checked, and the full EditMode manifest supplies the stable source snapshot.
- The final normalized Unity package verifies against its original run manifest. Package manifest SHA-256: 5671ac0c0bb486db9e87a1db441b1a2fcf304967b59eb88a4dbce1411e612ae7. Archive SHA-256: df8e91349ddad888eabbbbc6ec7cd5a1831c12aadaf4b3bbf9f5ee5846b7650d.
- Protected raw Unity XML SHA-256: 85b2bb22ee5ec82558c02a8f0ac10ceee67d70cbc1eb46eb75af1f40930ffbfb. Protected log SHA-256: 5f3f1faf1fa5ac38abcd6256d28bd2c8b67b7d0ddbe92885492ad84379ffcc7b.
- Package manifest and ZIP matched the recorded hashes. The reviewer confirmed the normalized XML reports 3,299 passed and found no workstation-path leakage.
- git diff --check: PASS.
- CoreValidation ran with a Unity Library present; no Library-independent validation claim is made.
- No test suite was rerun for the docs-only hash correction.

## Constraints and remaining integration

This candidate binds only the listed Core-selected owner authorities before host binding. It does not move all P12 owner, projection, or census authority into Core. The Unity host still supplies non-graph mutation authorities and projections. The shared DailyV1Application lifecycle, complete detached .NET host, and separate-process T/T+1/T+2 continuation remain outstanding. CORE-F remains incomplete. No canonical promotion is implied.