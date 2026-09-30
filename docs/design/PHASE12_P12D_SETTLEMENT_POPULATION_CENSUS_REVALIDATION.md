# P12-D Settlement Population census design revalidation

**Original design:** `codex/phase12/P12DSettlementPopulationCensusDesign` at
`811c639ff2f3697eba674e87c44ff4a0463369f3`, based on canonical `d0c2733`.

**Current canonical reviewed:** `676196bcd807603deb9d01bd2855342a7d47a01e`.

**Current integration code:**
`codex/phase12/P12BCensusOwnersCumulativeIntegration` at
`44fc3ab94c9666f656149f346fb2cc553d3cb689`, tree
`b0be75370d32679d0745ed15d29ce359dada0bb6`.

## Independent design review

The bounded design received independent exact-tip review against the current
canonical baseline, accepted P12 capability decomposition, and refreshed owner
inventory: **PASS with revalidation required**. The review found no change to
the accepted semantics: each section is bound to the City's exact installed
`SettlementPopulationRuntime`; aggregate cardinality and population revision
remain distinct from retained-receipt cardinality and receipt-local revision;
receipt count/revision are read together under the existing gate; and the
receipt revision distinguishes same-cardinality replacement without changing
rollback success semantics at saturation.

The original design base is preserved as provenance. The canonical changes
since `d0c2733` did not alter the SettlementPopulation owner/store code. They
did add the promoted Genealogy provider to `SimulationBootstrapComposition`,
which the original design explicitly required this slice to preserve. The
current integration adds the population providers after the existing
Genealogy/site composition, and the selected-profile test verifies both the
four population providers and the existing Genealogy witness through
`simulation.Bootstrap`.

No second user checkpoint acceptance is required: prerequisite P12-B through
P12-G capability scopes and implementation authority were already accepted.
This review settles technical ownership and revalidation only; it does not
promote code or broaden the accepted profile.

## Exact implementation revalidation

The earlier owner-local code candidate `0428d596259344c788c3738b184e2681861cea07`
was retained. Its earlier integration blocker was the absence of publication
through `SimulationBootstrapComposition`. Code tip `44fc3ab` closes that
specific gap and tests the normal bootstrap handoff. The exact cumulative
candidate review and validation table are in
[`PHASE12_P12B_CENSUS_OWNERS_CUMULATIVE_INTEGRATION_CANDIDATE.md`](PHASE12_P12B_CENSUS_OWNERS_CUMULATIVE_INTEGRATION_CANDIDATE.md).

The witnesses remain passive and unsynchronized. They do not register with
the P12-B coordinator, provide a shared mutation epoch, establish an owner
thread or quiescent boundary, grant capture eligibility, or export/hydrate
population state. P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`;
P12-D remains blocked on P12-B and P12-C.
