# Independent Review — CORE-F Selected NPC Core Materialization Binding

## Verdict

**VALIDATED_CANDIDATE** — no actionable implementation or test-integrity findings.

## Exact identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`.
- Candidate commit: `68699734ba26957987a438e8fa77a99c00cb2b32`.
- Candidate tree: `eb0af646fbb558a58bb1eb3f0ccddbd3224613f7`.
- Implementation commit: `ebbc8aa8671d89fed20b5730a9114dbde01324ca`.
- Implementation tree: `55f3559d28c7a4d8d7f6b043200aa611d8e526c4`.
- Exact base and validation snapshot HEAD: `2ca23aab32bc71e40097f6e10b38579f0eb3fb44`.
- Base tree: `ba220e9d76749d78b0bc589659db741d76f020d7`.
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Candidate and canonical refs were refreshed; remote candidate matched `68699734`, canonical remained unchanged.

## Review scope and findings

Reviewed the complete source/test/evidence delta from `2ca23aab` through candidate `68699734`, including the unchanged membership transaction introduced at `caee829` and the new selected materialization operation.

For selected Daily-v1 candidates, `PersonMaterializationSystem` now prepares a Core binding plan against the exact selected owner graph and `PersonStore`. Preparation checks definition and owner identity, Person registration, active graph membership, and duplicate binding state without writes. Commit rechecks the exact graph/store/person objects, current store revision, and binding indexes before using the existing guarded `PersonStore` mutation. It reports the committed revision delta to the caller. Compensation is conditional on the exact committed store revision and person/index bindings, then reports its own revision delta. Failure to compensate faults the enclosing membership boundary.

The Unity runtime keeps the existing outer `runtime.npc-membership` P12 scope, opens only the target Person lifecycle census section, and records every PersonStore revision delta, including rollback. The selected Core graph/ordered-roster transaction remains the publication authority for registering the new owner. The mixed/unselected path still uses the existing `PersonStore` binding flow; no selected Core plan is applied to it. Candidate host references are cleared on failed binding. No owner identity, lifecycle, residence, persistence, or compatibility semantics were changed by this slice.

The new Core test is additive and verifies non-mutating preparation, stale-revision rejection with zero write delta, successful binding, and exact compensation with both store revision increments accounted for. The complete test diff adds coverage and does not remove or weaken existing assertions. The full Unity XML contains exactly one passing `SimulationBootstrapCompositionTests.SelectedDailyV1PersonMaterializationReconcilesDynamicOwnerCardinality` case.

## Changed source/test identity

The validation record’s SHA-256 values match the current files and the implementation commit blobs:

| File | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedNpcPersonBindingOperation.cs` | `100c01570e11b3805493a25c1161317b8e1fd8bfd5ccf53c32a9e1f9202da59c` |
| `Assets/_Project/Scripts/Person/PersonMaterialization.cs` | `6e63a45581f0390a41e92b5ac43fb9bb84e8928a37b663b18caabd60ce66dc80` |
| `tests/Simulation.Core.Tests/DailyV1SelectedRuntimeOwnerGraphTests.cs` | `7aafcf570d4df87fce876ebba75bd8ce38b59b29d9e5d56d38e0d072024dd4af` |

The official runs validated the pre-commit worktree based on `2ca23aab`. Its complete Unity input snapshot has that exact base tree; all three changed code/test file hashes match the later implementation commit exactly. The implementation commit therefore records the already-validated source inputs. The candidate tip after it adds only the validation record and result package; no code changed after validation. The preserved local `ProjectSettings/ProjectSettings.asset` remained at SHA-256 `f0771a23c3ebcb50d651c13515ee66e6e56d321c6115f54a2193bf35dd994633` and was unstaged/excluded.

## Validation evidence inspected

- Official CoreValidation run `d4ccd0737f864d29af27269a7c4f53a7`: cold and warm **1,102/1,102 PASS**; Core, CalendarDemo, and Lab gates passed. Recorded input digest `91734003843b04aa68730fa1dc37e774b6d04b1adb52b22dd20fba081f90699f`; private manifest SHA-256 `ce349a337befadc44f479bec45206c3d7ab3fd2214a4e8b8a0d8e8ea15f5a9ca`; cold/warm TRX SHA-256 `a4c77e9c4d05cca64830ca9a292e3fce0c424377d1ac996a30a13cbb6272c046` / `125ec1ee6dc915f716543217fd20b7ad725d90991fd8eb4a68e06307e807704d`.
- Focused selected materialization EditMode run `c41cc0a64383404fb5dde4c649a6d668`: **1/1 PASS**; recorded XML SHA-256 `bb72013834ccccc52d5427477ebc0c099b7e27182b5279730d8625fe4d4dafb1`.
- Full official Unity EditMode run `f1b2c651075341839951c87904b83c69`: **3,318/3,318 PASS**, no failed/skipped/inconclusive; original XML SHA-256 `8d29d825b3b8f77340b1b2d49244cf2cb6e4dcc415ef92a0636c407e82f1eaf2`.
- Official Smoke run `f38cdd8686ab4e3b998efc606bb0f631`: **5/5 PASS**, no failed/skipped/inconclusive; XML SHA-256 `45969357829875c3687ad8e3a06af926d3da549ca99ea4519e664b30aa9c7dab`.
- The three Unity runs record 1,982 inputs each, stable before/after inventories, and the same separately computed sorted path/size/SHA-256 inventory digest: `7bbea25538535dc7f7cfdcce685e83cd2ecec541a01ec477870cfc30a6f86e61`. The public full-run manifest independently confirms 1,982 snapshot entries and records its runner input-snapshot digest `3aae09a3e4b6cf558eb386ace8853fbf81b21cca3cf25cf5f54aabfa609d893c`; these are distinct digest fields. The validation record states that the focused case and all five Smoke identities/outcomes are subsets of the full XML.
- Full Unity run-manifest SHA-256 `ff95355d971864c5407658da08e9abed4ed301ea288e4e7232c78ab2f96990ab`; Smoke `c4a1dcc87550468055eb6e8de37149024731b8e18c05f5e907e64fd4c5670daf`; focused `06fbd98644962c7b770faee2b843ca9b4578578c698b240b6d5bbfd1cbca822f`.
- Privacy-safe full EditMode archive SHA-256 `5df8b15b8319b34535976511acc62bf309986cdda592d01d40443c189138019a`; package manifest SHA-256 `a254a4e51e585003b7fcca53d077e92840e1e9ffdf30dbf777c6ac1bec71a008`; normalized XML SHA-256 `ad0235f21c1309c079588d7e5109bc8fbd886ddfe20166d998dea59814a1adc3`. The archive and manifest hashes match their records, and the normalized XML reports 3,318/3,318 passed. A scan of the published validation record, manifest, and normalized XML found no absolute Windows drive paths or user/private-workstation paths.
- `git diff --check`: PASS, recorded by the implementation preflight.

## Protected-evidence boundary

The protected Core TRX/manifest and filtered focused/Smoke originals were not available in this review worktree. Their run identities, counts, digests, and hashes above are taken from the committed validation record; I did not claim to open or independently rehash those protected originals. The full Unity package and its public manifest were available and verified. The Core runner’s evidence schema is not the public TRX packager’s required source-context schema, so no public Core TRX package was fabricated. This is a recorded evidence-access limitation, not a source or test failure.

## Scope and limitations

This is an exact-tip validated candidate only. It does not promote Core canonical or complete the CORE-F shared runtime/session, `DailyV1Application.TryStart`, full detached P12 restoration, executable .NET hosting, process-separated T/T+1/T+2 continuation, or CORE-F acceptance. Core canonical was not changed. No tests were rerun for this review.
