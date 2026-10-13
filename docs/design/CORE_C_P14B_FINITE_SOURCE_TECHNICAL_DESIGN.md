# CORE-C — P14-B finite-source domain extraction

**Status:** bounded technical design proposal; independent review pending. This
document does not authorize implementation, architecture changes, or numbered
Phase promotion.

**Checkpoint:** CORE-C, one pure-definition/domain consumer.
**Selected consumer:** the promoted P14-B finite-reserve daily source and its
prepared Market stock installation, on the exact source behavior already
implemented in Unity.
**Architecture:** `codex/architecture/simulation-core-extraction` at
`32358d2a0faf511e098c4f295a5132a4bfb8cfb2`.
**Production baseline:** `codex/phase12/canonical` at
`4cc31073f0ec6ba5c718084e4cebd6e914931d5d`.
**P14 canonical:** `codex/phase14/canonical` at
`09fcbe46ffb5a7d80377185692d550310debc110`.
**Design source availability:** remote refs were confirmed with `git ls-remote`.
The shared worktree denied a `FETCH_HEAD` update, so no claim is made that a
local fetch completed.

## 1. Contract and boundary

CORE-C extracts the existing bounded P14-B operation that debits one finite
source reserve and credits its identified item row in the settlement's Market.
The operation retains stable settlement, source, Market store, item, and
content identities; source and Market revisions; the absolute-day duplicate
boundary; the existing exact-one-row rule; checked integer stock addition; and
one prepared installation that either changes both owners or changes neither.
The Unity profile-composition boundary separately retains the stable
LocationId-to-City-anchor agreement; the current finite-source owner itself
does not store LocationId. The operation's result and rejection reasons remain
compatible with P14-B.

The Unity authoring profile and `SimulationRuntime`/`CityRuntime` composition
remain on the Unity side in this checkpoint. Unity continues to resolve
ScriptableObject content, validate the current authored profile before owner
construction, provide the already admitted absolute day, and connect the
existing P12 mutation admission/commit hooks. The extracted operation is the
actual P14-B source and stock rule implementation used by both hosts; it is not
a parallel .NET model.

P14-B remains one City, one finite source, and one item for its supported
profile. P14-C is separately promoted on current P14 canonical and provides
multiple identifiable sources. It is deliberately outside this CORE-C design:
CORE-C neither migrates P14-C composition/order nor turns its fixture into a
general multiplicity contract. P14-A's exogenous source and free-consumption
behavior remain unchanged. P10 Ruin composition remains separate. The selected
P12 Daily-v1 profile continues to reject P14 material-flow profiles before
world identity or owner construction.

This design adds no population consumption, paid consumption, market trade,
money, transportation, intraday schedule, multiple-City profile, source
framework, save/load, capture eligibility, export, hydration, or compatibility
promise. It does not claim that a P14 profile is admitted by P12 or usable by
P13.

## 2. Existing implementation inspected

At the production baseline, `FiniteSourceProductionService.cs` contains the
authoritative prepare/reject/result behavior. `FiniteProductionSourceStore.cs`
contains the source's reserve, identity, content revision, local revision, and
last-processed absolute day. The service currently takes `ItemData` and
`MarketRuntime`, so moving these files alone would not produce a portable
closure.

`MarketRuntime.cs` is mixed Unity/domain code. Its finite-source path depends on
`MarketItemRuntime`, `PreparedMarketState`, the market revision, exact matching
Market rows, stock overflow checks, and price recalculation. It also contains
Unity serialization attributes, P12 mutation admission callbacks, ordinary
market operations, and other economy behavior. The finite-source price update
uses `Mathf.Max` and `Mathf.Clamp` through `MarketItemRuntime.CalculatePrice`.
The whole file must not be copied into a second implementation or moved as a
single unreviewed block.

`Data/ItemData.cs` is a `ScriptableObject`; only the stable item definition ID
and base price are needed by this operation. `Data/CityData.cs` co-locates
`CityData`, `CityProductionConfig`, and profile authoring. Those Unity assets
remain in `Assets`. `CityRuntime.cs` coordinates the existing daily flow and
resumable economy step; its broad population, paid-consumption, diagnostics,
and temporal dependencies make it an integration hotspot, not a portable
file-level dependency. Its finite-source owner is `[NonSerialized]` today.

The authoritative P14-B record is in `docs/PHASE14_STATE.md`,
`docs/design/P14B_FINITE_SOURCE_IMPLEMENTATION_CANDIDATE.md`,
`docs/design/P14B_FINITE_SOURCE_IMPLEMENTATION_REVIEW.md`, and
`docs/validation/P14B/P14B-current-base-anchor-deferral-validation-5b5b18c.md`
at the P12 production history. P14-B's recorded exact-tree validation is not
evidence of .NET portability. P14 State at the current P14 ref records P14-A,
P14-B, and P14-C promoted and Phase 14 open.

## 3. Proposed source ownership and API boundary

Use the existing single Core source tree from CORE-A:

```text
Packages/com.simulation.core/Runtime/Economy/
  MarketItemDefinition.cs
  MarketStockAuthority.cs
  FiniteProductionSourceDefinition.cs
  FiniteProductionSourceState.cs
  FiniteProductionSourceStore.cs
  FiniteSourceProductionService.cs
```

The final file list is conditional on exact type splitting during implementation;
it is a proposed boundary, not authorization to rename public P14 APIs. Keep
global names and callers compatible where practical. Do not duplicate existing
source logic into new files while leaving the old implementation active.

### Effective item and source definitions

Add a small immutable Core `MarketItemDefinition` containing the semantic item
ID and the effective `basePrice` value consumed by Market pricing. A Unity-side
conversion from `ItemData` creates it from the existing authored asset; it must
not derive identity from Unity object identity, asset path, order, or label. The
Core type is an operation-specific effective definition, not a replacement
authoring format and not a partial substitute for `ItemData` in unrelated
capability systems.

Convert the selected `CityProductionConfig` into a Core finite-source
definition containing exactly the values consumed by the P14-B operation:
`ProductionSourceId`, `SettlementSemanticId`, `MarketStoreSemanticId`,
`LocationId`, item definition ID, `ContentRevision`, daily output limit, and
initial reserve. Existing validation for missing/blank identity, non-positive
daily limit, and negative reserve remains fail-closed. The Unity adapter owns
asset/profile resolution and supplies these effective values; Core owns the
operation validation and transition.

### One Market stock authority and atomic operation

Move the existing stock/revision/price algorithm into one Core
`MarketStockAuthority`, retaining its public behavior behind the existing
Unity `MarketRuntime` facade. The Core owner holds the authoritative rows,
revision, desired amounts, stock amounts, and current prices. Its preparation
operation returns a private immutable prepared replacement bound to the
expected Market revision. It validates exactly one matching semantic item row,
integer capacity, and Market revision exhaustion before commit.

`FiniteSourceProductionService.TryPrepareDaily` consumes that same Market
authority plus the existing finite-source owner. It first validates identities,
source/Market revisions, the strict absolute-day progression, source bounds,
and the daily limit. It prepares the Market row and source debit without
publishing either. The commit rechecks both owners and the existing host
admission gate in the same serialized call window, installs the source and
Market prepared states, advances the applicable local revisions once, then
emits the existing P12 committed-mutation notification once. There is no
callback between the two state installs. Rejection or stale state changes
neither owner. Preserve exhausted-source behavior and the current zero-output
and repeated-day semantics exactly as covered by the P14-B tests.

The Core algorithm must not depend on P12 types. Preserve the existing narrow
P12 boundary through the Unity facade: the facade checks
`CanCommitP12OwnerMutation()` before installation and calls
`NotifyP12OwnerMutation()` only after a successful atomic install. The design
does not create a second epoch, weaken admission, or claim broader P12 coverage.
The exact current callback ordering and failure behavior must be characterized
against code/tests before implementation; any callback that can throw or mutate
the participating owners would make the proposed no-intervening-callback
atomicity boundary invalid and must be handled before extraction is approved.

### Unity serialization boundary

`CityData`, `CityProductionConfig`, and `ItemData` remain Unity authoring assets
with their existing GUIDs, serialized field names, and inspector workflow. Core
contains no `UnityEngine`, `ScriptableObject`, or `[SerializeField]` reference.
The Unity adapter resolves those assets into the pure effective definitions at
the existing construction boundary.

The finite-source runtime owner is currently nonserialized on `CityRuntime`;
this design does not make it durable or add a P12 owner. Preserve that boundary
and test that the existing Unity owner construction still yields the same
initial state and operation results. `MarketRuntime` and its row types currently
have Unity-serialized private fields. The implementation must retain a Unity
serialization adapter for those existing fields and prove an asset/runtime
round-trip before changing their storage shape. The preferred boundary is Core
authority plus a Unity-only serialization snapshot adapter that imports state
before runtime use and exports it only at Unity's serialization callback. The
snapshot is an interchange representation, never a second live stock
authority. If that cannot preserve existing serialized values and field
compatibility, stop and return with the concrete mismatch; do not introduce a
save format or use `MovedFrom` to hide a failed move.

## 4. Numeric, ordering, and reconstruction requirements

All reserve, stock, quantity, and revision behavior remains integer/Int64 with
the existing bounds. Preserve the current guards against reserve underflow,
stock overflow, and revision exhaustion; do not saturate, wrap, or reorder
source debit and stock credit. Keep item-row matching by semantic `DefinitionId`
and preserve the existing deterministic iteration/order of nonmatching rows.

The selected operation recalculates current price after increasing stock. Its
existing formula is `basePrice = Mathf.Max(0.01f, basePrice)`,
`stockForRatio = Mathf.Max(1, amount)`, `multiplier = desiredAmount / stockForRatio`,
then `basePrice * Mathf.Clamp(multiplier, 0.5f, 3f)`. CORE-C must preserve the
Unity `Mathf` results for the exact operation, including float rounding,
`NaN`, infinities, signed zero, and clamp boundary behavior for effective values
that the existing profile admits. Do not replace these calls globally or assume
`System.Math` is equivalent. Before extraction, capture expected Unity results
from the canonical operation for boundary and representative values; the same
shared table must pass in Unity and .NET with exact authoritative float values.
Whether bitwise equality is the existing required equivalence must be recorded
from the numeric contract; tests must not use an epsilon to conceal a changed
price.

The operation consumes `absoluteDay` as an already-resolved monotonic daily
boundary and does not itself call `SimulationCalendar`, `SimulationDate`, or
`SimulationTimeline`. CORE-B's timeline, `BoundaryContinuationStep`, and
`SpatialStableKey` are not included in this domain checkpoint. The Unity
`CityRuntime`/`SimulationRuntime` adapter continues to invoke the same extracted
service from the existing prepared daily operation and supplies its actual
`manifest.AbsoluteDay`; no alternative scheduler or wall-clock source is
introduced.

The architecture dependency table explicitly requires CORE-A before CORE-C.
Although the P14-B kernel receives a `long absoluteDay` and has no direct source
reference to Calendar types, CORE-C does not remove that canonical DAG edge.
Implementation remains `WAIT_DEPENDENCY` on CORE-A unless the Architecture
owner revises the dependency contract. CORE-B is not a CORE-C dependency for
this bounded kernel because the domain operation accepts the existing admitted
boundary and does not move continuation/timeline types. A later CORE-D
composition that extracts the resumable timeline must consume CORE-B.

For reconstruction-sensitive results, keep source/item/settlement/Location/
store/content identities, initial/current reserve, source revision, absolute
day, Market row identity, stock amount, desired amount, current price, and
Market revision consistent with existing P14/P12 projections. This extraction
does not define persistence, serialization compatibility, replay, history, or
fork behavior. P14 material profiles remain rejected by P12 Daily-v1.

## 5. Test migration and validation

Before moving production code, freeze the exact current P14-B Unity case table
and record source identity. Retain a complete old-case to new-case mapping for
the `FiniteSourceProductionTests` suite. Move only cases whose complete setup
closure is engine-free to the shared Core test project; use the pure effective
definitions above rather than Unity `ItemData` fixtures. Keep the corresponding
Unity adapter, asset conversion, profile admission, P10/P14 separation, and P12
Daily rejection tests in Unity. Do not drop or silently rewrite any assertion.

Required Core-focused cases include all existing finite-source outcomes:
applied, exhausted, duplicate absolute day, wrong identity/content, stale source
and Market revisions, exact row cardinality, integer stock overflow, reserve
bounds, and revision exhaustion. Add atomicity assertions proving every
rejection preserves source and Market state/revisions. Add price parity vectors
for normal values and the admitted numeric boundary cases; compare the same
expected value table from the Unity baseline and .NET. Verify source identity,
item ID, output quantity, stock, reserve, revisions, and rejection reason
exactly.

Unity validation retains finite-source integration, `CityDailyEconomyContinuationTests`,
`SimulationRuntimeAdmissionTests`, and the existing P14-B profile/anchor cases.
It must show that the Unity facade calls the same Core code, the P12 mutation
gate and notification remain correctly ordered, P14-A remains unchanged, P10
and P14 profiles remain separate, and P12 Daily-v1 still rejects the profile
before identity/owner allocation. Re-run the relevant P14 focused suites, full
ALL EditMode, official Smoke, and `git diff --check` on the final integrated
tree. Because the selected service participates in daily material flow and
changes Market price/stock state, retain the applicable long-run regression
gate if the final diff changes or reintegrates daily-loop causal behavior.

Run the locked .NET restore/build/test and CoreValidation evidence workflow on
the explicit linked source/test closure. The .NET report must have nonempty,
complete case discovery and preserve original retrievable results per the
current evidence policy. Unity evidence uses the promoted Unity packager
unchanged. No XML/log edits or hash-only substitution.

## 6. Candidate files and ownership

### Core source candidates

- New `Packages/com.simulation.core/Runtime/Economy/MarketItemDefinition.cs`.
- New Core home for the existing finite source state, store, service, result,
  and prepared operation currently in
  `Assets/_Project/Scripts/FiniteProductionSourceStore.cs` and
  `FiniteSourceProductionService.cs`.
- New or moved Core Market stock authority containing the exact existing stock
  row/revision/preparation/installation and price behavior from
  `Assets/_Project/Scripts/MarketRuntime.cs`; preserve one implementation.

### Unity host/admission candidates

- `Assets/_Project/Scripts/Data/ItemData.cs` and
  `Assets/_Project/Scripts/Data/CityData.cs`: retain authoring types and add
  only the explicit effective-definition conversion needed by this consumer.
- `Assets/_Project/Scripts/MarketRuntime.cs`: retain Unity facade, serialization,
  P12 gate/notification, and unrelated market API; delegate migrated stock rules
  to Core.
- `Assets/_Project/Scripts/CityRuntime.cs`: retain construction and current
  prepared daily-step coordination; switch the existing call path to the
  extracted source/Market authority. This file is a serialized integration
  hotspot and must not be concurrently edited by another workstream.
- `Assets/_Project/Scripts/FiniteSourceProfileAdmission.cs`: remains Unity
  composition admission. Keep current P12 and P10 rejection ordering.
- Relevant `.meta` files move with source files; retain GUIDs for Unity assets
  and do not add an SDK DLL to Unity.

### Test candidates

- `Assets/_Project/Tests/EditMode/Editor/FiniteSourceProductionTests.cs`:
  preserve the full original case list; move only proven portable cases and
  retain adapter/integration coverage in Unity.
- `Assets/_Project/Tests/EditMode/Editor/CityDailyEconomyContinuationTests.cs`
  and `SimulationRuntimeAdmissionTests.cs`: Unity integration/regression only.
- `tests/Simulation.Core.Tests/`: explicit Compile links for the portable
  shared source/case file; no recursive legacy `Assets` compile.
- P14-B current-base validation/review records remain historical evidence; add
  current-tree CORE-C evidence separately rather than rewriting them.

Core package/project source paths and the Unity façade are one shared hotspot.
Perform source move, assembly integration, Unity serialization, P12 hook
integration, and validation in a single serialized implementation window.
Read-only audits and evidence review may run independently. No Library is
created in the .NET-only worktree; Unity validation uses its separately owned
Studio checkout and Library.

## 7. Dependencies, gates, and readiness

### Already satisfied or consumed by P14-B

- P8 stable `LocationId` and City anchor capabilities remain required by Unity
  composition and stay in their existing owner.
- P14-A's Market/source/sink ownership distinction remains intact; CORE-C
  changes only the finite-source domain implementation.
- P10's profile-admission boundary remains current; CORE-C does not combine the
  P10 Ruin and P14 City profiles or mint geography.
- P12's existing Market mutation-admission/commit hooks remain a host boundary.
  P12 closure does not make P14 owner state save-compatible.

### Explicit dependency edges

- **CORE-A → CORE-C:** retained because it is an explicit architecture DAG
  edge. The absolute-day parameter avoids a direct Calendar API in the narrow
  operation, but does not independently authorize removing this canonical
  prerequisite.
- **CORE-B:** not required by this design's extracted operation. Do not move
  `LogicalTimeline`, temporal continuation owners, or stable key code here.
- **P14-B:** promoted and consumed within its exact bounded scope.
- **P14-C:** promoted but deliberately not consumed; multiple-source behavior
  remains separate and is a later CORE domain closure if explicitly selected.
- **CORE-D:** depends on CORE-C and CORE-B for a shared runtime/Lab composition;
  it is not delivered here.
- No Simulation-External, P18, P19, or P20 dependency for this passive daily
  finite-source operation.

**Implementation readiness:** `WAIT_DEPENDENCY` on promoted/reviewed CORE-A,
plus independent exact-tip technical-design review and implementation handoff.
The architecture makes CORE-C ready for technical design now. This proposal is
not independently reviewed yet and does not claim implementation readiness.

## 8. Exclusions and unresolved technical checks

Excluded: P14-C multi-source composition, broad `ItemData`/capability
extraction, full `CityRuntime` or `SimulationRuntime` extraction, CORE-D Lab
host, P12 profile admission/persistence, save migration, P13 history/fork,
other P14 scopes, P19 loader/API, and any new gameplay.

Before implementation review passes, the design review must resolve from code
and tests—not product scope—(1) whether every supported Market row price value
has exact Unity/.NET parity, including non-finite authoring values; (2) how the
Unity serialization adapter round-trips the existing Market backing fields
without changing live ownership or save semantics; and (3) whether the current
P12 admission/notification callbacks can be kept outside Core while preserving
the P14 prepared install's atomicity and mutation epoch. If these checks show a
semantic change or an incompatible serialized field shape, stop and return with
the exact evidence instead of broadening the migration.

The next gate is independent technical-design review against the exact
architecture, P12 production, and P14 canonical refs recorded above. A design
PASS makes only the bounded design reviewed; CORE-A dependency and a separate
implementation authorization/readiness check remain before code migration.
