# P12-D restored TravelParty owner bindings — exact-source validation

This record covers only the Core restored-owner-graph admission checks for active `TravelParty` bindings. It does not close P12-D, complete CORE-F, promote Core canonical, establish P12-A or P13 readiness, or claim a detached host or operational `DailyV1Application.TryStart`.

## Source identity

- Code candidate: `1dff6d485da0f1b4460b5114e5883d5fadecfc18` (tree `f79cb8568270e701bc1d6ede6654ab7c8711740e`).
- Base: `20475a3aa6822278b2c38b8061e97a558256ec4e`.
- Core canonical at validation: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Production source SHA-256: `93dbe22b571dd047de2210893a8d5137033fe43a65c517f7f4d8b2f391cc03bb` (`DailyV1P12DSelectedOwnerGraphStager.cs`).
- Core test source SHA-256: `5dbc1cf94f98a1b50de9a873bb6513e9c0802feea3ede9fc74b49e3ffd41cdab` (`DailyV1P12DSelectedOwnerGraphStagerTests.cs`).
- The exact changed production and test inputs match these hashes in the official CoreValidation manifest. The Unity snapshot records the exact production source hash and the same candidate commit/tree.

## .NET validation

Official `Tools/CoreValidation/Invoke-CoreValidation.ps1` completed successfully: **766/766 cold and 766/766 warm**, with zero failed, skipped, or inconclusive tests.

- Run ID: `291198d727bc4490ad5a231b4c6322b9`.
- Private manifest SHA-256: `3a0727e819f81d0006fbd0094319f4908bcd938351bd7d3428e42eb5aa4f5f09`.
- Input digest: `ec12c8bfc9ebda69f750110af6b11de8730c3c37efc5fc9c351eaf1ba47a404a` (stable).
- Cold TRX SHA-256: `5bac78a9f576e117345445a026e5bdac5ae3178cb23600369a12e764358bc035`.
- Warm TRX SHA-256: `528ff47b52e2f1897e01805711df7b5f6ef3e22c805734e42c328c949cf64141`.
- The runner reported that this checkout has a Unity Library. No Library-independence claim is made. Original cold/warm TRX and logs remain in protected local retention. The approved shared evidence packager supports the `simulation-core-validation/v1` runner manifest through its closed `dotnet-trx-source-context-v1` adapter; no public CoreValidation package was produced for this run, so durable .NET per-case evidence remains incomplete.

## Unity validation

The official evidence wrapper ran serially against a detached checkout at the exact candidate commit/tree using one stable 1,884-file input snapshot. All three runs report snapshot digest `74982583d4c2dcbe9afcdb6b352a8d5cf07a808ac127ddac4915f3e2a44aeda0`, `inputStable=true`, Unity `6000.3.9f1`, and the exact production source hash listed above.

| Gate | Run ID | Result | Private manifest SHA-256 | Original XML SHA-256 | Original log SHA-256 |
|---|---|---:|---|---|---|
| Focused `P12GPoliticalBindingRestoreTests` | `cc9be1acf2c74fc0933aaca0e4a71f6f` | 15/15 PASS | `fc027f8da4397c3a1034f80fe44b669ab882f2ada640700c94d75d4018b70f9d` | `6b308c6fab6d8c0cefa61fea75653f2280f90087c6d13f0e2e21f9990fff5838` | `7640027b248e0f71ca2f630f56460dc2aa7f7e9550fe05865c4fd4fedf4b98a1` |
| Full EditMode | `0f397ea25d234dd2b4547118eb50cb0c` | 3263/3263 PASS | `e5aee1a71e41a7d13fda6be3f29636add2baa23a0c202e8333c59dd40680d210` | `1d9a3f8eb70478b0e852fbd0031c653cea7c7f87530c6d54b70e5c4999724be3` | `891deb1765cc6fbb7465299569e98b207bb2e394628d182770f3737de4eb5bc3` |
| Official Smoke filter | `5ff97bd078074adcab022ef19f540239` | 5/5 PASS | `ce0fc53c75d00a1f2374a47c6cc7690b7210a5c3c8beaea780cade3b68a5075d` | `7aafb4dca7a0cc93a6e3ca2a3d59e4203008870a2dd6718312d6bb9ff4d6ab70` | `87608425c26b20f68a306449bfec87425c885709e628fa83b5498e14a209c74d` |

The result-only package for the coherent full EditMode report is [`coref-p12d-travelparty-editmode-exact-1dff6d48.zip`](editmode/coref-p12d-travelparty-editmode-exact-1dff6d48.zip), with manifest [`coref-p12d-travelparty-editmode-exact-1dff6d48.manifest.json`](editmode/coref-p12d-travelparty-editmode-exact-1dff6d48.manifest.json). The approved packager verifier passed (`runs=1`, `entries=1`). Manifest SHA-256 is `c629a98e5776281882f49e5e0e98b2c11cc3ddb36347c9c5c9db9aec5699b55a`; archive SHA-256 is `d97436b3bff6eb253b7fe4dd00098d2e3871ca783bb58aaa421434a3202d3d81`. The package is result-only, and a path scan found no local absolute paths.

The packager correctly refuses to publish the two filtered original XML reports: Unity leaves the ancestor `Simulation`/Assembly suite `testcasecount` at 3263 while each filtered report contains only its 15 or 5 descendants. The original filtered XML and logs were not modified. Their protected run manifests report coherent root counts and PASS, and all 15 focused plus all 5 Smoke `(id, fullname)` identities/outcomes were cross-checked as present and Passed in the packaged full EditMode report. No separate public package is claimed for either filtered XML.

The isolated Unity validation checkout received the pre-existing local ProjectSettings bytes unchanged; those user-owned changes remain unstaged and are not part of the candidate. Protected original reports remain unchanged.

## Portable invocations

The official Unity wrapper recorded these effective argument vectors (machine-specific paths replaced with its public placeholders):

- Focused: `-batchmode -runTests -testPlatform EditMode -projectPath <PROJECT_ROOT> -testResults <RESULT_XML> -logFile <RAW_LOG> -testFilter P12GPoliticalBindingRestoreTests`.
- Full EditMode: `-batchmode -runTests -testPlatform EditMode -projectPath <PROJECT_ROOT> -testResults <RESULT_XML> -logFile <RAW_LOG>`.
- Smoke: `-batchmode -runTests -testPlatform EditMode -projectPath <PROJECT_ROOT> -testResults <RESULT_XML> -logFile <RAW_LOG> -testFilter Smoke`.

The CoreValidation run used the approved `Tools/CoreValidation/Invoke-CoreValidation.ps1` runner against the candidate project root with the standard cold/warm flow. Its portable argument vector is not yet included in this public record; the private original run manifest remains the authority for that invocation.

## Checkpoint and review boundary

`git diff --check`: PASS. The implementation and test-source review is PASS with no actionable code or assertion findings. The overall independent review verdict is **NEEDS_CHANGES — evidence packaging only** for code candidate `1dff6d485da0f1b4460b5114e5883d5fadecfc18` / tree `f79cb8568270e701bc1d6ede6654ab7c8711740e`.

The current public bundle contains only the complete 3,263-case EditMode report. The separate focused and Smoke report records, and CoreValidation cold/warm records, remain protected local originals; they are not represented as complete permanent per-gate evidence. The full EditMode bundle contains the same focused and Smoke case identities/outcomes, but does not substitute for the separate filtered run records under `VALIDATION_EVIDENCE_POLICY.md` §§2–3. No canonical promotion is performed by this validation record.
