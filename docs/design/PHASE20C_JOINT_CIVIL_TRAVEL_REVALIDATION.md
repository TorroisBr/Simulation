# P20-C — Two-Person Joint Civil Travel: Current-Canonical Revalidation

**Current status (2026-10-07):** Design revalidation **PASS**; the canonical General Architect handoff marks P20-C `READY_FOR_IMPLEMENTATION`. Bounded implementation candidate `c0253cad69c0dc09ee4c601c5048eef99e13ce40` passed exact-tip implementation review and required validation. This record is not a P20-C promotion or Phase closure.

The current architecture handoff is `docs/architecture/P20C_JOINT_CIVIL_TRAVEL_MASTER_HANDOFF.md` at `codex/architecture/world-identity-projection` tip `a29ddd1271fff8fc45abb3270229b43bfe89f2a9`. Its linked independent technical review records `PASS / READY_FOR_IMPLEMENTATION`; it closes the bounded FailedStart design correction and confirms no open product or architecture decision. The Architecture Roadmap now assigns joint travel to P20-C while preserving P20-A and the already-promoted P20-B admission checkpoint.

## Current canonical baselines

| Authority | Refreshed tip | Relevance |
|---|---|---|
| Architecture | codex/architecture/world-identity-projection at a29ddd1271fff8fc45abb3270229b43bfe89f2a9 | Current P20-A/B/C identities, both alignment records, and the reviewed P20-C implementation handoff. |
| P8 | codex/phase8/canonical at 470667d | Closed spatial/travel authorities, including individual Person travel. |
| P18 | codex/phase18/canonical at 8ac2d78 | Closed logical timeline, activity lifecycle, and availability/decision capabilities. |
| P20 | codex/phase20/canonical at fe4909a0fc371a2fedb55cb9cef086e5dbf63526 | P20-A and P20-B current state and code. |
| P12 | codex/phase12/canonical at 94551b08be8cc9347de35eae5051b8e578ea4c1e | Selected Daily-v1 admission remains unchanged and excludes populated P20 travel state. |

No P20-C checkpoint reservation was found in current P20 canonical content or retained P20 checkpoint history.

## Accepted identity and bounded scope

- **P20-A** remains the promoted Synthetic Multi-participant Operation.
- **P20-B** remains the promoted Daily-profile census admission checkpoint. Its identity, promotion record, review, code history, and recorded boundary are preserved.
- **P20-C** is the accepted identity for the bounded Two-Person Joint Civil Travel consumer.
- The two-Person arrangement is this proving fixture only. It does not cap participants, roles, or activity composition in generic P18/P20 contracts.

The reused design describes one supported civil leg, two distinct PersonIds, independent assent, P18-owned scheduling/commitments/lifecycle, a coordinated atomic start across each Person's P8 travel authorities, individual position/Knowledge/effects, staggered arrival, and an explicit terminal rule. It adds no persistent Group/Party membership, shared Person identity, common-interval rule, generic role engine, recruitment AI, military/War behavior, or second scheduler.

## Existing design and code evidence

The historical design is docs/architecture/P20B_TECHNICAL_DESIGN.md, content tip 8afc463fb71112a0c7b8902e7e5673aee9e31bd9. Its independent review docs/architecture/P20B_TECHNICAL_REVIEW.md passed against architecture base f6924e63d8e5731da1d33021d0361e7defe6dad7. The old filenames and review remain historical; this revalidation does not rename or rewrite them.

P20 canonical already contains the joint-travel implementation commits that preceded the P20-B promotion, including 0a3e0a2, e93731c, 6e44c25, d14d235, and a234f20. P20-B's exact reviewed implementation commit is de24dff356a54a0a4037e16c0ca5dc9ca379bc18; its reviewed repository tree is 62f8f3f3ad803e3f8eca832f7e39cff8196d5b85. The current P20 canonical Assets tree is unchanged at 8b579d9f61ad3145b535b27cdd71a37d512a32b1, identical to the de24dff Assets tree. The P20-B review and its focused 8/8, ALL EditMode 2273/2273, Smoke 5/5, and diff-check evidence remain exact-tree evidence for that code.

The P20-B State explicitly limits that checkpoint to the Daily-v1 empty-owner census/admission seam. This record preserves that attribution. Existing joint-travel code is retained in canonical history as input to P20-C revalidation; this record does not call it a promoted P20-C result or change P20-B's scope. A P20-C delivery claim requires a current-base review that explicitly evaluates the consumer against the accepted P20-C contract.

## Compatibility findings

1. **P18 ownership remains intact.** P18 owns timeline, lifecycle, participant commitments, availability, and terminal disposition. P20-C remains one consumer-bound composition; P20-A's synthetic transition participant stays isolated. The reused design has no P18-D dependency.
2. **P8 ownership remains per Person.** P8 owns each person's route, position, passage validation, and travel transition. Joint start must coordinate prepared per-Person changes atomically; it does not create a shared position or merge Knowledge. P20-C is the consumer-specific batch boundary, not a universal P8 group-travel rule.
3. **P20-B admission remains a profile boundary.** P20-B's Daily-v1 provider is explicitly empty and rejects populated P20 travel state. P20-C must preserve that behavior and use a separately admitted consumer composition. It does not enlarge the accepted P12 Daily-v1 profile or require P12-B completion.
4. **The accepted P20 semantics still fit.** Independent decisions, current-truth validation, coherent coordinated transitions, distinct outcomes, and separation from persistent membership remain consistent with the current P20 Brief and multi-participant alignment.
5. **Conditional input remains conditional.** The reused design does not require P11 external command capture; no new command queue or public API is selected here.

These checks find no semantic conflict in reusing the design under P20-C. The independent current-base technical-design review and architecture identity reconciliation have now passed; this source record carries the refreshed baseline rather than treating the older preliminary review as current authority.

## Dependency edges and gates

P20-C consumes promoted P18-A/B/C, P20-A, and P8-E capabilities. It must integrate with and preserve the P20-B Daily-profile admission boundary. The P20-B relationship is an admission/owner-identity compatibility constraint, not a new traveler behavior dependency. P18-D, P19 loader work, P12 save completion, P13 fork, P14, P15-P17, Groups, and War are not prerequisites for this bounded consumer.

The former P20-B/joint-travel mismatch is resolved by the canonical architecture identity reconciliation. No P20-B history is rewritten; the joint-travel consumer is P20-C.

## Implementation boundary

Implementation preserves the existing P20-B and P12-Daily rejection behavior, corrects FailedStart revision synchronization/reconstructability without weakening strict restore equality, and avoids duplicate code. Exact-tip review is recorded in `PHASE20C_IMPLEMENTATION_REVIEW.md`; focused P20/P18 regressions, ALL EditMode, official Smoke, `git diff --check`, and explicit P12 Daily-v1 admission/exclusion tests are recorded in `../validation/P20C/VALIDATION.md`. The implementation remains a P20-C candidate until canonical integration; Phase 20 remains open.
