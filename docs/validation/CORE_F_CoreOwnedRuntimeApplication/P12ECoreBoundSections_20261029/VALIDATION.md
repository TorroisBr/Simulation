# P12-E Core-Bound Owner Sections

## Candidate

- Implementation candidate: `0773279fe27f08d919583d0047b78dbecc70fc6f`
- Candidate tree: `9f7ab078dc506963ea222a277f48bbfb7ccdd343`
- Base: `67e92e368470a314713649cf254f8a8b383557a3`
- Core canonical remains unchanged at `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

This bounded P12-E integration captures the selected institution/office, property/estate, faction, political claim/support/decision, military, conflict, war, and battle owners through the active Core continuation-save operation. The exact token and census-vector references are required. Captured snapshots are cached on that operation and reused by repeated staging calls. The encoder cross-checks each Core section against the existing Unity staging projection, then places the Core-produced sections in the continuation image. A mismatch fails closed.

Justice, crime-social appraisal, and receipt sections remain on their existing adapter path. This checkpoint does not complete P12-E, detached restoration, the .NET host, or CORE-F. It does not claim full EditMode, Smoke, LongRun, or full CORE-F acceptance.

## Source identity

Both official runner input snapshots were stable. The CoreValidation working-input digest was `dbf05e4191ad1f5c2920c8bf9e30c8ee057f4d3a781cf4369fdc2da95edb0ccb`; the Unity input digest was `b49bf9658128e33bda3cc3cc722b128ebf953a6c13c3e42e8074ab6f45a693e5`. Both runs used the same candidate base commit/tree listed above. The six implementation/test file hashes below match the Unity runner's captured inputs and the committed candidate:

| File | SHA-256 |
|---|---|
| `Assets/_Project/Scripts/P12ADailyV1OwnerSectionEncoder.cs` | `5717270e5e51660447a86f0a7384cfde8065363b74b56609b7f0f377739d4e5f` |
| `Assets/_Project/Scripts/P12EDailyV1OwnerPackage.cs` | `bba39558ddd341577afdc7e52d29e517182432d4ce9f53728326c52c3456a362` |
| `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.cs` | `b69af21a7f983b1a36b07e285862e82c3f94ed5104a4d5bbd3327b96e4ec2d1c` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `700beb79a669f51431e3c4df1aca7a796443fad348a1ea0a6ca25e971a7645ed` |
| `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12ECoreOwnerSourceCapture.cs` | `f80dd7edcd042417f4abc57bb6b9998eff6e9137d5ba04f15f3344db32c01be0` |
| `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GContinuationSourceWitness.cs` | `4c16019d8e6469efbc7e06c9ad265450a3dfe889e54a8dc2b6f82872dbf0df65` |

## Validation

- Official CoreValidation, SDK `9.0.315`: **936/936 cold and 936/936 warm**, all failures, aborts, skips, inconclusive, and not-executed counts zero. The default runner gates passed. Run ID: `d3e1b1f1793244e5992b49c8d093dd44`; private manifest SHA-256: `9ad8294f873eda728568b75849f9bb9bd64e00816321fc7cb914af6e78ed6a49`; working-input digest: `dbf05e4191ad1f5c2920c8bf9e30c8ee057f4d3a781cf4369fdc2da95edb0ccb`. Cold and warm TRX SHA-256: `53bdabc7d5e96f67f9f89cbb14ac2f3c34f7c982672c28f4f60f88e0c433c964` and `ef173bd8270d4eb0a53dff2afd389ab684661a804e823bd1910f91bc6eb70b7d`.
- Official Unity EditMode filter `SimulationRuntimeAdmissionTests`: **228/228 PASS**. Unity `6000.3.9f1`; run ID: `6e360ec5720244d790558102ac576b9c`; input digest: `b49bf9658128e33bda3cc3cc722b128ebf953a6c13c3e42e8074ab6f45a693e5`; private run-manifest SHA-256: `50a2675bb4eae802a92ae91eb79f10106c04ae901441389db37c6c0c3f07ee91`; original XML/log SHA-256: `3949db11c2f4941e47609876f6f8c24913b6eda6ef0789ccfc9c405b8da1efed` / `b0e359ea9351d233ee0b9b3942ae0dbe51538ef4e07c9c1541ad4eca833d4137`.
- Official Unity EditMode filter `P12EDailyV1OwnerPackageTests`: **6/6 PASS**. Run ID: `1b5441c43b054b989f8f0b2708e7c7cd`; same stable input digest; private run-manifest SHA-256: `ccf28e9fcc0d013d9f9e5a22b37cc2527640ca97408e65f5e1b8e0b4c20acb67`; original XML/log SHA-256: `f188739c2f61964e080a44485e685faecbdffc19497e5bc31e3d343adb0cd0dc` / `87806376391f64e042a656a85dad41df62a6823bbcaa884e038e80cd6f06e853`.
- `git diff --check`: PASS.

The original TRX, Unity XML, logs, and private manifests remain in the protected local evidence store and were not added to Git. The official Unity packager rejected the filtered admission XML because its parent `Simulation` suite `testcasecount` does not match that filtered report's descendants. No normalized Unity package is claimed; the original report remains preserved and the official runner's stable input manifest is the validation evidence for this focused checkpoint.

The three unrelated local `ProjectSettings` modifications were preserved and excluded from the candidate. No raw evidence, workstation paths, or machine identity are included here.
