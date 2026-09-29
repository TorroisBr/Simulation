# P12-E Manpower and Armed-Force Position Census Design Review

**Result:** PASS — independent exact-tip design review.

**Reviewed proposal:** `codex/phase12/P12EManpowerSpatialCensus` at
`c645446779b972becbd887ba0cf661106b575665`.

**Base:** `codex/phase12/P12BCensusOwnerIntegration` at
`cad27805f7f014ac753ebe6ecf34f3e322a90feb`.

The reviewer confirmed that the selected profile constructs runtime-owned
manpower and armed-force position stores, both exact-zero at startup, while
`LocalTopologyStore` alone is absent. The proposed section cardinalities,
owner identities, revisions, store boundaries, and battle rollback treatment
match the implementation. The inventory correction removes the stale
`NOT_COMPOSED` statement without changing P12 readiness or extending scope.

One nonblocking test suggestion is to assert that the manpower census provider
reports the restored revision after Battle rollback. Existing Battle tests
already verify restoration of the underlying store revision; implementation
may add the direct witness assertion without changing the reviewed contract.

No implementation tests were run during this design-only review.
P12-B remains incomplete and P12-A remains `WAIT_DEPENDENCY`.
