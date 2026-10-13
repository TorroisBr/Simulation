# CORE-B — deterministic primitives and logical-time closure

**Checkpoint:** CORE-B Deterministic Primitives and Temporal Closure (unnumbered Core infrastructure).
**Design status:** ready for independent technical review; **not implementation-ready**. Implementation remains `WAIT_DEPENDENCY` until CORE-A is promoted into `codex/core/canonical`, this design receives an independent PASS, and the implementation worker refreshes the integrated source baseline.
**Production source baseline:** `codex/phase12/canonical` at `4cc31073f0ec6ba5c718084e4cebd6e914931d5d`.
**Architecture baseline:** `codex/architecture/simulation-core-extraction` at `32358d2a0faf511e098c4f295a5132a4bfb8cfb2`.
**Design branch:** `codex/core/CORE-B-technical-design`. This artifact is documentation only and does not move either canonical ref.
**Authority:** the promoted Simulation Core extraction contract and its CORE-A handoff. This design chooses implementation boundaries within those accepted semantics; it does not amend architecture or authorize a new runtime, product rule, save profile, or compatibility promise.

## 1. Objective and bounded outcome

Compile the existing authoritative deterministic random primitives, stable length-prefixed identity encoding, and complete logical-time scheduler/continuation implementation from the one shared `Simulation.Core` source tree used by Unity and the pinned .NET project. Preserve the existing values, public type identities, ordering, sequence allocation, input sealing, owner commit contexts, temporal continuation/reconstruction behavior, failure semantics, and consumer rules.

This is an extraction of existing implementation. It does not create a second scheduler or random provider, alter `SimulationRuntime`'s authority, add a host, change action or temporal rules, or claim that all consumers of these capabilities have migrated from Unity. `SimulationTimeline` remains a pure capability used by current Unity owners; CORE-B does not move those owners or all P18 runtime consumers.

The CORE-B implementation starts only from the then-current integrated `codex/core/canonical` tip after CORE-A promotion. The architecture branch's executable tree is stale and must not be merged into production. The production baseline above identifies the audited source and test behavior, not a future implementation base if CORE-A advances it.

## 2. Exact implementation closure

### 2.1 Deterministic random source and explicit stream state

Move the complete `Assets/_Project/Scripts/DeterministicRandom.cs` source and its existing `.meta` GUID into `Packages/com.simulation.core/Runtime/Determinism/DeterministicRandom.cs` (or the equivalent agreed Core Runtime subdirectory). Remove the old source path so every moved type has one definition. Add this exact source to the SDK project's explicit Compile list; do not glob legacy `Assets/_Project/Scripts`.

The move includes, without redesign:

- `IAuthoritativeRandomSource`;
- `DeterministicRandomState`, including non-empty-key/nonnegative-index guards and its current unchecked `DrawIndex++` progression: at `long.MaxValue`, `ConsumeIndex()` returns `long.MaxValue` while the stored index wraps to `long.MinValue`; the next consumption returns that negative value and advances the stored index to `long.MinValue + 1` before `DeterministicRandomSource.NextUnit` rejects the negative draw index. This is not checked overflow or saturation. Preserve this observed behavior and pin the arithmetic context identically under Unity and SDK compilation; do not silently "fix" it during extraction;
- `DeterministicRandomStream` and independent stream-local draw indexes;
- immutable `DeterministicRandomRootSnapshot` and its schema/provider/algorithm identity;
- `DeterministicRandomSource`, including capture and staged restore;
- the `fnv1a64-keyed-utf16` v1 algorithm: exact little-endian integer mixing, UTF-16 code-unit byte order, seed/key/draw-index inputs, final fold, midpoint scaling, float conversion, and current validation/exception behavior.

Do not change seed defaults, stream-key equality/casing, draw numbering, string encoding, constants, conversion order, or snapshot metadata. No replacement RNG, Unity random dependency, `System.Random`, global state, draw cache, or seed migration is introduced.

### 2.2 One shared stable-key encoder

At the baseline, `SpatialStableKey` is an internal type at the end of `Assets/_Project/Scripts/SpatialRouteKnowledge.cs`. Its `Encode` function is pure length-prefixed concatenation, while `OptionKey` consumes spatial `TraversalOptionRef` and its enum. Move only the encoding implementation into a BCL-only Core source, for example `Packages/com.simulation.core/Runtime/Identity/StableKeyEncoding.cs`, with the single method `public static string Encode(params string[] parts)`.

The public visibility is required because `SimulationTimeline` and existing production call sites compile in different assemblies after extraction. This is an internal assembly-composition utility, not a supported P19 mod API or a new compatibility promise. Retain the existing internal `SpatialStableKey` in `SpatialRouteKnowledge.cs` as a compatibility adapter for current Unity-side callers: its `Encode` delegates to `StableKeyEncoding.Encode`, and its spatial `OptionKey` implementation remains in the Unity source. Do not duplicate the encoder, move spatial authority/types, or rewrite all key consumers in CORE-B.

Preserve the exact encoding: invariant-culture decimal `string.Length`, colon, then value for each component; null components become empty strings; null params array produces no components; component order and ordinal content remain untouched. The encoder is not a hash and does not normalize Unicode or delimiters. Keep all existing callers' encoded identities byte-for-byte/string-for-string equal.

### 2.3 Complete logical-time implementation

Move the complete `Assets/_Project/Scripts/LogicalTimeline.cs` file and its existing `.meta` GUID to `Packages/com.simulation.core/Runtime/Temporal/LogicalTimeline.cs`; remove the old definition. Include all declared types and implementation, not only `LogicalTick`:

- `LogicalTick`, including nonnegative validation, checked next-day boundary, calendar projection and value semantics;
- `TimelineFailure`, `DueWorkKind`, `DailyBoundaryOperation`, `DueWorkReference`, and `TimelineInputReference`;
- due-work, input, and day-boundary owner/commit interfaces, including timeline binding and resumable-boundary protocols;
- immutable continuation step, manifest, published fact, and continuation state values;
- `SimulationTimeline`, including admission/indexing, preview/read APIs, sealing, due-work selection/ordering, input dispatch, day-boundary activation, continuation progression, publication, source-signal handoff, retries, reconstruction validation, sequence accounting, limits, reentrancy rejection, and all current failure paths.

The only verified source dependencies are BCL APIs, CORE-A's `SimulationCalendar`/`SimulationDate`, and the stable-key encoder above. The moved file must call `StableKeyEncoding.Encode` for its stable identities. Do not bring `SpatialRouteKnowledge.cs`, Unity lifecycle, `SimulationRuntime`, `SimulationTime`, P18 owner implementations, or host services into Core. Compilation against the exact allowlist is the closure proof; the architecture's file-level inventory is advisory. If compilation exposes another dependency, stop and classify it before expanding the closure.

## 3. API, visibility, and serialization boundaries

Keep all current type names, global namespace, overloads, values, and behavior unless a visibility change below is strictly required for assembly composition. `Simulation.Core` remains a private embedded package and single shared implementation, not a general extension surface.

Two current random APIs are `internal` because the source and P12 codec/stager presently share `Assembly-CSharp`:

1. `DeterministicRandomRootSnapshot`'s constructor is used by `P12ACausalRootOwnerCodec` to reconstruct the exact raw serialized fields (including unsupported metadata so normal validation can reject them).
2. `DeterministicRandomSource.TryCreateStagedFromSnapshot` is used by P12 root staging to validate metadata and create a detached source before publication.

After the move, those existing P12 callers remain in the predefined Unity assembly and cannot access Core internals. Do not add `InternalsVisibleTo`, expose random/domain stores, copy types, or create a second implementation. Make only the snapshot data constructor and `TryCreateStagedFromSnapshot` public, preserving their parameter/result behavior. The constructor remains a data-transfer value and must not silently validate/default unsupported fields; validation remains in the existing staging method. Keep stream construction and index consumption internal to Core. Add focused tests proving unsupported schema/provider/algorithm still fail closed, active sources remain unchanged, and successful staging preserves exact seed and keyed outputs. The new public members are an assembly bridge only and imply no P19 or third-party stability guarantee.

Core's SDK compile list and Unity asmdef must own each moved type exactly once. Preserve original `.meta` GUIDs for the two moved files, create ordinary Unity metadata for new files, and do not import generated SDK DLLs. The `SpatialStableKey` adapter stays with its spatial inputs. No Unity serialization attributes or persistent data shape is added to Core types.

Assembly identity/MVID changes are expected. They are not a save conversion or compatibility promise. The selected P12 Daily-v1 exact current-host/current-build contract must continue to fail closed for descriptors from the prior assembly identity and must pass new-build same-build capture, detached load, restoration, and continuation. Do not weaken descriptor discovery, hardcode MVIDs, or claim cross-build/host compatibility.

## 4. Test-source migration and evidence map

Retain every existing test and its name/case identity. Share portable NUnit source between Unity EditMode and `tests/Simulation.Core.Tests`; run equivalent deterministic assertions on both hosts. Preserve explicit old full-name to new full-name/case mapping in the validation record, following CORE-A's test conservation practice.

| Existing source at audited baseline | CORE-B treatment |
|---|---|
| `Assets/_Project/Tests/EditMode/Editor/DeterministicRandomTests.cs` | Split the first three algorithm/stream tests (`SameSeedContextAndDrawProduceTheSameValue`, `UnrelatedStreamConsumptionDoesNotShiftOperationStream`, `StreamStateMakesDrawPositionExplicitAndIndependent`) into a shared pure Core test file. Remove their `SimulationTestFactory` setup/teardown there. Keep `NpcDecisionSelectionDoesNotDependOnActionInsertionOrder` in Unity because it constructs `NpcRuntime`, ScriptableObject-backed actions, and `NpcDecisionSystem`; it is consumer parity, not a portable RNG primitive test. Preserve exact test identities and fixture cleanup for the retained case. Add a Unity-side boundary regression that constructs `DeterministicRandomState` at `long.MaxValue`, observes the max return/wrap, then observes the next negative index and source rejection/state progression. Keep this test Unity-side if internal `ConsumeIndex()` cannot be called by the separate SDK test assembly without widening visibility. Add shared provider boundary checks that `NextUnit(key, long.MaxValue)` remains accepted and `NextUnit(key, long.MinValue)` rejects; do not expose `ConsumeIndex` solely for testing. |
| `Assets/_Project/Tests/EditMode/Editor/DeterministicRandomRootSnapshotTests.cs` | Move/share all five methods and all three `StagedSourcePreservesEveryBoundarySeed` parameter cases (seven selected cases total) into the Core test project and Unity EditMode. Include invalid-metadata creation through the newly public data constructor; do not remove rejection/immutability assertions. |
| `Assets/_Project/Tests/EditMode/Editor/LogicalTimelineTests.cs` | Move/share the entire BCL/NUnit fixture (38 NUnit attributes, 33 methods at the audited source) into Core tests and Unity EditMode. No test is dropped or replaced by a smoke test. Preserve parameterized cases and all fake owner commit/reconstruction contexts. |
| Current P18, P12, Runtime, Crime/NPC and bootstrap consumer suites | Keep in Unity as integration regressions. They exercise actual owner wiring, Runtime admission/authority, P12 staging/descriptor discovery, and domain consumer behavior; CORE-B does not transplant those consumers. |
| Stable key encoder coverage | Add a small shared pure test fixture if current coverage does not prove the encoder's existing null/empty behavior, delimiter-bearing component injectivity, component order, and invariant-culture output. Retain `LogicalTimelineTests` coverage for delimiter-bearing due-work and boundary identities. No hash golden or alternate key format is introduced. |

Before moving test sources, capture the baseline selected cases and exact outputs from the unchanged source tree. After extraction, the same test source/case set must compile and pass under both Unity and .NET. Candidate scans are not proof of portability; source inspection and actual builds are required. The single RNG consumer-order test remains host-side. No P18 consumer or P12 persistence test moves merely because its interface becomes Core-owned.

## 5. Ordering, context, and invariants that must remain exact

The extraction must preserve, with no normalization:

- logical time as nonnegative integer millisecond-sized quanta (`TicksPerDay == 86400000`), checked overflow and current calendar projection;
- owner-thread/operation context supplied by existing owners; no new authority, implicit lock, wall clock, frame time, Unity callback, or synchronization service;
- accepted input identities, target instants and sequence uniqueness; sealed-prefix requirements and rejection of late/duplicate input;
- stable identity encoding, owner IDs, due-work IDs, instance IDs, occurrence IDs and sequence values;
- deterministic ordering of same-instant inputs, due work and boundary steps, including existing causal-wave behavior and collision rejection;
- atomic owner prepare/commit boundaries, retry identity, commit failure behavior, no sequence drift on failed commit, and current stale-work behavior;
- frozen continuation content/ordinal/roster, immutable progress, exact retry point, restored world/profile/owner revision matching, published-fact reconciliation, and delayed signal handoff until outer success;
- current dispatch limits, pure preview/query semantics, reentrant rejection, target-before-now/input-not-sealed errors and faulted/runtime-boundary behavior.

Do not reorder dictionaries/sets, recalculate rosters on retry, assign fresh IDs, recompute random results, change serialization order, alter causes/epochs, or replace failed commits with exceptions/defaults. If any baseline parity differs, diagnose and return to design before integration.

## 6. Integration plan and ownership

1. Implement CORE-B only after CORE-A is promoted. Start a fresh isolated candidate from current `codex/core/canonical`; classify the delta since the audited production base. Re-read current architecture/design and verify the expected `SimulationCalendar`/`SimulationDate` Core API.
2. Move the two complete source files and metadata; add `StableKeyEncoding` plus the delegating Unity adapter; update the Core asmdef and explicit SDK source list. Add the focused shared test files and explicit project compile links. Keep the Core assembly netstandard2.1/C# 9 and existing pinned SDK/test setup from CORE-A.
3. Compile the Unity package and .NET allowlist early to prove closure before broad consumer edits. Check symbols are unique, no Unity references enter Core, no `Assembly-CSharp`/Editor/WorldExchange dependency exists, and no source-copy or SDK-DLL duplicate appears.
4. Rebind existing consumers to the Core public types. Update only the two P12 snapshot bridge call sites as needed and retain P12 serialization bytes/field order. Keep the internal `SpatialStableKey` wrapper so current Unity call sites continue to compile without mass rewrite.
5. Run shared deterministic tests on both hosts; then run affected Unity consumer suites, P12 exact-profile compatibility/capture/restore/continuation plus prior-descriptor rejection, ALL EditMode, official Smoke, and `git diff --check` on the final integrated input tree. Use CORE-A's bounded CoreValidation/evidence procedure and preserve full .NET result evidence. Compare code/test sources and runtime/toolchain input manifests across the final host gates.
6. Obtain independent exact-tip implementation review and the normal refreshed integration/promotion review. Update the single `docs/CORE_STATE.md` only with exact candidate, test-source mapping, evidence, limitations and integration facts.

Serialized hotspots: shared Core assembly and Unity import/compilation; P12 random codec/stager bridge and descriptor/MVID validation; any actual P18 `SimulationRuntime`/owner composition touched by baseline refresh. The timeline algorithm itself does not require editing runtime owners. Keep independent Core source/test work isolated from active numbered-phase checkouts, and integrate only in an owned validation window. Do not touch protected ProjectSettings or unrelated worktrees.

## 7. Validation and promotion evidence

Required before CORE-B implementation promotion:

- exact code/source closure build under both Unity and SDK with Core containing only BCL and CORE-A references;
- .NET locked restore, nonempty complete selected test discovery, all shared RNG/snapshot/timeline/key cases passing with zero skipped, failed, inconclusive, aborted, or unexecuted cases;
- Unity focused RNG and logical-time suites, retained RNG consumer-order case, and all affected P18/P12/owner integration tests;
- exact stable key and random output parity, including exceptional values and boundary seeds; specifically assert the `long.MaxValue` draw is accepted, the plain state-counter increment wraps to `long.MinValue`, and the following stream draw is rejected after consuming that negative index. Run the counter-rollover case in Unity where the internal state method is available; assert direct provider validation on both hosts. Record and verify the effective checked-arithmetic compiler setting so Unity and SDK do not diverge.
- logical timeline parity for same-instant ordering, continuation/reconstruction, retries, stale references, boundaries, IDs and causal sequences;
- Daily-v1 compatibility and fail-closed prior-assembly descriptor rejection; new-build same-host save/detached-load/restore/continue, preserving slot/publication atomicity;
- full ALL EditMode and official Smoke on the final integrated source tree, zero failed/skipped/inconclusive; `git diff --check`;
- full source input identities, test case map, original per-case report bundle and privacy-checked evidence under CORE-A's evidence policy; exact-tip independent implementation review.

These are required gates, not results. No test was run as part of this technical-design candidate. A mechanical extraction should not change causal behavior; if integration changes daily/long-horizon behavior, stop and apply the architecture's corresponding LongRun gates rather than relying on the extraction label.

## 8. Dependencies, exclusions, and readiness

**Required before implementation:** CORE-A's promoted shared calendar/date source assembly; independent PASS on this CORE-B design; refreshed clean Core implementation worktree; exact compile closure confirmed on the post-A baseline.

**Explicitly excluded:** moving `SimulationRuntime`, `SimulationTime` advance/dispatch, P18 boundary owners or consumer workflows; moving crime/NPC/city/market/spatial consumers; moving P12 codecs or changing the save schema; P12 profile widening, P13 historical replay/fork, P19 extension/mod API, Lab scenario host, external transport, a second scheduler/RNG implementation, new domain rules, numeric replacement, cross-build/host migration, and changes to architecture semantics.

There is no unresolved product or canonical architecture decision in this design. The public visibility of the shared encoder and two random snapshot bridge members is a narrowly scoped technical consequence of the existing cross-assembly consumers and is documented above; review may refine names while retaining the same boundary. This design is **not** implementation authorization or readiness by itself. Implementation remains blocked until CORE-A is promoted and this design has an independent review PASS. The exact production baseline is an audit anchor; do not branch implementation from the stale architecture executable tree or assume baseline code is still current.
