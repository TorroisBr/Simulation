# P12-E effective-provider and owner source crosswalk

**Audit baseline:** P12 canonical `a2e8b696054089378748354703dd2e0f50245769` (`codex/phase12/canonical`), refreshed 2026-10-09. This is a read-only source crosswalk for the accepted `UnityBootstrap-Daily-v1` profile. It does not add runtime behavior or declare P12-E complete.

## Profile identity and effective choices

`Assets/_Project/Data/Simulations/Simulation-DailyV1.asset` is the SampleScene-selected Daily-v1 profile. Its `enabledModules` serialize `Economy`, `Merchant`, `Crime`, and `GuardCrime`, matching the declared `SimulationModule` enum and `SimulationConfigData.CreateConfigurationOverrides`. The module list is input: the effective configuration and the branches in `TesteSimulacao.RebuildSystems` determine the live provider graph.

The asset has no `authoredP10RuinSite`; its natural-mortality and aggregate-demography fields retain their default false values. `Simulation-GeneralTest.asset` is the separate P10 proving profile. The current profile correction therefore keeps P10 Ruin/LocalTopology outside Daily-v1. P10 facts are consumed dependencies only where the accepted profile says so; this crosswalk does not admit them.

## Effective provider to owner crosswalk

| Effective choice / runtime condition | Instantiated provider or service | Owner boundary and checkpoint | Current export / staging disposition |
|---|---|---|---|
| Economy policy (enabled in the selected profile) | `EconomyTransactionService` is constructed unconditionally; the legacy daily loop runs `SimulateEconomyDay` only when `configuration.Economy.Enabled` is true. The P18 boundary economy adapter is not installed for this profile. | City, market, stock, population and account truth belongs to the single D-owned City/NPC roots. Transaction orchestration is not a second factual owner. | Use the existing D owner package/root adapters and their current validations. E does not emit a duplicate City or account section. |
| Merchant enabled | `MerchantSystem` and `CommercialKnowledgeSharingSystem` are constructed and the merchant action provider is added. | Market facts are D-owned. Merchant plans and per-NPC commercial Knowledge are F-owned `NpcRuntime` state. The two services do not own an additional durable Daily-v1 factual section. | Reconstruct the services from the accepted effective configuration. F still owns export/staging for Knowledge and active commitments. The P18-only receipt caches below are excluded and source-proven unreachable in this profile. |
| Justice infrastructure | Because normal bootstrap has a non-null `simulationConfig`, `JusticeSystem` is constructed. | Wanted-record and sentence facts are E-owned Justice state; status fields and NPC roots remain in their assigned D/NPC sections. | Promoted `P12EJusticeRecordsOwnerSnapshot` captures and privately stages ordered wanted-record and sentence values. Evidence is in `validation/P12EJusticeRecordsOwnerSnapshot/VALIDATION.md`. |
| Crime enabled | `CrimeSystem` is constructed with Justice. It is added as an action provider only when effective `Crime.Enabled` is true. | Crime/Social Appraisal factual stores are E-owned; NPC consequences remain in the single D/F NPC owner. Decision/event/history records are read models, not replacement world truth. | The Crime/Social Appraisal owner snapshot is promoted and tested in `P12ECrimeSocialAppraisalOwnerSnapshot`. Crime/Justice operation invalidation is separately recorded under P12-B; it is not an E snapshot. |
| GuardCrime enabled | `GuardSystem` is added as an action provider only when `GuardCrime.Enabled` and Justice exists. | The provider has behavior/configuration but no separate persistent owner identified by the source audit. Its committed effects belong to the Justice/Crime or NPC owners above. | Rebuild from effective configuration; preserve the owning authorities' exports and exact committed-write semantics. Do not serialize the provider as a world-state owner. |
| Natural mortality / aggregate demography | The legacy `DailyDemographicSystem.Advance(...)` is called from `SimulationRuntime.AdvanceDayAfterClockAdvance` on each successful day. The selected asset leaves natural-mortality and aggregate-demography flags false, so this runner applies neither corresponding transition. The separate P18 `DailyDemographyBoundaryStepProvider` is absent because no intraday profile is passed. | Population remains D-owned. The legacy runner is composed and called; both policies are currently disabled. | No separate E value owner is identified for this profile. If either policy is enabled in a later profile, refresh effective provider/owner inventory and checkpoint disposition before admission. |
| Travel, TravelParty and Expedition core services | `RebuildSystems` constructs these core services independently of optional modules. | Their owner facts are already assigned to D/F by the accepted inventory; they are not E-owned merely because the E composition contains these services. | Reuse the current owner sections and checkpoint disposition; no duplicate export is added here. |

`SimulationRuntime` composes `ActorChoiceStore` by default and P12-E does not own it; it remains in F. The accepted inventory continues to identify the concrete shared `CityRuntime` and `NpcRuntime` capture boundaries: D/E/F values for one concrete owner are split into disjoint sections under one P12-B token and revision vector, merged, and reconstructed once.

## P18-only cache disposition

The normal `TesteSimulacao` runtime constructor call omits `p18dIntradayProfile`; the optional constructor argument defaults to null. `InitializeP18DIntradayProfile` returns immediately for null, before building a P18 timeline or its boundary providers. Therefore the Daily-v1 bootstrap does not register the P18 adapters that call the following writers. These two caches are additional to the already-inventoried exact-zero receipt sections for LocalKnowledge, MerchantTradeState, keyed-sale, and Crime/Justice; this source crosswalk does not replace those census witnesses:

| Composed service field | Initial value | Only supported writer path found | Daily-v1 classification |
|---|---|---|---|
| `MerchantSystem.planUrgencyStepReceipts` and `planUrgencyStepRevision` | Empty ordinal dictionary and revision zero | `MerchantPlanUrgencyDailyBoundaryStepProvider` → `TryPreparePlanUrgencyStep` → prepared receipt commit. | Exact-zero under normal Daily-v1 composition; P18-only continuation evidence, excluded from E export and not a distinct Daily-v1 causal owner. Legacy `AdvanceMerchantPlanUrgency` changes F-owned NPC trade-plan state and does not write this receipt map. |
| `CommercialKnowledgeSharingSystem.retainedPhaseSnapshots` | Empty ordinal dictionary | `P18DCommercialSharingBoundaryProvider` → `TryPreparePhaseSnapshot` / `TryCommitPhaseSnapshot`. | Exact-zero under normal Daily-v1 composition; P18-only continuation evidence, excluded from E export and not a distinct Daily-v1 causal owner. Legacy `ShareAmongPresentMerchants` changes F-owned NPC Knowledge and does not write this receipt map. |

This is a source-level reachability and initialization proof for the selected normal bootstrap, not a new live census row or a general claim about manually composed runtimes. P18 composition, receipts, activities, and intraday state remain outside this profile. A future profile that supplies `P18DIntradayProfile` must refresh its own provider inventory and exact owner dispositions.

## Evidence anchors

- Profile fields: `Assets/_Project/Data/Simulations/Simulation-DailyV1.asset`.
- Effective module-to-policy mapping: `Assets/_Project/Scripts/SimulationConfigData.cs`, `CreateConfigurationOverrides`, `HasModule`, and `SimulationModule`.
- Conditional service/action-provider composition: `Assets/_Project/Scripts/TesteSimulacao.cs`, `RebuildSystems`.
- Normal runtime construction and omitted optional P18 profile: `Assets/_Project/Scripts/TesteSimulacao.cs`, selected `p9.genesis.validate-profile/v1` stage; `Assets/_Project/Scripts/SimulationRuntime.cs`, optional `p18dIntradayProfile = null` and initialization call.
- P18 early return and provider assembly: `Assets/_Project/Scripts/SimulationRuntime.P18D.cs`, `InitializeP18DIntradayProfile` and `BuildP18DDailyBoundaryProviders`.
- Merchant receipt state and prepared commit: `Assets/_Project/Scripts/MerchantSystem.cs`; P18 caller: `Assets/_Project/Scripts/P18DDailyBoundaryStepProviders.cs`.
- Sharing receipt state and prepared commit: `Assets/_Project/Scripts/CommercialKnowledgeSharingSystem.cs`; P18 caller: `Assets/_Project/Scripts/P18DCommercialSharingBoundaryProvider.cs`.
- P12-E snapshot state and retained limits: `docs/PHASE12_STATE.md`; accepted ownership scope: `docs/design/PHASE12_E_TECHNICAL_DESIGN.md`; current inventory: `docs/design/PHASE12_OWNER_COVERAGE_INVENTORY.md`.

## Result and remaining obligations

The crosswalk resolves the read-only question of which normal Daily-v1 configuration branches compose the Merchant, Justice, Crime, and Guard providers, where their retained factual state belongs, and why the two P18 receipt caches are zero and excluded on this profile. It found no additional populated E value owner supported for immediate snapshot implementation; Merchant plans/Knowledge belong to F, while City/market/account/population facts belong to D.

This audit is not a runtime provider manifest and does not prove that every core/configured E owner has a complete exact export plus private staged hydrator. P12-E remains IN PROGRESS until its full included-owner coverage and integrated validation obligations pass. P12-F remains WAIT_DEPENDENCY on E; P12-G waits on B–F and validated live-profile inventory; P12-A remains WAIT_DEPENDENCY pending every included export/staged hydrator, live inventory validation, and its separate implementation authorization. P13 remains BLOCKED. No capture eligibility, whole-profile continuation, or Phase 12 closure is claimed.
