# CORE-E — deterministic demographic policy: technical design and Master handoff

**Date:** 2026-10-14. **Scope:** first individually reviewed CORE-E domain closure;
CORE-E remains a collection of closures, not a promise that every domain is portable.
**Production design base:** `codex/core/canonical` at
`ad7a18899a13ec42a3d2b6b3098dff2f0a318492`.
**Architecture base:** `63d3b8352a450b27b9a5f593c4ea166c0343c59e`.
**Design verdict:** PASS / READY_FOR_IMPLEMENTATION for this exact slice after
architecture promotion. Two independent reviews passed; implementation itself
is not delivered. Review: [E/F record](SIMULATION_CORE_E_F_REVIEW.md).
**Implementation owner:** General Orchestrator under its active Core mandate.
**Target:** `codex/core/canonical`; suggested isolated candidate
`codex/core/CORE-E-demographic-policy`. No numbered-phase ref is changed.

## 1. Bounded outcome

Extract the existing deterministic demographic sample and aggregate birth/death
policy into the same engine-free shared Core assembly used by Unity. Unity's real
daily demographic coordinator must call these same moved classes. .NET tests must
exercise the same existing rules on explicit input snapshots. This advances a
production population rule beyond the calendar/economy closures delivered by A–D.
It does not deliver a headless population owner, world advance, natural-mortality
query, full demographic subsystem or Daily-v1 continuation host.

A–D are promoted at the inspected base. A supplies the actual SimulationCalendar;
B supplies separately preserved random/timeline infrastructure, but this sampler
must retain its own existing algorithm. C/D remain delivered finite-source
kernels/scenario runtime; this slice does not depend on their economy rules.
No new gameplay, owner, definition format, universal extension surface or host
framework is needed. Domain mutation semantics remain unchanged.

## 2. Exact source, type and assembly boundary

All source paths in this document are repository-relative at the production base.
Move each type once; global type names and signatures remain as recorded below.

| Existing source/types | Core destination | Required treatment |
|---|---|---|
| `Assets/_Project/Scripts/Person/PersonId.cs`, entire PersonId | `Packages/com.simulation.core/Runtime/Identity/PersonId.cs` | Move unchanged with original .meta GUID. Pure BCL semantic identity is the complete dependency needed by the existing person sampler overload. No PersonRuntime/PersonStore move. |
| `Assets/_Project/Scripts/Population/AggregateDemographyContracts.cs`: AggregateDemographyChange, AggregateDemographyContext, IAggregateDemographyProvider | `Packages/com.simulation.core/Runtime/Population/AggregateDemographyPolicyContracts.cs` | Split whole named type bodies. Keep AggregateDemographyFailure and IAggregateDemographyRandomSource at the original path with its original .meta. New split source gets a new GUID. |
| `Assets/_Project/Scripts/Population/DailyDemographicSystem.cs`: IPersonNaturalMortalitySampleProvider, AggregateDemographicOutcomeKind, IAggregateDemographySampleProvider, entire DeterministicDemographicSampleProvider | `Packages/com.simulation.core/Runtime/Population/DemographicSampling.cs` | Split the existing interfaces/enum and complete sampler, including its PersonId overload, private string overload and Mix helper. No duplicate sampler/facade or second hash formula. |
| Same DailyDemographicSystem file: entire DeterministicAggregateDemographyProvider | `Packages/com.simulation.core/Runtime/Population/DeterministicAggregateDemographyProvider.cs` | Split unchanged policy, constructor/default sample choice, GetChange and CalculateDailyEvents. |

Preserve the remaining DailyDemographicSystem source and .meta. Its retained types
are RepresentedResidentFloorSnapshot, DailyDemographyDiagnosticSeverity,
DailyDemographyDiagnostic, DailyDemographyReport and DailyDemographicSystem.
Do not move them just because they shared a file with the policy.

One intentional accessibility change is fixed by this design:

```csharp
public AggregateDemographyContext(
    string settlementRuntimeId,
    long populationRevision,
    int currentPopulation,
    int representedResidentFloor,
    long currentAbsoluteDay = 0L)
```

This is the existing constructor changed from internal to public with the same
body, field meanings, argument order and default. It constructs a detached policy
input, not a validated population transition, owner witness or mutation permission.
Do not add constructor validation, normalization, new defaults or a second input DTO.
The existing Assembly-CSharp caller in AggregateDemographySystem must still create
this context after its own validation. Its transitions remain non-publicly
constructible; its current TryPropose/TryApply validation and authority remain.
This narrow value-input surface is internal composition support, not a supported
P19 mod/public extension API promise. No blanket InternalsVisibleTo or Store exposure.

Add exactly four Compile links to `src/Simulation.Core/Simulation.Core.csproj`.
Keep existing `Simulation.Core.asmdef` noEngineReferences, autoReferenced and its
empty dependency list; no new assembly is needed. Continue netstandard2.1/C#9,
net8.0 tests, SDK 9.0.315 and locked NUnit/Test SDK dependencies. Unity compiles
package source; SDK output DLL must never also be imported into Unity.
Only BCL, moved PersonId, moved policy contracts and promoted SimulationCalendar
are in this closure. It has no upward reference to Assembly-CSharp, Unity,
SimulationRuntime, population owners, assets or P12 DTOs. Preserve semantic
identity/equality; randomized string hash codes are not durable identities or
ordering keys and must not become such during extraction.

## 3. Dependencies that stay in Unity and in the current authority

Leave AggregateDemographySystem/Transition, SettlementPopulationRuntime/System/
Transition, PopulationChangeSet, PopulationOperationReceipt, resident membership/
presence, Person lifecycle/queries/stores, DailyDemographicSystem orchestration,
DailyDemographyBoundaryStepProvider, configuration resolution, P9 genesis, P12
census/capture/codec/restore and SimulationRuntime at their existing paths.

No dependency inversion port is needed beyond making the existing immutable
context constructible across the assembly boundary. The existing provider/sample
interfaces already invert policy and sample supply. Move those interfaces with
their consumers; keep injected providers compatible. Unity's authoring/configuration
adapter continues to resolve effective rates/calendar. Its daily coordinator
supplies settlement identity/day/current population/revision/resident floor and
installs changes only through existing owner operations. The moved policy chooses
births/deaths; adapters cannot reimplement its formula. Natural-mortality evaluation,
Person mutation order, aggregate ledger commits/receipts and P12 callbacks remain
owned by their current production code. No new mutable population snapshot owner.

## 4. Determinism, parity and reconstruction-sensitive inputs

Preserve exact existing behavior, including awkward edge behavior rather than
opportunistically repairing it:

- Sampler mixes outcome then entity by UTF-16 characters with the existing 0xff
  separators, FNV offset/prime, unchecked day mixing and high-word shift. Keep
  `person`, `birth`, `death`, the 53-bit conversion and null/blank/negative-day
  0.5 fallback. The aggregate outcome overload currently treats any value other
  than Birth as Death; do not add validation or change enum values.
- Annual-rate/calendar conversion, Math.Floor, strict sample < fractional-event
  comparison, int.MaxValue saturation, NaN/infinity/range exceptions and their
  order remain exact. Constructor validation remains calendar-null only; retain
  the existing default sampler construction. GetChange evaluates the birth sample
  before the birth calculation, then the death sample before the death calculation.
  A rejected/throwing birth calculation prevents the death request. Injected sample
  side effects/exceptions must retain this exact order and short-circuit behavior. Deaths use the same Math.Min
  against AggregateOnlyPopulation. Do not reinterpret malformed detached contexts
  as valid owner state or silently normalize them.
- Preserve AggregateDemographyChange/Context value equality/hash and unchecked
  arithmetic behavior. No extra RNG draw, keyed algorithm substitution, global
  Random, host clock, culture-sensitive IDs, new day cadence or altered resident
  floor. Do not replace this algorithm with CORE-B streams merely to reuse them.
- Retained inputs are the same semantic Person/settlement IDs, logical day,
  effective calendar/rates, resident floor/current population/revision and any
  explicit sample provider configuration/state. Built-in samples are stateless;
  no new causal owner or serialization state is created. Arbitrary injected
  stateful providers do not gain support in P12 by moving the interface.

P12 Daily-v1 retains its exact composition/owner/provider/epoch inventory and
exclusions. No P14, P18 intraday, P20 populated activity or P12-F scope growth.
P12 current-host Unity descriptors, content/genesis fingerprints, binary fields,
ID strings, ordering and full allocator/RNG state remain intact. New assembly MVIDs
are real build changes: do not mask them, translate old saves or promise cross-host
interchange. New extracted Unity build must save/load/restore/continue within itself;
old-build descriptor/save mismatches fail closed without slot overwrite or WorldId
publication. PersonId moves assembly but not semantic identity or P12 wire values.

## 5. Implementation, migration, hotspot ownership and rollback

1. Refresh refs; classify all changes after ad7a188. Import only this reviewed
   architecture/documentation delta onto current executable Core canonical; never
   merge the older architecture Assets tree. Preserve A–D/evidence history.
2. In the isolated candidate, add one shared BCL/NUnit test source under
   `Assets/_Project/Tests/EditMode/Editor/Core/DeterministicDemographyPolicyTests.cs`
   plus optional adjacent data-only vectors. First run these characterization
   cases in Unity against the unchanged pre-move classes and record exact
   source/input identity and expected results. Obtain exact doubles as bit values
   and fixed results from this production baseline; do not compute expectations
   with a copied sampler implementation. This focused baseline run is a parity
   reference, not a replacement for the final integration gates.
3. Split/move exactly the four sources above, adjust only required imports and
   the named context constructor visibility; add explicit SDK links for production
   and shared tests. Leave owner/coordinator logic unchanged. Keep source/GUID
   mapping and class/constructor inventory in the candidate record.
4. Run final .NET and Unity gates, compare the characterized cases, review full
   migration diff independently, then normal bounded Core promotion and owning
   CORE_STATE update. No current implementation worktree is edited by this design.

Own the shared package, Core csproj/shared test inventory, PersonId and the two
mixed population source files in one serialized integration window. Read-only
CORE-F definition/owner design is PARALLEL SAFE. Other writers touching these
symbols/package inputs are PARALLEL WITH ISOLATION and serialize integration.
SimulationRuntime/P12 admission/codec behavior is outside the edit: a new need to
change it requires bounded technical review, not an implementation shortcut.
Unrelated numbered-phase work is not blocked by CORE-E or the open Core roadmap.

Keep a recoverable pre-extraction build/content/save fixture and candidate refs.
Use additive revert commits if integration fails; restore the matching old build
and saves together. Never force-reset shared history, mix descriptor identities,
overwrite a user slot for tests or delete other worktrees/caches. If a hidden
transitive dependency or serialization migration appears, stop only this slice,
record the precise edge and revise/review its design. No unreviewed public bridge.

## 6. Required validation and evidence

**Shared .NET/Unity characterization:** exact sample bit vectors for PersonId and
settlement identity, Birth/Death, UTF-16 (including surrogate pairs), empty/blank/
null and boundary days (0, positive, negative, long.MaxValue), culture and call-
order independence. Test aggregate fallback/default vs injected sample provider;
zero/integer/fractional rates, strict threshold equality, both sides of threshold,
int saturation, nonfinite/negative rates and nonfinite/out-of-range samples,
calendar-null rejection, deferred rate validation, thrown samples/order, default
and malformed input-context behavior and death cap. Assert complete change/context
values and unchanged caller state, not aggregate pass counts alone. Verify PersonId
construction/TryCreate/equality/null/ordinal semantics and one defining assembly.

Use an explicit shared-test case map: original policy has no standalone conformance
suite at the pinned base. All existing AggregateDemographyFoundationTests,
DailyDemographyIntegrationTests and DailyDemographyBoundaryStepProviderTests remain
in Unity, with original assertions/case identities preserved. Do not claim those
owner/receipt tests became portable.

**.NET:** locked source-closure build with no Unity references; cold/warm full
CoreValidation, nonempty complete discovered selection, all existing 113 cases
preserved plus the new cases; current Lab locked cold/warm regression unchanged.
Use the promoted Tools/CoreValidation and supported TRX packager at dispatch.
Current entrypoints are `Invoke-CoreValidation.ps1 -SkipLab` for supported Core
TRX packaging and `Invoke-LabValidation.ps1` for locked Lab cold/warm validation;
the default Core runner also composes the Lab regression. Retain its actual source
inventory behavior rather than inventing a new runner.
Capture actual complete SDK/CLR/OS/architecture/project/lock/source/test/tool/
assembly identities, before/after inventories and full results. Do not hardcode
113 as a permanent total; map additions and any intervening canonical change.
No Unity Library should be created in the .NET-only checkout.

**Unity focused:** shared characterization and Person identity, aggregate foundation
(proposal/apply/stale/floor/overflow/receipt replay), daily demography and temporal
boundary integration, named mortality/age/Person lifecycle, configuration/genesis
fingerprint and existing selected-profile admission/owner/cardinality suites.
Run P12 codec/compatibility, persisted save/detached-load/restore/continue parity,
stale-token/faulted boundary rejection, and old/new incompatible descriptor rejection.
Assert no owner/census membership change. Validate existing authoring/bootstrap
references and script import with moved .meta GUIDs; no scene/asset semantics edit.

**Integration:** full ALL EditMode and official Smoke remain mandatory. Because
this slice relocates policy used in daily population causality, retain the existing
official LongRun gate (`SimulationRuntimeLongRunTests`, including 100/1000/5000-day
scenarios) and its original horizon/seeds/assertions on final executable inputs; no shortened replacement. No .NET substitution for these Unity gates.
All selected cases must pass; diagnose genuine failures rather than weakening
numeric parity or silently changing selection.

Apply `docs/operations/VALIDATION_EVIDENCE_POLICY.md` and the supported
`DOTNET_TRX_EVIDENCE_CONTRACT.md`. Complete normalized public results must be
source-compared to unchanged protected originals under the delivered adapter;
archive integrity alone is a separate claim. Keep exact input/case/assembly mapping,
correct Git-blob versus checkout digest labels and original run versus later
packaging-tool provenance. Reuse unchanged baseline A–D evidence only as history;
new source/assembly inputs require the affected fresh runs above.

**Demonstrability:** FOLLOW-UP_DEMONSTRATION. A later Lab demographic scenario
should show real owner population changes after that owner/operation closure is
reviewed and extracted. Adding an independent CLI population ledger or copying
DailyDemographicSystem now is excluded. Current real finite-source Lab remains
available; .NET tests prove this bounded pure policy directly.

## 7. Closure and Master dispatch

Close only this CORE-E slice when the exact split compiles engine-free, Unity uses
that sole implementation, shared results match pre-move production outcomes,
retained daily/owner/P12 behavior and all gates pass, independent implementation
review passes and the bounded delta is promoted on Core canonical. CORE-E as a
whole is not marked complete. Record remaining domain closures and CORE-F gates.

After design PASS and architecture promotion, this slice is READY_FOR_IMPLEMENTATION
for Master dispatch against refreshed Core canonical with the above ownership window.
No product decision is required. CORE-F implementation remains WAIT_DEPENDENCY;
its [readiness inventory/design work](SIMULATION_CORE_F_READINESS.md) is separate.
CORE-X stays DEFERRED; P13/Phase status is unaffected.
