# CORE-F Planned Local SellGoods Projection Recovery

## Candidate identity

- Candidate branch: codex/core/CORE-F-core-owned-runtime-application
- Implementation commit: 5d9ce19a7c4221dca385f2053f222dff5094e68a
- Implementation tree: 99a68f4f37bb9ae0b2159fe832bbfe98a50055e6
- Parent: ec659552f211390c2e227686d05c4af0bf4be574
- Core canonical during validation: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Origin: https://github.com/TorroisBr/Simulation

The implementation changes only SimulationRuntime.cs and SimulationRuntimeAdmissionTests.cs.

## Bounded behavior

When an admitted local active-plan SellGoods action retains its exact Core ActionSlot and payload but lacks its Unity NpcActionRuntime compatibility projection, SimulationRuntime can rebuild that projection from the admitted action, exact current City projection, and exact selected ItemData binding.

The reconstruction gate requires an active Merchant plan; the payload item must be the exact plan item; the payload City must be the actor's current City and the plan target; the amount must be positive and no greater than the plan remainder; and the action must have no NPC target. Missing or ambiguous City projections and missing exact item bindings fail closed before a random draw or owner write. A stale non-null projection remains an identity error.

Projection recovery installs only the compatibility view and does not notify P12 as an owner mutation. The existing Core selected planned-sale execution remains authoritative and uses the established market and Merchant-plan admission/commit callbacks. The regression verifies deterministic action-roll identity, account/inventory/market/plan revisions, the four corresponding mutation epochs, and final census.

Planned NPC sales, cross-City trades, unsupported Merchant-plan states, and other action types remain outside this slice. This does not complete CORE-F or promote Core canonical.

## Validation source identity

- Unity Editor: 6000.3.9f1
- Official runner: PowerShell 7.6.5, .NET 10.0.11
- All successful runs reported inputStable=true and the same validation-input digest: 783bb5941a1dfdf060665999f5418824f65421b2bdfb6d5f41de3224e25b16cc
- SimulationRuntime.cs SHA-256: 86bd847ca244dbd6605e0d8f37a18b91fa3f8654fff7751922c67e135158a8d2
- SimulationRuntimeAdmissionTests.cs SHA-256: 73a1fb70b1564ae70d5cbb550553fd2032314fc74ab71e3cdf36673485db69bb

| Gate | Run ID | Result | Original XML SHA-256 | Original log SHA-256 |
| --- | --- | --- | --- | --- |
| Focused planned local sale projection | 89cf1b70082048769f2f7f930ccdbe08 | 1/1 passed | cbb240c2bf3b6b28662a0cf69e5884563158debb72250184368cf8adfd0888d8 | ed2b3d24c1a28a04df6159f7f21b5b61364409a3dee2657a3b004a3c3b06f2d8 |
| Full EditMode | 5ec18537df4d45379b400f177b9075af | 3292/3292 passed | d1a90c3937eacd6b3c4080bf96685a6a47e0e661a9778e37c5eb40006feb7683 | 85c0dedb09aea054c2e47b052b08a78980588a02939d2c92b16ccb77f69cc381 |
| Official Smoke filter | 11be3f17680b4bf28bc1b46f65fba8df | 5/5 passed | 217d27786b2b5be55e11856a7369a68f3ada830f1f813f7126354b4dcbf4cf5d | 983409bf75cadd36a353a768c133a4e7f2050fa426b820a534953bf0e1945a28 |

Every successful gate reported zero failures, skips, or inconclusive cases. The complete EditMode report contains the Smoke identities.

Three intermediate test attempts are not counted as passing evidence: one failed because the fixture installed a plan before Person binding; one had transient compile errors after setup was inserted into the wrong fixture; and one reported OwnerCoverageIncomplete because the fixture mutated the Core plan owner without updating its Unity projection. Those attempts used intermediate test sources; the fixture now uses the established NpcRuntime selected-plan API. Their original reports and logs remain privately retained and unchanged.

## Evidence package

The approved result-only EditMode package is in UnityEvidence:

- Manifest SHA-256: 23fb2025bc63dd553ee3cd33a2ca5b39123deea808cdfb9571477e36790ab16c
- Archive SHA-256: aa5750d3a718cc21ac72ead01a6909bcb9b3693a811ccea83dfb46b6032ff48d
- Archive size: 173972 bytes
- Official verification: VALID against the protected original run manifest; one run and one normalized XML entry.

The archive includes the new test identity and contains no E: worktree, C: user-home, or private evidence-root paths. The filtered focused and Smoke originals, all raw logs, and the intermediate failed attempts remain in protected local evidence storage; none are included in Git. The package contains the complete normalized EditMode result XML. No source or test input changed after the successful runs. git diff --check passed for both implementation files.