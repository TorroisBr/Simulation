# P12-E Institution and Office Census Design Review

**Result:** PASS — independent exact-tip design review.

**Reviewed proposal:** `codex/phase12/P12EPropertyCensus` at
`e22b8b3fa2301d53bd2113c6fbffde41b7b37e0c`.

**Base:** Property census candidate at `1dc9ea2`.

The review confirmed that current runtime composition clones Institution
before Office; Office cloning replays office rows, active incumbencies (which
create open tenure rows), then closed historical tenure records. The clone's
revision is a fresh local post-composition baseline and is not required to
equal the source revision. Office vacancy closes a tenure in place, so its
tenure cardinality stays constant while its content changes; the Office
revision must still advance once for that successful commit. Existing
expected failure paths reject before writes, and overflow must be checked
before the first write. Append `RevisionOverflow` to the existing failure
enum to preserve numeric values of current members.

The dependency order is resolved InstitutionStore → OfficeStore →
PropertyOwnershipStore; PoliticalClaim cloning follows and consumes Person,
Institution, Office, and Property owners. Multiple sections from one Office
owner are consistent with the existing census provider pattern. The design
now names the actual in-assembly installed-owner accessors
`InstitutionStoreForWorldBoundary` and `OfficeStoreForWorldBoundary`.

Returned validation, guard, and overflow failures must leave state unchanged;
process-level exception rollback is not claimed. Because the three Office
sections share one revision, future shared epoch wiring must refresh all
three section baselines on every OfficeStore commit, even if only one
cardinality changes.

The existing P12-B–P12-G accepted capability scope covers this passive
census plus owner-local revisions; no new human checkpoint is required.
Shared P12-B epoch wiring, thread/quiescence, export/hydration, political
Claim census, and P12-A remain deferred. No blocking findings were reported.
