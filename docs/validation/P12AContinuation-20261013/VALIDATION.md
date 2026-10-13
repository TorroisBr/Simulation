# P12-A detached Daily-v1 continuation validation

## Candidate and tested-source identity

- Repository: `https://github.com/TorroisBr/Simulation`
- Candidate branch: `codex/phase12/P12AContinuationImplementation`
- Canonical base: `25d2dcf1371137ceb0e158eef4ba02d2e2304612`
- Initial implementation commit: `5b6b0061b06aa4191db000070aa10ef23abd775c`
- Evidence-record/package commit: `3a341887ec23c5898f62b68bad11e24ccfc0c83a`
- Whitespace-normalized implementation commit: `23025975117433f8cc091144e868efc4abc78651`
- Whitespace-normalized implementation tree: `258471d25e82d1e4828145917716dbc400910416`
- Unity: `6000.3.9f1`

The three final validation runs captured source after the whitespace-only Unity metadata/EOF cleanup, while Git HEAD was `3a341887` and those 18 cleanup edits were present in the worktree. The validation inventory contains 1,531 inputs and has SHA-256 `e6dd9ecc625eeee8cc6fb608c540829e6f6e6514f9a6e965b245f4f92ad83b65`; all runs report `inputStable=True`. After committing the exact cleanup as `23025975`, every one of the 1,531 captured file hashes was compared with the candidate checkout: zero mismatches. No source, test, metadata, ProjectSettings, or other captured input changed after the runs.

The private wrapper reported PowerShell Core 7.6.5, .NET 10.0.11, compression assembly 10.0.0.0, and Windows 10.0.26200.

## Validation runs

| Run | Selection | Result | NUnit XML SHA-256 | Log SHA-256 |
|---|---|---:|---|---|
| `7a0c1ac6a88e4bf096fa29f1b444a235` | Focused P12-A, owner-package, invalid-save, and restored-session suites | 140/140 passed; 0 failed, skipped, or inconclusive | `ce8ece5b25c9781d845c319a6b56967a5bd77547db75f55665af5237fbe3f984` | `63272e7c189c44913530bf910f1c2db65125880d0ac799fdbfd42ad5214e1eb2` |
| `c6e74b14eb594c2987e94e80e139f60e` | All EditMode tests | 3059/3059 passed; 0 failed, skipped, or inconclusive | `bcfe9f24729db92c1a3fd29da2f43e841ed550d8ca8654ef43bfc7a30754ce3d` | `5342c89535188d33514b2695f75197e34448c6201cdf19d9bd3ac75adac85183` |
| `7af87e6e27624bfb9d6f11357a2a77e8` | Official Smoke | 5/5 passed; 0 failed, skipped, or inconclusive | `a4888dcfe4a0894f5ef24bdb8d2e5970e127807bfb2d5dfb111fd77df4b62b1f` | `59c959cd34c7115c93dc2ffb07c3b37fb9e73dde7303e4c9409024bceabe52f8` |

All three runs used the same 1,531-input inventory SHA-256 above.

## Evidence package and privacy

The promoted evidence tool packaged the complete EditMode run:

- Archive: `p12a-all-editmode-r2.zip` (159,891 bytes)
- Archive SHA-256: `eeb935a6c066be90bb387f451f1f195ce62329d3185a7b2711a3a30845ea8c2e`
- Manifest: `p12a-all-editmode-r2.manifest.json`
- Manifest SHA-256: `d905a0b5e13395d109e68f3efb008338073f61bf80c44eb0027a167c1bdd381b`
- Verification against the full EditMode private run manifest: PASS (`runs=1`, `entries=1`; archive hash matched)

The packager rejected the focused and Smoke XMLs when packaging them separately or together because their nested `Simulation` suite still declares the full-suite `testcasecount=3059` while those filtered reports contain only 140 and 5 descendant cases. The XML was not edited. A direct identity/result comparison against the complete EditMode XML found all 140 focused cases and all 5 Smoke cases, with zero missing cases, result mismatches, or non-passing selections. The complete EditMode package therefore contains the exact results for every selected focused and Smoke case; their distinct run IDs and original XML/log hashes remain listed above.

The manifest declares the packager's local-path redaction policy. It contains normalized test-result XML only; private diagnostic originals remain in the protected local evidence store. The earlier `p12a-all-editmode.zip` and its manifest are retained as historical evidence for the pre-normalization run; `p12a-all-editmode-r2` is the current package for this candidate.

## Checks and scope

- Exact candidate diff: `git diff --check 25d2dcf1371137ceb0e158eef4ba02d2e2304612..23025975117433f8cc091144e868efc4abc78651` — PASS.
- The user's unrelated `ProjectSettings/EditorBuildSettings.asset` and `ProjectSettings/ShaderGraphSettings.asset` edits, and three unrelated untracked `.meta` files, were not committed. Their captured input hashes remained stable during validation.
- Scope is the accepted selected-profile, same-build/current-host Daily-v1 detached capture, durable storage, detached load, restoration, and deterministic continuation contract. This is not a P13 readiness claim and does not close Phase 12.
- No canonical promotion is represented by this record.

## Independent-review correction and recaptured validation — 2026-10-13

Independent exact-tip review of candidate `4005000e293fabc1082e136c42a10a4c2f5dc8bf` against canonical base `25d2dcf1371137ceb0e158eef4ba02d2e2304612` returned `NEEDS_CHANGES`. It found that `File.Exists` could suppress access/I/O errors and let startup interpret an uninspectable continuation slot as absent, then start genesis. It also checked parity coverage and confirmed the integrated persisted-continuation test compares C roots (P9/P8, runtime identities, allocator, record sequence, random root) and D–F owners at T, restored T, both later boundaries, and idle replacement.

The correction now probes the slot by opening it. Only `FileNotFoundException` and `DirectoryNotFoundException` mean `NoSave`; access and I/O failures return `StorageFailure`. The detached load path uses the same missing-versus-inspection-failure distinction. `UninspectableDailyV1SaveFailsClosedBeforeGenesis` injects an access failure and verifies that startup remains unpublished, sets `bootstrapFailed`, and allocates no genesis `WorldId`.

The validated implementation was committed as `d54f0f1074f71061a9f546d4b30859f538a69e09` (`Assets` tree `f71eeaf8879b6e7bbe0ff611ada3794941d13de3`). All 1,531 captured source inputs had the same SHA-256 inventory before and after every run: `fc78a7553d296310d6b99e5b96692473d3625349ced7509039368d5ffd2a045a`; `inputStable=true` for each. After commit, every captured input file was compared with the checkout and there were zero mismatches. The unrelated ProjectSettings edits and untracked `.meta` files remained unchanged and unstaged.

| Selection | Result | Run ID | Original XML SHA-256 | Private log SHA-256 |
|---|---:|---|---|---|
| P12A codec suites (`P12A`) | 131/131 passed | `38f558472b364d1b88ffc9d4ed1f499c` | `2f7a5c53e8c13f29f1b943659bc988d6b3329de470c187407ed1c050088ee02a` | `4d1bc3f11a55d16d3ed8436185adaeedd05dd5706b495e3f4cad36e0e1982d99` |
| Startup inspection-failure regression | 1/1 passed | `3a0d6d98b03e48b1b6e5fdb22d873e29` | `556518a708fa43497c6ee7c738c9dfb005b00e791ecee8a7dc72279aea432a8e` | `acecd48ae9b25ee896311118c9915c6ef231cf34462cf60df4e27b8625a6ae42` |
| Persisted continuation parity | 1/1 passed | `5e64217a2ad849368f0b6ce330052e93` | `e62a544a45c3cfe3fc4d46e6843f61c29f50055163f2aeabcfdb87363101a3c7` | `ee73088b88fed373ea478930b24060f2d9fbf42b585d8863f7be4820c640d6f2` |
| ALL EditMode | 3060/3060 passed | `4c5d4835ef564bcca6cebb843ce09382` | `f63bedf75af61b75150f6dcb66f3328814b33a7f0bf67e693210e2143d589372` | `0cb496b65bb334c42814534a30f870ee35198fd9545a2fc9d680fe971de7207d` |
| Official Smoke (`Smoke`) | 5/5 passed | `d56169e8cb924d498b39ff0fa75e2a82` | `2f63fa752f826a5b14391a3687ca892058aed174dfa7d89f8916a72b3c7e562d` | `5a85078f88e08989de8b1b8fc04351c4e5d9f16c56b5bbca3ff0ac882b8c8f4a` |

Each run reported zero failed, skipped, or inconclusive cases. The full EditMode result is packaged with the promoted evidence tool:

- Archive: `p12a-continuation-review-fix.zip` (160,019 bytes); SHA-256 `f9d62bc532bdb0aca158e37e661fa31a8b9b396654c924ba7446599e6747c46b`.
- Package manifest: `p12a-continuation-review-fix.manifest.json`; SHA-256 `bad8ae2810a84fd2100c62aa74941e3924561130bae2c6d6a5fdec9f2e24382b`.
- The archive contains only normalized test-result XML; normalized XML SHA-256 is `89c6829ebd0f7836e2589391ee10da71de7e0a67fee167430858a0efe6f8ae1f`. The policy is `redact-absolute-local-assembly-suite-fullnames`; package verification against the protected source run passed (`runs=1`, `entries=1`).
- Focused XML and raw logs remain in the private local evidence store; the package contains no raw logs. The package and manifest contain no local user-root path.

`git diff --check` passes from canonical base through the implementation correction. This validation update changes no executable/test inputs and does not claim Phase 12 closure or P13 readiness.
