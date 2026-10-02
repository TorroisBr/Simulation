# FR-C Factual Reader — provisional candidate

**Status:** `PROVISIONAL_IMPLEMENTATION_VALIDATED`; exact-tip independent code
review is in progress. This candidate is stacked on the reviewed FR-B live
integration candidate and is not Phase-integrated or canonical-ready.

## Identity and dependencies

| Identity | Value |
|---|---|
| Capability branch | `codex/frc/frc-factual-reader` |
| FR-C code candidate | `b1e9985d49bc9b6353e0b13e727bf9a3cd1a2a87` |
| Tested code tree | `5dd1be8a771101803886139fbfbeb8c1eddf553f` |
| Provisional FR-B base | `5c43733088bfbe860183f849d16295b542c1f465` (tree `d7d463960756e313bdc3800a20ecc976b659f2c5`) |
| FR-B live exact-tip review | `f4e23a7c9a8777c36fb006626f24dbed1b6d6d46` |
| FR-C architecture authority | `451340c56e9b676bf6ea43412bcb856b9ccde3de` |
| Diagnostic revision proposal / review | `64f33fd2c935ff1ab78ccfa08fb3bbecdf4b4c17` / independent **PASS** |
| FR-C diagnostic acceptance record | `775f3c9c7f11dc3312dd4dbf7ccac424d67f2029` |
| Phase 12 canonical at last refresh | `1ac675cc558aa919a749167647c10506c11303fc` |

FR-B live remains `INTEGRATION_HANDOFF_READY` and its handoff is queued for the
Phase Master. FR-C remains dependent on FR-B becoming canonical. `SimulationRuntime.cs`
is a shared P12 hotspot with active worktrees, so this provisional commit
delivers the immutable reader, contracts, diagnostics, and focused coverage
without runtime registration. Recompose that narrow binding against the actual
post-FR-B Phase canonical tree, then rerun integration validation and exact-tip
review. No numbered Phase canonical ref was changed.

## Delivered provisional scope

The FR-B internal reader seam now carries an immutable outcome. A failed
coherent capture exposes a sorted, read-only diagnostic collection while
keeping each factual result `Unavailable` and discarding all copied fact values.
Reader exceptions map to a stable generic diagnostic; exception type, message,
and stack are not exposed. Diagnostics must refer to a requested capability
whose result is `Unavailable`.

`FactionFactualReader` implements `simulation.faction-truth/v1`. It returns
copied immutable Faction and current active-affiliation facts, preserves source
fields, represents blank names as absent, validates active Faction and Person
endpoints, rejects duplicate/impossible affiliation state, filters ended
affiliations, and uses ordinal/numeric ordering. The reader accepts only its
FactionStore's exact PersonStore and retains neither store in its result. The
scope consumes no actor Knowledge, support, presentation, or World Exchange
data.

## Exact-tree validation

The code-bearing candidate at `b1e9985d49bc9b6353e0b13e727bf9a3cd1a2a87`
passed the following gates. XML and logs are retained under the ignored local
archive `Library/ValidationResults/FRC-Provisional/`.

| Gate | Result | XML | XML SHA-256 | Log | Log SHA-256 |
|---|---:|---|---|---|---|
| `FactualReadFoundationTests` | 9/9 | `EditMode-20261002-221022-fe8a8e3e59bb49c0acd7796b9a00e496.xml` | `90F17B818C6EC7BCFFF3C73506CC7FB2B1D54EFD7BE5B355505F2DB2B31AA7A8` | matching `.log` | `BFEBFCC6B4851E97659F4C557EDA5025472622EFF46FA4D4EEE42CB0B95DAED5` |
| `FactionFactualReaderTests` | 5/5 | `EditMode-20261002-221036-180e425ebc574f68af14f3e409c2dbdb.xml` | `7BF6B3D5C375475925EA1592184DF8C9A03B1CA616B324694BF76F75F1275BB6` | matching `.log` | `DB2E8215DD026CA53B1EE221E04EBE5CA6000990AF96F2E028D053F4A1666CC5` |
| ALL EditMode | 2182/2182 | `EditMode-20261002-221050-c9c5d4de14e94fdeb63ffbeaf0a3e130.xml` | `4673D292E78A05D79B3FEDE4C722D7C488D613804BEC30F7FBDD9300A49FE5EA` | matching `.log` | `A5288CC3315CA7069033B08D44420E6D5A8C97880701676C42DF679A4EA64B5B` |
| Official EditMode `Smoke` | 5/5 | `EditMode-20261002-221123-ab73469e0d504190b4b9da35a61699aa.xml` | `2D541FC513BFC73E72E6AEA28073EF9C40A1F491B98C77B07E07A4778B809C58` | matching `.log` | `A8250C45C78AF1F52360C59A0144C423026B06446E1A3FFB752E5CBC06FD48D0` |
| `git diff --check` from FR-B candidate base | PASS | — | — | — | — |

The final two full gates and focused runs were executed after the final
code-bearing change. `FactionStore.cs`, `PersonStore.cs`,
`SimulationBootstrapComposition.cs`, and `SimulationRuntime.cs` are unchanged.

## Current orchestration status

- WI-A: promoted/current.
- FR-B core: promoted.
- FR-B live integration: handoff-ready; waiting for Phase Master canonical integration.
- FR-C design: implementation-ready after independent diagnostic-revision review.
- FR-C implementation: provisional validated core; exact-tip independent code review in progress; runtime binding waits for canonical FR-B.
- WX-D: waits on FR-C and the approved Simulation-External collection-coverage contract.
