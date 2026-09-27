# Phase 14 Technical Design — Local Daily Material Flow v1

**Status:** bounded technical proposal only. It is not an approved Phase 14 checkpoint contract, implementation authorization, Phase State, or canonical architecture change. No P14 checkpoint IDs exist in the observed canonical state. This proposal uses the entry boundary at `8b6a3455b00bc25a0be8a2d5c50ec254de0ba7f5` in `codex/phase14/MaterialFlowEntryDecision` and Phase 8 canonical State `c5b2e06b534f4b2af38f10e6510b10800aa8b28c`, which incorporates architecture baseline `c285466c355103d3637ac165246591b72eb7bda0`.
**Prior independent technical-design review:** PASS at content commit
`4a84b76a608a24fd74de032bfb274e0c59f9d3a0`. Targeted revalidation against
the current P8 City-anchor composition found that its owner lookup key is
currently checked against `CityRuntime.RuntimeId`; it is not a durable City
instance identity. This revision uses a separate authored `SettlementSemanticId`
associated with the stable P8 `LocationId`. Independent re-review of content
commit `3d9ed793ae7fdfce74b9224856d40e5ea73ef2a0` passed against the historical
canonical baseline `c5b2e06b534f4b2af38f10e6510b10800aa8b28c` and architecture update
`c285466c355103d3637ac165246591b72eb7bda0`, confirming the exact LocationId
association, uniqueness/cardinality rules, and conditional P18/P20 edges.
**P8-E promotion impact revalidation:** the latest canonical Phase 8 State is
at `77f3e1a`; P8-E was promoted at `d95b60d174cb0b17df09e2775b3cbd134c74b21f`.
The reviewed P14 single-settlement source/sink scope includes no transport,
route flow, or multi-actor work, so P8-E is now canonical but is not a
dependency for this design. The reviewed architecture baseline remains
`c285466c355103d3637ac165246591b72eb7bda0`, with both alignment records current.

## 1. Scope and governing contracts

Design one manually authored City, one configured daily source for an item, and the same City's population consumption of that item, when effective `Economy.Enabled` is true. The configured production is an explicit exogenous source: there are no inputs, finite reserves, depletion, or transformation. The boundary is the closing aggregate market stock after that day's consumption.

The semantic rules are:

- Keep City/settlement title over the configured source and resulting material distinct from the market's custody of aggregate stock. Existing market-counterparty identity and purchasing power are not title records.
- Use stable semantic source, settlement/City, market-store, item, and effective-content identities. Each selected City instance carries an authored `SettlementSemanticId`, unique across City instances in the supported world/profile and distinct from `CityData.DefinitionId`, `CityRuntime.RuntimeId`, and the P8 anchor-owner lookup key. Missing or duplicate settlement IDs reject composition. The selected profile associates that identity with the current stable P8 `LocationId`; runtime IDs and object references are in-process lookup handles only. Runtime IDs, Unity object references, display names, list positions, and diagnostic event IDs are not durable identity.
- The domain source and market-stock authority own their mutations. A schedule/cadence invokes them; it does not define material truth. Existing daily automatic production and consumption need no P18 capability.
- Preserve deterministic order and atomic domain transitions. Overflow cannot partially apply a source contribution. Insufficient stock caps the free population sink at actual available stock as the existing behavior intends; the stock decrease and reported consumed quantity are one atomic transition.
- Preserve current moddability constraints: domain behavior remains independent of Unity presentation where practical, seams permit compatible extension, and independent contributions have stable identity and deterministic composition. P19 API/loader and module lifecycle remain deferred.
- No P20 prerequisite applies to this single-settlement passive source/sink.
  P8-D and P8-E are canonical, but neither route/travel capability is used by
  this design; transport, route flow, and multi-actor work remain excluded.

The design follows architecture §§2, 11–13, 41–45, 69, 91–92; the current Roadmap and Execution Model; Phase 8 State at historical baseline c5b2e06 (including architecture baseline c285466); and both the intraday/extensibility and multi-participant alignment records. P18 is required only if a later approved consumer promises duration-based or intraday production. P20 is conditional only if a later consumer coordinates multiple participants.

## 2. Current implementation and fit

`CityData` currently authors `CityProductionConfig` entries (`ItemData`, `amountPerDay`) and `MarketItemConfig` stock and consumption values. `CityRuntime.SimulateProductionDay()` adds output directly to its `MarketRuntime`; `SimulateConsumptionDay()` requests a population amount and removes up to available stock through the free mode, or uses the account-backed transaction path. `SimulationRuntime` gates the economy pass on `configuration.Economy.Enabled` and runs production for Cities before consumption and price updates. `MarketRuntime` stores aggregate integer balances; `MarketCounterpartyRuntime` supplies market liquidity/transaction identity. `CityProductionResult` and `CityConsumptionResult` are operation results, not durable causal history.

The existing daily order and aggregate stock are sufficient for this bounded profile. The code does not yet represent a durable production-source identity, separate settlement title from market custody, or retain enough causal inputs in a reconstruction projection. `CityRuntime` currently orchestrates domain work and is a shared hotspot; the implementation should avoid expanding it into a general production engine.

## 3. Proposed component ownership and interfaces

### 3.1 Authored source definition

Give each configured source a stable authored `ProductionSourceId` (or equivalent stable semantic key) and an explicit compatible definition/content revision. The source identity is namespaced by the selected City instance's authored `SettlementSemanticId`; it must not use `CityData.DefinitionId`, `CityRuntime.RuntimeId`, the P8 anchor-owner lookup key, or a configuration list index. The one-source proving profile can use one explicit stable source key. If legacy content lacks a source key, a narrowly scoped compatibility resolver may derive an identity only when `SettlementSemanticId` plus item definition uniquely identifies that source within this City's configured production entries. Duplicate entries that cannot be distinguished by that scoped unique key are unsupported for this profile until authored identity is supplied. Do not silently assign order-dependent identities.

The source definition carries item semantic identity and configured amount per enabled economy day. It is exogenous: it does not claim a physical producer, resource reserve, or material transformation. Its resolved effective content/configuration version participates in compatibility and reconstruction.

### 3.2 Authority boundary

Keep the source application as a small domain operation that receives the identified source, settlement/City authority, market stock authority, and logical daily boundary. It proposes/validates one exact addition and returns an applied/rejected result with a stable reason. The authoritative stock mutation remains in the market/domain stock owner; callers cannot update an independent mirror.

Represent the concrete title/custody facts explicitly in the flow contract: `SettlementSemanticId` is an authored, stable per-instance identity carried with the selected City in the P14 profile, unique across City instances in the supported world/profile, and is the title owner; `MarketStoreSemanticId` is custodian; the source refers to its owning settlement and the destination store. The profile also associates `SettlementSemanticId` with that City instance's current P8 `LocationId`, which is the stable spatial anchor, not the settlement's identity. Before publication or mutation, validate that this `LocationId` resolves in the P8 spatial authority and equals the anchor returned for the selected City by `LegacySpatialAnchorBindingStore`; a missing or mismatched binding rejects the profile/operation without mutation. The existing P8 `SpatialAnchorOwnerId` remains an in-process City lookup key in this implementation because `SimulationRuntime` validates its value against `CityRuntime.RuntimeId`; P14 does not persist it as semantic settlement identity or change P8 behavior. Runtime IDs may be used only to resolve the selected City during execution. This is a P14-specific identity relation, not a generic universal asset-holder framework.

The population consumption operation belongs to the existing City population-economy domain boundary and the same market stock authority. For v1 it uses the already-supported free-consumption semantics; it does not mutate money or use the account-backed branch. The simulation coordinator invokes the operations in the existing order and under the effective economy-enabled gate. No autonomous behavior or scheduling engine is added.

## 4. Daily processing and atomicity

At each enabled daily boundary:

1. Capture/refer to the opening balance for the selected `ItemDefinitionId` and the resolved source/configuration identity and revision.
2. Resolve due configured source contributions. Order contributions by stable semantic source ID using ordinal comparison; do not rely on authored list or dictionary iteration order. A single source contribution is accepted only if the complete positive quantity fits the destination aggregate integer balance. Validate the resulting balance before mutation, then apply the full quantity once. On overflow, reject that contribution without changing stock, record a deterministic failure diagnostic, and continue independent contributions in stable order. Never saturate or wrap the balance.
3. After all source contributions, calculate the existing configured daily population request from the authoritative population count and resolved consumption parameters. For the v1 item, determine `actual = min(requested, available stock)`; the stock removal and resulting `actual` are one mutation. If no stock is available, apply zero. No reservation or partial account debit is involved because this profile uses free consumption.
4. Update derived market prices after the production and consumption operations, as in the current daily sequence.
5. Expose the closing stock and operation outcomes as projections. The material balance for successful source contributions is `closing = opening + sum(applied source quantities) - actual free consumption`.

The current `MarketRuntime` stock methods are the aggregate stock authority and mutate directly; the existing runtime mutation-guard binding does not bind `MarketRuntime`. This profile therefore does not assume a market stock guard exists. If the accepted implementation introduces or explicitly binds one, a guard rejection must leave that operation unapplied and report rejection without a success result. Any such binding belongs to the runtime-owned mutation boundary. A later paid-consumption scope would require a separately reviewed atomic stock-and-money contract and is outside this proposal.

## 5. Determinism, causal identity, and reconstruction

For this daily v1, the logical boundary is the effective calendar day plus the deterministic economy-pass order. A day number suffices only for the declared daily profile; it does not imply that a day is the minimum causal boundary for future work. A later intraday consumer must add P18's exact logical instant, same-instant ordering, pending-work and lifecycle facts before claiming intraday behavior.

The authoritative reconstruction inventory for this slice must expose or retain:

- stable semantic City/settlement, production source, market store/custodian, and item-definition identities;
- compatible source/content revision and resolved effective economy/consumption configuration, including the enabled policy and effective calendar identity/version;
- the initial/opening aggregate item stock at the reconstruction boundary;
- daily boundary and deterministic source-application order, each accepted source identity and quantity (or sufficient compatible inputs/state to deterministically reproduce those results), and actual population sink quantity;
- title owner and stock custodian as distinct semantic facts; and
- closing aggregate stock and any rejected contribution reason needed to make diagnostics explain the applied result.

This is an inventory contract for future P12 save and P13 historical reconstruction work, not an event-sourcing mandate, history-retention policy, or save schema. Diagnostics/canonical writers should sort by stable semantic IDs and include the causal inputs/results above where available. A projection must not become an alternative authority or consume randomness.

## 6. Extensibility seam within current constraints

Keep the source definition and its application semantics separate from City presentation/bootstrap and from a single hard-coded manager. The initial built-in source is one deterministic contribution provider. Its contract should permit additional compatible source definitions or behavior contributors later, each with stable identity/version and explicit effective enablement. Merge contributions by stable semantic key and reject duplicate keys deterministically during composition; contributor registration alone does not enable behavior. A disabled Economy capability does not execute sources.

Do not add a general plugin registry, public mod API, loader, package protocol, arbitrary code execution boundary, mod security model, or retroactive world-generation hook here. P19 defines the supported public extension lifecycle after real consumers stabilize it. If later extensions change existing-world state, that must be an explicit new boundary and participate in P12/P13 causal compatibility; installation must not implicitly rerun genesis.

## 7. Integration sequence and affected surfaces

If a bounded implementation checkpoint is later accepted, keep it on an isolated branch from the then-current canonical base. The likely ownership is:

1. Carry a globally unique within-world authored `SettlementSemanticId` and P8 `LocationId` association for the selected City instance in compatible profile input; validate before publication that the Location resolves and equals the selected City instance's current P8 anchor. Add stable authored semantic source identity/version and narrowly validate the selected local source profile. Reject missing, duplicate, unresolved, or mismatched identity/binding inputs rather than deriving them from runtime IDs, content definitions, or ordering.
2. Add the P14 source/title/custody operation at the domain boundary and mutate aggregate stock through the existing `MarketRuntime` stock authority. Do not assume a market mutation guard; compose with a guard only if the accepted implementation introduces or binds one. Keep `CityRuntime` as coordinator, not owner of new universal rules.
3. Preserve the existing daily economy ordering and Economy.Enabled gate while invoking source operations; constrain the v1 consumer to one City and its own market/population.
4. Extend deterministic diagnostics/reconstruction projections and focused tests for identity stability, deterministic ordering, complete-add overflow rejection, stock-limited consumption, title/custody distinction, disabled economy, and repeatable same-input results.
5. Review the full diff against the selected base and run relevant economy, market, population-consumption, and diagnostics suites. Add mutation-guard composition coverage only if the accepted implementation introduces or binds a guard for market stock. Any required broader regression gates follow the accepted checkpoint and repository execution policy.

Likely code surfaces include `CityData`, `CityRuntime`, `MarketRuntime`, `CityProductionResult`, the `SimulationRuntime` economy-pass composition, and authoritative diagnostics/projection components. These are change-risk indicators, not authorization to edit those files. No code or tests are changed by this proposal.

## 8. Gates that remain open

- Phase 14 still has no approved checkpoint IDs, Phase State, implementation contract, or implementation authorization. The bounded entry proposal is a reviewed recommendation, but implementation requires an accepted checkpoint contract with closure and explicit dependencies.
- Independent review of this identity-reconciled proposal passed on content commit `3d9ed793ae7fdfce74b9224856d40e5ea73ef2a0` against the current canonical architecture. It confirmed that the authored settlement-instance identity and explicit Location association reconcile with the runtime-keyed P8 anchor lookup, along with title/custody separation, uniqueness/cardinality, and conditional temporal/shared-activity dependencies. This review does not approve implementation.
- Promotion/integration will require executable implementation, independent code review, targeted validation, and any repository-required integration gates. P8-D/E travel is not a dependency. P18 is not a dependency for the legacy daily profile. P20 is conditional on a later multi-participant consumer. P12/P13 remain the owners of save/reconstruction implementation.

## 9. Explicit exclusions

This design adds no resource extraction, finite source reserves, production inputs, transformation, spoilage, transport, route/network flow, merchant transactions, trade repositioning, money mutation, paid population consumption, multi-worker operation, collective activity, job/crew system, gameplay loop, or production framework. It does not implement Sleep, Dreams, robbery/gangs, rituals, War gameplay, or MegaEventos.

## 10. Promotion impact revalidation — 2026-09-26

This proposal remains bounded to manually authored daily production and
population consumption within one settlement and its own market. It introduces
no transport or route flow and remains a proposal, not an approved checkpoint
contract or implementation authorization. Revalidation uses P8 canonical
State `470667d`, P9-A promotion `988b6f5`, P11 Actor Choice State promotion
`0803670`, P18-A State promotion `0b52898`, and architecture baseline
`c285466c355103d3637ac165246591b72eb7bda0`, including current
intraday/extensibility and multi-participant alignment records.

- **P8:** retain the actual dependency on the promoted spatial authority and
  City-to-Location anchoring. Confirm the selected City's Location resolves
  and matches its current anchor. P8 passage/travel capability remains
  irrelevant because this profile moves no material.
- **P9-A:** upstream-irrelevant to the manually authored v1. Generated content
  may populate sources in a later profile, but is not required here.
- **P11 Actor Choice:** upstream-irrelevant because production and consumption
  remain passive daily Economy behavior, not actor-selected actions or pending
  command inputs.
- **P18-A:** upstream-irrelevant to the existing daily cadence. If a later
  accepted scope promises duration-based, shift-based or intraday operation,
  it must consume the specific promoted P18 scheduler/lifecycle capabilities
  required by that behavior.
- **P20:** conditional only for a later consumer that actually coordinates
  multiple participants, using relevant P20 capability on P18. No shared
  activity or crew is introduced here.

The alignment records constrain future temporal and participant extensions
without changing this design's closure. The open product/entry gate remains
acceptance of the first consumer, conservation boundary, and actor/asset
authority; no new content scope is inferred by this revalidation.
