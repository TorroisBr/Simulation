# P12-B P8-D exact-zero census registration design

**Status:** bounded P12-B integration design, based on current P12 canonical
`e5405cf897c30224f86ce605a9efe6777f93749a`.

## Finding and contract

The selected `UnityBootstrap-Daily-v1` P12 profile excludes P8-D route
Knowledge and route-plan facts and requires their composed owners to be
explicitly empty. The owner-specific schema-v1 providers already exist:

| Section | Role in this profile | Installed owner | Required witness |
|---|---|---|---|
| `p8d.spatial-route-observations` | `ExplicitlyEmpty` | Runtime-cloned `SpatialRouteKnowledgeStore` | `ObservationCount=0`, `Revision=0`, exact installed owner identity |
| `p8d.person-route-plan-history` | `ExplicitlyEmpty` | Runtime-cloned `PersonRoutePlanStore` | `PlanCount=0`, `Revision=0`, exact installed owner identity; `History.Count=0` cross-check |

The provider classes are currently passive only. `InitializeNpcRosterCensusProtocol`
does not register either section, so neither is part of the selected profile's
sealed 258-section owner inventory. The exact-zero direct-provider test does
not close that registration gap.

## Bounded implementation

Reuse `SpatialRouteObservationCensusProvider` and
`PersonRoutePlanHistoryCensusProvider`; do not add another owner, provider
schema, operation ID, route feature, or serialization format. During the
selected-profile census setup, register both providers against the exact
runtime-installed cloned owners with `OwnerSectionRole.ExplicitlyEmpty`.
Reject protocol setup unless each first witness has the expected section ID,
schema v1, exact owner identity, zero cardinality, and revision zero. For plan
history, check both `PlanCount` and the retained `History` view at setup.
Include both sections before the inventory/provider seals, increasing the
selected Daily-v1 partial inventory from 258 to 260.

Extend the normal selected-profile bootstrap composition test to prove that
both contracts are registered with the explicit-empty role, the registered
providers read the exact installed owners, the witnesses stay stable across
repeated reads, and the current partial protocol assessment succeeds. Preserve
the existing provider-level tests for observation replay, plan-history
cardinality, and P8-E status revision behavior.

The current selected Daily-v1 source has no production caller for
`TryRecordSpatialObservations` or `TryAcceptSpatialRoutePlan`; the accepted
profile does not compose an external WorldCommand queue and excludes populated
P8-D state. This slice therefore adds no P8-D mutation operation or shared
epoch notification. It does not convert public API visibility into a supported
Daily-v1 write path. If a later profile admits route Knowledge or plans, that
profile must revise its empty-section contract and separately cover successful
P8-D commits in the operation/epoch map.

## Exclusions and limits

- Do not change P8-D or P8-E mutation ordering, route semantics, or runtime
  behavior; do not add route planning or gameplay integration.
- Do not add mutation-epoch wiring, owner-thread/quiescence claims, a completed
  boundary token, capture eligibility, P12-A export/hydration, or a P12-B
  completion claim.
- Do not treat 260 as a complete effective-owner inventory. It is the tested
  count for this selected-profile partial protocol only.
- P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains
  blocked; P12-F Expedition remains deferred to its documented dependencies.

## Validation and review boundary

No Unity test is needed for this design record. The implementation must run
the focused bootstrap-composition coverage, ALL EditMode, official Smoke, and
`git diff --check` on the exact code tree. The exact-zero provider contract
already has independent P8-D review and validation; implementation review must
also verify registration against the installed runtime clone and that no
additional operation or mutation semantics were introduced.
