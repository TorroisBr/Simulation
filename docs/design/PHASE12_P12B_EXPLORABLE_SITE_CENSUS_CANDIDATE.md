# P12-B ExplorableSite Census Candidate

**Status:** Owner-local passive census witness and fixed bootstrap publication
are implemented and validated on the current integration tip. Exact-tip code
review passed; exact-tip documentation review is pending. This is not canonical
delivery, complete P12-B evidence, or P12-A readiness.

**Branch:** `codex/phase12/P12BExplorableSiteCensusIntegration`.

**Code tip:** `8dcfc031cb983003a04fc20d037c7b5c18e81402`.

**Code tree:** `fccf4cc59d32bafdb5a0d8aa1bbd40fa955a4c8f`.

**Base:** P12 canonical `676196bcd807603deb9d01bd2855342a7d47a01e`.

## Delivered owner-local surface

`ExplorableSiteStore` now exposes exact `Count` and a monotone local
`Revision`. A successful `Add` advances the revision once after publishing the
site to both owner collections. Guarded, null, blank-ID, duplicate-ID, and
revision-saturated additions do not change the owner or its witness. At
`long.MaxValue`, a valid new site is rejected rather than allowing the local
revision to wrap.

`ExplorableSiteCensusProvider` reports schema-v1 section
`p12d.explorable-sites`, exact owner instance identity, site count, and the
owner-local revision. `SimulationBootstrapComposition` publishes one fixed
provider built from its exact authored `ExplorableSites` owner. The selected
profile test consumes that published provider and confirms stable provider and
owner identity across reads.

The store's `Add` is only the final step in authored site installation. The
bootstrap flow also allocates the site identity, registers it, and publishes
its legacy spatial location. This site witness does not cover those owners or
prove an outer transaction/invalidation boundary across them.

## Validation

| Gate | Result | XML |
|---|---:|---|
| `ExplorableSiteCensusTests` at integration tip | 5/5 | `Library/ValidationResults/P12BExplorableSiteFocus/EditMode-20260930-143655-888cf291c947415fbdf18007aa44d97d.xml` |
| ALL EditMode at integration tip | 1995/1995 | `Library/ValidationResults/P12BExplorableSiteAll/EditMode-20260930-143714-06577168c40c45f5a9f21669df1d2362.xml` |
| Complete official `Smoke` at integration tip | 5/5 | `Library/ValidationResults/P12BExplorableSiteSmoke/EditMode-20260930-143800-a6a4dbc9bf5e42309ed9dd9fe1c036b8.xml` |
| Exact-tip code review | PASS | `8dcfc031cb983003a04fc20d037c7b5c18e81402`, against `676196bcd807603deb9d01bd2855342a7d47a01e` |
| `git diff --check` | PASS | clean at integration tip |

The integration tip's full EditMode run includes the affected
`ExplorableSiteFoundationTests` and `AdventureExpeditionAutonomyTests` suites.

The new tests cover the selected authored profile's exact-zero published
store, stable owner identity, successful addition, duplicate/null rejection,
faulted mutation-guard rejection, and overflow fail-closed behavior. Overflow
is reached in the test by reflecting the private owner revision; no
test-only production setter was added. The integrated selected-profile case
reads the provider fixed on `SimulationBootstrapComposition` rather than
constructing an alternate provider in the test.

## Limits retained

This is passive and unsynchronized evidence. It does not register the provider
in the P12-B coordinator or complete the live profile inventory. It does not
cover the allocator, identity registry, legacy spatial network, exploration
progress, shared mutation epoch, owner-thread/quiescence, capture eligibility,
export, or hydration. Its composition change shares the bootstrap surface with
the promoted Genealogy witness; exact-tip review covered the combined
composition.

P12-B remains incomplete. P12-A remains `WAIT_DEPENDENCY`, and P12-D remains
blocked on P12-B and P12-C.
