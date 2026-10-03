# P12-B selected-profile solo travel-start operation candidate

**Status:** submitted implementation candidate; no canonical promotion or
Phase closure is recorded here.

## Candidate identity and reviewed contract

- Canonical base: `codex/phase12/canonical` at
  `e76e50bfefc68b827d54ceb74e0b25293e0ad7b8`.
- Technical design: `codex/phase12/P12BSoloTravelStartOperationDesign` at
  `91728e4aee7717c6b002691a9a9e96c4ad22ac71`;
  `docs/design/PHASE12_P12B_SOLO_TRAVEL_START_OPERATION_DESIGN.md`.
- Independent design review: PASS at design tip
  `91728e4aee7717c6b002691a9a9e96c4ad22ac71`, recorded by
  `812b04628bd8d3f04c491b1df4adae7e2c3692e2`.
- Code commit: `63d4e3a880e7dba7246bce1ea93b1982cb9bb951`.
- Code tree: `cab0b326354bdd90d1f9c55a3daba70c52d9e7ca`.
- Implementation branch: `codex/phase12/P12BSoloTravelStartOperationImplementation`.
- Validation artifacts below were produced on the exact code tree above.

The code commit is the implementation tree; this document is a subsequent
documentation-only evidence commit. The focused, full EditMode, Smoke, and
long-run results therefore remain tied to the code commit/tree, not to a
changed executable tree.

## Bounded delivery

For the selected `UnityBootstrap-Daily-v1` profile, this candidate registers
and enters the nested `runtime.travel.start` operation only when a
`TravelSystem` is composed and the runtime executes the bound
`TravelActionProvider`. Before the existing travel provider runs, the runtime
validates the exact installed NPC, provider-to-system binding, owner-thread
admission, and the unchanged census witnesses for the account, travel state,
travel plan, paired SpatialKnowledge sections, source-City presence when one
exists, event counter, and SimulationRecordSequence.

Committed owner callbacks within the operation collect their changed section
IDs in an ordinal set. Closing the operation publishes that deduplicated set
once, so account charge/compensation, travel-plan clearing, travel-state and
source-presence changes, route discovery, and successful event-sequence
allocation are represented by one nested-operation epoch step. Empty/no-op
starts add no epoch. An actor without a City projection does not acquire a
fabricated source- or target-City section. A failed admission faults before
travel execution. If an existing provider path commits a charge and then
throws, scope unwinding reports that committed account revision before the
existing runtime fault path; it does not add rollback or change provider
failure policy.

The existing action roll occurs before the nested operation. Existing daily
origin observation and the later `TravelStartedToday` clear remain outside
this nested boundary. Legacy execution does not register or enter this P12
operation. The narrowly scoped mutation-baseline update lets a later
compensation write to an owner section already changed in the same active
operation validate against its current revision and publish that section once
at scope close; outside an active batch, existing unchanged-section validation
remains in force.

## Changed files

- `Assets/_Project/Scripts/SimulationRuntime.cs`
- `Assets/_Project/Scripts/TravelActionProvider.cs`
- `Assets/_Project/Tests/EditMode/Editor/P12SoloTravelStartOperationTests.cs`
- `Assets/_Project/Tests/EditMode/Editor/P12SoloTravelStartOperationTests.cs.meta`

## Exact-tree validation

Every XML reported `result="Passed"`, zero failed tests, and zero skipped
tests. XML/log files are retained in the validation worktree under
`Library/ValidationResults/P12BSoloTravelStartFinal/`.

| Gate | Result | XML / SHA-256 | Log / SHA-256 |
|---|---:|---|---|
| `P12SoloTravelStartOperationTests` | 9/9 | `EditMode-20261003-174009-5efdc929ea9145cabfba290c41247ed4.xml` / `E53152DFFE254B6609960F7848913C0673A41F3C37F8AE9259ADEFE2C6533924` | `EditMode-20261003-174009-5efdc929ea9145cabfba290c41247ed4.log` / `7A12A3ADCCBFA0718195CC3AF9021764DB4EA07119F9BD8BFAC8767A76C68FA2` |
| `SimulationRuntimeAdmissionTests` | 31/31 | `EditMode-20261003-174023-2c28ae780165459da021eff72afd7593.xml` / `B0E88F15B7A3C164A642F98BA06CD1F0D56CF776FCDFB70C6CAF90A0E7087C54` | `EditMode-20261003-174023-2c28ae780165459da021eff72afd7593.log` / `80F36A00C559C2B67C43228B55851EDE34CABA457A2C774CB03FFB4BE1DC59AB` |
| `P12TravelPartyAdvanceTests` | 10/10 | `EditMode-20261003-174041-67b6209fc60844a1b9d292e3461d9c98.xml` / `CB5EA33AC078E06680128E2B5640F81F833B6FFB8FF0EBE321173EF64024F3F9` | `EditMode-20261003-174041-67b6209fc60844a1b9d292e3461d9c98.log` / `0FD7BCB5F306B26E26668221C199E3B500D7CB25F879FC458E26F5B9E88D6CEA` |
| `SimulationRuntimeOrchestrationTests` | 12/12 | `EditMode-20261003-174055-c6145e14dac340c1b7b56d49b182a66d.xml` / `3B1B7339917D14C878DFD7B6947F79EBDACC961496F94C21AD74C2B555B97829` | `EditMode-20261003-174055-c6145e14dac340c1b7b56d49b182a66d.log` / `8BA835537EBD16901E2C17A70FE14E60ABA9E7FFC974BBE70D802EF248F95762` |
| `SimulationRuntimeLongRunTests` | 7/7 | `EditMode-20261003-174112-f6747c0a40d8424caf0e561f66531989.xml` / `F0D787F87F7C36CF3F24F90CB01BCFC608633A34B0109162B798BCE9BDF21D9C` | `EditMode-20261003-174112-f6747c0a40d8424caf0e561f66531989.log` / `0A71A3CFC228995E0FD322F749D9805ECB342BA3872E44027C37F81F632B28F7` |
| ALL EditMode | 2214/2214 | `EditMode-20261003-174129-2cce2b27b1164c34a8740a539fc92a2e.xml` / `290D71D2D535111888E06E4A8A9ED03C7E34AF9CA3A321B39600CCC759C53D84` | `EditMode-20261003-174129-2cce2b27b1164c34a8740a539fc92a2e.log` / `AB0338AB6024A1B333068A02E92267D0A8DC2DDC3BE46366B6591874B71E9EE9` |
| Official Smoke (`EditMode -TestFilter Smoke`) | 5/5 | `EditMode-20261003-174204-4caf2ac1b5a3455b9c3a6b01ded62e8b.xml` / `CC56FCF8EB94BD8FCCFACFC000AD8720CC2DAAB1B8520B4CA8A5D10843F65D40` | `EditMode-20261003-174204-4caf2ac1b5a3455b9c3a6b01ded62e8b.log` / `847374EF0F64A7D402C72B6AA93D8B0B648B5E6C1602D03E15AE27A53D1B1706` |

`git diff 'HEAD^' HEAD --check` passed for the code commit. The exact
pre-validation and post-validation SHA-256 values remained equal for the
unrelated local files `ProjectSettings/EditorBuildSettings.asset`
(`58BCBFB23DA7AAB5ACD696E7D83E9D75A86442ED39A582159D2493049361BA28`) and
`ProjectSettings/ShaderGraphSettings.asset`
(`5C432C9C73FDF73FEE91C8823EA02AB98E3B7A8EDF341B5AE6901D699CCE52A7`). The
unrelated ArmedForce `.meta` files were not staged.

## Limits retained

This is one selected-profile travel-start operation/invalidation slice only.
It does not establish complete live owner or operation coverage, complete
shared-epoch coverage, global quiescence, capture eligibility, export,
hydration, P12-A readiness, P12-B completion, P13 readiness, or Phase 12
closure. No target-City presence or money-transfer child operation is claimed.
Canonical State remains unchanged until a separate approved promotion.
