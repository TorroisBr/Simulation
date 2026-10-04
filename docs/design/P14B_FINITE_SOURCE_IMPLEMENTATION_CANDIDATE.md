# P14-B finite-source implementation candidate

**Status:** `VALIDATED_CANDIDATE`; independent exact-tip code review is pending. This candidate is not promoted and does not close Phase 14.

## Ancestry and exact candidate

- Branch: `codex/phase14/P14BFiniteSourceCoreP12`.
- Implementation base: P12 canonical `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e`.
- P14-A canonical `4caecbbfb0464c965811402b3c11d8717605114a` is an ancestor of the implementation base.
- Core implementation commit: `4c7d9ab2c5b2d2b9abc5f45cc528397bce92327c`.
- Final focused-coverage commit: `cf9e6030b6b83ec12f548792a977404700fc4bd2`.
- Exact candidate tree after both commits: `06217d567edc86ff194f3c0461898ad343a9d87f`.
- Reviewed P14-B technical design: `95f2327667010edef79a3dcd21b31e8b7e5b16ab`; independent design review/handoff: `fed04f061485af5305ff32ab71b510928b757b12`; architecture base: `f6924e63d8e5731da1d33021d0361e7defe6dad7`.
- The P14-A to P12 implementation-base change is `BASE_DRIFT_ONLY`: P12 canonical retains P14-A and supplies the Market revision, prepared install, read-only stock view, mutation admission and post-commit notification hooks. P14-B reuses those hooks without a duplicate revision or bypass path.

## Bounded implementation

- The explicit `ExogenousDaily` default preserves P14-A and creates no finite-source owner. `FiniteReserveDaily` creates one source owner for one authored source, item, settlement/title, and Market custodian.
- The owner retains authored source identity, settlement/store/item links, content revision, daily output limit, initial and current reserve, local revision, and last processed day.
- The daily operation validates identities, current revisions, reserve bounds, matching Market-row cardinality, stock capacity and mutation admission; prepares a private Market replacement; then synchronously installs equal reserve debit and stock credit before notifying the existing P12 owner hook.
- City daily-flow dispatch runs finite output before the existing free-consumption sink. P14-A stays on its existing `AddStock` path and retains the existing economy gate and balance projection.
- Focused coverage includes capped output, already-empty/exhausted reserves, overflow/revision exhaustion, duplicate Market rows, stale revisions, identity mismatch, P12 mutation rejection, notification ordering, same-day duplicate rejection, repeat-input determinism, P14-A exogenous behavior, and material-flow balance.

## Validation evidence

All three final-tree gates passed on candidate tree `06217d5` with zero failed, inconclusive, or skipped tests:

| Gate | Result | Retained raw XML | Raw XML SHA-256 | Raw log SHA-256 |
|---|---:|---|---|---|
| `FiniteSourceProductionTests` | 11/11 | `EditMode-20261004-034626-bdf9b70aa11044d0b38f71b270babf7b.xml` | `57D533180EEDE3827048BE32E6FC9FF640C56494C91BA4F502B45CCE60F76EDD` | `09242DAF1223B155FD30E72C3BF92E0060D8F332DCC0B6A9BDF31D71A2D89C2D` |
| ALL EditMode | 2252/2252 | `EditMode-20261004-034646-ab95c56e39654dca80b9e4d45d29cb5f.xml` | `897BDA281B03F11FEF13A56163873674CA187B8278BBD719C2196F25E9990EEC` | `46FA47FE531E5073F9FCA897C9AFD25CD8D309CA768DC93F482D859836474030` |
| Official EditMode `Smoke` | 5/5 | `EditMode-20261004-034741-20be6bc8187c438fb44c24126f4e282a.xml` | `6E6746A7983625D027DBFAA6BBD69C80C823ED056EBB58A6581C13004181682F` | `749EC5AAE6D6EB52BE6599E3B46FA26371DC89D6D24BEFF5A0F404B1A4C42096` |

The binary archive `docs/validation/P14B/P14B-validation-artifacts-06217d5.zip` (SHA-256 `F6CCF38AF1821B1EE8C617E2D501B30B32582FA3CDB8387E1B3847804F74AEA6`) contains the exact raw XML/log pairs listed above. Archive inspection confirmed all six entries and their original byte lengths; this avoids Git text line-ending normalization of XML/log artifacts. The raw XML/log files were each hashed before archiving.

P14-A focused compatibility suites also passed: `LocalDailyMaterialFlowTests` 13/13, `SettlementStockOwnershipTests` 14/14, `PopulationConsumptionTests` 22/22, `WorldStateDiagnosticsTests` 46/46, and `EconomyTransactionTests` 45/45. These targeted runs preceded only a test-only addition to `FiniteSourceProductionTests`; no production source changed afterward. The final ALL EditMode gate above reran every test on the exact final candidate tree.

`git diff --check` passes. No Unity test output was edited. The archive preserves raw output bytes; this candidate does not rely on Git-blob XML hashes.

## Integration boundary and limitations

- The required `TesteSimulacao` pre-construction rejection of `FiniteReserveDaily` under P12 `UnityBootstrap-Daily-v1` remains a serialized bootstrap integration obligation. It is deferred until the active P10 bootstrap ownership window is released; this candidate does not modify `TesteSimulacao`, `SimulationRuntime`, P12 admission or P10 topology. A later combined integration must prove P14-A remains admitted and finite composition fails before owner construction/publication.
- The P12-selected profile must continue rejecting this owner until a supported profile explicitly inventories it. No P12-B completion, P12-A readiness, capture eligibility, export/hydration, P13 readiness, or Phase 14 closure is claimed.
- No multi-source, multi-item, multi-City, conversion, trade, transport, price decision, labor, timed production, P20 coordination or mod-loader scope was added. Intraday/extensibility and multi-participant alignment requirements do not add a dependency to this passive daily finite-source slice.
- Canonical promotion is a separate gate; this report records only a validated candidate awaiting exact-tip independent code review and serialized admission integration.
