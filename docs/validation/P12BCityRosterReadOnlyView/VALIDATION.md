# P12-B City roster read-only view validation

**Status:** implementation validation PASS; awaiting independent exact-tip
code review. This is a bounded selected-profile roster immutability
correction. It does not complete P12-B or make P12-A implementation-ready.

## Candidate identity

- Canonical base: `69a5a41ca879fb66ce62efbe0d62e31f7298675b` on
  `codex/phase12/canonical`.
- Code candidate: `256c443903fd3e33ed05cfe4d86a11b3467176a2`.
- Code tree: `42ea8bfda3d95a57965192f550c58be226bf0004`.
- Reviewed design: `24522619b4418b9f506f9a59aec21eb188848560`.
- Independent design review PASS:
  `a39039eecdc95e4ea9e2160d72dd57df1ab3b3c0`.
- Unity: `6000.3.9f1`; the existing worktree `Library` was reused.

The code retains `SimulationRuntime.Cities` as `IReadOnlyList<CityRuntime>`,
returns one retained read-only wrapper around the privately owned sorted City
list, and preserves the exact City references and order. The selected
Daily-v1 composition regression checks the `IList<CityRuntime>` read-only flag,
rejected Add/index assignment/Clear operations, unchanged owner references,
and unchanged RuntimeIdentity City cardinality/revision/owner identity.

## Exact-code-tree validation

All Unity results below ran against code commit `256c443` and tree `42ea8bf`.
The selected profile is the P9-B-only `Simulation-DailyV1.asset`; the test
confirms `authoredP10RuinSite` is absent. The full EditMode run also confirms
the separate `Simulation-GeneralTest.asset` P10-A proving profile remains
available.

| Gate | Result | Artifact | SHA-256 |
| --- | ---: | --- | --- |
| `SimulationBootstrapCompositionTests` | 24/24 | `focused-composition/SimulationBootstrapCompositionTests.xml` | `FE50D500AF4EBEC1EF871551AB4FFBA2410A4077B5546B2AC65BFC5636826748` |
| Exact Daily-v1 authored geography, 253-section inventory case | PASS | Included in focused composition XML and ALL EditMode XML | — |
| `SimulationRuntimeAdmissionTests` | 50/50 | `profile-admission/SimulationRuntimeAdmissionTests.xml` | `73EACAE5E2C53AF9AC7C2F5BE241FE235DC4842ABAFF88B63AD0881A245EA9E6` |
| ALL EditMode | 2417/2417 | `all-editmode/AllEditMode.xml` | `B70AE2847D5C1649D2F7B01488354FD6DACE162CD977E3E207C5C8FA5FD16D8F` |
| Official Smoke (`-testFilter Smoke`) | 5/5 | `official-smoke/OfficialSmoke.xml` | `0B1E2F845AEFB2B81B2992D3F39CF22EADFF08E99C1EE7551C3FEC388C9D3ABD` |
| `git diff --check` | PASS | Candidate diff against canonical base | — |

Raw Unity logs for all four runs are retained in `raw-logs.zip` (SHA-256
`4117A502002D26DDB43A6CAC7AFBF6F16F67AC35E0916147400A6D5D6111BBFA`).

Committed source blob byte hashes:

| File | SHA-256 |
| --- | --- |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `DE57827FAF0FC43EFF077B3208AB28E6FA7CA489AFA9BB0F51456F5AC79D6B94` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs` | `13F44D0289773C14EACF5498DA86EFA2CCD1EA6B667E695384F966A12755A532` |

## Scope and status limits

This correction prevents consumers from changing the composed City roster by
casting the public read-only API back to its underlying mutable list. It adds
no City creation/removal operation, new census section, revision, or shared
epoch behavior. A future supported City lifecycle requires a separately
accepted operation and invalidation contract.

Daily-v1 remains a dedicated P9-B-only profile. The P10-A Ruin/LocalTopology
proving profile remains separate. This evidence does not establish complete
owner coverage, complete shared-epoch coverage, global quiescence, capture
eligibility, export, hydration, P12-A readiness, P12-B completion, P13
readiness, or Phase 12 closure. P12-B remains `INCOMPLETE`; P12-A remains
`WAIT_DEPENDENCY`; P13 remains blocked.
