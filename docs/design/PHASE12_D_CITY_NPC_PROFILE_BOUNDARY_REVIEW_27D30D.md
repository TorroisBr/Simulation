# P12-D City/NPC Profile-Boundary Design Review

**Review ID:** P12D-CITY-NPC-PROFILE-BOUNDARY-REVIEW-27D30D-R1  
**Outcome:** `NEEDS_CHANGES`  
**Review date:** 2026-10-08  
**Scope:** exact-content review of the corrected P12-D design against current P12 canonical and architecture. Design-only; this does not authorize implementation, declare a City/NPC owner ready, make P12-A ready, or close Phase 12.

## Exact revisions reviewed

- Candidate branch: `codex/phase12/P12DCityProfileBoundaryCorrection`
- Candidate commit: `27d30d02f4a9ae4e873c395b6f34d2e804e953d8`
- Candidate tree: `f6aa2df3ee5a64e40fdbe29d291b3f2ffd711ce8`
- Candidate design blob: `f42ba665f1c777df1098681cbb24f2c926bf11b2`
- P12 canonical base: `dbba3e9a227f66da0381e3e042e826518d63c240`
- Architecture canonical: `47eff220c7ce00f6e7c759bdc2b76780bb46f628`
- The candidate is based on the P12 base through the City/NPC ordering update `e6f20a54d1b7665a8cd66cb0ce4ba9b58bf22c02`; the final profile-boundary correction is `27d30d0`.
- The cumulative base-to-candidate diff changes only `docs/design/PHASE12_D_TECHNICAL_DESIGN.md`.
- Referenced City snapshot proposal: `837bcdafa736cc15ef7ecba0e6c20bddfaa1ff7f`.
- Current P12 State records the P12-E typed LocalTopology correction review at `cdea870ef9eeb27603d021bac97ec56a81856617`, on review branch tip `d40e222affe67690287887195dfdc425798cd7e1`.

Remote preflight confirmed P12 canonical remains at the stated base and the candidate branch remains at the reviewed candidate. The review branch was already present at the exact candidate tip; this record adds only a review document.

## Findings

### City/NPC staged reference order: PASS

The revised sequence remains sound:

- Capture binds the City and merged D/E/F NPC projections to one validated P12-B boundary token, the same transient capture stamp, and the exact relevant owner-component revision vector. These are transient capture evidence and are not serialized.
- Each final City is constructed once with its exact captured membership revision and private backing list; captured ordered NPC IDs remain pending and are not rebuilt from NPC roster order.
- Each NPC is constructed once after staged Cities and legacy locations exist, with direct references to the exact staged City/location. Gameplay presence mutation paths are excluded.
- After NPC construction, the City's private membership list is filled once in captured order. Duplicate, dangling, and cross-owner IDs fail; reciprocity is checked in both directions without changing the captured City revision.
- Any failure discards the unpublished graph before handoff.
- Person rows and materialization bindings are restored after NPC identities exist, consistent with the current PersonStore-owned relation and rebuilt derived index.

This implements the ordering correction requested by the referenced City proposal. It does not add an aggregate City/NPC revision or claim whole-D publication.

### Daily-v1 Site and P12-E LocalTopology boundaries: materially corrected

The candidate now states the key contracts accurately:

- The accepted Daily-v1 `p12d.explorable-sites` section is schema-v1, bound to the exact installed `ExplorableSiteStore` and the P12-B completed-boundary token, and remains `ExplicitlyEmpty`. Populated site rows are rejected and are not exported or hydrated.
- The P8-C City/Site anchor authority remains a separate excluded authority.
- P10 `LocalTopologyStore` is not composed by Daily-v1. The candidate distinguishes P12-E's typed `NOT_COMPOSED`/provider-absence witness from a composed-empty owner; it says not to require or instantiate an empty LocalTopology owner.
- Generic multi-site owner behavior is explicitly deferred to a future profile that admits it in the current-profile crosswalk, purpose/boundary, owner table, staging order, and validation cases.

This matches the accepted profile split and the P12-E correction recorded in canonical State. It does not weaken P10-A or add LocalTopology to Daily-v1.

### Remaining scope ambiguity: NEEDS CHANGES

Two generic site-owner clauses remain unqualified within the current Daily-v1 design:

1. In §3, the `Site` semantic-export bullet still lists site runtime IDs, definition, store order, and legacy location as facts the export should contain, with no “future profile only” condition.
2. In §5, the relation rules still state unconditionally that every site has a unique runtime ID and compatible definition/location, and that multiple sites may share a location.

Earlier sections clearly prohibit site rows for Daily-v1 and defer generic site-row behavior. These later clauses can be read as reintroducing site rows into the selected profile's export and graph contract. The current State also says site rows are not admitted for Daily-v1. To make the contract internally exact, qualify both clauses as applying only when a future profile explicitly admits populated `ExplorableSiteStore` state. Daily-v1 must continue to export/hydrate no site rows and must preserve only the exact empty witness.

This is a documentation consistency fix. It does not require changing the City/NPC staging order, Daily-v1 profile, P10-A, or the P12-E witness.

## Readiness and validation boundary

The candidate's main profile correction and City/NPC ordering pass, but the unqualified §3/§5 clauses leave the full design ambiguous. This review therefore does **not** mark the City owner or City/NPC shared stage `READY_FOR_IMPLEMENTATION`. Current canonical State still classifies the City/population and D/E/F NPC adapters as serialized shared-owner work; no readiness is inferred from this design review.

No code or tests were run. No source or unrelated files were changed.
