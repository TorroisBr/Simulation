# FR-C Factual Reader — provisional candidate

**Status:** `PROVISIONAL_IMPLEMENTATION_VALIDATED`; independent exact-tip
implementation review passed. This candidate is stacked on the reviewed FR-B
live integration candidate and is not Phase-integrated or canonical-ready.

## Identity and dependencies

| Identity | Value |
|---|---|
| Capability branch | `codex/frc/frc-factual-reader` |
| FR-C code candidate | `2177f7aabcf395c9192d021a0541d0aad6e97250` |
| Tested code tree | `04c266297354b128358fae52570d941115e8a8bc` |
| Exact-tip implementation review | **PASS** at `2177f7aabcf395c9192d021a0541d0aad6e97250` / `04c266297354b128358fae52570d941115e8a8bc` |
| Provisional FR-B base | `5c43733088bfbe860183f849d16295b542c1f465` (tree `d7d463960756e313bdc3800a20ecc976b659f2c5`) |
| FR-B live exact-tip review | `f4e23a7c9a8777c36fb006626f24dbed1b6d6d46` |
| FR-C architecture authority | `451340c56e9b676bf6ea43412bcb856b9ccde3de` |
| Diagnostic revision proposal / review | `64f33fd2c935ff1ab78ccfa08fb3bbecdf4b4c17` / independent **PASS** |
| FR-C diagnostic acceptance record | `775f3c9c7f11dc3312dd4dbf7ccac424d67f2029` |
| Phase 12 canonical at latest refresh | `e64caf08e7ada24a0f6b8c193207a6242018896d` |

FR-B live remains `INTEGRATION_HANDOFF_READY` and its handoff is queued for the
Phase Master. The refreshed Phase 12 canonical ref `e64caf08` does not contain
FR-B live candidate `5c437330` (`5c437330` is not an ancestor of `e64caf08`).
Comparing those tips shows 227 changed lines in `SimulationRuntime.cs` plus
changes in `SimulationBootstrapComposition.cs` and `FactualReadAdmission.cs`.
FR-C remains dependent on FR-B becoming canonical. The FR-C delta leaves those
runtime/bootstrap/admission files unchanged from its `5c437330` base. Because
the Phase canonical has since changed those same shared files, this provisional
commit delivers the immutable reader, contracts, diagnostics, and focused
coverage without runtime registration. Recompose that narrow binding against
the actual post-FR-B Phase canonical tree, then rerun integration validation
and exact-tip review. No numbered Phase canonical ref was changed by this work.

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
endpoints, validates Faction endpoints and join chronology for active and ended
affiliations, rejects duplicate/impossible affiliation state, filters ended
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
| `FactualReadFoundationTests` | 9/9 | `EditMode-20261002-223315-3a8c2680151c4572be93aac19e4a0c9e.xml` | `805992AB0843FDA6BDAE6F00243FC4312E7BD1500A4F6481138E9C814B1625CF` | matching `.log` | `F5856E4AC201DD879E297677DF09FD8BFB89F150716EE632FE91EC9F183B8BEC` |
| `FactionFactualReaderTests` | 7/7 | `EditMode-20261002-223328-65b04dec513d4831842bff3e64c0a85a.xml` | `7009EA4A5CD64F8C36B79421F8AF65BEE6C2F93E11BB123099215947D2AE914E` | matching `.log` | `456E8890EFC1BD92BABC3A657DCF01902DF68B559A978769DC225DACAF3D1D00` |
| ALL EditMode | 2184/2184 | `EditMode-20261002-223342-84a14c4b4c554eb5b46e28b71c1879ae.xml` | `EAB6ABC7CF3B130FD0C4AA4CA24DB84AA2A169DDA79CF1CE628F78DC568AF12A` | matching `.log` | `C6931100E991C2381E0A98E8B98B192139A2C516E9E5A30A1EA3E25803D81914` |
| Official EditMode `Smoke` | 5/5 | `EditMode-20261002-223417-51aa0ce008454a958be7648c326881a5.xml` | `268726230ED6A9E7E01CC011164F69687C01FEC10E3E6C6A71CEB03CD95C0D3A` | matching `.log` | `8239A18171BABECB348D0B8A006FF475C69364DD0642ECE108E8547C50EE40F7` |
| `git diff --check` from FR-B candidate base | PASS | — | — | — | — |

The final two full gates and focused runs were executed after the final
code-bearing change. The first exact-tip review found a P1: creation, join,
and end dates were not bounded by the captured logical boundary. The internal
reader seam now passes the coordinator's pre-capture boundary, and tests
reject future dates. The second review found a P2: ended affiliations skipped
Faction endpoint and join chronology validation. Those checks now run before
the inactive-row filter, with new ended-row regressions.

An independent reviewer approved exact code candidate
`2177f7aabcf395c9192d021a0541d0aad6e97250` / tree
`04c266297354b128358fae52570d941115e8a8bc` against FR-B base
`5c43733088bfbe860183f849d16295b542c1f465` and architecture authority
`451340c56e9b676bf6ea43412bcb856b9ccde3de`. The reviewer confirmed both prior
findings are fixed and found no blocking code issue. Review was read-only and
ran no tests; the validation table records the independently executed gates.
`FactionStore.cs`, `PersonStore.cs`,
`SimulationBootstrapComposition.cs`, and `SimulationRuntime.cs` are unchanged
in the FR-C delta from its provisional FR-B base.

## Current orchestration status

- WI-A: promoted/current.
- FR-B core: promoted.
- FR-B live integration: handoff-ready; waiting for Phase Master canonical integration.
- FR-C design: implementation-ready after independent diagnostic-revision review.
- FR-C implementation: provisionally validated and independently reviewed at `2177f7a`; runtime binding waits for canonical FR-B.
- WX-D: waits on FR-C and the approved Simulation-External collection-coverage contract.
