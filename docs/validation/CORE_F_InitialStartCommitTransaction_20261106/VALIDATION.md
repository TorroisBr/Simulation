# CORE-F initial-start commit transaction validation

## Scope

The existing Core application now selects the initial genesis or initial restore commit route from its exact `DailyV1ApplicationStartPreparation`. Unity genesis publication and the initial restored-session projection route use this shared application method. The host still constructs the Unity projection and supplies the runtime boundary. This does not expose public `TryStart`, move Unity projection/restoration adapters into Core, or complete CORE-F.

## Source identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Source base: `1f0204156f2ae9af4b0e21c854a8c0b9d9e7bc18`
- CoreValidation working-input digest: `417ef212f2f39551dfb545d9fd16e72e445935bd9b76225a321b4b228dc2d516`
- Modified source/test file SHA-256:
  - `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs`: `99dd00e2daeb731fad72b315f5aa3038a1578722f4ed3066196fcd19e0634488`
  - `Assets/_Project/Scripts/TesteSimulacao.cs`: `98d8b6bbd490e5746090add24dfd5ddbd922de94b03a3b1360021d0f2aa81ccd`
  - `tests/Simulation.Core.Tests/DailyV1ApplicationHostTests.cs`: `e4eba34fad2ac13a81e4ead816d3feabc273f25f8634d76b66a0ae1bd0c15013`

## Test results

| Gate | Result | Run identity |
|---|---:|---|
| CoreValidation cold | 1,023/1,023 PASS | `5cd4c3ac9a5e496d94c19c7eeb622572`; private manifest SHA-256 `1e3c69570369e5e956f7710ef28a68f17a4c09ddde82ca3d6301037bb8eb63ca` |
| CoreValidation warm | 1,023/1,023 PASS | Same run |
| Focused selected NoSave startup | 1/1 PASS | Official EditMode filter |
| Full Unity EditMode | 3,300/3,300 PASS | Official base EditMode runner |
| Official Smoke filter | 5/5 PASS | All five exact test identities/outcomes also appear as passed in the full EditMode result |
| `git diff --check` | PASS | Checked after source changes |

Unity raw XML SHA-256 values: focused `0e48ced8884fa5940cbc4a1b487305e3d3ff13b385b8eb7b52006a8d78e8cf8f`; full EditMode `a8510501136e05bba5f25cdbbf7cdfb3e5db2624934eee2499601355bc2fb2f6`; Smoke `e00c28f56d280fb429fa6a35cc3b03e2612736bf5b5bd741d11b9f52445bbeca`.

## Evidence packaging limitation

The raw Unity reports and logs are retained in a newly created owner-only local evidence directory. The approved private evidence wrapper stopped before launching Unity because it requires PowerShell Core 7.6.5; the available Codex PowerShell is 7.6.9, and no 7.6.5 installation was present in the checked standard or existing Codex runtime locations. The repository's official base Unity runner was used for the test execution, but no run manifest or public Unity evidence package was fabricated. CoreValidation's raw TRX and private manifest also remain protected; its approved public TRX adapter still requires the unavailable `dotnet-trx-source-context-v1` record. These test counts are operationally successful, but this checkpoint does not claim evidence-package-complete acceptance or canonical promotion.

The first CoreValidation attempt exposed one incorrect test expectation (it expected the already-bound host boundary to reject publication); that test was corrected to assert successful publication of the exact prepared Core session, and the complete cold/warm run then passed. The failed original run remains unmodified in private retention.
