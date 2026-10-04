# P14-B finite-source implementation candidate

**Status:** implementation update after independent review findings; the bounded fixes and focused regressions passed at code commit `8b64e62d851f8d39440eb04090a474a6e37a144a`. Full EditMode/Smoke gates and a fresh independent exact-tip code review remain pending. This candidate is not promoted and does not close Phase 14.

## Ancestry and exact candidate

- Branch: `codex/phase14/P14BFiniteSourceCoreP12`.
- Implementation base: P12 canonical `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e`.
- P14-A canonical `4caecbbfb0464c965811402b3c11d8717605114a` is an ancestor of the implementation base.
- Core implementation commit: `4c7d9ab2c5b2d2b9abc5f45cc528397bce92327c`.
- Final focused-coverage commit: `cf9e6030b6b83ec12f548792a977404700fc4bd2`.
- Exact candidate tree after both commits: `06217d567edc86ff194f3c0461898ad343a9d87f`.
- Reviewed P14-B technical design: `95f2327667010edef79a3dcd21b31e8b7e5b16ab`; independent design review/handoff: `fed04f061485af5305ff32ab71b510928b757b12`; architecture base: `f6924e63d8e5731da1d33021d0361e7defe6dad7`.
- The P14-A to P12 implementation-base change is `BASE_DRIFT_ONLY`: P12 canonical retains P14-A and supplies the Market revision, prepared install, read-only stock view, mutation admission and post-commit notification hooks. P14-B reuses those hooks without a duplicate revision or bypass path.
- The independent review of prior exact tip `b5a71fc5b69861c032457a0cd994a032ca03600c` found two bounded gaps: the legacy `CityRuntime.SimulateProductionDay` and prepared `TryPrepareDailyEconomyStep` production routes could add stock without debiting finite reserve, and the prepared-step identity omitted finite profile/source fields. Code commit `8b64e62d851f8d39440eb04090a474a6e37a144a` closes both gaps. It also rejects a finite owner reinterpreted through the exogenous production entries. The shared `TesteSimulacao` bootstrap/admission boundary was not edited; its P12 `UnityBootstrap-Daily-v1` pre-construction rejection remains serialized behind P10.

## Bounded implementation

- The explicit `ExogenousDaily` default preserves P14-A and creates no finite-source owner. `FiniteReserveDaily` creates one source owner for one authored source, item, settlement/title, and Market custodian.
- The owner retains authored source identity, settlement/store/item links, content revision, daily output limit, initial and current reserve, local revision, and last processed day.
- The daily operation validates identities, current revisions, reserve bounds, matching Market-row cardinality, stock capacity and mutation admission; prepares a private Market replacement; then synchronously installs equal reserve debit and stock credit before notifying the existing P12 owner hook.
- The prepared `Production` continuation now obtains a finite-source preparation from the domain service, then commits the source and Market owners together before publishing the receipt. The legacy no-boundary production entry fails closed for a finite owner or authored finite-reserve data. The daily owner revision now includes the profile discriminator, settlement/Location/store identities, and production item, rate, reserve, source ID and content revision; runtime reserve/revision remains protected by the prepared operation's owner-revision checks.
- City daily-flow dispatch runs finite output before the existing free-consumption sink. P14-A stays on its existing `AddStock` path and retains the existing economy gate and balance projection.
- Focused coverage includes capped output, already-empty/exhausted reserves, overflow/revision exhaustion, duplicate Market rows, stale revisions, identity mismatch, P12 mutation rejection, notification ordering, same-day duplicate rejection, repeat-input determinism, P14-A exogenous behavior, and material-flow balance.

## Validation evidence

The earlier full validation passed on code tree `06217d567edc86ff194f3c0461898ad343a9d87f`: `FiniteSourceProductionTests` 11/11, ALL EditMode 2252/2252 and official EditMode `Smoke` 5/5. Those results are preserved in `docs/validation/P14B/P14B-validation-artifacts-06217d5.zip` (SHA-256 `F6CCF38AF1821B1EE8C617E2D501B30B32582FA3CDB8387E1B3847804F74AEA6`) as prior-candidate evidence; they do not cover the review-fix code below.

The review-fix executable tree `edc20380ed3a162397d4c2f7c2f4e6490aa46db1` at code commit `8b64e62d851f8d39440eb04090a474a6e37a144a` passed these focused suites with zero failed, inconclusive, or skipped tests:

| Suite | Result | Raw XML SHA-256 | Raw log SHA-256 |
|---|---:|---|---|
| `CityDailyEconomyContinuationTests` | 19/19 | `8A36BBA2A921F17ADF065F10DA9B29D0DDAC21A0012C80B88FFF4A5CAF0ED846` | `C405D9FBFF66613442A72F34576651A29DB6B406E2699F81D6E876A82E6F0C79` |
| `FiniteSourceProductionTests` | 11/11 | `B78256116145A208155402628C50774EAF70816C78DE051316AC583836BE9D92` | `C00A15FBD204CBC188ACCD238BA638C5E7721E623EEC1F20E2C31E9915E7AF3F` |
| `LocalDailyMaterialFlowTests` | 13/13 | `3AEF2C13B2227F9653C2B8741937998A8F9B6D633683A1942F2F62C40C299027` | `6D6BC350CB6749A35812484A6537650500DA0955076F53AB46CE6F084142D547` |
| `SettlementStockOwnershipTests` | 14/14 | `5383FAB72939C5DD6F75BF2E2F0633DE1551F997939CFB86F4D44475EB9AFBDB` | `3C5BDFDF168A465D2F0514FA8957C9772A02EC77883E4F0FDF82597A6AD05A0E` |
| `PopulationConsumptionTests` | 22/22 | `9B9E28266B24C3287A57D8D82A045606765D3E61AABD1EEE00335C2A6CECCC82` | `3DCD5216E4E5D190D159817211AB6B31D634957646D816527F949E81356129AA` |
| `WorldStateDiagnosticsTests` | 46/46 | `A39BF96473D98A2EA4D0E388C452D24DAD72515D13391850006F528E55D39D22` | `80DED5274B69635857358D6F5A9E703B2B1228EA688A6E204555438B7C1E3D05` |
| `EconomyTransactionTests` | 45/45 | `705DDB0011FE0B7D50F1B61C515C38BBEDA55674B99E6D742741FF45E6B86726` | `564951A8A2759EE65669217ADAEE6D06C09A732A2888CB11B89A3842BEFE0B21` |

The focused raw XML/log files are archived in `docs/validation/P14B/P14B-review-fix-focused-20261004.zip` (SHA-256 `93BAC3588E854EBF8D7655B6AA665D975AE58664E40A0FD14D0DA1E7F8FF9DFC`). `git diff --check` passes. ALL EditMode and official Smoke have not been rerun on the review-fix tree while P10/P20 own the serialized Unity validation slot; this candidate is not yet a fully validated integration candidate.

## Integration boundary and limitations

- The required `TesteSimulacao` pre-construction rejection of `FiniteReserveDaily` under P12 `UnityBootstrap-Daily-v1` remains a serialized bootstrap integration obligation. It is deferred until the active P10 bootstrap ownership window is released; this candidate does not modify `TesteSimulacao`, `SimulationRuntime`, P12 admission or P10 topology. A later combined integration must prove P14-A remains admitted and finite composition fails before owner construction/publication.
- The P12-selected profile must continue rejecting this owner until a supported profile explicitly inventories it. No P12-B completion, P12-A readiness, capture eligibility, export/hydration, P13 readiness, or Phase 14 closure is claimed.
- No multi-source, multi-item, multi-City, conversion, trade, transport, price decision, labor, timed production, P20 coordination or mod-loader scope was added. Intraday/extensibility and multi-participant alignment requirements do not add a dependency to this passive daily finite-source slice.
- Canonical promotion is a separate gate; this report records only a validated candidate awaiting exact-tip independent code review and serialized admission integration.
