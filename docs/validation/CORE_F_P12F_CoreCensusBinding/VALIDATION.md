# CORE-F P12-F Core census binding validation

Status: **validated candidate work only**. This bounded source change does not promote Core canonical, complete CORE-F, change P12 scope, or close a phase.

## Scope

The Core runtime capture path now rebinds the ActorChoice, ScheduledDirective, TravelParty, and Expedition owner rows to witnesses read from the exact admitted Core owners. It requires those rows to remain Required and to match the current schema, owner identity, cardinality, and revision. ActorChoice is additionally bound to the same PersonStore as the selected owner graph. Missing, duplicate, foreign-owner, and stale-revision rows are rejected. The complete existing owner vector is retained; this does not add or remove a family from the accepted 89-family Daily-v1 contract or claim new writer/epoch coverage.

The implementation follows the promoted runtime/restore contract's exact-owner capture and recapture boundary. It does not implement `DailyV1Application.TryStart`, detached .NET continuation, or full CORE-F acceptance.

## Source identity

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`.
- Base: `8651385f14a777ffee5a4051367792f83e47bb16` (reviewed Genealogy restore binding and review record).
- Code/test candidate: `b9ff9a1ce0a8751e373c2f31a1454533a1c72f61`.
- Candidate tree: `693b3311f04f572d70c097d13e04cb490afebf13`.
- Changed implementation/test files: `DailyV1RuntimeAdvanceStage.cs` and `DailyV1ApplicationSessionGateTests.cs`.
- The test adds positive exact-owner assertions and rejection cases for missing, duplicate, foreign, and stale rows. Existing assertions were retained.
- The Unity validation input inventory was stable across 1,974 files (digest `791df034453b8ae910802f5a372e5ed46bd47db5ea500ae4ea4651d031c0a8dc`); Unity executable identity was stable. The run manifest binds source HEAD and tree above.

## Validation

| Gate | Result | Evidence |
|---|---:|---|
| Official CoreValidation cold | 966/966 passed | Run `cc95a32e1ddf4333bfd97ceb3fb48a7b`; zero failures, skipped, or inconclusive. |
| Official CoreValidation warm | 966/966 passed | Same run; all CoreValidation, CalendarDemo, and Lab command gates exited successfully. |
| CoreValidation storage independence | PASS | Exact committed source was tested in an isolated worktree with no Unity `Library`; runner manifest says `worktreeLibraryAbsent=true`. |
| Full Unity EditMode | 3,283/3,283 passed | Run `9eb32210a5f549508895a71b78cd1bac`; Unity 6000.3.9f1; zero failed, skipped, or inconclusive. |
| Official filtered Smoke | 5/5 passed | Run `5e429ca081f74f69931ff3d468b99756`; filter `Smoke`; stable input and executable. All five Smoke cases also appear as passing identities in the complete EditMode archive. |
| `git show --check` / `git diff --check` | PASS | Candidate code commit. |

The complete EditMode result is published as the privacy-safe package [coref-p12f-core-census-editmode.zip](coref-p12f-core-census-editmode.zip), SHA-256 `b9cfbf758f1dd5801ac9533d30a0479a809a10be150aa2ec012e05a3b632bd6b`, with [its manifest](coref-p12f-core-census-editmode.manifest.json), SHA-256 `f3f03f2496f55e1f7abf0d787c48a518b4b95a2f83eacb9c3078772405f75a80`. The official packager verified the package against its protected source manifest. The archive contains 3,283 cases, including all five passing Smoke cases.

The standalone filtered Smoke report is retained in the protected local evidence run. The package verifier rejects that filtered XML because the parent suite's declared case count does not match its filtered descendants. No filtered XML was changed or claimed as a normalized package; the complete EditMode package provides the five Smoke case identities and outcomes.

CoreValidation's original cold/warm TRX files and private run manifest remain in protected local evidence. The runner recorded manifest SHA-256 `e56736ce29bf871e8dfb97661ebb2e4ffac7884f50bacdf67434713c416eb7a5`; cold TRX SHA-256 `5e3ad066a0b15d0a3b49fddad75341cda50b8507d0eef439769460358bdf7724`; warm TRX SHA-256 `3dcd1b45f68e3729fd48868c28c72383f70e2d87ddb4397d93c70c9295f2026e`. This runner did not produce the additional private `dotnet-trx-source-context-v1` required by the approved TRX adapter, so no normalized .NET TRX package is claimed or fabricated.

An earlier Unity attempt passed tests but was not accepted as evidence because Unity normalized two ProjectSettings files from CRLF to LF during first import, making the input snapshot unstable. Its protected run remains preserved. The accepted exact-source run started from an LF checkout matching the Git blobs and reported a stable before/after inventory. The implementation worktree's user ProjectSettings edits were not used or touched by Unity validation.

## Review and promotion boundary

Independent exact-tip review of this code candidate is pending. Core canonical remains unchanged. This is one recoverable integration milestone within the incomplete CORE-F package; it does not establish full owner closure or application/host readiness.
