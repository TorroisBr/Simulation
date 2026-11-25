# Selected Daily-v1 arrival knowledge incremental validation

This record covers the incremental Core authority move that applies arrival
discovery to the exact selected `SpatialKnowledge` owner before publishing the
per-NPC travel result. It is recovery evidence for an incomplete CORE-F
candidate; it does not claim full runtime, persistence, or CORE-F validation.

## Source identity

- Candidate base: `62d27581fe1b3b9255dbe810be07b53bb3465551`
- Unity and Core tests were run after the four listed source/test files reached
  the following SHA-256 identities. The Core rerun occurred after the Unity
  result; no source or test file changed between the two runs.

| File | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedRuntimeOwnerGraph.cs` | `A0855327200D44C23957B202E2EAF84EF6A95EE958DD8681FC8DA8B2D266FC76` |
| `Assets/_Project/Scripts/TravelSystem.cs` | `8D7792FEEE08F76AF7960EAAB52B29CDD20659649DBAA0E7F877798B25CF4389` |
| `tests/Simulation.Core.Tests/DailyV1SelectedRuntimeOwnerGraphTests.cs` | `069A5951B6E8E9071C001E4E58FAEB5EBAD0285727C2D8CE6CCE143DE411962D` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `906747BD556B26B8A088D31E76FDFF8428DC9CB0BE82104BAEB7A63445401208` |

## Results

- Core: `dotnet test tests/Simulation.Core.Tests/Simulation.Core.Tests.csproj
  --no-restore --filter FullyQualifiedName~DailyV1SelectedRuntimeOwnerGraphTests`
  — 7/7 passed on `net8.0`.
- Unity EditMode: filter
  `SimulationRuntimeAdmissionTests.SelectedSoloTravelProjectsEachArrivalBeforeP12CensusInvalidation`
  — 1/1 passed, 0 failed, run ID
  `6b7a22e9-1fd5-48aa-9c88-561995d353be`.
- `git diff --check` — passed.

The Unity report's `test-run` result is `Passed`, with one selected case,
one passed case, and zero failures. The raw XML and log are retained unchanged
in the protected local evidence store. They are not included in this repository
record or candidate commit because the XML contains machine-local path data.
The retained originals were SHA-256 checked against the bytes produced by the
runner:

- XML: `473811D6D7D89FE05FB4553C65FEE6218CA98C3EA3223A225ABD4EFAAF2AC758`
- Log: `82B03A38AB5067CD720BBC247AD9C9FAB3888379EC048E7BFB874FAE4D6FAA2E`

## Earlier attempts

- `9f04b825-84ff-4e45-a885-4df16cd2e4eb` was a test-instrumentation failure:
  the test read a startup-only alias that normal runtime composition clears.
- `f079c183-3636-4af2-9493-dd371e5ee01b` produced no result XML after a
  compile error from the then-incomplete test edit.

Those earlier attempt artifacts are no longer present in the worktree's
`Temp/ValidationResults` or the protected evidence store. Their original bytes
could not be retained or reverified; this record preserves only their run IDs
and classifications and does not claim otherwise.

## Scope limit

This is focused incremental evidence only. It does not substitute for the
affected full Unity regression suites, the approved complete application/host
validation gate, an independent exact-tip review, or canonical promotion.
