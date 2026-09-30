# P12-D Genealogy Owner Census Design

**Status:** Bounded technical proposal; independent design review pending.

**Base:** `codex/phase12/canonical` at
`69f456d5e3c6d6f7e4b85b36e98968ced0549bf3`.

**Authority:** The accepted P12 capability decomposition assigns genealogy
truth to P12-D. The P12-B blocker-resolution sequence calls for D owner
witnesses after the canonical census protocol, using isolated owner work and
without treating owner evidence as P12-D completion.

## Purpose and boundary

Add one passive, owner-issued census witness for the runtime-installed
`GenealogyStore` so the selected profile can distinguish a composed-empty
genealogy owner from a missing owner and can observe later committed parentage
changes. This is a P12-B live-owner/cardinality evidence prerequisite within
the accepted P12-D capability scope; it does not create a new checkpoint.

The selected `UnityBootstrap-Daily-v1` profile currently composes an empty
`GenealogyStore`. The runtime installs a clone and exposes the exact installed
owner internally through `SimulationRuntime.GenealogyStoreForWorldBoundary`.
Construct the census adapter from that installed clone after normal runtime
composition. Do not retain or witness the constructor source store, expose the
raw owner through the bootstrap, or add a general registration hook.

## Section contract

Publish one required schema-v1 provider-backed section:

| Section | Exact owner | Cardinality | Revision |
|---|---|---|---|
| `p12d.genealogy.parentage` | installed `GenealogyStore` | `GenealogyStore.Count`, the direct parentage-edge record count | owner-local monotone commit revision |

The witness reports the installed store object as its opaque owner identity.
`GenealogyStore.Records` is already a deterministic read-only snapshot; it and
the derived parent/child adjacency indexes are not separate census sections.
Cardinality counts direct edges, not distinct people or transitive
ancestor/descendant pairs. Repeated reads without a successful write preserve
owner identity, cardinality, and revision. The selected authored bootstrap
starts at exact zero; zero means composed and empty, not excluded or absent.

Use a fixed `IOwnerSectionCensusProvider` owned by
`SimulationBootstrapComposition`. Construct it from the installed owner via
the existing internal runtime accessor after `SimulationRuntime` is built.
Expose the provider through a typed read-only bootstrap property so the
eventual P12-B inventory can register the exact section; do not expose the
`GenealogyStore` itself or mutable census membership. The witness remains
ephemeral runtime identity and is not serialized.

## Owner revision and commit behavior

Add a read-only `long Revision` initialized to zero on each new store. Advance
it exactly once after each successful `TryAddParentage` or
`TryRemoveParentage` store commit. The `ParentageRecord` overload delegates to
the same add path and must not double-increment. Increment on successful
removal even though the adjacency indexes and edge count both shrink; the
revision records authoritative content changes, not only count changes.

All existing semantic failures—runtime fault, invalid/null endpoints,
self-parent, duplicate edge, cycle creation, and missing edge—leave both
records and revision unchanged. Preserve existing enum numeric values and
append one `RevisionOverflow` failure at the end of `GenealogyFailureCode`.
When revision is `long.MaxValue`, return that failure before the first write
to the edge set or adjacency indexes. Do not add a public/test-only revision
setter or claim exception-level rollback.

Runtime clone construction already rebuilds genealogy by replaying the
deterministically ordered source records through `TryAddParentage`. The
installed clone's revision is therefore its local post-composition baseline
from successful replay; it is not persisted state and need not equal the
source revision. Census identity must refer to the clone. Source records,
indexes, and source revision remain unchanged by construction.

There is no temporal field on a parentage edge, so this witness makes no
calendar or day-boundary assumption. Revalidation concerns the current edge
set and the store revision at a quiescent P12-B boundary. The witness itself
does not synchronize reads or prove that boundary.

## Required evidence

- The selected-profile composition exposes the required section at
  cardinality/revision `0/0`, with stable provider and installed-owner
  identity across reads. Reflection may be used by the separate test assembly
  to confirm the internal runtime owner; do not add a production test seam.
- Through normal `SimulationRuntime.TryAddParentage` and
  `TryRemoveParentage` calls, a successful edge add/removal increments the
  local revision once. A duplicate, self-edge, cycle, invalid endpoint, or
  missing-edge rejection leaves count and revision unchanged.
- After add/remove/re-add activity, the census reports the live direct-edge
  cardinality and a revision that reflects every successful content commit,
  including a later different edge that replaces a removed edge at the same
  final cardinality.
- Compose a runtime from a populated source `PersonStore` and
  `GenealogyStore`, including a prior add/remove sequence. Verify the
  provider witnesses the installed clone, the clone's cardinality and
  replay-local revision, deterministic records, and unchanged source owner
  records/revision.
- If overflow cannot be reached without unsafe reflection or an exposed test
  setter, verify the preflight ordering by code review and leave direct
  overflow injection untested.
- Run focused genealogy and composition tests, ALL EditMode, the complete
  official `Smoke` filter, and `git diff --check` before implementation
  candidate review.

## Explicit exclusions and dependencies

This adds no genealogy behavior, person-membership policy, family semantics,
population/residence/materialization logic, exports, staged hydration,
mutation-epoch notification, owner-thread/quiescence enforcement, capture
eligibility, or restore. `SimulationRuntime`'s existing higher-level wrappers
remain responsible for normal Person endpoint semantics; the owner census
does not duplicate that validation. P12-D remains dependency-gated on the
accepted P12-C roots, P12-B remains incomplete, and P12-A remains
`WAIT_DEPENDENCY` pending the complete owner inventory, export/hydration, and
its separate implementation authorization.
