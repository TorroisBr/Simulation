# P12-E Persistent Battle Census Design Review

**Result:** PASS — independent exact-tip design review.

**Reviewed proposal:** `codex/phase12/P12EBattleCensus` at
`ea4afa77268070694df59e136c8e58af81401ce7`.

**Base:** reviewed War census candidate
`4c6319164f11e1112ed21317f8cddb32f12bec61`.

The review confirmed that `SimulationRuntime` creates or clones its installed
`PersistentBattleStore` after resolving Conflict and War, and the store's
existing `Count` represents retained BattleId rows. The selected authored
bootstrap supplies no Battle owner, so the runtime-installed store starts
empty. Reporting `Runtime.BattleStore` as the exact owner identity follows
existing census providers.

Successful registration, binding changes, start, and terminal installation
advance the local store revision. The existing Battle transaction restores
the original record and exact pre-commit revision after failure following
terminal assignment. Extending the existing rollback test with census identity,
cardinality, and revision assertions is sufficient targeted evidence.

The bounded witness follows Conflict → War → Battle, reuses the current
read-only owner facts, and adds no mutation path. It does not establish global
invalidation, owner-thread/quiescence, capture eligibility, export, or
hydration. Existing accepted P12-B/P12-E authority covers this passive slice;
P12-B remains incomplete and P12-A remains `WAIT_DEPENDENCY`.
