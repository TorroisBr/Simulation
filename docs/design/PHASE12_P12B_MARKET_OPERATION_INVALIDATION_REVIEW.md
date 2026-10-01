# P12-B Market Operation Invalidation — Independent Implementation Review

**Disposition:** PASS — exact candidate tip reviewed; canonical promotion remains
a separate human gate.

## Exact review target

- Canonical base: `codex/phase12/canonical` at
  `b8a7da54864bee3fb9b8916793240e91fbce0955`.
- Published implementation candidate:
  `codex/phase12/P12BMarketOperationInvalidation` at
  `b577312c2edc6d3124c6d2b9dd3a1201dc9055ae`.
- Candidate tree: `88ec452a979a7439b825858a6b1b271f8bc6c948`.
- Validation record:
  [`PHASE12_P12B_MARKET_OPERATION_INVALIDATION_CANDIDATE.md`](PHASE12_P12B_MARKET_OPERATION_INVALIDATION_CANDIDATE.md).

## Review findings

Independent exact-tip re-review passed after the first review identified
missing coverage for a successful changed-price refresh and the daily
Free-consumption/price-refresh paths. The candidate now verifies:

- A direct changed-price refresh advances the Market local revision and the
  partial census epoch; a subsequent no-op refresh advances neither.
- A successful daily Free-consumption commit changes Market stock, revision,
  and the partial epoch inside `TryAdvanceDay`.
- A successful daily changed-price refresh changes price, revision, and the
  partial epoch inside `TryAdvanceDay`.

The implementation still matches the bounded Open-market contract. The exact
composed Market owners are registered before profile sealing. Bound purchase
and sale preflight the exact rostered NPC account, Inventory, and Market
identity/revision before writes and report committed revisions, including
successful compensation. The composed Markets use the exact shared transaction
service. Owner-thread rejection, baseline checks, revision exhaustion, and
account-backed exclusion remain in scope as documented by the candidate.

Direct Market mutators are guarded by owner-thread and baseline checks and
notify their Market section after a successful commit, but they do not require
an active named operation scope. Daily production, Free consumption, and price
refresh are observed inside the existing `runtime.advance-day` scope. This
remains a bounded partial invalidation path, not a universal write boundary.

## Evidence and limits

The candidate record lists the focused, full EditMode, and official Smoke
results with XML/log hashes. The independent reviewer inspected the record but
did not rerun the tests or independently recompute artifact hashes.

This review does not establish complete owner coverage, complete shared-epoch
coverage, capture eligibility, export, hydration, P12-B completion, P12-A
readiness, or P13 readiness. P12-B remains incomplete; P12-A remains
`WAIT_DEPENDENCY`; P13 remains blocked; Phase 12 remains open.
