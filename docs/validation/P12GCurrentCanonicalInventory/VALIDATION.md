# P12-G selected Daily-v1 inventory witness validation

**Status:** validated test-only evidence slice; P12-G implementation remains `WAIT_DEPENDENCY`.

**P12 canonical base:** `02009f9063dd252bd4b177fd6aef1e74dcd947f5`

**Architecture baseline:** `47eff220c7ce00f6e7c759bdc2b76780bb46f628`

**Initial code candidate:** `a2a48da75b818133252f4cf51ce5beeb2fc4b5a0`

**Initial reviewed code tree:** `ec20bc3fae20f3bdb4dc7b89bed61373d41b4974` (`Assets`)

The test-only change adds an independent literal Daily-v1 section manifest and compares its exact IDs and roles with both the protocol's expected-section set and its live provider registrations. It applies the same comparison to the authored bootstrap fixture, NPC roster mutations, and Person registration/materialization. It checks the documented `61 + 22N + (N-M) + P + 4C` cardinality equation, exact-zero roles, witness identity stability, revision/cardinality shape, the unregistered ActorChoice temporal provider's shared owner identity and zero current inputs, allocator counters outside the vector, P10 LocalTopology absence, and known omitted read-model/root dispositions.

This records source and fixture evidence only. It does not claim exhaustive supported-writer or shared-epoch coverage, global quiescence, a completed-boundary token, target hydration, P12-G readiness, P12-A readiness, P13 readiness, or Phase 12 closure. Existing independent owner-specific tests continue to check source owner identity/cardinality for the fixed and dynamic provider families.

## Validation

Unity Editor: `6000.3.9f1`. Each result XML reports `Passed`, zero failures, zero inconclusive tests, and coherent counts.

| Run | Result | XML SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|
| `SimulationBootstrapCompositionTests` | 26/26 PASS | `C4E4C408C3882CFE660D1CBA7F0E6ED091F46FCFD6D79602D03D05BE5AB36609` | `91F9F416C8812FC60D82C6D9425D0577627EE6FAE027B6BC34082654E43CB373` |
| ALL EditMode | 2732/2732 PASS | `93065AC0671CCD64C72275178DBDBA271139C99103ABF4AAECCE6FEC0BB5C6E8` | `496DB3D6A5EB7CA49D9CEB245F598C95279C0DC2D72F4F29233536EC9BD7E138` |
| Official Smoke (`-testFilter Smoke`) | 5/5 PASS | `BE76BE75CC0E779B7A155295FE55EF0E5CC21623A57CFA0178C688C3B509E43E` | `311F99975C17982AF291174EE53C59420B72A2384D31858B5AD2B783004CD3A6` |

The XML and `.log.gz` artifacts are stored in the `Focused`, `AllEditMode`, and `OfficialSmoke` subdirectories. Commands used:

```powershell
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter SimulationBootstrapCompositionTests -ResultsDirectory docs/validation/P12GCurrentCanonicalInventory/Focused
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -All -ResultsDirectory docs/validation/P12GCurrentCanonicalInventory/AllEditMode
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter Smoke -ResultsDirectory docs/validation/P12GCurrentCanonicalInventory/OfficialSmoke
git diff --check
```

`git diff --check` passed after the test change. The two modified `ProjectSettings` files and three untracked `.meta` files in the worktree were not staged or changed by this candidate.

## Composed no-vector disposition follow-up — 2026-10-09

**Code commit:** `f1233a95090467694a8be810a3f6c1060175c402`

**Reviewed Assets tree candidate:** `653a88e6dc2cdc5654b7ae33594ac67cbce92d67`

The focused inventory test now verifies that the runtime holds the exact selected `DeterministicRandomSource`, its captured provider and seed agree with the P12-C root contract and genesis manifest, and no registered census owner is that source. It explicitly classifies the composed no-vector objects: WorldId, P9 genesis manifest, the shared SimulationTime boundary clock, CalendarDefinition profile metadata, deterministic-random root, and P8-E transaction coordinator. Clock identity and calendar dimensions/month lengths are checked against their current runtime and manifest counterparts. No production source or runtime behavior changed.

This closes the identified fixture-level no-vector classification gap. It remains source/fixture evidence and does not establish every supported writer, all reachable owner states, restored-boundary admission, whole-graph publication, or continuation parity. P12-G remains `WAIT_DEPENDENCY`; P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains open.

### Follow-up validation

Unity Editor: `6000.3.9f1`. Each XML reports `Passed`, zero failures/inconclusive tests, and coherent counts. Artifacts correspond to the code tree above.

| Run | Result | XML SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|
| `SimulationBootstrapCompositionTests` | 26/26 PASS | `BE51BAC6C5B9F36D4DEFFED2CC8A56DBD0F4778DE0052822839FFD6F369ABEDF` | `6E810B301E3309E9117F982701BAAE9F361603C27ABDBFE5BE287A415AC0B428` |
| ALL EditMode | 2732/2732 PASS | `C4BA383F1809734F9A7A0004628213BBB6FC85FD6E2AF0B262A0323C5F495639` | `087BED9BEB4F068B16B88C422FC249AD7044FD6B475996F42F3FCD684194F666` |
| Official Smoke (`-testFilter Smoke`) | 5/5 PASS | `3C49F6F152374734928E65D17B7D2D8AD6F7097B68AFE2AB748C748EA67E54D4` | `303E46C2E07C1BCB503D2A0ED383DBDB459E309B08C027D698B0B0B56BC892AA` |

The exact XML and `.log.gz` artifacts are stored in the existing `Focused`, `AllEditMode`, and `OfficialSmoke` directories. The successful runs used `Invoke-UnityValidation.ps1` with their corresponding filters; ALL EditMode used `-All`. `git diff --check` passed. The unrelated `ProjectSettings` edits and untracked `.meta` files remain untouched.
