# P12-B census owner composition candidate

**Status:** Submitted for independent integration review. This is not a canonical
promotion or Phase closure.

**Canonical base:** `codex/phase12/canonical` at
`19d0373d6a71b63536248ecc9091e66c9b3a708b`.

**Code candidate:** `codex/phase12/P12BCensusOwnersCompositionIntegration`
at `72239ad1013dad5d507bad9737c358b1cadfe752`, tree
`c9fc250195bb5fda11935c687294f8ae5738bd9a`. The candidate code tree is
identical to the locally validated tree `3ab131d1aab9ea52b77015c10205fc6269d547c0`.
The source merge commits remain ancestors of the code candidate; no canonical
history was rewritten.

**Candidate record branch:** `codex/phase12/P12BCensusOwnersCompositionCandidateRecord`.

## Bounded delivery

This candidate composes three already bounded census owner families through
the normal runtime/bootstrap composition:

- **Scheduled directives:** publishes the reviewed fixed census provider
  through composition and corrects the test expectation to one revision per
  successful mutation (three adds plus three terminal updates = revision 6).
- **Expeditions:** publishes the reviewed census providers for the existing
  Expedition owners through the normal composition.
- **NPC Knowledge:** installs the per-NPC Knowledge family in the runtime's
  existing census protocol. Normal NPC registration and unregistration
  reconcile its sections with the existing SpatialKnowledge and Inventory
  families. The family is ordered by stable runtime identity and bound to the
  exact installed owner. Bootstrap exposes the providers, and selected-profile
  evidence verifies the live Knowledge section identities, schema, counts,
  and revisions alongside the existing census families.

The selected-profile tests read the actual composed owners and authored City
market truth. The Knowledge test verifies successful add/remove reconciliation,
retention of the Inventory provider family, and one shared membership epoch
per normal roster operation. Failed Knowledge reconciliation leaves the
Inventory provider snapshot and exact owner evidence unchanged.

## Retained source candidates

- ScheduledDirective implementation: code tip
  `03ffa1031c3a7f125d1d8cff00f72d22749816bb`; static independent review
  record `f906453` is retained on
  `codex/phase12/P12BScheduledDirectiveCensusImplementationReview`.
- Expedition implementation: code tip
  `fabebcce0a4b6d7e278a41a4cd44179c98ecea6c`; the earlier exact-tip
  independent implementation review passed. Its bounded review did not claim
  bootstrap composition; this candidate supplies that integration.
- NPC Knowledge implementation and test completion: exact reviewed tip
  `b3939550ccbef0869366235bd526f8e1ae013399`, tree
  `8fad4baca495507edbcc9661bbc96df530d3541e); independent exact-tip review
  passed. The source provider and test branch remains
  `codex/phase12/P12BKnowledgeCensusTestCompletion`.

The resulting implementation is a composition integration of these retained
capabilities on current P12 canonical. This candidate does not replace their
owner-specific contracts or broaden their semantics.

## Validation evidence

Validation was run on the exact code tree `c9fc250`:

- `NpcKnowledgeCensusTests`: 17/17.
- `SimulationBootstrapCompositionTests`: 14/14.
- `ScheduledDirectiveCensusTests`: 6/6.
- ALL EditMode: 2099/2099.
- Complete official Smoke filter: 5/5.
- `git diff --check`: clean.

The validation result directories were ignored local artifacts and are not
present in the migrated E: checkout. These counts are retained from the
completed exact-tree validation run in the orchestration record; the reviewer
must call out that XML/log hashes cannot be independently checked from this
checkout. The code tree itself is unchanged from the validated tree.

## Scope limits

This adds partial passive census composition only. It does not complete the
selected-profile owner inventory or P12-B. It does not add shared mutation
epoch wiring for every supported owner write, global owner-thread/quiescence
proof, capture eligibility, immutable owner exports, staged hydration, or
restoration. It does not claim temporal occurrence cardinality from structural
owner sections. P12-B remains incomplete; P12-A remains
`WAIT_DEPENDENCY`; downstream P12 checkpoints remain blocked as recorded;
P13 remains dependency-gated. Phase 12 remains open.

The candidate is based on the current canonical tip, not on numeric Phase
ordering or a future checkpoint. Explicit human approval is required before
canonical promotion under `docs/EXECUTION_MODEL.md`.
