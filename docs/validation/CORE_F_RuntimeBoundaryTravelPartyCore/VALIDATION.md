# CORE-F Core TravelParty owner and selected day stage — incremental validation

This is a recoverability checkpoint for the existing P8/P12-F TravelParty owner only. It is not a full CORE-F validation package, exact-tip independent review, canonical promotion, or completion claim.

## Source identity

- Candidate branch: `codex/core/CORE-F-runtime-boundary-3ec0aa`
- Base commit: `1e67ddab316e7496712b5d0f0d3d129d2e74f76f`
- The exact source/test manifest used for the validation runs is listed below. SHA-256 is over the UTF-8 encoding of these lines joined with LF and no final newline.
- Source/test manifest SHA-256: `527c70c5f00aaa9d02463b5edfce44dd82fbd6919dde20730d0912c6dbe53fa9`

| SHA-256 | Repository path |
| --- | --- |
| `e494e7e77cadb5b781b62de795a5ffd4503bbd40c71fa84ccead4eb7791c1ec1` | `Assets/_Project/Scripts/P12FCommitmentOwnerSnapshots.cs` |
| `d8eddacf374404c1adb5ae4b688c7dead19922fe839d7275da4f6c222b26cedc` | `Assets/_Project/Scripts/SimulationRuntime.cs` |
| `d7f28ed0b8ea51008f660d8cf7c357432e161be3a60920c69320dcd55b6633da` | `Assets/_Project/Scripts/TravelParty.cs` |
| `946515df2d85888194b2ed813f98e01755d0d0635bf869ed44faab6211671105` | `Assets/_Project/Scripts/TravelSystem.cs` |
| `5b826294a8e7285a4034d5d6f557b86ebad83aa2b142c19d185cba136c9d369e` | `Assets/_Project/Tests/EditMode/Editor/P12FCommitmentOwnerSnapshotTests.cs` |
| `590b6444f8c2063834d4a941b3e9936a2b69a7038d2e2829e074b7a132395fac` | `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedRuntimeOwnerGraph.cs` |
| `2e7e1a75a30ea2fce524db7ab68db5a1b46a209bc78d00b9b989c1ae81dc90df` | `Packages/com.simulation.core/Runtime/Travel.meta` |
| `1323b160fd10abd06b4b741f5efdd396f5a058c93cc77f6ce0dd087367a60713` | `Packages/com.simulation.core/Runtime/Travel/TravelPartyOwners.cs` |
| `e8f6a651f62ae7bd6f149ba2e05a08436c3be5cab0f5b7b0cc2a038529672d23` | `Packages/com.simulation.core/Runtime/Travel/TravelPartyOwners.cs.meta` |
| `458220fa33022a4a025c5d2a9437999ace278a62451cb2c28bbdcd863b8213ca` | `src/Simulation.Core/Simulation.Core.csproj` |
| `181e57c972122a86dfe18771e32251195d850b2fcba2d8701db689baa8d34462` | `tests/Simulation.Core.Tests/DailyV1SelectedRuntimeOwnerGraphTests.cs` |

## Behavior and validation

- `dotnet build src/Simulation.Core/Simulation.Core.csproj --no-restore`: PASS, zero warnings/errors.
- `dotnet test tests/Simulation.Core.Tests/Simulation.Core.Tests.csproj --no-restore`: 370/370 PASS on the final whitespace-normalized source.
- Unity `P12TravelPartyAdvanceTests`: 10/10 PASS, run ID `66a8448f343d43c8b3591a800ae8f810`.
- The Unity test source snapshot was stable for this focused run. Afterward, only the extracted C# file final blank line and trailing whitespace in the new Unity folder metadata were normalized; this does not change compiled behavior. The XML and log originals are retained unchanged outside the disposable Unity project Temp. XML SHA-256: `7e75be7178a31b86bf5f0e425de4a084ffd123c952c5812ae23ef29cb8d4d856`; log SHA-256: `1e66f8bc9d218d5348bc95db32c850a5c633d99e456593ae9e809bfcccad8e8e`. Evidence artifacts are not included in this repository commit.
- `git diff --check` on this checkpoint's selected paths: PASS. The earlier unredirected Core rerun exited without console diagnostics; the captured rerun immediately afterward completed successfully with the 370/370 result above.

An earlier Unity invocation produced no result XML because the P12-F Unity test still called the removed snapshot factory. Its original log remains retained unchanged. This was a stale test seam and was corrected to exercise the canonical `P12FTravelPartyOwnerSnapshot.TryStage` path. The final focused suite above passes.

## Scope and limits

The shared Core graph now owns the selected daily TravelParty transition against the same `TravelPartyStore` instance that P12-F snapshots stage. Member/party ordering, live NPC/travel/presence/Knowledge owners, arrival projection/invalidation order, and existing event read-model timing are covered at this boundary. This remains an intermediate owner-stage integration: SimulationRuntime still owns the outer ordered day, admission, lease, operation, completed-boundary token, and full P12 capture lifecycle. The Unity XML run is focused rather than a complete evidence-package report. No canonical promotion or CORE-F completion is claimed.
