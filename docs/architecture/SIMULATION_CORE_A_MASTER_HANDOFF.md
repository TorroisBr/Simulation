# CORE-A — shared calendar extraction: technical design and Master handoff

**Checkpoint:** CORE-A Calendar Shared Assembly (unnumbered infrastructure).
**Prepared:** 2026-10-13. **Production design base:**
`4cc31073f0ec6ba5c718084e4cebd6e914931d5d` on `codex/phase12/canonical`.
**Architecture base:** `e940714bcc05c0d790e0cb9a7f614ee6b493e21a`.
**Readiness:** READY_FOR_IMPLEMENTATION only after the independent PASS in
[review](SIMULATION_CORE_EXTRACTION_REVIEW.md) and this architecture promotion.
**Owner:** General Orchestrator; independent implementation worker/reviewer.
**Implementation candidate branch:** `codex/core/CORE-A-calendar`.
**Intended infrastructure canonical:** `codex/core/canonical`, initialized by Master
from the current reviewed integrated production base at dispatch (initial evidence
base `4cc31073...`). Do not move numbered-phase canonical refs as a side effect.
Future consumers integrate this capability by explicit reviewed delta; branch
existence alone is not delivery. Owning implementation record: `docs/CORE_STATE.md`
(created by implementation, not a fictional Phase State).
**Architecture:** [Core extraction](SIMULATION_CORE_EXTRACTION.md), §§2/12/85A
of the constitution; no production implementation delivered in this design.

## 1. Exact objective, scope and exclusions

Move the existing calendar/age capability into one engine-free shared source
assembly, build/run its same rules via .NET, retain all Unity consumers and
prove parity. Calendar/date/age participate in actual authoritative population
and temporal semantics. This is a real deterministic domain slice, **not** a
full SimulationRuntime, world advance, P18 scheduler or P12 .NET host.

Exactly these existing types, global names and behavior move unchanged:

| Source at pinned base | Destination in Core Runtime/Calendar |
|---|---|
| `Assets/_Project/Scripts/Data/CalendarDefinition.cs` (204 lines) | `CalendarDefinition.cs` + move its existing `.meta` GUID |
| `Assets/_Project/Scripts/Calendar/SimulationCalendar.cs` (238 lines) | `SimulationCalendar.cs`, including the entire `CalendarAgeCalculator` class + move `.meta` |
| Entire `SimulationDate` struct at `Assets/_Project/Scripts/SimulationTime.cs:134–222` | New `SimulationDate.cs` with `using System;` and new `.meta` |

Line counts are navigation evidence; current source/blob identities govern.
The struct includes **all** fields, constructor, equality, unchecked GetHashCode,
comparison, operators and ToString, not merely its initial properties. Leave
SimulationTime and its existing meta/guard/advance dispatcher at the legacy path.
No calendar/name/namespace/API/field/exception/fallback/hash/order redesign.
CalendarDefinition remains Serializable with its existing mutable authoring
fields; SimulationCalendar retains validation and its own effective snapshot.

Excluded: RNG extraction, numeric replacement, world/temporal mutation owner
moves, SimulationRuntime changes, broad definition adapters, P12 codecs/profile
expansion, save conversion, P13, P19 loader/public extension API, Lab world
scenario framework, gameplay, repo relocation or worktree/cache cleanup.
No engine stubs, copied calendar implementation or shim rules in the CLI.

## 2. Fixed assembly/build boundary

Add embedded `Packages/com.simulation.core/package.json`, private version 0.1.0,
name `com.simulation.core`, Unity minimum `6000.3`; no package dependencies.
Runtime `Simulation.Core.asmdef`:

```json
{
  "name": "Simulation.Core",
  "autoReferenced": true,
  "noEngineReferences": true,
  "references": [],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false
}
```

No rootNamespace rename, engine reference, Assembly-CSharp reference, public Store
surface or blanket InternalsVisibleTo. Existing predefined Unity assemblies
consume the Core assembly automatically. Unity compiles package sources; do not
import the SDK-generated DLL. Move the two original source files with their
original metadata; remove original source paths so types occur exactly once.
Keep SimulationConfigData's asset/script GUID and inline `calendar` field intact.

Add `src/Simulation.Core/Simulation.Core.csproj` with TargetFramework
`netstandard2.1`, AssemblyName `Simulation.Core`, LangVersion `9.0`,
EnableDefaultCompileItems `false`, Nullable/ImplicitUsings disabled, deterministic
compilation, explicit three Compile links into `../../Packages/com.simulation.core/Runtime/Calendar/`.
No NuGet runtime dependencies. Do not glob legacy Assets. Core source must compile
with only BCL references; no `UNITY_*` conditional alternative domain logic.
Assembly build determinism does not mean identical MVIDs between Unity and SDK.

Root `global.json`: SDK `9.0.315`, `rollForward: disable`, `allowPrerelease: false`.
SDK is present on the inspected host; tests/demo use net8.0 with installed CLR
8.0.28. Pin/record actual runtime selected; this host is a first proof, not a
new permanent deployment support policy. No SDK/Unity upgrade during this slice.
Ignore SDK bin/obj, validation output/private evidence and generated IDE files;
keep them outside Unity import roots. No unrelated .gitignore cleanup.

`tests/Simulation.Core.Tests/Simulation.Core.Tests.csproj` targets net8.0,
IsTestProject true, explicit shared test source Compile link, ProjectReference to
Core, default Compile disabled, Nullable/ImplicitUsings disabled, LangVersion 9.0.
Pin NUnit **3.14.0**, NUnit3TestAdapter **4.6.0**, Microsoft.NET.Test.Sdk **17.12.0**;
lock transitive restores for test and demo projects (`RestorePackagesWithLockFile`, checked-in lockfiles,
then `dotnet restore --locked-mode`). These versions expose the existing NUnit 3
assertions; Unity keeps its own Test Framework dependency untouched.
Package identities verified against the primary registry:
[NUnit](https://www.nuget.org/packages/NUnit/3.14.0),
[adapter](https://www.nuget.org/packages/NUnit3TestAdapter/4.6.0),
[test SDK](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/17.12.0).
If the identified toolchain cannot restore/build, diagnose the actual issue and
review a bounded toolchain correction before claiming readiness; no floating upgrade.

## 3. Test-source separation and evidence conservation

Current `Assets/_Project/Tests/EditMode/Editor/Calendar/CalendarFoundationTests.cs`
has **32** `[Test]` cases at the base. Move **28 portable cases** and their pure
helpers to a new `CalendarCoreTests.cs` in the **same Editor/Calendar directory**.
This file uses only System/Core/NUnit and is compiled unchanged by both the
predefined Unity Editor assembly and the SDK test project (explicit Compile link).
Do not add a test asmdef that cannot reference still-legacy consumers.

Retain these four host-dependent cases in the original Unity-only class:

- AdvanceOneAbsoluteDayAdvancesCalendarDate;
- ConversionDoesNotMutateSimulationTime;
- AgeCalculationDoesNotConsumeRng;
- AgeCalculationDoesNotAdvanceTime.

Retain each helper where needed; sharing fixture helpers is fine, copying domain
logic is not. Keep an explicit old full-name -> new full-name map for the 28
moves and four retained cases in the validation record; no test silently dropped.
The host RNG preservation test still runs in Unity, not under a fake engine.

Add focused pure cases for strict definition validation (including null, custom
lengths, overflow), negative absolute day, inverse overflow, effective snapshot
isolation for custom month lists, SemanticIdentity/SemanticVersion, SimulationDate
all-field equality/hash/compare/operator/ToString behavior, conventional and
nonuniform birthday boundaries, long.MaxValue round-trip and existing legacy
CreateValidatedOrDefault behavior. Keep expected values derived from canonical
behavior/explicit domain examples, not a copied calendar algorithm in tests.
No NUnit 4-only features or engine fixtures in the shared file.

## 4. Deterministic parity protocol and Studio validation

Before moving production sources, add bounded table-driven parity tests/data and
run them on the unchanged canonical Unity calendar. Record this source/input
identity and complete result evidence; preserve the baseline expected table.
After extraction, run that identical shared case table on both Unity and .NET,
including configurations (1/1/1), conventional 12/1/30, unequal [10,20,5],
long-day/overflow/negative/invalid inputs, birthday before/on/after and value
semantics. Compare exact date fields, completed age, semantic identities,
ordering/hash values and exception types. Record cultures en-US and pt-BR and
supported Windows/x64 Mono/CoreCLR facts. No epsilon or diagnostic-string
normalization for authoritative values. CLI uses this same Core API.

The first SDK Compile list is exactly the three Core files. Build dependency
inspection must show no UnityEngine, UnityEditor, Assembly-CSharp or WorldExchange
reference in Core. Verify Unity compiles one Simulation.Core and finds no duplicate
source/type or imported SDK DLL. Verify old consumers still bind to these types.

Authoring gate: in Editor tests create/save/reimport a temporary SimulationConfigData
asset with explicit uniform and unequal-month calendars, round-trip fields and
construct the same effective calendar; check canonical selected profile assets
retain their serialized values/references. Use owned temporary fixtures; do not
rewrite live Scenes or user's ProjectSettings. Existing sources use inline
serialization, not SerializeReference assembly-qualified type identities. If
cross-assembly serialization actually fails, stop and prepare bounded correction;
do not add Unity MovedFrom dependencies to the pure Core or silently default data.

P12 gate: run the exact selected Daily-v1 admission/completed-boundary,
compatibility, capture/restoration and persisted continuation suites. Show the
new Unity build durably captures at a valid completed boundary, detached-loads,
restores identity/state and continues with parity. Reuse actual production
coordinator/owners/capture, no snapshot overrides or manufactured tokens.
Record new descriptor assembly/MVID/build facts. A pre-extraction compatibility
descriptor/known old-build fixture must be rejected as incompatible by the
existing path, leaving slot/publication unchanged. A controlled descriptor
fixture is acceptable for this negative test, labeled as such; do not claim an
actual old-save test if only descriptor injection ran. Keep any genuine prior
save with its known old build; no old file may be overwritten in the negative test.
This is same-build continuation regression, not cross-build compatibility.
## 5. Bounded .NET runner/evidence contract

Add a small `Tools/CoreValidation/Invoke-CoreValidation.ps1` plus synthetic self-tests
and README; no change to the Unity harness/packer. It invokes explicit Core
restore/build/test project paths with the pinned SDK, uses fresh unique results
under an ignored root outside Assets/Packages, and requests `--logger trx`.
Normal invocations:

```powershell
dotnet restore tests/Simulation.Core.Tests/Simulation.Core.Tests.csproj --locked-mode
dotnet test tests/Simulation.Core.Tests/Simulation.Core.Tests.csproj --no-restore --logger trx
dotnet restore tools/Simulation.Core.CalendarDemo/Simulation.Core.CalendarDemo.csproj --locked-mode
dotnet run --project tools/Simulation.Core.CalendarDemo/Simulation.Core.CalendarDemo.csproj --no-restore
```

The wrapper also captures before/after content digests of global.json, every
participating project/props/targets/lock/tool/runsettings file, all explicit Core
and linked test/demo sources/metadata, and any actual external/generated inputs.
Reject unstable or undeclared influencing inputs. Record OS/architecture, SDK,
CLR, package versions, exact portable argument vector and discovered/expected case
manifest. No `dotnet test` empty-discovery or process exit alone can pass.

Required result verification: parse TRX with DTD/entity resolution disabled; bind
TestRun identity, Results and TestDefinitions/TestEntries by their run-local IDs;
retain complete full test names/parameter identities, outcomes/reasons/errors,
counts and available timings. Reject missing/truncated/duplicate/unknown result
records, empty selection, mismatched IDs/counters or missing expected cases.
Gate requires all expected selected cases executed/Passed, zero failed/error/
timeout/aborted/skipped/not-executed/inconclusive. Record operational failures
without converting them into pass; no automatic retry of valid test failures.
Preserve a complete original TRX bundle and manifest, independently inspect actual
content/privacy and outgoing Git objects before publication; do not strip or
hand-edit it to satisfy the unsupported Unity packer. Protected originals/logs
follow evidence policy. Self-tests cover a passing populated report, failed and
skipped cases, empty discovery, malformed/missing report and count/ID/tamper or
input-instability rejection. No generalized normalized report/schema framework.

Unity runs use the current production evidence wrapper/packager unchanged. Add a
sidecar full-input manifest for SDK/project/linked sources outside its captured
roots; verify equality across the final two-host gate set. Do not claim existing
Unity package hashes cover newly added .NET inputs. If a report format or privacy
rule cannot safely retain required evidence, leave that evidence gate unresolved;
no manifest/hash-only substitute. One final cumulative package per host is enough,
with earlier unchanged-source parity baseline evidence referenced, not copied.

## 6. Validation and closure criteria

Mandatory before CORE-A implementation promotion:

1. Diff-check; exact source move/API/symbol uniqueness review and no engine
   assembly dependency in Core SDK/Unity builds. Verify the new Core MVID appears
   in the Unity P12 project-assembly descriptor by the existing discovery path.
   Do not hardcode it or weaken factory checks if discovery fails.
2. .NET locked restore/build; complete pure calendar cases (28 moved + reviewed
   additions) pass with no skips and exact case mapping; bounded runner self-tests.
3. Unity focused CalendarCoreTests plus four retained CalendarFoundationTests and
   authoring round-trip tests pass; exact parity table succeeds on both hosts.
4. Current selected-profile P12 compatibility, admission/completed-boundary,
   capture/restoration and persisted continuation/failure tests pass. Include
   old-descriptor negative rejection/new-build same-host continuation evidence.
5. Full Unity ALL EditMode and official Smoke pass on the final integrated input
   tree, zero failures/skips/inconclusive. Existing final 3060 and 5 are historical
   anchors, not fixed future totals; account for every moved/added case.
6. No daily-loop/long-horizon rule changes are authorized. If the actual diff
   changes causal behavior, stop scope growth, review it and run corresponding
   LongRun gates; do not use the mechanical-extraction label to omit them.
7. Human demonstration `REQUIRED_FOR_CHECKPOINT`: tiny .NET CalendarDemo invokes
   the same Core with [10,20,5], reports absolute days 0/10/30/35, date and
   completed-age before/on a birthday, semantic version and a rejected negative
   input. Show identical expected results in Unity tests. Explicitly label this
   a calendar capability demo, not a full world/Lab/AdvanceDay demonstration.
8. Record cold/warm .NET elapsed time, local bin/obj/results bytes and absence of
   Library in its .NET-only worktree. No speculative claim of project-wide speedup.
9. Complete retrievable per-case evidence, exact input/toolchain identities,
   independent exact-tip implementation review and refreshed safe promotion
   preflight; update one owning unnumbered CORE State/record after implementation.

Test execution/passing is **not** claimed by the architectural design. A design
PASS makes the contract executable; closure requires all nine actual gates.

## 7. Ownership, integration, reconstruction and rollback

Master must create/reuse an isolated implementation worktree from current
production canonical, classify any advancement since `4cc31073...`, load the
promoted architecture documents by bounded delta, and claim these hotspots:
calendar/date sources/metas, new Core package/SDK/test/demo/runner inputs, calendar
fixtures and relevant ignore rules. No direct SimulationRuntime/AdvanceDay,
spatial, owner stores, P12 codec/schema/profile or active ProjectSettings ownership.
Serial integration is still required for assembly compilation/validation and
P12 consumer alignment. Other numbered-phase work continues independently;
source moves must be reintegrated against actual consumers, not stale copies.
Never merge this architecture branch's older executable tree into P12.

No new mutable world owner or causal input is introduced. Reconstruction-sensitive
inputs remain effective month lengths/days-per-week, calendar semantic identity/
version and original absolute days; all existing checked overflow, age and date
value semantics stay unchanged. Source location/assembly is a changed build
identity, not new world identity or an instruction to mint IDs or replay history.
No P12 intraday/P20/Knowledge/profile widening and no save-version migration.

Use additive commits, retain old source-path mapping and known validated old
build/content/save fixture, then integrate/review/gate in an owned window. On
failure keep the candidate recoverable; fix within this contract or return to
bounded design for a real unanticipated boundary. Roll back by an additive revert
and matching old build/save, followed by regressions. No destructive reset,
force-push, shared Library strategy or edits to active implementation worktrees.

**Master next action after architecture promotion:** dispatch CORE-A implementation
under this exact contract, then independent implementation review and existing
canonical promotion workflow. CORE-B/C design may proceed read-only in parallel;
no CORE-D/F implementation or P13 readiness is authorized by CORE-A.
No unresolved product/architecture choice remains for this bounded slice.
