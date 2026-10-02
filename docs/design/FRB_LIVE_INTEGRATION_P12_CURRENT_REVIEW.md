# FR-B live runtime integration — current P12 re-composition

**Status:** `VALIDATED_CANDIDATE`; fresh exact-tip implementation review
passed. This is not canonical promotion or P12-B completion.

## Candidate identity and refresh

| Identity | Value |
|---|---|
| Candidate branch | `codex/frb/FRBLiveIntegrationP12Current` |
| P12 canonical base | `1dce6d54a33ac1b1778b1a44a7794512a58416e8` |
| Current P12 canonical at review | `1dce6d54a33ac1b1778b1a44a7794512a58416e8` |
| Promoted P12 record-sequence code included in base | `cd7ca4498d2c1d3591c227bd9011429f9bd06d8f`, tree `5282d65fbc4311bb6b907770a4e6fa363ad633df` |
| Re-composed FR-B code tip | `aeb76c687d00a49f505ab264a58a508a20e4923b` |
| Re-composed code tree | `862eb904c6679a66d4dd2a2e2ad7f174ec8479c2` |
| Parent sequence | `1dce6d5` → `64960e2` (replayed initial integration) → `aeb76c6` (replayed unsupported-profile fix) |
| Earlier reviewed candidate preserved | `codex/frb/frb-live-integration` at `5c43733088bfbe860183f849d16295b542c1f465`, tree `d7d463960756e313bdc3800a20ecc976b659f2c5`, based on old P12 tip `1ac675cc558aa919a749167647c10506c11303fc` |
| Earlier handoff record preserved | `codex/frb/frb-live-integration-review` at `f4e23a7c9a8777c36fb006626f24dbed1b6d6d46` |
| Architecture authority | `451340c56e9b676bf6ea43412bcb856b9ccde3de` |

The old-base candidate was not discarded or rewritten. Its two implementation
commits were replayed onto the current P12 canonical in a new branch. The first
replay had one conflict at the `SimulationRuntime` field block; resolution
retained both the P12 `SimulationRecordSequence` adapter fields and the FR-B
coordinator fields. The follow-up unsupported-profile fix replayed cleanly.
The resulting code tree differs from the old reviewed tree because it includes
the newer canonical P12 code. Its five-file FR-B diff from the current P12 base
is otherwise the same bounded change: 214 insertions and 3 deletions.

## Delivered boundary

Every composed runtime exposes a non-null `FactualReadCoordinator`. Only the
selected `UnityBootstrap-Daily-v1` profile binds the exact composed
`FactionStore` and `PersonStore`; other profiles remain unbound and return
`Unavailable`. The selected surface becomes available only after world
publication succeeds and the bootstrap operation closes in a healthy state.

Capture admission remains on the bound owner thread and requires a healthy,
idle runtime. The synchronous read cut is bracketed by the selected profile's
logical day and the exact bound FactionStore/PersonStore revisions; their
supported mutation entry points remain guarded during materialization. The P12
mutation epoch is read only as a runtime-health signal. It is not treated as a
whole-world coherence boundary. The reader set remains empty, so requested
capabilities return `Unsupported`.

The selected profile is daily-only. Intraday/P18 runtimes do not pass this
profile's admission check. Post-publication day-zero factual reads are allowed
by the accepted FR-B contract and covered by the integration test; this is not
P12-A save-capture eligibility, which remains tied to its separately accepted
completed-day and full-owner requirements.

No Faction or Person reader, FR-C, exporter, complete owner/epoch inventory,
whole-world capture guarantee, export/hydration, P12-A readiness, P12-B
completion, or P13 readiness is included. P12-B remains INCOMPLETE; P12-A
remains `WAIT_DEPENDENCY`; P13 remains BLOCKED.

## Exact-tree validation

Validation ran after re-composition on code tree
`862eb904c6679a66d4dd2a2e2ad7f174ec8479c2`. The harness XML and logs are
retained in the candidate worktree under
`Library/ValidationResults/FRB-Live-Integration-PostP12/`.

| Gate | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| `SimulationBootstrapCompositionTests` | 21/21 | `C8765937851390261C69DEAC6B962F8A1F2D7E16C6BF390345C8CF7CE146D826` | `6C45919F0C43B8305CB11415B0EB2AA0331E69FE043725691909BDA809B29164` |
| ALL EditMode | 2182/2182 | `8DD30A7A66BE1B7C572E55FC2009A0E56C626B74FB272C59E22DAB51BD8691BA` | `D8DA001F3F502937999D445D1B30B628B10B01EDC8EA3ACA5F434CA8C717B4A7` |
| Official EditMode Smoke | 5/5 | `BA5661BA2CF8874706E9EF0EB3DBD9DAB876F5BC3E32E460143B30AFE678FF17` | `61DD3EE4CF6C2BE9A6F2CAEA74755DFDBB4432BB86FDF029370C409F81690D2A` |
| `git diff --check` against P12 base | PASS | — | — |

All reports have `result="Passed"`, zero failures, zero skipped, and coherent
counts. The unrelated ProjectSettings edits and untracked `.meta` files in the
candidate worktree were not staged or changed; their SHA-256 values match the
pre-run snapshot.

## Fresh exact-tip review

An independent Luna reviewer returned **PASS** for code tip
`aeb76c687d00a49f505ab264a58a508a20e4923b`, tree
`862eb904c6679a66d4dd2a2e2ad7f174ec8479c2`, against P12 base
`1dce6d54a33ac1b1778b1a44a7794512a58416e8` and architecture authority
`451340c56e9b676bf6ea43412bcb856b9ccde3de`. The reviewer inspected the
current full candidate diff and its interaction with the newly promoted P12
record-sequence adapter. No actionable findings remain.

The review confirmed exact store-pair binding; owner-thread, publication,
health and idle admission; synchronous non-reentrant reads; day and exact
store-revision bracketing; store mutation guards; unsupported-profile
`Unavailable`; and that the P12 epoch is used only for health, not whole-world
coherence. It also checked that the sequence adapter receives the exact shared
`SimulationRecordSequence` owner and invalidates once after successful
allocation. The reviewer confirmed the daily profile excludes P18/intraday
runtime use and that day-zero read availability is distinct from P12-A capture
eligibility. Tests were not rerun by the reviewer; the exact-tree validation
above was run separately.

The earlier `f4e23a7` handoff review remains evidence for its unchanged FR-B
core and earlier base. This fresh review covers the recomposed current-base
integration and is the evidence required for a separate P12 canonical
promotion decision.
