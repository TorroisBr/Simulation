# CORE-F Core-owned CurrentAction Census Boundary

## Scope and source identity

This checkpoint moves the selected P12 `Npc.CurrentAction` section baseline and its shared mutation-epoch publication into the sealed Core owner census. Core verifies the exact owner instance and unchanged pre-write witness, then stages the complete census vector with only the changed action-slot row replaced. The successful write advances the shared epoch once. Unity retains its projected-action check and synchronizes only its legacy compatibility census mirror. Non-selected/unsealed compatibility execution retains its existing path.

It does not complete the full Core-owned runtime, all 89 P12 census families, `DailyV1Application.TryStart`, detached restore, or the .NET host. No Core canonical promotion is implied.

The runs used base commit `0454135ecfc47a625c075d138e50a1c83cf0f83d`, committed tree `eb7a983c7b6079dbec825ec10abd1b6dabafa60b`, plus the six implementation/test working-tree inputs listed below. CoreValidation recorded stable input digest `67ed88b8a0a5f9211313201d0f1f86c4db8c7847eb86eb7263a1abb5b42fa1c5`; Unity recorded stable digest `0acb8bfb2c95a6a60e9c5b9ce9174f983ad027e5f3d16fa699f4701ef40add88` before and after each run.

| File | SHA-256 |
|---|---|
| `Assets/_Project/Scripts/ContinuationCensusProtocol.cs` | `ce64fb7831ef9a3cf651e5d3962972aada198456f80c02e72d3335f2f445145b` |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `8a0947ce6f265f41460f08f0dd1c9616d0da1432599c459e2947d8f822e310a2` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1ActorTurnExecutionStage.cs` | `260112679d1a0b61d2f0e9310b0573ced2e8eeb086d6bdd56bc69a4d6556f5ce` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs` | `697edfb2e73496ad5aa9c48af5bd309f8d26ab4be6c3cbd9c5c7bf65d2956cc3` |
| `tests/Simulation.Core.Tests/DailyV1ExecutionBoundaryStateTests.cs` | `0b258fe4bc535b208c3f24bfab7237653bbe93fb367d4d6c1a143b8e490396c0` |
| `tests/Simulation.Core.Tests/DailyV1SelectedDailyExecutionStageTests.cs` | `f4ee38e4d05294748fab301658245ad439fb073bf775a4b018f21f794719f7ad` |

## Validation

- Official CoreValidation run `c76e60a7d7754150bef104fd6ea4205a`: **1,132/1,132 cold and 1,132/1,132 warm PASS**; discovery and generated dependencies stable. The original private manifest SHA-256 is `101460a12f748ad68ba3cea582ce9c0d7144863e99072ae413d7e02c4665c284`. Original reports and logs remain in protected local evidence.
- Focused Unity EditMode (`CurrentAction`): **14/14 PASS**, run `1e808acc56b14967b319001becaf4ee2`; stable input.
- Full official Unity EditMode: **3,321/3,321 PASS**, zero failures, run `bec495f2f73d49b1a725ff9b54ea464f`; stable input. Every focused case identity and result occurs in the full XML with identical outcome.
- Official filtered Smoke: **5/5 PASS**, run `846fa0729f224774a5afe7de1ad3cf16`; stable input.
- Unity editor: 6000.3.9f1; Test Framework: 1.6.0.
- Privacy-safe full EditMode package: [`coref-currentaction-census-editmode.zip`](Unity/coref-currentaction-census-editmode.zip), 176,968 bytes, SHA-256 `16990ce93195c769fe9ab2348bf4e7c433086a1c7ed91f6e44f3f8ef15648f6e`. The official package verifier passed against the retained private run manifest (`runs=1`, `entries=1`). Its public manifest is `Unity/coref-currentaction-census-editmode.manifest.json`.
- The CoreValidation runner emitted its native `simulation-core-validation/v1` manifest. The public .NET TRX adapter requires a separate verified `dotnet-trx-source-context-v1`; it rejected the native manifest. No context was fabricated, no original report was changed, and no public .NET TRX package is claimed. The cold/warm originals and runner manifest remain protected in private evidence.
- `git diff --check`: PASS.

## Review and state

Independent exact-tip review: PASS for implementation commit `ebd6406fea0ecb8a8e4c48842ad8cef3d7609f8a`, recorded in [`CORE_F_CORE_CURRENT_ACTION_CENSUS_REVIEW_EBD6406F.md`](../../reviews/CORE_F_CORE_CURRENT_ACTION_CENSUS_REVIEW_EBD6406F.md). The review found no actionable implementation, contract, test-integrity, or evidence-package findings. This is an incremental CORE-F checkpoint only; it is not canonical promotion or CORE-F acceptance.