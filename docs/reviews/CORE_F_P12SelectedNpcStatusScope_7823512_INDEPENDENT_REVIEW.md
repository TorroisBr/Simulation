# Independent review — CORE-F selected NPC status scope

**Verdict: VALIDATED_CANDIDATE**

## Reviewed identities

- Repository: TorroisBr/Simulation
- Current Core canonical at review: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Candidate branch: codex/core/CORE-F-runtime-app-session-boundary-20261025
- Exact base and merge base: 64a63a90beff495ccefdee135f1f90e1b6c92399
- Implementation commit: 7823512fade5a0d8c797155446efef40ba5076f3
- Implementation tree: 329b7727bad4b442465ff8f86e598b803b5f070c
- Final candidate: 315a3913da3b1818d688a93d2d1732c311f6014a
- Final candidate tree: be5691cf8f7302b1d9b6338c0fb142022462b72b
- The final candidate adds only validation documentation and its evidence manifest/archive after the implementation commit. The remote candidate and canonical refs matched these expected SHAs at review. The complete base-to-candidate diff was reviewed; git diff --check passed.

## Code review

The change adds a Core-coordinated immediate P12 scope for status-only operations (includeJustice == false) when every distinct, installed participant has its selected Core owner. P12CrimeJusticeInvalidation.cs retains the Unity-side exact runtime/roster admission check and selects this path only for the all-selected case. Missing selected owners continue through the existing compatibility path; Justice-containing operations remain on their existing path. An owner that is selected but is not the exact member of the active Core graph fails closed.

DailyV1P12CrimeJusticeMutationCoordinator.TryBeginSelectedNpcStatusMutation derives the status-section identity and revision reader from each exact graph-installed DailyV1NpcRuntimeOwners instance. It checks graph membership and reference-identical definitions before P12 protocol access, deduplicates repeated owner references, and delegates to the existing immediate admission policy. That policy retains the owner-thread check, no-overlapping-scope rule, unchanged-section preflight, revision-capacity check, and committed-section notification.

The existing leaf-level CanCommit/NotifyCommitted path remains responsible for each actual write; this change does not add a second owner write or a second notification. The empty/null participant and duplicate handling is consistent with the prior action-scope behavior. No action ordering, RNG, daily scheduling, Crime/Justice semantics, or P12 profile scope changed.

The two added Core tests verify ordered section derivation from exact owners, null/duplicate handling, successful notification, and rejection of an equal-ID but distinct owner before protocol access. I found no actionable code defect or unresolved product/architecture decision.

## Validation and artifact review

- CoreValidation run 4f321fa732384dd5bf72068e4b96b97c: cold 825/825 and warm 825/825; both protected TRX result summaries report zero failures. The input snapshot was stable at f5e4467de3d5fea7b2aad2ac6f174a206148606e16b0671dd7f5e0435c2d4291. Its private manifest SHA-256 is 1553db7f366f0c522a788e272f61f487d3b6ebb3b50d7df7de97fe30895f9743. The changed Core coordinator and test source hashes in that manifest match the candidate (fd6c37ac13e7882f1c031c6553b304d84876d5a24e991f4c3563ec5f102d0cd7 and b6279ae6a419d5c4babf8db25242b2b96da95373d303325c9917fc635333109a); the Unity bridge hash also matches the recorded and Unity-snapshot hash (40d65f213e9b9cfba11b80891045ad4a3cf6f2c4589017a7dfab3f89c975ddf5). Cold/warm TRX hashes are bc7d9f219b91026f7389161ff584ebae51d609384fc0ef5105970b0bc1550c0a and 91d1203a7ea29817adcfcdb518f373b34df13f783e682bf01b52e74fd67b0a9d.
- Focused Unity run a11153fd4ed84926b0bc185034996110 passed 1/1. Full EditMode run 797195c8a2d84f3b90f565f578724015 passed 3269/3269, with zero failed, skipped, or inconclusive cases. Both Unity runs used stable input digest a3f497d3ba6904e1d6d4beff778e140fae7e0a615f3d2e6794bf6efac089b150; the changed bridge source hash matches the candidate. The packaged EditMode archive is 3c4ed47c5aa424e82759b44939d20bdfdd55728085238aaa2aa14142e637ad85 and its manifest is 4965d3d49216f41e4f5365264fc9ff80430c5ca5881142fdaead296e6409e905. I verified the one archived result report has 3269 passed and no absolute drive or Windows user paths.
- Official Smoke run 7b340b4d03c248b7a9829d0bce9c1ac0 reports 5/5 passed, zero failed/skipped/inconclusive, on the same stable Unity input digest. Its protected run-manifest SHA-256 is e5dab18e70686bf8db61aee41485ebdebc8a034cbc76d8f67984049be1a4d2f0; original XML and log hashes are 83b1d6e86087af535f18eac6bf1e0b205951920bfc98c3420985093bcefbaff9 and d979015da9bae6f9a300d986f4b3c7657db1f3c11af4bac0dc364c1c32cb14b1. The XML root reports total 5/passed 5/failed 0, but four of its six suite nodes advertise testcasecount values inconsistent with the filtered subtrees. The official packager rejected normalization for this structural mismatch. The unchanged original and log remain protected; there is no normalized Smoke package. The private originals contain local path strings and were not published.
- The CoreValidation raw TRX and logs remain protected. The public validation note accurately says that only hashes/results are recorded and no normalized trx-vstest-flat-v1 source-context package was produced. I inspected the protected TRX summaries and corresponding manifest without copying their contents into this review record.

The committed Unity package manifest contains no absolute filesystem paths. The public evidence therefore does not expose the protected machine paths. The validation note accurately records both packaging limitations; neither has been treated as a successful normalization.

## Integration constraints

This is a reviewed bounded candidate only. It does not complete CORE-F, P12-B, the shared runtime/application, detached restoration, or the .NET host. The Smoke packaging mismatch and missing normalized .NET source-context package remain evidence limitations that must be resolved or accepted under the applicable promotion policy before any canonical promotion. This review is not promotion approval.
