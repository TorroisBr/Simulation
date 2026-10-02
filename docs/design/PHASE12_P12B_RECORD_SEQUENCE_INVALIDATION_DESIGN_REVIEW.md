# P12-B SimulationRecordSequence invalidation design review

**Verdict: PASS** — bounded technical design is ready for implementation under
the already accepted P12-B capability authorization.

## Reviewed revision

- Canonical base: `1ac675cc558aa919a749167647c10506c11303fc`
- Design candidate: `a9c1214bff81907f6f0b34b329e2d391401f4a7a`
- Design: `docs/design/PHASE12_P12B_RECORD_SEQUENCE_INVALIDATION_DESIGN.md`
- Review date: 2026-10-02
- Review: independent Luna review; exact-base source rechecked after the initial
  summary correction below.

## Findings

The selected profile already has one exact `SimulationRecordSequence` owner,
one cardinality-one census section, and a monotone local revision. All three
production allocation paths converge on `Allocate()`: domain-event recording,
ordinary NPC-decision recording, and first-time occurrence recording through
`TryRecordOccurrenceOnce`. An idempotent receipt replay returns before
allocation. Instrumenting the shared owner method therefore covers the
sequence mutation itself without adding an operation family or changing
domain behavior.

The design correctly requires exact bootstrap/runtime owner identity,
owner-thread and unchanged-baseline preflight, one post-commit notification
per successful allocation, and fail-closed admission if binding or
post-commit notification fails. A post-allocation storage/receipt failure must
retain the consumed sequence value; it is not rolled back. Preflight rejection,
exhaustion, and receipt replay must not advance the sequence or epoch.

The initial review summary understated the call-site count. Exact-base source
reinspection confirmed the additional first-occurrence path in
`NpcDecisionRecorder.TryRecordOccurrenceOnce`. The corrected review verdict
remains PASS; implementation validation must explicitly cover both decision
paths, event recording, receipt replay, and consumed allocation if later
record storage fails.

## Scope limits and implementation obligations

This is sequence-owner invalidation only. It does not notify Event, Decision,
or receipt stores and does not establish coverage of the preceding domain
mutation. It does not prove complete owner/operation or shared-epoch coverage,
global quiescence, capture eligibility, export/hydration, P12-A readiness,
P12-B completion, or P13 readiness.

P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13 remains
blocked; Phase 12 remains open. Implementation must use the reviewed design,
preserve the selected-profile-only boundary, and serialize shared
`SimulationRuntime`/bootstrap edits with other P12 runtime-composition work.
