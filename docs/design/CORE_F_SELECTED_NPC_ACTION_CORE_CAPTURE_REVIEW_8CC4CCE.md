# CORE-F Selected NPC Action Capture Review

**Verdict: `VALIDATED_CANDIDATE`**

## Identity

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Reviewed implementation commit: `8cc4cce57a7d621c8b436b5bc25d9efbfe211266`
- Reviewed implementation tree: `b90ffa944f93f8f2daabf59be0d4d01bbc34ddee`
- Exact base: `8a06a28855c681454f5bfea2e63a05754c0c8abe`
- Current Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Independent reviewer: `/root/p12_core_action_capture_review`
- Review result: PASS; no actionable findings.

The candidate adds one bounded P12 capture seam. Selected NPC current-action identity and payload fields are projected from the exact `DailyV1NpcActionSlotOwner` and its `DailyV1NpcActionPayloadOwner`. The Unity `NpcActionRuntime` remains a compatibility projection and must reference the same payload owner and pass its existing projection-consistency check before capture. An empty Core slot is accepted only when its payload and Unity projection are also absent. Selected D-row action identity and census cardinality use the Core slot. The unselected compatibility path is unchanged.

The P12 row/value and wire schema are unchanged. The snapshot retains the exact payload-owner identity for duplicate/alias rejection. It validates exact admitted action and optional item definition objects before exposing the immutable persistence view.

## Changed sources

The implementation commit changes only these four source/test files:

- `Assets/_Project/Scripts/P12DNpcRootOwnerSnapshot.cs`
- `Assets/_Project/Tests/EditMode/Editor/P12GPoliticalBindingRestoreTests.cs`
- `Packages/com.simulation.core/Runtime/Person/DailyV1NpcRuntimeOwners.cs`
- `tests/Simulation.Core.Tests/DailyV1NpcActionSlotOwnerTests.cs`

The reviewed Git blob identities are:

| File | Git blob |
| --- | --- |
| `Assets/_Project/Scripts/P12DNpcRootOwnerSnapshot.cs` | `393b5dfeea2535326c0bf69bc8c74e0151a7db3b` |
| `Assets/_Project/Tests/EditMode/Editor/P12GPoliticalBindingRestoreTests.cs` | `e393582c24ddd80c40622b19bcb9a43473391147` |
| `Packages/com.simulation.core/Runtime/Person/DailyV1NpcRuntimeOwners.cs` | `30d72fb6287ae8b7d6268adb5cc8ba4831c613c7` |
| `tests/Simulation.Core.Tests/DailyV1NpcActionSlotOwnerTests.cs` | `69f076146b2051a7f5de73615968d30d1d576334` |

The implementation tests are additive. They verify that the Core snapshot returns the exact owner and current payload values, preserves evidence object identities, rejects an incomplete selected slot, and represents an empty slot without inventing a payload. The selected-profile P12 integration assertions compare captured values to the Core owner while checking the Unity projection remains bound to that owner.

## Validation

Validation ran on the exact changed source/test inputs. The evidence wrappers recorded stable before/after input inventories; the independent reviewer recomputed the four changed-source hashes against the available protected manifests.

| Gate | Result | Private evidence run |
| --- | --- | --- |
| CoreValidation cold | 680/680 PASS | `8ccf2f7e20bf43fd802dde4677592989` |
| CoreValidation warm | 680/680 PASS | `8ccf2f7e20bf43fd802dde4677592989` |
| P12-D NPC snapshot focused suite | 30/30 PASS | `36dbde730cdb43fe85cb2f78d1caa849` |
| P12-G selected Daily-v1 continuation suite | 15/15 PASS | `eba10b4d57ac42f58c804c5de51b7b87` |
| All EditMode | 3255/3255 PASS | `36a9d0d31027415f906d942b7d431101` |
| Official EditMode Smoke | 5/5 PASS | `e8598eaa8f3d420dbd9db964f9e2d2be` |
| `git diff --check` | PASS | Implementation commit preflight |

Original TRX/XML reports, raw logs, and private run manifests remain in the protected local evidence store; they are not added to this candidate. The focused P12-D run preceded the final small empty-projection consistency check; the later exact-source full EditMode run includes P12-D and is the applicable final result.

## Review scope and limitations

The review checked the complete implementation diff against the stated base, current P12 capture contracts, payload/definition identity, alias handling, empty and non-empty selected slots, census cardinality, wire compatibility, unselected fallback, test integrity, and the evidence/source identities. It found no actionable defect.

This is a bounded selected action-payload capture improvement only. It does not complete `DailyV1Application`, move all P12 capture/staging code into Core, establish detached restoration, complete the CORE-F runtime/host package, or authorize canonical promotion. The candidate remains subject to the remaining CORE-F integration contract and its later complete-package validation/review gates.
