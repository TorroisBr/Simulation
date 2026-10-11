# P12-G terminal ActorChoice and active TravelParty continuation parity

- P12 canonical base: `c8dd04ac5d002e68eb5d03db814615d3d35f70b7`
- Test implementation commit: `ff6961ded876b6e2b64eddecbe30c473470bc858`
- Implementation Git tree: `786c8b2ccb072ca8cd5a4e98e4e64b98edcea15a`
- Validated `Assets` tree: `ecd855a0f2d3c767bb494f2fe34112193381d190`
- Changed executable path: `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` only
- Unity Editor: `6000.3.9f1`

## Evidence scope

The new test uses the existing trusted LocalPlayer/Request SellGoods ActorChoice path with a Person-bound authored Daily-v1 merchant, plus the normal TravelParty API and authored route. The merchant is excluded from the proving party so the two domain owners advance independently. After both worlds reach the completed day-1 boundary, the test verifies a terminal `AttemptReturned/Succeeded` ActorChoice and an active party, then restores the same state.

Restored state must match the complete included-owner projection, continuation roots and owner-section vector. Re-submitting the same command ID/payload is rejected as a duplicate without changing ActorChoice census revision, sequence, count, included-owner projection, party or roots. Restored and uninterrupted worlds are then compared at each of four later completed boundaries; the party is explicitly required to remain active across the first two post-restore boundaries and both parties arrive by the final boundary.

This is test-only continuation evidence for one co-composed terminal successful ActorChoice and active TravelParty fixture. It adds no production behavior, profile scope, actor-control policy, gameplay semantics or serialized owner. It does not establish the broader compatibility/graph matrix, remaining failure-path coverage, universal no-replay, all-owner parity for every state combination, exhaustive writer/epoch coverage, runtime-wide quiescence, capture eligibility, P12-A export/hydration, P13 readiness or Phase 12 closure. Protected ProjectSettings edits, unrelated `.meta` files and prior raw validation outputs were not staged or changed.

## Validation

Each retained XML reports `Passed` with zero failures, skips or inconclusive tests. All runs used the same validated `Assets` tree above.

| Gate | Result | XML SHA-256 | Compressed log SHA-256 | Decompressed log SHA-256 |
|---|---:|---|---|---|
| Focused new case (`DailyV1RestoreRetainsTerminalSuccessfulActorChoiceWithActiveTravelParty`) | 1/1 | `4F87FD1B84EA82A6231C75827DAF2011D3F2750D0772A0F9983963DDE848901F` | `F4E9B25EAE90D18B4028EB34D8FFE488E9BEB492F669C3052E92AA163E7CD2EB` | `EEDF0D7FA98EF9AF161E376F23BB83B5986B9BC61F5C91410F2E8C4AD7DA09BD` |
| `SimulationRuntimeAdmissionTests` | 191/191 | `708DEBBE5F85DEF0852245E068F5DF2B7C66237274615C2C0A19D343D6C87B26` | `8589553613A34690A195483514216D4ED38F2AEDBE63C157D3F4AB7160F27038` | `364BF0741396C2B0187FC0CAD3261836F375C46D907FA56BE5690396CE2491DB` |
| ALL EditMode | 2865/2865 | `8F1A43DD7AD32C1FAF6383AF6C4AC1F9F4D8DF2852DC871A74A8B69CF62C8B99` | `7A34831E810A2CBF2CEDDB849CC057B618849307FD16199778853E7A0E206D77` | `9D109FF410DEC9BD471918783F8F661C25C1EF75F72963C7538EF97247CEC966` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 | `82C213C431513E79BDAB3294493C50AA6E072C9F2484301D2349A586915D82E8` | `E617C9EF888F98E95406694A8B976A95AA29F5BF3BE898A33EC88AC1195CC71D` | `11256186C780B573692543D81DE529088F665A4E6448DFFD445509A8294FCC11` |

`git diff --check c8dd04ac5d002e68eb5d03db814615d3d35f70b7..ff6961ded876b6e2b64eddecbe30c473470bc858` passed.

The corresponding XML reports and `.log.gz` files are retained in `P12GDirectiveTravelContinuationParity/Focused/`, `Admission/`, `AllEditMode/`, and `OfficialSmoke/` using run IDs `025835-2bd4dac347d745969d51d6feccd643f5`, `025857-1e8978aa232249b6a97e49e12cda3fbf`, `025937-65ae33ebcc014cffa730aff8e1cafcfa`, and `030043-e69020193ee548eab6566656b81667c3`, respectively.
