# CORE-F restored Core graph-binding order

**Status:** review-fix candidate validated; this does not promote Core canonical or complete CORE-F.

This follow-up addresses the test-scope finding recorded for candidate `9c080384` in [its independent review](../CORE_F_RestoredCoreGraphBindingGate_20261109/REVIEW_9c080384.md): the Core unit test now describes only the graph-binding gate, and the Unity test exercises the actual restore-path ordering.

## Scope

The private restored target's exact Core Person/NPC/Genealogy and TravelParty graph bindings are checked before Unity's compatibility projection validation and before the target owner-vector census. If Core composition returns no session, restore now reports a specific binding failure.

The Core regression verifies valid and dangling genealogy graphs. The Unity restore-path regression mutates the private candidate's Core genealogy after composition, then verifies the restore rejects it with `BindingValidationFailed` before `TargetOwnerVectorCaptured`; the application remains unpublished and the source continuation image stays unchanged.

This is a bounded pre-census binding gate. It does not complete the full 89-family census protocol, the complete P12 restore lifecycle, detached .NET hosting, or the remaining CORE-F runtime authority.

## Source identity

- Repository: `TorroisBr/Simulation`.
- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`.
- Git HEAD during validation: `fd597ca5474096c164b80d1ad3d5d522ad4a8f67`.
- Committed tree during validation: `db9d00f344228fdd08b3daa633b3025736bd5148`.
- Core canonical during validation: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- CoreValidation working-input digest: `4e2337ba09d920c9cd88c77168a5d9f858cdfadff5543268530a8d43c96208d3`.
- Unity reports a stable before/after validation-input inventory. Its captured repository identity is the same HEAD/tree; the verified package binds the production and Unity-test file hashes.

| File | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | `a47b6c4ea2e5743aaba3cb3c519fc8da0e10232195aab357ad7fabf081545176` |
| `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.ImageRestore.cs` | `4d69a356c11158ae0c0bee38d8f102cf0580004ca2424d799dc29ec4cb4938fa` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationHostTests.cs` | `63d03e096223fde02c37855b00e25d023ced8f8658fb77124be5ed8f0b831222` |
| `Assets/_Project/Tests/EditMode/Editor/P12GOwnerEntryQualificationTests.cs` | `e0a66bad7476f36f17fdd557955e9424f5df8eab51dce40bec48e7902b987d15` |

## Validation

| Gate | Result | Evidence identity |
|---|---:|---|
| Official CoreValidation cold | PASS, 1,073/1,073 | Run `1bb34f230a734ea7a9cd9b96df3aacfe`; SDK `9.0.315`; private native manifest SHA-256 `1b3068241542a46b2af6be85ea28b1449b332b3aca6ecd310fd7e309482c41e6`; cold TRX SHA-256 `8a397c973e9595496635642c9d7f9f5c5cc4249f07ac8502976fabf173a4efc4` |
| Official CoreValidation warm | PASS, 1,073/1,073 | Same run; warm TRX SHA-256 `a8a888c3d1cd5a45d67265aedaf5dd532cd2016ead359f7725a28f75dbe5c4f3` |
| Focused Unity restore-path test | PASS, 1/1 | Run `21fc047c54f2470fa02ae929400b687a`; filter `CoreImageRestoreRejectsDanglingGenealogyBeforeTargetOwnerVectorCapture`; protected run-manifest SHA-256 `b3c20551870552ea6e9126ff2afa12a17efd17b4a3ed637d82ad9e3db4bcc75d`; original XML SHA-256 `ab4b234b90190c47214946c95c0c32f569533b83de0ccb0177f10248c7340538` |
| Full Unity EditMode | PASS, 3,303/3,303 | Unity `6000.3.9f1`; run `567144d55de9400e9b98cd1f8be1cd98`; protected run-manifest SHA-256 `251d50db203676475fe2f6051b2a7b09f442ee107ba77c5ce54caecebfb24bc6`; original XML SHA-256 `b1f9ae6cd44b80923fa57cc8bb3c5476583bdb221d3ccc5dbfcd04cae1dd7e08`; original log SHA-256 `87aac1d027d5370af96163873f0ba187d80f43e63c5a6a45f007135058907f0a` |
| Filtered official EditMode Smoke | PASS, 5/5 | Run `7f28da7222374a06bfe98ea913ec6b48`; protected run-manifest SHA-256 `8bc2006893f7a280d26c91b260bddea0637dfbf7e63c7f8b9afe185d1c5656da`; original XML SHA-256 `3eb2f7edfe47a4db977a7ed35420b16eba710c5cee1043a7aae22297083e49e1` |
| `git diff --check` | PASS | Candidate changes |

The full EditMode package is [manifest](UnityEvidence/coref-restored-core-graph-binding-order-editmode.manifest.json) and [archive](UnityEvidence/coref-restored-core-graph-binding-order-editmode.zip). The official verifier accepted it against the protected source run manifest: one run and one archive entry. Public manifest SHA-256 `7c78bdcf4889c1fbcbd733565852cc680be75ccb61ba1ff8bbfbac47971d1340`; archive SHA-256 `57060115a7cb7ebbad917b58e2c272a1e1c9d801ef86e444a684a27598c8e752` (174,892 bytes). The full report contains all five passing Smoke identities. Raw logs and original reports remain private.

The official CoreValidation runner's native evidence format is not the separate `dotnet-trx-source-context-v1` adapter context. No public TRX package is claimed, and no context was fabricated. Core originals and all Unity originals remain in protected private retention; private file locations are intentionally omitted.

## Preservation and limits

The pre-existing ProjectSettings edit, pre-existing validation-record edit, untracked user files, and protected evidence were not staged or changed. This candidate does not claim complete P12 restore/census, P12 readiness, or CORE-F completion. Independent exact-tip review of the corrected candidate remains required.
