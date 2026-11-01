# Selected planned NPC SellGoods dispatch validation

Status: **validation complete; independent exact-tip review recheck pending; not Core canonical**.

## Source and scope

- Repository: `https://github.com/TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Core canonical baseline: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Exact implementation/test commit: `008a910c6f43542e87ac2763594c957063f85c11` (tree `a74adaffc6162232100cf6353f56cfd40b6888d0`, parent `6bae7cf7ba43a485fb0ea7ad866a001658d10b34`). Later candidate commits are documentation/evidence-only; this implementation tree is unchanged.
- Validated implementation/test file SHA-256:
  - `Assets/_Project/Scripts/SimulationRuntime.cs`: `81e37cbcc49e81e00d92219e1c767cd51210de4f893a8bd2d227286c3a8c4f88`
  - `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedActionExecutionStage.cs`: `fa7a89cdfbea4f54074c05131c8828701509e3086eca5cac073d5f97bceb6376`
  - `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`: `932b1566d74607b069242637e7a33fc85466daad5d028e017e00346432c7d7da`

This slice restores the Unity compatibility projection for an exact Core-slot planned sale to an NPC. The adapter reconstructs a view from the selected Core slot and does not execute or own the sale. Core dispatch admits only the supported planned `SellGoods` shape (active plan, nonblank target NPC identity, null target City identity); other market actions retain the City-target requirement.

Failure-path coverage verifies missing or duplicate seller City projections and missing or duplicate target NPC projections reject before RNG, owner revisions, or P12 epoch changes. The success path verifies exact target/item projection identity, deterministic sale results, four trade-owner writes plus plan commit (five expected P12 epoch advances), no Market mutation, and final owner census. No assertion was weakened.

## Validation

- Focused planned NPC SellGoods dispatch: **1/1 PASS**, run `d66cd5a44f8747279ddb4acf5336af93`.
- `SimulationRuntimeAdmissionTests`: **240/240 PASS**, run `5b6a289df7cd46ae9cd812096e23b19e`.
- Official CoreValidation on a clean worktree at the exact implementation commit/tree: **987/987 cold and 987/987 warm PASS**, run `527733afd106403e9e49a1ff8cb94bc0`. The input snapshot was stable (`268ea0230a6a39006e7f7d172c5c559a2324134e1ce7725273729e76667601a1`), with 537 inventoried source/input files and three generated dependency inputs. Every test outcome other than Passed was zero. The supported `-SkipLab` profile was used for this packageable TRX run; Lab was separately run below.
- Official fixed Lab validation on the same exact implementation commit/tree: **PASS**, run `ed2da0d4fddd4b838164e9702ea87a1c`, SDK 9.0.315, `net8.0` Release, no Unity Library. Locked restore passed; build completed with zero warnings/errors; cold and warm outputs matched exactly, including the three-day reserve/stock/price-bit sequence. Protected Lab manifest SHA-256: `58ff9316564d3ed7cfa7654a5987c9bb613e9f1ab2ff2eadc253af413e135145`.
- Full Unity EditMode: **3293/3293 PASS**, run `14bea138614e4020b2ec9d8c649fe354`; official Smoke: **5/5 PASS**, run `80598d2ec72b44ca9bb7a3885be27f9e`. Both recorded the same stable Unity input digest `3d391df5808011d73fa4b47379d79d4c0e1fa3099428a630ed302228b371dfe6`; the three validated implementation/test file hashes above match the committed candidate.
- `git diff --check`: **PASS**.

The first focused attempt (`ce3ae79d3e7b4ab0856a7eb15ea0a174`) failed and is retained as diagnostic evidence, not counted as a passing gate. It exposed that the generic Core merchant dispatch guard rejected a null City target before the existing planned-NPC-sale operation. The narrow guard correction described above was made, after which the focused test and all listed regressions passed. Its original XML and log remain in protected local retention; their SHA-256 values are `75c6255399513f2b3423a3d5c4d81552181307d3632dd38fe2132a043e5c9e31` and `793959d7d473d3c87089f07cef7faac6fd270e5985806b3579a5165d5fe55996`.

## Reviewable evidence packages

### Unity

The package contains the normalized full EditMode result. Filtered XML was not packaged because the Unity evidence packager rejects a filtered report whose outer assembly suite retains the full suite count. Focused and class-filtered originals remain in protected local retention.

- Manifest: `coref-planned-npc-sell-editmode.manifest.json`, 472,946 bytes, SHA-256 `fb1c1e19189c141e7a7b286cd99efdac7daa1740aefa52b9562f57a56561d356`.
- Archive: `coref-planned-npc-sell-editmode.zip`, 173,992 bytes, SHA-256 `92ad739e71936ea1a53b4ba5e6eb2959c5c9c3af3aaa06fcb39dd0d26b8b1d0e`.
- Official verification: `VALID`, one run, one normalized XML entry, matching archive hash.

### .NET CoreValidation

The original cold/warm TRX reports and runner manifest remain immutable in protected local retention. The official shared packager created the public `validation-evidence-trx-package-v1` package from the approved private source context. Its official verifier checked both archive integrity and original-to-normalized comparison (`originalComparisonVerified`).

- Package manifest: `coref-planned-npc-sell-corevalidation.manifest.json`, 234,039 bytes, SHA-256 `32e42f269701c334f213cea3c011f8606dcdcaf6c2873307827ac6dd35e1e927`.
- Archive: `coref-planned-npc-sell-corevalidation.zip`, 469,597 bytes, SHA-256 `66d6b06b0a2797db2cded37d60558d9618dae0c57b93e9e21076215a98c5dfed`.
- The archive has separate normalized cold and warm reports, each with all 987 cases accounted for: 987 Passed and zero in every other outcome. Original report lengths and hashes, normalized-entry hashes, exact source commit/tree, stable input digest, producer identity and comparison verdict are recorded in the public manifest.
- The official verification returned `VALID` for one run/two report entries with original comparison verified. A privacy scan found no absolute local filesystem paths or credential terms in the outgoing package.

Raw runner manifests and original reports are not included in public bundles. Their access-controlled retention remains separate from the normalized result packages.

## Boundary

This candidate slice is limited to planned NPC SellGoods projection recovery and its corresponding Core dispatch gate. It does not complete CORE-F, claim detached-host or DailyV1Application integration, or advance Core canonical. No ProjectSettings, unrelated source files, user modifications, or protected evidence were included in the candidate scope.
