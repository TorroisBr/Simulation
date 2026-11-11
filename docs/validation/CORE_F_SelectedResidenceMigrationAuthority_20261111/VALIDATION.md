# Selected Residence Migration Authority Validation

## Candidate change

The selected Daily-v1 route now prepares and commits residence migration from the exact Core owner graph. Core binds the selected NPC, origin City, destination City, and either the PersonStore-backed residence owner or the NPC-only residence owner. The adapter compares the supplied legacy transition against that Core-prepared plan before admitting the P12 operation. Core then commits the residence owner only after both City population writers pass preflight and immediately before the existing paired City writes; the P12 scope publishes one completed operation boundary.

The route preserves the registered `runtime.population.residence-migration` operation and its exact change set: origin and destination aggregate rows plus one Person-life/residence or NPC-residence row. The non-Person Unity projection still receives its normal post-commit notification. Mixed/unselected rosters remain on the scoped compatibility path. With P12 admission active, inability to build the exact operation scope now faults closed before any fallback write.

Regression coverage exercises selected Person-backed and non-Person residence owners, both City revisions, one residence revision, one epoch, census consistency, and a forged transition rejected before any owner write. The first test attempt found two fixture setup omissions for non-Person initial residence; the fixture now uses the established resident-binding path. A subsequent full run found two existing reflection lookups made ambiguous by the new callback overload; the new API was renamed to `TryApplyPairedMigrationWithResidenceCommit`, and the full suite passed afterward. No assertions were weakened.

## Source identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Base candidate commit: `551d977151352ef8fe0df4a95aeac071b48fe579`
- Base tree: `0d3037197fe16015504cd805499c8887dbed2d6e`
- Unity input digest for focused, full, and Smoke runs: `1709ff8edccd7d7aa66069e76984a2c2fb8c9789654580ccbaf33fcc3006b334`
- All three official Unity runs report stable before/after source snapshots. Every changed source/test file below was re-hashed and matches the final full EditMode snapshot.
- CoreValidation working-input digest: `c3a5c24f621a5b08993b5d90716816378de8a35f1c932469ceea903edb44fa25`

| File | SHA-256 |
| --- | --- |
| `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedDemographyStage.cs` | `b571834984b7c2ce5713abb131c5134fd3d6e823177048c90d35251f5055baaf` |
| `Packages/com.simulation.core/Runtime/Population/SettlementPopulationRuntime.cs` | `880238e1fac5b017f9372dd259f9c720fc09af712c0fc1c1703170a0890d990e` |
| `Packages/com.simulation.core/Runtime/Population/PopulationOwnerBridge.cs` | `5e92b4b8381161892b66392473a6819b0bdb80f94af6244963cd1deca48c4651` |
| `Assets/_Project/Scripts/NpcRuntime.cs` | `f78f854fec8d7ed8f0954ff61413ac877caca5e03a2dde3ecd5c7f973894a082` |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `2617ae941e9c8a6cf4db86ec679a25d8786ef9a8840564a60ed47533844be349` |
| `Assets/_Project/Tests/EditMode/Editor/P12PopulationLifecycleInvalidationTests.cs` | `abcac68361ff95670dc44211983a66e98e6bc813150510de7b4ee404231d1e2a` |

## Validation results

| Validation | Run ID | Result | Input identity |
| --- | --- | --- | --- |
| Official CoreValidation (`-SkipLab`) | `d33f961d4d0640049df907d820ab60d7` | 1,092/1,092 PASS cold; 1,092/1,092 PASS warm | `c3a5c24f621a5b08993b5d90716816378de8a35f1c932469ceea903edb44fa25` |
| Focused selected-migration tests (`CoreSelectedResidenceMigration`) | `4ed969ede032458ca8b0293bdd89ddf5` | 3/3 PASS | `1709ff8edccd7d7aa66069e76984a2c2fb8c9789654580ccbaf33fcc3006b334` |
| Full Unity EditMode | `1beb6a3f3c7d4bf1bda57a31b1b560c2` | 3,311/3,311 PASS; zero failed/skipped/inconclusive | `1709ff8edccd7d7aa66069e76984a2c2fb8c9789654580ccbaf33fcc3006b334` |
| Official Smoke filter | `bcf0a81876034e429587594006843670` | 5/5 PASS | `1709ff8edccd7d7aa66069e76984a2c2fb8c9789654580ccbaf33fcc3006b334` |
| `git diff --check` | local check | PASS | candidate source/docs changes |

The earlier full run (`50bd8949947242dab5db425e8bc0b0cf`) reported 3,309/3,311 because existing tests reflectively selected the old paired-migration method by name; the new overload caused `AmbiguousMatchException`. Renaming the new API removed the ambiguity. The follow-up full run above passed. A later P12-class XML reported 28/28 but the process did not exit; after the official runner's bounded cleanup, its evidence wrapper could not read the still-locked raw log and produced no run manifest. That attempt is preserved privately and is not counted. The standalone new-test run and finalized full EditMode run provide the accepted results.

## Public Unity evidence package

The approved packager accepted the full EditMode report. Filtered focused and Smoke NUnit XML files carry full-suite intermediate suite counts, so the packager correctly rejects those filtered XMLs; their original reports remain private and are represented by the official run manifests above.

- Archive: `Unity/coref-selected-residence-migration-full-editmode-20261111.zip`
- Archive SHA-256: `ea9f3cb90e7c86b13290f24571ee8e2db75d1c7fe665e40bb8db2a57e19ceca7`
- Archive size: 175,485 bytes
- Package manifest SHA-256: `9d1fa0f44ecdf7814d089f82a1d215fecb5f23b868ad4bb09069bb2355f436d0`
- Official package verification: PASS; one run, one XML archive entry.

CoreValidation's raw cold/warm TRX and logs remain in the private owner-only evidence store. Its `simulation-core-validation/v1` manifest is not accepted by the current `dotnet-trx-source-context-v1` public normalization adapter. No raw .NET TRX/log is published and this checkpoint makes no normalized-public-.NET-evidence claim. Private manifest SHA-256: `27788e9119fde613545eddfe4d9644ba96de0268b0dbf110ddd4b9eaa16014fc`. Cold TRX SHA-256: `c87b6816777b2ed456021162cde64e136bd8530fe2047ae880ca5f97447f74a2`. Warm TRX SHA-256: `132a6f300518c7d321189b6169a0502c06879428eb80444b719d175429cecb71`.

## Scope boundary

This is an intermediate CORE-F implementation checkpoint. It is not an exact-tip independent review or Core canonical promotion. The shared runtime/application/restore/host contract remains incomplete. The pre-existing ProjectSettings edit and protected/untracked evidence remained unstaged and untouched.
