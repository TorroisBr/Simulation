# Independent review — initial restore admission gate

**Verdict:** `PASS` for implementation and tests. The independent reviewer did not inspect the protected validation manifests or the validation record, and did not independently check the worktree cleanliness. This record does not upgrade that boundary.

- Code commit: `4fff61b27a3cafb6badb7423e7dc434d5559408e`
- Code tree: `7db93bccc24a92baa646b24583d9fd28f21aef1a`
- Base: `51c658f3e047b735226bcfbfdd48bff724bf574e`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Candidate docs-only tip available to reviewer: `e035be68589b494dc27f117f6e67ded77890966f`

The code change requires the exact app-owned `RestoreRequired` admission token for initial application restore, rejects missing, foreign, wrong-image, or genesis tokens before decode or staging, and keeps replacement restore on its expected-session/replacement-operation path without an initial token. The added test verifies early rejection without decode/stage callbacks. Unity fixtures obtain the token by admitting the exact bytes. Existing split-alias owner-census rejection remains asserted; no assertions were weakened. No actionable code/test findings remain.

The reviewer explicitly did not verify test results or evidence artifacts. The Master Orchestrator separately checked the validation run manifests, source hashes, public Unity package integrity, and privacy scan; those checks are described in `VALIDATION.md` and are not attributed to this independent code review.

This is a candidate review record only. CORE-F remains incomplete and Core canonical was not advanced.