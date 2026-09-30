# P12-B Expedition owner census design review

**Verdict:** PASS — bounded technical design review only.

**Reviewed candidate:** `codex/phase12/P12BExpeditionCensusDesign` at
`7e6851540b016cea10abcb16b7a71091fb776ea6`.

**Exact base:** `codex/phase12/canonical` at
`e9ced8e451f42e80ed2132ce494cd5c26e439894`.

The review checked the candidate design and its amendment against the exact
base's P12-B blocker map, ExpeditionStore/Runtime/System and autonomy source,
and the current intraday and multi-participant alignment records. The candidate
adds only the bounded P12-B passive Expedition owner census contract; no code
or tests were changed or run for this review.

## Review findings

- The owner is the exact composed `ExpeditionStore`; cardinality is its active
  list count and revision covers committed active-runtime/objective truth.
  The design requires list/index/reference identity agreement and fail-closed
  behavior for drift or invalid/saturated revisions.
- Attachment and exact-object completion preserve identity: an attached
  `TryComplete` delegates to store finalization, which checks the expected
  object and active membership, then changes state and removes the same object
  from both indexes in one revision. A stale, mismatched, missing, or
  wrong-state object cannot partially finalize.
- The start and return two-slot reservations consistently protect each
  separately committed ExpeditionStore write, including compensation. Other
  writes account for outstanding slots; one-slot/two-slot saturation cases,
  successful and compensating paths, and reservation release are called out
  without claiming an atomic transaction across TravelParty or NPC owners.
- Objective reservations match the source's resource, notable-item and
  opposition completion paths. They bind the exact expedition/objective and
  expected eligibility; the matching token alone may commit. The same
  expedition's direct runtime/objective, return, completion, and membership
  writes are explicitly fenced while the external operation runs. Capacity is
  reserved before external effects, unused tokens release on failure or
  exception, and unrelated expeditions may still commit when capacity remains
  after reservations.
- The ample-capacity eligibility interleaving is separated from the
  `long.MaxValue` saturation tests and uses an injected boundary, so it can
  deterministically test owner-busy behavior without timing races. Keep that
  seam narrow and internal to the operation/test; do not rely on sleeps or
  claim rollback of effects in other owners.
- Expedition instance identity remains distinct from its member, performer,
  and support NPC identities. This does not impose a universal one-activity to
  one-actor relationship, change P18/P20 dependencies, or add timeline state
  to the accepted P12-A daily profile.

The contract remains passive P12-B owner evidence. It does not connect the
owner to the shared mutation epoch, establish runtime-wide thread ownership or
quiescence, grant capture eligibility, close P12-B, make P12-A ready, or define
P12-F export/hydration. Implementation still follows the accepted P12-B
dependency and checkpoint gates. Cross-owner rollback and transaction
semantics remain outside this design.
