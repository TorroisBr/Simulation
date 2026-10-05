# P12-B Crime/Social Appraisal Invalidation Implementation Candidate

**Status:** `VALIDATED_CANDIDATE`; independent exact-tip code review passed for corrected implementation `ec956c7247c39f8984f7c5a6bca3d813091a0047`. The review is recorded in [the R1 implementation review](PHASE12_P12B_CRIME_SOCIAL_APPRAISAL_INVALIDATION_IMPLEMENTATION_REVIEW_R1.md). The initial implementation review found a post-commit failure-path issue at `bb45f50`; its durable finding remains in [the initial implementation review](PHASE12_P12B_CRIME_SOCIAL_APPRAISAL_INVALIDATION_IMPLEMENTATION_REVIEW.md). This candidate is not canonical promotion or P12-B completion.

## Boundary and source

This accepted P12-B prerequisite-capability slice adds selected-profile owner census witnesses and shared-epoch invalidation for the existing `CrimeSocialAppraisalWorldState` owners. It preserves existing action/domain behavior and the reviewed operation boundary.

- P12 canonical base: `a00cba49f642c9b3203df838f9ba27d675f560b2`
- Reviewed implementation design: `7770119d7ae4dd69186ff6394946168db1473527` (tree `0cc85e95776512a35edee114f51709af23a5180d`)
- Initial implementation candidate: `bb45f5025128f1aa192cb2a498ed1b887c741e65` (tree `f9a6a88b0cf34a6b4b54a735b4fce77fbe14ce61`), review `NEEDS_CHANGES`
- Corrected implementation candidate: `ec956c7247c39f8984f7c5a6bca3d813091a0047`
- Corrected implementation tree: `61956354146f3437396d93d91c4df246da8df89d`
- Candidate branch: `codex/phase12/P12BCrimeSocialAppraisalInvalidationImplementation`
- Current canonical at validation/review start: `a00cba49f642c9b3203df838f9ba27d675f560b2`
- Architecture: `ffd75652d89d862b83d634868c560f8540869b89`
- Intraday/extensibility alignment: `4b6dd1d38cffeaf3cc1ac3effea0f8ede8771194`
- Multi-participant activity alignment: `c285466c355103d3637ac165246591b72eb7bda0`

The candidate registers exact singleton Outcome, Knowledge, and Reaction owners with empty admission baselines; tracks committed row revisions including compensation; joins nested Knowledge/Appraisal into the outer TheftAcceptance operation; and emits one shared epoch notification for the logical operation. Local revision ceilings remain 2/2/4 for outer TheftAcceptance and 0/2/4 for standalone KnowledgeAndAppraise, in Outcomes/Knowledge/Reactions order. Direct store writes retain one-write invalidation. Local budget or epoch-capacity refusal occurs before the affected Crime/Social writes; the existing money transfer and compensation order remains unchanged.

The correction keeps committed domain results distinct from notification failure. If rows have committed but the final census notification faults, the runtime faults closed while the integration still reports that committed theft as accepted; it must not tell `CrimeSystem` to reverse money against rows that now exist. Standalone Knowledge/Appraisal follows the same post-commit result rule. A new regression injects a stale owner witness at notification time and verifies the action retains the transfer and committed rows while subsequent census assessment reports `ProtocolFaulted`.

## Validation

All evidence below corresponds to corrected implementation commit `ec956c7247c39f8984f7c5a6bca3d813091a0047`, tree `61956354146f3437396d93d91c4df246da8df89d`. No executable source or test files changed after these runs.

| Suite | Result |
|---|---:|
| `P12CrimeSocialAppraisalInvalidationTests` | 11/11 PASS, including `TheftActionDoesNotReverseCommittedMoneyWhenPostCommitEpochNotificationFails` |
| ALL EditMode | 2367/2367 PASS |
| Official Smoke (`-TestFilter Smoke`) | 5/5 PASS |
| `git diff --check` | PASS |

The six exact Unity XML/log artifacts are in [`P12B-CrimeSocialAppraisal-validation-20261005-r1.zip`](../validation/P12B-CrimeSocialAppraisal-validation-20261005-r1.zip), SHA-256 `7c13183315486d6753879b36002f42ffa962fea15e6c651ccb94f0e8f35e253f`. The archive entries and all six XML/log hashes were independently checked against this manifest after the correction.

| Result | XML SHA-256 | Log SHA-256 |
|---|---|---|
| Focused 11/11 | `9294aa749a86212e287e292cb918d96ef102f56e9cba9bc7818ecceef161c087` | `82fe28b5ef44e81dbc9fb9be26d3bb2abc2a75b1bbaa30dc9b237d300bc4ee24` |
| ALL EditMode 2367/2367 | `7b214244f00496dda866b135f18fb03aa5c5b91880b2d7ee8563ea36069924f5` | `e8fe283dfe4b43da0793afc56baabeaf7f7c8deb68dbbc7f18db5c8991eb02f2` |
| Official Smoke 5/5 | `5d52fea54c934616b832a8a22fe91d0f478702bf7f2a333a6684f66b3845ca84` | `d66839591e26087c9423c54344857b1a89b88b6d9f022f099078efe87e5b8585` |

## Limits

This is a bounded census and invalidation capability for the three existing Crime/Social Appraisal owners. It does not add gameplay behavior, change robbery eligibility or amounts, make Person-backed NPC actions available, or add a generalized transaction framework. It does not establish complete selected-profile owner or shared-epoch coverage, global owner-thread/quiescence, capture eligibility, export, hydration, or save/load support.

P12-B remains incomplete. P12-A remains `WAIT_DEPENDENCY`. P13 remains blocked. Phase 12 remains open. This candidate note records no canonical promotion.
