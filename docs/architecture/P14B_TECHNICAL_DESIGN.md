# P14-B Technical Design — Finite Source Availability v1

**Checkpoint:** P14-B — Finite Source Availability v1
**Canonical architecture/planning base:** `f6924e63d8e5731da1d33021d0361e7defe6dad7`
**Branch:** `codex/architecture/p14b-technical-design`
**Status:** proposed bounded technical design; independent review and implementation authorization are outstanding. This document changes no architecture, runtime code, or P14-A contract. P14-A Market behavior was checked at `origin/codex/phase14/canonical` `4caecbbfb0464c965811402b3c11d8717605114a`; the active P12 composition/admission code was checked at `origin/codex/phase12/canonical` `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e`.

## 1. Goal and boundary

Add one authored finite productive source to a new local daily profile. The source has a stable identity, one item, configured maximum output per enabled day, and a finite initial reserve retained as authoritative source state. For each daily boundary, actual production is `min(configured daily rate, remaining reserve)`. The same operation decreases reserve by that quantity and increases the selected Market's aggregate stock by exactly that quantity. When reserve is zero, output is zero.

The P14-A profile remains intact: its one exogenous source has no reserve and continues to add its configured daily amount. Finite availability is a distinct source behavior and a distinct selected profile, not a reinterpretation or hidden field on the P14-A source. Both profiles use the same material identity, stock authority, title/custody distinctions, daily coordinator and balance projection seams. The finite proof has exactly one authored City/settlement, one source, one item, one Market stock row and the existing same-City free population sink. Preserve the `Economy.Enabled` gate and P14-A order (source operation, free consumption, price refresh).

P14-B does not add multiple sources, multiple items, multiple Cities, source-to-source conversion, transport, trade, price decisions, labor, actor-selected production, or autonomous trade AI. It does not make City a universal economic agent. P14-C may later compose multiple identified source behaviors; P14-D transfer and route dependencies remain separately scoped.

## 2. Authority, identities, title and custody

- `FiniteProductionSourceStore` is the sole owner of source instance identity and mutable reserve. It is a small domain owner, not a generic source framework. Its key is stable `ProductionSourceId`, scoped to stable `SettlementSemanticId`; source definition identity/revision and `ItemDefinitionId` are stable semantic references. No `RuntimeId`, Unity object identity, display name, collection index or diagnostic ID is persisted as identity.
- The one source record retains `InitialReserve`, `RemainingReserve`, configured `DailyOutputLimit`, owning `SettlementSemanticId`, `ProductionSourceId`, item ID, compatible content/source revision, and destination `MarketStoreSemanticId`. Reserve is nonnegative; `0 <= RemainingReserve <= InitialReserve`. The source owner controls reserve mutation. Do not store a duplicate reserve on `CityRuntime`, Market, a diagnostic, or a second projection.
- For this profile the Settlement is the explicit title holder of the source and its material. The source record names that title owner by semantic ID. The Market store is custodian of aggregate stock; it does not acquire title from custody. Any future separate owner, operator, extractor, custodian, or transfer needs its own scope and must not be inferred from this profile. Market counterparty identity and liquidity remain unrelated to title.
- The selected settlement identity remains distinct from `CityData.DefinitionId`, `CityRuntime.RuntimeId` and P8's in-process anchor lookup key. Preserve P14-A's stable `LocationId` association and anchor agreement checks. Source ID must be unique and nonempty within the supported profile. The exact source ID is authored; no index-based fallback or implicit ID generation.
- `MarketRuntime` remains authoritative for aggregate stock and its revision. The proposed material operation is the only caller coordinating finite reserve with that Market stock change. Normal reads are projections; result/event objects are never alternate truth.

## 3. Transaction boundary and exact Market seam

Promoted P14-A `MarketRuntime` has a serialized mutable `List<MarketItemRuntime> Items`, no stock revision, and no prepared replacement API. `AddStock` validates integer capacity, creates or mutates the matching row, and refreshes that row's price immediately. `RemoveStockUpTo` removes the actual available amount and refreshes the row's price immediately. P14-B must add the following narrow Market stock-authority seam; it must not assume these facilities already exist.

- Make public `Items` a read-only view so callers cannot bypass stock authority by editing the list. Keep the serialized backing list private; constructors and `MarketRuntime` internal helpers remain responsible for adding rows. Make `MarketItemRuntime.AddAmount` and `RemoveAmount` non-public and keep amount assignment internal to Market's commit seam; the current live row returned by `GetItem` must not provide a public stock mutation path. Repository callers that only enumerate, index or inspect `Count` continue to work; any mutation callsites must be moved behind `MarketRuntime` methods.
- Add a Market stock mutation revision seam. On the P14-A `4caecbb` baseline, add private `long stockRevision` and an internal read-only accessor; successful `AddStock`, `RemoveStockUpTo`, and finite stock commit each advance it once, rejected/no-op operations do not, and `long.MaxValue` is checked before mutation. On an implementation base that already has P12's broader `revision` (as at `a6572ab`), use that existing owner revision for stale-preparation checks and do not add a duplicate counter; the finite prepared install advances it once. Existing purchases/sales route quantity changes through `RemoveStockUpTo`/`AddStock`. A price-only refresh is not a stock change on the P14-A baseline; where the reused P12 revision also tracks a price-only change, that only makes a prepared operation conservatively stale.
- Add an internal finite-only prepare/validate/commit seam using the actual backing list. Prepare captures the expected owner revision, finds exactly one matching item row, and creates a private cloned `PreparedMarketState`: clone unrelated rows in authored order; replace the matching row with a new row holding `currentAmount + q` and its existing desired amount; preserve the matching item identity and recompute price using the same `MarketItemRuntime` constructor/`UpdatePrice` formula as P14-A. The selected profile requires an authored Market item row exactly once, so absent or duplicate rows reject at profile validation. No live list, row, amount or price changes during preparation. P14-A needs an internal row copy constructor and a prepared-state type if those are not already present on the exact implementation base.
- Commit accepts the prepared replacement only if its captured owner revision is current, capacity/revision remain valid, and Market mutation admission succeeds. It atomically installs the completed backing list and its read-only view with one owner method, then advances the Market owner revision once and issues any applicable owner-mutation notification. The live list is replaced as a whole; consumers must reacquire `Items`/rows after a committed operation and must not retain mutable list/row handles. Keep raw amount mutators non-public. Do not call `AddStock` as part of the composite transaction because that would expose a stock-only intermediate commit and immediately mutate the live row.

Introduce one domain operation, conceptually `TryProduceFiniteDaily(sourceId, settlementId, marketStoreId, itemId, day, expectedSourceRevision, expectedMarketStockRevision)`. The daily coordinator supplies resolved identities and the existing logical boundary; no caller independently decrements reserve and adds stock for this source.

Before commit, resolve exactly one source and its title owner/item/destination store, validate source content revision and expected owner revisions, validate reserve bounds and positive configured rate, and calculate `q = min(DailyOutputLimit, RemainingReserve)`. If `q == 0`, return deterministic `Exhausted`, zero output, and no revision changes. Otherwise prepare the source replacement (`RemainingReserve - q`, source revision + 1) and Market stock addition (`stock + q`, Market stock revision + 1). Any invalid/stale identity, `int` overflow, exhausted revision, duplicate matching Market row or mutation admission rejection returns failure before either owner changes. No saturation, wraparound, partial output or reserve-only debit.

Commit runs synchronously in the existing `SimulationRuntime` daily call stack. It revalidates both prepared states, then performs only prevalidated assignments: install source reserve/revision and swap Market's prepared backing list/read-only view/revision. There are no callbacks, events, logging hooks or observer calls between these owner installs; the operation returns before the coordinator exposes results or runs consumption. On this single-threaded boundary no supported observer can see the intermediate assignments. Do not claim thread-safe atomicity for arbitrary concurrent readers; cross-thread simulation/query support is outside this profile. Preparation allocates/clones before commit, and commit uses no-throw owner installs after revalidation; all expected failures reject before the first write. The method reports `Applied(q, reserveBefore, reserveAfter, stockBefore, stockAfter)` only after both owners installed matching quantities and revisions. `MarketRuntime` remains stock owner, source store remains reserve owner, and the operation coordinates without becoming a third source of truth.

Preserve P14-A price semantics and ordering: the finite commit updates the affected Market row's price immediately after its stock amount changes, as `AddStock` currently does. The existing free sink then removes stock and immediately refreshes price via `RemoveStockUpTo`; `SimulationRuntime`'s normal final `UpdateMarketPrices` remains after source and consumption. Do not defer all price recomputation or change P14-A's daily balance/order.

Daily balance for this one-item finite profile is:

```text
closing stock = opening stock + applied finite output - actual free consumption
closing reserve = opening reserve - applied finite output
```

Rejected production adds zero and debits zero; consumption then follows the existing stock-limited free sink. The source operation occurs once per enabled day in deterministic profile order. Re-running the same boundary must be prevented by the daily coordinator's existing one-boundary progression; if current orchestration can call a day operation twice, add an owner-scoped processed-boundary/stale revision check so duplicate application cannot occur. Do not add a generic scheduling or idempotency framework.

## 4. Composition, P14-A compatibility and selected P12 profile

Use an explicit finite local profile discriminator/configuration which selects this one finite source owner. P14-A's existing exogenous configuration must continue resolving to its current behavior and must neither allocate nor hydrate finite reserve state. Reject a finite source in the wrong profile; reject missing/duplicate source IDs, negative or inconsistent reserve/rate values, unknown item/store/settlement identities, mismatched anchor, or unsupported source/content revision before publication. Do not silently coerce invalid reserve to zero or fall back to the exogenous source path.

The concrete bootstrap boundary is `TesteSimulacao.InitializeSimulation`: at stage `p9.genesis.resolve-profile/v1`, it first validates `SimulationConfigData` and starts constructing the private draft; it later calls `ValidateCandidateProfile()` in `p9.genesis.validate-profile/v1` and publishes at `p9.genesis.publish/v1`. The selected P12 profile is carried by `runtimeAdmissionContext.Profile == UnityBootstrapDailyV1`; it is distinct from the P14 material profile. Add `ValidateP14SourceAdmission(simulationConfig, runtimeAdmissionContext)` at the start of the existing `p9.genesis.resolve-profile/v1` stage, before `SimulationGenesisPipeline.ValidateProfile(simulationConfig)` and before any source/store/runtime owner is created. It scans selected authored City production configs and rejects any source explicitly marked finite when the P12 daily admission context is present. The selected P14-A exogenous source remains allowed and continues through existing profile validation. The earlier check ensures the unsupported owner never enters even the private world draft.

The admission test belongs in `SimulationRuntimeAdmissionTests` (or the owning bootstrap profile fixture): select `UnityBootstrapDailyV1`, author one finite source, invoke normal staged bootstrap, assert `p9.genesis.resolve-profile/v1` fails before source/store/SimulationRuntime construction, `Bootstrap`/published composition remains null, the bootstrap is latched failed, and no day can advance. A paired test selects the same P12 profile with the existing exogenous P14-A source and confirms it remains admitted. Test the `None` non-P12 profile separately to show the finite profile remains usable outside the selected P12 composition. This is a verified fail-closed hook integrated at the actual selected-profile boundary, not a promise that finite data is absent from all P12 configurations.

P12 canonical code already has `SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1`, `SimulationRuntimeAdmissionContext`, staged bootstrap validation/publication, and market stock census/operation hooks, but does not provide exact finite-source owner export/hydration or automatically infer the new owner. The P14 finite profile therefore must be explicitly rejected by the admission test above until a later P12 profile/version registers and exactly supports this owner. Any future admission inside `UnityBootstrap-Daily-v1` is a serial P12-B integration and promotion blocker until its market/source inventory and operation coverage are updated and negatively validated. This does not claim P12 persistence is complete.

P14-B defines the owner seams needed by §92A without implementing P12 DTOs, storage, capture, hydration or P13 history. Any later save profile that admits the finite source must include exact source identity, reserve, compatible definition/content revision, title-owner/source/store/item links and revision or an equivalent exact semantic export, and must privately hydrate/validate all links before publication. Unsupported versions/profile compositions reject. Cache indexes are derived and rebuildable.

## 5. Determinism and reconstruction inventory

Retain or deterministically reproduce:

- `SettlementSemanticId`, `LocationId`, `ProductionSourceId`, `MarketStoreSemanticId`, `ItemDefinitionId` and their relationships;
- source definition/content revision, configured daily limit, `InitialReserve`, current `RemainingReserve`, title owner and Market custodian;
- effective `Economy.Enabled` and relevant source/profile configuration, calendar identity/version and absolute day boundary;
- opening stock/reserve, deterministic operation order, accepted produced quantity, actual free consumption, closing stock/reserve, and stable rejection reason where rejection explains the result;
- compatible code/content/numeric profile necessary to reproduce integer bounds and operation results.

IDs and results are ordered by stable semantic identity in diagnostics. Queries and diagnostics do not mutate, consume randomness or become causal history. P14-B uses no random input. No History/event-sourcing mandate is introduced; later P12/P13 work owns actual persistence/reconstruction and may use exact state plus retained causal inputs consistent with its profile.

## 6. Ordering and hotspots

`SimulationRuntime` retains the existing `Economy.Enabled` gate and daily boundary. For the finite profile, per City: finite production transaction, existing free population consumption against actual available stock, then price update. The coordinator only dispatches to domain owners. It does not perform source state arithmetic or write Market rows itself. P14-A's exogenous branch retains its existing order/behavior.

Expected implementation hotspots: a new narrow finite-source store/record and transaction service; authored profile/config data and validation; the local flow projection/result; `CityRuntime` only for profile dispatch/coordination; `SimulationRuntime` only for finite-vs-exogenous profile dispatch; `MarketRuntime` read-only Items view, non-public row amount mutators, stock revision and prepared existing-row commit seam; `TesteSimulacao` at `p9.genesis.resolve-profile/v1` for the pre-construction P12 profile check; focused EditMode tests. Keep changes localized because `SimulationRuntime`, bootstrap publication, `CityRuntime`, Market stock and diagnostics are shared hotspots. Integrate sequentially with any active runtime/P12 composition owner. No P12 worktree edits from this checkpoint.

## 7. Required validation for later implementation

Focused tests should prove:

- finite source configuration, semantic identity uniqueness, title/custody linkage and anchor validation; reject invalid and duplicate identities without publication;
- output is capped at reserve when reserve is below rate; reserve reaches zero, and later days produce zero;
- one successful transaction changes reserve and stock by exactly the same `q`, with expected source and Market revision changes;
- Market integer overflow, stale source/Market stock revisions, guard rejection and source/Market revision exhaustion leave both owners unchanged; unauthorized `Items` mutation is unavailable through the public API;
- no reserve debit occurs when stock cannot accept the full output; no partial output, saturation or wraparound;
- daily idempotency/order, same-input determinism, `Economy.Enabled == false`, stock-limited population consumption after production, prices after both material operations, and both balance equations;
- P14-A exogenous profile has unchanged source behavior and creates no finite reserve owner/state;
- through normal `TesteSimulacao` staged bootstrap, `UnityBootstrap-Daily-v1` admits the existing P14-A exogenous source and rejects a selected finite source at `p9.genesis.resolve-profile/v1` before any world owner is constructed; finite source remains available under the separate non-P12 profile;
- exact state export/hydration seam round-trips semantic values privately in owner-level tests if the interface is implemented, without claiming P12 persistence capability.

The implementation wave must additionally run the repository-required regressions, official Smoke and `git diff --check`, then receive independent exact-tip code review. P14-B promotion is a separate human gate; this technical design does not authorize it.

## 8. Dependencies, exclusions and planned file ownership

**Dependencies:** promoted P14-A source ID, settlement/title, market-stock custody, local daily order and free consumption; promoted P8-A stable `LocationId` and P8-B/C City-anchor composition as used by P14-A. No P8-D/E routes, P18 time/activities, P20 shared participation, P9 generation or P10 topology dependency. P12 is not a prerequisite to domain design/implementation, but the selected P12 profile exclusion/rejection rule is a promotion gate. P15/P16 may consume only after relevant P14 capability promotion.

**Excluded:** changing P14-A meaning/content, universal source abstraction, multi-source/multi-item/multi-City composition, transfer, extraction workers, production duration, money, paid consumption, trade, autonomous merchants/guild/City decisions, prices/opportunity logic, logistics, random production, generic scheduler, public mod loader/API, and P12/P13 persistence/history implementation.

Expected files are intentionally a candidate inventory, not permission to edit canonical/shared files:

- `Assets/_Project/Scripts/FiniteProductionSourceStore.cs` (+ Unity `.meta` if required by repository convention)
- a narrowly scoped finite-source daily transaction/result file, e.g. `FiniteSourceProductionService.cs` and `FiniteSourceProductionResult.cs`
- `Assets/_Project/Scripts/Data/CityData.cs` or the established authoring type for finite source profile data
- `Assets/_Project/Scripts/CityRuntime.cs`, `MarketRuntime.cs`, `SimulationRuntime.cs` for narrow composition/commit seams
- `Assets/_Project/Scripts/TesteSimulacao.cs` for finite-source rejection at P12's selected pre-construction profile resolution boundary
- `Assets/_Project/Scripts/LocalDailyMaterialFlow.cs` or a finite profile result alongside it
- focused EditMode tests under `Assets/_Project/Tests/EditMode/Editor/` for finite reserve, atomic stock commit, profile compatibility and P12 admission rejection
- checkpoint/Phase 14 State evidence only after implementation review/validation; no change to architecture in the implementation branch.

## 9. Review boundary

The design matches the promoted P14-A Market implementation at `4caecbb`: that version has no stock revision or prepared stock API, exposes mutable `Items` and public row amount mutators, and refreshes price within each successful add/remove. The P12 canonical code at `a6572ab` has since added a general `revision`, prepared snapshot/install helpers, mutation admission and owner notification hooks; P14-B must inspect its exact implementation base and reuse those compatible seams rather than duplicate them or bypass P12's hooks. P12's Market stock census is not finite-source state admission. The design names the actual P12 bootstrap `p9.genesis.resolve-profile/v1` stage and positive/negative admission cases. Independent review must validate this technical boundary against the exact current branch tip before marking it `READY_FOR_IMPLEMENTATION`; this document does not self-approve or authorize implementation.
