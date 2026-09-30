# P12-E Persistent War Census Design Review

**Result:** PASS — independent exact-tip design review.

**Reviewed proposal:** `codex/phase12/P12EWarCensus` at
`0a8a883226761a706e3c70c01967c1f4c756b91b`.

**Base:** reviewed Conflict census candidate `b170d4ea93cc7e3e780ea318b9925407df982639`.

The review confirmed that the runtime composes ArmedForce, Conflict, War,
then Battle, and binds War to the mutation guard. The fixed `p12e.wars`
section correctly reports the runtime-installed `WarStore`, its record count,
and local revision. Successful register/binding/end operations increment the
revision once; failure paths do not. `ConflictId` is optional: an unlinked War
is valid, while a supplied unresolved reference rejects.

The design preserves War-after-Conflict and Battle-after-War ordering and
does not duplicate upstream records or include Battle state. Existing
P12-B/P12-E capability authorization is sufficient for this passive-witness
slice. It does not provide global epoch, owner-thread/quiescence, capture
eligibility, export/hydration, or restore. P12-B remains incomplete and P12-A
remains `WAIT_DEPENDENCY`.

No implementation tests were run during this design-only review.
