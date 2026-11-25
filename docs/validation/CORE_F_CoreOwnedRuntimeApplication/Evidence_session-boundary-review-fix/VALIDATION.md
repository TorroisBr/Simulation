# CORE-F DailyV1Application session-boundary validation

## Candidate inputs

Candidate base is `49d823ef81178107795939bdf317a8393ef50c49`. The implementation/test source inputs were byte-identical between the candidate and isolated E: Unity validation worktree; the three protected `ProjectSettings` files also matched by SHA-256 before and after execution. The complete Unity input inventory is retained in the public manifest.

| File | SHA-256 |
|---|---|
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `e7ca8290031cbb1e3af2775df63f0f25fe8edf7aa966e23f0201e05585046905` |
| `Assets/_Project/Scripts/TesteSimulacao.cs` | `bc3ba1216a052742b12854ce5709493a6925f937fa6777d40cafbb5b4d78df15` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeOrchestrationTests.cs` | `dc59de262d7e017310b19d8fd76719bda835fd3e01e9be8994f8dcc2d2481b68` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | `40e0fb0bd4b1dfb621af55236f08791f78001bb906773d0737a1a080583076ff` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1ApplicationSessionGate.cs` | `734fb14fac6084dcf01f8b3b662f36f5426714c3cca61ee91d3e552162233019` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs` | `0ca2f38efed96dab95b95979d402ad85b8a8b9024ffab2741e9bcd459ae7f4fd` |

## Validation results

- Official CoreValidation: run `3e2ef52c803e4f07a8a32acf68cb0389`, cold **704/704 PASS**, warm **704/704 PASS**. The runner's originals and private manifest remain in the E:-local owner-only evidence directory. The runner did not emit the required `dotnet-trx-source-context-v1`; therefore no public .NET TRX package or private-to-public projection is claimed for this checkpoint.
- Focused Unity EditMode: run `4c74d20f138e439bbb7174bbe4cb22f7`, **1/1 PASS**, stable input snapshot `a0deb7e53fa95abd470fb98385f9b4ab1c7c7e9a377bef8dd6bd84506059fa06`. Its private original remains retained. The packager refused the filtered report because the parent `Simulation` suite count did not match its descendants; it was not published.
- Full Unity EditMode: run `19f898f2c69e41019fdba958dfe78e4a`, **3259/3259 PASS**, zero failures, skips, or inconclusive cases. Snapshot was stable with 1,858 input files and SHA-256 `a0deb7e53fa95abd470fb98385f9b4ab1c7c7e9a377bef8dd6bd84506059fa06`.
- The full run includes the focused application-bound diagnostic test and all five Smoke cases; all five passed.
- Independent exact-source review: **PASS**, see [review record](../../../reviews/CORE_F_DailyV1ApplicationSessionBoundary_49D823EF.md). Reviewed working diff SHA-256: `76ba4c88d41adda1b9472c0833a4d3cfb074e7ee326481fe32402efc1a898c07`.
- `git diff --check`: **PASS**; only existing ProjectSettings line-ending warnings were emitted.

The public result-only Unity archive is `coref-session-boundary-review-fix-full.zip`, SHA-256 `740d69889beef79a5603be94a17f21b7adb4b3dbc4091ad59ba6f625e00c4e0b`, 172,112 bytes. Its public manifest is `coref-session-boundary-review-fix-full.manifest.json`, SHA-256 `cb7794ef2aeac050c6465d3391198e997a8ec4479421671d0dcdecb463e2e00c`. Package verification passed with private run-manifest comparison. Raw Unity XML and logs remain in owner-only local retention; they are not committed.

## Scope limit

This checkpoint binds each Core application session to one exact runtime advance boundary before publication and preserves the typed host diagnostic when the selected loop advances through the application. It does not complete the Core-owned runtime/application, P12 detached restoration, fixed .NET host, separate-process continuation, or CORE-F acceptance, and it does not advance Core canonical.