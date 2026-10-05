# P12-B CrimeSocialAppraisal invalidation design review

**Result:** PASS — READY_FOR_IMPLEMENTATION under the previously accepted P12-B capability authorization.

**Reviewed design tip:** `7770119d7ae4dd69186ff6394946168db1473527`

**Reviewed design tree:** `0cc85e95776512a35edee114f51709af23a5180d`

**P12 canonical base:** `a00cba49f642c9b3203df838f9ba27d675f560b2`

**Architecture:** `ffd75652d89d862b83d634868c560f8540869b89`

**Intraday/extensibility alignment:** `4b6dd1d38cffeaf3cc1ac3effea0f8ede8771194`
**Multi-participant activity alignment:** `c285466c355103d3637ac165246591b72eb7bda0`

The independent exact-tip review confirmed the three exact cardinality-one owners, existing temporal identities, dynamic post-publication Person registration, all direct and composite writers, and the current `ContinuationCensusProtocol` semantics. Store-local revisions advance for each committed row write or compensation. The outer `TryAcceptTheftOutcome` context owns the mutation scope through its nested `TryRecordKnowledgeAndAppraise` call and outcome rollback; the nested call joins that same-thread, same-integration scope. A standalone `TryRecordKnowledgeAndAppraise` owns its own scope.

The protocol's reserved and immediate notification paths each advance the shared mutation epoch once for one logical outer commit over a distinct set of changed sections. Inside one active registered operation, the design uses one reserved token. Outside exactly one active operation, it uses immediate notification only when `TryValidateMutationEpochCapacity` confirms available capacity and no active token; the current public `NotifyCommittedMutations` path is owner-thread-bound and does not require an active operation. Synchronous exact-owner hooks reject unrelated or reentrant mutation while the scope is active, so the checked capacity cannot be consumed before the immediate notification. If an active token prevents either path, the owner write fails before mutation.

The reviewed local-revision headroom is 2/2/4 advances for a full theft acceptance and 0/2/4 for standalone knowledge-and-appraise, across Outcomes/Knowledge/Reactions. The review found the forward and compensating call graph compatible with those maxima. The design adds no operation IDs, generalized transaction framework, gameplay behavior, or separate checkpoint. `git diff --check` passed; no Unity test run was required for this documentation-only design review.

Implementation is limited to the accepted P12-B owner-census and mutation-invalidation capability. P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, P13 remains blocked, and this review grants no complete owner/epoch coverage, global quiescence, capture eligibility, export, hydration, or Phase-closure claim.
