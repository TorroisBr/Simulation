# P12-D City/NPC Current-Base Crosswalk — Exact-Tip Review

**Result: PASS as current-source evidence; implementation readiness remains withheld.**

- Candidate: `3b1c8508b85995d468ccd955210e4fa8c89fab24`
- Crosswalk: `docs/design/PHASE12_D_CITY_NPC_CURRENT_BASE_FIELD_CROSSWALK.md`
- P12 canonical base: `ef0cafb5848cadcf0ac91a3e1af7ff3faaab1367`
- Architecture authority: `47eff220c7ce00f6e7c759bdc2b76780bb46f628`
- Prior profile/order correction: `9e0dcce1e0d348dba3853be67a2565a5f4826be1`; exact-content review `c65499d3baa52a134016fce5a0cefee8f0e09c4e`.
- Current P12-D City root promotion in the base: code `58c142329d034fbed18ece25ee017d0d69f4d62f`, reviewed candidate `22f20b1ab0cf9ba4fc36155c645554c98bff9c6e`, review `14afa99c802305d75761e589c47e7997276c80c0`.

## Review findings

The crosswalk accurately extends the current D/E/F contract with source-level NPC field ownership, exact owner/cardinality/revision witnesses, writers, and fail-closed conditions. It does not add a synthetic NPC revision or a second NPC projection. The division of D-owned roots/state, F-owned action/plan/travel/Knowledge payloads, Person-owned materialization/residence relations, and separate E authorities matches `docs/design/PHASE12_D_TECHNICAL_DESIGN.md` and the accepted P12-E boundary.

The writer and witness summaries match the source contracts in `NpcRuntime.cs`, `Person/PersonStore.cs`, the inventory/account/Knowledge owners, and the P18-D receipt owners. The crosswalk correctly preserves ordered City membership and its reciprocity check; distinguishes City and NPC projections; identifies the two P18 receipt ledgers as separate nested owners; and does not mistake their absence from the P12-B vector for proof that they are empty. The `9e0dcce` profile correction is accurately revalidated: Daily-v1's `p12d.explorable-sites` section stays required-empty, LocalTopology stays typed `NOT_COMPOSED`, and the City-before-NPC staging sequence remains compatible with the promoted City snapshot. No P10 scope, product semantics, or P12-A/P12-G/P13 claim was added.

## Receipt-owner classification

The two receipt owners are a **determinable evidence/capability blocker, not a genuine product or canonical-architecture gate**.

`NpcRuntime.cs` eagerly initializes `localKnowledgeObservationRuntime` and `merchantTradeStateRuntime` for each normally constructed NPC. Their local revisions and receipt counts are independently owned. The committed writers are P18-D paths (`NpcLocalKnowledgeObservationRuntime.TryCommit` and `NpcMerchantTradeStateRuntime.TryCommit`); the accepted Daily-v1 profile excludes P18 temporal continuation and does not compose those P18 daily consumers. The accepted boundary therefore supplies the semantic decision already: keep the owners out of the P12 continuation payload and fail closed unless exact empty state is proven.

Current P12 evidence has no owner-section witness for either embedded owner. This is a concrete missing witness, not a question about whether P18 receipts belong in Daily-v1. Before a field-complete NPC capture can be declared ready, the bounded follow-up must provide owner-issued exact identity/cardinality/revision evidence for each receipt owner and bind it into the applicable completed-boundary vector, or establish another source-backed exact-zero proof that satisfies the same contract. An absent section or a lazy getter that manufactures a default owner is not proof of emptiness. The source also exposes lazy internal accessors rather than non-lazy `Existing*` accessors for these two owners, so any witness/capture seam must avoid changing the owner while reading it. No receipt export/hydration or P18 state admission is implied.

This is consistent with the P12-D design rule that a mutable component missing from the P12-B witness vector blocks its owner slice until the accepted census is updated. The crosswalk correctly leaves assignment unresolved rather than inventing a D/E/F owner for excluded P18 state. No product clarification is needed to continue the evidence work.

## Scope and readiness boundary

This review accepts the crosswalk as an accurate read-only source audit, not as a complete export/hydration contract or authorization. It does not make City/NPC implementation ready, complete P12-D/E, authorize a new P12-B slice, establish P12-A readiness, or unblock P13. P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked. No tests were run; the candidate delta is documentation-only.
