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

| Gate | Result | Artifact | Unity-emitted XML SHA-256 | Committed Git blob SHA-256 |
| --- | ---: | --- | --- | --- |
| `SimulationBootstrapCompositionTests` | 24/24 | `focused-composition/SimulationBootstrapCompositionTests.xml` | `FE50D500AF4EBEC1EF871551AB4FFBA2410A4077B5546B2AC65BFC5636826748` | `D5F089469486B209DB3E3E16D1AB492B6532B6E8F00576C3D8C05A1BB26989BF` |
| Exact Daily-v1 authored geography, 253-section inventory case | PASS | Included in focused composition XML and ALL EditMode XML | — | — |
| `SimulationRuntimeAdmissionTests` | 50/50 | `profile-admission/SimulationRuntimeAdmissionTests.xml` | `73EACAE5E2C53AF9AC7C2F5BE241FE235DC4842ABAFF88B63AD0881A245EA9E6` | `778B8DEA9D0B8AA9E007D0B761097AAD053ABB2A84D9B65C18DA093D891649A4` |
| ALL EditMode | 2417/2417 | `all-editmode/AllEditMode.xml` | `B70AE2847D5C1649D2F7B01488354FD6DACE162CD977E3E207C5C8FA5FD16D8F` | `A905D399C440229101A79C57EB665DFDA8EC71C1F56A311C1C9BA1E4D8FB3F8B` |
| Official Smoke (`-testFilter Smoke`) | 5/5 | `official-smoke/OfficialSmoke.xml` | `0B1E2F845AEFB2B81B2992D3F39CF22EADFF08E99C1EE7551C3FEC388C9D3ABD` | `DEBC70919990FF2519B08E354EACF199897621B332AF5D6ED47C3F8056F19425` |
| `git diff --check` | PASS | Candidate diff against canonical base | — |

Raw Unity logs for all four runs are retained in `raw-logs.zip` (SHA-256
`4117A502002D26DDB43A6CAC7AFBF6F16F67AC35E0916147400A6D5D6111BBFA`).
The Unity-emitted hashes identify the exact result files written by the test
runner. Git normalizes the committed text XML, so the separate Git blob hashes
identify the exact committed artifact bytes.

SHA-256 over the exact committed source blob bytes at code commit `256c443`:

| File | SHA-256 |
| --- | --- |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `9FE524550CBA99537CB9238AEEC17BCFD5F7039FB1C6DF5D940D2964674193F0` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs` | `AB15AD8AEEF59EAB73C85257CE186F1AA0665D86AD883FEEE0F98F7EA9F838AC` |

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
