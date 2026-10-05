# P14-B finite-source implementation candidate

**Status:** exact-tip implementation review and validation passed; checkpoint promotion is pending. Phase 14 remains open. See the authoritative dated run record below; earlier status text is historical.

## Authoritative current-base run - 2026-10-04

**Status:** exact-tip independent code review and validation passed; checkpoint promotion is pending. Phase 14 remains open. This section supersedes earlier statements in the historical candidate record that validation or P10 admission wiring remained pending.

### Exact integration and scope

- Candidate branch: codex/phase14/P14BCurrentBaseIntegration.
- Integrated base: 1ba58eeda6296be349b0a1def6d1d08a1fb1258f, combining P14 canonical 4caecbbfb0464c965811402b3c11d8717605114a with P10 canonical e53252de5277fd5af46bbacb8eda5ee6e74aff08; P12 canonical a6572ab3d4330d81edb334ae8b4c84ca5e6b173e is included.
- Architecture baseline: f6924e63d8e5731da1d33021d0361e7defe6dad7.
- Implementation commit: 709bf0ec198da5c64c276ba4ab6084faccba87ff; exact implementation tree: 5b5b18cb652d383e9ae9202066a95766c9097c20.
- P14-B enforces the reviewed one-City finite-reserve cardinality before City/runtime owner construction, rejects that finite profile during P12 UnityBootstrap-Daily-v1 profile resolution before world identity allocation, preserves P14-A ExogenousDaily admission, and allows the bounded finite profile outside the selected P12 profile.
- Bootstrap composition now binds each authored local-material-flow City to its stable P8 LocationId through the existing LegacySpatialAnchorBindingStore after geography is composed. This closes the observed P14-A and P14-B bootstrap gap while reusing the existing P8 anchor authority.
- Current P14/P10 bootstrap composition is deliberately deferred. P8's one-owner-per-Location invariant and both accepted proving scopes remain unchanged. Since the authored P14 City and P10 Ruin would claim the sole P8 Location, admission rejects the combined profile before world identity or runtime-owner allocation and publication. This checkpoint does not mint a City Location from P10. Future combined worlds require distinct factual Locations from the geography/genesis layer. City-to-ruins historical succession and Ruin-as-local/site content remain possible future representations, not claims or implementation in this slice.
- The daily source/Market prepared install, reserve debit, stock credit, local source revision, and exclusions remain within the previously reviewed P14-B contract. No P12 readiness, capture eligibility, export/hydration, P13 readiness, or Phase 14 closure is claimed.

### Exact-tree validation

All results below are from implementation commit 709bf0ec198da5c64c276ba4ab6084faccba87ff and tree 5b5b18cb652d383e9ae9202066a95766c9097c20.

| Suite | Result |
|---|---:|
| FiniteSourceProductionTests | 13/13 PASS |
| SimulationRuntimeAdmissionTests | 37/37 PASS |
| LocalDailyMaterialFlowTests | 13/13 PASS |
| CityDailyEconomyContinuationTests | 19/19 PASS |
| SimulationBootstrapCompositionTests | 21/21 PASS |
| P10RuinLocalTopologyGenesisTests | 6/6 PASS |
| P10BGeneratedRuinGenesisTests | 10/10 PASS |
| SimulationRuntimeLongRunTests | 7/7 PASS |
| ALL EditMode | 2293/2293 PASS |
| Official Smoke | 5/5 PASS |

git diff --check and fresh exact-tip independent implementation review passed. The raw NUnit XML and Unity logs are archived in docs/validation/P14B/P14B-current-base-anchor-deferral-validation-5b5b18c.zip (SHA-256 6FE147E20D38B054AEF4D5EE3792B57F657407977549A736FEA87F7CFA0F3EBB). Exact XML/log hashes, unchanged user-file hashes, and review details are recorded in `docs/validation/P14B/P14B-current-base-anchor-deferral-validation-5b5b18c.md` and `docs/design/P14B_FINITE_SOURCE_IMPLEMENTATION_REVIEW.md`.

The unrelated ProjectSettings edits and untracked ArmedForceSpatialPosition .meta files were not staged or modified; their SHA-256 values were checked before and after validation.

## Ancestry and exact candidate

- Original candidate branch (preserved): `codex/phase14/P14BFiniteSourceCoreP12` at
  `02ff62862cb953e29ad478d01d1a348c41a2ce38`.
- Current-base integration branch: `codex/phase14/P14BCurrentBaseIntegration`.
- Implementation base: P12 canonical `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e`.
- P14-A canonical `4caecbbfb0464c965811402b3c11d8717605114a` is an ancestor of the implementation base.
- Core implementation commit: `4c7d9ab2c5b2d2b9abc5f45cc528397bce92327c`.
- Final focused-coverage commit: `cf9e6030b6b83ec12f548792a977404700fc4bd2`.
- Exact candidate tree after both commits: `06217d567edc86ff194f3c0461898ad343a9d87f`.
- Reviewed P14-B technical design: `95f2327667010edef79a3dcd21b31e8b7e5b16ab`; independent design review/handoff: `fed04f061485af5305ff32ab71b510928b757b12`; architecture base: `f6924e63d8e5731da1d33021d0361e7defe6dad7`.
- The P14-A to P12 implementation-base change is `BASE_DRIFT_ONLY`: P12 canonical retains P14-A and supplies the Market revision, prepared install, read-only stock view, mutation admission and post-commit notification hooks. P14-B reuses those hooks without a duplicate revision or bypass path.
- The independent review of prior exact tip `b5a71fc5b69861c032457a0cd994a032ca03600c` found two bounded gaps: the legacy `CityRuntime.SimulateProductionDay` and prepared `TryPrepareDailyEconomyStep` production routes could add stock without debiting finite reserve, and the prepared-step identity omitted finite profile/source fields. Code commit `8b64e62d851f8d39440eb04090a474a6e37a144a` closes both gaps. It also rejects a finite owner reinterpreted through the exogenous production entries. The shared `TesteSimulacao` bootstrap/admission boundary was not edited; its P12 `UnityBootstrap-Daily-v1` pre-construction rejection remains serialized behind P10.

## Current-base P14-owned integration update

- Current canonical refs were refreshed before this isolated integration. P14
  canonical `4caecbbfb0464c965811402b3c11d8717605114a` is an ancestor of P12
  canonical `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e`; the P12 tip is therefore
  the current combined implementation base. The earlier candidate and its
  archived focused results remain intact on
  `codex/phase14/P14BFiniteSourceCoreP12` at `02ff62862cb953e29ad478d01d1a348c41a2ce38`.
- This isolated current-base branch replays that candidate's source/test and
  evidence commits without changing their original refs. The Market adapter
  uses the P12 `revision`, `TryPrepareFiniteStockIncrease`,
  `CanInstallFiniteStock`, `InstallFiniteStock`, and post-install
  `NotifyFiniteStockInstalled` path; it adds no duplicate Market revision and
  does not route around P12 mutation admission or owner notification.
- The current P14-owned source/test addition is commit
  `ad075ce8bb04a2a219a1ca4769f71345548c01dd`, tree
  `e2579dc8e65578b7ed62a3707163c0b728901b72`, based on P12 canonical
  `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e`. `git diff --check` passed for
  this code commit. Focused Unity validation is pending; no Unity process was
  started by this worker.
- Added `FiniteSourceProfileAdmission.TryValidateAuthoredCityCardinality` as a
  pure authored-input check. A caller can invoke it over the complete authored
  `CityData` list before constructing any `CityRuntime`; when the finite profile
  is selected it requires exactly one list entry, including rejecting a
  repeated definition reference that would construct two City instances.
  P14-A `ExogenousDaily` remains outside this finite-only cardinality gate.
  The current P10-owned bootstrap does not yet call this helper, so no
  pre-construction enforcement claim is made until that serialized integration
  lands.
- New isolated P14 tests cover a single finite City, rejection of an added
  City and repeated CityData entry, and the unchanged P14-A exogenous pass
  through the finite-only gate. These tests have not been run on this branch.

## Bounded implementation

- The explicit `ExogenousDaily` default preserves P14-A and creates no finite-source owner. `FiniteReserveDaily` creates one source owner for one authored source, item, settlement/title, and Market custodian.
- The owner retains authored source identity, settlement/store/item links, content revision, daily output limit, initial and current reserve, local revision, and last processed day.
- The daily operation validates identities, current revisions, reserve bounds, matching Market-row cardinality, stock capacity and mutation admission; prepares a private Market replacement; then synchronously installs equal reserve debit and stock credit before notifying the existing P12 owner hook.
- The prepared `Production` continuation now obtains a finite-source preparation from the domain service, then commits the source and Market owners together before publishing the receipt. The legacy no-boundary production entry fails closed for a finite owner or authored finite-reserve data. The daily owner revision now includes the profile discriminator, settlement/Location/store identities, and production item, rate, reserve, source ID and content revision; runtime reserve/revision remains protected by the prepared operation's owner-revision checks.
- City daily-flow dispatch runs finite output before the existing free-consumption sink. P14-A stays on its existing `AddStock` path and retains the existing economy gate and balance projection.
- Focused coverage includes capped output, already-empty/exhausted reserves, overflow/revision exhaustion, duplicate Market rows, stale revisions, identity mismatch, P12 mutation rejection, notification ordering, same-day duplicate rejection, repeat-input determinism, P14-A exogenous behavior, and material-flow balance.

The earlier current-base cardinality/owner-core tree `e2579dc8e65578b7ed62a3707163c0b728901b72` passed `FiniteSourceProductionTests` 13/13 on 2026-10-04. Raw XML/log hashes and artifacts are recorded in `docs/validation/P14B/P14B-owner-core-focused-20261004.md`. That historical run was focused owner-core evidence only; the assembled current-base admission integration is covered by the exact-tree validation above.
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

The focused raw XML/log files are archived in `docs/validation/P14B/P14B-review-fix-focused-20261004.zip` (SHA-256 `93BAC3588E854EBF8D7655B6AA665D975AE58664E40A0FD14D0DA1E7F8FF9DFC`). Those results validate the pre-cardinality review-fix tree only. The current branch adds the pure cardinality helper and tests, so neither archive is evidence for the current source/test tree. ALL EditMode and official Smoke have not been rerun here; the Phase Master will serialize validation after P10's shared boundary work is ready. `git diff --check` on the current branch is recorded separately at commit time.

## Integration boundary and limitations

- The current-base candidate owns the admission integration at `TesteSimulacao.InitializeSimulation`, during `p9.genesis.resolve-profile/v1`, before `SimulationGenesisPipeline.ValidateProfile` or any `CityRuntime`/Market/source owner construction. It invokes P14 authored-profile admission after P10 profile resolution. The selected P12 Daily profile rejects the finite-source profile; P14-A ExogenousDaily remains admitted. The accepted user boundary additionally rejects a simultaneous P14 City plus P10 Ruin profile at the same early stage, before world identity or owners are allocated. `SimulationRuntimeAdmissionTests` verifies these early exits and the standalone profile cases. P8 anchor ownership and P10 topology semantics are not changed.
- The P12-selected profile must continue rejecting this owner until a supported profile explicitly inventories it. No P12-B completion, P12-A readiness, capture eligibility, export/hydration, P13 readiness, or Phase 14 closure is claimed.
- No multi-source, multi-item, multi-City, conversion, trade, transport, price decision, labor, timed production, P20 coordination or mod-loader scope was added. Intraday/extensibility and multi-participant alignment requirements do not add a dependency to this passive daily finite-source slice.
- Canonical promotion is a separate gate; this report records only a validated candidate awaiting exact-tip independent code review and serialized admission integration.
