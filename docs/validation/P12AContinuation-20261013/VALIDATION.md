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