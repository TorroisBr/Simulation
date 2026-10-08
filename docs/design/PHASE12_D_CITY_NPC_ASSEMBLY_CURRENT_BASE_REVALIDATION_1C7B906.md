# P12-D City/NPC assembly design revalidation

**Review ID:** P12D-CITY-NPC-ASSEMBLY-DESIGN-REVALIDATION-1C7B906  
**Verdict:** PASS — the existing relation-order City/NPC assembly design is technically ready to enter bounded implementation, within the scope stated below. This is a design revalidation only. It validates no new code and does not complete P12-D.

## Exact bases

- P12 canonical: `1c7b906c172d9e47020996888db64bd2516b451a`
- Architecture canonical: `47eff220c7ce00f6e7c759bdc2b76780bb46f628`
- Repository: `TorroisBr/Simulation`

The review is anchored to those exact canonical refs. The earlier D technical-design review is supporting historical evidence, not a substitute for this current-base verdict.

## Evidence reviewed

- `docs/SIMULATION_ARCHITECTURE.md` at architecture canonical `47eff220c7ce00f6e7c759bdc2b76780bb46f628`.
- `docs/phases/PHASE12_BRIEF.md` and `docs/PHASE12_STATE.md` at P12 canonical `1c7b906c172d9e47020996888db64bd2516b451a`.
- `docs/design/PHASE12_D_TECHNICAL_DESIGN.md`, especially the City/NPC owner crosswalk, staged hydration order, and single-owner constraints.
- `docs/design/PHASE12_D_TECHNICAL_DESIGN_REVIEW_0735103.md`, as earlier design-review context only.
- `docs/design/PHASE12_D_CITY_NPC_RECEIPT_ZERO_WITNESS_IMPLEMENTATION_REVIEW_R3.md` at the current P12 canonical, including its exact code/test evidence.
- The State’s linked receipt exact-zero design and design-review records.

## Basis for PASS

The accepted D design already specifies the assembly order and ownership constraints: construct each City once with its exact ordered `ImportantNpcs` IDs retained as pending references; capture and stage each NPC once from the matching D/F projections; then resolve City membership in the captured order. Hydration sets the direct references through exact-value staging, not gameplay mutators. Before handoff, it rejects duplicate, dangling, or cross-owner links and checks bidirectional City/NPC and current-location reciprocity. It preserves `ImportantNpcRevision` and does not rebuild City membership from NPC roster order.

The receipt-owner evidence gap is now closed for the two excluded P18 receipt owners. The current State records selected Daily-v1 exact-zero identity, cardinality, and revision witnesses for both `LocalObservation` and `MerchantTradeState`, bound per applicable NPC. The R3 exact-tip implementation review identifies code/test commit `4947ec926b3a48427e9c07de5d722d45014cf1bf`, code/test tree `dd0cda707693237a56f5664b2eabc8aa6821b100`, and Assets tree `b239758a62c9c670f7400af2567515308e138a4f`. It reports validated stale-token rejection after receipt writes and fail-closed behavior for roster additions with populated receipt owners. This supplies the exact-empty witness required by the existing assembly design; it does not broaden that design.

## Ready scope and implementation obligations

Ready scope is limited to implementing the already designed relation-order City/NPC assembly path within accepted P12-D semantics. In particular, implementation must validate:

1. Each City and NPC is constructed exactly once; captured ordered membership and `ImportantNpcRevision` are preserved.
2. Hydration invokes no gameplay presence or membership mutations and causes no gameplay side effects.
3. Every City/NPC/location relation is reciprocal and resolves to the exact staged objects; duplicate, dangling, cross-owner, or nonreciprocal links reject the unpublished stage.
4. Disjoint D/F NPC projections share the exact P12-B completed-boundary token, capture stamp, and relevant component-revision vector before they are merged for the one NPC reconstruction.
5. Either receipt owner being populated, missing, mismatched, or otherwise not exactly empty causes Daily-v1 admission/capture validation to fail closed.

These obligations are validation criteria for implementation, not claims that the tests have already been written or passed. Exact City/NPC owner exports and private factories, broader merged-graph validation, runtime integration, and remaining accepted D owner slices still require their own scoped work and evidence.

## Boundaries and status

This review adds no scope. It introduces no P18 receipt export or replay, no new P12-B mutation or capture semantics, no P12-A integration or authorization, no P13 fork/replay semantics, and no whole-profile save/load or continuation-parity claim. P12-D remains IN PROGRESS; City/NPC shared assembly, merged-graph validation, runtime integration, and remaining D owner coverage remain open. Phase 12 remains open.
