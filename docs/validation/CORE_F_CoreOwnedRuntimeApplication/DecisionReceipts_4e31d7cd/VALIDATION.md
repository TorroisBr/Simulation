# Decision Occurrence Receipt Review-Fix Validation

## Scope and source identity

This is an intermediate candidate checkpoint for the approved CORE-F decision-occurrence receipt authority, not CORE-F completion or canonical promotion.

- Candidate code commit: `4e31d7cdc19628c403e9503ddbcd9514f08fd1df`.
- Committed source tree: `b4898ec7d62869bde10dee20db5736d81d688b69`.
- Parent candidate: `088244f0f9f67f38dabdaad2a45c40f6fed6e7ec`.
- The code commit changes five intended production/test files only. It addresses both findings in the earlier exact-tip review of `93ec441801712a5e522b98ec84d04098ef0e5ee3`: preserve the default recorder logger for receipt diagnostics, and reject a missing decision-store owner before restored runtime/session publication.
- The earlier independent review record is `d8d88cf5ec94a5116521d5fa8573a91cb9b49910` on its separate review branch. Its `NEEDS_CHANGES` verdict remains historical evidence and is not overwritten by this candidate.

The new regression coverage verifies that a public recorder constructed without an injected logger still emits the expected failure diagnostic and publishes neither a decision nor a receipt after sequence exhaustion. Restore-boundary regressions verify that both supported restoration paths return a diagnostic failure and publish no candidate/session when the selected decision store is missing.

## Official CoreValidation

- Cold: 916/916 passed; warm: 916/916 passed.
- Exact commit/tree: the identities above; stable input digest `117f50eda40385d584366f12c543c9a687e49dff37313986dc852b098d80da73`.
- Protected run manifest SHA-256: `73a777a1b13e733a70b73aedb95c812767bb602c127546eb62e80f44a48e8ecf`.
- The original TRX and logs remain in the private evidence store. This record does not claim that private originals were repackaged or that the run was Library-independent.

## Official Unity validation

The repository-supported evidence wrapper ran against the exact candidate commit with Unity `6000.3.9f1`. Both accepted runs report stable input snapshots and a stable Unity executable.

| Run | Selection | Result | Protected manifest SHA-256 | Original XML SHA-256 | Original log SHA-256 |
|---|---|---:|---|---|---|
| `eb0e66758f9f42338f10ef4d06abcdb5` | Full EditMode | 3277/3277 passed; 0 failed, skipped, or inconclusive | `ce83cb4c17b066d9c4b590e592db613c25a401c7b3fddf49e557b8a7208a5126` | `cfc99a6bb5c107b45ed84b4737e623f6967f2d254ca4d4434d07990397630dfc` | `ff6f557e394b6303b15e0eb7688b4e5b4eea97773ff06f1f66da498aa440fc03` |
| `94fbe0c6db8f4f2da4646f7d40ac6025` | Official `Smoke` filter | 5/5 passed; 0 failed, skipped, or inconclusive | `086b81d85fa8bcee714ee077c934ea3f7688d751831fd200f699a3d9f9bb8758` | `d8fd7917ab1a4c5b5407caf7f6d709ad5b32d3c50941b0b69a7c01525367a0bb` | `eb99b2b4fe6b37dbb1e38e0eb7688b4e5b4eea97773ff06f1f66da498aa440fc03` |

The full EditMode XML is distributed only as the normalized package below. The Smoke XML and both runs’ original reports/logs remain private and unchanged.

## Privacy-safe full EditMode package

- Manifest: [`coref-decision-receipts-editmode-4e31d7cd.manifest.json`](coref-decision-receipts-editmode-4e31d7cd.manifest.json), 468,994 bytes; SHA-256 `cb76c6cb25634077d817972029f0e6f23a6f260c0aef743fe64eafa7aedac937`.
- Archive: [`coref-decision-receipts-editmode-4e31d7cd.zip`](coref-decision-receipts-editmode-4e31d7cd.zip), 173,925 bytes; SHA-256 `4adc06df2d0c974a5cd40928df14015d99f1462e3806343d3e20e51b03776217`.
- Official verifier: `VALID`, one run, one normalized XML entry; the package is bound to the protected full-run manifest.
- A privacy scan found no local workstation paths or credential-like material in the package manifest or normalized XML. The XML output elements were removed and the absolute local assembly-suite path was projected under the approved policy. Original XML/logs were not changed.

## Environment diagnostic and validation disposition

An initial full run (`075f83409948441aaba74bde33328305`) is retained privately but is not counted: Unity’s first import changed validation inputs, and the nested temporary root made the persistence fixture’s generated path exceed the legacy Windows path limit. A temporary diagnostic confirmed `DirectoryNotFoundException` at creation of the long temporary filename. The diagnostic-only test was removed from the validation checkout before the accepted runs; no candidate source or test file was changed for that diagnosis. The two accepted runs used the project-prescribed short E:-local temp/evidence roots and report stable inputs.

The Unity validation checkout also contains Unity-generated local ProjectSettings changes from its initial import. Those files were not staged or included in this candidate. They are separate from the candidate worktree’s protected ProjectSettings changes, which remain untouched.

## Review status and limitations

`git diff --check` passes for the implementation range. The code candidate has not yet received a new independent exact-tip review after the prior `NEEDS_CHANGES` findings were addressed. No canonical promotion is claimed. CORE-F remains incomplete; this checkpoint does not establish a complete Core-owned Daily-v1 runtime, `DailyV1Application.TryStart`, detached .NET host, or T/T+1/T+2 continuation.