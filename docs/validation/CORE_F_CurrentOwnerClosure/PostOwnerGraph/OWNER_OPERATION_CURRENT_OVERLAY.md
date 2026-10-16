# CORE-F post-demographic owner/operation overlay

**Status:** current-source reconciliation overlay; additive to and not a replacement for the promoted 89-family owner and capture-target matrices or the 24-operation map.

**Core canonical:** `b2c2c0e94c7a8dd9175a8db3fab4fb9d78dcfb2a`.
**Implementation:** `9c1ed5e2065f2d509b61e63b39fb6c84508fd5fe`, tree `f9934338811f7272c8f7013f5bbda14e06c4a92e`.
**Source graph and mechanical path/blob join:** [`README.md`](README.md), [`owner-source-identity-join.csv`](owner-source-identity-join.csv), and the graph CSVs beside them.

## Purpose and boundary

The promoted owner matrices remain valid historical contract and P12 evidence, but three rows directly depend on the demographic owners now extracted to shared Core:

- `p12d.city-population.aggregate/`
- `p12d.city-population.operation-receipts/`
- `p12d.genealogy.parentage`

Neither historical matrix names the current Core owner or bridge source paths. The operation map also cites an older `SimulationRuntime.cs` blob (`a1880b40…`); current is `4344293459f48eff44a433bc6438b34a21196981`. Its population census anchor was `1c2b87e2…`; current `P12PopulationLifecycleCensus.cs` is `1a714ce53d68e447a85fd74d8a43bc86217b1ed4`. The existing genealogy provider and codec source contents are unchanged, but their source-base labels are still historical. This overlay binds current source identity and operation-to-family references without rewriting those records.

The receipt row is a directly affected family because it belongs to the extracted `SettlementPopulationRuntime` and is registered/captured per City. It is **not** an admitted writer edge in the current accepted P12 Daily-v1 profile: the population lifecycle builders omit its exact section ID, the owner callback requires that ID for a receipt-changing commit, keyed Person-death receipt operations are rejected under runtime admission, and the accepted profile test verifies Natural Mortality and Aggregate Demography writers are disabled. P18-D's separate `DailyDemographyBoundaryStepProvider` must not be folded into P12 `runtime.advance-day` by name alone. The bounded current-profile classification is therefore **registered census row with no admitted successful receipt writer in the reviewed P12 operation set (exact-zero under the retained exclusion/config guard)**. The generic receipt API tests prove owner semantics, not a P12 runtime writer path.

The source/declaration graph proves file identity and declaration reach only. The test names below identify retained proof already included in the promoted ALL EditMode evidence; this document does not claim that those tests were rerun or that the operation map exhausts every possible writer.

## Three affected owner rows

| Family | Shared authoritative owner and current identity | Current runtime instance/binding path | Existing P12 capture/restore path and retained case evidence |
|---|---|---|---|
| `p12d.city-population.aggregate/` | `SettlementPopulationRuntime` plus aggregate contracts/system/transitions in `Packages/com.simulation.core/Runtime/Population/`; exact blobs are in [`core-build-inputs.csv`](core-build-inputs.csv). | Each `CityRuntime` creates its `Population` owner; `CityRuntime.TryBindRuntimeMutationGuard` binds that same owner through `PopulationOwnerBridge` to the existing runtime guard. The simulation binds all City authorities before publication. | `SettlementPopulationCensusProvider` reads the exact City owner; P12-A codec and P12-D City-root capture/stage use `PopulationOwnerBridge`; the Daily-v1 owner package and P12-G coordinator retain the selected restore route. Retained cases include `P12PopulationLifecycleInvalidationTests.SelectedDailyProfileTracksImmigrationEmigrationAndResidentDeathOnceEach`, `PairedResidenceMigrationRequiresRuntimeBoundaryAndAdvancesOneEpoch`, and City-root capture/stage round-trip and rejection cases.
| `p12d.city-population.operation-receipts/` | Same `SettlementPopulationRuntime` instance; receipt contents/cardinality and receipt revision are accessed only through `PopulationOwnerBridge`. | Same City construction and same-guard binding as aggregate population. P12 lifecycle binding installs admission/committed callbacks, but the receipt callback requires the exact receipt section in the active operation's expected-section set. The current population-operation builders omit it; the accepted Daily-v1 config disables Natural Mortality and Aggregate Demography, and keyed Person-death receipts are rejected under runtime admission. Thus no successful receipt writer is admitted in this P12 operation set. | `P12DCityRootOwnerSnapshot` exports and stages receipt state with the City owner. Retained cases include `P12DCityRootOwnerSnapshotTests.CaptureAndStage_PreservePopulationReceiptsAfterRollbackPruning`, `P12PopulationLifecycleInvalidationTests.PopulationProvidersUseExactCityOwnersAndRejectEnabledDailyWriters`, and generic owner semantics in `SettlementPopulationCensusTests.ReceiptCensusTracksNewInstallReplayConflictAndStaleRejection`. These establish census/capture, the accepted writer exclusion, and owner receipt semantics respectively; they do not prove a P12 receipt writer.
| `p12d.genealogy.parentage` | Shared `GenealogyStore` and contracts in `Packages/com.simulation.core/Runtime/Genealogy/`; exact blobs are in [`core-build-inputs.csv`](core-build-inputs.csv). | `SimulationRuntime` retains one `genealogyStore` instance, validates/installs that instance, and registers `GenealogyMutationGuardBinding` for it against the existing mutation guard. `PersonGenealogySystem` and P12 adapters access that same store through the runtime boundary; `P12AGenealogyOwnerCodec` directly consumes `ParentageRecord`. | P12-D owner-package capture/staging and P12-G detached target validation carry the captured genealogy revision. Retained cases include `GenealogyCensusTests.SelectedDailyCensusRegistersGateOneOwnersAndTracksParentageOperation`, wrong-thread and saturated-epoch rejection, add/remove/same-cardinality revision changes, plus `P12DNpcRootOwnerSnapshotTests.DailyV1Package_StagesDormantMaterializedPeopleAndGenealogyTogether` and the restored-clone revision regression.

## Current source identity anchors

The complete exact file hashes are in `core-build-inputs.csv`, `source-inventory.csv`, and `owner-source-identity-join.csv`. Relevant current adapter identities include:

| Source | Current Git blob |
|---|---|
| `Assets/_Project/Scripts/CityRuntime.cs` | `888691d92f66ae84ebbd42a71312c4ed5043cbd2` |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `4344293459f48eff44a433bc6438b34a21196981` |
| `Assets/_Project/Scripts/P12PopulationLifecycleCensus.cs` | `1a714ce53d68e447a85fd74d8a43bc86217b1ed4` |
| `Assets/_Project/Scripts/Population/SettlementPopulationCensusProvider.cs` | `4fc8fc273aa2943aaededde61e95d36213b655c9` |
| `Assets/_Project/Scripts/Genealogy/GenealogyMutationGuardBinding.cs` | `d2d71dc5afd38fe68417818494dda90e71e4f8f5` |
| `Assets/_Project/Scripts/Person/PersonGenealogySystem.cs` | `3e78b80c3d5c5ad5e4fca004094fb50d84c30cfe` |
| `Assets/_Project/Scripts/P12ACityOwnerCodec.cs` | `8cb4285169ad63ca3fc3b28182d8d48fe53c5874` |
| `Assets/_Project/Scripts/P12DCityRootOwnerSnapshot.cs` | `5e18dead96e30dbf03a0a1b3d029c4a478399307` |
| `Assets/_Project/Scripts/P12DDailyV1OwnerPackage.cs` | `48ee45d2b87161f499133b143c2c6f79cafddb30` |
| `Assets/_Project/Scripts/P12EDailyV1OwnerPackage.cs` | `72385189d6f472ee18887ea49bdacf2b3a59612c` |
| `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.cs` | `90c5d08ba838f0a1b8dd1d0f56f9d55315b7361d` |

The direct Core bridge identities are `PopulationOwnerBridge.cs` `085521f7cb1e8242acd232eec65c80c8eeee3282`, `PopulationRollbackPoint.cs` `1bda286863cd308460336a258b078ce0096231b0`, `GenealogyOwnerBridge.cs` `8a2cb332e9f97808ad5bf40d6b3aa0169f3d401b`, `IAuthoritativeMutationStatus.cs` `eabd3dc94a1e405b69971ad8a278cffbb1733aa5`, and `MutationStatusBinding.cs` `90bb8002068248d48f1f978333e99f16b7c7f34b9`.

## Operation-to-family map at the promoted source tip

These links are grounded in the current operation adapters, their registered section IDs, and the existing operation-case/test evidence. Dynamic `/id` rows stay per-owner. Branch-dependent rows are conditional. `runtime.advance-day` is a dispatcher and is not assigned a made-up direct owner delta.

| Operation | Current affected family keys / rule | Primary current source and retained case source |
|---|---|---|
| `runtime.npc-membership` | `p12c.runtime-identities.npcs`; `p12d.person.membership` and `p12d.person.materialization-binding` when PersonStore changes; `p12b.city.important-npcs/{city}` on City-presence change. Reconciles the dynamic per-NPC provider roster; it does not mutate every owner family. | `SimulationRuntime.cs` register/unregister and roster reconciliation; `SimulationBootstrapCompositionTests.SelectedDailyV1PersonMaterializationReconcilesDynamicOwnerCardinality` and adjacent binding case. |
| `runtime.person.parentage` | `p12d.genealogy.parentage` only. The supported runtime facade commits to the shared GenealogyStore and its exact revision. | `SimulationRuntime.cs`, `PersonGenealogySystem.cs`; `GenealogyCensusTests.SelectedDailyCensusRegistersGateOneOwnersAndTracksParentageOperation`. |
| `runtime.bootstrap-publication` | No post-baseline domain owner section; this is a publication-scope operation. | `SimulationRuntimeAdmissionTests`. |
| `runtime.advance-day` | Only owner sections changed by successful nested writers; there is no direct day-wide owner delta. | `SimulationRuntimeAdmissionTests`; per-writer operation cases remain authoritative. |
| `runtime.travel.start` | `p12e.npc-money-account/{npc}`, `p12f.npc-travel-state/{npc}`, `p12b.city.important-npcs/{city}`, `p12b.npc-travel-plan/{npc}`, `p12f.spatial-knowledge.locations/{npc}`, `p12f.spatial-knowledge.routes/{npc}`, `p12c.runtime-id-allocator.events`, and `p12c.simulation-record-sequence`. | Current `SimulationRuntime.cs`; `P12SoloTravelStartOperationTests`. |
| `runtime.travel-party.advance` | `p12f.travel-parties`, per-NPC `p12f.npc-travel-state/{npc}`, `p12f.spatial-knowledge.locations/{npc}`, `p12f.spatial-knowledge.routes/{npc}`, impacted `p12b.city.important-npcs/{city}`, and `p12c.simulation-record-sequence`. | Current `SimulationRuntime.cs`; `P12TravelPartyAdvanceTests`. |
| `runtime.economy.npc-trade` | `p12e.npc-money-account/{buyerNpcId}`, `p12e.npc-money-account/{sellerNpcId}`, `p12f.inventory/{buyerNpcId}`, and `p12f.inventory/{sellerNpcId}`. | `EconomyTransactionService` and runtime adapter; `SimulationRuntimeAdmissionTests`. |
| `runtime.economy.money-transfer` | `p12e.npc-money-account/{sourceNpcId}` and `p12e.npc-money-account/{destinationNpcId}`. | `EconomyTransactionService` and runtime adapter; `SimulationRuntimeAdmissionTests`. |
| `runtime.economy.market-purchase` | `p12e.npc-money-account/{actorNpcId}`, `p12f.inventory/{actorNpcId}`, and the matching item-row mutation within `p12e.city-market-stock-rows/{length-prefixed CityRuntimeId}`. | `EconomyTransactionService` and runtime adapter; `SimulationRuntimeAdmissionTests`. |
| `runtime.economy.market-sale` | `p12e.npc-money-account/{actorNpcId}`, `p12f.inventory/{actorNpcId}`, and the matching item-row mutation within `p12e.city-market-stock-rows/{length-prefixed CityRuntimeId}`. | `EconomyTransactionService` and runtime adapter; `SimulationRuntimeAdmissionTests`. |
| `runtime.merchant.advance-npc-trade-state` | Per-NPC `p12b.merchant-trade-plan/{npc}`, `p12b.npc-travel-plan/{npc}`, `p12f.spatial-knowledge.locations/{npc}`, `p12f.spatial-knowledge.routes/{npc}`, `p12f.commercial-knowledge.markets/{npc}`, `p12f.commercial-knowledge.liquidity/{npc}`, and `p12f.commercial-knowledge.share-receipts/{npc}`. | Current runtime/merchant sources; `SimulationRuntimeAdmissionTests`. |
| `p12.institution-office.owner-commit` | `p12e.institution.records`, `p12e.office.records`, `p12e.office.incumbencies`, and `p12e.office.tenures`. | Current owner commit adapter; `InstitutionOfficeCensusTests`. |
| `p12.faction.owner-commit` | `p12e.faction.records` and `p12e.faction.affiliations`. | Current owner commit adapter; `P12FactionStoreCensusTests`. |
| `p12.political-claim.owner-commit` | `p12e.political-claim.records` and `p12e.political-claim.recognitions`. | Current owner commit adapter; `P12PoliticalClaimCensusTests`. |
| `p12.political-support.owner-commit` | `p12e.political-support.relations`. | Current owner commit adapter; `P12PoliticalSupportCensusTests`. |
| `p12.property.owner-commit` | `p12e.property.ownership` and `p12e.property.transfer-history`. | Current owner commit adapter; `PropertyEstateMutationEpochTests`. |
| `p12.estate.owner-commit` | `p12e.estate.records`. | Current owner commit adapter; `EstateCensusTests` and `PropertyEstateMutationEpochTests`. |
| `runtime.population.immigration` | `p12d.city-population.aggregate/{length-prefixed CityRuntimeId}` plus `p12b.person-life-residence/{PersonId}` for a bound Person, otherwise `p12b.npc-residence/{NpcId}`. | `P12PopulationLifecycleCensus`; `P12PopulationLifecycleInvalidationTests`. |
| `runtime.population.emigration` | Same exact `p12d.city-population.aggregate/{length-prefixed CityRuntimeId}` and bound-Person-versus-unbound-NPC residence branch as immigration. | `P12PopulationLifecycleCensus`; `P12PopulationLifecycleInvalidationTests`. |
| `runtime.population.resident-death` | `p12d.city-population.aggregate/{length-prefixed CityRuntimeId}` plus branch-specific Person/NPC life and residence; NPC life-state/current-action are affected only when their admitted owner is present and changed. | `P12PopulationLifecycleCensus`; `P12PopulationLifecycleInvalidationTests`. |
| `runtime.population.residence-migration` | `p12d.city-population.aggregate/{length-prefixed OriginCityRuntimeId}` and `p12d.city-population.aggregate/{length-prefixed DestinationCityRuntimeId}`, plus the exact bound-Person or unbound-NPC residence owner. | `P12PopulationLifecycleCensus`; `P12PopulationLifecycleInvalidationTests.PairedResidenceMigrationRequiresRuntimeBoundaryAndAdvancesOneEpoch`. |
| `runtime.person.death` | `p12b.person-life-residence/{PersonId}`; if materialized, NPC life-state and optional current-action; `p12d.city-population.aggregate/{length-prefixed CityRuntimeId}` when resident. | Current `SimulationRuntime`/lifecycle adapter; `P12PopulationLifecycleInvalidationTests`. |
| `runtime.person.residence-bind` | `p12b.person-life-residence/{PersonId}`; membership is a separate `runtime.npc-membership` owner change. | Current `SimulationRuntime`/lifecycle adapter; `P12PopulationLifecycleInvalidationTests.PersonResidenceBindingAndReceiptFreeDeathUseCompleteOneEpochBoundaries`. |
| `runtime.travel-party.start` | `p12f.travel-parties`; `p12c.runtime-id-allocator.travel-parties` and `p12c.runtime-id-allocator.events`; `p12c.simulation-record-sequence`; and for each participant `p12e.npc-money-account/{npc}`, `p12f.npc-travel-state/{npc}`, `p12b.city.important-npcs/{city}`, `p12f.spatial-knowledge.locations/{npc}`, and `p12f.spatial-knowledge.routes/{npc}`. | `P12TravelPartyStartOperation` and current runtime adapter; `P12TravelPartyStartOperationTests`. |

The current operation source identities and all cited case-source identities can be recomputed from [`owner-source-identity-join.csv`](owner-source-identity-join.csv). The prior evidence map's expected blobs are historical; the CSV records current-tip blobs without changing historical evidence.

## What this resolves and what remains blocked

This overlay resolves the current file-identity joins and supplies an exact family-key link for all operation rows where the current source constructs those registered section IDs. No concrete missing invalidation edge was found among the 24 documented supported operation cases. The receipt family is explicitly exact-zero within the reviewed P12 Daily-v1 writer set, subject to the named admission/configuration exclusions; it is not linked to aggregate lifecycle operations or inferred from P18-D's daily boundary consumer. This is narrower than complete writer coverage: the 24-case map does not prove every ingress, complete 89-family member/alias closure, all successful mutation paths, global owner-thread quiescence, or a completed-boundary token. `runtime.advance-day` remains nested-writer fan-in, and dynamic membership reconciliation is not proof that every per-owner family has a complete writer census.

The full `CORE-F-Shared-DailyV1-Application-and-Host` package therefore remains `WAIT_DEPENDENCY`. The remaining entry gate is the complete 89-family, member-level source/access join across exact live instances and constructors, every admitted writer plus its revision/epoch effects, source export, decoder/hydrator, cross-owner relations, rejection atomicity, and the documented quiescence/completed-boundary mechanism. Keep shared runtime/bootstrap and persistence integration serialized. Do not add new behavior or broaden the Daily-v1 profile based on this overlay.
