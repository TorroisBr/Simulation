# P12-E Persistent Conflict Census Candidate

**Status:** Implementation, required validation, and independent exact-tip
implementation review complete; durable review record is
`PHASE12_P12E_CONFLICT_CENSUS_REVIEW.md`.

**Canonical promotion:** Included in the cumulative census stack promoted at
`b889b47`; exact integration review passed at `35ec988`, with full-tree
validation on code tip `65ebc7f`.

**Candidate branch:** `codex/phase12/P12EConflictCensus`.

**Code-bearing candidate:** `e371d67` (`Add persistent conflict census
witness`). It extends the reviewed manpower/position census candidate at
`7a4a95a`. The design review PASS is recorded in
`PHASE12_P12E_CONFLICT_CENSUS_DESIGN_REVIEW.md`.

## Delivered boundary

The bootstrap composition publishes one fixed schema-v1 passive section:
`p12e.conflicts`. It reports `Runtime.ConflictStore.Count`, the same store's
existing `Revision`, and the exact runtime-installed `PersistentConflictStore`
as owner identity. The selected authored profile verifies its default
runtime-owned ConflictStore has exact day-zero count/revision zero and stable
identity across reads.

Tests verify registration, participant binding, and ending update revision
without increasing record cardinality. Missing-Conflict and invalid
one-side-record rejections, unknown-ArmedForce binding, post-end binding, and
duplicate registration preserve the witness.

## Validation

| Gate | Result | Evidence |
|---|---:|---|
| `PersistentConflictCensusTests` | 1/1 | `Temp/ValidationResults/EditMode-20260930-005334-eaba4155e79f4231973def6dce9edb0f.xml` |
| `SimulationBootstrapCompositionTests` | 14/14 | `Temp/ValidationResults/EditMode-20260930-005355-a329405c66444e1fa7874cca0d5733c5.xml` |
| `PersistentConflictWarBattleStateTests` | 8/8 | `Temp/ValidationResults/EditMode-20260930-005418-ebae7b1dce3941fcbc9a069c341215dc.xml` |
| ALL EditMode | 1978/1978 | `Temp/ValidationResults/EditMode-20260930-005435-d4a2e575f7d84b86ba9e134aab32d60d.xml` |
| Official complete Smoke | 5/5 | `Temp/ValidationResults/EditMode-20260930-005511-4e62263ddd5d404bbfe22239bcb6b4c5.xml` |
| `git diff --check` | PASS | Candidate tree |

## Limits retained

This witnesses Conflict rows only. It does not witness War or Battle, provide
cross-owner atomicity, global epoch invalidation, owner-thread/quiescence,
capture eligibility, export, staged hydration, or restore. Existing runtime
mutation-guard binding remains unchanged. P12-B remains incomplete and P12-A
remains `WAIT_DEPENDENCY`.
