# CORE-F reserved baseline refresh for selected Crime/Justice commits

Status: **validated candidate checkpoint; not promoted**.

## Scope

The Core coordinator now passes its opaque daily mutation-epoch reservation
token to the registered P12 census adapter when refreshing changed Crime and
Justice owner baselines. The adapter accepts only the exact active token and
refreshes all listed provider witnesses without advancing the epoch. Core then
completes the same reservation once. The existing unreserved path still rejects
refresh while an epoch reservation is active; legacy and mixed-roster mutation
paths retain their existing host-owned notification behavior.

This does not migrate the 89-family census registry, change P12 capture or
hydration, or claim a Core-owned census. It does not complete CORE-F or establish
`DailyV1Application.TryStart`, detached hosting, or cross-process continuation.

## Candidate and source identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`.
- Validated source was based on candidate commit `a001664f55f8e7d4659d6729d9a69bb7f95d67a3`.
- Core canonical at validation: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- The exact-source CoreValidation recheck ran at commit
  `040ad0aae03340fd78b4d2272010c32504d4b6c8`, tree
  `a4fd703e5898ba0566eefd76afb9c44563ad5f90`, with a stable working-input
  digest of `e2e0cbb074de59e44380d3c92bd63859ba798341af9116d469a0fc8c4292cb5e`
  (541 inputs). A previously reported local run manifest could not be found and
  is not relied on by this record. Unity input digest: `7bd3217c1bfc5a667cc9d2584180b93c332f560be3408a0e106209945166074d`
  (1,982 inputs). The Unity snapshot also records committed base tree
  `2552fe3bb111025a4361502ae5a2d38c99fe335c`; the separate input digest binds
  the tested working source.
- Changed implementation and test files: `Assets/_Project/Scripts/ContinuationCensusProtocol.cs`,
  `Assets/_Project/Scripts/P12CrimeJusticeInvalidation.cs`,
  `Assets/_Project/Scripts/SimulationRuntime.cs`,
  `Assets/_Project/Tests/EditMode/Editor/ContinuationCensusProtocolTests.cs`,
  `Packages/com.simulation.core/Runtime/Application/DailyV1ActorTurnExecutionStage.cs`,
  `Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs`,
  `Packages/com.simulation.core/Runtime/Application/DailyV1P12CrimeJusticeMutationCoordinator.cs`,
  and the four corresponding tests in `tests/Simulation.Core.Tests/`.
- `ProjectSettings/ProjectSettings.asset` and the pre-existing unrelated
  validation-note edit were preserved and excluded from the candidate payload.
  The ProjectSettings Git blob matched the candidate base.

## Validation

| Gate | Result | Evidence |
|---|---:|---|
| Official CoreValidation cold | 1,122/1,122 passed | Exact-source recheck run `08fab9f52f7d4d2d84e271d09ddfc027` (TRX run `ed2b6195-692c-44dc-a0e9-4bb17920620c`); private manifest SHA-256 `acfb2269b365c57c022f39b7c52f6fc2f904387e6157ac7c61fdc87efd31881f`; original cold TRX SHA-256 `2cb7b7d7420deee2b643ab5deffdc9b7589e13979a66b79b87f1a422e3eb428b`. |
| Official CoreValidation warm | 1,122/1,122 passed | Same run (TRX run `a9ebd729-7a4a-4e5c-bf86-2be1634ec481`); original warm TRX SHA-256 `ac1244d0b461b21a5a73236fee0a6297bd96d012230f1afcdea70117d464a9ac`. |
| Focused production census regression | 1/1 passed | Unity run `3eab8a9a89ec410f8bc431fdc4e7074c`; original XML SHA-256 `844a06dc82d05446c340e2c170e27090e1192d7218dfee0820253c36a4de7eef`. |
| Full official Unity EditMode | 3,320/3,320 passed | Stable run `57b8a1dfceeb4ec9b679bfa27aa2402a`; original XML SHA-256 `672d3b47db35ab63f05dd5ed56b2ad3a692189575dcbcfd04d878782acc2c41b`; raw log SHA-256 `217a9e45f47cf8fef59371e35f08828ccef91e41dcac17ac1ef952d5042302e4`. The new regression and all existing LongRun tests are in this report. |
| Official EditMode Smoke filter | 5/5 passed | Run `74a697180d3b41168d7776c4874d1433`; XML SHA-256 `11d308773f9d19bff308f7480d591a92d9ff280c4523c38420ac6cb4cd013993`. All five cases also occur in the full EditMode report. |
| Unity evidence package | Verified against retained original | [Manifest](Unity/coref-epoch-reserved-baseline-editmode-20261112.manifest.json), [archive](Unity/coref-epoch-reserved-baseline-editmode-20261112.zip). Archive SHA-256 `8dcf17d9392c3698398462180d65711bcdcc3e02cbcae5cf5af863a4af27b34d`; manifest SHA-256 `3e75b5d8958ceb25dc8bcf3542d7cd681bc01e69597ce2e726efd2d8bd3a1796`. The package contains the complete full EditMode result; the Smoke identities are included there. |
| `git diff --check` | Passed | Checked after the implementation and regression changes. |

The official CoreValidation manifest is `simulation-core-validation/v1`, not
the separate `dotnet-trx-source-context-v1` required by the normalized .NET TRX
adapter. No source context was fabricated and no normalized .NET package is
claimed. The original cold/warm reports and runner manifest remain in protected
local retention; their hashes are recorded above. This run used a worktree with
a Unity `Library`, so it is not a Library-independent .NET host proof. The Unity
originals likewise remain unchanged in protected local retention.

## Review and limits

An independent static review found that an ordinary baseline refresh would be
rejected while the exact Core reservation was active. The implementation now
uses a distinct reservation-aware operation: the protocol checks owner-thread
affinity and exact token identity, the regression rejects the ordinary path and
a wrong token, and only Core completes the epoch after witness refresh succeeds.
The Core coordinator regression also verifies that it supplies its active
reservation token and advances exactly one epoch. Independent exact-tip review
of the committed candidate remains required.

P12-B remains within its existing scope. No claim is made about global
quiescence, full owner/epoch coverage, capture eligibility, export, hydration,
Phase closure, or CORE-F completion.
