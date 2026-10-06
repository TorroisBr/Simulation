# P12-B Property and Estate Owner Mutation Epoch Design

Status: Proposed for independent technical review. No implementation is claimed.

## Authority and base

Checkpoint: P12-B Property/Estate owner mutation invalidation slice, within the accepted profile-admission and completed-boundary prerequisite capability scope.

Design base: P12 canonical 82735cb0ac7878fda0efd7d9e6a3029fe8a501f7, after the reviewed Institution/Office promotion. The promoted runtime code is 13a4ff503d336d34ed008f27571cce82c019dfe4, tree 85ad013074b727ffe8727c2d90b079a45e0ca5c0.

Scope derives from accepted P12-B capability authorization and the current Phase 12 Brief. Reuse the reviewed P12-E owner definitions in PHASE12_P12E_PROPERTY_CENSUS_DESIGN.md and PHASE12_P12E_ESTATE_CENSUS_DESIGN.md. The current canonical architecture baseline referenced by P12 State is e16796014d348e3b59da7ed848101c4c03926ba5.

This design covers only existing selected UnityBootstrap-Daily-v1 runtime owner commits. It adds no property, transfer, succession, inheritance, death, estate, or gameplay semantics. It does not change P12-A authorization or P12 checkpoint dependencies.

## Owner sections and temporal identity

| Required section | Exact owner | Cardinality | Local revision |
|---|---|---|---|
| p12e.property.ownership | installed Runtime.PropertyOwnershipStore | PropertyOwnershipStore.Count | PropertyOwnershipStore.Revision |
| p12e.property.transfer-history | the same installed Runtime.PropertyOwnershipStore | TransferHistory.Count | the same PropertyOwnershipStore.Revision |
| p12e.estate.records | installed Runtime.EstateStore | EstateStore.Count | EstateStore.Revision |

All three sections are Required in the selected Daily-v1 protocol. The existing authored profile begins with zero rows in these owners; that is an admission observation, not a permanent empty-state promise. The two property sections must report the same exact owner identity and revision because they are separate semantic views over one store. An increment to that shared revision changes both property witnesses, even when one section's cardinality does not change.

Runtime composition clones initial rows into the resolved installed stores before the P12 baseline is captured. Those clone writes establish initial state; they are not live post-admission operations. The P12 census providers must bind the resolved runtime stores, not the source stores used for composition.

## Existing selected-runtime commit paths

The source audit at this base finds these selected runtime façade paths:

| Existing path | Successful owner commit | Changed sections |
|---|---|---|
| SimulationRuntime.TryRegisterPropertyOwnership | PropertyOwnershipStore.TryRegister adds one ownership row and advances the store revision once | both property sections, because both expose that revision |
| SimulationRuntime.TryApplyPropertyTransfer, reached by TryTransferProperty | PropertyTransferSystem.TryApplyTransfer atomically replaces current ownership and appends one transfer-history row; PropertyOwnershipStore advances its revision once | both property sections |
| SimulationRuntime.TryApplyEstateSuccession | EstateSuccessionSystem validates the current candidate set, then calls PropertyTransferSystem.TryApplyTransfer directly; it changes ownership/history but does not change EstateStore | both property sections |
| SimulationRuntime.TryApplyEstateOpening, reached by TryOpenEstate | EstateOpeningSystem.TryApplyOpening inserts the EstateId row and deceased-Person lookup entry together; EstateStore advances its revision once | p12e.estate.records |

The convenience APIs delegate to the listed apply method and must not create a nested second operation or duplicate notification. Proposal/query methods do not mutate and do not enter a P12 operation.

The P12 behavior applies to the selected Daily-v1 SimulationRuntime façade methods. The current production call graph reaches the property/estate static systems through those methods, including the nested PropertyTransferSystem call made by EstateSuccessionSystem; other direct invocations found here are isolated domain tests or clone construction. Direct external calls to the exposed store/system APIs are outside this selected runtime façade contract. This boundary follows the current normal game flow and adds no security-oriented detection. Runtime clone population is before the sealed census baseline. If a normal production caller is found outside the façade, classify its owner/commit semantics before extending this bounded slice.

## Admission and operation scopes

Register these three providers and fixed Required contracts in InitializeNpcRosterCensusProtocol only for the existing runtime-admission context, following the Institution/Office and other selected-profile registrations. Reuse PropertyOwnershipCensusProvider.CreateProviders(propertyOwnershipStore) and EstateCensusProvider(estateStore). Verify each provider reports the exact installed object, schema version, starting cardinality, and starting local revision. The protocol's exact section count becomes 242 after the current 239-section baseline.

Use two bounded operation IDs:

- p12.property.owner-commit for property registration, property transfer, and estate succession's property transfer.
- p12.estate.owner-commit for explicit estate opening.

Each apply method performs its pure transition/input checks, then, when Daily-v1 runtime admission is active, checks the bound owner thread, validates all affected section baselines, validates shared mutation-epoch capacity, and enters the corresponding registered operation before invoking the existing domain commit. Preserve existing non-P12 behavior when no runtime-admission context is installed.

After a successful property commit, preserve existing PoliticalWorldRevision behavior where that runtime path already advances it, then notify both property sections together through the existing shared mutation protocol. Estate succession currently delegates to the property transfer system and has no additional EstateStore write; notify only the two property sections. After a successful estate opening, preserve its existing world-revision behavior and notify only p12e.estate.records. One successful store revision change produces one shared-epoch advance per operation.

Domain rejection leaves owner cardinality, owner revision, and shared epoch unchanged. P12 admission refusal occurs before the owner mutator and maps to the existing runtime-faulted failure code for that API. EstateSuccessionFailureCode currently has no such value, so append RuntimeFaulted = 17 after PropertyTransferFailed = 16; preserve the existing numeric values 0-16 and do not misreport a P12 admission refusal as a failed property transfer. If post-commit notification fails, fault-close P12 admission while preserving the already committed operation's success result, as in the reviewed Institution/Office adapter. Dispose the scope on every result path; the registered operation count must return to zero.

## Validation contract

Focused tests must prove:

1. The selected P9-B-only Daily-v1 profile admits the exact three sections, exact installed owners, schema-v1, and expected initial zero cardinality/revision. The profile inventory rises from 239 to 242. GeneralTest/P10-A remains its separate proving profile.
2. Property registration advances ownership cardinality and both property revisions once, leaves transfer-history cardinality unchanged, and advances the shared epoch once. Duplicate registration and domain rejection leave all witnesses unchanged.
3. A successful transfer advances the same PropertyOwnershipStore revision once, changes ownership and transfer-history cardinality/facts as defined by the existing transfer, advances the shared epoch once, and keeps both property witness revisions equal.
4. Successful estate succession uses the existing candidate and property-transfer semantics, changes the two property witnesses once, leaves the Estate witness unchanged, and advances the shared epoch once. Stale/invalid succession leaves all witnesses unchanged.
5. Explicit Estate opening advances only the EstateStore row count/revision and shared epoch once. Duplicate or invalid opening leaves its witness and epoch unchanged.
6. Selected-profile owner-thread rejection happens before store mutation. Estate succession reports the appended EstateSuccessionFailureCode.RuntimeFaulted value (17) without changing either property or Estate owner. Successful and rejected operations all leave the registered operation count at zero and pass the existing registered-operation quiescence assessment.

Run the focused property/estate/runtime admission suites, ALL EditMode, official Smoke, and git diff --check on the final code tree. Retain XML and raw-log hashes. A code change after these runs requires the affected validation to be rerun and exact-tip review to target the final code tree.

## Limits

This is one bounded P12-B owner/operation/epoch slice. It does not establish complete owner coverage, complete shared-epoch coverage, global owner-thread or quiescence proof, capture eligibility, export, hydration, P12-A readiness, P13 readiness, P12-B completion, or Phase 12 closure. Property transfer and Estate semantics remain owned by existing domain systems. P12-F Expedition work remains deferred to its documented P12-C/D/E dependencies.