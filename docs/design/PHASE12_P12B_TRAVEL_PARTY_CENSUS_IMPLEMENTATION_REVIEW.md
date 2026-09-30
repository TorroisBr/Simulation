# P12-B TravelParty census implementation review

**Result: HOLD — independent exact-tip implementation review.**

**Candidate branch:** `codex/phase12/P12BTravelPartyCensusImplementation`
at docs tip `d2663dda305c0dfe229c83b1999b0888fb276c21`.

**Code-bearing tip:** `8c446ed8920028566b8af84583e7599bc907791c`, tree
`bf95798acdf932a91d54f52c8604ba20928f2cd0`.

**Base:** `codex/phase12/canonical` at
`e9ced8e451f42e80ed2132ce494cd5c26e439894`. Canonical, candidate, and review
branch refs were verified locally; `git ls-remote` confirmed the canonical
and candidate remote refs at the same SHAs before review.

## Review scope and findings

I inspected the full implementation diff against the exact base, the reviewed
design and design review, the candidate evidence, and the amended tests.
The provider reports the exact installed `TravelPartyStore` identity,
`ActiveParties.Count`, and store-owned revision. Store Add/Complete/Remove
serialize their validation and owner commit, preflight overflow, preserve the
existing Complete-versus-Remove lifecycle behavior, and increment only for a
successful mutation. Group start reserves two owner commits before travel
preparation or side effects; arrival checks completion capacity before member
progress; Expedition rejects a split store and holds the same reentrant monitor
through return start, association, and store compensation. The implementation
remains limited to TravelParty owner membership/revision and does not claim
cross-owner rollback or general runtime thread safety.

The targeted alignment review passes. TravelParty instance IDs remain distinct
from member `NpcRuntimeId`s; owner cardinality counts active party instances,
not participants. Existing travel members remain individual NPC identities,
and this slice adds no persistent Activity-to-Actor cardinality rule, NPC-owned
authority contract, formation behavior, or generic participant model. It does
not edit P18 scheduler, lease, timeline, or handoff code and does not imply
intraday execution. The candidate preserves the accepted passive P12-B census
boundary and its explicit lack of global epoch, quiescence, complete profile
coverage, or capture eligibility.

Two review blockers remain:

1. `ExpeditionTests.ReturnAssociationRaceCompensatesPartyStoreAtSaturationBoundary`
   attempts to force the failure by polling `expedition.State == Returning`
   from one thread while another immediately proceeds to party start and
   association. There is no synchronization point after party Add and before
   association. Scheduling may let the return thread associate first, so the
   test does not deterministically exercise the documented compensation path.
   Replace the state-poll race with a controlled barrier at the return-party
   start-event recorder boundary: pause after the TravelParty Add while the
   store window is held, complete the Expedition from the test thread, then
   resume event recording and verify failed association plus the Add/Remove
   revision result. Keep assertions limited to this owner as the design
   requires.

2. The candidate document cites exact-code XMLs for focused TravelParty,
   GroupTravel, Expedition, bootstrap, and ALL EditMode runs, but those five
   XML/log pairs are not present in the reviewed worktree's `Temp` or
   `Library/ValidationResults`, nor found in the repository worktrees. The
   cited complete Smoke XML/log is present and passes 5/5; its log confirms the
   project path is this candidate worktree. The recorded counts are plausible,
   but I could not inspect the cited focused/full-suite outputs. Preserve or
   otherwise make those exact-tip validation artifacts available for the next
   review. No tests were rerun as part of this independent review.

## Review limits

This review does not promote code or change Phase state. P12-B remains
incomplete and P12-A remains `WAIT_DEPENDENCY`. The implementation should be
re-reviewed at a new exact code tip after the compensation test is made
deterministic and exact-tip validation evidence is available.
