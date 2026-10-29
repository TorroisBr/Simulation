# Independent exact-tip review: P12-D Person and Genealogy restore integration

## Verdict

**VALIDATED_CANDIDATE** for this bounded Person/Genealogy restore integration. This review does not approve Core-canonical promotion or claim CORE-F completion.

## Exact reviewed identities

- Repository: `https://github.com/TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate commit: `ee8c7bd41f8a44bdd97ffc68614c4ea2b6f115f1`
- Candidate tree: `66a30a593a0703a291b6bb0f4b584c7ebebbe271`
- Immediate base: `6c8fa641fdf022094f68700a1e16b5a23c614e7c`
- Base tree: `3646fef48b28c069840984466a13139b4f8a3a48`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea` (`codex/core/canonical`)
- Remote candidate ref pointed to the exact reviewed commit when checked.

## Reviewed diff and payload

The complete base-to-candidate diff contains exactly 14 paths: 8 production source files, 3 test source files, and the three scoped validation files in `docs/validation/CORE_F_CoreOwnedRuntimeApplication/PersonGenealogyOwnerStage_20261029/`. No `.meta` files, ProjectSettings, private evidence, or unrelated/untracked data are committed. The candidate makes no Core canonical or Phase-closure claim.

The validation record's tested-source manifest lists 11 implementation/test files and a changed-file-set digest `55364fc838af61eac6234b3f05796d6b2e352c3bc32e15ab125215b5257c8409`. Each recorded source hash matches the corresponding exact-source CoreValidation or Unity input inventory. CoreValidation input digest is `1a3701156b6ee22aba627a1ba3001bf9b38c707bbb218ddc760b26e4c41ca39c`; Unity full and Smoke input digest is `050df564ebbfef4db4d57dafc508963c015a028591f660725a09f42b94bcea64`. Both runners reported stable inputs from base `6c8fa641` and tree `3646fef`.

The repository uses `core.autocrlf=true`; these files have no path-specific text, EOL, or filter attributes. Eight tested-worktree file hashes differ from the corresponding Git blob content hashes because Git cleans CRLF to LF when committing mixed-line-ending files. Three file hashes are byte-equal. This is the repository's ordinary text normalization; the reviewed candidate tree contains the same source/test paths and code changes, with no non-line-ending post-test delta identified. The recorded tested-worktree hashes and candidate tree identity are both retained here and in `VALIDATION.md`.

Candidate Git blob identities for the 11 source/test paths:

| Path | Candidate blob |
|---|---|
| `Assets/_Project/Scripts/P12DDailyV1OwnerPackage.cs` | `ec75d30acbaea83de2f266eb8a1d21c5e8b86d1c` |
| `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.ImageRestore.cs` | `d25dc346bb3596741edeb2d1a6f409804c0a6178` |
| `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.cs` | `29a94a1fbca59762509e64c847d7711b7da6fbab` |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `01d42fbd52b2b0c0f4d8152f375ee63e497b5b9a` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `84a20e53e1caf0a54f27e7ea617d616b08e29543` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | `9667a8fc18418b4faf66e000a2f90c4cb46419b2` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1RestoredExecutionBoundaryComposer.cs` | `0633ff938774761abaae0843341d0dbdd2239eb9` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedRuntimeOwnerGraph.cs` | `6b1a4d98c9eab764c80f50c168ddd9f00997834c` |
| `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12DSelectedCoreOwnerStager.cs` | `f13e8dee4cd3dd90eb0a8e31e70aff7a7b9a9d13` |
| `tests/Simulation.Core.Tests/DailyV1GenesisComposerTests.cs` | `3aaeff8bab7e1af1803c1e0598b91cafaef87359` |
| `tests/Simulation.Core.Tests/DailyV1P12DSelectedCoreOwnerStagerTests.cs` | `0506dc9dc3bc54e3d2f40047dce08bd4649e39ac` |

## Code and test findings

- The application restore path passes the exact snapshots from the single admitted image decode to Core's P12-D stager and to the subsequent Unity projection package. The new Person/Genealogy stage is built from those same decoded values; it does not initiate another decode or use a second persistence representation.
- Person hydration preserves row order and IDs, dates, residence, materialized-NPC bindings, aggregate revision, and each Person lifecycle revision while rebuilding derived indexes without replaying gameplay writes. Genealogy hydration validates local edge shape/cycles/duplicates and restores exact edge values and local revision without replaying mutations.
- The merged P12-D relationship validation checks reciprocal Person/NPC materialization and residence links plus Genealogy endpoint membership before candidate completion. The owner stage remains detached and unpublished until the package and whole restored candidate pass their checks.
- The Core owner graph retains the exact staged PersonStore and GenealogyStore. The application-backed SimulationRuntime constructor checks that its supplied GenealogyStore is that exact graph owner and retains it rather than cloning. The no-application compatibility path supplies no Core-bound Genealogy owner and keeps its prior defensive clone behavior.
- Failure paths discard only private staged owners; the active source session is unchanged until final boundary publication. Added tests cover exact owner binding, row/edge/revision preservation, malformed owner snapshots, and post-restore continuation parity. Existing assertions were not weakened.
- No actionable implementation or test finding remains.

## Validation evidence checked

- CoreValidation: cold/warm 933/933 PASS, with zero failures; original TRX hashes and logs match `VALIDATION.md`. Protected run `2cb99c0443394e9d8f44f6abd518297c`; protected manifest SHA-256 `588679f9c18e5dbefc64d6b1b7e66f761904507c5289c44c4bffe849b8afd8c`. Manifest source base/tree and stable input digest match the committed record. The protected CoreValidation manifest and reports remain outside the candidate.
- Focused persisted-image restore: 1/1 PASS, with run/manifest/XML/log hashes recorded in `VALIDATION.md`.
- Full Unity EditMode: 3279/3279 PASS; protected run `b4edb2fd82d04c56bcf217e0801402aa`, stable input digest above. The package contains one normalized full-result XML with coherent totals of 3279 passed and zero failed/skipped. Archive SHA-256 `e7438118a2f012a0077c8d15c922b975eafcdb418beb412fb0cc4a595ceffc50`; manifest SHA-256 `4e2da3575e8ff9bf7618d5f380ac775ebd409aec3f938323254be263a845a7be`; both match the committed declarations.
- Official Smoke: 5/5 PASS, stable on the same Unity input digest. Protected run `a2326dd84c644089aaf0c068edc5132a`, manifest SHA-256 `96f0bc22e9182188303f0d678f4ba50992b5418c7fe6bf88238bc447d8d2ff04`. All five Smoke case full names and passing outcomes also occur in the verified full EditMode package.
- `git diff --check`: PASS.

The committed validation text and Unity manifest were scanned; they contain no local workstation path, credential, or secret pattern. Raw CoreValidation and focused/Smoke originals remain protected and were not included in the outgoing commit.

## Scope boundary

This review covers only the exact P12-D Person/Genealogy restore integration above. It does not establish complete CORE-F application/host integration, P12 or Phase closure, or Core canonical promotion. Promotion remains a separate operation and must use refreshed current refs and the exact reviewed tree.

