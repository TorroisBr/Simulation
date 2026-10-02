# FR-B factual-read foundation core candidate

**Status:** `VALIDATED_CANDIDATE`; exact-tip implementation review and required
validation passed. Canonical promotion is a separate human gate. This is the
bounded core only: it does not provide production runtime composition, the
selected-profile Faction/Person read cut, capture eligibility, or P12-B
completion. P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, and P13
remains blocked.

| Identity | Value |
|---|---|
| Candidate branch | `codex/frb/frb-foundation-impl` |
| P12 canonical base and current P12 canonical | `d6e52dcdbf5fe2a36de92c6516485040d54e4279` |
| Architecture authority | `c285466c355103d3637ac165246591b72eb7bda0` |
| Code candidate | `0ad19ecd9633e01d358a9ff826ca3ca5f8e3627c` |
| Tested code tree | `09c3243ccb5f21a559dbcba3fa1dc6c4050ff516` |
| Exact-tip independent code review | PASS, against base `d6e52dcdbf5fe2a36de92c6516485040d54e4279`; reviewer record is captured below |

## Delivered boundary

The core adds factual-read contracts, admission, and a coordinator. Admission
binds the exact FactionStore/PersonStore pair and checks the selected profile,
owner thread, successful publication, runtime health, and idle advance state.
Reads are synchronous and non-reentrant. Supported mutation entrypoints in
both stores reject mutation during a read. The coordinator orders requests,
brackets materialization with logical-boundary and store-revision comparisons,
and discards partial results when data is unavailable, a reader throws, or the
boundary changes.

The candidate is limited to the two stores, new FactualRead core types, and
focused tests. It does not add production `SimulationRuntime` or bootstrap
composition and its tests use a fake runtime-state provider. The core does not
deep-copy arbitrary generic fact values: future readers must provide immutable
copied values as required by the `FactReadResult<T>.Present` contract.

No claim is made for a real composed-world read cut, effective-profile Faction
and Person source coverage, capture eligibility, complete owner/epoch coverage,
export, hydration, P12-A readiness, P12-B completion, or P13 readiness.

## Exact-tree validation

The following gates ran against code tree
`09c3243ccb5f21a559dbcba3fa1dc6c4050ff516`, before this documentation-only
record. XML and log files are retained in the ignored local archive
`Library/ValidationResults/FRB-Core-20261002/` for independent inspection.

| Gate | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| `FactualReadFoundationTests` | 8/8 | `8FA8C8EC48A1982B335DAFF740AE3E64D6F31F77F5B28CB38D7E7DA0A9733250` | `577F9B48B9B0977366DFEDF243233418C369D89EEF8B838380B6D0BDCE860168` |
| ALL EditMode | 2168/2168 | `A243963E707F5B0D14D020892F9228AC5BD4E9FDEF6A043F18C40DD5E3F002A2` | `307FE4BA2764B162FF711F395B56D44606CA3AD9B2D4BCC055DF825BE9E7A5C0` |
| Official EditMode `Smoke` | 5/5 | `09B0C834FE668E6FF55C389C9CDB7701E5C78E1934CEFBD2109EA5867D1E2D53` | `CB8459B1F74310B105B0FE4D0C9EAA4FEF868D806691F029104AEF3520CC6D60` |
| `git diff --check` against P12 canonical base | PASS | — | — |

## Exact-tip review

An independent Luna reviewer inspected the full 14-path candidate diff at
`0ad19ecd9633e01d358a9ff826ca3ca5f8e3627c` (tree
`09c3243ccb5f21a559dbcba3fa1dc6c4050ff516`) against P12 canonical base
`d6e52dcdbf5fe2a36de92c6516485040d54e4279` and architecture authority
`c285466c355103d3637ac165246591b72eb7bda0`. Review result: **PASS** for the
bounded core. It confirmed exact-store pairing, supported-mutation guards,
owner-thread/publication/health/idle admission, synchronous non-reentrant
reads, boundary and revision bracketing, deterministic request ordering, and
failure without a partial result. No scope conflict or code change was
identified.

The review also retained two implementation constraints: the real composed
Faction/Person read cut remains future serialized integration work, and fact
readers must supply immutable copied values because the generic result wrapper
does not deep-copy references. The review ran no tests; the exact-tree
validation above was run independently.

This validated candidate is not canonical and is not a promotion approval.
