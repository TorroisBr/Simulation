# P12-B Legacy SpatialNetwork Census Design Review

## Verdict

**PASS.** The bounded technical design is consistent with accepted P12-B
passive-census and P12-D factual-owner scope. No new human checkpoint
acceptance is required. The current cumulative candidate's canonical
promotion remains a separate explicit gate.

## Exact evidence reviewed

- Canonical base: `codex/phase12/canonical` at
  `676196bcd807603deb9d01bd2855342a7d47a01e`.
- Design document exact tip: `11b4a27` on
  `codex/phase12/P12BCensusOwnersCumulativeIntegration`.
- Cumulative composition candidate code: `44fc3ab94c9666f656149f346fb2cc553d3cb689`.
- Cumulative composition code tree: `b0be75370d32679d0745ed15d29ce359dada0bb6`.
- Independent reviewer: `p12_quiescence_audit`; read-only design review.

## Findings

The design correctly defines two schema-v1 sections for the exact installed
legacy `SpatialNetworkRuntime`, sharing a monotone local revision. The
selected authored profile's positive day-zero cardinalities are 2 Locations
and 2 Routes at revision 4. The design keeps these distinct from the
`RuntimeIdentityRegistry` indexes and all P8-owned spatial sections.

Local revision saturation is preflighted before identity registration;
ordinary failed registrations preserve network counts and revision. Direct
public `RuntimeIdentityRegistry.RegisterLocation/RegisterRoute` writes remain
a separate C-root invalidation obligation and are not covered by the network
revision. The P12-B operation boundary must span a full network registration
and notify the epoch once after its commit. Exception rollback, shared-epoch
wiring, and quiescence are expressly excluded.

The collection-view plan retains supported consumer behavior. It includes
gameplay readers plus `WorldObserverReadModel` and
`Diagnostics/WorldStateSnapshot`, whose copy-and-sort behavior must remain
unchanged and receive focused regression coverage.

## Limits

No implementation or tests were run for this design review. The design does
not close P12-B, make P12-A ready, provide export/hydration, or authorize
canonical promotion. Implementation is held until the current cumulative
composition hotspot is promoted or intentionally re-integrated on the
current canonical base.
