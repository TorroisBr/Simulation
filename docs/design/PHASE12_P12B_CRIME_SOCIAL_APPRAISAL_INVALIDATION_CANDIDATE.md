# P12-B Crime/Social Appraisal Invalidation Implementation Candidate

**Status:** initial code review returned NEEDS_CHANGES; see [implementation review](PHASE12_P12B_CRIME_SOCIAL_APPRAISAL_INVALIDATION_IMPLEMENTATION_REVIEW.md). The fix and fresh exact-tip review are pending. This is not canonical promotion or P12-B completion.

## Boundary and source

This accepted P12-B prerequisite-capability slice adds selected-profile owner census witnesses and shared-epoch invalidation for the existing `CrimeSocialAppraisalWorldState` owners. It preserves the existing action/domain behavior and the already reviewed operation boundary.

- P12 canonical base: `a00cba49f642c9b3203df838f9ba27d675f560b2`
- Reviewed implementation design: `7770119d7ae4dd69186ff6394946168db1473527` (tree `0cc85e95776512a35edee114f51709af23a5180d`)
- Implementation candidate: `bb45f5025128f1aa192cb2a498ed1b887c741e65`
- Implementation tree: `f9a6a88b0cf34a6b4b54a735b4fce77fbe14ce61`
- Candidate branch: `codex/phase12/P12BCrimeSocialAppraisalInvalidationImplementation`
- Current canonical at validation/review start: `a00cba49f642c9b3203df838f9ba27d675f560b2`
- Architecture: `ffd75652d89d862b83d634868c560f8540869b89`
- Intraday/extensibility alignment: `4b6dd1d38cffeaf3cc1ac3effea0f8ede8771194`
- Multi-participant activity alignment: `c285466c355103d3637ac165246591b72eb7bda0`

The candidate registers exact singleton Outcome, Knowledge, and Reaction owners with empty admission baselines; tracks committed row revisions including compensation; joins nested Knowledge/Appraisal into the outer TheftAcceptance operation; and emits one shared epoch notification for the logical operation. The reviewed local revision ceilings remain 2/2/4 for outer TheftAcceptance and 0/2/4 for standalone KnowledgeAndAppraise, in Outcomes/Knowledge/Reactions order. Direct store writes retain one-write invalidation. Local budget or epoch-capacity refusal occurs before the affected Crime/Social writes; the existing money transfer and compensation order remains unchanged.

## Validation

All evidence below corresponds to implementation commit `bb45f5025128f1aa192cb2a498ed1b887c741e65`, tree `f9a6a88b0cf34a6b4b54a735b4fce77fbe14ce61`. No executable source or test files changed after these runs.

| Suite | Result |
|---|---:|
| `P12CrimeSocialAppraisalInvalidationTests` | 10/10 PASS |
| ALL EditMode | 2366/2366 PASS |
| Official Smoke (`-TestFilter Smoke`) | 5/5 PASS |
| `git show --check` for implementation commit | PASS |

The six Unity XML/log artifacts are in [`P12B-CrimeSocialAppraisal-validation-20261005.zip`](../validation/P12B-CrimeSocialAppraisal-validation-20261005.zip), SHA-256 `7c4099b02819283399cdcc25c6650d1372b0aeb96f70fd9300ad810be5f8d314`.

| Result | XML SHA-256 | Log SHA-256 |
|---|---|---|
| Focused 10/10 | `28cf69ca0042a09dd4ce50bd7cae610cecc96beaa7be21334967905653753d08` | `322f167670a642f0fb1c0f3afd9247477cdfe2012d7d1e0932ba43ba8808d647` |
| ALL EditMode 2366/2366 | `2a29aa6b45a8f1645f5999fff9d8361e489bb55756667b97224491ea131bfc26` | `ac68506b6bcc66c03ee73d82029de541ffb4ac1f144c2166841f135feac309ae` |
| Official Smoke 5/5 | `99bb13aefe6e8b0b5ff66a21a4e1fa843838b18b5aea1a9390ee490be74e3f55` | `67acea0f330d0d7d263b8a4f0f30771a8e8e63fb7a3e84b933733943c61fab4c` |

## Limits

This is a bounded census and invalidation capability for the three existing Crime/Social Appraisal owners. It does not add gameplay behavior, change robbery eligibility or amounts, make Person-backed NPC actions available, or add a generalized transaction framework. It does not establish complete selected-profile owner or shared-epoch coverage, global owner-thread/quiescence, capture eligibility, export, hydration, or save/load support.

P12-B remains incomplete. P12-A remains `WAIT_DEPENDENCY`. P13 remains blocked. Phase 12 remains open. No canonical promotion is recorded by this candidate note.
