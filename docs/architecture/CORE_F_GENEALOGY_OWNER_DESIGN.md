# Core F Genealogy owner extraction: implementation design

## Decision

Extract the existing GenealogyStore and its value contracts as one pure Core owner package. Keep P12 snapshot/census/codec/coordinator and all Unity admission, membership, and writer behavior in the Unity adapter. The Core owner remains the single live store instance for each SimulationRuntime; there is no shadow owner and no conversion through IDs during ordinary reads or writes.

This is a bounded owner extraction, not a second data model. PersonId, genealogy edge semantics, the 164-field copied-definition contract, and the P12 wire format are not changed. Genealogy already depends only on PersonId for graph meaning; Unity coupling is isolated to mutation-guard binding and its internal P12 snapshot helper.

## Exact baseline and source map

Baseline: Core commit 0094febf7f97333d0653eaf9688fe1d24d73327f, architecture commit 4b69e43516b3fc548611ce704ef45af9e54e3d0e.

Source hashes at the baseline:

| Unity source | Git blob SHA-1 | Treatment |
|---|---|---|
| Assets/_Project/Scripts/Genealogy/GenealogyContracts.cs | ac98ef9bc9fedf2d88f5bb313071b71dcc8c4f5f | Split pure contracts into Core; move the internal P12-only snapshot DTO to Unity adapter |
| Assets/_Project/Scripts/Genealogy/GenealogyStore.cs | faad6785ef3ecffcbe35b27fe3efb78d7377304f | Move owner implementation to Core with guard binding substitutions and explicit lifecycle rollback port |

Relevant exact type and member responsibilities:

| Source member | Existing behavior | Boundary after extraction |
|---|---|---|
| GenealogyFailureCode, GenealogyFailure, ParentageRecord in GenealogyContracts.cs | Stable failure vocabulary, immutable failure and directed edge values | Core public contracts, behavior unchanged |
| GenealogyStore.Count, Revision, Records | Owner facts; deterministic sorted defensive edge snapshot | Core owner API, behavior unchanged |
| TryAddParentage, TryRemoveParentage | Duplicate/self/cycle checks, revision increment/overflow handling and exact overload-specific failure precedence | Core owner operations; mutation status via shared read-only port |
| TryRollbackParentageForBirth | Composite birth rollback may remove at saturated revision without increment | Core narrow lifecycle operation exposed to Unity birth coordinator; retain its special saturation semantics |
| GetParents, GetChildren, GetAncestors, GetDescendants, IsDirectParent, IsAncestorOf | Deterministic graph queries | Core owner API, behavior unchanged |
| CanReach, adjacency maintenance, record ordering | Private graph invariants | Core private implementation |
| CaptureOwnerSnapshot and TryCreateFromOwnerSnapshot | Internal P12-D owner export and unpublished exact revision hydration; includes a per-edge staging callback | Replace with immutable export/hydration bridge, preserving the per-edge callback at its actual point. |
| GenealogyOwnerSnapshot in Contracts | Internal P12 DTO with defensive/corrupt-input retention policy | Remains internal to Unity P12 adapter; Core does not own a wire/persistence DTO |

PersonId is already a Core identity value at Packages/com.simulation.core/Runtime/Identity/PersonId.cs. It is the key used by ParentageRecord equality, hash sets, adjacency dictionaries, sorting, and graph comparisons. No Unity object identity or Unity definition reference enters this owner.

## Source closure and exact consumers

### Runtime composition and writers

SimulationRuntime.cs resolves the constructor input at approximately line 1264, validates endpoint membership with ValidateGenealogyStore(resolvedPersonStore, resolvedGenealogyStore), then stores CloneGenealogyStore(resolvedGenealogyStore) at approximately line 1408. The genealogyStore field is readonly. The runtime later binds the resulting exact store to the runtime's existing mutation guard and creates the genealogy census provider from that stored instance.

P12 restore composition in P12GDailyV1RestoreCoordinator.cs around line 1113 passes stagedD.Genealogy into that constructor. It does not install another genealogy store after construction. The resulting SimulationRuntime.GenealogyStoreForWorldBoundary is the authoritative instance passed to the census provider in SimulationBootstrapComposition; preserve this exact-instance relationship.

SimulationRuntime.TryAddParentage and TryRemoveParentage remain the admitted world-write surface. They preflight the genealogy section and epoch, execute through PersonGenealogySystem under the P12 operation context, and notify the committed owner only after successful mutation. Failed mutations do not issue the committed notification. Keep these methods, section/epoch callbacks, and ordering in Unity.

PersonGenealogySystem remains Unity-owned. It checks person membership before Store writes, maps GenealogyFailureCode to PersonGenealogyFailure, and is also used by PersonBirthLifecycle. Birth's composite mutation and rollback ordering must not move into Core.

### Other readers and proof paths

- PersonBirthLifecycle calls PersonGenealogySystem.TryStoreAdd and the saturated-revision rollback operation. Preserve the exact rollback semantics.
- Succession candidate selection reads GetChildren.
- Diagnostics, WorldStateSnapshot, and TesteSimulacao read the runtime's genealogy owner and revision.
- GenealogyCensusProvider stays Unity-owned. It captures the exact runtime Store reference and emits P12 section p12d.genealogy.parentage, schema 1, count, and revision.
- P12DDailyV1OwnerPackage remains the Unity adapter. It consumes the immutable owner export for its genealogy section, performs cross-owner person-membership checks in the merged D graph, and asks the Core staged factory to hydrate the candidate.
- P12AGenealogyOwnerCodec remains the Unity wire adapter for contract p12a.genealogy-parentage/v1, schema 1 minor 0. It serializes sorted edge keys and row revisions; it rejects duplicate keys and cycles. No codec fields, aliases, or section semantics change.

Genealogy owner-local validation intentionally does not prove that endpoint PersonIds exist in the world. That validation remains in SimulationRuntime.ValidateGenealogyStore, PersonGenealogySystem, and the P12 merged graph validator. Do not add endpoint membership to Core.

## Core contract and API cut

Add the owner files under Packages/com.simulation.core/Runtime/Genealogy/, retain a single definition of every moved type, and remove the Unity copies. Add the same source files to src/Simulation.Core/Simulation.Core.csproj using explicit Compile links; the project uses EnableDefaultCompileItems=false, C# 9 and netstandard2.1. No Unity assembly reference is permitted from Core.

Core depends on exactly one mutation-status interface supplied by the aggregate owner package:

- public interface IAuthoritativeMutationStatus { bool CanMutate { get; } }
- The existing Unity AuthoritativeMutationGuard implements this interface on the original object. Its current CanMutate behavior remains authoritative; no wrapper, second latch, fault setter, or IVT is added. SimulationRuntime retains its internal IAuthoritativeMutationGuardBindable sweep: a Unity-side GenealogyMutationGuardBinding adapter delegates CanBind/TryBind to the Core bridge status-binding methods and binds the exact original guard. The Core Store must not implement the Unity internal interface.
- Each Store has a private, one-way status binding. It accepts a non-null status exactly once; rebinding the identical object is idempotent; rebinding a different object is rejected. The PersonId add overload and remove core query CanMutate before validation and return RuntimeFaulted first when false. Preserve the ParentageRecord add overload exception: a null record returns InvalidParentageRecord before forwarding to the guarded PersonId overload. No wrapper may reorder these checks.
- A Store not yet composed with a status remains usable, matching today's unbound binding behavior. This preserves independent Core tests and current staging use.
- The Unity guard binder composes the Store with the same AuthoritativeMutationGuard instance used by the Runtime. It does not replace that guard with an adapter object.

The shared interface is a cross-package dependency with the aggregate-population owner extraction. It must be introduced once and referenced from the common Core namespace/assembly. Genealogy must not define another status interface with equivalent meaning.

## Immutable export and staged hydration

Replace internal Store-to-P12 access with a public immutable GenealogyOwnerExport containing SchemaVersion, Revision, and a defensive sorted copy of immutable ParentageRecord values. GenealogyOwnerBridge.CaptureOwnerSnapshot(owner) returns this export. It exposes no Store reference, mutable collection, capture token, or world identity. Copies of PersonId values are value-equivalent; IDs are already immutable. The P12 adapter may construct its internal GenealogyOwnerSnapshot from this export.

Expose one narrow Core staged-hydration operation, GenealogyOwnerBridge.TryCreateStagedOwner(GenealogyOwnerExport export, out GenealogyStore stagedStore, out GenealogyFailure failure, Action afterRecordStagedForTest = null), that returns a fresh unbound Store or a structured GenealogyFailure. It builds indexes without ordinary mutation calls, then restores the captured revision before returning the unpublished owner. It rejects unsupported schema, negative revision, revision lower than edge count, null edge/endpoints, self edge, duplicate directed edge, and any cycle. It must not mutate or bind a supplied live Store. The P12 adapter does its person-membership/merged-graph validation after hydration and before publication, as today.

Preserve `afterRecordStagedForTest` at the existing point inside the actual per-edge hydration loop, after adjacency/record staging and before final revision installation. The official `GenealogyOwnerBridge.TryCreateStagedOwner` forwards the existing optional callback; the Core Store factory itself remains internal. This assembly bridge is not exposed through the application/host API. P12-D/G keeps its existing observer-to-factory path, including throwing and mid-edge mutation tests; do not shift these injections to after the complete factory or substitute test snapshot overrides. This preserves actual staged-owner validation and atomic failure evidence. A callback throw still escapes to the same surrounding private-stage catch, with no staged owner returned or candidate published.
Expose a narrowly named TryRollbackParentageForBirth operation because a separate Unity assembly can no longer call the current internal member. It preserves the existing exception: if revision is saturated, successful rollback removes the birth-created edge without advancing revision. This is a lifecycle port, not a general revision setter. No method may set arbitrary revision or bypass the ordinary mutation guard. The birth rollback retains its existing guard check, unlike the separate population rollback semantics.

## Guard identity and publication rules

- Bootstrap creates an unbound owner, validates all data, and SimulationRuntime binds the final Store through its existing bindable-authority sweep. A Unity GenealogyMutationGuardBinding adapter is the sweep participant and delegates binding to the Core Store; the status object is the exact existing AuthoritativeMutationGuard.
- P12 restore hydrates a fresh unbound candidate, validates graph relations, then Runtime composition binds the final clone through the same Unity adapter and existing sweep to the same Runtime guard before it is published to callers.
- Restore does not bind the discarded staged input Store and must not capture an old session's guard.
- Genealogy has no pair migration operation, so it does not impose a two-owner binding rule. If a later paired owner operation is defined, preserve the established shared rule: both-unbound is accepted, one-bound/one-unbound is rejected, and both-bound requires ReferenceEquals on the status object.
- A fault transition is performed only by the existing Runtime guard. The owner can observe CanMutate; it cannot fault the Runtime.

## Restored revision defect: narrowly scoped correction

There is one source-derived preservation defect in the current restore constructor path. TryCreateFromOwnerSnapshot restores the captured revision exactly after building edges. P12GDailyV1RestoreCoordinator passes that staged Store to SimulationRuntime, but the constructor unconditionally calls CloneGenealogyStore. That clone replays each edge with TryAddParentage and never reinstalls the source revision. Thus an admitted restored owner with zero edges and revision 2 (possible after add then remove) becomes a private runtime clone with zero edges and revision0. More generally the clone's revision is edge count, not the saved local mutation revision. This is a source-level counterexample derived from the implemented add/remove/clone algorithms; no execution or test was performed for this design.

Preserve legacy ordinary-constructor normalization and correct only the restored continuation path. SimulationRuntimeAdmissionContext.IsRestoredContinuation is internal; the true value can only be created by CreateRestoredContinuationContext, also internal. At the constructor's existing clone assignment, branch only when runtimeAdmissionContext?.IsRestoredContinuation == true to a private exact clone helper that copies the source's immutable export through the staged hydration port. Keep CloneGenealogyStore unchanged for ordinary construction. Do not use an external boolean, a public mode flag, a revision setter, or alter the P12 codec. The restored branch still produces a new unbound clone and passes the same subsequent Runtime guard binding flow.

Add a focused regression that captures a Store after add then remove (Count 0, Revision 2), restores through the real P12G-to-Runtime path, and asserts published Count == 0 and Revision == 2; add a companion ordinary-constructor assertion that its current normalization remains unchanged. Independent review is required for this correction because it repairs a promoted exact-revision contract while avoiding broader P12 semantic changes.

## Validation plan and gates

### .NET owner verification

Add focused tests to the Core test project for:

1. add, duplicate rejection, self-parent rejection, cycle rejection and exact failure codes;
2. removal and revision increments; overflow rejection; saturated birth rollback semantics;
3. transitive parents/children, ancestors/descendants and deterministic ordering;
4. immutable export defensive-copy behavior;
5. staged hydration preserving exact revision and rejecting unsupported schema, invalid revision, duplicates, self edges, cycles, and malformed entries;
6. one-way guard binding, same-object idempotence, different-object rejection, and mutation blocked by CanMutate == false;
7. unbound Store parity for staging and unit use; faulted guard plus null ParentageRecord retains InvalidParentageRecord, while the PersonId overload/remove core retain their guard-first failure.

These tests exercise the moved real owner source linked into .NET, not a second model or test-only substitute.

### Unity integration gates

Run the project's required compile and test gates after implementation. In addition to the existing full relevant suite, retain these source-level gates:

- runtime/world writes continue through TryApplyP12GenealogyMutation and preserve section/epoch admission, callback order, and no notification on rejection;
- PersonGenealogySystem still enforces person membership and failure mapping;
- birth composite mutation and rollback tests pass;
- census provider uses the exact SimulationRuntime owner and reports same count/revision;
- P12-A encode/decode round trip preserves exact sorted edges and revision;
- P12-D capture/restore reconstructs cross-owner endpoint membership and rejects cycles/duplicates as before;
- malformed late restore still leaves source retryable and published Runtime unchanged;
- restored revision regression above passes;
- no UnityEngine or other Unity source reference leaks into Core, and Unity has exactly one compiled copy of each moved type.

### Rollback

The extraction is additive at first in the Core package and .NET project, with Unity namespace/type relocation done in the same implementation change so no duplicate owner remains. If a gate fails, revert the two moved owner files and their project links together, restore the Unity type definitions, and revert the focused restored-revision branch/test. Do not leave a Core owner shadow beside the Unity owner. Preserve P12 wire bytes and Runtime publication order throughout rollback.


## Final exact cross-assembly surface and direct caller ledger

Existing public domain queries/add/remove remain public on the one Core owner. Its currently internal privileged operations remain internal and are reached only by the following official `GenealogyOwnerBridge` static assembly methods:

```csharp
bool CanBindMutationStatus(GenealogyStore owner, IAuthoritativeMutationStatus status);
bool TryBindMutationStatus(GenealogyStore owner, IAuthoritativeMutationStatus status);
GenealogyOwnerExport CaptureOwnerSnapshot(GenealogyStore owner);
bool TryCreateStagedOwner(GenealogyOwnerExport snapshot,
    out GenealogyStore stagedStore, out GenealogyFailure failure,
    Action afterRecordStagedForTest = null);
bool TryRollbackParentageForBirth(GenealogyStore owner, PersonId parent,
    PersonId child, out GenealogyFailure failure);
```

Only `public static` is omitted above. Use the one internal Core `MutationStatusBinding` shared with population; do not add a second implementation or guard latch. No Core owner implements the Unity-internal bindable interface. The Unity `GenealogyMutationGuardBinding` adapter holds the exact final runtime owner and participates where `AddRequiredMutationGuardBinding(authorities, genealogyStore, nameof(GenealogyStore))` currently runs; its two methods forward the original guard object to this bridge. It has no mutable state beyond its readonly owner reference and cannot create or fault a guard. Preserve the original sweep's preflight-all / bind-all order and restored late binding/recapture point.

`GenealogyOwnerExport` has the exact public constructor `(int schemaVersion, long revision, IEnumerable<ParentageRecord> records)` and getter-only SchemaVersion/Revision/Records. Move the existing snapshot's defensive-copy, ordinal sort and malformed-input-retention logic into that one Core value implementation. The retained Unity `GenealogyOwnerSnapshot` becomes a thin internal adapter over this export; its getters and constructor retain current outcomes without duplicating graph or copy rules. The staged factory uses the existing local graph validation and exact-revision install algorithm; it is not a new codec or world owner. P12 codec bytes and cross-owner membership validation remain unchanged.

The complete direct external file-reference set at0094 is:

| File | Exact retained responsibility |
|---|---|
| `Diagnostics/WorldStateSnapshot.cs` | Read owner facts/revision; no new writer |
| `Genealogy/GenealogyCensusProvider.cs` | Same installed owner identity, section/count/revision |
| `P12ADailyV1ImageOwnerDecoder.cs` | Retained decoded snapshot contract |
| `P12ADailyV1OwnerSectionEncoder.cs` | Existing exact owner-section encoding |
| `P12AGenealogyOwnerCodec.cs` | Same snapshot values, local validation and binary rows |
| `P12DDailyV1OwnerPackage.cs` | Bridge export and actual per-edge private hydration; merged Person endpoint validation |
| `P12EDailyV1OwnerPackage.cs` | Same related staged owner graph reference |
| `P12GDailyV1RestoreCoordinator.cs` and `.ImageRestore.cs` | Same private D graph→runtime construction, graph/vector validation, late guard/admission; no direct publication |
| `Person/PersonGenealogySystem.cs` | Existing membership checks, add/remove and saturated birth rollback via bridge |
| `SimulationBootstrapComposition.cs` | Provider bound to the exact final runtime owner |
| `SimulationRuntime.cs` | Same admitted ingress/epoch/political revision notification, ordinary clone, restored-only exact clone, guard sweep adapter |
| `Succession/SuccessionCandidateSystem.cs` | Deterministic graph queries |
| `TesteSimulacao.cs` | Existing bootstrap/read facade |

`Person/PersonBirthLifecycle.cs` remains an indirect composite writer through PersonGenealogySystem and keeps its rollback/epoch transaction. No new operation ID, callback or epoch increment is added to the Store. All supported world writes continue through the existing runtime admission path. A public CLR assembly bridge is official composition plumbing, not a factual Store API, supported arbitrary caller ingress, mod API or adversarial authorization mechanism.

The restored-only revision correction is part of this bounded package because the exact owner transfer must survive the existing Runtime clone. Both real P12-G source-capture and image-restore routes use that constructor. Source add/remove yields Count0/Revision2, but legacy cloning yields Count0/Revision0. Target-vector validation should reject that mismatched candidate; this design does **not** claim a wrong-revision world is currently published, or that this counterexample was executed. Implement and validate the exact restored-context branch above without weakening the vector check, changing ordinary constructor behavior or reopening Phase12 scope. Require authentic admitted add/remove→successful positive advance→P12-G capture/staging and persisted-image restore regression, plus empty and populated retained graphs with revision greater than Count. No seeded override substitutes for this path.

Existing case authorities include `GenealogyFoundationTests`, `GenealogyWorldIntegrationTests`, `GenealogyCensusTests`, `RuntimeAuthoritativeMutationGuardTests`, the Person birth/lifecycle tests and P12-G genealogy duplicate/late-recapture packages. Preserve exact current names found at0094; add only the focused bridge and restored-revision cases. Require full cold/warm .NET, Unity focused/P12 + ALL EditMode/Smoke and applicable LongRun under the common Master handoff. Pure owner export/SDK compilation alone cannot close those integration obligations.
