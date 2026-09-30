# P12-B cumulative owner census integration candidate

**Status:** `PROMOTED` to `codex/phase12/canonical` at
`81435f9f17816a3fb35b59d8ab374ed1cd719444`. Code tip `44fc3ab` and code tree
`b0be753` passed exact-tip independent code/design/evidence review and
required validation. The promoted State includes the reviewed SpatialNetwork
owner-census design at `11b4a27`; the separate design review record is linked
from `docs/PHASE12_STATE.md`. This partial census foundation does not complete
P12-B, make P12-A ready, or deliver any export/hydration.

**Canonical base:** `codex/phase12/canonical` at
`676196bcd807603deb9d01bd2855342a7d47a01e`.

**Integration branch:** `codex/phase12/P12BCensusOwnersCumulativeIntegration`.

**Code tip:** `44fc3ab94c9666f656149f346fb2cc553d3cb689`.

**Code tree:** `b0be75370d32679d0745ed15d29ce359dada0bb6`.

## Bounded delivery

The integration retains three previously developed owner-local census slices
and publishes them through the existing fixed bootstrap composition:

- **PersonStore:** two structural sections for Person membership and
  Person-to-NpcRuntime materialization bindings, tied to the installed
  `Runtime.PersonStore` identity and its revision. It preserves the reviewed
  compensation headroom and rejects invalid binding changes without changing
  witness cardinality or revision.
- **ExplorableSiteStore:** one site-count section tied to the installed
  `ExplorableSites` object and its revision. The authored selected profile
  retains its exact-zero witness.
- **SettlementPopulationRuntime:** two sections per composed City, tied to
  that City's exact installed population owner: aggregate-owner cardinality
  (always one while composed) with the population revision, and retained
  operation-receipt count with its receipt-ledger revision. Providers use
  stable length-prefixed City runtime IDs and ordinal ordering. The selected
  profile exposes four providers for its two Cities.

The population profile test reads `simulation.Bootstrap.SettlementPopulationCensusProviders`
instead of constructing providers ad hoc. It also reads the still-published
Genealogy witness, confirming that the population composition addition
preserves the promoted Genealogy provider. The previously reviewed PersonStore
and ExplorableSite providers remain present in the same composition.

These are structural/current-owner witnesses, not per-day occurrence sections;
their counts make no temporal cardinality promise. Population reads remain
after the outer operation returns. The providers do not create a cross-owner
atomic snapshot.

## Reused work and exact-tip impact

- PersonStore implementation `1e8d940d6b8db41098cd99c7c5c1c9f7ed5c7db8`
  was independently reviewed PASS against base `676196b`; its final evidence
  branch tip is `3e0452b08d8666b845f3e0c018fa2fe15a8cd456`.
- ExplorableSite integration code `8dcfc031cb983003a04fc20d037c7b5c18e81402`
  was independently reviewed PASS against base `676196b`; its evidence branch
  tip is `eaa458cd61cfa0c65f3f4c1b05238b51e1d8299b`.
- SettlementPopulation owner code `0428d596259344c788c3738b184e2681861cea07`
  was retained from branch `codex/phase12/P12DSettlementPopulationCensusWitness`.
  Its earlier exact-tip review blocked *complete integration* because the
  fixed providers were not yet published through bootstrap; the code itself
  was preserved. The implementation design at
  `811c639ff2f3697eba674e87c44ff4a0463369f3` received an independent review
  against current canonical `676196b`, the accepted P12 decomposition, and the
  refreshed owner inventory: **PASS with revalidation required**. The design's
  original `d0c2733` base is retained as provenance; the current integration
  satisfies its outstanding Genealogy-composition handoff without replacing
  the promoted provider.

The reviewed source design and original owner-local submission are retained at
[`PHASE12_P12D_SETTLEMENT_POPULATION_CENSUS_DESIGN.md`](PHASE12_P12D_SETTLEMENT_POPULATION_CENSUS_DESIGN.md)
and
[`PHASE12_P12D_SETTLEMENT_POPULATION_CENSUS_CANDIDATE.md`](PHASE12_P12D_SETTLEMENT_POPULATION_CENSUS_CANDIDATE.md).
Their original `811c639`/`4bc5a8e` refs remain identified in those records;
the revalidation notes distinguish the earlier owner-local block from this
integrated candidate.

The current code tip is a cumulative integration of those retained changes on
the named canonical base. The PersonStore, ExplorableSite, population and
Genealogy/bootstrap interactions have been revalidated together below.

## Validation on code tip `44fc3ab`

All result XMLs are under the ignored local
`Library/ValidationResults/` directory in the integration worktree.

| Gate | Result | XML |
|---|---:|---|
| `SettlementPopulationCensusTests` | 6/6 | `P12CensusCombinedFocus/EditMode-20260930-145956-3980aaf8f72c42af812c47bc1bde0d02.xml` |
| `PersonStoreCensusTests` | 7/7 | `P12CensusCombinedFocus/EditMode-20260930-150044-db5b7c6c261447daa6f792129894d779.xml` |
| `ExplorableSiteCensusTests` | 5/5 | `P12CensusCombinedFocus/EditMode-20260930-150100-58e98777e7f845a08ca081d17ead715f.xml` |
| `GenealogyCensusTests` | 4/4 | `P12CensusCombinedFocus/EditMode-20260930-150126-ad0bf831a5d3476d8a5d313572417e09.xml` |
| `SimulationBootstrapCompositionTests` | 14/14 | `P12CensusCombinedFocus/EditMode-20260930-150139-72b18d2d15ab4638bdf0f2ab98074ea9.xml` |
| `AggregateDemographyFoundationTests` | 25/25 | `P12CensusCombinedFocus/EditMode-20260930-150159-dcc01d61eaaf44dca36d05c2cfe5e6b3.xml` |
| `NpcResidenceMigrationTests` | 33/33 | `P12CensusCombinedFocus/EditMode-20260930-150212-a5367468ce454fffa4e262390a6cb883.xml` |
| `PopulationCanonicalIntegrationTests` | 9/9 | `P12CensusCombinedFocus/EditMode-20260930-150227-3cc3ffd721bb4acc993357ff5ea57d5b.xml` |
| ALL EditMode | 2008/2008 | `P12CensusCombinedAll/EditMode-20260930-150328-3f6656bcbe92431c935fbdad931b34ab.xml` |
| Complete official `Smoke` filter | 5/5 | `P12CensusCombinedSmoke/EditMode-20260930-150552-db77b3b22baf4bdd9d8014ba34da24ad.xml` |
| `git diff --check` from canonical base | PASS | clean |

The tests and provider design cover live identity, section cardinality,
revision, stable ordering, ordinary write/replay/rejection behavior, and the
existing saturated rollback rule. They do not establish capture eligibility
or a synchronized snapshot.

## Limits and remaining gates

No P12-B coordinator registration, shared mutation-epoch wiring, runtime
owner-thread enforcement, quiescence proof, or capture token/eligibility is
added. No immutable owner export, staged hydration, or restoration behavior is
added. This is partial owner census evidence only. P12-B remains incomplete;
P12-A remains `WAIT_DEPENDENCY`; P12-D remains blocked on P12-B and P12-C.

The exact-tip code/design/evidence review and final documentation
revalidation have passed. The remaining gate is explicit human approval to
fast-forward `codex/phase12/canonical` from `676196b` to the final reviewed
tip of this integration branch, under `docs/EXECUTION_MODEL.md`.
