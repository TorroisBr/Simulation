# CORE-F application-owned continuation image seal

## Scope

The active Core continuation-save operation now delegates image sealing to its captured `DailyV1Application`. The application verifies that the operation is active, belongs to the current application/session and owner thread, and that the header, causal roots, P9 binding, and completed-boundary token match that session. It composes with the owner vector carried by the operation witness and revalidates the operation after composition. The Unity restore coordinator uses this path for operation-backed capture. Its existing no-application compatibility path is unchanged.

This is an incremental Core/P12 integration checkpoint. It does not complete P12 capture/restoration, `DailyV1Application.TryStart`, the detached .NET host, or CORE-F.

## Source identity

- Base: `0f047b28285376a93ee7e9ca65e291b7a0ef77f4`
- The four changed inputs were tested as working-tree changes on that base; their exact SHA-256 values follow.
- Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

| Input | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | `451b185c87d8e96104fc989d0ff96a0d76f65b628c0755d70d44954b9e827ba9` |
| `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GContinuationSourceWitness.cs` | `4535d57fd163e94898b9ed17eb81576b93f7b1e995967c7e96fbc080c6acc9bb` |
| `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.cs` | `3a801c71da9de8b829922065b0d37a94ebaa59b4f6075485fd023c022e4fa8d6` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `cb59c79d7c5c2112b6aaed10c1fa8ead66753353d8448651aeee08d4e2f73116` |

## Validation

- Official CoreValidation, run `56816e7e013d4a27af4a36ca16700780`: cold 936/936 and warm 936/936; input digest `75c66a3e42d0004494d6801b6203d6eeae57c72f285c4fc65c07f637784d3d99`; stable inputs. Protected manifest SHA-256 `101d9c123cdfbea324ceaa4e488cf979b2b9f8c1aea14e3da77af2fbc9c2220a`.
- Official focused Unity persisted-image restore/continue, run `642c9a8c7c41443faedd3f9813df7f0b`: 1/1 passed, stable inputs. Protected manifest SHA-256 `25ef48109d621cd2ec21ede0d53a61fd6fe90a01c445d0711fbfa0c02d5fdfd9`; original XML SHA-256 `cc2a7d9055b9bf7718a00527ffd81e5925983421a304571acdff8a82c2ce4812`.
- Official focused Unity save-operation closure/invalidation, run `27c9fa2835c2478690e719c58319ec4f`: 3/3 passed, stable inputs. Protected manifest SHA-256 `470b50bd3d84b76af0ba02bdb685908cbb861f2cdb0b9ff52e7a323c12085e6d`; original XML SHA-256 `ca905f133ea7cd51a8c9ad6c26b2cb193bb1d97a25711398950f3b48b17abe9c`.
- `git diff --check`: passed on the candidate worktree.

Raw XML, logs, manifests with machine-local metadata, temporary outputs, and private validation directories are not included in this repository record. Their originals remain protected. Earlier failed focused probes exposed the null-application call path; the operation-owned delegation corrected it. They are not counted as passing validation.

## Review and limitations

Independent exact-tip review of this implementation change is pending. No canonical promotion is claimed. Before any future promotion, independently inspect the source and tests, verify the private original reports, complete the applicable permanent evidence packaging/privacy review, and run the required broader integration validation for the then-current candidate.