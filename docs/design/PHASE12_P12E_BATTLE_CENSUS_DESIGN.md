# P12-E Persistent Battle Census Design

**Status:** Independent exact-tip design review PASS. Durable record:
`PHASE12_P12E_BATTLE_CENSUS_DESIGN_REVIEW.md`.

**Base:** reviewed and validated War census candidate
`codex/phase12/P12EWarCensus` at `4c6319164f11e1112ed21317f8cddb32f12bec61`.

**Authorization:** Existing accepted P12-B/P12-E capability authorization
covers this passive census slice. It does not authorize P12-A profile
integration or claim P12-B completion.

## Dependency and fixed section

Battle follows War in the current runtime-owned conflict/war/battle chain.
Add one schema-v1, read-only `p12e.battles` section through the existing
`SimulationBootstrapComposition` handoff:

| Section | Installed owner | Cardinality | Existing stamp |
|---|---|---|---|
| `p12e.battles` | `Runtime.BattleStore` | `Count` | `Revision` |

The provider reports the exact installed `PersistentBattleStore` object as
owner identity. It reads one row per retained Battle, not participant rows,
side rows, or outcome details. The normal selected authored profile starts
with an empty Battle store; its live profile witness must show exact count and
revision zero and retain the same owner identity across repeated reads.

This section is dependency-ordered after the reviewed Conflict and War
witnesses. It neither duplicates their records nor treats optional
`ConflictId`/`WarId` references as owned Battle rows. No new Battle mutation,
validation, or restore authority is introduced.

## Existing revision and rollback semantics

Successful Battle registration, participant-binding changes, start, and
terminal-outcome installation advance the existing local store revision.
These lifecycle writes replace or add a Battle record while Battle
cardinality remains the number of retained Battle identities. Rejected
operations do not advance the store revision.

The terminal outcome service captures the pre-transaction Battle record and
store revision. If a later part of that transaction fails, the existing
`RestoreBattleTransactionSnapshot` reinstalls the prior Battle row and prior
revision. The census therefore reports the exact current local revision, which
can return to its previous value after rollback; it is not a monotone global
epoch. Tests must exercise a failure after terminal assignment and confirm
the census owner identity, count, and revision match the pre-transaction
witness. This makes rollback visibility explicit without adding a new stamp
or changing transaction semantics.

## Required evidence

- The selected live profile reports schema-v1, exact installed-owner
  identity, count/revision zero, and stable identity on a repeated read.
- A focused provider test confirms Battle registration and participant or
  lifecycle writes advance local revision while retained Battle cardinality
  is counted once per `BattleId`; rejected registration leaves the witness
  unchanged.
- Existing successful Battle resolution coverage is extended to assert that
  installing a terminal outcome advances revision without changing Battle
  count.
- Existing injected exception-after-terminal-assignment transaction coverage
  is extended to compare pre/post census owner identity, count, and exact
  restored revision, alongside its current world-state/revision rollback
  assertions.
- Reuse existing Battle invariants, guard behavior, and rollback machinery.
  The provider adds only a read path.

## Deferred boundaries

This slice does not complete the profile census or shared mutation-epoch
invalidation, prove Unity owner-thread/quiescence, issue capture eligibility,
or add export, staged hydration, persistence, or restore. It does not alter
Battle terminal transaction behavior or include gameplay beyond the existing
Battle domain. P12-B remains incomplete and P12-A remains
`WAIT_DEPENDENCY`.
