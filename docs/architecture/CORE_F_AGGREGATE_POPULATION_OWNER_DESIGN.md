# CORE-F first owner package: aggregate City population

Status: architecture proposal only. Source baseline is production `0094febf7f97333d0653eaf9688fe1d24d73327f`, with graph evidence in `../validation/CORE_F_ClosurePath/`. This proposal moves one deterministic aggregate-population capability to `Simulation.Core`; it does not close CORE-F or claim a complete Daily-v1 host.

## Package boundary

Move these seven source files, preserving algorithms, comparisons, exception policy, ordering, and strings exactly:

| Source file | LOC | Responsibility |
|---|---:|---|
| `Population/SettlementPopulationRuntime.cs` | 606 | Aggregate count/revision/receipt authority; paired migration; P12 admission callbacks; receipt snapshot staging |
| `Population/SettlementPopulationSystem.cs` | 68 | Proposal/apply facade |
| `Population/SettlementPopulationTransition.cs` | 114 | Revision-bound immutable transition and value equality |
| `Population/PopulationChangeSet.cs` | 59 | Count arithmetic |
| `Population/AggregateDemographySystem.cs` | 411 | Provider invocation, representational floor, proposal and receipt-backed commit |
| `Population/AggregateDemographyContracts.cs` | 30 | Rejection/random contracts |
| `Population/AggregateDemographyTransition.cs` | 91 | Immutable proposal values |
| **Total** | **1,379** | One executable owner capability |

The subset is 7 files versus the 191-file/1,324-internal-edge largest SCC. Six files are singleton SCCs. `AggregateDemographySystem.cs` currently belongs to SCC-191; its sole edge into that SCC is `SpatialStableKey.Encode(parts)` in `SpatialRouteKnowledge.cs`, which directly delegates to Core-owned `StableKeyEncoding.Encode(parts)`. Substituting the existing Core encoder is the proposed cut; preserve exact fingerprint bytes and prove the new graph is detached. Do not extract spatial code.

## Cross-assembly contracts

The proposal is intentionally small and explicit. Existing public domain types remain the owner API; no generic `SetPopulation`, raw receipt dictionary, writer callback injection from arbitrary consumers, guard fault setter, or `InternalsVisibleTo` is introduced.

### Existing guard identity

Add one read-only shared contract in Core:

```csharp
public interface IAuthoritativeMutationStatus
{
    bool CanMutate { get; }
}
```

The existing Unity-owned `AuthoritativeMutationGuard` implements this interface. The world still constructs and owns exactly one guard. `SettlementPopulationRuntime` retains that exact interface reference with the copied `MutationGuardBinding` semantics: repeated binding to the same object succeeds idempotently, and any other object is rejected. Binding is performed through existing Unity composition using the Core `PopulationOwnerBridge.CanBindMutationStatus(owner,status)` and `TryBindMutationStatus(owner,status)` methods, which wrap exact one-way binding checks and retain the same object. `PopulationOwnerBridge.BindP12PopulationMutationBoundary(owner, admission, committed)` keeps the existing exact delegate types, one-time state, and original owner reference. Unity composition still owns the concrete guard and aggregate roots. `CanMutate` is read-only; Core never controls fault state.

Pair migration preserves the baseline predicate exactly: both unbound owners are accepted; if either is bound, both must be bound; bound owners must report mutable and their retained `IAuthoritativeMutationStatus` references must be `ReferenceEquals`. Thus one-bound/one-unbound, two different guards, and any bound faulted owner reject; same original guard passes. The identity check is against the existing guard object, not a status proxy per owner. Genealogy must use this same bound/unbound rule.

### P12 operation boundary

`P12PopulationLifecycleCensus` remains Unity-owned and remains the only creator of P12 callback authority. It currently binds `CanCommitP12PopulationMutation` and `NotifyP12PopulationMutation` at lines 238–240. Preserve that one-time binding, with the original owner object passed to both callbacks: the census callback uses it as a key in `populationAggregateSectionIdsByOwner` / `populationReceiptSectionIdsByOwner` and its exact reference is significant. Do not replace it with an ID-only callback.

Keep callback fields private to `SettlementPopulationRuntime`; expose only `PopulationOwnerBridge.BindP12PopulationMutationBoundary(owner, admission, committed)` for the Unity bootstrap boundary. That Core bridge wraps the existing internal bind, takes the same two strongly typed callbacks, and has no callback replacement or unbind. Exact behavior to carry forward:

- Admission is called before the relevant owner change; `true`/`false` behavior is unchanged.
- Admission exceptions are swallowed and converted to `false` (operation rejected as `RuntimeFaulted`).
- Commit callback runs after owner fields/receipt have been changed; callback exceptions are swallowed.
- Ordinary aggregate transition: callback flags `(true,false)`; receipt-backed aggregate commit `(true,true)`; paired migration admits each side `(true,false)` before either mutation, then notifies both after both commits.
- A paired operation requires both boundaries if either is installed. No one-sided P12 binding.
- Preserve existing checked revision guards and callback order. In particular, do not “improve” failure/fault behavior in this extraction.

The two delegates retain their existing exact type and callback contract: `(SettlementPopulationRuntime owner, bool aggregateChanged, bool receiptChanged)`. The existing Unity census bridge remains their only production provider. The actual owner reference is required because the P12 dictionaries key by owner identity.

### Owner operations and snapshot boundary

These currently `internal` members need a deliberate cross-assembly path because current callers compile in one Unity assembly. Expose only the domain operations already used by these callers, with no blanket public visibility on helpers:

| Current operation | Proposed Core boundary | Constraint |
|---|---|---|
| `TryApplyTransition` / `TryApplyTransitionWithReceipt` | Existing named owner operations become callable by Unity | Retain all validation, rejection values, receipt atomicity, and bool/out behavior |
| `ResolveOperationReceipt` | Existing named read query callable by Unity | Continue to lock on the current receipt gate; no returned receipt object |
| `TryApplyPairedMigration` | Named paired-owner operation callable by Unity | Keep same owner preconditions, same status identity, validate both sides before first write, same callbacks after both writes |
| `TryCaptureOperationReceiptSnapshot` | Replace internal out-args with/retain typed immutable receipt snapshot export | Sorted by ordinal identity; defensive copied immutable rows; include exact receipt revision |
| `TryCreateFromOwnerSnapshot` | Typed factory for a detached staged owner | Validate exactly the existing fields/duplicate rules; does not bind a guard or publish the owner |
| `RestoreSnapshot` | Keep private/internal; expose only opaque captured rollback point via `PopulationOwnerBridge` | Preserve invalid-input no-op, receipt discard predicate and receiptRevision increment; arbitrary targets cannot be supplied |
| `GetOperationReceiptCensus` | Read-only snapshot/count query callable by the census adapter | Count and revision read under the same lock |
| `CopyTransitionValue`, receipt concrete class, snapshot constructor, `ResolveOperationReceiptCore`, `CanApplyTransition`, `TryApplyTransitionCore` | Remain private | Implementation details; no API migration |
| `CanBindMutationGuard` / `TryBindMutationGuard` / explicit bindable implementation | `PopulationOwnerBridge.CanBindMutationStatus` preflights and `TryBindMutationStatus` binds the exact read-only status object | No `IVT`, no public concrete-guard dependency in Core |

Retain the existing immutable `PopulationOperationReceiptSnapshot` name and fields; make only that value class, its defensive-copy constructor and getter properties public to the official persistence assembly. Retain null/malformed value representation where the existing snapshot permits it, so the unchanged codec/factory produces the same rejection. Keep `PopulationOperationReceiptResolution` public as the named result enum used by the retained PersonDeath caller. No new whole-owner DTO or serialization format is needed: the existing City root snapshot already holds aggregate identity/count/revision plus receipt values/revision. Cross-assembly capture/factory calls go through the exact bridge signatures below and retain all current validation. The binary payload/schema and alias identity remain unchanged.
`RestoreSnapshot` remains internal and keeps its exact existing behavior. `AggregateDemographySystem`, now in Core, continues calling it directly after provider throw or detected mutation. For Unity battle rollback, add an opaque Core `PopulationRollbackPoint` with a non-public constructor and immutable internal fields `(exact owner reference, population count, population revision)`. Capture it inside the existing `PreparedSourceTransition` constructor in place of the current sequential count/revision reads, then populate `PopulationBefore` and `PopulationRevisionBefore` from that one point so no second read can drift. `PopulationOwnerBridge.CaptureRollbackPoint(owner)` creates the point; `TryRestoreRollbackPoint(owner, point)` requires `ReferenceEquals(owner, point.Owner)`, calls the exact existing internal `RestoreSnapshot(point.Count, point.Revision)`, and returns true only if count/revision match the captured values. It does not check current revision or guard status before restore: existing rollback must work after a fault and retain the current receipt-pruning predicate and receipt-revision increment. The token does not snapshot or rewrite receipts. `BattleOutcomeApplication` uses this point in its rollback loop instead of passing arbitrary count/revision values. This is a narrow captured rollback capability, not a general count setter; no arbitrary target values can be supplied.

The proposed capability splits mutation operations (domain-authorized transitions, receipt resolution, paired migration), immutable snapshot read, isolated stage factory, and trusted rollback. It keeps writer ownership explicit and prevents a DTO from becoming a shadow owner.

## Caller and writer ledger

The source graph’s runtime closure for this owner is 235 app files. The package does not move these callers. They remain explicit Unity callers through the same `SettlementPopulationRuntime` object and therefore stay in the cumulative F migration ledger:

| Caller / surface | Use and current operation | Migration disposition for this package |
|---|---|---|
| `CityRuntime` | Owns/constructs the per-City `Population` instance; guard preflight/bind currently call `Population.CanBindMutationGuard` / `TryBindMutationGuard` (lines 1261/1268) | Unity aggregate-root remains; replace only these two calls with `PopulationOwnerBridge` bound-status methods |
| `SimulationRuntime` | Supplies aggregate provider; owns runtime setup | Unity host remains |
| `AggregateDemographySystem` | Policy provider, proposals, receipt-backed commits; currently uses `RestoreSnapshot` on provider failure/mutation | Moves to Core with owner |
| `SettlementPopulationSystem` | Build/apply transition | Moves to Core with owner |
| `SettlementPopulationCensusProvider` | Two per-city read sections: `p12d.city-population.aggregate/<cityId>` and `p12d.city-population.operation-receipts/<cityId>` | Unity adapter remains; reads owner count/revision and atomic receipt census |
| `P12PopulationLifecycleCensus` | One-time bind; expected section admission; records changed section epochs | Unity composition authority remains; same delegates and owner identity |
| `P12DCityRootOwnerSnapshot` | Captures sorted receipt snapshot and stages `TryCreateFromOwnerSnapshot` | Unity persistence codec remains; value DTO crosses boundary |
| `P12ACityOwnerCodec` | Serializes/deserializes city owner payload | Unity codec remains; schema and exact values unchanged |
| `WorldStateSnapshot` | Reads population for full-world snapshot/projection | Unity diagnostics adapter remains |
| `DailyDemographicSystem` | Aggregate population policy/step path | Unity orchestration remains; calls Core proposal/apply |
| `DailyDemographyBoundaryStepProvider` | Boundary provider orchestration | Unity orchestration remains; owner calls stay on same instance |
| `PersonBirthLifecycle` | Person creation paired with aggregate count and operation receipt | Unity identity-bearing lifecycle remains writer |
| `PersonDeathLifecycle` | Receipt resolution and aggregate decrement within death transaction | Unity identity-bearing lifecycle remains writer |
| `NpcPopulationLifecycleSystem` | NPC-to-resident aggregate increment/decrement | Unity named-NPC lifecycle remains writer |
| `NpcResidenceMigrationSystem` | `TryApplyPairedMigration` at current line 478 | Unity lifecycle remains writer; must call atomic Core pair operation |
| `SettlementPopulationPresenceQuery` | Read projection | Unity adapter remains |
| `BattleOutcomeApplication` | `PreparedSourceTransition` captures rollback point with owner/count/revision at lines 104–117; rollback uses it at line 656 | Unity planner remains; cannot invent rollback target |
| `Military/ManpowerSourceConsequencePlanning` | Reads/proposes aggregate consequence | Unity consequence planning remains |

This is a represented writer ledger, not a claim that all writers have migrated. Any additional direct count/revision writer found during implementation must be added before merge. No raw count mutation API is added. Public count and revision remain read-only properties.

## Existing P12 and lifecycle semantics to freeze

- One authoritative population object per settlement `RuntimeId`; do not replace references during normal operation.
- Revisions begin at zero for a new owner and increment once per successful aggregate change; receipt-only changes follow existing behavior.
- Transition checks preserve exact ordering of checks and `PopulationTransitionFailure` codes.
- Pair migration rejects null/same-reference/same-ID, negative values, stale revisions, empty origin, full destination, revision overflow, status mismatch, and incomplete P12-boundary pairing before either write.
- A matching `(operation identity, fingerprint, transition)` is idempotent and does not apply twice; same identity with a different fingerprint or transition is a conflict.
- Receipts export in `StringComparer.Ordinal` order. Snapshot copy does not expose internal receipt entries.
- P12 census section IDs/cardinalities/revisions and operation admission/changed-section callbacks are byte/schema/epoch stable.
- A callback exception has the existing asymmetric policy: admission fails closed; post-commit notification is swallowed by the owner (the census callback itself faults runtime admission when its active context is absent/wrong-thread).

## Verification plan (not run in this read-only audit)

At package extraction, compile `Simulation.Core` and Unity against the exact active 0094 source graph; Unity parity must use the same active compiler/rsp, and no synthetic/stale cache is acceptable. Keep original implementations unchanged except necessary visibility/bridge and the direct stable-key helper substitution. Bind tests to the source commit/blob identities in the audit inventory.

Required direct Core semantic cases:

1. Preserve/port existing tests by exact name where applicable: `PopulationFoundationTests` (proposal purity, stale application, overflow/underflow, exact revision); `AggregateDemographyFoundationTests` (provider snapshot, floor, provider mutation recovery, receipt replay/conflict); `SettlementPopulationCensusTests` (receipt census, replay/conflict, restore pruning, paired migration); `DailyDemographyIntegrationTests.ProviderMutationIsRestoredAndRuntimeCalendarIsDetachedFromDefinition`; `DailyDemographyBoundaryStepProviderTests.RetainedAggregateProposalIsReusedAfterInstallInterruption`; `RuntimeAuthoritativeMutationGuardTests.FaultedRuntimeBlocksPopulationApplyButInternalRestoreCanRestoreSnapshot`; `P12DCityRootOwnerSnapshotTests.CaptureAndStage_PreserveExactCityMarketPopulationAndOrderedMembers` and `CaptureAndStage_PreservePopulationReceiptsAfterRollbackPruning`; plus `BattleOutcomeApplicationTests.UnexpectedMidTransactionFailure_RestoresWorldTruthAndEveryRevision`, `TerminalBattleStoreCommitFailure_RollsBackEarlierDomainWrites`, and `RollbackFailureFaultsRuntimeAndNextApplyRejectsBeforePlanning`. These are tracked in the exact 0094 test tree.
2. Provider success, throw, invalid result, mutation attempt, and stale-state rejection leave owner count/revision/receipt snapshot unchanged except exact legacy rollback semantics; provider mutation recovery checks receipt discards too.
3. Represented-resident floor, signed/negative components, integer underflow/overflow, stale revision/count, revision max, invalid settlement and transition failure-code precedence.
4. Receipt first apply, exact replay, conflicting identity/fingerprint/transition, ordinal snapshot order, receipt revision overflow, malformed/duplicate restore entries.
5. Pair migration succeeds atomically; each precondition rejects without changing either side; both-unbound and same-bound-guard succeed; one-bound/one-unbound, different guard, or faulted guard reject; admission receives origin then destination before writes; notifications occur after both writes.
6. P12 boundary unbound/bound/second-bind behavior; admission false/throw; commit throw; exact flags; changed aggregate/receipt sections and operation epoch under real census.
7. Encode/decode/hydrate/capture parity for exact P12 City payload and two census sections; malformed DTOs rejected before publication; staging leaves current live owner untouched.
8. Battle consequence rollback and failed-provider rollback preserve existing count, revision, receipts, receipt revision, and object identity.

The test names above were found by `git show` and `git grep` at exact baseline 0094. This audit did not execute them. The graph audit proves binding/compilation only, not these semantics.

## Cumulative closeout relationship

This owner package would be the next executable .NET capability after the already promoted definitions/owners/persistence/shared codec and provider switch. It is not Daily-v1. Continue to hold `CORE_FWAIT` until the full host cut is closed: the49 historical CORE-B regression identities plus the separate89-family/24-operation and finite P12-G obligation maps, aggregate roots and identity-bearing writers, lifecycle/death/birth/pair migration, admission and epoch callbacks, spatial/topology authority and guards, scheduler/day boundary/response pipeline, restore/hydrate codecs and parity, runtime projection/census providers, SDK9.0.315 `netstandard2.1` Core build and `net8.0` test-host proof, source-bound Unity compile/reference closure, P12 no-recompute proof, and end-to-end consistency vectors must be covered by the cumulative source-host evidence. This package increments that ledger; it does not discharge those remaining rows.








## Final assembly API and complete access cut

This section fixes the implementation boundary; the earlier table describes responsibilities, not alternative APIs. All currently internal owner methods stay internal in Core. The only new public assembly operations are the following `PopulationOwnerBridge` static methods (every `owner` is the exact installed `SettlementPopulationRuntime`, never a clone):

```csharp
bool CanBindMutationStatus(SettlementPopulationRuntime owner, IAuthoritativeMutationStatus status);
bool TryBindMutationStatus(SettlementPopulationRuntime owner, IAuthoritativeMutationStatus status);
bool IsP12PopulationBound(SettlementPopulationRuntime owner);
bool CanAdvanceP12AggregateRevision(SettlementPopulationRuntime owner);
void BindP12PopulationMutationBoundary(SettlementPopulationRuntime owner,
    Func<SettlementPopulationRuntime, bool, bool, bool> admission,
    Action<SettlementPopulationRuntime, bool, bool> committed);
void GetOperationReceiptCensus(SettlementPopulationRuntime owner,
    out int cardinality, out long receiptRevision);
bool TryCaptureOperationReceiptSnapshot(SettlementPopulationRuntime owner,
    out IReadOnlyList<PopulationOperationReceiptSnapshot> receipts, out long receiptRevision);
bool TryCreateFromOwnerSnapshot(string settlementRuntimeId, int population, long revision,
    IReadOnlyList<PopulationOperationReceiptSnapshot> receipts, long receiptRevision,
    out SettlementPopulationRuntime stagedOwner);
PopulationOperationReceiptResolution ResolveOperationReceipt(SettlementPopulationRuntime owner,
    string identity, string fingerprint, SettlementPopulationTransition transition = null);
bool TryApplyTransitionWithReceipt(SettlementPopulationRuntime owner, string identity,
    string fingerprint, SettlementPopulationTransition transition,
    out bool newlyApplied, out PopulationTransitionFailure failure);
bool TryApplyPairedMigration(SettlementPopulationRuntime origin, SettlementPopulationRuntime destination,
    long originExpectedRevision, long destinationExpectedRevision,
    int originPopulationBefore, int destinationPopulationBefore,
    out PopulationTransitionFailure failure);
bool TryApplyAggregateWithReceipt(SettlementPopulationRuntime owner, int representedResidentFloor,
    AggregateDemographyTransition transition, string operationIdentity,
    out bool newlyApplied, out AggregateDemographyFailure failure);
PopulationRollbackPoint CaptureRollbackPoint(SettlementPopulationRuntime owner);
bool TryRestoreRollbackPoint(SettlementPopulationRuntime owner, PopulationRollbackPoint point);
```

Signatures above omit only the repeated `public static` declaration syntax. Each delegates to the existing same-named owner operation or, for aggregate receipts, `AggregateDemographySystem.TryApplyWithReceipt`; do not add validation/reorder failure precedence inside wrappers. Binding uses one internal Core `MutationStatusBinding` implementing the existing null/same-reference rules. The Unity `AuthoritativeMutationGuard` implements the read-only interface; its existing concrete binding helper for other owners stays Unity-owned. Neither is another latch. `PopulationRollbackPoint` has public getter-only `Population` and `Revision` for the existing Battle prepared-state checks; its owner reference and constructor remain non-public. No receipt restore, epoch reset or capture-token right is conferred by this point.

Assembly path: move the seven named sources with their meta GUIDs to `Packages/com.simulation.core/Runtime/Population/`; add explicit linked Compile entries to `src/Simulation.Core/Simulation.Core.csproj`. Add the shared status interface/binding and composition bridge under the same Core package (one definition consumed by genealogy). The Core asmdef retains `noEngineReferences`; Unity's application assembly references it. Remove the Unity-internal `IAuthoritativeMutationGuardBindable` implementation from the moved population owner; `CityRuntime` remains the runtime's bindable aggregate and forwards its existing preflight/bind to the bridge with the original guard instance.

Retained caller changes are precisely: CityRuntime guard forwarding; P12PopulationLifecycleCensus binding, bound-state and revision-capacity queries; SimulationRuntime's population revision-capacity query; PersonDeath receipt query/commit calls; NpcResidenceMigration paired call; DailyDemographyBoundaryStepProvider aggregate receipt calls; SettlementPopulationCensusProvider atomic receipt census; P12DCityRootOwnerSnapshot receipt capture/staged factory; Battle prepared rollback point and rollback invocation. P12ACityOwnerCodec continues constructing the now-public immutable receipt value. Other named writers use existing public proposal/apply APIs unchanged. AggregateDemographySystem's private fingerprint calls the same Core stable encoder directly. These are call forwarding/access edits; no owner, writer, epoch or algorithm is copied into an adapter.

This bridge is official composition/persistence plumbing, not a factual reader, general application API, mod API or security boundary. Runtime-supported writers retain their P12 contexts and original object-keyed callbacks. General application consumers gain no new supported mutation ingress by discovering a public CLR bridge. A source/compiler/access audit plus the enumerated tests is an implementation gate; compilation failure means fix the precise represented access, not silently widen all internals.
