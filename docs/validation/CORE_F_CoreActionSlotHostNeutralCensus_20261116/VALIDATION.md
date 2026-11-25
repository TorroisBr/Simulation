# CORE-F — Host-neutral CurrentAction census mutation

## Bounded implementation

The selected actor-turn boundary can now use the sealed Core owner-section census as the authority for a `Npc.CurrentAction` mutation when no host mutation protocol is supplied. Before the write, Core checks the exact graph/owner and the current witness against its registered identity, schema, cardinality, and revision baseline. After the write, Core refreshes the complete registered baseline vector with only the exact action-slot row changed, requires a newer local revision and cardinality 0–1, and advances the shared mutation epoch once.

A Unity protocol, when present, still validates Unity's action projection and refreshes only its compatibility mirror. An unsealed legacy path still requires its existing protocol. The change does not move the action projection into Core, replace the mixed-roster fallback, change the P12 identity contract, or establish the complete host boundary, `DailyV1Application.TryStart`, detached .NET restore, or CORE-F acceptance.

## Tested source identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`.
- Validation base commit: `99fbf59fda088e7b8c5b7ad4de57f1e4b9289369`.
- Base tree: `73b96eb48c02bec36706143cb12de75b72a09559`.
- The implementation and test working inputs were captured by the official runners and remained stable for each run. Their source digests differ because the Core and Unity runners inventory their respective input sets.

| Changed source/test file | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1ActorTurnExecutionStage.cs` | `0f354f0c43f016ea8e103cf6799b968685e67560cd58788a94100e169c2047e3` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs` | `ba5ef37ce10257bf28ad691bd0f76117d2c6baec9f45a73a6416c6e787be14c9` |
| `tests/Simulation.Core.Tests/DailyV1ExecutionBoundaryStateTests.cs` | `3a41402fa86b3f3a75db4dba5d4f9b5d65452ed7a37ae0f43efd2237adf610f1` |
| `tests/Simulation.Core.Tests/DailyV1SelectedDailyExecutionStageTests.cs` | `9d20fc62c80ea85b0c161749823fa5f1e8e23a6b46e9253a40c4e3f611e4a610` |

The later test-only addition is outside Unity's captured input roots. The two Core production source hashes above match the files in the stable Unity input snapshot. Existing ProjectSettings edits, local evidence, and unrelated untracked files were preserved and were not staged.

## Validation

- Official CoreValidation run `6ca349d0d8fd4603abf0fc0308401938`: **1,134/1,134 cold and 1,134/1,134 warm PASS**; all discovered cases executed, no failures/skips/inconclusive cases, and generated dependencies remained stable. Input digest: `8bc35005d7a91f0932ced9160fb31dbc4b0c429d58e89cefeee05f69117e3f85`. Private runner manifest SHA-256: `c34b90d7642079239f15fecd75f8d3cf026591aa063dd29b85325ed0ee87ed22`.
- Focused Unity EditMode (`CurrentAction`): **14/14 PASS**, run `9baf716945bb45a089e7fa861e5f6180`.
- Full Unity EditMode: **3,321/3,321 PASS**, zero failures, skips, or inconclusive cases, run `0108c1ce9da54aa888afd0cbc5053a70`.
- Official filtered Smoke: **5/5 PASS**, zero failures, skips, or inconclusive cases, run `a6b718931d92434f87c8fab43e854927`.
- All three Unity runs used Editor `6000.3.9f1`, had identical stable input digest `30c9b027553ab55fcf44efda6ff419607fc14f2fe281984cd0d233dddf804458`, and bound to the same base commit/tree. The 14 focused identities and five Smoke identities each appear exactly once with matching passing outcomes in the complete EditMode XML.
- The official privacy-safe Unity packager produced and verified the complete EditMode result package against its protected original run: [manifest](Unity/coref-action-slot-host-neutral-census-editmode.manifest.json), [result ZIP](Unity/coref-action-slot-host-neutral-census-editmode.zip). ZIP size: 176,307 bytes; SHA-256: `9ed40bf4783b5097842fef669933000c60684be6667e1aa349ba1673027764dc`. Manifest SHA-256: `9a0e4a7568e3faaacf6a88169f04b2711d382616954356dd1d810f214b65b4ba`. Official verification passed (`runs=1`, `entries=1`).
- `git diff --check`: PASS.

CoreValidation's native `simulation-core-validation/v1` manifest is retained with the cold/warm original reports in protected local evidence. The approved .NET TRX adapter requires a separate `dotnet-trx-source-context-v1`; it does not accept this native manifest. No source context was fabricated, and no raw report was modified. Therefore this record does not claim a durable public .NET result package or satisfy that later promotion evidence requirement.

## Review and disposition

Independent exact-tip implementation review PASS; see [review record](../../reviews/CORE_F_CoreActionSlotHostNeutralCensus_7676360_REVIEW.md). This is a recoverable candidate increment only; it is not Core canonical promotion or full CORE-F acceptance. The selected runtime still needs the approved shared application/session, runtime-port composition, and detached restoration/host work.
