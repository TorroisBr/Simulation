# CORE-F Selected NPC Core Materialization Binding

## Candidate and scope

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`.
- Base: `2ca23aab32bc71e40097f6e10b38579f0eb3fb44`.
- Implementation commit: `ebbc8aa8671d89fed20b5730a9114dbde01324ca`.
- Implementation tree: `55f3559d28c7a4d8d7f6b043200aa611d8e526c4`.
- Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

For selected Daily-v1 NPC materialization, Core now prepares the exact PersonStore binding against the admitted definitions, commits it under the existing outer `runtime.npc-membership` P12 scope, reports every PersonStore revision delta to that scope, and performs exact-state compensation if later registration or starting-City presence fails. Core's selected graph/actor-roster transaction remains the publication authority. Unity still constructs and projects `NpcRuntime`, owns host registries and City presentation, and opens the P12 census scope. The mixed/unselected compatibility path is unchanged.

The new Core regression covers no-mutation preparation, stale-plan rejection, successful PersonStore binding, and compensating rollback with the exact revision delta. The existing selected-profile Unity materialization regression exercises the integrated host path. Assertions were additive; no prior test expectation was weakened.

Changed source/test SHA-256 values:

| File | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedNpcPersonBindingOperation.cs` | `100c01570e11b3805493a25c1161317b8e1fd8bfd5ccf53c32a9e1f9202da59c` |
| `Assets/_Project/Scripts/Person/PersonMaterialization.cs` | `6e63a45581f0390a41e92b5ac43fb9bb84e8928a37b663b18caabd60ce66dc80` |
| `tests/Simulation.Core.Tests/DailyV1SelectedRuntimeOwnerGraphTests.cs` | `7aafcf570d4df87fce876ebba75bd8ce38b59b29d9e5d56d38e0d072024dd4af` |

## Validation

| Gate | Result | Evidence identity |
|---|---|---|
| CoreValidation cold | 1,102/1,102 PASS | run `d4ccd0737f864d29af27269a7c4f53a7` |
| CoreValidation warm | 1,102/1,102 PASS | run `d4ccd0737f864d29af27269a7c4f53a7` |
| Core, CalendarDemo and Lab gates | PASS | same default official runner run |
| Focused selected materialization EditMode | 1/1 PASS | run `c41cc0a64383404fb5dde4c649a6d668` |
| Full Unity EditMode | 3,318/3,318 PASS; zero failed/skipped/inconclusive | run `f1b2c651075341839951c87904b83c69` |
| Official Smoke filter | 5/5 PASS; zero failed/skipped/inconclusive | run `f38cdd8686ab4e3b998efc606bb0f631` |
| `git diff --check` | PASS | implementation commit preflight |

CoreValidation input digest: `91734003843b04aa68730fa1dc37e774b6d04b1adb52b22dd20fba081f90699f`; private Core manifest SHA-256: `ce349a337befadc44f479bec45206c3d7ab3fd2214a4e8b8a0d8e8ea15f5a9ca`. Cold/warm private TRX SHA-256 values: `a4c77e9c4d05cca64830ca9a292e3fce0c424377d1ac996a30a13cbb6272c046` / `125ec1ee6dc915f716543217fd20b7ad725d90991fd8eb4a68e06307e807704d`. The official runner's default Lab build and cold/warm output gates also passed. This checkout contains Unity `Library`; no Library-independent host claim is made.

The three Unity runs each recorded 1,982 validation inputs. Their sorted path/size/SHA-256 inventory digest was identical: `7bbea25538535dc7f7cfdcce685e83cd2ecec541a01ec477870cfc30a6f86e61`. Each before/after inventory was stable. The Smoke XML's five case identities and outcomes are an exact subset of the full EditMode XML; the focused materialization test appears once in the full XML and is Passed. Full EditMode XML SHA-256: `8d29d825b3b8f77340b1b2d49244cf2cb6e4dcc415ef92a0636c407e82f1eaf2`; Smoke XML SHA-256: `45969357829875c3687ad8e3a06af926d3da549ca99ea4519e664b30aa9c7dab`; focused XML SHA-256: `bb72013834ccccc52d5427477ebc0c099b7e27182b5279730d8625fe4d4dafb1`. The three private Unity run-manifest SHA-256 values are `ff95355d971864c5407658da08e9abed4ed301ea288e4e7232c78ab2f96990ab`, `c4a1dcc87550468055eb6e8de37149024731b8e18c05f5e907e64fd4c5670daf`, and `06fbd98644962c7b770faee2b843ca9b4578578c698b240b6d5bbfd1cbca822f` (full, Smoke, focused respectively).

The privacy-safe full EditMode package is [`coref-selected-npc-materialization-editmode.zip`](Unity/coref-selected-npc-materialization-editmode.zip), SHA-256 `5df8b15b8319b34535976511acc62bf309986cdda592d01d40443c189138019a`; manifest SHA-256 `a254a4e51e585003b7fcca53d077e92840e1e9ffdf30dbf777c6ac1bec71a008`. The official verifier passed with `runs=1`, `entries=1`. The package contains only normalized NUnit XML; original full reports and raw logs remain protected.

A combined bundle attempt for focused, full, and filtered Smoke XML was rejected by the official packager because a filtered suite's declared `testcasecount` did not match its descendants. No combined output was retained. The complete EditMode XML packaged and verified alone; filtered focused/Smoke XML and raw logs remain in protected private evidence. CoreValidation raw TRX and manifest remain protected because the Core runner's schema is not the public packager's required source-context schema; the manifest and raw report hashes above identify them without bypassing that boundary.

Validation ran against the worktree based at `2ca23aab` before the implementation commit was made. The official runners captured stable complete input inventories and the three changed-source SHA-256 values above match the implementation commit exactly. No source/test changes occurred after those runs; the commit added those exact validated inputs. The subsequently committed record and package are documentation/evidence-only. `ProjectSettings/ProjectSettings.asset` remained at SHA-256 `f0771a23c3ebcb50d651c13515ee66e6e56d321c6115f54a2193bf35dd994633` throughout validation and was not staged. Other pre-existing worktree changes and private evidence were also excluded from the implementation commit.

## Review boundary and limitations

This advances only selected PersonStore materialization binding and compensation inside the existing P12 membership operation. It does not move NPC construction, host registry/City presence, or the outer census scope into Core. It does not complete Core-owned daily execution/session composition, `DailyV1Application.TryStart`, full P12 capture/restoration, detached .NET hosting, process-separated continuation, or CORE-F acceptance. Core canonical was not advanced.