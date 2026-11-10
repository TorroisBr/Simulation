# CORE-F Selected Crime/Justice Session Binding — Validation

Date: 2026-11-10
Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
Validation base: `886aace112be63309f3c933c61434e27b84b4c36`
Status: candidate-only; no Core canonical promotion and no CORE-F completion claim.

## Scope

The selected Daily-v1 Crime/Justice host compatibility binding now carries the exact projected Core `JusticeRecordsOwner` facts and opaque host census/receipt identities. Core validates the relationships against the selected owner graph, pins the binding to the admitted execution-boundary session, and rejects a changed binding before later daily mutation. When admission is disabled, the existing host compatibility path still executes its mutation directly; this slice does not change that legacy behavior.

The change does not claim complete owner coverage, all 89 P12 census families, public host-neutral `DailyV1Application.TryStart`, detached .NET continuation, full P12 capture/restoration, or CORE-F acceptance.

## Exact implementation inputs

SHA-256 values below identify the source/test inputs validated by the official runners:

| File | SHA-256 |
|---|---|
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `9f7b69fbbf288303f3d04e2be889a3e31238a91b2555b6602891cd333f912dff` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `e4c907195da91fee298bf1e47e9ff0fc8c4904de9ee7276b6fd272df2a436a7b` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1ActorTurnExecutionStage.cs` | `840ff55c400fb171c9a486276a06426f138ec302c9ea8eae9ff5572fcd77274b` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs` | `92d52e46d43036c5d9b181d94220fce525323abc8911e48995b0269c082b45c3` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedDailyExecutionStage.cs` | `bca735d7952a89fe8d8a7456e742dfbc7d9be2d319055d93120edd358696fd8c` |
| `tests/Simulation.Core.Tests/DailyV1SelectedDailyExecutionStageTests.cs` | `a25dc6544fc616f3e2074a01354657bcbd714b71bdad6333b069edcaf7557d4f` |

## CoreValidation

Official CoreValidation run `029131e7fbe1423d95a9fc656807fb8d`: **1,088/1,088 cold and 1,088/1,088 warm PASS**. Captured inputs were stable (`3020ad6541b9d9af7d610ffd1b64da202809ad11b7ccab9dd879284b8a15f6ce`); the four Core source/test inputs above matched the protected run inventory. Original cold/warm TRX hashes are `7f2871d7beb2d9cc21a2dca18ca06a30ed0c7f137e48bc26ee0042523de75365` and `d1dadec7a4c37579112dee58e1ec202d7126fcb0c6a76e7a4f89431a88b4f7d0`. Private runner-manifest SHA-256: `5407966899b2dc03a0d23d31d016d816e855a7eef91b9371ba531158abb5ecef`.

The original TRX and manifest remain in protected local evidence. The native CoreValidation manifest is `simulation-core-validation/v1`, not the separately required `dotnet-trx-source-context-v1`; no public normalized DotNetTrx package or fabricated adapter context is claimed. The runner reported a Unity Library in this worktree, so this does not claim a Library-free validation environment.

## Unity EditMode and Smoke

- Official full EditMode run `3ec1e4b70c4243d0abe3fb86114e775f`: **3,305/3,305 PASS**, stable input inventory, zero failed tests.
- Official Smoke-filter run `174277b9a4cc429c8c3d470e8b3bf7ae`: **5/5 PASS**, stable input inventory, zero failed tests.
- The normalized full EditMode package is `core-owned-justice-editmode.zip`, SHA-256 `dd44103ff4107d0018cd192de95a144ac72a753a5d52cca28a7870811adcbbbf`; official package verification passed. Its public manifest and archive accompany this record.
- Smoke raw XML SHA-256: `1886d58dc4046926b3cf982a2f96e96cde87eea484c60be86bd7de09ee983d41`; raw log SHA-256: `0fe4685e90d6f66fb442e5e28b14033ada757c7a2969451e0168849b202b3fbf`. The official evidence packager rejected this filtered report because the `Simulation` suite's declared `testcasecount` does not match its descendants. The 5/5 runner result is retained privately; no public Smoke package or normalized report is claimed.
- Full EditMode raw XML SHA-256: `033920b4b29446df9f5fbdd2e19dd9b6af465d101ed7d23db035bfae877ef40c`; raw log SHA-256: `892a88b5b1e16ace6a8ba36159763cd034d68c29e770a109cc73c00bf5cf5812`. Raw reports/logs remain in protected local evidence; they are not committed.

## Integrity and limits

`git diff --check` passed. Full EditMode and Smoke both reported stable validation inputs. The full EditMode package was independently verified by the repository packager. Protected originals were retained unchanged. This is a recoverable implementation checkpoint only; it is not canonical promotion, Phase closure, complete P12 coverage, or full CORE-F acceptance.