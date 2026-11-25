# CORE-F P12-F Core Owner Codec Integration Review

- Verdict: **PASS / VALIDATED_CANDIDATE**; no actionable findings.
- Reviewed candidate: `6cf1fbd4eedfe50757dccb5764f3efd88505ce97` (tree `16e3703fe2a85c857f182dc1f36a474d0d1264d7`).
- Base: `1706c17139c270ed60b9d34776b45f8e45ecbdc6`.
- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`, an ancestor of the candidate.
- Independent reviewer: `/root/p12f_core_codec_review`.

## Scope reviewed

The full 37-file candidate diff moves the existing P12-F TravelParty and Expedition structural snapshot/codec code into `Simulation.Core`, preserves Unity asset GUIDs, links the same source into the SDK project, and adds Core codec tests. The structural wire encodings, section IDs and roles, row keys, field ordering, enum representations, size bounds, revisions, TravelParty roles/member order/cost bits, and Expedition progress/identity values remain stable. Exact capture token, owner vector, instance and census checks remain intact. Cross-owner NPC/spatial relationship checks stay in the Unity staging boundary. Existing Unity friend-assembly access is reused; no new public API or duplicate owner was introduced. Test assertions were not weakened.

## Exact-tip validation

- Official CoreValidation run `666e60e3298740558d80ce28c56311b4`: 758/758 cold and 758/758 warm. All 382 recorded source inputs matched the candidate. The validation record retains the private TRX hashes and states that the private reports are not a public TRX-EVIDENCE-v1 package.
- Official Unity EditMode filter `P12AContinuationCodecTests`, run `f75e813f104b44939ecaaea4fee3d78c`: 37/37 passed; all 1,884 recorded input files matched the candidate. The XML hash is recorded in the validation record.
- `git diff --check`: PASS.

The prior full EditMode 3,263/3,263 and Smoke 5/5 evidence belongs to the previous snapshot checkpoint, not this codec-move tip. No full-suite result is claimed for this tip.

## Limits

This is a reviewed Core source-ownership candidate only. It does not promote Core canonical, complete P12-F or CORE-F, establish the detached host, or satisfy remaining full-package validation and evidence requirements.
