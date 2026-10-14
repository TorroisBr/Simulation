# CORE-F prerequisite baseline execution validation

## Exact executed input

Core base: `8ea3b47e9ab1be9ab3fe6261a73dc33fece8cd8f`.
Executed integration commit: `2bd79fbc27fd82b1ce253d03558f0ac67d556949`.
Tree: `0baefca3457dafec961c24c6d643618d63a1f37c`.
Assets subtree: `96d4a520500302efbc9827685f35f2a1906677fe`.
Packages subtree: `2fbc3e11020f98a69b1ceaefd0e8f13a94c1dd47`.
ProjectSettings subtree: `db5c1636993b7f82f2409e07300df2744a5dfe85`.
UnityValidation subtree: `965c46eef5d172f96cec6a17fd39bdc18ffde1f0`.

Production is unchanged from the Core base. Added tests characterize Values and
exercise the Owners vector. The two NUnit compatibility corrections are present.
The three reviewed handoff documents and missing script metadata are additive.
This baseline is not validation of subsequent Persistence or Values production code.

## Executed gates

| Gate | Run ID | Result |
|---|---|---|
| Original-production full Values characterization | `1aea24e24b2547a3866a5d8e1086a6d6` | 1/1, stable captured inputs |
| Owners cumulative vector after two NUnit fixes | `8409311d988243908711bf22c13a65e8` | 1/1, stable captured inputs |
| ALL EditMode | `6c892ae7b7264385a1cc2095754f78fc` | 3134/3134, zero failed/skipped/inconclusive, stable inputs |
| Official EditMode `-TestFilter Smoke` | `3573a8c0b7e84d769f5506cd792d58d9` | 5/5, zero failed/skipped/inconclusive, stable inputs |

The ALL report includes the original seven SimulationRuntimeLongRunTests, with
100-, 1000-, and 5000-day cases unchanged and passing. It also includes all current
RuntimeAdmission, Bootstrap/P12, Values characterization and Owners cases. No .NET
live owner/runtime graph claim follows from these Unity tests. The .NET compiled
input closure remains unchanged at this baseline.

## Durable result evidence

`core-f-owners-baseline-all.manifest.json` and its same-basename ZIP retain the
complete normalized ALL result. Archive SHA-256:
`1cb1e334de32224b0b4a86a44aa916025c82ba86c74657f3c3f6537974815f74`.
The approved packager verified the package against the protected original run.
Raw XML/logs and the source run manifest remain unchanged in owner-only retention.
Capture and packaging use the same unchanged promoted UnityValidation tools.

The official Smoke identities are all present and passing in that durable ALL
report: the two GMConsole smoke cases, ObserverSceneCanRenderAndSelectNode,
ObserverPrefabSmokeInitializesWithoutMissingReference, and
ObserverPrefabSmokeHasNoMissingSerializedReferences. The separate official Smoke
XML SHA-256 is
`da4a635420efa8440270088177a21d09425b19305f164ff8ed0d7357486f6a28`.
Filtered report packaging is not claimed: the existing packager rejects Unity's
filtered-suite testcasecount/descendant inconsistency. The original filtered
reports were neither edited nor relabeled; complete durable case evidence is in
ALL. Packaging rejection does not alter test outcomes.

## Values baseline vector

The unchanged production root yielded 184 ordered P9 records, 39 P12 catalog
records, and 718 expanded occurrences for the 164 source-member declarations.
The focused and ALL runs produced byte-identical 212100-byte vectors, SHA-256
`422805cbe373cf1410ee6a928d363fa1ef413dbcf50dc062dce7867ed4fdd1b3`.
The vector and a supplemental manifest binding the source-member CSV blob/hash
are retained with the focused source run. The member CSV is additionally bound
by the executed Git tree; it is outside the Unity runner's default input roots.
The fixed .NET fixture/generator and field-by-field comparisons remain Values
implementation obligations; this capture does not itself establish parity.

P9 fingerprint: `4177863e9b01b20e8bf45adad5cd48e0416af658eebbf1185eb9deb3ec3cce21`.
P12 content fingerprint: `5e4a79c72967dd7e2a33aa300af722ed211a5a4ce4e0d2e87694d018f87f254a`.
Calendar's actual Unity-resolved values are 1/1/1. Preserve their observed semantics.

## Diagnostic attempts and remaining gates

The initial Values characterization failed by expecting only one config asset;
the explicit Daily-v1 path correction supersedes it. Owners initially had an
ambiguous NUnit comparer overload, then a Count-property assertion on a LINQ
iterator; both were corrected before the passing gates above. A diagnostic
`-TestCategory Smoke` invocation selected zero tests and is excluded from gates.
All diagnostic originals remain protected and unchanged.

Independent review requested the precise owner/provider/writer/capture/hydrator/
target/case crosswalk; passing tests do not close that finding. No package is
promoted by this record. Later executable changes require affected fresh gates
and independent exact-tip review. P12 scope/closure and CORE-X are unchanged;
CORE-F remains WAIT_DEPENDENCY on the actual runtime/restore/.NET continuation.
