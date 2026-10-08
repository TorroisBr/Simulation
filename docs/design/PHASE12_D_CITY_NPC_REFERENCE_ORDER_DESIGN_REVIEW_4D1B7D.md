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
- Relevant P12-E current-base correction handoff: `68ad4697117c72ba42718de0db8ab7307436c007`. P12 canonical State at the reviewed base marks its independent review pending; this review does not promote or validate that P12-E candidate.

Remote preflight confirmed the City/NPC candidate branch still points at the reviewed commit and P12 canonical still points at the stated base. The separate review branch was created from the exact candidate commit. This record is documentation-only.

## Findings

### City/NPC relation assembly: PASS

The correction resolves the reference cycle without a second owner construction or gameplay mutation:

1. It captures the City root and merged D/E/F NPC projection against the same already-prevalidated P12-B boundary token, transient capture stamp, and exact owner-component revision vector. Split/mismatched evidence is rejected, and the capture envelope is explicitly not serialized as owner state.
2. Each final City is constructed once with its captured `ImportantNpcRevision` and one private backing list. The exact ordered NPC IDs remain pending in the unpublished graph builder; they are not sorted or reconstructed from NPC roster order.
3. Each final NPC is constructed once after all Cities and legacy locations exist. Its current-City and current-location references resolve directly to those staged objects; gameplay constructors and mutation APIs are expressly excluded.
4. After all NPC instances exist, each City's private list is filled once from its captured ordered IDs. The contract rejects duplicate, dangling, and cross-owner references and validates reciprocal City membership/current-City/current-location links in both directions without changing the captured membership revision.
5. Any invalid graph is discarded before package handoff; no partially linked graph is published.

This matches the pertinent private-backing-list and one-time relation-assembly seam in the referenced City proposal. It also addresses that proposal's instruction to amend and independently review D design §4 before implementation.

The token/stamp/vector rules are capture evidence only: the candidate does not make them serialized continuation state or invent an aggregate City/NPC revision.

### Current Person owner: compatible

At the reviewed P12 canonical base, the promoted Person snapshot owns Person rows, their local revision, optional materialized-NPC IDs, and the derived NPC-to-Person lookup. Its exact staged factory rebuilds that index and rejects duplicate bindings. The candidate stages Persons only after NPC identities exist, then establishes the reciprocal materialization relation. This keeps the relation's Person authority in `PersonStore`, avoids copying Person ownership into `NpcRuntime`, and does not conflict with the current Person owner slice.

### P12-D/P12-E/accepted Daily-v1 profile boundary: NEEDS CHANGES

The current contracts distinguish two different site-related authorities. The candidate does not preserve that distinction precisely enough:

1. **Legacy `ExplorableSiteStore` in P12-D.** At the reviewed canonical base, the accepted Daily-v1 owner census section is schema-v1 `p12d.explorable-sites`, produced by `ExplorableSiteCensusProvider` for the exact installed `ExplorableSiteStore`. Runtime admission registers it as `ExplicitlyEmpty`. The current P12 State directs D to preserve that exact empty section and reject populated site state for Daily-v1. The candidate instead assigns generic site values to the selected D projection, restores each site in §4 step 7, and requires zero-and-multiple-site round trips in the validation list, without a Daily-v1 rejection rule.
2. **P10 `LocalTopologyStore` in P12-E.** This is a separate authority from the legacy `ExplorableSiteStore`. The accepted Daily-v1 profile has no LocalTopology owner. The P12-E correction handoff at `68ad469` requires a typed `NOT_COMPOSED`/provider-absence witness bound to the exact profile and provider inventory; it explicitly forbids instantiating a store or treating a composed-empty owner section as equivalent. P12-D §1 currently lists P10 LocalTopology among excluded authorities and says excluded authorities must be empty or reject populated state, which does not express the P12-E no-owner/typed-absence contract and could be read as requiring an empty LocalTopology owner.

These distinctions do not invalidate the City/NPC reference-order mechanics, but they block acceptance of the full current-base design against the already accepted Daily-v1 boundary.

### Bounded compatible resolution

Revise the D design to state all three profile states separately:

- For selected Daily-v1, retain `p12d.explorable-sites` with its exact P12-B owner/token binding and required-empty role; reject any populated `ExplorableSiteStore` site state. Do not run the generic multi-site site snapshot/hydration behavior in this profile.
- For Daily-v1 P10 LocalTopology, consume the separate P12-E typed `NOT_COMPOSED`/provider-absence witness. Do not require an empty LocalTopology section and do not instantiate `LocalTopologyStore`.
- Keep generic multi-site D support, if desired, deferred to a future profile that explicitly admits that owner and its dependencies. Keep the P8-C City/Site anchor section separate and excluded; do not add P10-A LocalTopology to Daily-v1 or weaken its separate proving profile.

P12-E still owns no separate City value section in current Daily-v1. City roots remain a single D-owned concrete owner captured once under the shared token/vector. The proposed resolution changes profile-boundary wording and readiness tests only; it does not change the City's accepted facts or the reviewed City/NPC object-construction order.

## Readiness and validation boundary

The candidate's City/NPC ordering correction is technically coherent, but the full current-base D contract does not pass review until the Daily-v1 `ExplorableSiteStore` and P12-E `LocalTopologyStore` states are reconciled as above. Therefore this review does **not** designate the City owner, the City/NPC shared stage, or any other owner as `READY_FOR_IMPLEMENTATION`.

No code or tests were run. The candidate is documentation-only; this record makes no implementation or validation claim.
