# CORE-F Daily-v1 shared runtime and host construction design

**Status:** bounded dependent design; no production implementation or source movement. **Production source:** `0094febf7f97333d0653eaf9688fe1d24d73327f` (`codex/core/canonical`). **Architecture/design checkout:** `4b69e43516b3fc548611ce704ef45af9e54e3d0e`; its working copy is an architecture branch, so all executable claims below were checked against the named production commit with `git show`. The older `CORE_F_DAILY_V1_HOST_TECHNICAL_DESIGN.md` and runtime source review are historical inputs, not current source authority.

## Current source applicability

The [current owner-closure assessment](CORE_F_CURRENT_OWNER_CLOSURE.md) supersedes the historical next-task facts below at promoted Core b2c2c0e. Demographic owners are PROMOTED; institution/claim owner preparation is separately bounded. The application/host API and acceptance contract remain in force. Whole shared runtime/restore implementation stays WAIT_DEPENDENCY on exact selected owner/definition/member closure, not on missing token/quiescence architecture. No .NET continuation result follows from Unity P12 evidence.

## 1. Decision

Build one engine-free `Simulation.Core` application composition which owns the selected Daily-v1 graph, runtime, admission protocol, capture coordinator, image codec/census, detached restoration, and publication-ready session value. Unity and .NET are thin host adapters around that same source. Do not implement a second .NET runtime or let either host provide authoritative owner values, epochs, tokens, or restore semantics.

The host boundary is a typed request/result surface over *effective inputs* and narrow host ports. The host prepares the selected content and truthful compatibility identity; Core validates those inputs and composes the graph. Core owns ordered bootstrap stages and their deterministic semantics. The existing stage hook receives only the completed stage ID after Core runs its mutating handler. Its exception preserves the current pre-publication failure behavior. It cannot supply composition values. Storage receives an explicit root and atomic file operations. Unity supplies `Application.persistentDataPath`; the .NET executable supplies an explicit fixture/user path. This proves same-host/same-build continuation only; no cross-host save promise.

No new world truth, second effective-definition graph, restore token, public plugin API, broad `InternalsVisibleTo`, Unity stubs, or direct store access is introduced.

## 2. Source facts at 0094

- `SimulationBootstrapComposition` has an `internal` constructor taking the manifest, time/calendar, spatial network, event/history/directive/decision stores and recorders, economy transactions, chronicle service/formatter, travel parties/system, `SimulationRuntime`, identity registry/allocator, sites and expeditions/system. It assembles passive census providers from those *same instances*. This is already the correct conceptual aggregate, but its implementation is Unity source and it exposes many concrete owner types.
- `TesteSimulacao : MonoBehaviour` currently constructs the graph in `InitializeSimulation`, runs stages/callbacks, validates the candidate, and publishes the composition/session. `CreateNpcChronicleFormatter` binds lookups to active identity/spatial/city/site owners. The restore coordinator also calls this formatter factory. The display formatter must be built on detached candidate owners before publication; its semantic lookup behavior and ambiguity/missing-ID behavior must remain byte/output equivalent.
- `SimulationRuntimeAdmissionContext` captures a specific `Thread` object and managed thread ID, permits only the selected `UnityBootstrapDailyV1` composition identity, and creates a private restored-continuation context. Preserve the profile enum/string in every manifest and snapshot; host kind is independently represented in compatibility descriptor v2. Preserve the exact object+ID owner-thread test.
- `DailyCaptureEligibilityToken` binds runtime object identity, admission-context identity, effective configuration identity, calendar identity, composition profile, world ID, absolute day, completed core sequence, boundary provenance, mutation epoch, and owner snapshots. It is private/internal issuance evidence, never serialized or transferred to another graph.
- The runtime uses a single advance lease (`TryAcquireAdvanceLease`/`AdvanceLease`) and mutation guard. On positive completed outer advance, the current completed token is issued; failed, partial, or faulted advance cannot create eligibility. Restore must not advance time or issue a normal advance token.
- `P12GDailyV1RestoreCoordinator.TryCreateRestoredSessionFromImage` takes the validated image, expected compatibility, `SimulationConfigData`, effective config, calendar, bootstrap admission context, and returns a private `SimulationActiveSession` plus failure/diagnostic. It validates profile/thread/host compatibility, current 89-family census, typed decoder, privately stages C roots then D/E/F owner groups, composes candidate, validates identity/genealogy/political/spatial/travel links and sentinels, captures target owner vector, binds mutation guard, recaptures vector, admits the restored boundary, and verifies healthy owner-thread boundary. It does not publish.
- `TesteSimulacao.TryLoadDailyV1ContinuationAtPath` holds `activeSessionGate`, snapshots expected active session, requires idle healthy completed boundary and owner thread, reads+validates the slot, builds the private candidate, then calls the one publication gate. `TryPublishRestoredSession` validates the expected session/current session reference, no active operations, compatible config/profile, owner thread, healthy candidate and valid restored admission; it uses one `Interlocked.Exchange`. Failure retains prior active reference. Do not move this facade lock/Unity session object into Core without proving that its operations/callbacks are host-neutral.
- `P12AContinuationStore` already has explicit-root methods and atomic-file slot operations. It also has convenience `*Current` methods that read `Application.persistentDataPath`. Keep only explicit-root interface in Core. The existing slot name `continuations/unity-bootstrap-daily-v1.simcont` and compatibility v1 are Unity-coded; the accepted shared design needs typed descriptor v2/host kind while retaining v1 Unity behavior. The .NET slot must use the same codec/store algorithm, explicit root, and truthful .NET identity; it must not claim old Unity slot compatibility.
- `P12AContinuationStore.TrySave` validates bytes by decoding against the manifest/compatibility before atomic write and rereads/verifies through `AtomicFileSlot.Write`; `TryLoad` reads bounded bytes and decodes before returning image. Invalid writes must leave prior slot bytes intact. Storage failure and codec/admission failures remain distinct result categories.
- The fixed selected census is not 89 total rows. It is 61 + 22*N + U + P + 4*C, with positive N/C, matching per-NPC/per-City key sets and U keys referring to NPC keys. `Required` means the required owner can legitimately have zero records; `ExplicitlyEmpty` means zero records. AliasGroup encodes reference identity. Source of truth is `P12ADailyV1Census.TryCreate` and the exact section inventory; preserve the 49 portable CORE-B regression identities separately from the finite P12-G graph/rejection suite.

## 3. Shared application boundary

The final surface is specified in §11 from exact 0094 declarations. It replaces this earlier draft API. The fixed shared API is DailyV1Application.TryStart(DailyV1EffectiveDefinitions, DailyV1ApplicationHost), then TryAdvanceDay, TrySaveContinuation, TryLoadContinuation, and the listed operation-gated read/command methods. Its host constructor takes the explicit root, atomic file operations, existing stage hook, committed log sink, and exclusive Unity-v1 or .NET-v2 identity. No loader, owner provider, resolver, serializer, or mutable store is injected.

## 4. Capture and restore ownership sequence

**Capture:** Core calls current completed-token validation on the same live runtime. Under the single operation gate/advance lease, it verifies owner thread, healthy runtime, selected admission, token runtime/context/config/calendar/world identity, CompletedAdvance provenance, day/sequence and current mutation epoch. It consumes the token's complete owner snapshots and recaptures/compares the current owner vector as existing P12 says. Compose immutable `P12AContinuationImage`; run current census, alias, ordering, codec and size checks; encode using shared codec. Release operation gate. Save uses explicit-root atomic slot API and preserves old slot on any failed validation/write/reread. A capture attempt/staging attempt cannot create a new successful completed boundary.

**Restore:** while holding the app session gate, require the expected active session healthy, idle, on its owner thread and at a current completed Daily-v1 boundary. Load/strict-parse/check compatibility before any target allocations. Use a new detached graph: stage causal roots C, owner groups D/E/F and all Required / ExplicitlyEmpty / AliasGroup sections. No genesis, world ID allocation, RNG draw, gameplay step, event append, operation receipt, or publication against the old graph. Validate all cross-owner bindings and exact 89-family vector. Bind target mutation guard, recapture and match target vector+epoch after binding, then admit the header's restored `WorldId`, day, sequence, owner vector and epoch. Verify private candidate healthy and `HasValidRestoredDailyBoundaryAdmission`; preserve the restored-boundary provenance distinction. Only then revalidate active-session generation/expected ref and atomically exchange once. On any fail, discard private graph, return failure+diagnostic, keep old session and slot unchanged. Successful continuation performs ordinary next-day operation from restored T; only its successful outer completion issues the next completed token.

The existing restore path currently has both in-memory source-session staging and persisted-image restore overloads. Keep these as two internal input adapters to the same detached `BuildCandidate`/validation/admission core; do not confuse source-token capture with persisted bytes. Keep all test-only override delegates internal and exclude from host contract.

## 5. Exact code/source partition and assembly cuts

**Core authoritative source:** promoted definitions/effective-value contracts, genesis composition/fingerprints, runtime/domain authorities and all selected owner writers, revisions/epochs and mutation guard, operation/owner-thread admission, sole advance lease, token issuance/validation, `SimulationBootstrapComposition` semantic graph, P12 owner-family manifest/census/codecs/composer/decoder, C/D/E/F staged owner packages and hydrators, P12-G detached graph checks, restored admission, atomic slot algorithm + explicit-root store, typed compatibility descriptor v2, host-neutral chronicle formatter and selected formatter model dependencies. Existing selected 89-family semantic authorities must compile exactly once from these shared bytes in both hosts.

**Unity adapter:** `TesteSimulacao : MonoBehaviour`; ScriptableObject types and authoring resolution; `DailyV1DefinitionAdapter`; default mapping/Unity profile conversion; Unity startup thread capture; `Application.persistentDataPath`; Unity descriptor/host fingerprint capture and v1 adapter; logger/debug/file presentation; Unity facade and existing public UI methods; Unity lifecycle, inspector fields, scene and assets. Adapter owns no state mutation that bypasses Core runtime protocols.

**.NET host:** fixed .NET 8 console/test fixture host; explicit `DailyV1EffectiveDefinitions` fixture and genesis inputs; fixed truthful `DailyV1HostIdentity` manifest; explicit temporary or user-selected continuation root; narrow log/stage callbacks; CLI result/report. No Unity dependencies, runtime clone, engine stubs, local owner implementations, or direct owner mutation.

**Still source-closure gated:** selected owner runtime file list by transitive SCC and conditional path; `SimulationTime` is specifically retained as Unity-host candidate in predecessor design because callback/internal guard binding is not cut. Move only after a reviewed bridge uses exact semantics; do not blanket-expand internal visibility. The `TesteSimulacao` formatter call edge is concrete (`P12G...:1166` in predecessor baseline); extract only formatter + complete model closure after the current line is confirmed at 0094 and lookups are callback/value-only. `SimulationActiveSession`, operation counters, lock and report/logger state stay out until member-by-member proof shows no Unity/config/debug/environment edge. Keep exception/rejection and excluded P10/P14/P18/P19/P20 edges compiled or rejected according to current contract; empty startup collections do not erase those paths.

Use one Core asmdef with `noEngineReferences` and explicit `src/Simulation.Core/Simulation.Core.csproj` Compile items linked to the same production paths. Every selected source appears once in each compilation, preserves Unity `.meta` GUIDs, has no duplicate type/adapters, and has no `UnityEngine` reference in Core. New host identity needs new honest assembly MVIDs; do not attempt old binary identity.

## 6. Build anchors and fixture

The .NET 8 host project references `Simulation.Core` project directly. Pin SDK/runtime version through `global.json`/runtime config and record `dotnet --info`, RID, OS, architecture, configuration, compiler, source SHA/tree and all transitive production compile inputs. Assembly anchor manifest lists each selected Core and host assembly by logical role, exact path-relative project identity, assembly name, MVID at runtime, informational version and SHA-256 of bytes. Never infer anchors by scanning install directories or reference assemblies. Host compatibility identity records a typed `.NET` host kind, selected runtime/OS/architecture fields and the existing v2 content/provider/calendar/P8/P9 identities, numeric contract and source/build identity; Unity-specific fields remain Unity-only. Freeze and review compatibility field semantics and comparison before implementation.

Create one checked-in fixed-input test fixture (or a test assembly resource with input manifest) from the promoted authoring root's *effective* values, not raw YAML alone. It must bind ordered Cities/NPCs/actions/status/jobs/items, exact calendar/config/module flags, genesis geography and content references, effective defaults, definition identity/revision and ordered P9 lineage/fingerprints. Unity extraction compares every value/ordering/fingerprint to this fixture. The .NET host reads fixture into the promoted typed values only. No generated IDs, random seed or fixture-derived alternate profile.

Build anchors are evidence, not save truth. Same-host means matching host kind/build/content and selected compatibility identity. Do not assert Unity can read .NET save or that .NET can read Unity v1 bytes.

## 7. Acceptance test package

Preserve the exact 49 portable CORE-B identities listed in `docs/validation/COREB_DeterministicTemporal/TEST_CASE_IDENTITY_MAP.md` in the full CoreValidation cold and warm runs; their domain scope is RNG/root-snapshot/logical-timeline, not Daily-v1 host behavior. Add separate Daily-v1 host case identities additively. Combine those with the 89-family writer/capture/target crosswalk and current finite P12-G suite; none substitutes for another. Bind complete SDK TRX reports and native result details under the validation evidence policy.

For the fixed Daily-v1 fixture and all selected operations, compare .NET and Unity values/order/roots/compatibility and all 89 family definitions, with dynamic key expansion and alias object identity. At completed boundaries `T`, `T+1`, `T+2`, run paired branches:

1. uninterrupted reference branch;
2. save at T, fresh-process detached load, then ordinary advance to T+1 and T+2;
3. repeat round-trip at T+1 and T+2 (same-build host), including repeated save/load cycles;
4. control branch with same host/build/fixture and identical ordered inputs.

At each boundary compare every section key, schema/version/role, row identity/order/cardinality/values, owner aliases, revisions/epochs, all reconstruction-sensitive roots (WorldId, manifest, RNG, allocators and sequences, owner role/alias/revision, pending commitments, receipts/already-applied effects), core sequence/day and exact encoded bytes where operation and format require equality. Compare future operation results and causal event/decision sequence, not just state aggregates. Run the 49 preserved CORE-B identities in cold and warm full CoreValidation. Independently run current P12 owner capture obligations and the finite restore suite, including these classes:

- unsupported/missing/incompatible host descriptor, bad framing/limits/checksum/manifest/codec, invalid owner schema/key/order/alias/cardinality, each Required/ExplicitlyEmpty contract, dynamic NPC/City/U/P shape;
- stale source token, wrong runtime/context/config/calendar/world, wrong thread, active operation, held lease, mutation after token/epoch change, no completed boundary, partial/faulted advance, second/nested/failed advance;
- detached root and D/E/F stage failure at every declared owner-stage seam; no genesis/RNG/gameplay call; failures discard candidate and preserve active object/reference and serialized slot bytes;
- invalid Person/NPC/genealogy, political holder/revisions, travel party/allocator, spatial, sentinel, exact-zero Expedition and P8 target bindings; duplicate/dangling/cross-owner refs; false-return and throw cases; no observer can mutate during staging;
- candidate owner-vector initial capture, guard bind, post-bind recapture mismatch, epoch change, admission rejection, unhealthy candidate, wrong owner thread, stale expected source generation or active operation at commit; old active ref remains;
- successful restored admission starts exactly at saved day/sequence/root and does not issue normal advance token until next successful outer advance; failed/partial/faulted next advance remains ineligible;
- atomic slot failure points preserve prior valid bytes; rejected save/restore cannot overwrite; storage exceptions classify correctly; formatter callback outputs and missing/ambiguous lookup behavior remain parity-equivalent.

For each positive T/T+1/T+2 branch assert full family count from formula per realized keys, equality and alias identity, not just total section rows. Add post-population cases for each owner family writer/capture/hydrator and its epoch path; baseline empty values alone do not cover reachable Daily-v1 state. The exact evidence authorities are `docs/validation/CORE_F_OwnerClosure/CASE_MAP.csv`, `OPERATION_CASE_MAP.csv`, `OWNER_WRITER_MATRIX.csv`, `OWNER_CAPTURE_TARGET_CROSSWALK.csv`, and `docs/validation/P12GPoliticalBindingClosure/CLOSURE_MATRIX.md`, plus the P12-G validation links in §12. The 49 portable CORE-B regression identities are preserved separately and do not enumerate host/restore obligations. Map each P12-G obligation to its actual new host test identity before implementation validation.

Validation: SDK build + cold/warm .NET suite from same source bytes; Unity source/asmdef import and focused P9/P12 + full EditMode/official Smoke and any required LongRun if daily order changes. Capture real original NUnit/TRX and full toolchain/input manifests; independently review exact candidate source tip and parity reports. Docs-only design itself needs no Unity tests.

## 8. Readiness gates and package DAG

```text
Values (promoted) ─┐
Owners 89 families + writer/epoch/capture/hydration closure (promoted evidence) ─┤
Persistence codec/store/descriptor v2 (promoted) ─┤
Shared payload codec + numeric Unity oracle/port (promoted source in 0094) ──────┤
                                                                                ▼
              selected source-closure cut + constructor/accessibility ledger
                                                                                ▼
        shared DailyV1Application + one Core admission/capture/restore owner
                                                                                ▼
 Unity adapter ─┐                        fixed .NET host/fixture + anchors
                └──── same Core bytes ────────────────────────┬───────────────┘
                                                              ▼
                 49 preserved CoreValidation identities + complete owner/operation/P12-G matrices + T/T+1/T+2 parity
```

Promoted implementation inputs confirmed by current task: A-E and selected Values, Owners, Persistence, shared P18 codec/numeric source and Unity provider switch already exist at 0094; do not recreate them. Their implementation does not establish this host/application composition or complete source cut. Remaining work classifications:

- **Code missing:** Core-owned full application composition and thread-context construction that preserves the existing UnityBootstrap-Daily-v1 profile identity; same authoritative runtime/session operation gate and one publication path; detached persisted-image restoration wired into .NET host; fixed .NET host and effective-input fixture; truthful v2 .NET identity integration; Core-only formatter construction and host callback/diagnostic ports.
- **Evidence missing:** post-migration compiler/source ownership partition (the current0094 planning graph is supplied by this packet); constructor/member/internal API cut ledger; Unity effective-value fixture comparison; .NET cold/warm full CoreValidation with the 49 preserved CORE-B identities; separately, P12-G finite negative/atomicity closure on detached .NET path; actual T/T+1/T+2 repeated-roundtrip full 89-family/alias/root/output parity; full selected writer and epoch population exercise; exact resulting build-anchor/host descriptor vectors.
- **Upstream/source prerequisite:** any selected writer/census family lacking same-instance export, mutation epoch/writer path or staged hydrator is a blocker for the associated owner group. Resolve per the existing owner matrix and source call graph before that group moves; do not compensate with a fake provider. P12 family matrix alone proves schema scope, not compilation or writer closure. Frozen-format/numeric gates are satisfied only by the promoted matching codec/numeric source and exact oracle evidence at 0094, which should be revalidated as source dependencies when compiling.

## 9. Next executable package (after exact source/access gate)

One bounded package: `CORE-F-Shared-DailyV1-Application-and-Host`. Inputs are exact selected owner group list joined from promoted 89-family matrix + owner writer/capture/target crosswalk + source graph; accepted v2 identity contract and promoted codec/numeric source. Deliver shared application/API, unified owner-thread admission/lease/token/capture/restore/publication, thin Unity and .NET adapters, fixed effective fixture, SDK project/asmdef links, build anchor manifest, and full evidence matrix. One owner controls `SimulationRuntime`, operation/session gate, capture token and P12-G until integrated; split non-hotspot host work only after API contract and file ownership are fixed. Entry criterion: every admitted family and required transitive type has one source owner, constructor, owner instance, writer/epoch, capture and hydration mapping; no Unity dependency remains except named adapter cut; all inputs accessible and exact. Exit criterion is all acceptance evidence in §7 and independent exact-tip review; then promote under normal Core governance. If an owner group/source closure fails this criterion, keep package split by *complete semantic owner groups*, not by arbitrary file or micro-ID.

## 10. Rollback and explicit non-claims

Keep additive isolated commits and matching prior Unity builds/content/save evidence. On failure, revert through ordinary additive commits and run the matching prior host/build; never reset shared history, overwrite previous evidence or mix build identities. No implementation, source move, test run, architecture change, or Phase-12 scope change is authorized by this design artifact. No cross-host or cross-build restore, old-slot conversion, new content/profile, public mod API, Unity stubbing, second runtime, or unsupported owner omission is claimed.

## 11. Exact 0094 method ownership and host-facing API (supersedes §3's illustrative sketch)

The source is not one generic “bootstrap” method. At 0094 `TesteSimulacao` holds the mutable construction scratchpad, `SimulationActiveSession` is the immutable published reference bundle, `SimulationRuntime` owns day admission, and the P12-G coordinator owns capture/staging/restore validation. The only current active-session serialization and publication code lives in `TesteSimulacao`. A shared host package must relocate those roles together so neither executable becomes a second owner.

| Existing declaration in `Assets/_Project/Scripts/TesteSimulacao.cs` | Present behavior at 0094 | Final owner | Adapter action |
|---|---|---|---|
| `SimulationSessionReportState` | `LastEconomySnapshotDay` mutable report progress | Core `DailyV1SessionReportState` (or retained renamed type) | Unity logger/report rendering consumes a read-only value; no separate counter. |
| `SimulationActiveSession` and its constructor | Validates same-instance Runtime/composition/identity/spatial/site/Justice/logger/admission links; keeps City-by-location reference map; compares profile compatibility | Core `DailyV1ActiveSession` | Replace `SimulationConfigData` with `DailyV1EffectiveDefinitions`; retain exact owner-reference checks, map identity and `RandomSource` snapshot behavior. No Unity `MonoBehaviour`, config SO or logger sink reference in session. |
| Fields `activeSession`, `activeSessionGate`, `activeSessionOperationCount` | Volatile capture; synchronized operation begin/end; one active graph | Core `DailyV1Application` | `TesteSimulacao` may hold only one `DailyV1Application` reference. It cannot own a mirror authoritative active session or a second counter/lock. Existing query methods forward through Core operation scope. |
| `Start` | Validates enum profile; captures `Thread` instance + id once; attempts slot load before genesis; otherwise initializes | Unity lifecycle adapter calls `DailyV1Application.TryStart` exactly once on Unity Start thread | .NET entry point calls same method on its owning thread. Keep profile identity `UnityBootstrap-Daily-v1` for selected contract compatibility; host kind is separate. |
| `TryLoadSavedDailyV1BeforeGenesis` overloads | Inspect slot, derive current descriptor and effective/calendar inputs, decode; malformed/incompatible existing slot prevents genesis; valid image detached-restores and publishes, so no genesis/IDs/RNG first | `TryStart` orchestration in Core app + host descriptor construction | Unity adapter supplies Unity identity and explicit `Application.persistentDataPath`; .NET supplies host identity and explicit root. Preserve “slot exists but cannot load => fail closed, no genesis”. |
| `InitializeSimulation` and all five `SimulationGenesisPipeline.ExecuteStages` callbacks | Validates excluded profile branches before WorldId/allocator/owners; allocates roots, initializes logger/config, creates City/site/routes, geography, NPCs/directives, systems/runtime, checks candidate, installs formatter/composition, closes publication scope, marks FR-B world published, publishes one session | Core `DailyV1GenesisComposer.TryCompose` (private to app) | Unity adapter's `DailyV1DefinitionAdapter.TryCreate` converts selected authoring root to the promoted `DailyV1EffectiveDefinitions` once; it does not pass `SimulationConfigData` into Core. It passes `Action<string> stageCompleted` unchanged in order. .NET fixture produces same typed graph. Preserve every current stage ID/order and the first profile-validation-before-allocation cut. |
| `TryCreateCurrentDailyV1CompatibilityDescriptor` | Before genesis, computes selected profile validation, effective/calendar, P9 fingerprint+manifest, exact P8 authored geography snapshot/fingerprint, then Unity v1 descriptor with project assembly/provider/numeric/content/calendar identities | Core builds semantic fingerprints; Unity adapter owns v1 Unity identity; .NET adapter owns v2 environment/assembly identity | Use descriptor union described below. For no-slot path the descriptor is not required to allocate; for existing slot it must be computed before any root allocation. |
| `TrySaveDailyV1ContinuationAtPath` + `TrySaveCompletedDailyV1Boundary` | Enters active operation scope; requires correct owner thread/healthy boundary; asks P12-G `TryCreateRestoredSessionAndCaptureImage`; census+encodes; atomic explicit-root save; logs precise result | `DailyV1Application.TrySaveContinuation` | Unity wrapper supplies cached persistent path; .NET host supplies fixed explicit path. Save call must preserve P12-G full source witness + detached staged/target admitted validation; do not optimize to direct row export. |
| `TryLoadDailyV1ContinuationAtPath` | Holds gate for full load/restore; captures expected active session; requires no active operation and completed boundary; checks current compatibility; explicit-root load/decode; builds detached candidate; one publication; releases bootstrap aliases | `DailyV1Application.TryLoadContinuation` | Same operation/gate implementation in both hosts. Failed candidate or commit returns exact result and leaves active reference. |
| `TryRestoreDailyContinuationCore` | Current-session in-memory source continuation test path; requires expected healthy, idle, completed owner-thread source; P12-G source capture/stage, observer-before-publication, then one exchange | Internal Core-only `TryRestoreCurrentForValidation`; no host API | Preserve source-session test use only; production persistent load uses `TryLoadContinuation`. Test snapshot override delegates remain internal test seams. |
| `TryPublishRestoredSession` | Single gate checks no ops, candidate restored context/thread, exact configuration reference, healthy runtime/restored admission; for replacement checks expected ref, profile compatibility, source healthy completed boundary; `Interlocked.Exchange` | Core app's private `TryCommitRestoredCandidate` | No host callback may veto after exchange. Unity `OnPublished` callback is notification only after Core commit and exceptions are contained as diagnostics. |
| `SimulateWithSession` / `SimulationRuntime.AdvanceDay` | enters app operation scope, calls `AdvanceDay`, updates output and does auto-save after each completed day | `DailyV1Application.TryAdvanceDay` | preserve loop-level auto-save scheduling in Core app because it is existing behavior. UI-visible report line calls are host callbacks/readbacks, never rules that influence day result. |
| `TryGetNpcRuntime`, `TryGetCityRuntime`, `TryGetSpatialLocation/Route`, `TryGetExplorableSiteRuntime`, `GetNpcChronicle`, travel/expedition calls | operation-scoped facade into active owner graph | Core app operation-scoped query/command methods | `TesteSimulacao` preserves public method signatures and forwards. Raw mutable store exposure remains internal or existing compatibility surfaces must forward under operation lifetime without exposing setters/new API. |
| `CreateNpcChronicleFormatter` | Creates four lookup delegates over identity registry/spatial/city/site stores, currently receives config and active-world references | Core `NpcChronicleFormatter` constructor given four Core-owned lookup delegates from candidate composition | Remove config parameter. Bind from *candidate* owners before commit. Preserve current null/missing/ambiguous selection order and exact output; never capture previous session. |
| `ResolveCalendarDefinition`, `ResolveRuntimeConfiguration`, report rendering, Unity input, `Debug.Log`, `Application.*`, `MonoBehaviour` | Reads SO defaults or presents/hosts output | Unity adapter (conversion) / host report shell | Shared code consumes only promoted effective values and emits log events through below callback. |

### Final construction and host API

The concrete public API for the implementation package is exactly this surface; all types except the host environment manifest are owned by the shared Core assembly. `DailyV1EffectiveDefinitions` is the already-promoted immutable six-map graph; `P12AResult` and `SimulationRuntimeAdvanceFailure` retain their existing result semantics. This API replaces the pseudo-code in §3.

```csharp
public sealed class DailyV1HostEnvironmentIdentity
{
    public ushort HostKind { get; }                 // 1 = .NET host, per descriptor-v2 admission
    public string RuntimeFrameworkDescription { get; }
    public string RuntimeVersion { get; }
    public string OperatingSystemDescription { get; }
    public string RuntimeIdentifier { get; }
    public string ProcessArchitecture { get; }
    public string TargetFramework { get; }
    public IReadOnlyList<P12AAssemblyIdentityV2> Assemblies { get; }
    public string NumericProfileId { get; }
    public int NumericProfileVersion { get; }
}

public enum DailyV1LogSeverity : byte { Log = 0, Warning = 1, Error = 2 }

public sealed class DailyV1ApplicationHost
{
    public DailyV1ApplicationHost(string persistentDataRoot,
        IAtomicFileSlotOperations fileOperations,
        Action<string> stageCompleted,
        Action<DailyV1LogSeverity, string> logOutput,
        DailyV1HostEnvironmentIdentity dotNetIdentity = null,
        P12ACompatibilityDescriptor unityV1Identity = null);
}

public sealed class DailyV1StartResult
{
    public bool Succeeded { get; }
    public bool ResumedFromContinuation { get; }
    public DailyV1Application Application { get; }
    public P12AResult PersistenceResult { get; }
    public string Diagnostic { get; }
}

public sealed class DailyV1Application
{
    public static DailyV1StartResult TryStart(
        DailyV1EffectiveDefinitions definitions,
        DailyV1ApplicationHost host);
    // Compatibility projections retained from TesteSimulacao; all read the current Core-owned session.
    public SimulationBootstrapComposition Bootstrap { get; }
    public string FullLog { get; }
    public SimulationTime SimulationTime { get; }
    public CalendarDefinition Calendar { get; }
    public SpatialNetworkRuntime SpatialNetwork { get; }
    public DomainEventStore DomainEventStore { get; }
    public HistoryStore History { get; }
    public ScheduledDirectiveStore ScheduledDirectives { get; }
    public NpcDecisionStore Decisions { get; }
    public NpcChronicleService NpcChronicles { get; }
    public NpcChronicleFormatter ChronicleFormatter { get; }
    public TravelPartyStore TravelParties { get; }
    public TravelPartySystem GroupTravel { get; }
    public SimulationRuntime Runtime { get; }
    public ExplorableSiteStore ExplorableSites { get; }
    public ExpeditionStore Expeditions { get; }
    public ExpeditionSystem ExpeditionRuntime { get; }
    public ExpeditionSystem ExpeditionSystem { get; }
    public long CurrentDay { get; }
    public SimulationDate CurrentDate { get; }
    public bool TryAdvanceDay(out SimulationRuntimeAdvanceFailure failure);
    public bool TryAdvanceDays(int dayCount, out int daysAdvanced, out SimulationRuntimeAdvanceFailure failure);
    public bool TrySaveContinuation(out P12AResult result);
    public bool TryLoadContinuation(out P12AResult result);
    public bool TryGetNpcRuntime(string runtimeId, out NpcRuntime value);
    public bool TryGetCityRuntime(string runtimeId, out CityRuntime value);
    public bool TryGetSpatialLocation(string runtimeId, out SpatialLocationRuntime value);
    public bool TryGetSpatialRoute(string runtimeId, out SpatialRouteRuntime value);
    public bool TryGetExplorableSiteRuntime(string runtimeId, out ExplorableSiteRuntime value);
    public IReadOnlyList<NpcChronicleEntry> GetNpcChronicle(string npcRuntimeId);
    public bool TryStartTravelParty(ActionExecutionContext context);
    public bool TryStartExpedition(ExplorableSiteRuntime targetSite, ActionExecutionContext context, out ExpeditionRuntime expedition);
}
```

`TryStart` creates `SimulationRuntimeAdmissionContext(SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1, Thread.CurrentThread, Thread.CurrentThread.ManagedThreadId)` inside Core, so the context is born on the actual call thread in either host and uses the exact existing reference+ID owner-thread check. The selected profile enum/string remains unchanged in every manifest and owner/root snapshot; host kind is separate in descriptor v2. It runs pre-genesis compatibility validation when the single slot exists. If no slot exists it runs Core genesis. The `unityV1Identity` and `dotNetIdentity` parameters are an exclusive union: Unity caller passes only the precomputed v1 identity from Unity's adapter; .NET caller passes only host kind 1 identity fields above. Constructor rejects both set or both absent. The Core app calculates the shared profile/content/provider/calendar/P9/P8 identity from `DailyV1EffectiveDefinitions` and selected authored-geography facts, then compares/builds the correct current-host descriptor. It never scans loaded assemblies, resolves locations, queries Unity environment, or makes host identity up.

`DailyV1ApplicationHost` callback contracts are the exact current callback shapes: `stageCompleted` is the existing `InitializeSimulation(Action<string> stageCompleted = null)` test/host hook, called synchronously by `ExecuteStages` *after* each internal mutating stage handler and before the next stage. Preserve stage ID/order and the exact existing exception boundary: a hook exception caught by `InitializeSimulation` faults admission and prevents final session publication. The hook receives only the stage ID; it cannot replace stage work, supply profile values, or receive candidate owners. It remains a hook rather than a newly invented read-only event API. `logOutput` receives Log/Warning/Error severity and message at the current `SimulationLogger` emission point; `SimulationLogger.SaveToFile` writes under `persistentDataRoot/SimulationLogs` and reports through the same sink. Its adapter must not feed values back into the runtime.

`TryStart` is the only genesis-or-resume entry point. `TryLoadContinuation` does not genesis; it requires an already active, healthy, idle session on the owner thread and then performs the same current source/completed-boundary requirements. Both call the same private `P12GDailyV1RestoreCoordinator.TryCreateRestoredSessionFromImage` path and the same `TryCommitRestoredCandidate` gate. `TrySaveContinuation` wraps the existing full P12-G source capture, detached source owner staging and target admission used today by `TryCreateRestoredSessionAndCaptureImage`; this design preserves that extra proof path even when the temporary restored candidate is discarded after image composition. `TryAdvanceDay` encloses one ordinary outer runtime advance in Core's active-session operation count and existing runtime lease. The existing positive-completed-advance token remains issued only by `SimulationRuntime` after successful outer advance finalization. No API accepts, creates, serializes, or allows caller reuse of `DailyCaptureEligibilityToken`.

`ActiveSession` accessors and all listed queries are operation-gated. `TryLoadContinuation` holds the app gate across storage read, detached graph construction, late owner-vector recapture, restored-boundary admission, and active reference exchange; no app call can enter while this is in progress. `TrySaveContinuation` increments operation count before capture; P12-G's staging protocol is aware of that existing registered operation and must keep its current valid capture allowance, not accidentally force count zero. The app's `TryCommitRestoredCandidate` requires operation count zero after the load operation has acquired exclusive gate mode; use one monitor with explicit exclusive state or retain existing current pattern of entering the monitor and checking count. It does not unlock between commit checks and one `Interlocked.Exchange`.

### v1/v2 persistence union is a concrete implementation prerequisite

At 0094 `P12ACompatibilityDescriptorV2` exists at `Packages/com.simulation.core/Runtime/Persistence/ContinuationDescriptorV2.cs`, with `HostKind` restricted to 1/2 and `P12AContinuationDescriptorBridge.AdmitDotNetV2`, but `P12AContinuationHeader`, `P12AContinuationImage`, `P12AContinuationCodec`, `P12AContinuationStore`, `P12ACompatibilityDescriptorFactory`, and `P12G` are still v1 `P12ACompatibilityDescriptor` paths. This means the v2 descriptor source is **not** end-to-end .NET Daily-v1 persistence. Before this API is implemented, the persistence integration must carry the discriminated compatibility identity in a versioned continuation header, teach decode/load/save/atomic reread to use the host's expected descriptor and reject other host kinds before staging, and pass exact source/target descriptor bytes through capture/restore. Preserve Unity v1 byte acceptance and v1 storage behavior unchanged. The .NET slot uses the existing single atomic slot algorithm with v2 descriptor, cannot accept Unity v1, and does not write a Unity descriptor. This is a precise WP2 integration dependency, not optional implementation discretion.

The host inputs are fixed: selected effective definitions, truthful host identity, persistent data root, atomic file-operation object, existing stage hook, and log-severity sink. Runtime graph definitions, profile/genesis semantics, stage order, runtime/session, owner providers and all image algorithms are shared. The definitions-runtime package's known reference-identity constraint is explicit: runtime owner construction must consume the canonical typed definition graph while preserving one canonical node instance per DefinitionId for current-action/inventory/market identity comparisons; no extra caller resolver or `SimulationConfigData` port is permitted.

## 12. Exact validation identity and finite P12-G links

The phrase “49-case map” means exactly the 49 portable .NET identities in `docs/validation/COREB_DeterministicTemporal/TEST_CASE_IDENTITY_MAP.md`, **not** the 49 owner-map rows and not a claim that CORE-B proves runtime/restore. Preserve those names unchanged in cold and warm CoreValidation and add new host/runtime cases additively. The map counts: 3 moved RNG cases, 7 unchanged root-snapshot cases, 38 unchanged logical-timeline cases, and 1 new provider-boundary case. Its only Unity-only case (`DeterministicRandomTests.NpcDecisionSelectionDoesNotDependOnActionInsertionOrder`) is not in portable .NET 49. Full exact names are in that linked source file and the map is the identity authority.

Also retain the 89-family plus 24-operation closures exactly from `CORE_F_OwnerClosure/{CASE_MAP.csv,OPERATION_CASE_MAP.csv,OWNER_WRITER_MATRIX.csv,OWNER_CAPTURE_TARGET_CROSSWALK.csv}`; these are separate acceptance evidence. Every operation/family mapping has row identity, source blob, writer, epoch, capture, target hydration and applicable test case; do not replace with aggregate counts.

P12-G source-level finite proof anchors at 0094:

- Capture source: `P12GDailyV1RestoreCoordinator.TryCreateRestoredSessionAndCaptureImage` -> private `TryCreateRestoredSessionCore(captureContinuationImage:true)`. It obtains `TryGetCompletedDailyCaptureToken`, validates it, holds exact token owner-section reference, begins `DailyCaptureStagingAttempt`, captures F plus C roots and D/E owner packages in the same attempt, validates source token both before and after target checks, calls `TryComposeContinuationImage`, and revalidates token again before success. Do not replace with just `P12ADailyV1ImageComposer` on raw section rows.
- Persisted restore: `TryCreateRestoredSessionFromImage` rejects wrong/missing descriptor, profile, restored/foreign/off-owner-thread bootstrap context before decode/staging; `P12ADailyV1Census.TryCreateForImage`; `P12ADailyV1ImageOwnerDecoder.TryDecode`; private C/D/E/F stages; exact target checks; target owner vector captured; mutation guard bound; vector+epoch recaptured and matched; `TryAdmitRestoredDailyBoundary(WorldId, Header.Day, Header.Sequence, sections, epoch)`; healthy restored boundary checked; only returned candidate can reach app commit.
- Owner coverage/replay: `docs/validation/CORE_F_OwnerClosure/CASE_MAP.csv` (all cumulative executable scenarios) and `OPERATION_CASE_MAP.csv` (sealed 24-operation live set); matrix and crosswalk are exact migration maps. At 0094 the validation map input source identities carry historical source roots noted per row: refresh to code-bearing 0094 before any new run; case identity/name stays stable.
- Existing P12-G regression packages are at `docs/validation/P12GRestoredBoundaryCoordinator/VALIDATION.md`, `docs/validation/P12GActiveSessionPublication/VALIDATION.md`, `docs/validation/P12GGraphRejectionCoverage/VALIDATION.md`, `docs/validation/P12GFailedRestoreNoGameplayInvocation-20261010/VALIDATION.md`, `docs/validation/P12GRestoredBoundaryOrder/VALIDATION.md`, `docs/validation/P12GGenealogyDuplicateEdgeFailureAtomicity/VALIDATION.md`, `docs/validation/P12GTargetVectorPrecheckMutation/Final/VALIDATION.md`, `docs/validation/P12GP8CCityBindingLateRecapture-20261011/VALIDATION.md`, `docs/validation/P12GP8DPostGuardRecapture-20261011/VALIDATION.md`, `docs/validation/P12GP12DGenealogyLateRecapture-20261012/VALIDATION.md`, `docs/validation/P12GP12DExplorableSiteLateRecapture-20261011/VALIDATION.md`, `docs/validation/P12GPoliticalBindingClosure/VALIDATION.md`, and P12-G graph-rejection subdirectories. They collectively require no gameplay/genesis on failed restore, no candidate escape, unchanged old active session, duplicate/dangling/false-return/internal-stage rejection, exact target identity and relationships, late post-guard recapture, and one publication. Reuse exact test identities and source applicability; do not infer coverage just from package names.
- Same-host test extension adds required restored-boundary and publication failure points from §7 on both .NET host and Unity adapter. The root v2 cross-host mismatch test must reject pre-staging and preserve bytes; it is a new test of host identity boundary, not cross-host save/load support.

## 13. Package readiness after exact map

The next coherent package is one shared `DailyV1Application` integration containing the specific `TesteSimulacao` roles above, the selected owner/source closure needed by its typed definition graph, current runtime, P12 capture/restore, and persisted-image host descriptor. It does not require waiting for unrelated later owners/domains, but it **does** require the current shared runtime source closure and definition-consuming owner graph because `SimulationRuntime`/constructors/P12 hydrators reference those types as direct semantic dependencies. The “complete package” can be scoped to the selected existing daily graph; it cannot be a facade-only host over promoted A-E and cannot claim a ready .NET executable until those direct dependencies compile from shared source.

Exact blocked inputs are named, finite: (1) selected runtime-owner source/member/constructor cut and typed definition binding preserving canonical-node reference identity; (2) v1 header/image/codec/store integration for the already-promoted descriptor-v2 type; (3) one shared app gate/session/application implementation and Unity forwarding adapter; (4) fixed .NET 8 fixture/host identity manifest; (5) new .NET cold/warm app tests + preserved CORE-B identity map + 89/24 owner operation maps + existing finite P12-G negative set + T/T+1/T+2 parity. Once those code interfaces land, no further abstract API phase is needed: the exact API and ownership contract above is fixed. Shared project/Unity source movement and `SimulationRuntime`/P12-G work stays one serialized integration owner. Definition-owner source movement can proceed only within that bounded same package and owner closure; do not insist on moving every one of 191 tracked files if complete selected semantic closure compiles and rejected-branch types remain valid.






## 14. Assembly/access graph and exact access cuts

Every current P12 A/G source file is compiled into Unity `Assembly-CSharp`, so `internal` currently lets `TesteSimulacao`, `SimulationRuntime`, P12 codecs and the restore coordinator cooperate. A .NET host project is a different assembly. The prescribed cut is to move all semantic internals together into the *same* `Simulation.Core` assembly, keep them internal, and expose only the public `DailyV1Application` API in §11 to Unity/.NET hosts. No `InternalsVisibleTo` is needed.

| Type/member at 0094 | Current access | Assembly rule in the package |
|---|---|---|
| `SimulationBootstrapComposition` | public class; internal constructor; mostly public owner/provider properties; `RuntimeIdAllocator` and `RecordSequence` internal | Move as one shared Core composition. Its constructor stays internal. Keep the current Unity-facing `TesteSimulacao.Bootstrap` projection if source compatibility requires it; do not make another host construct it directly. |
| `SimulationActiveSession` and `SimulationSessionReportState` | internal classes in `TesteSimulacao.cs`; members public only within the internal class; constructor validates exact owner references | Move/rename internal session/report state into Core with composition/runtime/owner refs validated exactly. Public host receives only read/command surface and existing public `Bootstrap` compatibility projection. |
| `SimulationRuntimeAdmissionContext` | public type and public `(profile, Thread, threadId)` constructor; `IsRestoredContinuation`, owner Thread/id, restored factory and ownership test internal/private | Move with Runtime to Core unchanged in profile identity. App calls the public constructor internally on the Start thread. Keep restored-context factory private to Core/runtime path. |
| `DailyCaptureEligibilityToken` | internal sealed immutable token; constructor internal; all identity evidence internal | Remains internal in Core; no host method parameter or result ever contains the token. |
| `SimulationRuntime.TryAcquireAdvanceLease` and nested `AdvanceLease` | internal acquire; internal nested type and constructor; private release | Runtime and app in Core assembly call the same lease implementation. Host can only call app `TryAdvanceDay`; never expose lease type. |
| `SimulationRuntime.TryGetCompletedDailyCaptureToken`, `TryValidateCompletedDailyCaptureToken`, `TryCaptureUnadmittedRestoredDailyOwnerVector`, `TryBindRestoredCandidateMutationGuard`, `TryAdmitRestoredDailyBoundary`, `HasValidCompletedDailyBoundaryAdmission`, `HasValidRestoredDailyBoundaryAdmission` | internal/private validation/admission surfaces | Move runtime + P12 G in Core, retain internal. Call only from app/coordinator; no host-side token/census-authority API. |
| `P12GDailyV1RestoreCoordinator` and `P12GDailyV1RestoreFailure/Stage` | internal coordinator, internal enums and methods; partial source; restore returns internal `SimulationActiveSession` | Move all P12-G parts and staged P12 D/E/F constructors into Core. Keep failure enum internal; app maps to public `P12AResult`/stable failure code+diagnostic. Stage enum/observers remain private/internal. |
| `P12AContinuationStore`, `IP12AContinuationFileOperations`, explicit-root methods | store/internal IO interface internal; public `IAtomicFileSlotOperations` exists in Core Persistence; save explicit overload takes internal subtype; load explicit overload uses `SystemAtomicFileSlotOperations.Instance` internally | Store stays internal. Change its operation parameter to the already-public `IAtomicFileSlotOperations` and add explicit-root `TryLoad(..., IAtomicFileSlotOperations, ...)` so .NET uses the passed narrow host port. Keep Unity convenience `TryLoadCurrent`/`TrySaveCurrent` only in Unity adapter. This source/API seam is missing at 0094. |
| `P12AContinuationCodec`, image owner decoder/encoder, census and image composer | mostly internal Core-contract helpers; Unity A/G entry points call them in same assembly | Move image pipeline with store/coordinator into Core, keep helpers internal. V2 discriminated header integration described in §11 is part of this move. |
| `P12ACompatibilityDescriptorV2`, `P12AAssemblyIdentityV2`, bridge `AdmitDotNetV2` | public Core Persistence types; strict canonical serializer and host-kind check; do not own P12 image/header/store | Reuse exact public types. Do not add a duplicate descriptor DTO. Wire these existing identities through the image/header/store in the WP2 integration package. |
| `DailyV1EffectiveDefinitions` and `DailyV1DefinitionIdentity` | public Core immutable graph/identity builder; graph factory public; authoring reader remains Unity | Reuse directly. The only content reader is `DailyV1DefinitionAdapter.TryCreate`; do not add a host-specific resolver contract. Definition runtime cut must consume canonical graph node references while preserving current action/item reference-identity invariants. |
| `P12ACompatibilityDescriptorFactory` | internal Unity static factory; reflection scans Unity assemblies and queries Unity `Application` values | Split semantic fingerprint calculation into internal Core helper over `DailyV1EffectiveDefinitions`/P8/P9 values. Keep Unity assembly/build fields in Unity adapter. .NET identity fields come only from the fixed launch manifest. |
| `SimulationLogger` | public Unity-dependent type using `UnityEngine.Mathf`/`Debug`; also owns report lines, begin-day receipts, keyed receipts and revisions | Move receipt/report authority to Core; replace direct `Debug.Log`, `Debug.LogWarning`, and `Debug.LogError` calls with the constructor-bound `Action<DailyV1LogSeverity,string>` defined in §11. Unity binds the matching Debug method; .NET binds deterministic captured output. Route `SaveToFile` under the same explicit persistent data root. Never route host callback results back into state transitions. |
| `NpcChronicleFormatter` / `TesteSimulacao.CreateNpcChronicleFormatter` | formatter currently under Unity type closure; factory internal and takes runtime stores, cities and `SimulationConfigData` | Move formatter/model closure only after compiler graph confirms complete SCC. Factory is Core private and receives staged owner lookup functions. The adapter receives formatted value output only. |

This access map fixes a **single assembly cut**, not an exact selected file inventory: current production `SimulationRuntime`, P12-G, test harness, conditional P10/P14 branches and owner types have transitive edges that still need a fresh source/compiler response graph. The package may move fewer than the 191 RFI candidate files if, and only if, the selected executable graph plus compile-time rejected-branch dependencies builds from one shared Core source owner. Keep source-accessibility `internal` unless a specific Unity forwarding call proves a public host operation is required; do not widen internals for convenience.

The API constructor’s file-operation seam is deliberately the promoted `IAtomicFileSlotOperations` (`AtomicFileSlot.cs`), not current `IP12AContinuationFileOperations`: at 0094 explicit `P12AContinuationStore.TryLoad` does not accept operations and selects `SystemAtomicFileSlotOperations.Instance`, so the host’s injected testable storage port cannot currently govern reads. Adding that exact explicit overload is part of the persistence integration, while `TryHasSlot` and `TrySave` already accept the explicit object/root path. The atomic algorithm and result taxonomy remain `AtomicFileSlot`/P12A behavior.
