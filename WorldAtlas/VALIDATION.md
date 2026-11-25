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
