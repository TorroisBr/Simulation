# P12-B TravelParty Census Design Review

**Result: PASS — independent exact-tip technical design review.**

**Reviewed design:** `codex/phase12/P12BTravelPartyCensusDesign` at
`765e346214e128104ce18c967121ef29abb88195`, file
`docs/design/PHASE12_P12B_TRAVEL_PARTY_CENSUS_DESIGN.md`.

**Canonical base:** `codex/phase12/canonical` at
`e9ced8e451f42e80ed2132ce494cd5c26e439894`. The design tip is based directly
on this canonical commit. The promoted Inventory provider prerequisite at
`15e5a543f3896c02f9dbd9c36c9ba75bc90dac2b` is included in the base.

## Findings

The proposal stays within the accepted P12-B passive owner-census scope. It
binds one schema-v1 `p12f.travel-parties` witness to the exact composed
`TravelPartyStore`, counts `ActiveParties`, and reports a store-owned monotone
revision. Its fixed composition provider does not extend the sealed dynamic
NPC/Person/SpatialKnowledge/Inventory roster protocol or its reconciliation
path. The required day-zero exact-zero witness is distinct from a permanently
empty section.

The store-local reentrant monitor covers public `Add`, `Complete`, and
`Remove`, plus the specified compound windows in `TryStartTravelParty`,
`AdvanceParties`, and `ExpeditionSystem.TryBeginReturn`. The two-commit
capacity preflight occurs before start-side gameplay mutations; final-arrival
capacity is checked before member progress. The max-1 rejection and max-2
Add-plus-compensation boundaries are internally consistent. The design also
requires one reference-identical store across composition, group travel, and
`ExpeditionSystem`; its test scope correctly limits Expedition compensation
claims to party-store commits rather than unrelated NPC or cost rollback.

The final revision explicitly bounds the monitor guarantee to the selected
authored recorder/logger path with no same-thread callback that re-enters the
store while a compound window is held. Arbitrary injected reentrant callbacks
are outside this slice; supporting them later requires a scoped commit permit
and tests. This caveat is necessary because a reentrant monitor alone does not
reserve the preflighted capacity against same-thread nested writes. The design
does not claim global thread safety, owner-thread affinity, quiescence, or a
synchronized census read.

## Limits

This review is for the design only; no implementation or tests were run. The
slice does not complete P12-B, make P12-A ready, establish full profile
coverage or capture eligibility, provide shared-epoch invalidation,
owner-thread/quiescence proof, export/hydration, or authorize canonical
promotion. P12-B remains incomplete and P12-A remains `WAIT_DEPENDENCY`.
Implementation review and required validation remain separate gates under
the execution model.
