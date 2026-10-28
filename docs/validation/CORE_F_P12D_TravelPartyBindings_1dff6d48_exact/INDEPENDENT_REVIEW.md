# Independent exact-tip review — P12-D restored TravelParty owner bindings

## Verdict

**NEEDS_CHANGES — evidence packaging only.** The implementation and test-source review is PASS; no actionable source, behavior, or assertion finding was found.

## Reviewed identity

- Candidate: `1dff6d485da0f1b4460b5114e5883d5fadecfc18`.
- Tree: `f79cb8568270e701bc1d6ede6654ab7c8711740e`.
- Base: `20475a3aa6822278b2c38b8061e97a558256ec4e`.
- Core canonical inspected: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Independent reviewer: `/root/p12f_core_codec_review`; the reviewer did not modify candidate source or evidence.

## Implementation and test review

The Core restored-owner-graph admission checks validate active TravelParty bindings against the selected NPC owner graph and fail closed before publication. The reviewed checks preserve the dynamic party membership vector, reciprocal relationship constraints, deterministic order, route/destination and boundary consistency. Added tests exercise a party spanning the fixture NPC vector and reject the reviewed malformed relationship cases. No writer, wire-format, codec, owner-identity, ordering, or P12 scope change was found.

## Evidence finding

The exact-source validation is bound to the reviewed candidate. CoreValidation passed 766/766 cold and warm. Unity passed the focused `P12GPoliticalBindingRestoreTests` selection 15/15, full EditMode 3263/3263, and the `Smoke` filter 5/5. The complete EditMode result-only package and manifest hashes match the package verifier.

The permanent package is incomplete under `VALIDATION_EVIDENCE_POLICY.md` §§2–3. Focused and Smoke filtered run records and CoreValidation cold/warm result records remain only in protected local retention. The full EditMode result contains the corresponding focused and Smoke identities/outcomes but does not retain those separate invocation results. Exact portable Unity argument vectors are recorded in `VALIDATION.md`; the portable CoreValidation command vector is still absent from that record. Preserve the originals unchanged and complete policy-compliant durable per-gate evidence before treating this checkpoint as promotion-ready.

This review does not promote Core canonical, close P12-D, or complete CORE-F.
