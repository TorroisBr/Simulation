# P12-B Dynamic NPC SpatialKnowledge Census Design Review

**Result:** PASS

- **Reviewed design tip:** `156dcb19ecb8f15dc9611e9e0ee45637f775faa8`
- **Design base:** P12 canonical `0a37e9f053b482d80d0815c95352e3d96b56ed8f`
- **Review type:** Independent technical-contract review; no implementation or tests.

The revised contract resolves the prior review findings. It includes both
fixed `PersonStore` census sections in the same outer commit whenever their
shared revision changes, including compensated binds whose cardinality returns
to its starting value. A runtime-owned nesting context prevents nested NPC
registration and rollback calls from publishing an early family snapshot.
Dynamic family membership and changed fixed sections are validated and
published atomically with at most one mutation-epoch advance.

The review confirms the proposed boundary preserves current materialization,
adoption, registration, and unregistration semantics. It also correctly keeps
failed-compensation handling fail-closed and retains the known City
`ImportantNpcs` projection, other `PersonStore` writers, complete owner-thread
and quiescence proof, and all export/hydration work as unresolved blockers.
The proposal does not claim complete P12-B coverage, P12-A readiness, or phase
closure.

No tests were run because this review covered a documentation-only technical
design. Implementation must use an isolated branch from the then-current P12
canonical tip, serialize the `SimulationRuntime` and materialization hotspot,
and provide the evidence listed in the design before candidate review.
