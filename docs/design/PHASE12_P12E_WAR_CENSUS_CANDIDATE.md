# P12-E Persistent War Census Candidate

**Status:** Implementation and validation complete; independent exact-tip
implementation review PASS. Durable review: `PHASE12_P12E_WAR_CENSUS_REVIEW.md`.

**Canonical promotion:** Included in the cumulative census stack promoted at
`b889b47`; exact integration review passed at `35ec988`, with full-tree
validation on code tip `65ebc7f`.

**Candidate branch:** `codex/phase12/P12EWarCensus`.

**Code-bearing candidate:** `90a6103` (`Add persistent war census
witness`). It extends the reviewed Conflict census candidate at `b170d4e`.
The exact-tip design review PASS is recorded in
`PHASE12_P12E_WAR_CENSUS_DESIGN_REVIEW.md`.

## Delivered boundary

The bootstrap composition publishes one fixed schema-v1 passive section:
`p12e.wars`. It reports `Runtime.WarStore.Count`, that installed store's
existing `Revision`, and the exact runtime-installed `PersistentWarStore` as
owner identity. The selected authored profile verifies exact day-zero
count/revision zero and stable owner identity across repeated reads.

Tests verify successful War registration, participant binding, and ending
advance revision while record count remains unchanged. They verify an
unresolved supplied Conflict reference, unregistered-ArmedForce binding,
post-end binding, and duplicate-registration rejection preserve the witness.
They also prove a War without the optional Conflict reference remains valid.

## Validation

| Gate | Result | Evidence |
|---|---:|---|
| `PersistentWarCensusTests` | 1/1 | `Temp/ValidationResults/EditMode-20260930-010448-f027cfb723e54051b27004653856b915.xml` |
| `SimulationBootstrapCompositionTests` | 14/14 | `Temp/ValidationResults/EditMode-20260930-010509-32bb9f90fbac4330a068fd593a67b878.xml` |
| `PersistentConflictWarBattleStateTests` | 8/8 | `Temp/ValidationResults/EditMode-20260930-005418-ebae7b1dce3941fcbc9a069c341215dc.xml` |
| ALL EditMode | 1979/1979 | `Temp/ValidationResults/EditMode-20260930-010528-7beaa704ea214b56ad690fce5a367bf1.xml` |
| Official complete Smoke | 5/5 | `Temp/ValidationResults/EditMode-20260930-010605-da41f726a971497d95df34aa2b751b6c.xml` |
| `git diff --check` | PASS | Candidate tree |

The War-containing exact-tip rerun of `PersistentConflictWarBattleStateTests`
also passed 8/8 at
`Temp/ValidationResults/EditMode-20260930-010819-114623b4ce0b4d3da3752930e5f8fe37.xml`.

## Limits retained

The witness covers War rows only. It does not witness Battle, duplicate
Conflict/ArmedForce facts, provide cross-owner atomicity, global epoch
invalidation, owner-thread/quiescence, capture eligibility, export, staged
hydration, or restore. P12-B remains incomplete and P12-A remains
`WAIT_DEPENDENCY`.
