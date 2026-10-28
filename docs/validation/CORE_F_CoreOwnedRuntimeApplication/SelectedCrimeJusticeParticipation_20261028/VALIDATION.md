# CORE-F selected Crime/Justice participation validation

## Scope

For an admitted selected Daily-v1 execution, Core now treats its required Crime
and Justice day-start infrastructure as present and runs the existing ordered
Core phases from the admitted session contract. Unity's system-presence flags
remain the compatibility input only outside an admitted selected execution.
The mixed-roster whole-stage fallback, four separate P12 scopes, owner mutation
logic, and Unity mutation/projection ports are unchanged.

This is a validated candidate checkpoint only. It does not complete CORE-F,
publish `DailyV1Application.TryStart`, remove the remaining Unity action/
projection/P12 adapters, or constitute canonical promotion.

## Exact inputs

Base candidate commit: `96ff547d8489c4b311aec6d039f017f4a2fcadff`.

| Input | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedDailyExecutionStage.cs` | `6f875d95ed754f6e73e32f371dbd03730bdb6fd29b8da6bcb53efa8bebf51b34` |
| `tests/Simulation.Core.Tests/DailyV1SelectedDailyExecutionStageTests.cs` | `129e26402cf5190a663822835522a660d33c06616e2b882ab8aba4565459da47` |

The regression supplies `false` for both Unity system-presence flags while the
exact selected Core execution is admitted. It verifies Core still performs
`ClearArrestedToday`, `AdvanceHiddenStatuses`, `AdvanceSentences`, and
`SyncWantedStatuses` in that order, using the Core-bound roster. The existing
mixed-roster test continues to require exactly one whole-stage compatibility
fallback with no selected Crime/Justice or Merchant phase duplication.

## Validation

| Gate | Result | Durable private evidence identity |
|---|---|---|
| Official CoreValidation, cold and warm | 907/907 PASS in each run; input snapshot stable | Run `f8dd83520f894d2a8a11868c58fbbf9d`; manifest SHA-256 `f51d13b51f72150f1f7ff09e08ac2d8cc2c6a8d90596a851ae5a3adb5fb6c82e` |
| Official full Unity EditMode | 3276/3276 PASS; 7 LongRun cases included; input snapshot stable | Run `c25556ed7c3b4997942ab56362323af2`; input snapshot SHA-256 `85fd105040dde6aea4de11333ad5d6deddd8662148477ecb23fb73dd8a29cf70`; manifest SHA-256 `d00834bf8dba3f72cbfbd22b21c4735aedb682e88732a97bd14962944e9cccae` |
| Official EditMode Smoke filter | 5/5 PASS; input snapshot stable | Run `b3cdba635aae4ff5bd4e81506e9a9c38`; same input snapshot SHA-256; manifest SHA-256 `a4c10c529154ff284df3ce757a539820a91516273eb6a5efc3fbc35d145d01e1` |
| `git diff --check` | PASS | Candidate worktree |

The CoreValidation run used the official runner but its manifest records that a
Unity `Library` directory was present in this worktree; no Library-independent
.NET claim is made. Raw XML and logs remain in the protected local evidence
store. Only run identities and hashes are recorded here.
