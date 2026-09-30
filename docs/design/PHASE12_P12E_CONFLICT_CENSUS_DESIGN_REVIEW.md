# P12-E Persistent Conflict Census Design Review

**Result:** PASS — independent exact-tip design review.

**Reviewed proposal:** `codex/phase12/P12EConflictCensus` at
`27c74e602fe9e39e809667ffa70c3080603c1292`.

**Base:** `codex/phase12/P12EManpowerSpatialCensus` at
`7a4a95af57e342e94a0d8d7ae781a1f69b6a1daf`.

The review confirmed `p12e.conflicts` reports the exact runtime-installed
`PersistentConflictStore`, its `Count`, and its local `Revision`. Default
composition creates an empty store and binds it to the runtime mutation guard.
Successful register, participant-binding, and end transitions each increment
the revision; rejected operations preserve the witness.

The design correctly assigns side and ArmedForce link validation to
`PersistentConflictStore` and leaves Conflict references to
`PersistentWarStore` and `PersistentBattleStore`. The required evidence now
covers missing-Conflict and unregistered-ArmedForce binding rejections.

This is passive census work only. It does not provide export/hydration, global
epoch invalidation, owner-thread/quiescence, capture eligibility, or P12-B or
P12-A readiness. No tests were run during this design-only review.
