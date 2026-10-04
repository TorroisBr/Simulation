# P14-B Technical Design — Finite Source Availability v1

**Checkpoint:** P14-B — Finite Source Availability v1
**Canonical architecture/planning base:** `f6924e63d8e5731da1d33021d0361e7defe6dad7`
**Branch:** `codex/architecture/p14b-technical-design`
**Status:** proposed bounded technical design; independent review and implementation authorization are outstanding. This document changes no architecture, runtime code, or P14-A contract.

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

## 3. Transaction boundary and algorithm

Introduce one domain operation, conceptually `TryProduceFiniteDaily(sourceId, settlementId, marketStoreId, itemId, day, expectedSourceRevision, expectedMarketRevision)`. The coordinator supplies resolved identities and the existing logical daily boundary; callers cannot independently decrement reserve and call `AddStock` for this behavior.

The operation validates the complete pre-state before mutation:

1. Resolve exactly one source and its exact title owner, item, destination store, current content revision and expected revisions. Reject missing, duplicate, mismatched or stale identity/revision inputs.
2. Validate reserve bounds and positive configured rate. Calculate `q = min(DailyOutputLimit, RemainingReserve)`, without arithmetic overflow. If `q == 0`, return deterministic `Exhausted` with zero produced and no revisions or state changed.
3. Validate `MarketRuntime.CanAddStock(item, q)` and that both owner revisions can advance. Market integer overflow, source revision exhaustion, market revision exhaustion or mutation guard rejection rejects the operation before any mutation. No saturation, wraparound, partial output or reserve-only debit.
4. Prepare the exact replacement source state (`RemainingReserve - q`, source revision + 1) and exact Market stock replacement (`stock + q`, Market revision + 1) in private values. Preserve unrelated source/Market rows and price policy. Validate the prepared pair against expected revisions and the same source/store/item identities.
5. Under the existing single-threaded simulation mutation boundary (or a narrowly scoped stable lock if the code path is concurrently callable), install both prepared replacements as one commit. There must be no observer/callback between the two installs. If either install can still fail, use prevalidated no-throw swaps or a transaction token that restores both exact prior snapshots; report failure only after both owners are restored. Do not depend on catching arbitrary exceptions after partially visible mutation as the normal rollback protocol.
6. Return `Applied(q, reserveBefore, reserveAfter, stockBefore, stockAfter)` only after both revisions advance and both values agree. Rejected attempts leave source reserve/revision and stock/revision identical to pre-state. Price refresh occurs after production and consumption using existing daily order; it cannot cause a successful production result.

The transaction coordinator does not become the owner of either store's truth. Source and Market expose internal prepare/validate/commit seams; each store remains sole authority for its state. Existing `MarketRuntime.AddStock` is not sufficient for the composite operation because it commits stock independently. The implementation should add only the prepared stock seam needed for this transaction and avoid broad changes to `EconomyTransactionService` or money transactions.

Daily balance for this one-item finite profile is:

```text
closing stock = opening stock + applied finite output - actual free consumption
closing reserve = opening reserve - applied finite output
```

Rejected production adds zero and debits zero; consumption then follows the existing stock-limited free sink. The source operation occurs once per enabled day in deterministic profile order. Re-running the same boundary must be prevented by the daily coordinator's existing one-boundary progression; if current orchestration can call a day operation twice, add an owner-scoped processed-boundary/stale revision check so duplicate application cannot occur. Do not add a generic scheduling or idempotency framework.

## 4. Composition, P14-A compatibility and selected P12 profile

Use an explicit finite local profile discriminator/configuration which selects this one finite source owner. P14-A's existing exogenous configuration must continue resolving to its current behavior and must neither allocate nor hydrate finite reserve state. Reject a finite source in the wrong profile; reject missing/duplicate source IDs, negative or inconsistent reserve/rate values, unknown item/store/settlement identities, mismatched anchor, or unsupported source/content revision before publication. Do not silently coerce invalid reserve to zero or fall back to the exogenous source path.

The selected `UnityBootstrap-Daily-v1` P12 save profile does not admit P14-B's new finite source owner/state. P14-B composition must be excluded by construction from that selected profile: finite-profile selection is refused before publishing the world when the selected save profile is `UnityBootstrap-Daily-v1`. Existing P14-A exogenous worlds remain eligible under their existing profile rules. This is an admission boundary, not a claim that P12 persistence is complete or a reason to expand its inventory. If future composition makes a finite P14-B owner selectable inside `UnityBootstrap-Daily-v1`, implementation cannot proceed to promotion until a verified fail-closed admission/inventory hook and negative rejection test are integrated serially with the active P12-B owner. A documentation promise is insufficient.

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

Expected implementation hotspots: a new narrow finite-source store/record and transaction service; authored profile/config data and validation; the local flow projection/result; `CityRuntime` only for profile dispatch/coordination; `SimulationRuntime` only for finite-vs-exogenous profile dispatch; `MarketRuntime` prepared stock commit seam; focused EditMode tests. Keep changes localized because `SimulationRuntime`, `CityRuntime`, Market stock and diagnostics are shared hotspots. Integrate sequentially with any active runtime/P12 composition owner. No `P12` worktree edits from this checkpoint.

## 7. Required validation for later implementation

Focused tests should prove:

- finite source configuration, semantic identity uniqueness, title/custody linkage and anchor validation; reject invalid and duplicate identities without publication;
- output is capped at reserve when reserve is below rate; reserve reaches zero, and later days produce zero;
- one successful transaction changes reserve and stock by exactly the same `q`, with expected source and Market revision changes;
- Market integer overflow, stale source/Market revisions, guard rejection and source/Market revision exhaustion leave both owners bit-for-bit unchanged;
- no reserve debit occurs when stock cannot accept the full output; no partial output, saturation or wraparound;
- daily idempotency/order, same-input determinism, `Economy.Enabled == false`, stock-limited population consumption after production, prices after both material operations, and both balance equations;
- P14-A exogenous profile has unchanged source behavior and creates no finite reserve owner/state;
- `UnityBootstrap-Daily-v1` can continue admitting the existing P14-A profile but rejects selected P14-B finite profile before world publication; include negative integration validation if exclusion is not statically guaranteed by the profile composition path;
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
- `Assets/_Project/Scripts/LocalDailyMaterialFlow.cs` or a finite profile result alongside it
- focused EditMode tests under `Assets/_Project/Tests/EditMode/Editor/` for finite reserve, atomic stock commit, profile compatibility and P12 admission rejection
- checkpoint/Phase 14 State evidence only after implementation review/validation; no change to architecture in the implementation branch.

## 9. Open technical review point

The current Market owner supports aggregate integer rows and revision-checked prepared replacement helpers, while source reserve will be a new owner. Independent review must confirm the actual candidate can commit both owner replacements without any observable intermediate state and can enforce the selected-profile exclusion in the real composition path. If the runtime has a reentrant/concurrent observer at the daily boundary, or `UnityBootstrap-Daily-v1` can compose the finite profile through an indirect path, this design needs a narrow transaction/admission refinement before implementation readiness. Those are technical integration questions; the product semantics in §§1–2 are bounded by the approved direction.
