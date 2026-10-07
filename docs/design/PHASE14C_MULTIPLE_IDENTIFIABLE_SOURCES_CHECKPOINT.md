# P14-C — Multiple Identifiable Sources v1: Technical Design

**Status:** `DESIGN_CANDIDATE`; independent technical review pending. This is a bounded technical design, not an implementation, canonical promotion, Phase closure, or P12 readiness claim.

## Exact planning baseline

- P14 canonical before this design: `06e9c30101a74bd618d3651885c489c79fe866bb`.
- Current code/composition base: P12 canonical `405f70e58a7a1dd8be255798b795faff095f44b4`, a descendant of P14 canonical and the current implementation base used for design inspection. Its advance from `c7c8bbf` is documentation-only; the P12 owner-matrix code tree and retained validation remain unchanged.
- Current architecture: `codex/architecture/world-identity-projection` at `e16796014d348e3b59da7ed848101c4c03926ba5`.
- Promoted product-direction document blob: `45fb75b99731e47fec69aca7f6e34d5433d34a52` (`P14-C — Multiple Identifiable Sources v1`). It names P14-A plus promoted P14-B as the dependency for the recommended mixed-source proof.
- Current Roadmap blob: `39ad0144677cd8f344111b762f0c77ab8c7423e8`; Execution Model blob: `fcd49f34ec8b2f0321cc444bf7b3e612ffd963b2`.
- Intraday/extensibility alignment blob: `a231a2a014bf58be5ce382c48a55f3654df89a61`; multi-participant alignment blob: `4ed6fcc60b348461e3201d4f3d480c21a3154ec3`.

The architecture sequence marks P14-C `WAIT_DEPENDENCY` on P14-B for this mixed proof. P14-B is now promoted in P14 State at `06e9c30`; that dependency is satisfied. The P14 Brief did not yet name P14-C, so this candidate records the architecture-ordered checkpoint there without changing its semantics.

## 1. Bounded proof

Use one authored P14 City, one stable P8 Location binding, one Market store, one item, and exactly two distinct daily source identities:

1. one exogenous source using the already delivered P14-A meaning; and
2. one finite-reserve source using the already delivered P14-B meaning.

Both source rows target the same item. Keep settlement title distinct from Market custody, and retain the existing free same-City population sink after the production step. For each day, expose the opening balance, ordered per-source outcomes, actual free consumption, and one closing balance:

`closing = opening + sum(applied source quantities) - actual free consumption`.

This is a separate P14 proving profile. P14-A and P14-B retain their existing single-source profiles and behavior. The accepted P12 `UnityBootstrap-Daily-v1` profile remains unchanged and continues to admit only its selected P9-B geography composition; it rejects any authored P14-A, P14-B, or P14-C material-flow configuration before world identity or owner construction. P14-A and P14-B retain their existing standalone proving-profile behavior, and P14-C is a separate standalone proving profile. The P14 City and P10 Ruin remain separate; do not create a second Location or combine their current bootstrap profiles.

## 2. Identity, cardinality, and authoring contract

Reuse `SettlementSemanticId`, authored `LocationId`, `MarketStoreSemanticId`, `ItemDefinitionId`, `ProductionSourceId`, and source content revision. The durable source identity is the pair `(SettlementSemanticId, ProductionSourceId)`; runtime IDs and list indices remain lookup/order artifacts only.

Add an explicit per-source kind to the existing source configuration so a mixed profile does not infer source semantics from a zero reserve, array position, or current profile name. Use an `Unspecified` enum default so existing serialized P14-A/P14-B configuration keeps its current profile-determined meaning; the mixed P14-C profile requires explicit `ExogenousDaily` or `FiniteReserveDaily` on both rows. P14-C admits exactly two rows, exactly one of each kind, with non-empty distinct source IDs, non-empty content revisions, positive daily output limits, and the same item definition. The exogenous row carries no reserve (`initialReserve == 0`), preserving P14-A meaning. The finite row carries a non-negative initial reserve; the proving fixture uses a positive reserve so both contributors produce. The profile keeps exactly one matching Market item row, one City, and free population consumption. Existing P14-A/P14-B profile admission retains its current cardinality and meaning.

Reject null/malformed rows, duplicate source identities under the existing ordinal semantic-ID comparison, multiple finite or exogenous rows, mismatched item identity, unsupported source kind, or mismatched settlement/Location/Market bindings before constructing `CityRuntime`, Market, finite-source owners, or world identity. Daily-v1 must reject `MixedSourcesDaily` before identity or owner construction, consistent with its existing rejection of authored P14-A and P14-B; P14-A and P14-B standalone proving-profile behavior remains intact. This is not a claim that P14-C state is saveable.

## 3. Deterministic source order and stock capacity

Sort the two contributors by `ProductionSourceId` with `StringComparer.Ordinal`, independent of authored collection order. This is the stable source order used by the profile fingerprint, output diagnostics, and production receipt.

For the exogenous row, the planned quantity is its configured positive daily output. For the finite row, the planned quantity is `min(daily output limit, current remaining reserve)`; an exhausted source plans zero and retains the existing P14-B no-owner-mutation behavior. The finite reserve is debited only by the finite source quantity actually applied; an exhausted or overflow-rejected finite row leaves its owner unchanged.

Prepare both source outcomes against one Market snapshot. Walk in stable source order. A contributor applies only its whole planned quantity when that quantity fits the current prepared aggregate `int` stock; otherwise that contributor is recorded as rejected with `AggregateStockOverflow` and contributes zero. Continue to the next contributor using the unchanged prepared balance after a rejected row. Compute capacity with checked/widened arithmetic; never wrap or partially apply a contributor. This preserves P14-A's all-or-nothing source addition while making the existing architecture requirement for deterministic multi-source order observable at capacity boundaries.

## 4. Shared prepared daily-source operation

Use one P14-owned prepare/commit operation from both existing execution routes: the ordinary daily path through `CityRuntime.SimulateLocalDailyMaterialFlow`, and the P18D `CityEconomyDailyBoundaryStepProvider` path through `CityRuntime.TryPrepareDailyEconomyStep` when that boundary provider is composed. Both paths must use the same ordered source outcomes and source/Market commit algorithm.

Do not call live `Market.AddStock` or debit the finite reserve while planning. Build one prepared Market replacement from the opening snapshot, calculate ordered source results, and prepare the accepted finite reserve transition from its exact owner revision. Recheck the Market revision, finite-source revision/day/configuration, source configuration snapshot, and normal mutation-admission requirements before installing anything. Extend `CaptureDailyEconomyConfiguration` and the P14-C profile fingerprint with an order-independent source tuple that includes each stable source ID, explicit kind, item identity, daily output limit, initial reserve, and content revision; sort the tuple by ordinal source ID so changing kind or source configuration invalidates the prepared P18D step. On stale state, exhausted revision capacity, rejected runtime admission, or inability to prepare the complete accepted set, install nothing and report no applied source quantity.

On successful commit, synchronously install the one Market replacement and accepted finite-source reserve/revision/day transition, then publish the existing owner notifications. Define an immutable per-source outcome containing `ProductionSourceId`, kind, configured quantity, applied quantity, and rejection reason. `LocalDailyMaterialFlowResult` and the P18D receipts expose the same ordinally ordered outcome list. The direct daily result also exposes opening stock, free consumption, and closing stock. In the P18D route, add `BoundaryOccurrenceId` and absolute day to both receipts: the Production receipt exposes opening stock and post-production stock; the Consumption receipt exposes actual free consumption and closing stock. The pair then proves `closing = opening + applied sources − actual free consumption`. These are in-memory runtime execution projections; the design adds fields to the existing step receipts, not a separate owner. They do not admit P14-C to P12 Daily-v1 or imply P12 owner inventory, capture, export, or hydration support. Replaying the P18D operation returns its prior receipts without reapplying stock or reserve; the direct daily path is not a separate replay API.

The existing daily sequence remains production, free consumption, then price refresh under `Economy.Enabled`. P14-C adds no intraday timing semantics, money/account mutation, reservation, transport, or autonomous decision. A failed production install does not authorize partial source/Market writes.
## 5. Owner and profile boundary

The runtime state remains the existing Market stock owner plus one existing `FiniteProductionSourceStore`; the exogenous contribution is source configuration with an explicit semantic identity, not a new mutable reserve owner. The operation receipt uses the existing CityRuntime daily-step execution boundary only; it is not a new owner or P12 continuation capability. The P12 Daily profile admits only its selected P9-B geography composition and rejects authored P14-A, P14-B, and P14-C material-flow profiles before identity or owner construction. Preserve the existing standalone P14-A/B proving profiles. The implementation must add/retain a pre-identity admission assertion for P14-C and verify no City, Market, or finite-source owner is constructed on that rejection. Keep P14-C outside the selected P12 owner inventory; the CityRuntime execution receipt does not change that boundary.

The design does not claim that P14-C is continuation-ready under P12, or add any P14 state to the P12 census. It also does not add a source catalog, source plug-in API, loader, event bus, general economy framework, multiple-item aggregation, multiple-city aggregation, a second finite source, a second exogenous source, or generic contributor registration. P19 loader/API work remains deferred.

## 6. Implementation ownership and integration

Likely owned surfaces are `CityData` source-kind/profile input; `CityRuntime` profile validation and shared daily source preparation/installation; the existing finite-source owner, adjusted to accept the mixed profile while representing only the finite-kind row; `CityEconomyDailyBoundaryStepProvider` integration; `LocalDailyMaterialFlowResult` and `CityDailyEconomyReceipt` source-result projection; and the P14-owned early profile admission boundary in `TesteSimulacao`. Reuse Market's current prepared snapshot/revision/notification boundary. Do not create a second stock authority or alter P8 City/Location ownership.

P14's current canonical branch predates the latest P12 integration. Implementation must start from the refreshed P12 canonical code base that already descends from P14 `06e9c30`, preserving P14-B while incorporating current `SimulationRuntime`, admission, and P12 operation/admission behavior. Classify actual changed-file drift before editing; shared runtime/bootstrap edits remain serial. P14-C's current code should be promoted to P14 canonical only through the normal exact-tip review and validated additive fast-forward.

## 7. Validation plan

Focused coverage must prove:

- exactly one City/item/Market row and exactly two source rows, with one explicit kind each;
- missing, duplicate-under-ordinal-identity, extra, malformed, or mismatched sources fail before owner/world identity construction;
- swapping authored row order yields byte-equivalent ordered results, fingerprints, stock, and finite reserve;
- the exogenous quantity and finite reserve-capped quantity apply once in stable source-ID order;
- exhausted finite source is an exact zero with no finite-owner mutation;
- a near-`int.MaxValue` Market demonstrates deterministic whole-contributor overflow rejection, with no partial contributor mutation and correct finite reserve behavior;
- stale Market/source/config revisions and mutation-admission failures leave both owners unchanged and do not publish a success receipt;
- replay through the P18D boundary does not duplicate either output or reserve debit; changing a source tuple under the same occurrence is rejected; the direct daily route adds no replay API
- direct daily and P18D routes produce equivalent ordered source outcomes, Market stock, finite reserve, free consumption, and closing balance; the P18D Production/Consumption receipts carry the same occurrence/day key and satisfy the balance equation;
- free population consumption follows the ordered production results and the closing balance matches the equation;
- P14-A and P14-B single-source behavior remains unchanged;
- Daily-v1 continues its selected P9-B-only composition and rejects P14-A, P14-B, and P14-C before identity and owner construction; standalone P14-A/B behavior remains intact, and P10/P14 combined bootstrap rejection remains intact.

Run focused P14 and profile-admission suites, affected bootstrap/runtime regressions, ALL EditMode, official Smoke, and `git diff --check` on the final integrated code tree. Retain XML/log hashes. Do not infer P12-B completion, P12-A readiness, P13 readiness, full continuation coverage, or Phase 14 closure.

## 8. Review and readiness boundary

This design is bounded to the promoted P14-C architecture direction and chooses a deterministic, stable-ID source order and per-contributor whole-output admission into one prepared daily production commit. Independent technical review must check these rules against the current P14-A/B contracts, the exact P14/P12 code base, §92A, the Daily-v1 negative admission rule, and both alignment records. If that review finds an unresolved product or canonical architecture choice, stop only P14-C at that specific decision while independent roadmap work continues.
