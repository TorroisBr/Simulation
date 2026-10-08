# P12-D NPC owner field/writer crosswalk — current-base revalidation

**Status:** Current-base design/evidence revalidation candidate; requires independent exact-tip document review. No NPC export/staging code is implemented by this record.

**P12 canonical reviewed:** `ed3aad0bcf98fc1b709b6bc632452689448b8803`  
**Architecture authority:** `47eff220c7ce00f6e7c759bdc2b76780bb46f628`  
**Repository:** `TorroisBr/Simulation`

## Decision

The accepted D/F assignment remains technically coherent at this base, and the former receipt-witness blocker is resolved. I found no remaining semantic or evidence blocker to the **bounded NPC D/F value snapshot and private staging slice** after an independent exact-tip review passes this revalidation. The disposition requested from that review is `READY_FOR_IMPLEMENTATION` for that owner slice only.

This is not implementation authorization by itself, does not make full City/NPC integration ready, and does not claim P12-D completion. Runtime/bootstrap publication and whole-D graph validation remain separate work.

## Exact evidence and deltas from the prior crosswalk

The prior current-source crosswalk is `docs/design/PHASE12_D_CITY_NPC_CURRENT_BASE_FIELD_CROSSWALK.md`, reviewed as source evidence in `PHASE12_D_CITY_NPC_CURRENT_BASE_FIELD_CROSSWALK_REVIEW_3B1C850.md`. It was based on P12 `ef0cafb5848cadcf0ac91a3e1af7ff3faaab1367` and explicitly withheld implementation readiness because the two embedded P18 receipt owners lacked exact-zero census evidence.

The P12 State at this review base records these exact intervening capabilities:

- City/NPC relation-order assembly candidate `483edb791f523d797e9b52e88eda00fc34eaa5ff`; implementation code commit `5aceb2b7ce49ffe009489627731cd6689fe2d200`; reviewed Assets tree `e6c0776052dbfdd8a83ce51eb15fd15cf9d89b2c`; exact-tip implementation review `28a4f060c84ad568bb9f3d21faabb6581af2d0bb` PASS.
- NPC receipt-owner witness code commit `4947ec926b3a48427e9c07de5d722d45014cf1bf`; code/test tree `dd0cda707693237a56f5664b2eabc8aa6821b100`; Assets tree `b239758a62c9c670f7400af2567515308e138a4f`; exact-tip review `85acfead11d84a8940fe6da87ace6fab243ffc56` PASS.
- Current State validation for the assembly tree: focused City assembly 17/17, receipt-owner suite 13/13, all EditMode 2592/2592, official Smoke 5/5, and cumulative `git diff --check`, recorded in `docs/validation/P12DCityNpcAssembly/VALIDATION-FOLLOWUP-CAPTURE-ENVELOPE-5ACEB2B.md`.

I compared the prior crosswalk base with this P12 base (29 commits ahead) and checked the current source. Relevant `NpcRuntime.cs` additions are the non-lazy `ExistingLocalKnowledgeObservationRuntime` and `ExistingMerchantTradeStateRuntime` accessors and `TryCreateForStagedPresence`. That factory creates an NPC with exact direct current City/Location references for unpublished assembly; it does not capture or restore the NPC's other D/F fields. The City snapshot and assembler live in `Assets/_Project/Scripts/P12DCityRootOwnerSnapshot.cs`; its independent review confirms order/revision preservation, exact City/D/F evidence pairing, and pre-fill reciprocal-relation validation. It is not an NPC value snapshot.

The receipt census is in `Assets/_Project/Scripts/P12DNpcReceiptOwnerCensusProviders.cs`; the owner-local exact-zero reads are in `P18DLocalKnowledgeObservation.cs` and `P18DMerchantTradeStateOwner.cs`. The provider creates exactly two distinct witnessed rows per installed NPC, ordered by ordinal RuntimeId and family: `p12d.npc-local-observation-receipts/{RuntimeId}` and `p12d.npc-merchant-trade-state-receipts/{RuntimeId}`, schema 1, each cardinality 0 and local revision 0. Every read confirms the exact child owner object is still installed and remains 0/0. Missing, aliased, replaced, malformed, or populated owners fail closed. These are **excluded-empty evidence rows**, never D/F payload rows; no P18 receipt export/replay is admitted.

## Revalidated D/F field and witness map

This matrix preserves the accepted ownership in the prior crosswalk and the P12-D technical design. Owner-section IDs below are census evidence for capture; they are not export DTOs or permission to duplicate the value into another section.

| NPC facts | Sole projection | Current source evidence / capture rule |
|---|---|---|
| Runtime identity and definition/content ID | D | `NpcRuntime.RuntimeId` and admitted `NpcData.DefinitionId`; one non-null, unique roster NPC per stable RuntimeId, with definition resolved from the compatible profile content. Do not serialize Unity definition objects or allocate replacement IDs. |
| Person binding and residence | D references Person-owned values | `PersonStore` is authoritative for the reciprocal PersonId↔NPC materialization relation and bound Person residence. The promoted `PersonStoreOwnerSnapshot` in `Person/PersonStore.cs` covers Person rows, binding, and owner revisions; it does not capture the rest of NPC state. For an unbound NPC, residence remains on NPC under `p12b.npc-residence/{RuntimeId}`, singleton cardinality and `ResidenceRevision`; for a bound Person, use `p12b.person-life-residence/{length}:{PersonId}`, singleton and Person life/residence revision. The merged stage must validate both directions and settlement existence. |
| Life, injury, status, hidden-day value | D | Life state uses `p12b.npc-life-state/{RuntimeId}`, singleton and `LifeStateRevision`; status plus `hiddenDaysRemaining` use `p12b.npc-status-crime-state/{RuntimeId}`, singleton and `P12CrimeJusticeRevision`. Status list length is part of that fact, not witness cardinality. `HideForDays`, `AdvanceHiddenDay`, and `ClearHidden` increment only on effective supported change; lifecycle writers commit life revision under the P12 boundary. Injury has no independent revision row. Current selected-profile binding makes `TryApplyInjury` fail before changing the field; capture the admitted value from the exact NPC owner and fail closed if a selected-profile writer is introduced without reviewed witness coverage. |
| Current action definition reference | D | `p12b.npc-current-action/{RuntimeId}`, schema 1, cardinality 0/1, `CurrentActionRevision`; validate the definition ID against the same NPC's F action payload, if present. `SetCurrentAction`, slot installation, and successful death clearing are the relevant writers. |
| Mutable action runtime payload | F | Same NPC current-action slot witness/revision, cardinality 0/1. Preserve payload exactly; require action-definition identity equality; do not rerun the action or serialize it as a second D-owned payload. |
| Current/residence/destination City and legacy Location references | D | Current presence is cross-witnessed by `p12b.city.important-npcs/{CityRuntimeId}` (exact ordered members/cardinality and `ImportantNpcRevision`) and `p12f.npc-travel-state/{RuntimeId}` (singleton and `TravelStateRevision`). City and NPC must resolve to the same staged Location. Destination City, when present, must resolve to its declared Location. `SetCurrentPresence`, successful arrival/cancel, and travel transitions are existing supported writers. |
| Travel progress, route/party and decision correlation | F payload; D supplies referenced roots | `p12f.npc-travel-state/{RuntimeId}`, schema 1, singleton and `TravelStateRevision`. Preserve nullable typed route/party references and progress exactly; preserve decision correlation as opaque correlation. Do not schedule or replay travel on restore. |
| Inventory item rows | D root/value | Exact installed `InventoryRuntime`, `p12f.inventory/{RuntimeId}`, schema 1, row cardinality from existing backing storage, and `InventoryRuntime.Revision`. Census uses `ExistingInventory`, rejects absent storage without materializing it, and sorts provider rows by NPC RuntimeId. Preserve all item identity, amount, cost and order semantics from the owner; require exact definition resolution and reject owner replacement/alias or unsupported mutation path. |
| NPC money account | D root/value | Exact distinct installed `MoneyAccountRuntime`, `p12e.npc-money-account/{RuntimeId}`, schema 1, cardinality 1 and `MoneyAccountRuntime.Revision`. Provider binds the exact account object and rejects aliasing across NPCs. Preserve balance/revision; do not duplicate into E merely because the census section has a P12-E prefix. |
| Merchant and travel plan payloads | F | `p12b.merchant-trade-plan/{RuntimeId}` and `p12b.npc-travel-plan/{RuntimeId}`, schema 1, one row each, each bound to its exact embedded owner and local revision. Preserve current plan/commitment; do not replan or charge during staging. |
| Admitted commercial, spatial, exploration-site and adventure-intel Knowledge | F | Existing Knowledge census providers bind current child owner identity, exact list cardinalities and owner-local revision: commercial has three views sharing one revision; spatial two; adventure intel four; exploration-site observations one. Use non-lazy existing-owner accessors where absence matters. F references D roots by typed IDs. |
| LocalTopology Knowledge | F conditional, excluded for Daily-v1 | Its witness is Knowledge only, not a composed LocalTopology world owner. Daily-v1 rejects populated P10-only values and consumes P12-E's typed LocalTopology `NOT_COMPOSED` status. Do not construct P10 state. |
| P18 LocalObservation and MerchantTradeState receipt ledgers | **Excluded; exact-empty witnesses only** | Exactly the two P12-D census rows per installed NPC above, each exact owner identity/cardinality/revision = 0/0. No field goes into a D/E/F snapshot. Any non-zero, missing, changed, aliased or malformed owner rejects admission/capture. |

### Snapshot reuse check

No promoted E/F snapshot covers an NPC row in this matrix. Current executable snapshot inventory includes P12-E `PersistentBattleOwnerSnapshot`, which covers Battle records only, and D owner snapshots for City, Person, Genealogy, legacy SpatialNetwork, and ExplorableSite. They do not export `NpcRuntime`, inventory, NPC MoneyAccount, action payload, plans, or per-NPC Knowledge. Existing P12-B census providers for those owners are passive evidence, not detached export/factory APIs. Reuse D's Person/City snapshots for those concrete owners; keep NPC capture/staging as one new, disjoint D/F adapter with references to those roots. No second City/NPC adapter may be added by E or F.

## Required implementation evidence and rejection semantics

A bounded implementation must add focused tests proving:

1. Detached immutable repeat capture for empty and populated NPCs; source mutation after capture cannot alter exported values. All projections for one NPC share the exact completed-boundary token, capture stamp, NPC owner identity and relevant component vector. A mismatched token/stamp/vector, duplicate/aliased owner, stale token, or overlapping D/F field ownership rejects before staging.
2. Exact round trips for D roots and revisions, including both zero/positive hidden-day counts, current and nullable destination presence, Person-bound and NPC-only residence, and the D action definition with optional matching F action runtime. Later daily inputs must observe the same hidden-day behavior; hydration consumes no day and executes no action.
3. Exact F payload preservation for active travel, merchant/travel plans, party/route references, correlation IDs and admitted Knowledge owner values/revisions. Hydration must not replan, charge, execute, or rewrite observations. Typed references must resolve against staged D City/legacy Location roots; opaque decision correlation is preserved, not treated as a foreign key.
4. Exact D inventory and NPC account owner capture: item row values/order/revision and account balance/revision; reject absent/ambiguous definitions, malformed amounts/balances, account aliasing, wrong owner identity, unsupported writer, impossible revisions, or owner replacement.
5. Cross-owner rejection for Person↔NPC materialization and residence reciprocity, current City↔NPC ordered membership, City/NPC Location agreement, destination City/Location consistency, incompatible definitions, and any dangling typed root. The rejected package remains unpublished and the active runtime is unchanged.
6. Both P18 receipt owners remain exact-zero through every read. Tests must reject a post-token receipt write, populated owner at initial capture, missing/replaced owner, or revision/cardinality mismatch. They must not export or replay receipt payloads.
7. Daily-v1 excludes populated P10-only LocalTopology and populated legacy site rows; preserve the exact-empty site witness and typed `NOT_COMPOSED` LocalTopology semantics.

These are owner-slice tests and rejection proofs. Whole-D merged graph validation must additionally check Person residences, Genealogy endpoints, SpatialNetwork IDs/routes, site witness, and all D/F links before D hands its unpublished package to G. Continuation parity, single runtime/bootstrap publication, and profile-wide rejection gates remain G/integration obligations, not claims of this slice.

## Decision boundary

After independent exact-tip review passes this revalidation against P12 `ed3aad0bcf98fc1b709b6bc632452689448b8803`, the bounded NPC D/F export and private-stage slice is ready for implementation within the existing accepted field boundary. The source/census evidence supports that decision: the prior crosswalk's only explicit readiness blocker was the two receipt-owner witnesses, now supplied, and no promoted E/F snapshot substitutes for the missing NPC adapter. The City/NPC assembler already provides the relation-link seam that implementation must use.

That readiness does not include City/NPC whole-profile runtime/bootstrap integration, merged-D graph validation, P12-E or F completion, P12-G, P12-A authorization, P13, P18 export/replay, or Phase closure. P12-D remains IN PROGRESS.
