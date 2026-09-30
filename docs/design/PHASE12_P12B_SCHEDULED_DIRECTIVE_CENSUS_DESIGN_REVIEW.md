# P12-B ScheduledDirective Census Design Review

**Result:** PASS — independent exact-tip design review.

**Reviewed proposal:** `codex/phase12/P12BScheduledDirectiveCensusDesign` at
`f01c827781c9f18875b3ccd700cb58d282ee9e94`.

**Original base:** P12 canonical `e9ced8e451f42e80ed2132ce494cd5c26e439894`.

**Revalidated against:** P12 canonical
`19d0373d6a71b63536248ecc9091e66c9b3a708b` after TravelParty promotion.

The proposal matches the current `ScheduledDirectiveStore` and
`ScheduledDirective` contract. The exact composed store is the owner; the
section counts stored rows, including terminal rows, and reads count/revision
as one owner-local sample. The proposed owner callback preserves the existing
public `MarkSucceeded`, `MarkFailed`, and `MarkSkipped` entrypoints, so direct
transitions on stored directive objects cannot bypass revision accounting.
Standalone directives retain their current behavior.

The commit matrix correctly treats accepted Add as one commit even when
existing schedule rules immediately mark it Skipped. It accounts for
`PrepareDay` conflict and unresolved-actor skips, rejects saturated revisions
before visible mutation, and leaves transient `TryTakeDirective` consumption
and no-op preparation outside the persistent owner revision. Runtime-only
callback binding is appropriate for the serializable directive type. The
proposed provider uses the exact installed store, and its profile tests cover
owner identity, initial and populated cardinality, direct transitions,
no-ops, saturation, and coherent samples.

The TravelParty promotion adds a separate `TravelPartyCensusProvider` to
`SimulationBootstrapComposition`, so both implementations will touch that
composition hotspot. This requires intentional integration and exact-tip
revalidation, but it does not alter the ScheduledDirective ownership,
mutation, or profile contract and does not invalidate this design.

P12-B remains incomplete: the proposal does not provide shared committed-write
invalidation, runtime owner-thread/quiescence, a complete profile inventory,
or capture eligibility. It claims neither P12-B completion nor P12-A
readiness/export-hydration. The proposal stays within the already accepted
P12-B capability scope. No blocking findings were identified. No Unity tests
were run because this is a design-only review.
