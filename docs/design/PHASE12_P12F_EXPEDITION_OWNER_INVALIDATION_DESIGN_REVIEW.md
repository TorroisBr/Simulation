# P12-F Expedition Owner Invalidation — Exact-Tip Design Review

**Verdict:** PASS — independent technical-design review of the documentation tip.
This is not an implementation candidate validation, an implementation-ready
status, canonical promotion, or Phase closure.

## Exact references

- Candidate design tip: `747ff2dc35cf75047c497539215a1752e73cc636`
- Candidate parent: `122004397bfb0aa7e0f74230dbdb0672969c4b2f`
- Retained P12 canonical base: `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e`
- P12 canonical remote at review: `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e`
- Current architecture and P12 matrix baseline: `f6924e63d8e5731da1d33021d0361e7defe6dad7`
- Candidate branch: `codex/phase12/P12BExpeditionStartInvalidationDesign`
- Reviewed artifact: `docs/design/PHASE12_P12B_EXPEDITION_OWNER_INVALIDATION_DESIGN.md`

The candidate branch diff from its retained P12 base is documentation-only and
adds only the Expedition design artifact. This review evaluated the exact final
tip, including the callback-exception matrix added after earlier review.

## Findings and resolution

The prior return-lock finding is resolved. The design correctly describes the
existing `TryBeginReturn` order: it holds the reentrant `TravelPartyStore`
mutation window while calling `ExpeditionStore.CommitReserved`, nested party
start, and the association commit. It explicitly prohibits future Expedition
callbacks from acquiring the party mutation window or introducing the reverse
lock order. This matches `ExpeditionSystem.TryBeginReturn`,
`TravelPartyStore.EnterMutationWindow`, and the reviewed source paths.

The prior compensation/exception finding is resolved. The summary now
separates TravelParty-start exceptions and boolean-false outcomes from a
boolean-false final association result and from final-association callback
exceptions. The detailed matrix matches the source: preflight exceptions occur
before the affected owner mutation; post-commit reporting exceptions do not
undo a completed commit; the final association exception bypasses cancellation
and party removal; and a thrown cancellation skips subsequent party removal.
The required evidence section asks future implementation tests to assert the
resulting Expedition/TravelParty state and compensation/event behavior at each
commit and compensation boundary.

The current architecture additions are reflected by exact hash and do not
conflict with this bounded owner design. It introduces no activity semantics,
participant cardinality rule, mod hook, or new gameplay contract.

The readiness boundary is correct. P12-F Expedition integration remains
**NOT READY** until P12-C, P12-D, and P12-E are canonical and the included owner
set is refreshed. The proposal does not revise the matrix's dependency edges.
It expressly excludes Expedition owner-section registration and shared-epoch
wiring from the P12-B runtime-admission adapter. No implementation or
canonical promotion is authorized by this review.

## Validation and constraints

This exact-tip change and review are documentation-only; no Unity tests or
source validation were run. Future implementation must revalidate its exact
base, perform the design's focused Expedition/TravelParty/runtime/bootstrap
coverage and the required full validation, then receive independent exact-tip
code review. P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, and
P13 remains blocked.
