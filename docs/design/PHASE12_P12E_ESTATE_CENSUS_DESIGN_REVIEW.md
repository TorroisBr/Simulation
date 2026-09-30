# P12-E Estate Census Design Review

**Result:** PASS — independent exact-tip design review.

**Reviewed proposal:** `codex/phase12/P12EEstateCensus` at
`70b2de9caf3a190c81c3416448a992c9192902c3`.

**Base:** implementation-reviewed Battle census candidate
`6300c5b3debd1ebbca7c68a545dc9c3469dd5d50`.

The review confirmed that `SimulationRuntime` exposes an installed
`EstateStore`, clones supplied records into an owner bound to the resolved
runtime `PersonStore`, and binds the installed owner to the runtime mutation
guard. Default composition creates an empty Estate store. `Count` is one per
retained EstateId; the deceased-Person lookup is a secondary index, not a
second record set. Successful live registration advances the local revision
once; rejected domain operations do not change either index or revision.

The existing accepted P12-B/P12-E capability authorization covers this
passive witness. The design preserves P12-B incomplete and P12-A
`WAIT_DEPENDENCY`, and makes no export/hydration, shared-epoch,
owner-thread/quiescence, capture-eligibility, or restore claim.

Implementation clarification: runtime construction also invokes the same
internal registration while cloning existing Estate records. The candidate
will identify that construction-time copy population separately from live
explicit opens and will test duplicate EstateId rejection with a different
deceased Person, in addition to repeated-person and invalid-Person paths.
