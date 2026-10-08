# P12-D City/NPC Reference-Order Design Review

**Review ID:** P12D-CITY-NPC-REFERENCE-ORDER-DESIGN-4D1B7D-R1  
**Outcome:** `NEEDS_CHANGES`  
**Review date:** 2026-10-08  
**Review scope:** exact-content review of the current-base P12-D City/NPC staging-order correction and its referenced City snapshot proposal. This is a design review only; it is not implementation authorization, a code review, City readiness, P12-A readiness, or Phase 12 closure.

## Exact revisions reviewed

- Candidate branch: `codex/phase12/P12DCityNpcReferenceOrderDesign`
- Candidate commit: `4d1b7d32ac7f608400a953e3da7a76a03762cf46`
- Candidate tree: `63621e18141643aada0a2330a47cbedbe404a2d3`
- Candidate's only changed path: `docs/design/PHASE12_D_TECHNICAL_DESIGN.md`
- Changed design blob: `aae227c5f043bcc315dc2f51b96e01ed60fb8344`
- P12 canonical base and candidate parent: `63cb5e7156ce703f73f78b8837a13889d7f92492`
- Architecture canonical reviewed by the design: `47eff220c7ce00f6e7c759bdc2b76780bb46f628`
- Referenced City proposal: `837bcdafa736cc15ef7ecba0e6c20bddfaa1ff7f`, tree `cbb767d0e55340803b2add8276ac500c14b16e76`, document blob `7c01278cab29d7cd477f19aa3bf69de49d9ac640`.

Remote preflight confirmed the candidate branch still points at the reviewed commit and P12 canonical still points at the stated base. The review branch was created from the exact candidate commit; this review record is its only new file.

## Findings

### City/NPC relation assembly: PASS

The correction resolves the reference cycle without a second owner construction or gameplay mutation:

1. It captures the City root and merged D/E/F NPC projection against the same already-prevalidated P12-B boundary token, transient capture stamp, and exact owner-component revision vector. Split/mismatched evidence is rejected, and the capture envelope is explicitly not serialized as owner state.
2. Each final City is constructed once with its captured `ImportantNpcRevision` and one private backing list. The exact ordered NPC IDs remain pending in the unpublished graph builder; they are not sorted or reconstructed from NPC roster order.
3. Each final NPC is constructed once after all Cities and legacy locations exist. Its current-City and current-location references resolve directly to those staged objects; the described gameplay constructors and mutation APIs are expressly excluded.
4. After all NPC instances exist, each City's private list is filled once from its captured ordered IDs. The contract rejects duplicate, dangling, and cross-owner references and validates reciprocal City membership/current-City/current-location links in both directions without changing the captured membership revision.
5. Any invalid graph is discarded before package handoff; no partially linked graph is published.

This matches the pertinent private-backing-list and one-time relation-assembly seam in the referenced City proposal. It also addresses that proposal's instruction to amend and independently review D design §4 before implementation.

The token/stamp/vector rules are capture evidence only: the candidate does not make them serialized continuation state or invent an aggregate City/NPC revision.

### Current Person owner: compatible

At the reviewed P12 canonical base, the promoted Person snapshot owns Person rows, their local revision, optional materialized-NPC IDs, and the derived NPC-to-Person lookup. Its exact staged factory rebuilds that index and rejects duplicate bindings. The candidate stages Persons only after NPC identities exist, then establishes the reciprocal materialization relation. This keeps the relation's Person authority in `PersonStore`, avoids copying Person ownership into `NpcRuntime`, and does not conflict with the current Person owner slice.

### Current Daily-v1 Site boundary: NEEDS CHANGES

The candidate's inherited design text does not match the current canonical Daily-v1 admission contract:

- Current P12 State requires the exact P12-B section to remain empty for Daily-v1 and directs D to preserve that empty section and reject populated site state.
- The exact owner is `ExplorableSiteStore`, witnessed by schema-v1 section `p12d.explorable-sites` from `ExplorableSiteCensusProvider`; current runtime admission registers it with role `ExplicitlyEmpty`.
- The reviewed D design instead assigns generic `ExplorableSiteStore` values to the selected D projection, says to restore each site in §4 step 7, and requires zero-and-multiple-site round trips in the focused validation list. It does not state that the selected Daily-v1 token's exact site section must stay empty and that populated site state is rejected.

Please amend the current-base design to state the selected Daily-v1 boundary explicitly: preserve the exact required-empty `p12d.explorable-sites` section and fail closed on populated site state. Any multi-site snapshot/hydration behavior must be identified as a future profile scope that explicitly admits the owner. Keep the separate P8-C City/Site anchor section excluded, and do not add P10-A LocalTopology to Daily-v1.

This is a bounded profile-contract correction; it does not call for changing P10-A or the City/NPC graph-assembly solution.

## Readiness and validation boundary

The candidate's City/NPC ordering correction is technically coherent, but the full current-base D contract does not pass review until the Daily-v1 required-empty Site boundary is reconciled. Therefore this review does **not** designate the City owner, the City/NPC shared stage, or any other owner as `READY_FOR_IMPLEMENTATION`.

No code or tests were run. The candidate is documentation-only; this record makes no implementation or validation claim.
