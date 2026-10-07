# P12 Expedition mutation and shared-epoch design review

**Verdict:** PASS — source analysis and deferral classification  
**Reviewed candidate:** `6e5712c` on `codex/phase12/P12BExpeditionEpochCurrentDesign`  
**Design base:** P12 canonical `94551b08be8cc9347de35eae5051b8e578ea4c1e`  
**Review type:** read-only documentation/source review; no implementation or test claim.

## Findings

- The design correctly assigns Expedition and active commitments to P12-F and
  preserves the documented dependency on P12-C/D/E. It does not mark P12-F
  implementation ready or claim P12-B, P12-A, or P13 readiness.
- The current source exposes the passive Expedition census provider through
  bootstrap, but the provider/`p12f.expeditions` section is not registered in
  the current sealed continuation protocol. The design labels this as a
  proposed future owner boundary.
- The described reachable mutation path is supported by source: the public
  `TesteSimulacao.TryStartExpedition` facade can reach `AddAndFence`; a direct
  nested TravelParty start without its P12 request is rejected; successful
  compensation via `RemoveReserved` may still advance Expedition local
  revision. The candidate correctly treats each successful owner write as a
  future invalidation obligation, including on an enclosing false return.
- The writer inventory distinguishes selected-profile paths, exposed public
  methods, optional autonomy not composed by the selected bootstrap, and
  cross-owner work that remains outside the bounded owner analysis.
- The candidate is docs-only and `git diff --check` passes. No implementation,
  tests, P12 promotion, or readiness change is implied.

Review was performed against the exact candidate and canonical base above. The
full design is `PHASE12_P12B_EXPEDITION_MUTATION_EPOCH_DESIGN.md`.
