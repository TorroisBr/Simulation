# P12-B Dynamic NPC SpatialKnowledge Census Design Revalidation

**Result:** PASS

- **Reviewed exact design tip:** `f3c7edee28968b6af0020c09a08fc0ab8740fe64`
- **Canonical code baseline:** `0a37e9f053b482d80d0815c95352e3d96b56ed8f`
- **Review scope:** amendment from reviewed design tip `156dcb19ecb8f15dc9611e9e0ee45637f775faa8`.
- **Review type:** independent source-backed technical-design revalidation; no edits or tests.

The amendment correctly narrows implementation tests to currently reachable
behavior. `PersonRuntime.TrySetResidenceSettlementRuntimeId` unconditionally
assigns and succeeds. After a fresh candidate has passed the synchronous
PersonStore and roster checks, its starting-City presence uses that City's own
Location and the candidate is alive and not traveling. The existing adoption
and materialization paths therefore have no deterministic supported
post-bind failure input. Adding a synthetic seam or new failure semantics
would exceed the accepted contract.

The fail-closed requirements remain: compensation return values must be
checked; any compensation failure must fault/block census assessment; and a
future supported post-bind rollback path must account for both shared-revision
PersonStore sections at one outer commit. The amendment remains within accepted
P12-B scope and preserves P12-A as `WAIT_DEPENDENCY`.
