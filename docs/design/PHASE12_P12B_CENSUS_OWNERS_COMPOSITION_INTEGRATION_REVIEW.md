# P12-B census owner composition integration review

## Verdict

**VALIDATED_CANDIDATE — PASS.** Canonical promotion remains a human gate.
The candidate adds partial passive census composition only. P12-B remains
incomplete and P12-A remains `WAIT_DEPENDENCY`.

## Exact references reviewed

- Current canonical/base: `codex/phase12/canonical` at
  `19d0373d6a71b63536248ecc9091e66c9b3a708b`.
- Code candidate: `codex/phase12/P12BCensusOwnersCompositionIntegration`
  at `72239ad1013dad5d507bad9737c358b1cadfe752`.
- Code tree: `c9fc250195bb5fda11935c687294f8ae5738bd9a`.
- Candidate record, including the reviewed wording correction:
  `codex/phase12/P12BCensusOwnersCompositionCandidateRecord` at
  `3b60dd61c7d84e08cfda587e83fb18a69decb957`.
- Durable candidate description:
  `docs/design/PHASE12_P12B_CENSUS_OWNERS_COMPOSITION_INTEGRATION_CANDIDATE.md`.
- Reviewer: independent P12 implementation reviewer, separate from candidate
  author; no candidate edits or promotion.

## Review

The reviewer inspected the integrated owner composition against canonical
P12 contracts. The Expedition provider is bound to the same store used by its
system. ScheduledDirective is published through the composition. The NPC
Knowledge family is exposed through the runtime census protocol and produces
ten stable sections per NPC; it validates exact owner identity and shared
revisions within each owner group and stages roster reconciliation before
publishing it.

Selected-profile evidence reads the live providers. Tests cover successful
and failed Knowledge reconciliation, preservation of the Inventory family,
and membership epoch changes. The reviewer found no scope or temporal/
cardinality defect. No permanent one-activity-to-one-actor or NPC-owned
authority contract is inferred.

## Validation evidence and limits

The completed exact-tree run reported Knowledge census 17/17, bootstrap
composition 14/14, ScheduledDirective census 6/6, ALL EditMode 2099/2099,
complete official Smoke 5/5, and clean `git diff --check`. These counts are
recorded in the candidate record.

The reviewer independently verified the retained official Smoke XML/log:
the Smoke run passed 5/5, and its assembly metadata reports 2,099 test cases.
That run executed the five Smoke tests; the assembly metadata does not itself
prove the ALL EditMode run. The focused-suite and full EditMode XML/logs are
absent from the migrated checkout, so those exact results could not be
independently inspected. This evidence limitation does not prevent the
reviewer's VALIDATED_CANDIDATE verdict; it remains explicit.

## Scope and promotion boundary

The candidate does not establish complete profile coverage, global committed
write invalidation, runtime-wide owner-thread/quiescence, capture eligibility,
immutable exports, staged hydration, restoration, or P12-A readiness. It does
not claim temporal occurrence cardinality from structural owner sections.

P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`; downstream P12
checkpoints and P13 remain gated by their recorded dependencies. Phase 12
remains open. Per `docs/EXECUTION_MODEL.md`, an explicit human approval is
required before advancing `codex/phase12/canonical`.
