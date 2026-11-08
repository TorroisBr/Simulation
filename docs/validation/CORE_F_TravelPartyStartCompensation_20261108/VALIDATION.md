# CORE-F TravelParty start compensation validation

Date: 2026-11-08
Status: PASS for the compensation correction; exact-tip review pending

## Candidate identity

- Base: `d7d697c628838decb10390beb5679f1b8071807a`
- Corrected implementation commit: `aab04809c465fe110d9424c3f3500d70f86afc82`
- Corrected implementation tree: `a566a7f3a423aa0165374db908e13cbe39853929`
- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`

## Review finding resolved

A later independent exact-tip review of candidate `acd4b94f795ecec0d5824097ed4ec4a512f46310` found that the selected Core receipt accepted only a TravelParty-store cardinality increase. The existing failed-start rollback adds a party, then removes it after a later failure. That valid compensating removal increments the owner revision while restoring the initial cardinality, and was incorrectly rejected.

The Core protocol now records the initial store cardinality and admits at most one initial add receipt followed by one compensating removal receipt that returns to that initial cardinality. Each receipt still requires exactly one revision increment. A second add/removal or a removal without the preceding add is rejected. The new Core test exercises the selected owner graph, add receipt, compensating removal receipt, restored cardinality, two revision increments, one final P12 notification, and healthy scope disposal.

The earlier PASS and the later NEEDS_CHANGES records for the previous tip are both preserved. The later finding supersedes the earlier acceptance until this correction receives a fresh exact-tip review.

## Validation results

| Gate | Run ID | Result |
|---|---|---:|
| CoreValidation cold | `36c536e000c843a2a2076f19062a538b` | 1,060/1,060 PASS |
| CoreValidation warm | `36c536e000c843a2a2076f19062a538b` | 1,060/1,060 PASS |
| Unity TravelParty start/P12 focused suite | `0892aca989e749f3983b18c47d2a2a6a` | 16/16 PASS |
| Full Unity EditMode | `36dd405007de4804b5b27aee445f0e89` | 3,300/3,300 PASS |
| Official Smoke | `1fc0679dac694af7bd41006e36be1653` | 5/5 PASS |
| `git diff --check` | corrected source | PASS |

All failed, skipped, and inconclusive test counts were zero. CoreValidation reports `libraryAbsent=False`; this evidence does not claim Library-independent validation. The user-owned `ProjectSettings/ProjectSettings.asset` modification remained outside the candidate and was not changed. Unity''s project-input digest remained stable before and after each run.

## Corrected source hashes

| File | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs` | `8722740ebc8a697827994f33851df6e123bf6c67594a01b98d72e93c6a94777f` |
| `tests/Simulation.Core.Tests/DailyV1SelectedSoloTravelStartOperationTests.cs` | `59cf1b6f125d06f3fda6cbcf368412be14f363ffaa8f7ac5ad934234e2424a5b` |

CoreValidation manifest source identity: base commit `d7d697c628838decb10390beb5679f1b8071807a`, committed tree `f3824b512426d9f8e465793193ff8ff7222eb36d`, working-input digest `c655f208b6ff9567ee0b8d9370d07d15ca78eef5b184d1e572fbde0e63a2aab2`.

The Unity runner reported stable project-input digest `d8c10bb952796a8ab4d5541ea8228354cf7acfae3b288e4aad5085db5264913e` for all three runs. The changed Core boundary hash matches the Unity manifests and CoreValidation manifest; the new Core test hash matches CoreValidation.

## Private evidence integrity

Original TRX/XML reports, private runner manifests, and logs remain in the private validation store. This record contains only run IDs and hashes; it does not include workstation paths or private runner metadata.

| Evidence | SHA-256 |
|---|---|
| Core cold TRX | `75420ca8d70cb46e2c054378617284f6add9c1ae127b66a77664ee2421840d30` |
| Core warm TRX | `e42eba3143a23e7c2d5734961bed7d6f0208761e3e2fd2f9287037863906b243` |
| Core manifest | `6ecec5de5b1311beaa6e5254db2fb382c85dcbd26b3551fce85df0f4c6fa9242` |
| Core cold build log | `52cfbbde65c6d571cac19de4f376d652a84289034196be2f4dd6075afe1fecbd` |
| Core cold test log | `035d84610ed9938304bc87852ecf50cc77ba754e6722f6c018f259dca65ed3ed` |
| Core warm test log | `c3b65d5b7f9b726c6bde6af6cc276ccb07dd95a7aa043f89b12a8cbb6d541392` |
| Focused Unity XML | `689641cef1d91d6dc2dc7735199ff864c0df434096c3a6b46829ee1cca1f5d2a` |
| Focused Unity manifest | `b0d7bb28be35eb215c3637c9c3498b407124244e97fbdccd68a9065ddfea60ae` |
| Focused Unity log | `fd62021c3c2699cfe81ea187a395995888e9426af00d0609d680645375e77793` |
| Full EditMode XML | `4ae46f02234a680f484642f4a22c283438e4451c964fc0a8cc3681115088a727` |
| Full EditMode manifest | `04b41b3d9e1a3c0d3b9ee635a703511c884447a0c8ecf00b015412384423ec7f` |
| Full EditMode log | `48a11b70493b3313fbc264d8b27d9c0bdf5e654b9f5bb38a6c3926d6efb5ce06` |
| Smoke XML | `b5bd51f5fa5567233cc9829e0da08e597ee3ddacbbadbf4a1dd1a4829709f8d9` |
| Smoke manifest | `e3bd68baf7076e93db6ae0dd99fcc5eb9aa34116a857d41ccaecc309fe1805a7` |
| Smoke log | `f496e3582ec45d71b539c1a26c7333c0d043b7fcf8fd9445631599ff41982e25` |

## Scope limits

This is the bounded selected TravelParty-start compensation correction only. It does not complete CORE-F, does not establish the public DailyV1Application/TryStart, detached .NET host, or full Core-owned action execution, and does not authorize Core canonical promotion.
