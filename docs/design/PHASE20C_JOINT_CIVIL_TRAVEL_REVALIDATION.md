# P20-C — Two-Person Joint Civil Travel: Current-Canonical Revalidation

**Status:** User scope accepted on 2026-10-07. Master source revalidation: **preliminary compatible**. Independent current-base design review and Architecture Roadmap reconciliation remain pending. This record is not a P20-C promotion, implementation review, Phase closure, or Unity validation claim.

## Current canonical baselines

| Authority | Refreshed tip | Relevance |
|---|---|---|
| Architecture | codex/architecture/world-identity-projection at e16796014d348e3b59da7ed848101c4c03926ba5 | Current multi-participant and extensibility contracts; includes both alignment records. |
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

These checks find no semantic conflict in reusing the design under P20-C. They do not substitute for the independent current-base technical-design review required before implementation readiness.

## Dependency edges and gates

P20-C consumes promoted P18-A/B/C, P20-A, and P8-E capabilities. It must integrate with and preserve the P20-B Daily-profile admission boundary. The P20-B relationship is an admission/owner-identity compatibility constraint, not a new traveler behavior dependency. P18-D, P19 loader work, P12 save completion, P13 fork, P14, P15-P17, Groups, and War are not prerequisites for this bounded consumer.

The Architecture Roadmap still carries the historical P20-B/joint-travel mismatch. The General Architect must update that canonical artifact and the current P20 technical-design handoff. The additive architecture reconciliation handoff is separate from this P20 candidate.

## Next review boundary

The historical P20-B technical-design PASS is useful source evidence but does not independently approve a P20-C design at current canonical tips. The General Architect should review this exact revalidation and issue a P20-C handoff or findings. Any implementation candidate must preserve the existing P20-B and P12-Daily rejection behavior, audit the current canonical joint-travel code before changing it, avoid duplicate code, and obtain a fresh exact-tip code review if its code tree changes. No tests were run for this documentation-only revalidation.
