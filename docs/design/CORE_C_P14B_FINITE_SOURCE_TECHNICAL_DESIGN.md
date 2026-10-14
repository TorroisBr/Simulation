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
multiple identifiable sources. It is outside this CORE-C implementation
boundary: CORE-C does not migrate P14-C composition/order, change its finite
contribution calculation, or turn its fixture into a general multiplicity
contract. P14-C continues to use the existing Unity Market prepared snapshot
and live Market authority. Its existing tests are retained as integration
regressions for this extraction, but they do not assert that P14-C calls the
CORE-C operation. P14-A's exogenous source and free-consumption behavior remain
unchanged. P10 Ruin composition remains separate. The selected P12 Daily-v1
profile continues to reject P14 material-flow profiles before world identity
or owner construction.

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
ID and the effective `basePrice` value consumed by Market pricing. The Unity
row adapter creates it from the `ItemData` reference already attached to each
Market row; this matches current `MarketItemRuntime.UpdatePrice`, which uses
that row reference rather than the `item` argument used to locate it. It must
not derive identity from Unity object identity, asset path, order, or label. The
Core type is an operation-specific effective definition, not a replacement
authoring format and not a partial substitute for `ItemData` in unrelated
capability systems.

Convert the selected `CityProductionConfig` into a Core finite-source
definition containing exactly the values consumed by the P14-B operation:
`ProductionSourceId`, `SettlementSemanticId`, `MarketStoreSemanticId`, item
definition ID, `ContentRevision`, daily output limit, and initial reserve.
`LocationId` is not consumed by the current finite-source owner/service; the
Unity profile boundary continues resolving the authored Location and validating
its City anchor before construction. Existing validation for missing/blank
identity, non-positive daily limit, and negative reserve remains fail-closed.
The Unity adapter owns asset/profile resolution and supplies these effective
values; Core owns the finite operation validation and transition.

### Core prepared operation behind the existing Market authority

Keep `MarketRuntime` as the sole live Market owner in this checkpoint. Do not
move or duplicate its general stock, revision, price, serialization, or
transaction authority. Add a pure Core finite-source preparation function that
accepts immutable values for the finite-source state and an ordered snapshot of
the existing Market rows, validates the P14-B operation, and returns a prepared
finite-source transition plus a prepared ordered Market-row replacement. It
does not mutate either input. The Unity facade converts its live owner to this
value request, calls Core, converts a successful result back into the existing
`PreparedMarketState`, and performs the existing revision/admission/install/
notification protocol. The finite-source owner and `MarketRuntime` remain the
only live owners; the Core request/result are short-lived prepared values, not
second stores.

The Core operation preserves current P14-B checks: exact identity/content,
expected source and Market revisions, strictly newer absolute day, valid source
reserve/output bounds, one matching Market row by ordinal `DefinitionId`,
integer stock capacity, and revision exhaustion. Preserve existing rejection
precedence across source validation and `MarketRuntime.TryPrepareFiniteStockIncrease`;
in particular, do not reorder mutation admission, row cardinality, overflow,
and stale-revision failures. A successful positive contribution prepares the
same whole reserve debit and stock increase; exhausted or rejected operations
leave both owners unchanged.

#### Public Core boundary and Unity handshake

The Unity production assembly consumes the package as a separate assembly, so
the value-only entry point and its DTOs must be public; do not use
`InternalsVisibleTo` or expose live owner types. The bounded API is:

```csharp
public static class FiniteSourceProductionKernel
{
    public static bool TryPrepare(
        FiniteSourceOperationRequest request,
        out FiniteSourceOperationResult result,
        out string rejectionReason);
}
```

`FiniteSourceOperationRequest` is an immutable value request containing the
source snapshot (source/settlement/Market/item identities, content revision,
reserve, daily limit, local revision, last processed absolute day), expected
source and Market revisions, admitted absolute day, requested quantity, and an
ordered copied list of `MarketStockRow` values. A row has an explicit row-kind
discriminator (`NullRow`, `NullItem`, or `Item`) so null list rows and rows with
no item remain distinct; item rows carry DefinitionId, the row's effective
basePrice, amount, desiredAmount, and currentPrice. The request constructor
copies its input list so callers cannot mutate the prepared input during the
call.

`FiniteSourceOperationResult` is an immutable prepared value containing the
next reserve, source revision, last-processed day, and a full ordered Market
row replacement with the same row kinds and count. It contains no Unity object,
live store, callback, or install method. Failure returns `false`, a null result,
and the existing P14-B rejection reason; it does not mutate the request.
`MarketStockRow` and the source snapshot types are public immutable Core values
because they cross the Unity/Core assembly boundary, but they are operation
DTOs, not new live authorities or a persistence format.

The Unity facade in the existing `MarketRuntime`/finite-source service owns the
handshake. It snapshots `FiniteProductionSourceStore` and `MarketRuntime` rows
in order, builds the request, and calls the Core function. On success it
validates the result's cardinality and row-kind/index correspondence, maps it
back to the existing `PreparedMarketState` using the retained parallel row
references, and creates the existing Unity-side
`FiniteSourceProductionPreparation`. That preparation remains the only commit
coordinator: it rechecks live revisions and admission, installs the source
owner and Market owner using their existing methods, then sends the existing
post-commit notification. Core never installs either owner. On failure, the
facade forwards the same rejection reason and does not create an installable
preparation. The current source validation and Market row/cardinality checks
retain their rejection precedence at the same boundary as today.

#### Existing P12 callback sequence to preserve

P12 mutation admission is a synchronous, potentially stateful callback. Keep
the existing calls, order, and failure points; do not consolidate them as an
optimization. For the direct `FiniteSourceProductionService.TryProduceDaily`
route, a successful positive operation reaches these admission calls:

1. `MarketRuntime.TryPrepareFiniteStockIncrease` invokes
   `CanCommitP12OwnerMutation()` while preparing the Market replacement.
2. `FiniteSourceProductionService.TryPrepareDaily` invokes
   `MarketRuntime.CanInstallFiniteStock(...)` after the source-store
   preflight; that helper invokes `CanCommitP12OwnerMutation()` again.
3. `FiniteSourceProductionPreparation.Install()` invokes `CanInstall()`,
   which calls `CanInstallFiniteStock(...)` and therefore checks admission a
   third time before either owner mutates.

After the source and Market installs, `NotifyFiniteStockInstalled()` invokes
the P12 committed-mutation hook once. The direct route therefore has three
admission calls and one post-commit notification on success.

The `CityRuntime` prepared daily route retains the same first two checks during
`TryPrepareDaily`, followed by: (3) the outer City preparation's final
`FiniteSourceProductionPreparation.CanInstall()`, (4) the
`CityDailyEconomyCommit.TryCommit` validation's `FiniteSource.CanInstall()`,
and (5) the final `FiniteSource.Install()` pre-mutation `CanInstall()` check.
This route therefore makes five admission calls before owner installation and
one post-commit notification after both installs. Preserve the current
admission callback exception behavior (rejected) and notification callback
exception behavior (swallowed). No callback may be introduced between the two
owner installs. P14-C's separate aggregate sequence below remains unchanged.

Add focused tests with a recording/stateful admission callback that assert the
exact call count and order for direct and CityRuntime prepared success, failure
at each admission position, no partial owner mutation on rejection, and exactly
one notification after successful dual install. Retain callback-throws cases
for the existing reject/swallow behavior. These assertions are part of the
extraction contract because callback behavior is observable.
Admission callback exceptions remain rejected by the current helper;
notification callback exceptions remain swallowed by the current helper. There
is no callback introduced between owner installs. For P14-C, preserve its
separate aggregate sequence: `CanInstallMaterialFlow` checks admission during
outer validation and again in `P14CMixedSourceProductionPreparation.Install()`
when the revision-increment count is positive, followed by one aggregate Market
install and one existing post-install notification. Preserve P14-B's
zero-output, exhausted-source, stale-state, and duplicate-day behavior, along
with P14-C's zero-applied finite-source preflight.

The Core operation is called only by the existing P14-B single-source route.
P14-C remains the already-promoted mixed-source consumer and remains wholly
Unity-side in this checkpoint. Its coordinator retains its ordinal source
sort, exogenous-source behavior, whole-contributor overflow policy, outcomes,
replay/receipt behavior, one prepared Market replacement, and commit boundary.
Its finite contribution calculation and call to the authoritative Market
price calculation are unchanged. P14-C's integration suites must demonstrate that the extraction
leaves those results unchanged; they do not establish Core sharing. A successful
P14-B finite install increments the Market revision once. P14-C retains
`MarketRevisionIncrements` equal to the count of successfully applied source
contributions and advances revision once by that count at aggregate install;
it emits its existing single post-install notification. An exhausted P14-C
finite source retains its current no-owner-mutation behavior while still
honoring the existing stale/day preflight. Moving P14-C to a shared Core
contribution API is a separate design and is expressly outside CORE-C.

All other Market callers remain on the same live Unity `MarketRuntime` and its
existing methods. In particular `AddStock`, `RemoveStockUpTo`, `UpdatePrices`,
`PrepareReplacement`, keyed-sale preparation, and the purchase/sale flows in
`EconomyTransactionService` are unchanged and are not redirected through the
finite-source function. Existing single-increment operations retain their
current one-revision behavior; P14-C's aggregate multi-increment path retains
its existing count semantics. CORE-C must not change price or transaction
behavior outside the finite-source prepared contribution.

The pure Core function has no P12 dependency. The current Unity facade remains
responsible for `CanCommitP12OwnerMutation()` and the post-success notification.
The existing hooks remain in the same admission/commit order, with no new epoch,
lock, or callback between source and Market installs. P12 owner-mutation
regressions are part of the integration suite; no broader P12 coverage is
claimed.

### Unity serialization boundary

`CityData`, `CityProductionConfig`, and `ItemData` remain Unity authoring assets
with their existing GUIDs, serialized field names, and inspector workflow. Core
contains no `UnityEngine`, `ScriptableObject`, or `[SerializeField]` reference.
The Unity adapter resolves those assets into the pure effective definitions at
the existing construction boundary.

The finite-source runtime owner is currently nonserialized on `CityRuntime`;
this design does not make it durable or add a P12 owner. Preserve that boundary
and test that the existing Unity owner construction still yields the same
initial state and operation results. No Unity-serialized live owner is moved.
The pure request/result types carry values only; they never serialize
`ItemData`, `MarketRuntime`, or `MarketItemRuntime`.

The Unity adapter imports the existing serialized
`MarketRuntime.items: List<MarketItemRuntime>` list row by row. Each
`MarketItemRuntime` retains its current serialized private fields `item`,
`amount`, `desiredAmount`, and `currentPrice`; do not rename, reorder, remove,
or change their serialized types. `MarketRuntime.revision` remains
`[NonSerialized]` and is supplied separately. Import every list row by row,
without filtering, sorting, grouping, or resolving rows back from IDs. Preserve
the original ordinal index and a parallel reference to each original
`MarketItemRuntime` and its `ItemData` object. For every row, map its item
DefinitionId/basePrice when `Item != null`, plus exact `amount`,
`desiredAmount`, and `currentPrice` values. Represent a null row distinctly
from a non-null row whose `Item` is null. Keep duplicate DefinitionIds as
separate rows so Core's current exact-one-match rule rejects duplicates rather
than collapsing them. The target row is matched by ordinal DefinitionId but
uses the basePrice from that existing row's `Item` reference, matching current
`MarketRuntime.TryPrepareFiniteStockIncrease`; do not substitute the caller's
`ItemData` instance just because its DefinitionId matches.

On successful return, rebuild the existing `PreparedMarketState` at the same
list length and order. Preserve null positions and all non-target row fields
exactly; for the target row, preserve its exact `ItemData` reference and
`desiredAmount`, change only `amount` and recomputed `currentPrice`, and retain
all untouched serialized values. The current Market `revision` remains
`[NonSerialized]` and is supplied separately as the expected revision; prepared
installation continues to increment it using the existing P14-B/P14-C rules.
The adapter rejects the prepared result before any owner install if it cannot
map every input row one-to-one or preserve this representation. A failed map
is a hard stop for extraction; do not discard null rows, merge duplicate IDs,
change serialized field names/types, introduce a new save format, or use
`MovedFrom` to conceal incompatibility.

Keep the P14 authoring assets and their GUIDs/serialized fields unchanged. The
Unity adapter resolves `CityProductionConfig` and `ItemData` into pure values;
the finite source live owner remains on the existing Unity side for this
checkpoint. This avoids claiming that P14 state is serializable, durable, or
P12-compatible.

## 4. Numeric, ordering, and reconstruction requirements

All reserve, stock, quantity, and revision behavior remains integer/Int64 with
the existing bounds. Preserve the current guards against reserve underflow,
stock overflow, and revision exhaustion; do not saturate, wrap, or reorder
source debit and stock credit. Keep item-row matching by semantic `DefinitionId`
and preserve the existing deterministic iteration/order of nonmatching rows.

The selected operation recalculates current price after increasing stock. The
exact current expression in `MarketItemRuntime.CalculatePrice` is:

```csharp
float basePrice = Mathf.Max(0.01f, item.basePrice);
float stockForRatio = Mathf.Max(1, amount);
float multiplier = desiredAmount / stockForRatio;
currentPrice = basePrice * Mathf.Clamp(multiplier, 0.5f, 3f);
```

`Mathf.Max(1, amount)` selects the integer overload and its result is converted
to `float` by assignment to `stockForRatio`. Since the denominator is therefore
`float`, C# promotes `desiredAmount` from `int` to `float` and performs a
single-precision floating-point division; this is **not integer division**.
The multiplication is also single precision. Do not use `double`, reorder
operations, or introduce fused/approximate math.

For Unity's reference implementation (Unity-Technologies/UnityCsReference,
`Runtime/Export/Math/Mathf.cs`), the relevant overload semantics are
`Max(float a,b) => a > b ? a : b`, `Max(int a,b) => a > b ? a : b`, and
`Clamp(v,min,max) => v < min ? min : v > max ? max : v`. Thus the operand order
matters: `Mathf.Max(0.01f, NaN)` returns the second operand (`NaN`); positive
infinity remains positive infinity; negative infinity and either signed zero
select `0.01f`. The multiplier is finite for every `int` desired amount and
positive finite `stockForRatio`, including malformed non-positive desired rows;
the clamp maps those ratios below 0.5 to 0.5. This path has no explicit
base-price finite/positive validation before recalculation. Do not add one.
Consequently the current formula can produce NaN or positive infinity prices
from such ItemData values. P12's separate owner-snapshot validation rejects
non-finite/non-positive current prices, but P14 profiles remain outside that
P12 profile and CORE-C changes no admission boundary.

There must be one authoritative implementation of this Market price rule. Move
the expression into a narrowly named public pure-Core helper,
`MarketPriceCalculator.CalculateCurrentPrice(basePrice, desiredAmount,
amount)`, and make the existing Unity `MarketItemRuntime.CalculatePrice`
delegate to it. The helper owns only this current-price formula; it does not
own Market rows, revisions, stock, or transactions. Every existing Unity caller
of `MarketItemRuntime.CalculatePrice` continues through that delegation, so
ordinary Market operations and P14-C use the same implementation as the
finite-source prepared calculation. Do not retain a second copy of the
formula in the finite-source kernel or Unity adapter. Preserve the exact
expression order, `Mathf` comparison behavior, and source-run bit patterns;
the implementation design must confirm the Core target runtime reproduces
those semantics bit-for-bit. Any unavoidable runtime-specific compatibility
branch belongs inside this single helper, never in a second caller-specific
formula.

The following exact binary32 vectors are calculated from the source expression
above; input bit patterns identify `item.basePrice`, and `amount` is the stock
amount after the proposed finite-source increment. They are the expected
cross-host vectors to capture from the pinned Unity Editor before implementation
and then compare bit-for-bit in Unity and .NET:

| basePrice bits | desiredAmount | amount | normalized base bits | multiplier bits | currentPrice bits |
|---|---:|---:|---:|---:|---:|
| `0x40000000` (2.0) | 3 | 6 | `0x40000000` | `0x3F000000` (0.5) | `0x3F800000` (1.0) |
| `0x3FC00000` (1.5) | 3 | 4 | `0x3FC00000` | `0x3F400000` (0.75) | `0x3F900000` (1.125) |
| `0x40000000` (2.0) | 9 | 3 | `0x40000000` | `0x40400000` (3.0 after upper clamp) | `0x40C00000` (6.0) |
| `0x40000000` (2.0) | 1 | 3 | `0x40000000` | `0x3EAAAAAB` (binary32 1/3; clamp to 0.5) | `0x3F800000` (1.0) |
| `0x3C23D70A` (0.01) | 3 | 2 | `0x3C23D70A` | `0x3FC00000` (1.5) | `0x3C75C28F` (0.015) |
| `0x80000000` (-0.0) | 1 | 2 | `0x3C23D70A` | `0x3F000000` (0.5) | `0x3BA3D70A` (0.005) |
| `0x7F800000` (+infinity) | 1 | 2 | `0x7F800000` | `0x3F000000` (0.5) | `0x7F800000` (+infinity) |
| `0xFF800000` (-infinity) | 1 | 2 | `0x3C23D70A` | `0x3F000000` (0.5) | `0x3BA3D70A` (0.005) |
| `0x7FC00000` (canonical quiet NaN) | 1 | 2 | `0x7FC00000` | `0x3F000000` (0.5) | `0x7FC00000` (quiet NaN) |

These vectors do not replace the Unity baseline run: before source movement,
execute the unchanged Market operation in the pinned Unity 6000.3.9f1 Editor,
record the exact input and output bits, and compare the table. If an actual
Unity result differs—including NaN payload behavior—revise the expected vector
from that retained source-run evidence and independently re-review the design
before implementation. .NET must then match the measured Unity values exactly;
no epsilon comparison is allowed. The shared helper must preserve current
behavior for ordinary Market callers as well as this prepared operation.

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
expected value table from the Unity baseline and .NET. Add the callback-order
and callback-count cases specified above, including rejection at every
admission position and no owner mutation on each rejection. Verify source identity,
item ID, output quantity, stock, reserve, revisions, and rejection reason
exactly.

Unity validation retains finite-source integration, `CityDailyEconomyContinuationTests`,
`SimulationRuntimeAdmissionTests`, and the existing P14-B profile/anchor cases.
Also include `P14CMixedSourceProductionTests` in full, covering P14-C's direct
and P18D prepared routes, source ordering, replay, zero-output finite source,
one aggregate installation, and revision increments. Verify that P14-C's
existing Unity-side finite contribution and exogenous quantity/order,
aggregate receipt, and one-commit semantics remain unchanged. Add focused Market row-mapping
tests for ordered item references, null rows, null item references, duplicate
DefinitionIds, unrelated rows, and exact preservation of serialized values.
Run `SettlementStockOwnershipTests` and `EconomyTransactionTests` plus every
other suite that directly covers `MarketRuntime.AddStock`,
`RemoveStockUpTo`, `UpdatePrices`, `PrepareReplacement`, or its transaction
callers. These guard existing Market authority outside the finite-source
operation; they are not candidates for migration into CORE-C.

The Unity validation must show that only the P14-B facade calls the Core
finite-source kernel, all `MarketItemRuntime` price callers delegate to the
single Core `MarketPriceCalculator`, and the P14-C regression suite retains its existing
outcomes and prepared-flow semantics. It must also show that the P12 mutation
gate and notification remain correctly ordered, P14-A remains unchanged, P10
and P14 profiles remain separate, and P12 Daily-v1 still rejects P14 profiles
before identity/owner allocation. Re-run the affected focused suites, full ALL EditMode, official
Smoke, and `git diff --check` on the final integrated tree. Because the
selected operation participates in daily material flow and changes Market
price/stock state, retain the applicable long-run regression gate if the final
diff changes or reintegrates daily-loop causal behavior.

Run the locked .NET restore/build/test and CoreValidation evidence workflow on
the explicit linked source/test closure. The .NET report must have nonempty,
complete case discovery and preserve original retrievable results per the
current evidence policy. Unity evidence uses the promoted Unity packager
unchanged. No XML/log edits or hash-only substitution.

## 6. Candidate files and ownership

### Core source candidates

- New `Packages/com.simulation.core/Runtime/Economy/MarketItemDefinition.cs`
  and immutable Market-row request/result values for finite preparation.
- New Core implementation of the finite-source preparation rules currently in
  `Assets/_Project/Scripts/FiniteSourceProductionService.cs`. Keep only the
  finite source snapshot/transition values required for this operation; the
  Unity `FiniteProductionSourceStore` remains the live owner and installs the
  prepared source result.
- Core's finite stock calculation consumes an ordered value snapshot and
  returns a prepared row replacement. It is not a general Market stock owner.
  `MarketRuntime` remains the sole live store and keeps the current
  serialization fields, revision, non-P14-B callers, and P12 hooks.
- `FiniteSourceProductionKernel`, its immutable request/result/row DTOs, and
  `MarketPriceCalculator` are the narrow public pure-Core assembly surface.
  Unity adapters remain responsible for snapshots, callback admission,
  prepared-owner installation, and notification.

### Unity host/admission candidates

- `Assets/_Project/Scripts/Data/ItemData.cs` and
  `Assets/_Project/Scripts/Data/CityData.cs`: retain authoring types and add
  only the explicit effective-definition conversion needed by this consumer.
- `Assets/_Project/Scripts/MarketRuntime.cs`: retain the full live owner,
  serialized rows, P12 gate/notification, ordinary stock/price APIs, and
  transaction surfaces. Adapt only finite prepared increases to the Core value
  operation, then use the existing install/revision/notification methods.
- `Assets/_Project/Scripts/CityRuntime.cs`: retain construction and current
  prepared daily-step coordination; use the Core operation only for P14-B.
  Preserve P14-C's sorted mixed-source orchestration and existing Unity-side
  finite contribution behavior. This file is a shared
  integration hotspot and must not be concurrently edited by another
  workstream.
- `Assets/_Project/Scripts/P14CMixedSourceProductionPreparation.cs`: retain the
  aggregate prepared commit, source revision/day recheck, accepted-contributor
  count, and single post-install notification unchanged. It does not call the
  CORE-C operation.
- `Assets/_Project/Scripts/FiniteSourceProfileAdmission.cs`: remains Unity
  composition admission. Keep current P12 and P10 rejection ordering.
- Relevant `.meta` files move with source files; retain GUIDs for Unity assets
  and do not add an SDK DLL to Unity.

### Test candidates

- `Assets/_Project/Tests/EditMode/Editor/FiniteSourceProductionTests.cs`:
  preserve the full original case list; move only proven portable cases and
  retain adapter/integration coverage in Unity.
- `Assets/_Project/Tests/EditMode/Editor/P14CMixedSourceProductionTests.cs`:
  retain all nine current cases and verify P14-C's existing Unity-side
  contribution preserves ordering, receipts, atomic installation, and revision
  increments.
- `Assets/_Project/Tests/EditMode/Editor/CityDailyEconomyContinuationTests.cs`
  and `SimulationRuntimeAdmissionTests.cs`: Unity integration/regression only.
- `Assets/_Project/Tests/EditMode/Editor/SettlementStockOwnershipTests.cs`
  and `EconomyTransactionTests.cs`: retain as regressions for the unchanged
  Market live authority; these callers are not migrated to Core.
- `tests/Simulation.Core.Tests/`: explicit Compile links for the portable
  shared source/case file; no recursive legacy `Assets` compile.
- P14-B current-base validation/review records remain historical evidence; add
  current-tree CORE-C evidence separately rather than rewriting them.

Core finite-source calculation, `MarketRuntime` prepared row adaptation, and
the P14-B `CityRuntime` call site are one shared hotspot. Perform source move,
assembly integration, Market-row adapter, P12-hook integration, and validation
in a single serialized implementation window. P14-C remains a regression suite;
its existing code is outside this change.
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
- **P14-C:** already promoted and remains outside CORE-C's implementation
  boundary. Preserve its Unity-side mixed-source/finite-contribution path and
  use its full focused suite as a regression. No shared Core call is claimed;
  any future P14-C migration needs a separate reviewed design.
- **CORE-D:** depends on CORE-C and CORE-B for a shared runtime/Lab composition;
  it is not delivered here.
- No Simulation-External, P18, P19, or P20 dependency for this passive daily
  finite-source operation.

**Implementation readiness:** `WAIT_DEPENDENCY` on promoted/reviewed CORE-A,
plus independent exact-tip technical-design review and implementation handoff.
The architecture makes CORE-C ready for technical design now. This proposal is
not independently reviewed yet and does not claim implementation readiness.

## 8. Exclusions and unresolved technical checks

Excluded: migrating P14-C's multi-source coordinator/feature, broad
`ItemData`/capability extraction, full `MarketRuntime`, `CityRuntime` or
`SimulationRuntime` extraction, CORE-D Lab host, P12 profile
admission/persistence, save migration, P13 history/fork, other P14 scopes, P19
loader/API, and any new gameplay.

The row mapping and numeric behavior are specified above. Before implementation,
the Unity baseline must run the shared bit-vector table against the unchanged
P14-B implementation on Unity 6000.3.9f1. Any mismatch with a recorded
vector requires evidence-backed design correction and fresh independent review
before source movement. The adapter must also prove exact row import/export
mapping, and the P12 gate/commit hooks must be verified against their current
call order. The unchanged P14-C regression suite must retain its existing
multi-increment semantics. If a row cannot be represented one-to-one or
prepared installation cannot preserve the P14-B behavior, stop with the exact
counterexample; do not filter, merge, or broaden behavior.

The next gate is independent technical-design review against the exact
architecture, P12 production, and P14 canonical refs recorded above. A design
PASS makes only the bounded design reviewed; CORE-A dependency and a separate
implementation authorization/readiness check remain before code migration.
