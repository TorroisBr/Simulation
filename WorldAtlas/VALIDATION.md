# ATLAS-A Validation Record

Run date: 2026-11-25 (America/Sao_Paulo)

## Source identity

- Architecture base: `e3403ebe4d208a9e364b634893879488f424973a`
- Implementation under test: commit `cc6455a5c25ed3964ad44181032297a06de45c7a`
- Implementation tree: `d6adf74372b0740b69fb8bba8363940a94bdbf69`
- Branch: `codex/atlas/standalone`
- Toolchain: .NET SDK `10.0.404`, pinned by `WorldAtlas/global.json`

This validation record is added in an evidence-only successor commit. Source and test inputs are the implementation commit and tree above.

## Architecture gate refresh

Read-only `git ls-remote` on 2026-11-25 found `codex/architecture/world-identity-projection` at `7973a0dea4462811666519e251f7c4c447b9ae2f`. The confirmed promoted SHA `e3403ebe4d208a9e364b634893879488f424973a` is an ancestor of that remote tip. All five original World Atlas architecture documents are present at the promoted SHA. At the refreshed tip, the handoff still marks ATLAS-A `READY_FOR_IMPLEMENTATION` and ATLAS-B `WAIT_DEPENDENCY`.

The only World Atlas directory changes between the promoted SHA and refreshed tip are four LAB-OBS-A documents (`LAB_OBS_A_OBSERVATION_V1.md`, `LAB_OBS_A_REVIEW.md`, `LAB_OBS_A_SOURCE_AND_HANDOFF.md`, and `LAB_OBS_A_TECHNICAL_DESIGN.md`). The five original World Atlas documents, including the ATLAS-A handoff, are unchanged. These later LAB-OBS-A additions do not invalidate the ATLAS-A Demo-only scope. No architecture re-promotion or `main` change was made.

## Commands and results

The locked restore, Release build, win-x64 restore, and single-file publish all succeeded. The Release build reported 0 warnings and 0 errors. The publish was self-contained for `win-x64`, with `PublishSingleFile=true` and `IncludeNativeLibrariesForSelfExtract=true`.

The test run completed with **21 passed, 0 failed, 0 skipped**. It includes tests for observation parsing and compatibility, comparison semantics, hex projection and marker placement, visible-hex arrow-key navigation, headless selection and inspector behavior, and project boundaries.

The packaged executable remained running for four seconds, exposed a main window handle, and closed cleanly through `CloseMainWindow`. This was a process-startup check. **Native interaction smoke remains pending by user choice.**

All build, intermediate, publish, and NuGet package output was redirected to `C:\Users\Martins\AppData\Local\Temp\world-atlas-build` and `C:\Users\Martins\AppData\Local\Temp\atlas-nuget`. A temporary `Directory.Build.props` imported the repository's `WorldAtlas/Directory.Build.props` and redirected `obj`/`bin` to the C: scratch directory. The exact command record and original console output are preserved with the local evidence.

| Check | Command | Result |
| --- | --- | --- |
| Locked solution restore | `dotnet restore .\WorldAtlas\WorldAtlas.sln --locked-mode` | Passed |
| Release build | `dotnet build .\WorldAtlas\WorldAtlas.sln --configuration Release --no-restore` | Passed; 0 warnings, 0 errors |
| Locked Windows RID restore | `dotnet restore .\WorldAtlas\src\WorldAtlas.Desktop\WorldAtlas.Desktop.csproj --runtime win-x64 --locked-mode` | Passed |
| Self-contained single-file publish | `dotnet publish .\WorldAtlas\src\WorldAtlas.Desktop\WorldAtlas.Desktop.csproj --configuration Release --runtime win-x64 --self-contained true --no-restore --output <C: scratch publish> -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true` | Passed |
| Automated tests | `dotnet test .\WorldAtlas\WorldAtlas.sln --no-build --no-restore --configuration Release --logger "trx;LogFileName=ATLAS-A-final.trx" --results-directory <C: scratch results>` | 21/21 passed |
| Packaged process startup | Start packaged executable hidden, observe four seconds, send `CloseMainWindow` | Passed; process closed cleanly |

Each `dotnet` command also received `-p:DirectoryBuildPropsPath=<C: scratch Directory.Build.props>`. Restore used `NUGET_PACKAGES=C:\Users\Martins\AppData\Local\Temp\atlas-nuget`.

## Artifact identities

The ignored local package is under `WorldAtlas/artifacts/`.

| File | Size | SHA-256 |
| --- | ---: | --- |
| `WorldAtlas/artifacts/WorldAtlas-win-x64/WorldAtlas.Desktop.exe` | 102,080,122 bytes | `23be56a7fed3ddd0bf590104cef2ff32d51a6281de026ed11b25c876e5b91751` |
| `WorldAtlas/artifacts/WorldAtlas-win-x64.zip` | 44,520,958 bytes | `ac2baad872076dcce664e33230fd0122d07e77c2f7e3862fbd7fb97efade0ffc` |

The bundled fixtures match the source fixtures byte-for-byte:

| Fixture | SHA-256 |
| --- | --- |
| `observation-day-1.json` | `7b3c806ee75b7b1d49b6dc61c21b29610e09d4a48e66d6f89048525e9f612939` |
| `observation-day-2.json` | `06a749d5695b7f9541519cc325a18ee6e3b6a4f1503e7a711e8fe6edae907cc9` |
| `unplaced-hex-validation.json` | `fc9fb666d8ab604621751c1b7ec46812d0c1443d2ddb1c16564e9a9077dfc9c6` |

## Local raw evidence

Original validation files are retained outside Git at `WorldAtlas/.validation-evidence/ATLAS-A-cc6455a5-2026-11-25/original/`. The run directory ACL grants access only to the current user and SYSTEM. `.validation-evidence/` is ignored; the raw TRX is not committed because the repository's .NET TRX normalization adapter is not yet available.

| Original evidence file | SHA-256 |
| --- | --- |
| `ATLAS-A-final.trx` | `7373b5177a159ce7607ab42354bd0f475b620b5429b2536badb51c84000a6b32` |
| `ATLAS-A-final-console.txt` | `53c98620637fd35d7b901f607bb825fe788010f9739042acd2ba0abd11a66517` |
| `ATLAS-A-restore-build-transcript.txt` | `9ca041f52ffa0256eb8bd5c718e346231c701226d50279ce0bcf14b239f29a75` |
| `ATLAS-A-singlefile-publish-transcript.txt` | `d6b70580b52d325633ce60f622800dbac975639e11bd51e29da2b599e1b136df` |
| `ATLAS-A-packaged-startup-smoke.txt` | `c8d4167e08539b8b8e6582aa94f4a645b1b18340d5a5574dfb2409413eb9cfa8` |
| `execution-record.txt` | `12100dfb6b846eaaeb2b23da6cbaca8161f6dbfa6f8885801643d74c7c266df4` |

## Acceptance status

- Native interaction smoke: `PENDING` by user choice.
- ATLAS-B real artifact integration: `WAIT_DEPENDENCY` on reviewed LAB-OBS-A observations.
- Remote publication: none.

## Intermediate UX candidate f3b9078 (superseded; preserved)

This section records the bounded UX successor; the preceding hashes remain the historical evidence for the earlier `cc6455a5` package.

### Source and input identity

- Candidate branch: `codex/atlas/standalone-ux`.
- Tested implementation commit: `f3b907880b0bc0802bc421340dcb2b6b9d985e2b`.
- Tested root tree: `efc1636fed6e3f353dacb488db370396769b16aa`.
- Atlas base: `dba4a8c6ac85c4a7913a9a27b6e9d6ae0b69f9e0`.
- Refreshed architecture tip merged before implementation: `158f04d4a021c3984b3ba841e246b45e1b0a457b`; its user-authority changes continue to treat Atlas as a read-only consumer.
- The commit tree binds application source, tests, Demo fixtures, project files, app manifest, package manifest, and lock files. Desktop lock SHA-256: `72f9fa5284ebd35bc2337547b200289ccfad2910a2a431b131a81da262d5ef2a`; Observation lock: `1e39bb5149e1289978e01a366893a0697356e855bc7156b7c70747288030042f9`; test lock: `81641df0637e3a8000da45ac7b08ed5c06b2cb9b636f804077ee950fab541ffc`.
- Day 1 fixture SHA-256: `e833b19e05fb11b5376d3469490b7b592499eaac4fef4df46666ebec38bcc452`. Day 2 fixture SHA-256: `e4f55131bd55dd2767bcb7b1e29c1e403049a5f7a70ba8231a1d9638fc955900`.

### Feedback classification and changes

- Native tests A and B were confirmed passing by the user: map navigation and the Demo City marker indication. The Demo marker remains clearly separated from real Core-backed P8 relationships.
- C and D were UX gaps: the comparison was hard to find, JSON appeared to be the main entry point, and labels were not human-readable. The candidate starts on bundled Demo World without file selection, displays Day 1/Day 2 choices, elevates the comparison into a resizable panel, and labels its row `City city-1 — Wheat`, `Day 1 = 20`, `Day 2 = 25`, `+5`. It explicitly says causal events are not covered. Clicking the row selects that City. File opening remains under `Advanced: Open file…` and still uses the versioned reader.
- Fixing the checked-in Demo chain was necessary: Day 2 previously referenced a digest that did not match the actual Day 1 bytes. The Day 2 `previousDigest` now matches Day 1's raw-byte SHA-256; the existing reader and comparer enforce the chain. No observation schema, provenance rule, compatibility rule, or Core behavior changed.
- Typography now defaults larger and exposes 100%, 125%, 150%, and 175% interface scale choices. The setting is persisted under the current user's Local AppData, outside observation files. Avalonia's per-monitor DPI awareness remains enabled by default and the Windows manifest declares `true/pm`; no OS display setting was changed.
- The previous About window used `SizeToContent.Height` around wrapped text at a fixed width. Its content measurement could change as available width or control template state was remeasured. The Atlas dialog now has explicit 520×310 dimensions, matching min/max bounds, no resizing, and a scrollable text region. Headless regression coverage confirms bounds remain fixed after pointer-over at 175%. This Atlas-specific diagnosis does not assume Dungeon Studio has the same cause.

### Build and automated validation

Host/toolchain: Windows build 10.0.26200, `win-x64`, .NET SDK `10.0.404`, MSBuild `18.9.15`, runtime `10.0.15`. Outputs were redirected to the existing C: scratch build root; C: had more than 15 GB free and E: remained above its 8 GB operational floor.

The initial online vulnerability-data audit could not reach `api.nuget.org` and failed with `NU1900` under warnings-as-errors. Locked restores then passed with `NuGetAudit=false`; all packages stayed pinned and the lock files remained unchanged. The desktop-only restore/build also used `MSBuildEnableWorkloadResolver=false` and the existing output-redirection helper; its imported project-property lines were verified identical to this candidate's `WorldAtlas/Directory.Build.props`.

| Check | Result |
| --- | --- |
| Locked solution restore | Passed; C: scratch packages and existing NuGet cache |
| Locked `win-x64` restore | Passed |
| Release build | Passed; 0 warnings, 0 errors |
| Full .NET/Avalonia Headless suite | **29 passed, 0 failed, 0 skipped** |
| Headless viewport coverage | Five logical sizes: 1920×1080, 3840×2160, 1280×720, 2560×1440, and 1097×617; all four interface scales; toolbar, map, comparison panel, scale binding, and bounds checked |
| Native desktop interaction | **PENDING by user choice**; no executable launch or UI interaction in this iteration |

The smaller logical sizes approximate Windows 150% and 175% display scaling on Full HD/4K monitors; they do not replace a native per-monitor-DPI check. The user explicitly asked to leave native smoke pending. The manual file-picker itself was not automated; its validated ViewModel fallback and visible control are covered, with native picker interaction pending.

Original TRX and console transcripts are retained at `WorldAtlas/.validation-evidence/ATLAS-A-f3b90788-2026-11-25/`. The local evidence directory has inheritance disabled and was checked to contain only the current-user and SYSTEM ACL principals. TRX SHA-256: `19a751f0f4817df69ea5b2eb09e689fd00edd236f1811869d10ca107e1bcf9fa` (29/29 passing cases); test console SHA-256: `8ee9d4b0ee4d77a130f7296e42467b0aeb065522e1226475697f123bf2dadc2e`; Release build console SHA-256: `a1619540ace28ae1a36b3e46c7a33a90afe703b1e07f649c2778b21c2116e60c`; publish console SHA-256: `29108edb50871fd92a95d42d736b76b10e09997017fb15eb093b6fc668f6fc6b`. The full execution record is local with SHA-256 `0f3675723bda9179bdacd28bdfa03fca03a081bb3397f8520e830015525c1f87`.

The repository's .NET TRX result-only adapter is not available, so raw TRX is not committed and no permanent report package is claimed. This candidate has not been promoted. The protected local original is retained for this review and must not be mistaken for a durable public validation package.

### Windows package

Published self-contained single-file `win-x64` output under the ignored local `WorldAtlas/artifacts/WorldAtlas-win-x64-ux-f3b90788/` directory. The ZIP contains exactly the executable and the two Demo observation JSON files; symbol files were excluded. Published fixture bytes match source fixtures.

| File | Size | SHA-256 |
| --- | ---: | --- |
| `WorldAtlas.Desktop.exe` | 102,100,602 bytes | `368a4614237d6b8ed90a1cddf2065f6a8523426e743eceb08d604a0ceb2a4dea` |
| `WorldAtlas-win-x64-ux-f3b90788.zip` | 44,529,467 bytes | `4f27e2df3be7394c04d90aba1d011dfa77e7a1bbd23e61cc3bc31dfd1bd08cd5` |

### Follow-on boundaries and status

- `WorldAtlas/docs/REAL_WORLD_DISCOVERY_RECOMMENDATION.md` is recommendation-only. A producer-owned versioned catalog is a candidate for a separate design with LAB-OBS-A; no real-world discovery, arbitrary directory scan, P12 reader, ATLAS-B, or Core connection was implemented.
- `WorldAtlas/docs/SHARED_UI_ARCHITECTURE_REVIEW_REQUEST.md` contains the bounded cross-app review prompt. Its transmission to the existing General Architect thread was rejected by the Codex approval reviewer because the trusted transcript did not explicitly authorize sending this repository metadata. No alternate route was used; the review has not been received.
- Independent review for this intermediate tip returned `NEEDS_CHANGES` with two P2 scale/layout findings: the fixed header could clip scaled text, and file-open notices defaulted to 100%. Commits `d082319b119aa5e8387de06ab5a345ec1843c682` and `3e4ed67aecec74e857f6be080dea0e4f7cad5646` address those findings; the final successor is reviewed separately.
- ATLAS-A is **not fully accepted**. Native interactive smoke remains pending by user choice; ATLAS-B remains `WAIT_DEPENDENCY`; no canonical promotion or remote publication occurred.

## Final UX candidate 3e4ed67a

The final code candidate includes the f3 UX work and two reviewed follow-ups: scale-aware auto-sized header text/control geometry and applying the selected scale to file-open notices as well as About. The f3 artifact and evidence remain preserved under their own names above; this final package was rebuilt from the exact source commit below.

### Final source and inputs

- Branch: `codex/atlas/standalone-ux`.
- Tested implementation commit: `3e4ed67aecec74e857f6be080dea0e4f7cad5646`.
- Tested root tree: `b0b572257a580570e8d1a36f7c0480ad937d2436`.
- Atlas base: `dba4a8c6ac85c4a7913a9a27b6e9d6ae0b69f9e0`.
- Current architecture contract: `158f04d4a021c3984b3ba841e246b45e1b0a457b`.
- Source, tests, fixtures, app manifest, projects, and lock files are bound by the commit tree. The two fixture hashes and the three package lock hashes are identical to the f3 inventory above; the final build also records the configuration hashes and external scratch helper identity in its protected execution record.

### Final exact-tip validation

Host/toolchain: Windows build 10.0.26200, `win-x64`, .NET SDK `10.0.404`, MSBuild `18.9.15`, runtime `10.0.15`. The locked restore, locked `win-x64` restore, Release build, and full test suite were rerun for this final tip. Locked restores passed with the pinned package locks; the Release build had 0 warnings and 0 errors. The test run reports **29 passed, 0 failed, 0 skipped**.

Headless layout coverage uses logical client sizes 1920×1080, 3840×2160, 1280×720, 2560×1440, and 1097×617 at 100%, 125%, 150%, and 175%. Assertions cover scaled header text fitting its allocated rows, selector/action bounds, comparison panel height, map presence, and UI scale binding. These logical-size cases approximate Windows display scaling; they do not substitute for a native per-monitor-DPI check.

The complete original TRX and command transcripts are retained locally at `WorldAtlas/.validation-evidence/ATLAS-A-3e4ed67a-2026-11-25/`. The evidence directory has inheritance disabled; the directory and all children were checked to contain only current-user and SYSTEM ACL principals, with no reparse points. TRX SHA-256: `7b72bd1708a78eb762b95668128551f647c2c77a88f96a270d4fafc58d554907`; Release build console SHA-256: `a87030dc759a64b89cb307d067238406c1307da713e3334a84b4dfcdee709379`; test console SHA-256: `842518ba45fa1dd89a85425989640bab753f575cd29bbeff74f5e603f4f2b3e2`; publish console SHA-256: `6d2568e154a76ef8ad4fb97c674a681aeea658294ce94d8109e5eeca5dd7c460`; execution record SHA-256: `05ae22b49f35d9e9e6b951909f001e4068a9405ac8785a364dbb1014d290d9c1`.

The .NET TRX result-only adapter remains unavailable, so the raw TRX is not committed and no permanent report package is claimed. This local diagnostic evidence is available for exact-tip review, but it is not a durable public test-results package.

### Final Windows package

Self-contained single-file `win-x64` publish is under ignored local `WorldAtlas/artifacts/WorldAtlas-win-x64-ux-3e4ed67a/`. The ZIP contains only the executable and the two validated Demo fixtures; published fixture hashes equal source. No executable launch or native UI smoke was performed.

| File | Size | SHA-256 |
| --- | ---: | --- |
| `WorldAtlas.Desktop.exe` | 102,100,602 bytes | `93f1eaa5ec8e1d6f460aabc8cacfc668f0cf71ddbaa6c263ba85ba76d6675c1` |
| `WorldAtlas-win-x64-ux-3e4ed67a.zip` | 44,529,526 bytes | `e24535ddefe0d6ae15324c6464abfcebd51a85a0d402c20f5cae5aea1bf4a598` |

### Final review and acceptance status

- Independent exact-tip review of `3e4ed67aecec74e857f6be080dea0e4f7cad5646` returned **VALIDATED_CANDIDATE** with no blocking findings. The reviewer verified that both prior P2 findings are closed, the final TRX reports 29 passed / 0 failed / 0 skipped, the Release build has 0 warnings / 0 errors, evidence ACLs and archive contents are as recorded, and executable/archive/fixture hashes match. The reviewer also confirmed this documentation-only successor applies to the unchanged tested code tree.
- Native UI smoke remains **PENDING by user choice**. The user-confirmed A/B results are preserved; the C/D UX changes have headless coverage but have not been accepted through a new native interaction run. ATLAS-A is not marked fully accepted.
- ATLAS-B / real-world discovery remains `WAIT_DEPENDENCY`. Real catalog/discovery and shared UI extraction were not implemented. The bounded discovery recommendation is in `WorldAtlas/docs/REAL_WORLD_DISCOVERY_RECOMMENDATION.md`.
- The cross-application review prompt is prepared but was not transmitted: the Codex approval reviewer rejected sending repository metadata to the General Architect thread because the trusted transcript did not explicitly authorize that transfer. No alternate route was used. No promotion or remote publication occurred.

## ATLAS-A native acceptance and promotion preflight — 2026-11-25

### User acceptance

The user reports native Windows functional acceptance **PASS** for implementation commit `3e4ed67aecec74e857f6be080dea0e4f7cad5646`, with validation successor `69dc8f1e04fbe647ec0e0907699f5cc603569d78`. This is user-reported acceptance, not an independently captured desktop recording. Accepted workflows are automatic Demo World loading, Hex map navigation, City selection and Inspector, human-readable observation selection, Day 1 → Day 2 comparison (Wheat 20 → 25, observed delta +5), improved typography and UI scaling, and About dialog behavior. The user reported no remaining blocking UX issue. No application source or automated validation input changed after the reviewed implementation tip.

This acceptance supersedes the earlier **PENDING by user choice** native-interaction status in the 69dc validation record. The earlier status accurately described the evidence available at that time; the 29/29 automated result and its input identity are unchanged.

### Reconciliation of review dispositions

- The recorded `NEEDS_CHANGES` verdict applies to intermediate UX implementation `f3b907880b0bc0802bc421340dcb2b6b9d985e2b` and identified two P2 scaling issues. Commits `d082319b119aa5e8387de06ab5a345ec1843c682` and `3e4ed67aecec74e857f6be080dea0e4f7cad5646` fixed them; the independent exact-tip review of `3e4ed67aecec74e857f6be080dea0e4f7cad5646` returned `VALIDATED_CANDIDATE` with no blocking findings.
- The 69dc validation-only successor received an independent applicability check against the unchanged source tree; that check passed. Its pending-native status is now superseded by the user's acceptance above. The historical intermediate `NEEDS_CHANGES` result remains visible and is not treated as the final code verdict.

### Promotion evidence gate

The exact final TRX remains preserved in the protected local evidence directory with the previously recorded 29 passed / 0 failed / 0 skipped counters and SHA-256. A read-only content scan confirmed that the original TRX contains run-user, machine-name, and absolute profile-path metadata. The local original remains access-controlled and was not added to Git. The TRX adapter was first implemented at `c5ffa77faa68c522d3065eadc27e8b50242f01e7` and promoted on `origin/codex/core/canonical` by `74e8268c4b3874414ff73878351a93d24c32739f`. Later adapter-file updates `b7535041a8aeb6e341fe35c6fcc465da48ce313e`, `0a374c776fdd7d4bb38b8a3f04f539d5ef045693`, and `54801c0e851779a711ef99950295b615035271b9` are included in current Core canonical `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`. Its supported report profile remains `trx-vstest-flat-v1` / `vstest-17.14.1-flat`; the approved producer contract binds VSTest 17.14.1, SDK 9.0.315, and the CORE-A cold/warm plus demo command/input shape. This is not universal TRX support. Atlas's exact test locks bind `Microsoft.NET.Test.Sdk`, `Microsoft.TestPlatform.TestHost`, and `Microsoft.TestPlatform.ObjectModel` at 18.12.0; the execution record identifies .NET SDK 10.0.404, contains one 29-test suite invocation, and does not bind the logger binary identity or capture manifest required by that adapter profile. No independently validated normalized package has been produced for this Atlas run. The approved privacy-safe .NET contract does not permit replacing the original with a hand-edited or summary-only report. The current evidence policy requires complete retrievable result evidence in canonical Git or a qualifying durable store for promotion; the retained local TRX and its hashes alone do not meet that requirement.

**Scope correction:** an earlier preflight draft said that “the inspected evidence tooling does not support TRX.” That was too broad. The Core-canonical TRX adapter exists and is promoted, but the Atlas run's exact producer/input applicability has not been established. This correction does not change the evidence or claim a normalized package.

Therefore ATLAS-A's **native functional acceptance is PASS**, while canonical promotion remains **BLOCKED** on an approved durable TRX evidence path. The named `codex/atlas/standalone` branch is clean locally at `dba4a8c6ac85c4a7913a9a27b6e9d6ae0b69f9e0`, but no matching branch exists on `origin`; no remote branch was created or updated. No Atlas source changes, new test runs, evidence cleanup, worktree cleanup, or downstream ATLAS-B/LAB-OBS-A implementation occurred in this closure attempt.

### Independent final applicability review

The independent reviewer returned **VALIDATED_CANDIDATE** for docs-only tip `400db01cc955d68809bbf4609cdc8d28e0dcfcb2` (tree `a21a67aa3e620138a4ffe1e2801e426824a4706e`) against implementation `3e4ed67aecec74e857f6be080dea0e4f7cad5646` (tree `b0b572257a580570e8d1a36f7c0480ad937d2436`), candidate base `dba4a8c6ac85c4a7913a9a27b6e9d6ae0b69f9e0`, and architecture tip `158f04d4a021c3984b3ba841e246b45e1b0a457b`. It confirmed the only change after validation successor `69dc8f1e04fbe647ec0e0907699f5cc603569d78` was `WorldAtlas/VALIDATION.md`, `git diff --check` passed, the user acceptance text matched, source/test inputs and 29/29 evidence were unchanged, and the TRX/publication block is supported by current policy. It also confirmed that the missing remote Atlas branch only means future promotion creates it; it does not invalidate this local candidate.

For final docs-only tip `0734ac313c212bd817fd4ab8480bcbe69c4609a0` (tree `86c24a5a6b65e3f2951936612ef2ee03df3808ce`), the same independent reviewer returned **applicability PASS** against `400db01cc955d68809bbf4609cdc8d28e0dcfcb2`. Its only successor change was `WorldAtlas/VALIDATION.md`; `git diff --check` passed; the desktop source and test trees remain identical to `3e4ed67aecec74e857f6be080dea0e4f7cad5646`. The reviewer confirmed the recorded disposition and downstream CORE-F/LAB-OBS-A/ATLAS-B status against fetched refs and found no corrections. No tests were rerun.

## ATLAS-A final acceptance and durable evidence package — 2026-11-25

### Native acceptance and prior NEEDS_CHANGES reconciliation

The earlier feedback disposition **NEEDS_CHANGES** was validation-only: the then-current candidate had passed 21 automated tests, but the requested native interaction run was still pending, and the feedback explicitly withheld full acceptance. The user has since completed native Windows testing of implementation `3e4ed67aecec74e857f6be080dea0e4f7cad5646` and reports **PASS** for automatic Demo World loading, Hex navigation, City selection and Inspector, human-readable observation selection, Day 1 → Day 2 comparison (Wheat 20 → 25, observed delta +5), improved typography and scaling, and About behavior. No blocking UX issue was reported. This confirmation closes the pending-native basis of that validation-only verdict; it does not erase its historical record.

The distinct code-review `NEEDS_CHANGES` on intermediate implementation `f3b907880b0bc0802bc421340dcb2b6b9d985e2b` identified two P2 scale/layout defects. Commits `d082319b119aa5e8387de06ab5a345ec1843c682` and `3e4ed67aecec74e857f6be080dea0e4f7cad5646` fixed them; the independent exact-tip review of `3e4ed67aecec74e857f6be080dea0e4f7cad5646` returned **VALIDATED_CANDIDATE** with no blocking findings. The later evidence-only promotion block concerned the then-unreviewed Atlas/VSTest 18.12.0 provenance and missing durable package; the bounded profile and package below close that evidence gap without changing the application.

### Source and 29/29 result identity

The accepted application implementation remains commit `3e4ed67aecec74e857f6be080dea0e4f7cad5646`, tree `b0b572257a580570e8d1a36f7c0480ad937d2436`. The validation successor at `11b89d4aba3961f767bf4c86056f3382e193afd1` changes `WorldAtlas/VALIDATION.md` only relative to that implementation; no application source or automated test input changed. The original and controlled recapture are separate 29-test runs. The historical original TRX remains unchanged in protected local retention: 40,163 bytes, SHA-256 `7b72bd1708a78eb762b95668128551f647c2c77a88f96a270d4fafc58d554907`. Its protected copy has inheritance disabled and only the current user and SYSTEM ACL principals.

The controlled recapture ran one `WorldAtlas.sln` test invocation against the same application implementation and passed **29/29** (0 failed, 0 not executed). Its run ID is `e6f63b8b-9f2f-44cc-bff5-a11ee8666f4a`; original recapture TRX: 40,431 bytes, SHA-256 `fa0e16f267c5a11656222ba530882741e3c0d5c10dac0618edaa879c4a9e2cf6`; normalized report: 34,409 bytes, SHA-256 `a6830ae1bc7b181bf17dc6703c7d51d259af9833cac763472f39517de6483326`. The captured source input digest is `0b128c6340d38f86351f2908d0bb4bd4e895e2bf6aacb6ebb60b68bc309eab3b`; the private context binds 50 tracked WorldAtlas files, 11 explicit input hashes, the test assembly/PDB and producer/logger identities, and the 288-file generated test-output inventory. The protected original comparison verified source-to-normalized result equivalence and original immutability.

### Public package and profile applicability

The public, durable package is [`atlas-a-29-tests-v1.zip`](validation/atlas-a-29-tests-v1.zip) with its provenance [`manifest`](validation/atlas-a-29-tests-v1.manifest.json). The ZIP is 5,574 bytes (SHA-256 `7244c87bd9caf3f5c65a943e9c764cee0c53463f0af59ef4a8fdec6fdeeffe29`) and contains exactly one normalized TRX. The separate manifest is 98,941 bytes (SHA-256 `2476945c5e459ee51af93b2ec68fe36878db87f07d6145d3bc1d8f9cc1ec5118`). Archive-only verification reports `originalComparisonNotPerformed`; the protected-source comparison was run separately and reported `originalComparisonVerified`. Raw TRX, capture manifests, command logs, helper, and retained test outputs remain in owner-only protected local storage and are not published.

The package uses the independently reviewed, closed `atlas-a-29-tests-v1` profile from tooling candidate `c54bd013ed8efe40b292bbd15d06a997c617698d` (tree `6a32a492452a797a45f333e13bf27d9ef602f32e`), pinned to the captured .NET SDK 10.0.404 and Test Platform 18.12.0 producer shape. Existing CORE-A/VSTest 17.14.1 and Unity profiles remain unchanged. Independent exact-tip review of the tooling candidate returned **VALIDATED_CANDIDATE** with no actionable findings; it verified source/runner binding, private comparison, package integrity, regression results and `git diff --check`. This profile is limited to the captured Atlas shape and does not widen CORE-A claims.

No Unity or Simulation Core tests were rerun. No ATLAS-B, LAB-OBS-A, real-world discovery, Dungeon Viewer, or shared UI work was started.

### Downstream dependency refresh

Read-only refresh against fetched current refs found `origin/codex/core/canonical` at `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`. Its owning `docs/CORE_STATE.md` records bounded CORE-F prerequisites as promoted but **full CORE-F remains `WAIT_DEPENDENCY`**, including the official .NET host/continuation and remaining selected-owner/runtime work. The reviewed LAB-OBS-A technical design remains a design **PASS** only; its source handoff still gates production implementation on a suitable promoted Core host/fixture and refreshed reader-isolation/identity evidence. No canonical LAB-OBS-A implementation branch or approved real observation result was found in fetched refs. Consequently ATLAS-B remains **`WAIT_DEPENDENCY`** on valid reviewed LAB-OBS-A artifacts and its separate real-world conformance proof; ATLAS-A native acceptance does not satisfy that producer dependency.

## Dependency refresh against promoted Core state — 2026-11-25

The reviewed Atlas evidence profile was promoted to `origin/codex/core/canonical` by fast-forward commit `d0030542a075a01f0ca025745c172bf0c4e31407` (tree `c02f522c80bb5d3ee298e36154b60fdf14c9fd5d`), based on `145f4bc6534ee69de7915f2c94de798c22627794`. The independently reviewed post-promotion State successor `935a958512042a8660d5bb183e959bb9bd430b35` records the promotion; the remote canonical ref was verified at that successor. Core’s current State records CORE-F’s Daily-v1 host and continuation as promoted within the accepted scope.

The refreshed architecture ref is `origin/codex/architecture/world-identity-projection` at `cb71a70fc503e926c3b3f39415b86f2fab896ba8`. Its post-CORE-F handoff records the full LAB-OBS-A path as **`WAIT_DEPENDENCY`** and identifies a bounded Core-owned reference-input provider plus parity/stock probe as the first separate implementation handoff. That provider, Lab adapter, and observation reader have not been delivered. Remaining gates include production-owned admitted inputs; an official Lab adapter bound to the Core application and exact session; a coherent same-cut read of P8 Hex-to-Location geography, City identity/anchor census and scoped Market stock/revisions; callback-free capture and exclusion of unscoped writers during the held reservation; adjacent successful cuts and a real nonzero stock delta.

The dependency matrix on that architecture branch still contains pre-promotion baseline notes (documentation base `512cf…`, Core row `a07402…` and an unpromoted Core candidate). Those entries are historical and stale against the current Core State and the post-Core-F handoff at `cb71…`; this closure uses the current owning State and handoff for factual status and records the matrix mismatch for its architecture owner. No architecture file was changed here.

ATLAS-A’s native acceptance and evidence gates are now satisfied. ATLAS-B remains **`WAIT_DEPENDENCY`** until it can consume valid independently reviewed LAB-OBS-A artifacts and pass its separate real-world conformance proof. The dependency sequence is Core-F Daily-v1 host → LAB-OBS-A provider/probe → Core observation facade and capture → Lab artifact producer → ATLAS-B file reader. No ATLAS-B or LAB-OBS-A implementation, real-world discovery, Dungeon Viewer integration, or shared UI extraction was started; those remain within their separately approved boundaries.

## Final exact-tip review and promotion readiness — 2026-11-25

Package-bearing candidate `19677ba9f429097e813835655260e55488a13419` (tree `450c2cd7f2f5e3cc29d64156a0fdcee92d7cacc9`), based on canonical `dba4a8c6ac85c4a7913a9a27b6e9d6ae0b69f9e0`, received independent exact-tip **VALIDATED_CANDIDATE** review with no findings. The reviewer verified the package/manifest hashes, the single normalized report, 29/29 outcome, original protected comparison, source implementation `3e4ed67…` / tree `b0b572…`, reviewed tooling `c54bd013…`, current Core/architecture refs and scope boundaries. The package-bearing candidate differs from the accepted implementation only in `WorldAtlas/VALIDATION.md` and the two public evidence files; no app, test, fixture or project input changed. `git diff --check` passed; no tests were rerun.

Refreshed preflight found local canonical at `dba4a8c6ac85c4a7913a9a27b6e9d6ae0b69f9e0`, clean and an ancestor of the candidate by 17 commits. Live remote refs were Core `935a958512042a8660d5bb183e959bb9bd430b35` and architecture `cb71a70fc503e926c3b3f39415b86f2fab896ba8`; no remote `codex/atlas/standalone` branch existed. The exact candidate was fast-forwarded into the local canonical worktree; this documentation successor records the readiness snapshot before publishing the final reviewed tip.

**ATLAS-A — PASS / READY_FOR_CANONICAL_PROMOTION.** The native acceptance, unchanged-source 29/29 validation and durable public result package gates are satisfied. The next step is the non-destructive canonical fast-forward and verification of the newly created remote branch.

## ATLAS-A canonical promotion result — 2026-11-25

**ATLAS-A — ACCEPTED / PROMOTED.** The package-bearing candidate `19677ba9f429097e813835655260e55488a13419` received independent exact-tip `VALIDATED_CANDIDATE` review; the readiness successor `b6150fe20e00ca3332a4bda89c4ac4d5c85ca25f` received independent applicability **PASS**. The existing local canonical baseline `dba4a8c6ac85c4a7913a9a27b6e9d6ae0b69f9e0` was clean and an ancestor. The reviewed candidate and readiness successor were integrated without rewriting history.

No remote `codex/atlas/standalone` branch existed before publication. A normal push created it, and an immediate live-ref check verified `origin/codex/atlas/standalone` at `b6150fe20e00ca3332a4bda89c4ac4d5c85ca25f` (tree `c9aa948261e79ae9f9c398615d855e29c28f6819`). The push also confirmed current Core `935a958512042a8660d5bb183e959bb9bd430b35` and architecture `cb71a70fc503e926c3b3f39415b86f2fab896ba8`. The accepted application implementation remains `3e4ed67aecec74e857f6be080dea0e4f7cad5646` / tree `b0b572257a580570e8d1a36f7c0480ad937d2436`; the public 29/29 package remains the exact ZIP and manifest identified above.

This post-promotion closure record changes documentation only. It supersedes historical `PENDING`, evidence-blocked and `NOT PROMOTED` wording without removing the historical decision trail. ATLAS-B remains `WAIT_DEPENDENCY` on reviewed LAB-OBS-A artifacts and its separate real-world conformance gate; no downstream implementation was started.

## Shared viewport Atlas adoption candidate — 2026-11-25

This is a separate consumer candidate on the accepted ATLAS-A baseline. It does not revise ATLAS-A's `ACCEPTED / PROMOTED` status above. The earlier shared UI architecture-evaluation request did not itself authorize extraction; the later explicit user mandate for the bounded viewport integration supplied the separate implementation mandate required by the reviewed shared UI handoff. This Atlas adoption does not close the two-consumer shared viewport checkpoint; Dungeon adoption remains independently gated.

### Source and bounded adapter

- Atlas canonical base: `007af5799f9932e2a887cdb3edd64a9da1d674f4`.
- Implementation candidate: `c8e3c0a9b2a585544f6402cabeb969e5893c5019`, tree `d2f201b632007c5aaa6b8c264102cf47ee62d9e2`, branch `codex/atlas/shared-viewport-adapter`.
- Shared source commit: `b0e6130f44b6be62f4b9f26ea2ece22b47eef4c8`; the integrated `SharedDesktopUi` subtree is exactly `8f7b204878c1235b1e0c049762daff537407c92b`.
- The adapter delegates map-center projection and pointer-anchor recalculation through `Simulation.Ui.Viewport`. Axial long recentering, world geometry, hex vertices/radius, bounds, hit order, `Fit`, zoom/pan limits, visibility clamping, navigation, rendering and selection remain Atlas-owned. The equal-scale clamped case retains the previous pan recalculation order. No observation schema, reader, comparer, UI scale, or Demo fixture changed.
- The solution and project references build the shared source from this checkout. Updated lockfiles record the project graph; the shared project remains package-free and has no Avalonia or application project dependency. The desktop still directly references only Observations, which in turn references the pure viewport library.

### Validation and artifacts

Validation used .NET SDK `10.0.404`. Raw TRX and console logs remain in private owner-only local retention; they are not checked into Git.

| Gate | Result |
| --- | --- |
| Atlas locked solution restore after project-graph lock refresh | Passed |
| Atlas locked `win-x64` restore | Passed |
| Atlas Release suite | **33 passed, 0 failed, 0 skipped**; TRX SHA-256 `EE8F0AFE7C15B5E04679EF94B58F6EDE9F8AB5BA1385E93C32B115F01445135B` |
| Shared viewport library locked restore and Release suite from the integrated subtree | **22 passed, 0 failed, 0 skipped**; TRX SHA-256 `DCD6C3F62F54EA55DCFC240B5686C03B45221232E19BCEA95C022EC1F54C85B0` |
| Atlas Release solution build | Passed; 0 warnings, 0 errors |
| Self-contained `win-x64` publish through the normal Desktop project graph | Passed; `Simulation.Ui.Viewport.dll` is present and referenced by the published `.deps.json`; both bundled Demo observations match their source bytes |
| `git diff --check` | Passed |

Commands (run from `WorldAtlas/`, except the shared-library commands run from `SharedDesktopUi/`):

- `dotnet restore .\WorldAtlas.sln --force-evaluate --verbosity minimal` updated the project-reference lock entries; subsequent restores used `--locked-mode`.
- `dotnet restore .\WorldAtlas.sln --locked-mode --verbosity minimal`
- `dotnet restore .\src\WorldAtlas.Desktop\WorldAtlas.Desktop.csproj --runtime win-x64 --locked-mode --verbosity minimal`
- `dotnet test .\WorldAtlas.sln --no-restore --configuration Release -m:1 -p:UseSharedCompilation=false --logger "trx;LogFileName=atlas-viewport-adapter-final.trx" --results-directory <private-evidence-results>`
- `dotnet restore .\tests\Simulation.Ui.Viewport.Tests\Simulation.Ui.Viewport.Tests.csproj --locked-mode --verbosity minimal`
- `dotnet test .\tests\Simulation.Ui.Viewport.Tests\Simulation.Ui.Viewport.Tests.csproj --configuration Release --no-restore -m:1 -p:UseSharedCompilation=false --logger "trx;LogFileName=shared-viewport-integration.trx" --results-directory <private-evidence-results>`
- `dotnet build .\WorldAtlas.sln --configuration Release --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet publish .\src\WorldAtlas.Desktop\WorldAtlas.Desktop.csproj --configuration Release --runtime win-x64 --self-contained true --no-restore --output .\artifacts\shared-viewport\win-x64-final -m:1 -p:UseSharedCompilation=false`

The existing public `atlas-a-29-tests-v1` package is unchanged: ZIP SHA-256 `7244c87bd9caf3f5c65a943e9c764cee0c53463f0af59ef4a8fdec6fdeeffe29`; manifest SHA-256 `2476945c5e459ee51af93b2ec68fe36878db87f07d6145d3bc1d8f9cc1ec5118`. The new 33-test and 22-test raw reports are not represented by a new normalized public evidence package, so this record is not a promotion evidence package.

The user-facing executable is under ignored `WorldAtlas/artifacts/shared-viewport/win-x64-final/`. `WorldAtlas.Desktop.exe` is 162,816 bytes with SHA-256 `04A4396AFEFB2B770302EC32D3573F35AE59CBF989CE35B7C12F1D7B5F73A148`; the adjacent shared DLL is 9,216 bytes with SHA-256 `CB1E4AF249ABCBC63FCFB9436FF4D8D014E02924672B34736347CEC97B707542`. The complete 230-entry package is `WorldAtlas/artifacts/shared-viewport/WorldAtlas-SharedViewport-win-x64-acceptance.zip`, 75,478,389 bytes, SHA-256 `05A6AC500348F47259CB5BCAD79D5559BB5948942AE218EB3724BA32E7946A19`; it includes the executable, runtime dependencies, shared DLL, Demo fixtures and `NATIVE-ACCEPTANCE.txt`.

### Review and acceptance boundary

Independent exact-tip code review of `c8e3c0a9b2a585544f6402cabeb969e5893c5019` returned **VALIDATED_CANDIDATE** with no actionable findings. The reviewer verified the exact base and shared subtree, component arithmetic order, project/publish graph, boundary tests, 33/33 Atlas TRX, 22/22 shared-library TRX, build/publish result and owner-only evidence ACL. This section is a documentation-only successor bound to that reviewed implementation tip; no app or test inputs changed after validation.

Native interaction acceptance of this executable is **PASS**, confirmed by the user on 2026-11-25 for this Shared Viewport integration. The user verified automatic Demo World loading, pointer-anchored zoom, map panning, Hex selection, Inspector synchronization, keyboard navigation, Day 1 / Day 2 comparison (Wheat 20 → 25, observed delta +5), resizing, UI scaling and About behavior. No blocking native UX issue was reported. This acceptance applies to the `c8e3c0a9b2a585544f6402cabeb969e5893c5019` implementation and its published executable; it does not rely on predecessor-only acceptance.

The 33/33 raw Atlas report remains preserved locally with SHA-256 `EE8F0AFE7C15B5E04679EF94B58F6EDE9F8AB5BA1385E93C32B115F01445135B`; the 22/22 shared viewport report remains preserved locally with SHA-256 `DCD6C3F62F54EA55DCFC240B5686C03B45221232E19BCEA95C022EC1F54C85B`. The existing public `atlas-a-29-tests-v1` package remains unchanged. The approved 29-test profile is pinned to predecessor `3e4ed67aecec74e857f6be080dea0e4f7cad5646` and cannot normalize this 33-test run; the generic TRX profile requires a captured execution context not present in the retained raw artifacts. Therefore, normalized-publication evidence and exact-source applicability remain open. No canonical promotion or push is claimed in this record.

No ATLAS-B, LAB-OBS-A, real-world discovery or Dungeon Viewer integration work was started.
