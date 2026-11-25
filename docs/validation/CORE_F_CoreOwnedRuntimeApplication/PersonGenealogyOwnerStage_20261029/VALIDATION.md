# P12-D Person and Genealogy Restore Validation

## Scope and source identity

This record covers the bounded P12-D restore integration that stages `PersonStore` and `GenealogyStore` from the admitted decoded image, preserves those exact Core owner instances in the restored graph, and binds the Unity compatibility projection to them. The legacy no-application restore path remains unchanged.

- Candidate branch base: `6c8fa641fdf022094f68700a1e16b5a23c614e7c`
- Committed base tree: `3646fef48b28c069840984466a13139b4f8a3a48`
- Changed implementation/test inputs: 11 files listed below
- Changed-file set digest (sorted file path, byte length, SHA-256 records): `55364fc838af61eac6234b3f05796d6b2e352c3bc32e15ab125215b5257c8409`
- CoreValidation working-tree input digest: `1a3701156b6ee22aba627a1ba3001bf9b38c707bbb218ddc760b26e4c41ca39c`
- Unity full-project input digest: `050df564ebbfef4db4d57dafc508963c015a028591f660725a09f42b94bcea64`
- Core and Unity runners reported stable inputs before/after execution.

| Changed source/test file | Bytes | SHA-256 |
| --- | ---: | --- |
| `Assets/_Project/Scripts/P12DDailyV1OwnerPackage.cs` | 52785 | `b6f86760a9f26bdadd0fc8b70e044dec1c806026cf67e936556c00d63d3ac278` |
| `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.ImageRestore.cs` | 35830 | `c6054b8b4103199024e47d1f0829986c365a8d60c8e000d45e46438b5d1547ac` |
| `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.cs` | 98966 | `63fa990983d202da5a0a53a666ec6716660757931591b037015561ec47a62a83` |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | 715775 | `c25e699355456ef2dbb16eb39473563eb310a9f6cd4ef723bea56ba5e10bee13` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | 820827 | `ab0e4b988298d5e072448381fc8f8d65a46451461760bb3edfda994927132334` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | 90558 | `726d49ff26d12f26968d098112d1d6e9ee78bb1f16f4ab5edefecd309ee084f8` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1RestoredExecutionBoundaryComposer.cs` | 7427 | `15939fcd9d91c6d70430262145fc699a439fbfd4f57b665dd0945d77770e3e76` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedRuntimeOwnerGraph.cs` | 135592 | `66bcaa909539dec30534c209619997c53e2d2da44831a5c2c160a17e4cfe2947` |
| `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12DSelectedCoreOwnerStager.cs` | 19986 | `aadc4a9f353426069ef9a7610951e4035a2198a65a9ccc1071dda168081929c5` |
| `tests/Simulation.Core.Tests/DailyV1GenesisComposerTests.cs` | 28941 | `358e6425d01efe58faceb5748ede12f9e05e6918da6da0954ab3d82f417fc02b` |
| `tests/Simulation.Core.Tests/DailyV1P12DSelectedCoreOwnerStagerTests.cs` | 23931 | `2c5794ec36c85ac61c8657af04dee7060a4d4b65e272e3d1dd04714c771d77c0` |

## Validation results

### CoreValidation

Official CoreValidation cold and warm suites passed: 933/933 each, with zero failed, errored, timed out, inconclusive, or unexecuted tests.

- Runner ID: `2cb99c0443394e9d8f44f6abd518297c`
- Cold test run: `913f0eaa-a1ae-4a1e-bb52-fe041c6561c6`
- Warm test run: `f49da52f-be98-411a-8047-d4fbd1e233df`
- Protected runner manifest SHA-256: `588679F9C18E5DBEFC64D6B1B7E66F761904507C5289C44C4BFFE849B8AFD8C6`
- Cold TRX SHA-256: `68c9bf04ba107a9b2b937a39bbab4a16a130fa062355e2e88a1b701a13af877e`
- Warm TRX SHA-256: `f22214dd9dd44cd3f71ae52184e8e879c9e338b6027e8fc4ae9d230b70f4f109`
- Cold test log SHA-256: `971ab02732203c3453ae1c2e9d0da96637af2fa59fa5a2c64dd2cfa473336f6b`
- Warm test log SHA-256: `7cc5f718d9f0322806537e6714464924c050f0030d238f065ba6e68fde2eb0bc`

Original TRX and raw logs remain in protected private storage. This run used the existing Unity Library; it does not establish Library-independent .NET validation.

### Unity restore regression and full EditMode

The focused persisted-image restore regression passed 1/1. Full EditMode passed 3279/3279, with zero failures, skipped, or inconclusive tests. Input snapshots were stable.

- Focused run: `f3e3d6304e754a75a3053d61f22e9224`
- Focused protected manifest SHA-256: `634071322362CBCCBB17005075FD8365876C661CFBEAEFBF22DDDF7763445842`
- Focused XML SHA-256: `8833dd0b48ef8e8af151efc84c25300517fee049015c4838967ee8d7bea95027`
- Focused raw log SHA-256: `aec9e902c20771c77b797316b661ddac2e8d04e1bb756096ac7906f70d1725b1`
- Full EditMode run: `b4edb2fd82d04c56bcf217e0801402aa`
- Full protected manifest SHA-256: `1FE0FC8B143D008B8AD4582D2EDDBBCF1E7481E852E967C13AEC01A24F127559`
- Full original XML SHA-256: `1cc45d90f6064ca025b76f63e1f01831740cc6b64ea40ac10377e661e0a9c671`
- Full raw log SHA-256: `0309486e2a5d813c8addf0947c33165dddb869b906640d60d1f79fe7f877f729`

Privacy-safe public evidence is the full-run ZIP and manifest in this directory. The package verifier passed (`runs=1`, `entries=1`):

- `coref-person-genealogy-editmode-full.zip` SHA-256: `e7438118a2f012a0077c8d15c922b975eafcdb418beb412fb0cc4a595ceffc50`
- `coref-person-genealogy-editmode-full.manifest.json` SHA-256: `4E2DA3575E8FF9BF7618D5F380AC775EBD409AEC3F938323254BE263A845A7BE`

The public package uses the approved local-path redaction policy. Protected original reports and logs remain unchanged and are not included in the candidate.

### Official Smoke

Official Smoke passed 5/5 with zero failures, skips, or inconclusive results; the Unity input snapshot was stable.

- Run: `a2326dd84c644089aaf0c068edc5132a`
- Protected manifest SHA-256: `96F0BC22E9182188303F0D678F4BA50992B5418C7FE6BF88238BC447D8D2FF04`
- Original XML SHA-256: `3b0c36b9da99c45bd6db3f317d9078b24166b23f92ad81abed6d97cd9fb060a1`
- Raw log SHA-256: `3a9c5b2cc7cc21db5efff0a4bdb7a2659cd757544ce9283c2ad91c050305b605`

## Integrity and limitations

- `git diff --check` passed for the implementation snapshot.
- The public Unity evidence bundle verified successfully; private originals remain protected.
- ProjectSettings changes and unrelated untracked/private files were preserved and excluded from the candidate staging manifest.
- This is a bounded implementation checkpoint only. It does not close CORE-F or promote Core canonical.
