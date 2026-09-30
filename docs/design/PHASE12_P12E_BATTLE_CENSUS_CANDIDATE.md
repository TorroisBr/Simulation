# P12-E Persistent Battle Census Candidate

**Status:** Implementation, validation, and independent exact-tip review
complete. P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`.

**Branch:** `codex/phase12/P12EBattleCensus`.

**Code-bearing candidate:** `2c94fb13a4744e525b0c755674e8cab785f7a945`.

**Reviewed design:** proposal `ea4afa77268070694df59e136c8e58af81401ce7`,
design review recorded in `PHASE12_P12E_BATTLE_CENSUS_DESIGN_REVIEW.md` and
committed at `34ecb5a0d36fb87ec63c0b191347d07038df8bdd`.

**Implementation review:** PASS on exact code tip `2c94fb1` against design-
reviewed base `34ecb5a`. Durable review record is
`PHASE12_P12E_BATTLE_CENSUS_REVIEW.md`.

## Delivered boundary

The bootstrap publishes one fixed schema-v1 passive section, `p12e.battles`,
over the exact runtime-installed `Runtime.BattleStore`. It reports that store
object as owner identity, `Count` as the number of retained BattleId rows, and
the existing local `Revision`. It does not count side, binding, or outcome
details as independent Battles.

Coverage confirms the selected authored profile begins at exact count/revision
zero with stable owner identity. Registration, participant binding, and start
advance the local revision while the row count remains one; duplicate
registration leaves the witness unchanged. Successful terminal resolution
advances revision without changing cardinality. An injected exception after
terminal assignment restores the original owner witness identity, count, and
exact prior local revision.

The Battle revision can move back on transaction rollback; it is not a global
monotone epoch. The provider adds no Battle write or restore authority and
preserves Conflict → War → Battle ownership.

## Validation

| Gate | Result | Evidence |
|---|---:|---|
| `PersistentBattleCensusTests` | 1/1 | `Temp/ValidationResults/EditMode-20260930-011653-a38b9ccd804247099a858e04f63af5b9.xml` |
| `SimulationBootstrapCompositionTests` | 14/14 | `Temp/ValidationResults/EditMode-20260930-011713-af660076ebf54db98b3168319d360c2f.xml` |
| `BattleOutcomeApplicationTests` | 20/20 | `Temp/ValidationResults/EditMode-20260930-011735-4eb7c095e0c84a4ba8fc18079b32c0e8.xml` |
| `PersistentConflictWarBattleStateTests` | 8/8 | `Temp/ValidationResults/EditMode-20260930-011759-64dcd813a90c43aeaf10f62d46194aaf.xml` |
| ALL EditMode | 1980/1980 | `Temp/ValidationResults/EditMode-20260930-011935-25dccc30706c410d843f760fff13e41d.xml` |
| Official complete Smoke (`-TestFilter Smoke`) | 5/5 | `Temp/ValidationResults/EditMode-20260930-012201-64562c80a7854b5c9069e227940329ba.xml` |
| `git diff --check` | PASS | Candidate tree |

## Limits retained

This witness does not establish complete profile owner registration, connect
all supported writes to the shared invalidation epoch, prove Unity
owner-thread/quiescence, issue capture eligibility, or add export, staged
hydration, persistence, or restore. It does not close P12-B, make P12-A
ready, or authorize P12-A implementation.
