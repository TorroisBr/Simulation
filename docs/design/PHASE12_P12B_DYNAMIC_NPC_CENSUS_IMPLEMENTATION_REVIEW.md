# P12-B Dynamic NPC SpatialKnowledge census implementation review

**Result:** PASS at exact code tip `2c782a7a08abfc1c8b2ba9201efad12f4ac279ae`.

- **Implementation branch:** `codex/phase12/P12BDynamicNpcCensusImplementation`
- **Candidate evidence tip reviewed:** `d7ece9af9268cb38e5596c3e7433ceea5e05b360`
- **Exact implementation base:** `0a37e9f053b482d80d0815c95352e3d96b56ed8f`
- **Reviewed design contract:** `f3c7edee28968b6af0020c09a08fc0ab8740fe64`
- **Contract revalidation record:** design branch tip `f6a86405429baa7b310272d75fe200a3eff801f7`

## Review findings

The complete code diff was reviewed against its exact canonical base. The
candidate implements the bounded roster-following SpatialKnowledge family and
the affected fixed PersonStore census sections. Dynamic-family reconciliation
stages owner identities, witnesses, and baselines before publishing one
combined delta. The reviewed tests cover the accepted add/remove,
materialization/adoption, compensation-accounting, ordering, identity,
revision-drift, and fail-closed cases.

The initial exact-tip review found that a cross-thread nested roster call could
join the active runtime-owned census context before the protocol's owner-thread
check. Commit `2c782a7` records the context's owning `Thread` and managed thread
ID. A mismatched nested entry now faults this partial census and uses a
detached scope, so it cannot change the live context's nesting/accounting or
publish a family reconciliation or epoch. The focused regression exercises
that path. This is only the local NPC-membership census boundary; it does not
prove global runtime thread affinity or quiescence.

No blocking correctness or scope finding remains. A local variable named
`currentFixedProviders` in the reconciliation path is unused; this is a
non-blocking cleanup observation and does not affect the validated behavior.

## Validation evidence

The candidate note retains the exact Unity XML and log paths. The reviewed
results parse as passed:

- `ContinuationCensusProtocolTests`: 22/22
- `SpatialKnowledgeCensusTests`: 16/16
- ALL EditMode: 2035/2035
- Complete official `Smoke` filter: 5/5
- `git diff --check`: passed on the committed code diff

The full EditMode run includes the PersonStore and bootstrap suites; the
separate retained XMLs for those suites predate the thread-boundary correction
and are not used as exact-tip evidence.

The complete Smoke log confirms the `-testFilter Smoke` invocation and
`groupNames = Smoke`. Unity-generated settings and `.meta` files present in the
worktree were not included in the candidate commits.

## Limits

This review approves only the submitted partial passive NPC/Person census
candidate. P12-B remains incomplete and P12-A remains `WAIT_DEPENDENCY`.
City `ImportantNpcs`, other owner sections and writers, complete profile
inventory, global owner-thread/quiescence, capture eligibility, and all export
and hydration work remain unresolved. Canonical promotion and Phase closure
are not approved by this review.
