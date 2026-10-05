# P12-B Crime/Social Appraisal Invalidation Implementation Review

**Result:** NEEDS_CHANGES for the initial implementation tip.

- P12 canonical at review: `a00cba49f642c9b3203df838f9ba27d675f560b2`
- Candidate branch: `codex/phase12/P12BCrimeSocialAppraisalInvalidationImplementation`
- Code candidate: `bb45f5025128f1aa192cb2a498ed1b887c741e65`
- Reviewed code tree: `f9a6a88b0cf34a6b4b54a735b4fce77fbe14ce61`
- Exact code base: `d34b4d1460288b7e996dd3816dd5154dbf670c42`
- Candidate evidence commit: `8e6f34a2a0de9f62eddd392bf45285cb33588bec` (docs only)
- Design review: PASS at `7770119d7ae4dd69186ff6394946168db1473527`
- Architecture / alignments: `ffd75652d89d862b83d634868c560f8540869b89`; intraday `4b6dd1d38cffeaf3cc1ac3effea0f8ede8771194`; multi-participant `c285466c355103d3637ac165246591b72eb7bda0`.

The independent reviewer confirmed the code commit's parent, exact tree, unchanged source tree after validation, and clean base-to-code whitespace check. The pushed candidate manifest and validation archive contain matching artifact hashes and passing XML totals: focused 10/10, ALL EditMode 2366/2366, official Smoke 5/5.

## Required correction

The integration currently returns `accepted && scopeClosed` from `TryAcceptTheftOutcome`. It can therefore return false after all Outcome, Knowledge, and Reaction rows have committed if the final P12 notification fails. `P12CrimeSocialAppraisalMutationCoordinator.CloseContext` faults the runtime and reports the close failure, but `CrimeSystem.TryExecuteSteal` interprets the false sink result as pre-write rejection and reverses the money transfer. The Crime/Social rows remain committed, producing a phantom theft and breaking the action's compensation boundary.

Keep pre-write capacity refusal and fully compensated domain rejection as false. Once the domain operation committed, a later census notification failure must fault the runtime without being translated into the sink rejection signal that triggers money compensation. Apply the same rule to standalone `TryRecordKnowledgeAndAppraise` so a caller cannot retry a committed Knowledge/Appraisal write after a post-commit close fault. Add an action regression that injects a post-write census notification failure and verifies that funds are not reversed while the committed Crime/Social rows remain visible and the protocol/runtime is faulted.

This finding does not require a product or architecture decision. It is a bounded correction to preserve existing domain result and compensation semantics under the already-reviewed design. The candidate is not ready for promotion; after correction, rerun focused, ALL EditMode, official Smoke, and diff-check, then obtain fresh exact-tip independent code review.

## Unchanged limits

P12-B remains incomplete. P12-A remains `WAIT_DEPENDENCY`. P13 remains blocked. No complete owner or shared-epoch coverage, global quiescence, capture eligibility, export, hydration, save/load, or Phase-closure claim is made.
