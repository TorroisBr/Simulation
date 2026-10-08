# P12-D City/NPC Profile-Boundary Design Review

**Outcome: PASS — exact-content technical-design review**

## Reviewed material

- P12-D design candidate: `9e0dcce1e0d348dba3853be67a2565a5f4826be1`
- Candidate tree: `dcdaa9aab39c3cbd665b7ecfef9c7a9b5032a8ef`
- Changed design blob: `a6f72aabc26057b46ec8896738c1006c016d880e`
- P12 canonical base: `dbba3e9a227f66da0381e3e042e826518d63c240`
- Architecture baseline: `47eff220c7ce00f6e7c759bdc2b76780bb46f628`
- Prior review with requested corrections: `codex/phase12/P12DCityProfileBoundaryCorrectionReview` at `ff71709a4f787bbdf8f2ad300108790b8a2a292f`
- Referenced City proposal: `837bcdafa736cc15ef7ecba0e6c20bddfaa1ff7f`
- Scope of candidate delta: documentation-only change to `docs/design/PHASE12_D_TECHNICAL_DESIGN.md`; no code or unrelated files changed.

I reviewed the complete corrected design and compared it with the accepted Daily-v1 owner inventory, current profile/runtime admission evidence, the referenced City proposal, current Person owner design, the P12-E review correction, and the prior review findings.

## Findings

1. **City/NPC staging order is sound.** The design constructs one staged City with its ordered pending NPC identities, stages each NPC with direct references to the staged City and canonical Location, then fills City membership once after all NPCs exist. It requires order preservation, reciprocal City/NPC membership and current-location consistency, and fail-closed discard of the unpublished graph. Snapshot token/stamp/vector remain transient and are not serialized. This is compatible with the current PersonStore owner, which materializes the NPC-to-Person relation/index rather than duplicating Person state in NPC rows.

2. **Daily-v1 site boundary is now explicit and consistent.** The accepted profile retains the `p12d.explorable-sites` section as required `ExplicitlyEmpty`, with the installed owner and P12-B boundary token. The corrected design now says Daily-v1 exports and restores no site rows and has no site identities; populated site state is rejected. The prior review's two ambiguities are fixed: the site export record in §3 and the site identity/cardinality language in §5 are explicitly future-profile-only. A future profile must deliberately admit the owner before those generic multi-site rules apply.

3. **P10 LocalTopology remains typed `NOT_COMPOSED`.** The P12-E witness for absent LocalTopology composition is preserved. The design does not instantiate an empty LocalTopology owner or conflate its absence with the composed-empty ExplorableSite section. This matches the accepted profile composition and current runtime admission contract.

4. **The composition boundary and exclusions remain bounded.** The design does not add P10 topology, geography, market/population gameplay, activities, or other owners to Daily-v1. The City/NPC graph is staged against the existing canonical Location and consumed P12-B token; failures do not publish a partial graph.

The corrected design resolves the exact profile mismatch raised in the prior review without weakening P8 single-owner-per-Location or changing the accepted P12 scope.

## Readiness boundary

This PASS reviews the design document only. It does **not** authorize or establish City/NPC implementation readiness. Current canonical State records City/NPC as serialized shared-owner work, and no field-complete current owner/source/mutation evidence was established by this review. An implementation slice must still satisfy the design's current-review and owner-evidence criteria, with shared-hotspot sequencing handled as required by the execution model.

No tests were run; the reviewed delta is documentation-only. No candidate code, canonical ref, ProjectSettings edit, or untracked metadata file was modified.
