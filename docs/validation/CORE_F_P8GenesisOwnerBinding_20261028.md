# CORE-F selected P8 owner binding — validation record

## Candidate scope

Validated against candidate base `4b025b3ad18dbf6283cc44ba6d809863b9686d9c`.

Core genesis now creates the selected `PersonSpatialPositionStore` against the
exact `PersonStore` and composed spatial authority, using the passage
traversal resolver, and binds it together with the exact
`LegacySpatialAnchorBindingStore` into the selected runtime owner graph before
that graph is admitted and published. The selected Unity Daily-v1 path retains
those exact graph-bound P8 instances and resolver instead of cloning or
reconstructing a competing owner. Nonselected compatibility construction keeps
its existing clone behavior. Genesis tests assert the exact store identities
and dependencies.

This does not widen the accepted P12 profile. Its P8 anchor and position rows
remain exact-empty. Tests using live selected-profile owner witnesses source
them from the graph-bound providers. The P14 local-material-flow rejection
fixture deliberately retains the accepted Daily-v1 empty P8 rows while testing
that the out-of-profile City state is rejected by its existing City capture
contract. No all-owner migration, P12 readiness, detached restore, .NET host,
or CORE-F completion claim is made.

## Exact source inputs

The following source and test bytes were unchanged between validation and this
record:

| File | SHA-256 |
|---|---|
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `8dbdba4c96fb47e979653e455f33e2f3cb224e84edbc2d89e70889bba56f7f51` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1GenesisComposer.cs` | `c3edf33af4621f932a96330a245fe9d20630d65825a15f08209b332766d14078` |
| `Packages/com.simulation.core/Runtime/Geography/PersonSpatialPositionStore.cs` | `342805956ba63c3e62dc5f6b5c6c64595d092085b2843307654d1dd75e78fad1` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs` | `10ee008824987244bf927bafc1bd0ad445d68fbef0aaf1fb351202238455aa67` |
| `tests/Simulation.Core.Tests/DailyV1GenesisComposerTests.cs` | `43824837480cfcd626703eab5f1daede5c275945b70e166ed63b2e3cfb3bb168` |

The CoreValidation input manifest identifies the working source revision as
`4b025b3ad18dbf6283cc44ba6d809863b9686d9c`, records stable inputs, and includes
the exact hashes above for the Core production and test files. The Unity
manifests record stable inputs and the exact hashes for the three production
files in its compile inventory. Original reports, logs, and private manifests
remain in owner-only evidence retention; no raw report or workstation path is
published here.

## Validation

| Gate | Result | Run |
|---|---:|---|
| Official CoreValidation cold | 918/918 PASS | `ead578eb9a9b41d683d61c4faf12bfeb` |
| Official CoreValidation warm | 918/918 PASS | `ead578eb9a9b41d683d61c4faf12bfeb` |
| Official Unity EditMode | 3275/3275 PASS; zero failed, skipped, or inconclusive | `bf7a4b009a724389a5d79f273c5b2067` |
| Official Unity Smoke | 5/5 PASS; zero failed, skipped, or inconclusive | `fe83dc5d16c8481ab65a43887089f717` |
| `git diff --check` | PASS | working candidate |

CoreValidation discovered 918 tests. Its cold and warm report hashes are
`981658e8accfe04b0286e03075a6be2a34bd9ca0cf884153a3aa83b653cdcd5e`
and `f68c2cb40399f54d3f85f6d3266e8128991f524b941e5de472ebe441e946acb5`.
The full Unity EditMode XML hash is
`84dd409813d79fe8e7bf26887fea1519d75f3ac94f80c299274058a4b8b129dd`
(3,272,809 bytes); the official Smoke XML hash is
`73a6444e7ff1bc465975ea1dba718384069d87826b7bb4090902e03c433aa49b`
(6,365 bytes). The Unity runner reported coherent counts and stable before/after
inputs for both runs.

## Review and limits

Independent exact-tip implementation review is pending for the candidate
commit containing this record. This validation is not Core canonical
promotion. Core canonical remains
`a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`; shared runtime/session ownership,
`DailyV1Application.TryStart`, detached P12 restoration, the fixed .NET host,
and separate-process T/T+1/T+2 continuation remain outstanding CORE-F work.
