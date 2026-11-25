# CORE-F restored-image Core transaction validation

## Candidate

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Implementation commit: `95486e8e669a8b9018e6c89da442b95c8022354e`
- Implementation tree: `6980a266d08ba91f0dce2a07290126764301f70d`
- Base: `552966366307bde2fe15764a6dfcb77856d7eb38`
- Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

This checkpoint moves application-backed persisted-image admission, P12-C
staging, ordered P12-D/E/F Core staging, Core session composition, and the
unpublished stage token under one `DailyV1Application` transaction. Unity
callbacks only prepare the existing authored projection inputs and project
completed Core stages. Core invokes them in order and invalidates the private
stage token on failure. The legacy non-application restore path is retained.

The Core admission regression now verifies that a rejected initial-start
admission runs no image decode, root projection, host preparation, or owner
projection callback and publishes no active session.

## Validation

| Gate | Result | Run |
|---|---:|---|
| Focused `DailyV1ApplicationHostTests` | 21/21 PASS | `e4288d772dc948099181c9ccff291b4b` |
| Official CoreValidation cold | 1,017/1,017 PASS | `9213936a22644e5d8ca091c53f39db8e` |
| Official CoreValidation warm | 1,017/1,017 PASS | `9213936a22644e5d8ca091c53f39db8e` |
| Focused Unity restore rejection | 1/1 PASS | `29488bc5d346475eb80483612aecfe2b` |
| Full Unity EditMode | 3,299/3,299 PASS | `682631f373c7425c8efb9d2f79cdd96b` |
| Unity Smoke filter | 5/5 PASS | `e5df7701a2e544e887df220fff631666` |
| `git diff --check` | PASS | candidate diff |

All reported Unity runs have zero failed, skipped, or inconclusive tests. The
focused, full, and Smoke run manifests bind to the same stable Unity input
digest `9d1f7eda4f5d1ce207016431086d7cd3379758c8f9228655e78f64e95665dbc7`.
The full EditMode package contains the five Smoke test identities and their
passing outcomes. The separate filtered Smoke report remains in protected
local evidence; packaging that filtered report separately was rejected by the
official verifier because its NUnit parent aggregate count does not match the
filtered descendants. The complete full-suite report packages and verifies
successfully.

The official Unity package is
[`coref-restore-stage-transaction-unity-full.zip`](coref-restore-stage-transaction-unity-full.zip),
SHA-256 `5307a513f686f6e35daef17ac4b586937ad6239214546cc7727743ac17657004`.
Its manifest is
[`coref-restore-stage-transaction-unity-full.manifest.json`](coref-restore-stage-transaction-unity-full.manifest.json),
SHA-256 `45e8b2304541056e0b41a54fc3daa80789cb1b7fa998b38801ec7a8d8ca1f5d0`.
The official verifier compared the package with the protected original run
manifest and returned `VALID` (one complete EditMode report).

The CoreValidation runner's native manifest and original cold/warm TRX files
remain in protected local E: evidence. Native manifest SHA-256:
`12b17654d9c0cd9efb1452e3b04091d7f5b26e615635cf165dd52c74756a7aba`;
cold TRX SHA-256:
`354b213faed8add9f8ceeac00de15ce8125c2d343e21ad9fe34240f8be57ae3a`;
warm TRX SHA-256:
`89ed1958ed70dd3c01acb9ba2dcdf9f9c8f0c0fbdd898edc06f9d4ec1d6e0ea9`.
No public .NET TRX package is claimed: this runner emitted
`simulation-core-validation/v1`, while the approved TRX adapter requires a
separate `dotnet-trx-source-context-v1` record. No such context was fabricated
and no original report was modified.

The CoreValidation input digest was stable at
`f73659c3be8a72273b1881b28df16086a70bed0add7e0e2a9364c6994b4c7506`.
Its manifest reports that the worktree contains a Unity `Library`; this run
does not establish Library-independent .NET validation. The Unity runner
recorded base commit `552966366307bde2fe15764a6dfcb77856d7eb38` and tree
`037180ff68cb31de5426c284a1da190fb4030317`, plus the complete stable input
digest above. The hashes of the three changed source/test files in those
captured input inventories match the committed candidate worktree files.
The runner also captured the pre-existing `ProjectSettings/ProjectSettings.asset`
input; this checkpoint did not stage or alter that user change.

## Review and scope

Independent exact-tip implementation review is requested for candidate
`95486e8e669a8b9018e6c89da442b95c8022354e` and its validation record. This is
an implementation checkpoint only. It does not complete CORE-F, promote Core
canonical, establish a detached .NET host, or claim T/T+1/T+2 parity.
