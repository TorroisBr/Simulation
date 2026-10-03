# P12-B selected-profile solo travel-start operation candidate

**Status:** submitted implementation candidate; no canonical promotion or Phase closure is recorded here.

**Revision note:** The initial code and validation records below are historical. Corrected code `fe0e0be03403e92001173deae1fafe58dfe432d2` (tree `d72e84d6a442440ab82bacf6a0eb32165a9d7055`) supersedes that implementation tip; see Revision 2 below for its exact validation and pending independent implementation review.

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

## Revision 2 - corrected implementation candidate

The initial implementation commit `63d4e3a880e7dba7246bce1ea93b1982cb9bb951` received an exact-tip NEEDS_CHANGES review. This revision retains that historical result and corrects the source-City ownership condition and real charge-compensation coverage identified there. Independent design revalidation passed after the source-backed clarification at `4b79a5d176dd73e2814dbfdf76b73c6117ec8d23`, with its record in `8468b8e`; the clarification does not add scheduled Travel support.

**Current code candidate:** `fe0e0be03403e92001173deae1fafe58dfe432d2`
**Current code tree:** `d72e84d6a442440ab82bacf6a0eb32165a9d7055`
**Exact implementation review of this corrected code tip:** pending.

The runtime includes a source City section only when `CurrentCity` reciprocally contains the actor, matching the actual `NpcRuntime.StartTravel` mutation. Regression coverage exercises a stale non-reciprocal City pointer whose City census revision is stale; the operation starts travel without preflighting or publishing that unrelated City section. Compensation coverage executes the real selected Travel action: the charge commits, saturated travel-state revision causes `StartTravel` to reject, and the existing compensation restores the balance inside the same admitted operation. The account revision advances twice, the operation epoch once, and neither travel state nor event sequence commits.

The scheduled-request contract test records the current API boundary: `ScheduledDirective` accepts only EscapePrison and rejects a Travel action. The implementation does not broaden that API or claim a scheduled Travel consumer.

### Corrected-tree validation

Every XML below reports Passed with zero failures and zero skips. All listed files are retained under `Library/ValidationResults/P12BSoloTravelStartRevision2/` and correspond to the corrected code tree above.

| Gate | Result | XML / SHA-256 | Log / SHA-256 |
|---|---:|---|---|
| P12SoloTravelStartOperationTests | 11/11 | `EditMode-20261003-180617-e341e843df994c49b05a73574ea00249.xml` / `C224C81CC64116EDC3D053EF7D7D8EA4F0690A9FF68226CF786F0DB29DE47698` | `EditMode-20261003-180617-e341e843df994c49b05a73574ea00249.log` / `E83BDE7CE306E50A329B2F198A5A79C6CBDEB1289EF42D07260CD6A2BABF374C` |
| EconomyTransactionTests | 45/45 | `EditMode-20261003-180846-8184799901a24e85987dd69e795cb4c5.xml` / `E04EE0FCCE1D5603937D375A82B1553C2978458E925B9A3D70CF2498B9E8F5B2` | `EditMode-20261003-180846-8184799901a24e85987dd69e795cb4c5.log` / `6CAD13E029F578B0F4913AB4F2148005C3910E1DBE3E558FDCCCF33233915092` |
| GeneralizedSpatialTravelTests | 18/18 | `EditMode-20261003-180901-9ff3cad6238d42b8880e2baf3a46ef65.xml` / `FDD8E1287542E824F1F0D7E4B72CAD70AE1F1F8D3DC385459213A863F647730E` | `EditMode-20261003-180901-9ff3cad6238d42b8880e2baf3a46ef65.log` / `AC8B335434744288E1252E51642259EB3CE4D11EE1692F2A1DA019ABB0D4C01A` |
| TravelScoutingTests | 11/11 | `EditMode-20261003-180916-1e2892d57be34a9a879ac9a09270b8b4.xml` / `83D5874380B25369A17147527295E87D570B4C4748B106EB153E7B40C8668A8C` | `EditMode-20261003-180916-1e2892d57be34a9a879ac9a09270b8b4.log` / `EA0CD0AED167067D9C0798390E2C330D64F585456887EFDD2DBAAA361D962AB6` |
| SpatialKnowledgeCensusTests | 16/16 | `EditMode-20261003-180936-3f97a54bfa5c41599f1e98966c63794f.xml` / `949F8C92DF37616B38BF6EA67364EF58ACA7F49786E58724129616BF5EC92CD9` | `EditMode-20261003-180936-3f97a54bfa5c41599f1e98966c63794f.log` / `BCD6DFBECDE15BFD8D67ECB09426FD2847B5C10EBDA5E5A03E351CA33CB2EED8` |
| RuntimeIdAllocatorCensusTests | 3/3 | `EditMode-20261003-181028-09cb28282fd34d1b927228c6703bd38a.xml` / `C769C4C69B4E6149FA0892A700809ED4753CE294308CF519432D68C9138D8B29` | `EditMode-20261003-181028-09cb28282fd34d1b927228c6703bd38a.log` / `0515267B02E4BC9ACA1FBE175271DC394FEB211FB7E75B96BCA1E068C06EFF36` |
| SimulationRecordSequenceP12InvalidationTests | 11/11 | `EditMode-20261003-181042-7780df6f79ef4e4884bc45b949ed99bd.xml` / `D74984D9A07212606DFD2DFD452D0E098989058F2729A472C2128F2F6EB168A4` | `EditMode-20261003-181042-7780df6f79ef4e4884bc45b949ed99bd.log` / `86FB6707B22DA4584EF17C1513C04E0D1DE6BC210934890E3C835DF61E80B434` |
| SimulationBootstrapCompositionTests | 21/21 | `EditMode-20261003-181101-70b7ec7a9ed142b58703c17d79866dfe.xml` / `C4E2F62009E3A2695B57EBB65BA5C32757831481B2C31AB308ECF1B76E02FBDD` | `EditMode-20261003-181101-70b7ec7a9ed142b58703c17d79866dfe.log` / `6112167A9A5F3704F57ECD14EB7150CD5F7772FE24723A24DDE13A5744883ABD` |
| SimulationRuntimeAdmissionTests | 31/31 | `EditMode-20261003-181118-7d10952fec104c319a3cddde3bc3c06c.xml` / `68DDCFB3BB20E003845852A09DA99CB5DF6895F7091A3541938788EA1BFEB76C` | `EditMode-20261003-181118-7d10952fec104c319a3cddde3bc3c06c.log` / `8E1EBA49AD7826480966DAD01CCB9022F5D1A13C0AB326167F6426F9FE352088` |
| SimulationRuntimeOrchestrationTests | 12/12 | `EditMode-20261003-181133-4e15669fe1cf45ab8310fa429fb00773.xml` / `20A3292DE0062728917AA55250DEE5F34EBA1A11AC28F8B72CF1751CABAEF2A0` | `EditMode-20261003-181133-4e15669fe1cf45ab8310fa429fb00773.log` / `50FFE623BBF89134D47D1D4C9423629E5EE463AC0AF8389DA2E913B7D50906F7` |
| P12TravelPartyAdvanceTests | 10/10 | `EditMode-20261003-181147-4123c055cd604343a3969f8f4b56c5de.xml` / `EFB579037FDB20CAAB3A03B01F8982B7D154D77F09ABFF2AC232EE8737486B72` | `EditMode-20261003-181147-4123c055cd604343a3969f8f4b56c5de.log` / `D13CAADA9CCB2B67EAADB9229324883D5560DE579A12C62F32ACDFE0D97F4332` |
| SimulationRuntimeLongRunTests | 7/7 | `EditMode-20261003-181205-01200fd1a9cd4dfc816b966405472e71.xml` / `AC575B21E7032BE717DAC2FC388628CE22C09A3F9E194582452708C1C400055F` | `EditMode-20261003-181205-01200fd1a9cd4dfc816b966405472e71.log` / `BC9D0A7DC1930002C5A735C9A63ACC2629F8CA465FC60ECCC2327421EE2A2E63` |
| ALL EditMode | 2216/2216 | `EditMode-20261003-181226-14d2ec79163a40edb4b9e99bc1ed666a.xml` / `02EEBABDC75734CE270E91C547E8447C69089EBA1746314389A7CA4F391D256F` | `EditMode-20261003-181226-14d2ec79163a40edb4b9e99bc1ed666a.log` / `A994F5DDA0E8EDD983D3BB4CCFB69F3BF41F53E7B986FA218D7D4250E41481F9` |
| Official Smoke | 5/5 | `EditMode-20261003-181259-160d1758f50b41a48df95167264ce506.xml` / `92860D659B431B5AE8B3046F13827EA5CA2B853A3198C090C0E75EAE884FD312` | `EditMode-20261003-181259-160d1758f50b41a48df95167264ce506.log` / `077C0DA0B7B46E920C8CFAF1222789E1DC3FC2069C547E90E3076D93130A189E` |

`git diff --check` and `git diff HEAD^ HEAD --check` passed. `ProjectSettings/EditorBuildSettings.asset` and `ProjectSettings/ShaderGraphSettings.asset` remained byte-identical to their pre-validation hashes recorded above. The untracked ArmedForce `.meta` files remain unstaged.

### Review status and limits

The design clarification and design revalidation are PASS, but the revised implementation code still requires independent exact-tip review. Canonical promotion is not approved. This slice does not establish complete owner or operation coverage, complete shared-epoch coverage, global quiescence, capture eligibility, export, hydration, P12-A readiness, P12-B completion, P13 readiness, or Phase 12 closure.