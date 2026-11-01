# Selected planned NPC SellGoods dispatch validation

Status: **validated candidate; not Core canonical**.

## Source and scope

- Repository: `https://github.com/TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Canonical base used by these runs: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Candidate worktree HEAD at validation time: `6bae7cf7ba43a485fb0ea7ad866a001658d10b34`; committed tree `6161b72c97773a27c60d75947fe1d2f544f39bb7`.
- The three implementation/test files were uncommitted during validation. The Unity input snapshot was stable and had digest `3d391df5808011d73fa4b47379d79d4c0e1fa3099428a630ed302228b371dfe6`. CoreValidation recorded working-tree input digest `a409942e0a23020d5e4db2a5e574c43363b264a723f62faab79e1a01f2d3f8b5` and stable source inputs.
- Validated file SHA-256:
  - `Assets/_Project/Scripts/SimulationRuntime.cs`: `81e37cbcc49e81e00d92219e1c767cd51210de4f893a8bd2d227286c3a8c4f88`
  - `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedActionExecutionStage.cs`: `fa7a89cdfbea4f54074c05131c8828701509e3086eca5cac073d5f97bceb6376`
  - `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`: `932b1566d74607b069242637e7a33fc85466daad5d028e017e00346432c7d7da`

This slice recovers the Unity compatibility projection for an exact Core-slot planned sale to an NPC and permits the existing Core planned-NPC-sale stage to receive its supported payload shape (`SellGoods`, active plan, nonblank target NPC identity, null target City identity). The Unity adapter reconstructs a view from the exact selected Core slot; it does not execute or own the sale. The Core dispatch exception is limited to that planned-NPC-sale shape; other market actions retain the City-target requirement.

Failure-path coverage verifies missing/duplicate seller City and missing/duplicate target NPC projections reject before RNG, owner revisions, or P12 epoch changes. The success path verifies exact target/item projection identity, the deterministic sale result, four trade-owner writes plus plan commit (five expected P12 epoch advances), and no Market mutation. The final owner census passes. No assertion was weakened.

## Validation

- Focused planned NPC SellGoods dispatch: **1/1 PASS**, run `d66cd5a44f8747279ddb4acf5336af93`.
- `SimulationRuntimeAdmissionTests`: **240/240 PASS**, run `5b6a289df7cd46ae9cd812096e23b19e`.
- CoreValidation: **987/987 cold and 987/987 warm PASS**, run `fd21a8c3f7b64f46a35575f0871b83d5`; both TRX counters report zero failed, skipped, aborted, timed out, or inconclusive cases. The standard run's required build, demo, and fixed Lab gates exited successfully.
- Full Unity EditMode: **3293/3293 PASS**, run `14bea138614e4020b2ec9d8c649fe354`; input stable.
- Official Smoke: **5/5 PASS**, run `80598d2ec72b44ca9bb7a3885be27f9e`; same stable Unity input digest.
- `git diff --check`: **PASS**.

The first focused attempt (`ce3ae79d3e7b4ab0856a7eb15ea0a174`) failed and is retained as diagnostic evidence, not counted as a passing gate. It exposed that the generic Core merchant dispatch guard rejected a null City target before the existing planned-NPC-sale operation. The narrow guard correction described above was made, after which the focused test and all listed regressions passed. The original failed XML and log remain preserved in the protected local evidence store; their SHA-256 values are respectively `75c6255399513f2b3423a3d5c4d81552181307d3632dd38fe2132a043e5c9e31` and `793959d7d473d3c87089f07cef7faac6fd270e5985806b3579a5165d5fe55996`.

## Reviewable Unity evidence package

The package contains the normalized full EditMode result only. Filtered XML was not packaged because the Unity evidence packager rejects a filtered report whose outer assembly suite still carries the full suite count. Focused and class-filtered original reports remain in protected local evidence storage. The full EditMode run includes the new test and all five Smoke cases.

- Package manifest: `coref-planned-npc-sell-editmode.manifest.json`, 472,946 bytes, SHA-256 `fb1c1e19189c141e7a7b286cd99efdac7daa1740aefa52b9562f57a56561d356`.
- Archive: `coref-planned-npc-sell-editmode.zip`, 173,992 bytes, SHA-256 `92ad739e71936ea1a53b4ba5e6eb2959c5c9c3af3aaa06fcb39dd0d26b8b1d0e`.
- Official package verification: `VALID`, one run, one normalized XML entry, matching archive SHA-256.
- Manual privacy scan found no absolute local filesystem paths or credential terms in the public package manifest or normalized XML. Output elements were removed under the approved packager policy. Raw reports and private runner manifests remain protected and are not included in this public bundle.

The CoreValidation private manifest SHA-256 is `3dbad3c8abe28a810b8e2fd1d73a9a844be6fe4fb416af5f4ac985a2c598fb56`. Its protected raw cold TRX is 1,419,209 bytes (`ed53ea41f6f58374efa42a4c72b211276eb1cbdcf95df08ed005f572812cd9b`), and warm TRX is 1,419,209 bytes (`660745dc3ea7dbfec6027f4a26b80c6f5b0b477fd8740390cba8bf63589300b`). The CoreValidation manifest and raw TRX are preserved locally; they are not represented as a public TRX-EVIDENCE package because the available manifest schema does not provide the required `dotnet-trx-source-context-v1` context.

## Boundary

This candidate slice is limited to planned NPC SellGoods projection recovery and the corresponding Core dispatch gate. It does not complete CORE-F, claim a detached host or DailyV1Application integration, or advance Core canonical. No ProjectSettings, unrelated source files, local user modifications, or protected evidence were included in the candidate scope.


