# CORE-D — shared runtime and finite-source Lab host

**Status:** technical design proposal; independent design review pending. This
document does not authorize implementation, alter architecture, extend a save
profile, or promote a Phase.

**Checkpoint:** CORE-D, shared runtime composition + Lab host.
**Core canonical baseline:** `af084c2028528d8588268801daaee14a905f5f5b`.
**Architecture contract:** `codex/architecture/simulation-core-extraction` at
`32358d2a0faf511e098c4f295a5132a4bfb8cfb2`.
**CORE-A/B/C:** promoted on the current Core canonical. CORE-C candidate code
`114e3a7ec9f794b2996305eb1d640a8a58ab1083`, review `c04ec332eef29387463937c7153387d9b8728c75`.
**P14 canonical:** `09fcbe46ffb5a7d80377185692d550310debc110`.

## 1. Accepted behavior and bounded proving profile

CORE-D demonstrates one existing domain path through the same
Unity-independent Core application code and a small .NET Lab host:

1. admit an effective finite-source scenario definition;
2. initialize the selected source and Market state;
3. prepare and commit a daily finite-source operation for a host-admitted
   absolute day; and
4. read the resulting source reserve, source/Market revisions, Market stock,
   and calculated price.

The proving input is the already-promoted P14-B finite-reserve profile: one
City/settlement, one finite source, and one Item. Those counts constrain only
this fixture. They do not define universal City, source, or Item cardinality.
The existing LocationId-to-City-anchor check remains part of Unity profile
composition. The Lab fixture receives an explicit effective LocationId and
the same semantic identities; it does not mint geography or resolve Unity
assets.

The .NET path is an in-memory Lab execution, not an equivalent P12 continuation
host. P14 remains outside `UnityBootstrap-Daily-v1`. The design does not add
population/NPC behavior, trade, payment, transport, arbitrary scenario loading,
file persistence, save/load, cross-build compatibility, P13 history/fork,
intraday scheduling, P19 extension APIs, or a general-purpose host framework.
The existing P12 admission/owner hooks are preserved on the Unity path; this
work does not claim that P14 state is admitted to P12.

## 2. Shared ownership and API boundary

The Core package remains the sole implementation of finite-source validation,
day/idempotency checks, market-row selection, numeric pricing, and prepared
state transitions. CORE-D adds a small application-level scenario boundary
around the promoted CORE-C value kernel; it does not copy those rules into the
Lab executable.

The bounded Core surface is an immutable effective definition, a detached
snapshot, a prepared advance, and a runtime facade. Names below are proposed
for design clarity and may be adjusted during implementation without changing
their semantics:

```csharp
public sealed class FiniteSourceScenarioDefinition { /* effective values only */ }
public sealed class FiniteSourceScenarioSnapshot { /* immutable read view */ }
public sealed class PreparedFiniteSourceAdvance { /* expected revisions + result */ }

public sealed class FiniteSourceScenarioRuntime
{
    public static bool TryCreate(
        FiniteSourceScenarioDefinition definition,
        FiniteSourceScenarioSnapshot initialState,
        out FiniteSourceScenarioRuntime runtime,
        out string failure);

    public bool TryPrepareAdvance(
        FiniteSourceScenarioSnapshot currentSnapshot,
        long admittedAbsoluteDay,
        out PreparedFiniteSourceAdvance prepared,
        out string failure);

    public bool TryApplyPrepared(
        FiniteSourceScenarioSnapshot currentSnapshot,
        PreparedFiniteSourceAdvance prepared,
        out FiniteSourceScenarioSnapshot nextSnapshot,
        out string failure);

    public FiniteSourceScenarioSnapshot ReadSnapshot(
        FiniteSourceScenarioSnapshot currentSnapshot);
}
```

This is an internal Unity/.NET composition API, not a supported public mod or
extension surface. The definition contains the P14-B effective Item values,
stable source/settlement/Market identities, source content revision, daily
limit/reserve, initial Market rows and their expected revision, plus the
LocationId binding used by scenario admission. It contains no ScriptableObject,
asset path, Unity object identity, or scene reference.

The runtime composes `MarketPriceCalculator` and
`FiniteSourceProductionKernel`; it adds no second price or production formula.
The snapshot and prepared object copy ordered rows and carry source/Market
revision expectations. A prepared advance is ephemeral, single-use, and
cannot be restored, serialized, or accepted after either owner revision
changes. `TryApplyPrepared` rechecks both expected revisions and produces one
immutable next snapshot; stale, duplicate-day, overflow, invalid-definition,
revision-exhaustion, or invalid prepared input leaves the supplied snapshot
unchanged. The host then performs one atomic replacement. A successful
zero-output/exhausted result retains exactly the CORE-C/P14-B contract's
day/revision behavior and is pinned by shared tests rather than reinterpreted
by the host.

The runtime facade is a pure transition coordinator, not a mutable owner. It
cannot become a second long-lived store beside `MarketRuntime` or
`FiniteProductionSourceStore`. On Unity, the current live owners remain the
authorities; the adapter supplies a copied snapshot, incorporates the next
snapshot in its existing daily commit, and performs the already-admitted owner
installation. In the Lab host, one in-memory snapshot owner atomically replaces
its current immutable value with the returned next snapshot; it owns no
production or pricing rules. Both hosts call the same Core facade and kernel.

## 3. Host composition and call sequence

### Unity Studio adapter

The existing Unity profile resolver remains responsible for reading
`CityData`/`ItemData`, checking the P8 LocationId anchor, enforcing the P14-B
authored cardinality and rejecting the P14 profile from the selected P12
Daily-v1 composition before identity/owner publication. It converts the
accepted authoring objects to effective values and invokes the Core runtime.

The existing City daily production boundary remains in control of timing and
transaction composition. It asks Core to prepare the P14-B finite-source
transition and embeds that prepared value in its current daily commit. The
outer City/runtime transaction revalidates its expected owner revisions,
passes through the existing Market/P12 mutation-admission hooks, installs the
source and Market state, and then emits the existing notification/receipt.
No Core callback may mutate a Unity owner during preparation. Other Market,
City, and P12 operations retain their current implementations and behavior.

For a profile currently rejected by P12, that rejection remains before the
Core runtime is constructed. For any accepted Unity path that reaches an
existing P12 mutation boundary, the same gate and commit ordering remain
mandatory; CORE-D cannot add a bypass or turn a denied mutation into a Core
commit.

### .NET Lab host

Add a small `tools/Simulation.Lab` executable targeting the already selected
`net8.0` host and current pinned SDK. Its first command is a fixed, human-
readable P14-B scenario fixture; it does not load untrusted files or introduce
a configuration schema. The fixture creates the same effective definition and
initial rows used by the Unity parity test, supplies an ordered sequence of
admitted absolute days, calls the shared Core runtime, and prints only the
resulting snapshot values. It has no Unity dependency, network service,
persistence, random source, wall-clock input, or alternate world-rule code.

The host adapter calls `TryApplyPrepared` against its current snapshot and
replaces that value atomically only on success. If preparation or application
fails, the previous snapshot remains available and byte-for-byte value-equal.
The output includes stable identity, processed day, reserve, revisions, stock
and price, allowing a reviewer to compare Unity and Lab outcomes without
relying on log text as state.

## 4. File ownership and integration sequence

1. Add the bounded Core scenario definition, snapshot/prepared values, and
   runtime facade under `Packages/com.simulation.core/Runtime/Economy/`; add
   explicit SDK Compile links only. Reuse CORE-C DTOs and kernel.
2. Add the .NET Lab project and deterministic fixed scenario under
   `tools/Simulation.Lab/`; update the solution/runner only as required to
   build and execute this project.
3. Adapt only the P14-B Unity creation and finite-source daily production
   path in `FiniteSourceProfileAdmission`, `CityRuntime`,
   `FiniteSourceProductionService`, and the needed `MarketRuntime` boundary.
   `SimulationRuntime`, generic City/Market operations, P12 codecs/owner
   inventory, and P14-C orchestration remain outside the edit unless a
   reviewed necessity is proven.
4. Add Core tests first, then the Lab execution test, then Unity parity and
   admission/transaction regressions. Integrate in this one serialized
   City/Market/runtime hotspot window. Do not concurrently edit those files.

Before implementation, inspect all P14-B Market readers/mutators that are
reachable in this proving profile. The design must demonstrate one live source
of truth throughout prepare, commit, and readback. If existing public Market
operations can mutate a selected P14-B profile outside the prepared transaction
and cannot safely remain a host adapter, stop and record the exact call sites;
do not leave a shadow Core store or silently narrow existing supported
behavior.

## 5. Validation and closure

Core tests cover effective-definition admission; exact identity/cardinality;
ordered Market rows; initialization/readback; positive contribution; exhausted
and zero-output behavior; duplicate/older days; stale source and Market
revisions; overflow; invalid content; revision exhaustion; single-use prepared
advances; and atomic no-change on every rejection. Compare exact snapshots and
rejection reasons, not only aggregate totals.

The Lab integration test executes the fixed scenario outside Unity and records
its input identity plus exact snapshots before and after each day. A parity
test uses the same definition, initial rows, day sequence, and shared vectors
against the Unity adapter and asserts equality for status, source/Market
identities, day, reserve, revisions, row order/values and price bits.

Unity validation includes focused P14-B production, City daily continuation,
Market stock ownership/transactions, profile and Location-anchor admission,
P12 mutation-admission rejection, and P14-C regression suites. Run ALL
EditMode and official Smoke. If the final integration changes additional daily
loop behavior, run its required LongRun gate. The P12 `UnityBootstrap-Daily-v1`
owner/cardinality inventory remains unchanged and must still demonstrate that
the P14 scenario is rejected before identity or owner publication.

Run cold and warm `CoreValidation` for the new explicit source/test inventory,
validate the Lab project and deterministic output, and run `git diff --check`.
Bind all results to the exact source tree. Obtain independent exact-tip review
of the full Core/Unity/Lab delta and its boundary semantics before canonical
promotion. Do not rerun CORE-C suites merely for docs-only changes; rerun the
affected gates if any executable source/test/harness input changes.

## 6. Dependencies, status and exclusions

- CORE-A is promoted and supplies shared calendar primitives.
- CORE-B is promoted and supplies the accepted deterministic/temporal closure;
  this selected P14-B operation does not introduce random draws or P18 work.
- CORE-C is promoted at Core canonical `af084c2028528d8588268801daaee14a905f5f5b`
  and supplies the selected finite-source kernel/Market price implementation.
- P14-B is promoted at P14 canonical `09fcbe46ffb5a7d80377185692d550310debc110`.
- P12 Daily-v1 is closed for its accepted Unity current-host profile. It is
  not an input profile for the P14-B Lab scenario. No P12 continuation claim
  follows from CORE-D.
- CORE-E remains a set of individually reviewed domain closures. CORE-D does
  not extract population/NPC/political/military/spatial/Knowledge semantics.
- CORE-F requires its own exact Daily-v1 owner/definition/runtime closure and
  host design. CORE-D does not make CORE-F ready by itself.
- CORE-X and P19 public extension/loader work remain deferred.

Implementation readiness follows only after independent review of this exact
technical boundary. If the review cannot establish that the selected runtime
can preserve one-owner authority without changing P14-B or P12 admission
semantics, CORE-D remains `WAIT_DEPENDENCY` and the review must name the
smallest unresolved architectural contract. No gameplay or compatibility
decision is implied here.
