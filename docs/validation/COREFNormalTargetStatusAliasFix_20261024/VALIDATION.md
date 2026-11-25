# Core-owned targeted Normal action projection identity

This record revalidates the narrow target-projection finding from the review of `3cb725822f925981c90813adb144d5bac58dd664`. The Unity adapter now requires the projected `TargetNpc` object to be the exact runtime resolved from the admitted owner graph for the payload's target RuntimeId. A different runtime object that merely reuses the same RuntimeId cannot supply target text or a selected-action dispatch projection.

The admitted Core action still commits through the payload's exact target owner and the existing P12 mutation boundary. This change does not alter target selection, status semantics, action ordering, or writer authority.

## Source identity

The validation input snapshot recorded parent commit `3cb725822f925981c90813adb144d5bac58dd664` and parent tree `69fe50979b17fb404deb909bd92d548a4a5253cb`, with the two modified source inputs below. These are the exact post-fix source hashes recorded in the full EditMode run manifest and must match the reviewed candidate commit.

| File | SHA-256 |
| --- | --- |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `b511cfcd50db6f2cffda5e511aacef44b6257a526bc2bacde7d2077a6c45c66f` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `2e8cb72c6e3b327147d9972c084c361368b4b56d7bbf83c74d0918682a108856` |

## Validation

| Gate | Result | Run identity |
| --- | --- | --- |
| Stale RuntimeId-alias regression | 1/1 PASS | Focused test `ProjectionIndependentTargetedNormalActionCommitsExactTargetAndRejectsAliasedProjectionTarget` |
| `SimulationRuntimeAdmissionTests` | 223/223 PASS | `228bf4bddf6f4932a302fc410047dcd2` |
| Official Smoke filter | 5/5 PASS | `f980454968234784ae434869a43e4c48` |
| Full Unity EditMode | 3258/3258 PASS; 0 failed, skipped, or inconclusive | `514f761f865649e38229e5dcefdff48e`; Unity `6000.3.9f1`; input snapshot `0a33ae3288453ea2ab09e13b4d04a46c2d95cc8c6fc9c96599e133649ca4f9cb` |
| CoreValidation (`-SkipLab`) | 682/682 cold and warm PASS | `5352071c0a67492aaf7491b4b2851a15`; the runner reported `LibraryAbsent=False`, so this is not a Library-independence claim |
| `git diff --check` | PASS | Candidate source diff |

The official evidence packager verified the complete EditMode package against the private run manifest. The normalized package is `EditModeEvidence/coref-normal-target-status-alias-fix-editmode-20261024.zip`; its SHA-256 is `8497b96b59f570749480e7ea29db1968b84c72028cfff99749e2f68a36db1ce9` (172,621 bytes). The package manifest and archive are adjacent. A privacy scan of the public package manifest found no `C:\Users` or `C:/Users` paths. Original runner evidence remains in the private local retention area.

This is exact-slice revalidation only. It does not complete or promote the broader CORE-F application/host package.
