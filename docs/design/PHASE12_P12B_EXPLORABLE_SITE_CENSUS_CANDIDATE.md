# P12-B ExplorableSite Census Candidate

**Status:** Owner-local passive census witness implemented and targeted
validation passed. It is not canonical delivery, complete P12-B evidence, or
P12-A readiness.

**Branch:** `codex/phase12/P12BExplorableSiteCensusWitness`.

**Code tip:** `53fbbc08ff4bb5b7b111a1aab9b994a7c221863f`.

**Base:** P12 canonical `d0c2733994aaf51e417b7c9f49f2b3489c4c49c3`.

## Delivered owner-local surface

`ExplorableSiteStore` now exposes exact `Count` and a monotone local
`Revision`. A successful `Add` advances the revision once after publishing the
site to both owner collections. Guarded, null, blank-ID, duplicate-ID, and
revision-saturated additions do not change the owner or its witness. At
`long.MaxValue`, a valid new site is rejected rather than allowing the local
revision to wrap.

`ExplorableSiteCensusProvider` reports schema-v1 section
`p12d.explorable-sites`, exact owner instance identity, site count, and the
owner-local revision. The selected-profile test obtains the actual published
owner from `simulation.Bootstrap.ExplorableSites`, then constructs the provider
directly. The provider is not yet published as a fixed property of
`SimulationBootstrapComposition`; that shared composition handoff remains a
separate integration task.

The store's `Add` is only the final step in authored site installation. The
bootstrap flow also allocates the site identity, registers it, and publishes
its legacy spatial location. This site witness does not cover those owners or
prove an outer transaction/invalidation boundary across them.

## Validation

| Gate | Result | XML |
|---|---:|---|
| `ExplorableSiteCensusTests` | 5/5 | `EditMode-20260930-132015-32917397b3834e88a9e610c88d68cbb7.xml` |
| `ExplorableSiteFoundationTests` | 17/17 | `EditMode-20260930-132101-52e9ecc0a0a34a0ebd3d3b9fd4a5aa85.xml` |
| `AdventureExpeditionAutonomyTests` | 46/46 | `EditMode-20260930-132118-a0cef84e06c94448be35a72b74fbede1.xml` |
| `git diff --check` | PASS | clean before commit |

The new tests cover the selected authored profile's exact-zero published
store, stable owner identity, successful addition, duplicate/null rejection,
faulted mutation-guard rejection, and overflow fail-closed behavior. Overflow
is reached in the test by reflecting the private owner revision; no
test-only production setter was added.

## Limits retained

This is passive and unsynchronized evidence. It does not register the provider
in the P12-B coordinator or complete the live profile inventory. It does not
cover the allocator, identity registry, legacy spatial network, exploration
progress, shared mutation epoch, owner-thread/quiescence, capture eligibility,
export, or hydration. Provider publication through the fixed bootstrap
composition still needs an integration/review step coordinated with the
Genealogy composition change.

P12-B remains incomplete. P12-A remains `WAIT_DEPENDENCY`, and P12-D remains
blocked on P12-B and P12-C.
