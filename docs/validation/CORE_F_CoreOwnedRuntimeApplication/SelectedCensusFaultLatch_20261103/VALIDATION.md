# Selected census fault latch — validation

Validation date: 2026-11-03

## Candidate identity

- Candidate branch: `codex/core/CORE-F-try-start-transaction`
- Code commit: `60952e71d09d80ff72c3320d0ecdb6fbf7481acc`
- Code tree: `d09672938234a4804560b61b680a9a2cf16fc452`
- Parent: `ad05d8df802de3def31b8e4d2f048c220fd5a898`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

This bounded change makes the selected census protocol fault latch part of the Core execution-boundary state. Shared protocol checks read and write that latch; standalone protocol operation retains its local latch. The selected host-port interface no longer calls back into Unity to set Core-owned fault state. The legacy runtime admission path is unchanged.

The change does not alter census providers, the 89-family owner vector, mixed-roster fallback, writer/revision/epoch semantics, or capture and restoration payloads. It does not complete or promote CORE-F.

Changed code and test paths:

- `Assets/_Project/Scripts/ContinuationCensusProtocol.cs`
- `Assets/_Project/Scripts/SimulationRuntime.cs`
- `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`
- `Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs`
- `Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs`
- `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs`

## Validation results

| Gate | Result | Evidence |
|---|---:|---|
| CoreValidation cold | PASS, 1,003/1,003 | Official run `d3cd2800914047a9b612b3135714afcc`; clean E: worktree at the candidate commit; no Unity `Library` |
| CoreValidation warm | PASS, 1,003/1,003 | Same official run; cold/warm original TRXs compared to the normalized package |
| Focused selected census fault-latch test | PASS, 1/1 | Official Unity run `3dc88bd5f85b4a5cb44274c905432578` |
| Full Unity EditMode | PASS, 3,299/3,299 | Official run `4abd834d12b44abe968ce8e8e7264a34`; stable input snapshot |
| Official EditMode Smoke | PASS, 5/5 | Official run `8b6ef9716fc14693a425e14b3523972a`; all five case full names also occur in the full EditMode report |
| `git diff --check` | PASS | Candidate code commit against its parent |

The CoreValidation package is at `DotNetTrx/coref-selected-census-fault-latch-corevalidation-20261103.zip` (SHA-256 `5a52aa4d20bf103eebf3135c95a1544bfc6fbd3ae75aa73c71a531385a9efc3a`) with manifest `coref-selected-census-fault-latch-corevalidation-20261103.manifest.json` (SHA-256 `8abeabdff21e9e59b6826a7c270abc0fcc1b8530d19f6cadc01af72bfc66ba6b`). The official verifier reports `originalComparisonVerified`; both original report identities, cold/warm distinction, complete test identities and counters are preserved.

The full EditMode package is at `UnityEvidence/coref-selected-census-fault-latch-editmode-20261103.zip` (SHA-256 `2ba5433482d1764fab0603f282a201d9ab056a5655c1a17855daeb6fd5f2386f`) with manifest `coref-selected-census-fault-latch-editmode-20261103.manifest.json` (SHA-256 `c46fc1702ef64a16d8301eeafcee16ac1dc9ba55b4a885b40da313298487f7b4`). The official verifier accepted the stable full-run manifest. The public package privacy scan found no workstation paths. Original XML, logs, runner manifests, runtime traces, and private adapter context remain in protected E: evidence storage.

The focused Unity regression and filtered Smoke originals also remain protected locally. The full EditMode report contains all five filtered Smoke case full names. The two Unity snapshots differ only at the user-owned `ProjectSettings/ProjectSettings.asset`; that setting was not staged or changed by this candidate. The five Unity production/test inputs match their full EditMode input hashes. The .NET test file is not a Unity input; its exact committed blob and clean .NET runner input match.

## Evidence handling and limitations

An initial CoreValidation run from the dirty implementation worktree passed but could not be packaged: its captured Unity source change was outside the .NET compile-input inventory. The original report and manifest were preserved unchanged. The published TRX package instead comes from a clean detached E: worktree at the candidate commit; no report was edited and no source input was hidden.

The normalized public bundles contain no raw logs or private machine paths. The original test reports and diagnostics remain protected and unchanged. The filtered Smoke report is retained privately; the full EditMode archive carries the same five case identities.

Three existing local `ProjectSettings` changes remain unstaged and outside the candidate: `EditorBuildSettings.asset`, `ProjectSettings.asset`, and `ShaderGraphSettings.asset`. No unrelated or protected file was committed.