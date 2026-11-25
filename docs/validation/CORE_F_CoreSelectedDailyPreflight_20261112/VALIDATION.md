# CORE-F selected daily execution preflight

## Scope

This checkpoint moves selected daily-stage preflight responsibility into Core. Core now verifies that the admitted City or City/NPC owner graph is exact, requires an admitted calendar when a City has local daily material flow, and derives the Merchant trade flag from the admitted effective configuration. Unity receives projection-only callbacks to validate that its current `CityRuntime`/`NpcRuntime` projections and host systems correspond to the Core graph.

This is an incremental authority boundary. It does not claim a Unity-independent `DailyV1Application`, Core-owned Unity projections, complete daily execution migration, detached restoration, or a fixed .NET host. Existing owner mutation, revision, epoch, action, and P12 persistence behavior is unchanged.

## Tested input

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Tested worktree base: `ef6f5011da1301f4c6e10bbd9dce841b4cde04fd`
- CoreValidation input digest: `69346e95935dbdea8d5485dff9ac4b719ff31ea3389b6865afa3fa2411a37881`
- Unity input digest (full and Smoke): `b48e66e939ff717878f581c2021903f0e1ad1d52630adf29cfe643c56c61768c`
- All 541 CoreValidation input files and all 1,982 Unity snapshot files were rehashed against the current checkout after execution; no missing or changed files were found. The three Unity run manifests also record stable before/after snapshots.

## Validation

| Gate | Result | Run |
|---|---:|---|
| Official CoreValidation cold | PASS, 1,103/1,103 | `ac6ba77a22da4cb296a84f7168c5969c` |
| Official CoreValidation warm | PASS, 1,103/1,103 | `ac6ba77a22da4cb296a84f7168c5969c` |
| Focused Unity `SimulationRuntimeAdmissionTests` | PASS, 246/246 | `979553d0894a46548bd0cf13897d64e5` |
| Full Unity EditMode | PASS, 3,318/3,318 | `da2a6dc7edff4dd6aab9307fd14c3f18` |
| Official filtered Smoke | PASS, 5/5 | `b6d012ca59ca400a875108bfbe8f51e8` |
| `git diff --check` | PASS | Candidate worktree |

The CoreValidation manifest and original TRX/logs remain in the protected private E: run directory. Its record reports `worktreeLibraryAbsent=false`; this checkpoint makes no claim that the .NET run was performed in a Library-free checkout. A public TRX package is not included: the approved adapter requires a complete `dotnet-trx-source-context-v1` context, which this CoreValidation run does not emit. Passing the runner manifest directly is rejected by the adapter; no context was fabricated and the originals were not altered.

The full EditMode result has a verified normalized result-only package at `Unity/Full/coref-selected-daily-full-ef6f501.zip`. Its adjacent manifest is `Unity/Full/coref-selected-daily-full-ef6f501.manifest.json`. The official verifier accepted the package against the original private run manifest (`runs=1`, `entries=1`). Archive size is 175,875 bytes and SHA-256 is `d59d2e53ce3168ff580c29ba9544b67c454e100fb207ebad497ffbac1c5bb1b2`; manifest SHA-256 is `6403934ecdedd0a64e6f4d974c035180f46932942365b6e4c225692071f7b184`. The package contains no absolute local filesystem paths.

The separately filtered Smoke run passed 5/5 and all five case identities occur in the packaged full EditMode report. The packager rejects the filtered Smoke XML because its `Simulation` suite `testcasecount` does not match its descendants. That original XML remains private and unchanged; no separate Smoke archive is claimed.

An initial CoreValidation invocation from a non-project working directory failed at restore because the runner uses fixed relative project arguments. Its logs remain private. The successful invocation started from the E: checkout and passed; the failed setup attempt is not treated as a source failure.

## Independent review and promotion

Exact-tip independent review: **PASS**, no actionable findings, for commit `a1b16a1deac1e7695592cca5f0159d1843b8d021` and tree `9657f269086757492e86e1bbea5a30d23a448819`. See `docs/reviews/CORE_F_CoreSelectedDailyPreflight_A1B16A1D_INDEPENDENT_REVIEW.md`.

This candidate has not been promoted to Core canonical. The complete CORE-F application/host package remains in progress.
