# FR-C Factual Reader — provisional candidate

**Status:** `PROVISIONAL_IMPLEMENTATION_VALIDATED`; an exact-tip review found a
timeline blocker, fixed additively, and exact-tip re-review is in progress.
This candidate is stacked on the reviewed FR-B live integration candidate and
is not Phase-integrated or canonical-ready.

## Identity and dependencies

| Identity | Value |
|---|---|
| Capability branch | `codex/frc/frc-factual-reader` |
| FR-C code candidate | `22535a600e55a3b25aa76f7dddae1984b19c8a3f` |
| Tested code tree | `75a6d25874faf04eac97e4bf7b46d35777c6fc05` |
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
| `FactualReadFoundationTests` | 9/9 | `EditMode-20261002-222301-52fe0d003aad4328965ff83368ff8406.xml` | `3A0D98A18B1F448AFE102EC3C57AFD53B568C13CA74A73E79DC5280074943926` | matching `.log` | `BF0D6031A9DD9742C6C7B85B0888C857B0E1F27134F8312EF7ED655FBE8A5D21` |
| `FactionFactualReaderTests` | 6/6 | `EditMode-20261002-222357-14fdd0105f1f4856864f290a697c8e72.xml` | `D12DBB9993E3B3D90618CFCFB868C5AC1776762D7E24A403B395841AD69214CE` | matching `.log` | `3BA47EE144A079B417B71017346315630F866D044E4C0A25BE1117377076E270` |
| ALL EditMode | 2183/2183 | `EditMode-20261002-222427-5ac07272f51947ee8e8b01cd185f18d4.xml` | `E942BD79E8B55FDBA555A1E2FB02087A0F50902D5B28AE4C7929CECD89D9606A` | matching `.log` | `2EA8EA94CA59024E99685614084D12F125DDAF2731B51D8364A0497B5B222944` |
| Official EditMode `Smoke` | 5/5 | `EditMode-20261002-222501-3be099869b79463b8fbc5a17f6708ddf.xml` | `A1714B20FB3E351C1F1511F017640AD163C5BAE583092DC199736A4F4FD7C0B2` | matching `.log` | `FB5DD32B4FFC201394D1A0C86C505909E5A8145B6119B89B81BCFC7F5608E454` |
| `git diff --check` from FR-B candidate base | PASS | — | — | — | — |

The final two full gates and focused runs were executed after the final
code-bearing change. The code review's P1 finding was that the reader did not
bound creation, join, and end dates by the captured logical boundary. The
internal reader seam now passes the coordinator's pre-capture boundary, and
tests reject future creation, join, and end dates. `FactionStore.cs`, `PersonStore.cs`,
`SimulationBootstrapComposition.cs`, and `SimulationRuntime.cs` are unchanged
in the FR-C delta from its provisional FR-B base.

## Current orchestration status

- WI-A: promoted/current.
- FR-B core: promoted.
- FR-B live integration: handoff-ready; waiting for Phase Master canonical integration.
- FR-C design: implementation-ready after independent diagnostic-revision review.
- FR-C implementation: provisional validated core at `22535a6`; first review's timeline finding is fixed and exact-tip re-review is in progress; runtime binding waits for canonical FR-B.
- WX-D: waits on FR-C and the approved Simulation-External collection-coverage contract.
