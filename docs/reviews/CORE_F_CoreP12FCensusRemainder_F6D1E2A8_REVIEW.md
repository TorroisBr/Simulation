# CORE-F Core P12-F Census Remainder — Exact-Tip Review

Date: 2026-11-09

## Reviewed identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Implementation candidate: `f6d1e2a86b273dbc0e889ed9198269ba8e47e1a7`
- Implementation tree: `2d2a91cdb8fb5335bd9686a546f9a5ffbf404f18`
- Current docs-only candidate tip: `ce2b7f7f9d1ff956830987422c793e15aacb6315`
- Current candidate tree: `a86c648d46e7e44763f18d1c48217e6d2d7ddb30`
- Exact implementation base: `2a408689ad6429155e13eb3866691b8d1efee69e`
- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- The post-implementation candidate diff contains only the validation record; the implementation tree is unchanged.

## Verdict

**VALIDATED_CANDIDATE.** The complete implementation diff was reviewed against the exact base. The private validation evidence was accessible and its source hashes match the implementation candidate. No actionable contract or correctness findings remain.

## Review result

Core snapshots the selected owner graph and bound ActorChoice store with the census callback under the execution-boundary operation lock. It invokes the host callback for the raw quiescent census, validates the reported mutation epoch, and only then projects Core-owned rows. Unity no longer performs this projection itself.

The candidate defines one fixed five-row P12-F witness set: ActorChoice, ScheduledDirective, TravelParty, Expedition, and PoliticalKnowledge. The shared witness factory validates the exact owner graph bindings, known unique section identities, non-null owner-instance identities, positive schema versions, and nonnegative cardinality/revision. P12-F composition and restoration rebinding reuse this factory.

Projection semantics are bounded and fail closed:

- If the source contains no P12-F rows, projection fails rather than silently recreating a wholly absent source set.
- If the source contains a partial set, all captured P12-F rows remain in the remainder; the existing composition gate rejects the partial set.
- If all five rows are present, Core removes them only when each exact key, schema, Required role, owner-instance reference, cardinality, and revision matches the Core witness set.
- If any row is stale or foreign, all five source rows remain by object identity for the existing composition gate to reject. No partial subset is removed.
- A valid zero-cardinality ActorChoice owner remains Required and retains its exact owner identity when Core recomposes the vector.

The tests cover exact-set projection followed by Core recomposition, partial-set preservation and rejection, wholly absent-set rejection, stale revision preservation/rejection, and foreign owner-identity preservation/rejection. The updated capture-boundary test includes the five P12-F keys in its expected host remainder and verifies the Core boundary's epoch/projection behavior. The implementation does not change census keys, roles, persisted image schema, or the accepted `UnityBootstrap-Daily-v1` profile. It does not claim complete Core ownership of all 89 families, P12 readiness, or CORE-F completion.

## Validation evidence verified

The official private manifests were inspected and matched to the final code/test inputs. Raw XML and logs remain private; this record contains only run IDs, counts, and hashes.

- CoreValidation run `e4d4c92f95244c5a97c1847631fc1c3f`: **1,078/1,078 cold and 1,078/1,078 warm PASS**, with zero failed, timed out, aborted, or inconclusive tests. Stable input digest: `57e77d5bd63709d49867f7dc602890fcb601e95a369ebc337d0fda5e58b5b5bb`. Cold TRX SHA-256: `3013288414aa25f9e6e739911162dd7f446056f49d9548ef9c6124f9e640b54a`; warm TRX SHA-256: `c222f609881753ae89dd6578713eeccd62d608e0d4d977a9b76802e6a81079e6`.
- Full Unity EditMode run `c42c9c6a721f4ef998d6c93246a781c3`: **3,303/3,303 PASS**, zero failed or skipped. Stable input inventory SHA-256: `06d4ae4bce18387a85be2dab2f4d567a29004981333a22e33e98db26e4db6b0d`. XML SHA-256: `8d1c61d727e3646237940179b770bc6da90dc6cb0a65f2d38a5750d9dadbfedd`; log SHA-256: `e8924d5d59f06c4941de525a1d1659da3a22c6ae81361dd70cd39f7c67f87c85`.
- Official Smoke run `bd2d38acc4154f51a307cd27af8ba4c9`: **5/5 PASS**, zero failed or skipped, with the same stable Unity input inventory. XML SHA-256: `c896c3ed79f1fa22de8d3964a98f9e269c883b87a697b8449c5c513fe6319c28`; log SHA-256: `4ff9f0a688328f91e2b949416f8d86457e1238a9fdca69f95cfca2935eb13ae3`.
- Final source SHA-256 values: `DailyV1ExecutionBoundaryState.cs` `6ee101b16889fd7704ae4bf91b1541ba46e7cbd6830fdff59bf95ad7e2a50066`; `DailyV1RuntimeAdvanceStage.cs` `9f52a597030f6ed9a8c1ad4d801d89915d15a1905cd7b25164ba057ee02a41c5`; `DailyV1ApplicationSessionGateTests.cs` `c67f3acbc7dff35f0cb2b855ef554255098409c2bfe823374a26dba14b6c2cfe`.
- `git diff --check` from the exact implementation base to candidate: PASS.

The CoreValidation and Unity runner commit fields identify the pre-commit base; their stable input snapshots and per-file hashes match the implementation candidate. An earlier CoreValidation run that exposed three fixture expectation failures is preserved but excluded; the successful final run followed the fixture corrections. CoreValidation reports that this worktree contains a Unity Library, so this evidence makes no Library-independence claim. The reviewer did not rerun tests or modify candidate code/tests.
