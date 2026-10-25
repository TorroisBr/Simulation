# CORE-F P12-D selected NPC Knowledge owner review

## Verdict

VALIDATED_CANDIDATE. This independent exact-tip review found no actionable implementation or test-integrity findings. The verdict applies only to the candidate and evidence identities below; it does not promote Core canonical or complete CORE-F.

## Review identities

- Candidate branch: codex/core/CORE-F-p12d-npc-core-sources
- Candidate commit: ced294c0f2bd56b9b585f882e21bb01c23a1f177
- Candidate tree: d8b790049e5d7b6510b84228bccf2a42d95d1bf2
- Candidate base: f8d9d81ab1bdab21fe189d5bcec70f1a66264202
- Current Core canonical: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Remote branch and canonical refs were refreshed and matched those exact identities. The canonical tip is an ancestor of the candidate base.

## Scope reviewed

The 20-file base-to-candidate change moves selected NPC ExplorableSiteKnowledge and AdventureSiteIntelKnowledge state into internal Core-owned runtime owners. The Unity runtime types remain serialization and compatibility projections. The selected runtime graph, census providers, P12-D capture witness and staged restoration use the same Core owner instances.

The migration preserves the existing owner keys and ordering. Explorable-site observations remain keyed by SiteRuntimeId and retain their LocationRuntimeId, observed/received days and source. Their update priority remains observed day followed by source priority. Adventure observations retain their type-specific identity, local-place scope, provenance and payload. Their update priority remains observed day, source priority, then received day. The separate site owner revision and the shared Adventure owner revision advance once per accepted insertion or replacement; stale updates and revision saturation are rejected.

Core hydration receives the existing P12-D F-row values and exact revisions. It validates the complete input before publishing owners, rejects duplicate identities, retains row order and does not replay observation writers. The P12-D image package stages the selected Core owner vector once and passes those exact owner instances into the Unity NPC projection restore. The Unity wrappers bind to those instances and fail closed on identity mismatch. The direct source-boundary staging path uses the same Core hydrator. Existing section IDs, schemas, roles, cardinalities and persisted value representations are unchanged.

The selected wrappers delegate reads and writes to Core owners and copy values back only as Unity serialization projections. The census provider reports the selected Core owner identity and exact local counts/revision, and rejects a mismatched wrapper binding. Unselected legacy NPCs continue using their existing wrapper-owned state. No public Core API was added.

Tests were not weakened. Updated Unity assertions now require wrapper-to-Core owner reference identity and census identity. New Core tests cover ordered facts, replacement priority, revision changes, duplicate rejection without partial initial installation and P12-D restoration of populated knowledge facts with their exact revisions. The two new Unity .meta GUIDs are unique in the candidate tree.

## Independent validation evidence

The reviewer did not rerun tests. I inspected the exact-tip private run manifests, artifact hashes and source input inventories. The raw manifests and logs remain private and are not copied into this record.

- CoreValidation run 426ee406a42a4d01b14c13283fe24ef0: cold and warm 817/817 PASS; stable input; manifest SHA-256 c1b30de185991da71736eef6c06d38b70a6ec717553b7725d178346da7eccf1a. The manifest binds the exact candidate commit and tree. All changed Core project and test source inputs matched the candidate checkout bytes.
- Full Unity EditMode run 2f65fe6efcf241299bc7d4e923724504: 3264/3264 PASS, zero failures, skips or inconclusive cases; stable input; manifest SHA-256 061856fd888472005a15fadd5f9b4ab2be719ac68295cfc16dfe66d5f4a8c4a0. The changed Unity and Core package inputs matched the candidate checkout bytes. The result XML SHA-256 is 819fcc8fe622232f933ae5d8b056154d9711fa4d5556aa67f161db0d4887c5e1; log SHA-256 is 208d85744549275a09c276625daed47249c0e2d76a9d6031026564156ee30080.
- Official Smoke run 1a31f703dbf843aebc26a0f3364191e5: filtered Smoke 5/5 PASS, zero failures, skips or inconclusive cases; stable input; manifest SHA-256 b4e6ce5928d497caa5c0d1a1507249f2cc2d397725f91e4bb86a7ab5dbcb1ea0. The filter was Smoke. Result XML SHA-256 is 732c4930ea6f0e31a27d438e73a97c9a9080b1e5f3a74d2ae3e87389a23ab64c; log SHA-256 is 1f9a6f4abc50ecb07f69d8833cbd60ed0b2431a6911f4b1f53bb58e3c3cef8b9.
- git diff --check passed for the complete base-to-candidate diff.

An earlier pre-final Unity attempt failed and is not treated as passing evidence. The exact committed candidate was subsequently validated by the runs above; no claim here relies on the earlier attempt.

## Limits

This is a bounded CORE-F source and owner-integration checkpoint. It does not complete CORE-F, establish detached-host continuation, promote Core canonical, reopen or complete Phase 12, or assert Phase readiness. The candidate worktree's unrelated protected ProjectSettings changes were left untouched.