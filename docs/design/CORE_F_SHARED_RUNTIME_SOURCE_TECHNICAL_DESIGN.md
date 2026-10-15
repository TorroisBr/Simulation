# CORE-F Shared Runtime Source — bounded compatibility design

**Status:** `TECHNICAL_DESIGN_EVIDENCE_COMPLETE` / independent review `PASS`.
This is a continuation of the approved CORE-F source-preparation handoff, not a
new checkpoint or a change to simulation semantics.

| Authority | Verified identity |
|---|---|
| Core implementation/design base | `codex/core/canonical` `652ac7bf02943cf6160fd27f271a781db70657c5` |
| Architecture authority | `codex/architecture/core-f-shared-runtime-source-closure` `758d4eaf7022e09f4b106ef25b390d14d6b55f7a` |
| Parent handoff | [`CORE_F_SHARED_RUNTIME_SOURCE_MASTER_HANDOFF.md`](../architecture/CORE_F_SHARED_RUNTIME_SOURCE_MASTER_HANDOFF.md), exact text imported from promoted architecture |
| Selected Unity source graph | Values source identity `c69fcf471be7c73c3b63b539d0ae5e659cb9e029`; 329 source blob identities verified against the graph |
| Core prerequisite packages | Persistence `84b66da69be896f0db347b940d9e464cfaa1c4f2`; Owners `8fc975631db0d4e53fa58c490c752fb79d209b0e`; Values `c69fcf471be7c73c3b63b539d0ae5e659cb9e029` |

## Evidence status

The original draft fixture remains unchanged at
`docs/validation/CORE_F_SharedRuntimeSource/OracleCaptureTests.cs` (SHA-256
`39C4B617141A8EADC4419D90C7CE6A61EF2D4D77F29FC0680EB8F649C8104D3C`). The
expanded fixture was added to the existing Unity Editor test source area at
`Assets/_Project/Tests/EditMode/Editor/CORE_F_SharedRuntimeOracleCaptureTests.cs`
(SHA-256 `B0713750A006BBF1A5B75AD70EE183CF3CD0A2581D940DF49335B2037B70D5B5`).
No production source changed.

The final run used Unity Editor `6000.3.9f1` (revision `7a9955a4f2fa`),
Test Framework `1.6.0`, batchmode EditMode, and the full test selection:
`3173/3173` passed, zero failed/skipped/inconclusive. The evidence wrapper
reported stable before/after inputs. The immutable raw XML (SHA-256
`1c1bcfa171f5d3f38188fbe19bbf16bc7615baa4858ee4a0bb730ce18ebcbcda`, 3,193,379
bytes) and raw log (SHA-256
`4125df2b07a5e392fac0942fd848515a6e496c45d2c5a8173bfe41378219efa5`,
197,221,845 bytes) are retained outside Unity `Temp` in the wrapper's
account-protected local evidence store under run ID
`d8b4c40e04ab466a9c56fdf6e5f5361d`. The normal package and private-context
verification both passed. The public package is
[`core-f-oracle-final-d8b4c40e.manifest.json`](../validation/CORE_F_SharedRuntimeSource/core-f-oracle-final-d8b4c40e.manifest.json)
(SHA-256 `75652F63593191509DFC9E5E3B2F2C277657BAD4BF33B33D2E3D581CDC2AC80B`)
and [`core-f-oracle-final-d8b4c40e.zip`](../validation/CORE_F_SharedRuntimeSource/core-f-oracle-final-d8b4c40e.zip)
(SHA-256 `D930CA6E929ABBBDF14508C245363FE700F519CE4A6032CEB895233596520B9B`).
As required by the evidence policy, the normalized public XML omits
`TestContext` output. The separately derived
[`UNITY_ORACLE_CAPTURE_RESULTS_d8b4c40e.csv`](../validation/CORE_F_SharedRuntimeSource/UNITY_ORACLE_CAPTURE_RESULTS_d8b4c40e.csv)
(SHA-256 `95E3912A43712693CF6EF68CF22E306A770B53A51818B0D6123842F6FA1D7A44`)
contains the 247 oracle output records and binds each to the private raw report
hash, fixture hash, package hashes, Unity identity, and complete run counts. It
contains no local paths or credential-like values.

The earlier filtered run is also preserved privately, but its XML is not used as
the packaged gate: Unity leaves full assembly `testcasecount` values on pruned
parent suites in a filtered report, so the strict evidence verifier correctly
rejects that report as internally inconsistent. The subsequent unfiltered run
is the accepted, packaged evidence.

The batch run completed successfully using the installed Test Framework; the
interactive Editor was closed and no Editor owned this project during either
automated run. The installed framework package declares no dependency on
`com.unity.editor.headless`, and the official Unity 6.3/Test Framework command
reference documents `-runTests` plus `-testPlatform EditMode` without a
headless-package prerequisite. The licensing client resolved the existing
Editor entitlement and the batch run completed; no license activation or
credential workaround was used.

## Captured compatibility facts

The retained Editor outputs confirm `Mathf.Epsilon` is binary32
`0x00000001` with flush-to-zero disabled for this host. `Mathf.Approximately`
returns true at seven epsilon units and false at eight at zero; relative
threshold neighbors are recorded at five scales. For the production `0.8f`
Pow input, exponents 462–465 return the minimum positive subnormal and 466
returns zero. Negative exponents -394 through -397 remain finite, while -398
and below overflow to positive infinity. The tested signed-Int32 inputs also
record the float-conversion ties at both extrema and around `2^24`.

`JsonUtility` preserved isolated control characters U+0001–U+001F through
single-character serialize/deserialize cases, while a string beginning with
U+0000 serialized as empty. It preserved a valid supplementary surrogate pair;
isolated high and low surrogates serialized as U+FFFD. In the actual
commercial-sharing `TryRead` path, the tested duplicate string and array keys
kept the first occurrence; null and empty string values both became empty;
an isolated high surrogate was rejected, while an isolated low surrogate was
accepted as empty. Plan null and empty arrays both read as empty arrays. These
are observations for the selected Unity build and current provider reader, not
a broader JSON or persistence policy.

## Decision and scope

The bounded design exits the source-preparation window with two separately
reviewable compatibility surfaces: (1) one engine-free codec for the frozen
P18-D `Plan` and `Edge` causal payloads, and (2) one engine-free numeric
compatibility surface for the exact `Mathf` overloads found in the selected
runtime/restore source graph. Both preserve existing inputs and outputs; neither
creates a second simulation implementation.

The first implementation slice may contain only these Core values/codecs and
their golden tests, plus the narrow Unity provider call-site adaptation needed
to consume the same Core codec. The codec slice is separable from owner/runtime
migration. Numeric-helper implementation can proceed only after the reviewer
accepts the complete domain and vector record below; its helper is used as
selected owner sources migrate, not as an alternate calculation path. No owner
or writer is moved by this design.

This design does **not** claim production-source migration readiness for the
303-file Unity Application-CSharp restore closure, complete .NET host readiness,
or CORE-F closure. It does not change P12, profile membership, wire descriptors,
save compatibility, owner/writer/revision/shared-epoch authority, operation
admission, restore staging, tokens, leases, or causal ordering. It does not add
cross-host save compatibility, P13 history/fork, CORE-X, or another runtime.

## Existing API and composition boundary

The existing `Simulation.Core` assembly is `netstandard2.1`, C# 9, deterministic,
and `noEngineReferences`. Unity consumes the package assembly; the .NET project
uses explicit Compile links. The current project has **28** links at
`src/Simulation.Core/Simulation.Core.csproj`; this is verified in the promoted
source-closure packet and replaces stale historical prose that says 14 or names
`src/Simulation.Core.csproj`. No files are counted as moved based on the graph.

`DailyV1DefinitionAdapter.TryCreate` remains the Unity authoring boundary. It
resolves Unity assets/defaults and returns copied immutable
`DailyV1EffectiveDefinitions`; shared composition accepts that Core value
directly. The existing Unity-facing `SimulationGenesisPipeline` signature can
remain as an adapter while its pure fingerprint/genesis work consumes Core
values. Do not add a second mutable definition authority, duplicate defaults,
or Core references to `ScriptableObject`, `Application`, `UnityEngine`, scenes,
or Unity package assets.

The selected source graph has 329 Unity `Assembly-CSharp` Assets nodes, 2,990
deduplicated file-pair edges, 128 SCCs, 235 runtime-reachable nodes, 303
restore-reachable nodes (68 restore-only), and 26 nodes outside both closures.
The bins (236 mechanical `CoreMove`, 67 `SplitHostDomain`, 26 `HostOnly`) are
planning evidence only. The graph excludes Package and existing Core source
nodes and is not a type/method-level or owner-completeness proof. Recompute it
after each actual split. Do not move an SCC wholesale solely due to its graph
label. The 67 Unity-symbol-bearing sources require per-type/method adapter
boundaries; host/UI/logging/editor/Unity authoring stays in Unity.

## Frozen P18-D payload codec

The existing `P18DCommercialSharingBoundaryStepProvider` produces two causal
payload shapes. Current immutable wire fields and order are:

| Shape | Field order |
|---|---|
| Plan | `runtimeRoster`, `participants`, `edges` |
| Roster member | `runtimeId`, `personId` |
| Participant | `runtimeId`, `personId`, `cityRuntimeId`, `locationRuntimeId` |
| Edge | `senderRuntimeId`, `receiverRuntimeId` |

The reviewed Core API is the handoff's closed, typed surface:

```csharp
EncodePlan(IReadOnlyList<RosterMember>, IReadOnlyList<Participant>, IReadOnlyList<Edge>)
TryDecodePlan(string json, out Plan plan)
EncodeEdge(string senderRuntimeId, string receiverRuntimeId)
TryDecodeEdge(string json, out Edge edge)
```

`Plan`, `RosterMember`, `Participant`, and `Edge` are immutable Core values.
The exact namespace/type spelling may follow package conventions, but there is
one API and one codec implementation shared by both hosts. No general JSON
serializer, reflection model, runtime-type dispatch, per-host serializer,
version negotiation layer, or P12 JSON save format is introduced. Keep source
under `Packages/com.simulation.core/Runtime/`, explicit .NET inclusion in
`src/Simulation.Core/Simulation.Core.csproj`, and tests in the existing Core
test project. The Unity provider translates its private transport DTOs to/from
these values and retains its current roster discovery, merchant ordering,
pairing, frozen snapshot, edge ordinals, operation kinds, step IDs, owner
validation, replay and commit authority.

The accepted byte contract is compact UTF-8 JSON generated with the current
Unity field order, array order, escaping, and null/empty representation. Golden
payloads are bound in
[`JSON_PAYLOAD_ORACLE.csv`](../validation/CORE_F_SharedRuntimeSource/JSON_PAYLOAD_ORACLE.csv)
to exact UTF-8 byte count and SHA-256. The original portable fixture is
[`OracleCaptureTests.cs`](../validation/CORE_F_SharedRuntimeSource/OracleCaptureTests.cs);
the integrated Unity Editor fixture used for the accepted run is
[`CORE_F_SharedRuntimeOracleCaptureTests.cs`](../../Assets/_Project/Tests/EditMode/Editor/CORE_F_SharedRuntimeOracleCaptureTests.cs).
The provider fixture captures actual production-generated Plan bytes and Edge
bytes. The expanded Editor run captured quote/backslash, non-ASCII BMP,
escaped line feed, every U+0000–U+001F control character, null fields, valid and
isolated UTF-16 surrogates, duplicate object and array keys, null-versus-empty
strings, and the production reader's corresponding `TryRead` outcomes. The
hash-bound output records are in
[`UNITY_ORACLE_CAPTURE_RESULTS_d8b4c40e.csv`](../validation/CORE_F_SharedRuntimeSource/UNITY_ORACLE_CAPTURE_RESULTS_d8b4c40e.csv);
the exact fixture and packaged full report are identified above. These cases
were captured by the accepted unfiltered Editor run and independently checked;
they are no longer pending.

The decoder must preserve the current reader's outcomes for compatible generated
payloads and legacy cases rather than silently tightening admission under the
same operation version. The following `JsonUtility.FromJson<T>` outcomes were
captured in the accepted full Editor run and are bound to its preserved raw
report and result records:

- empty, whitespace, incomplete object, top-level array, and `null`: false with
  null output;
- `{}`: true with a Plan whose three array fields are null;
- omitted fields/defaults, empty arrays, and unknown fields: true;
- trailing non-JSON data: false;
- an array field with a scalar value: true with that field left null;
- For the captured `runtimeRoster:[null]` input, decoding succeeds and produces
  one non-null default-valued roster member whose `runtimeId` and `personId`
  are null. The row is retained. This is a decode result; existing provider
  validation rejects the incomplete member before authoritative mutation.
  Other captured reader outcomes retain their previously approved scope.
- missing edge strings remain null; a numeric string field is converted to
  its decimal text by the current reader.

These parser observations are not a new accepted-domain policy. Existing
provider validation remains responsible for rejecting incomplete roster,
participant, edge and identity state before owner mutation. Preserve error
classification and no-mutation behavior on `TryDecode*` failure. The attached
Unity oracle records all control escapes, UTF-16 surrogate cases, duplicate-key
resolution, and null-versus-empty behavior; Core golden tests must preserve
those observed outcomes. If the closed parser cannot reproduce one, report the
mismatch rather than changing the operation version or silently tightening a
same-version reader.

## Numeric compatibility surface

The exact selected closure contains 129 `Mathf` callsites across 18 source
files: `Max` 81, `Min` 23, `Clamp` 4, `Clamp01` 2, `FloorToInt` 8,
`RoundToInt` 4, `Approximately` 6 and `Pow` 1. The line/expression/source-blob
matrix is [`NUMERIC_CALLSITE_MATRIX.csv`](../validation/CORE_F_SharedRuntimeSource/NUMERIC_CALLSITE_MATRIX.csv);
reference outputs and binary32 bit patterns are
[`NUMERIC_ORACLE_VECTORS.csv`](../validation/CORE_F_SharedRuntimeSource/NUMERIC_ORACLE_VECTORS.csv).
All 129 are catalogued, including five host/report-only expressions in
`SimulationLogger` and `TesteSimulacao`; those are not authoritative domain
calculations, but retain their host behavior. The remaining 124 are candidate
domain/runtime calls and must be used by the same owners after their source
groups pass their own migration gates.

Overload identity is part of the contract: float and integer binary `Max` and
`Min`, three-integer `Min`/`Max`, float/int `Clamp`, `Clamp01(float)`,
`FloorToInt(float)`, `RoundToInt(float)`, `Approximately(float,float)`, and
`Pow(float,float)`. Do not replace by integer arithmetic, a differently typed
overload, or a mathematically equivalent expression with different rounding.
Current captured behavior requires at least:

- float Max/Min operand-order behavior for NaN and equal signed-zero operands;
- comparison-order Clamp behavior, including reversed limits, and Clamp01's
  NaN pass-through;
- round-to-nearest, ties-to-even; floor toward negative infinity; and the
  observed Editor/Mono conversion result `int.MinValue` for NaN, infinities and
  out-of-range inputs;
- `Approximately` near zero, at relative scale, for equal infinities/NaN and
  signed zero, with exact threshold and bit-neighbor cases;
- Pow outputs by binary32 bits for its generic reference cases and the
  production base `0.8f` over nonnegative and negative integer exponent edges.

Callsite domains have **not** been generalized from expected gameplay. The
matrix records each source blob, line, overload and expression; the review's
domain audit distinguishes proven guards from unknown bounds. Most utility,
preference, configured-float, price, money, and score operands lack a complete
finite upper bound at the callsite. `CityRuntime` rounding consumes configured
float products; floor-to-int paths consume owner/config-derived affordability
values. Preserve the present validators and invalid-state rejection; do not add
clamps as a migration convenience.

The sole production Pow call is `Mathf.Pow(0.8f, failedAttempts)` in
`CrimeSystem.TryCreateEscapeAction`. `failedAttempts` is read from
`JusticeSystem.GetFailedEscapeAttempts`; restore construction rejects negative
values, but the live `PrisonSentenceRuntime` increment is unchecked and can wrap
at `Int32.MaxValue`. Therefore this design treats the actual input as the full
signed Int32 domain after float conversion, not merely the ordinary nonnegative
range. Implementation must match Unity for every representable input used by
that callsite, including overflow/wrap behavior and subsequent multiplier
validation; do not narrow it to `0..N`, repair the counter, or change gameplay
semantics in this source-cut checkpoint.

The proposed single helper surface is a set of exact static overloads under
`Packages/com.simulation.core/Runtime/Numerics/`, with the same methods used by
Unity adapters and Core owner sources after their migration. Candidate algorithms
are fixed as follows; exact parity tests remain a required implementation gate:

```csharp
Max(float a, float b)       => a > b ? a : b
Min(float a, float b)       => a < b ? a : b
Clamp(float v, float lo, float hi) => v < lo ? lo : v > hi ? hi : v
Clamp01(float v)            => v < 0f ? 0f : v > 1f ? 1f : v
Approximately(a, b)         => Abs(b-a) < Max(1e-6f * Max(Abs(a), Abs(b)), 8f * Epsilon)
Pow(float a, float b)       => (float)System.Math.Pow((double)a, (double)b)
```

Max/Min and Clamp must retain binary32 comparison/operand order, including NaN
pass-through and equal signed-zero selection. Integer overloads use the same
ordered comparisons in `int`, with the current three-argument Min/Max folded in
the same left-to-right operand order. `FloorToInt` takes a binary32 input,
computes mathematical floor, and returns `int.MinValue` for nonfinite or
out-of-range conversion, matching the captured Unity Editor/Mono results.
`RoundToInt` uses ties-to-even and the same nonfinite/out-of-range sentinel.
Every intermediate stays binary32 except the explicitly exact floor/round
conversion needed to detect range before casting.

For the `Epsilon` term, a read-only probe of the installed Unity
6000.3.9f1 `UnityEngine.CoreModule.dll` (SHA-256
`987C9CE95133E347A0D91AE90C9E306F1EDDA38A9FD0D79E6359CE13F442FD07`) found
`Mathf.Epsilon` as a runtime-initialized static field. In this Windows x64
probe process, `UnityEngineInternal.MathfInternal.IsFlushToZeroEnabled` was
false, `FloatMinDenormal` was binary32 `0x00000001`, and `Mathf.Epsilon` was
`1.401298E-45` (`0x00000001`). The managed source selects the denormal value
when flush-to-zero is disabled and the minimum normal value otherwise. The
Core candidate constant for the selected Windows host is therefore
`System.Single.Epsilon` / binary32 bits `0x00000001`; do not substitute the
minimum normal value. The reflection probe itself did not run inside Unity
Editor; the accepted Editor oracle separately confirmed the effective
flush-to-zero flag and value for this validation host.

The accepted Unity Editor oracle independently confirmed the selected
6000.3.9f1 behavior. It recorded `Mathf.Epsilon` as `0x00000001` with
flush-to-zero disabled, all 32 `Approximately` cases including the absolute
`8*Epsilon` floor and binary32 neighbors around the relative threshold at five
magnitudes, and all 65 `Pow` cases. The expanded
[`NUMERIC_ORACLE_VECTORS.csv`](../validation/CORE_F_SharedRuntimeSource/NUMERIC_ORACLE_VECTORS.csv)
contains all 172 numeric result records from the final run: each operation and
payload matches exactly one hash-bound capture row. This includes the 23
expanded threshold vectors and 52 signed-Int32 escape vectors that were absent
from the earlier compact file. Its SHA-256 is
`2A5D2C653CEC82421936939A0FFBABABBB2F12B8834AFA645B890D884DEAF2F1`. The
complete run output remains in the capture CSV linked above. Independent
numeric comparison reproduced all 172 captured results with the proposed
ordered-comparison, floor/round, `Approximately`, and `System.Math.Pow`
algorithms.

The only production `Pow` caller has base `0.8f` and a signed-Int32 source
before float conversion. Inspection of the installed Unity 6000.3.9f1 managed
`Mathf.Pow(float,float)` method body resolves its call to `System.Math.Pow`; its
IL converts both inputs to double, calls that method, and converts the result
back to binary32. The Editor oracle confirmed the distinguishing subnormal
outputs: `Mathf.Pow(0.8f, exponent)` returns `0x00000001` at exponents 463,
464 and 465, while `MathF.Pow` on the same inputs returns zero. Therefore the
shared candidate is the explicit double `System.Math.Pow` call followed by one
binary32 conversion; repeated multiplication and `MathF.Pow` are not allowed.
Implementation golden tests must compare exact result bits across the captured
subnormal/zero and finite/infinite transitions, representative interior values,
exponent ties after int-to-float conversion, and both Int32 extrema. If Unity
and Core differ on any admitted callsite value, reject the algorithm and revise
the design before merging code. Golden tests compare `SingleToInt32Bits`, not
formatted decimal text.

If the approved implementation cannot reproduce current Unity outputs over the
actual admitted domains, the exact callsite remains on the Unity side until a
compatible algorithm is designed. No broad source move is a precondition for
the codec slice. This prevents an unverified numeric approximation from entering
authoritative daily execution.

## Runtime, owner and persistence boundaries

P12 is formally closed for same-build/current-host `UnityBootstrap-Daily-v1`.
The closed owner sets, IDs, causal inputs, ordering, rejection outcomes and
single-publication behavior remain the source contract. In particular:

- `SimulationRuntime` remains the only owner of owner-thread binding, operation
  scopes, mutation epochs, advance lease, successful completed-boundary token,
  restored-boundary admission, late owner recapture, private staging and one
  publication boundary;
- do not construct a second runtime, command scheduler, owner registry,
  mutation counter, save slot, image format, or P12 coordinator in Core;
- keep `Application.persistentDataPath`, Unity build/runtime identity, scene and
  asset reads, UI, logging, and session facade in Unity. .NET host supplies a
  truthful v2 host identity and explicit storage root; Unity v1 descriptor/slot
  remains byte/behavior compatible and rejects v2 as specified;
- existing shared `AtomicFileSlot`, v1/v2 descriptor and persistence codecs are
  reused, not duplicated. Unity P12 image owners remain binary typed sections;
  the P18-D frozen JSON boundary is separate and not claimed to be a P12 section;
- selected owner migration requires that owner's source, all live writers,
  identity/cardinality, revision/epoch propagation, export, staged hydration,
  relationships, validation and tests be present together. The promoted 89-family
  owner matrix and 24-route map remain prerequisites, not migration permission.

`SimulationTime` remains in Unity during this cut because its callback,
mutation-guard and advance-admission semantics are coupled to `SimulationRuntime`.
The `P12GDailyV1RestoreCoordinator` → `TesteSimulacao.CreateNpcChronicleFormatter`
dependency remains a follow-on seam only after all referenced owner types move;
preserve display lookup, ordering, and missing/ambiguous behavior. No blanket
`InternalsVisibleTo`, engine stubs, public state setters, or wholesale SCC move.

## Source/API and integration sequence

1. **Compatibility foundation (independent, narrow):** add the typed frozen
   codec and its golden tests to the existing Core package/project. Add numeric
   helpers only after the vector/domain reviewer confirms exact method semantics;
   do not route migrated owner calls until they are covered. Keep behaviorally
   unrelated owner, runtime and restore sources in Assets.
2. **Unity provider adapter:** route only `P18DCommercialSharingBoundaryStepProvider`
   encode/decode through the shared codec. Preserve private discovery and
   authority. Prove old/new bytes and decoded outcomes match in both Core tests
   and Unity EditMode; reject before any owner mutation. This single adapter
   handoff is an integration hotspot but does not modify `SimulationRuntime`.
3. **Owner groups:** schedule only complete promoted-owner groups after current
   source, writer, operation, revision/epoch, census, codec, relation, staged
   hydration and validation evidence show closure. Split concrete Unity APIs at
   narrow adapters. Recompute compiler source/reference graph after each group;
   classify edges rather than treating the inventory as a move list.
4. **Shared runtime/restore (serialized):** integrate only after source owners
   and their capabilities are Core-ready. Keep one `SimulationRuntime` owner;
   move/split construction behind adapters while preserving exact admission,
   epochs, owner thread, lease/token, recapture and publication behavior.
5. **.NET host and parity (later dependency):** use the fixed net8 proving host,
   same package source and selected immutable Daily-v1 values. Verify host identity
   v2, detached restore and authoritative same-input continuation at T/T+1/T+2.
   This is a dependency of full CORE-F, not claimed by this source design.

Do not make project/asmdef-only source duplication. Each selected shared source
must compile exactly once through the package in Unity and once in the explicit
.NET build. Keep Unity code in the Unity application/adapter assembly; the Core
assembly must keep `noEngineReferences` and have zero UnityEngine references.

## Validation and rollback contract

For a codec or numeric source change: run focused Core golden tests on .NET
cold and warm where state applies; the corresponding Unity EditMode parity and
rejection tests; ALL EditMode; official Smoke; and `git diff --check`. Use the
current TRX-EVIDENCE-v1 route for new .NET reports and current Unity evidence
packager for Unity runs. Changed executable or test source invalidates affected
prior run applicability. Run CORE-F long-run validation only when a migrated
owner/daily loop changes long-horizon behavior; do not invent a long-run gate
for a standalone codec without a domain behavior path. Exact-test inputs, host,
toolchain, report identity and hashes stay bound in the validation record.

After an owner source group, rebuild the graph and run its focused and full
affected regressions. After shared runtime, restore, or host composition
integration, validate the complete selected profile and exact T/T+1/T+2
continuation with same authoritative inputs, as well as rejection and
failure-atomicity cases. Compare Unity and .NET outputs using canonical
authoritative fields and exact identity/revision/sequence state; do not compare
diagnostic logs or serialize via a second runtime.

Preserve source GUIDs and public Unity signatures at adapters. Roll back with a
normal additive revert and the matching prior host/build. Never rewrite shared
history, overwrite historical evidence, mix host/build identity, or convert an
existing Unity v1 save into .NET v2.

## Readiness matrix after design review

| Work | Readiness |
|---|---|
| Frozen P18-D codec design | `INDEPENDENT_DESIGN_REVIEW_PASS / CORE_CANONICAL_ADOPTION_PENDING`; the full Editor run and hash-bound output record cover controls, surrogate handling, duplicate-key behavior, null/empty behavior, and the actual legacy reader. Production implementation is outside this design and requires separate authorization after exact-tip architecture/Core adoption; it must pass the specified .NET and Unity parity gates before promotion. |
| Numeric compatibility helper | `INDEPENDENT_DESIGN_REVIEW_PASS / CORE_CANONICAL_ADOPTION_PENDING`; the preserved Unity vectors cover all selected overload families, threshold neighbors, and Pow transitions including full signed-Int32 conversion edges. Production implementation is outside this design and requires separate authorization after exact-tip architecture/Core adoption; it must pass the specified .NET and Unity parity gates before promotion. |
| P18-D provider call-site switch | `WAIT_DEPENDENCY` on the reviewed Core codec implementation and Unity parity test. |
| Any owner source migration | `WAIT_DEPENDENCY` on complete owner-group source/writer/epoch/census/export/hydration/relation closure. |
| Shared runtime, restore composition, .NET host | `WAIT_DEPENDENCY` on owner-group migration and serialized integration contract. |
| Full CORE-F | `WAIT_DEPENDENCY`; requires the host, owner composition, detached restoration, exact continuation and authoritative execution comparison. |

No phase or checkpoint is closed by this artifact. The previous 14-source
description and incorrect project path are historical documentation drift; the
current direct Compile inventory is 28 at the path cited above. No executable
source, test input, profile, or owner contract changed as part of writing this
design.
