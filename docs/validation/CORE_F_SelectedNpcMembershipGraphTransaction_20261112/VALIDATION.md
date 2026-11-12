# Selected NPC Core membership transaction

## Scope

The selected NPC registration and removal paths now delegate owner-graph membership publication to DailyV1SelectedNpcMembershipChange. Core prepares the replacement graph and, when a selected actor roster exists, its replacement roster as one compare-and-swap transaction. When no actor roster has been bound, Core publishes or rolls back only the owner graph; it does not synthesize a roster. Unselected NPC roster projection remains a host compatibility path.

The focused regression covers graph-only addition, removal, publication, and rollback. Existing graph-plus-roster atomicity coverage remains unchanged. This checkpoint does not claim completion of DailyV1Application.TryStart, the detached host, or CORE-F.

## Validation

| Gate | Result | Run |
|---|---:|---|
| Official CoreValidation cold | PASS, 1,103/1,103 | d690e7758c1b4b3d834b7e4987df3533 |
| Official CoreValidation warm | PASS, 1,103/1,103 | Same run |
| Focused bootstrap/composition EditMode | PASS, 41/41 | 73d5132394064d9a8a45f9499aca943d |
| Full Unity EditMode | PASS, 3,318/3,318 | e0325aa28af94cc6afdddcc450ded71f |
| Official filtered Smoke (EditMode, filter Smoke) | PASS, 5/5 | 7d774117167148389d7bd9184c8f1938 |
| git diff --check | PASS | Candidate worktree |

CoreValidation discovered the new SelectedNpcMembershipChangePublishesAndRollsBackGraphWithoutRoster test in both cold and warm runs. Its protected source manifest records the pre-checkpoint base 68699734ba26957987a438e8fa77a99c00cb2b32 and working-tree input digest d09257a6ac8622c11b4edc3ab2b83e94281add161fa6d057af3a0994739c2588. The original TRX and logs remain in private worktree-local evidence under .private-core-validation/d690e7758c1b4b3d834b7e4987df3533; no public TRX package is claimed.

The full Unity EditMode run had a stable source-input snapshot. Its privacy-normalized package is UnityFull/coref-selected-npc-membership-graph-68699734.zip with manifest UnityFull/coref-selected-npc-membership-graph-68699734.manifest.json. Archive SHA-256: 9c0aad5fa693b4a36f45747f316e88a5d75146fe745dd22bdbed9eabde66b84e. The official verifier accepted it against the protected run manifest (runs=1, entries=1). Original XML and raw logs remain private under .private-unity-validation/e0325aa28af94cc6afdddcc450ded71f.

The five filtered Smoke identities also occur in the full EditMode report. An exploratory PlayMode filter returned zero cases and is not counted as validation.
## Independent review

Exact-tip independent review: PASS at bdfc406056bee8836b0d815349aa6a4cf4bcf9b4; see docs/reviews/CORE_F_SelectedNpcMembershipGraphTransaction_BDFC4060_INDEPENDENT_REVIEW.md. The review records private Core TRX/log availability and the graph-only Unity composition evidence limit.
