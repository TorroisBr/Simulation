# CORE-F Core-Bound Census Projection — Exact-Tip Review

Date: 2026-11-09

## Reviewed identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Exact candidate: `42c033d27109a9529b03ea87ab0754dccba43315`
- Candidate tree: `55a39d6bea3a1e2a503bd457b5bb0b46070f1ef4`
- Exact base: `f1340fdc2d9abcbba41de03236b12ee1259b352a`
- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Candidate branch is synchronized to the exact tip; Core canonical is unchanged.

This review covers the complete three-file delta from the exact base through the candidate, including the implementation at `e42bd26922b3481f9b5c38589f8fa52fb793913f` and its test-only follow-up. It supersedes the earlier `NEEDS_CHANGES` feedback on the epoch-order test. The preceding host-remainder helper review remains applicable to the helper already present at the stated base.

## Verdict

**VALIDATED_CANDIDATE.** The full delta was inspected against the actual base. The epoch-order test now uses an input that makes projection fail, so it proves the epoch mismatch is classified first. No actionable findings remain.

## Review result

The Unity `SimulationRuntime` census callback now returns the raw quiescent owner snapshot. `DailyV1ExecutionBoundaryState.TryCaptureBoundOwnerSectionCensus` snapshots the bound census source and the exact selected owner-graph reference under its operation lock, invokes the host source, and compares its reported mutation epoch with the current epoch before projecting any Core-owned rows. Only after epoch acceptance does Core create the host-owned remainder using the selected graph. A projection failure returns `OwnerCoverageIncomplete`; an epoch mismatch returns `OperationInProgress` with null owner output and zero output epoch.

The exact-NPC and exact RuntimeIdAllocator remainder logic is unchanged from the reviewed helper: selected NPC rows are omitted only for a complete matching owner fragment; allocator rows are omitted only as a matching three-row set. Stale, partial, duplicate, or mixed-roster facts retain the existing rejection or host-compatibility behavior. The Core P12 composition path reconstitutes Core-owned rows before its existing complete-vector gates. The Unity adapter no longer performs a second filtering pass.

The updated test appends a duplicate owner-section identity to an otherwise complete vector and reports a wrong mutation epoch. That vector would make remainder projection fail with `OwnerCoverageIncomplete`; the asserted `OperationInProgress` result therefore discriminates the required epoch-before-projection order. The companion test proves Core performs the projection and preserves the retained snapshot object identities. Existing helper tests cover exact rows, stale rows, and mixed-roster fallback.

This change does not alter the 89-family P12 keys, roles, cardinalities, persisted image, or profile. It moves the already bounded host-remainder selection into Core after epoch validation. It does not claim all 89 families are Core-owned, P12 readiness, or CORE-F completion.

## Validation evidence verified

The protected run manifests were inspected. Raw XML and logs remain private; this record includes only run identities, counts, and artifact/source hashes.

- CoreValidation run `e55616a5f47a40e18420f5b98aaed164`: **1,076/1,076 cold and 1,076/1,076 warm PASS**. Stable input digest: `ec478cab2bf9f57e71a448d5ba4c15f40662e4b503c8aefa8af4008ea89148f9`. Cold TRX SHA-256: `1b955364e6928afc5f968f7384428c6659abd1a031cbcd923b26a0e744243d4d`; warm TRX SHA-256: `80a8c8fed70c48bae23449d484cc0a0345d7fc03ce7fe13159c9f8970a21cbb1`.
- Unity EditMode run `5615a611a7fc41bb8aa08bdce973133d`: **3,303/3,303 PASS**. Stable input snapshot SHA-256: `8780ba7ac369d3012da9807757f32d56cf59c216d89f65e68c1525bc459ff626`; XML SHA-256: `e73a9494afe2df1e7505debadc440722004eb208d13f3a1d384aa1c6b3ef4448`; log SHA-256: `aef3d7ab30ca394975da9f9c44fcffb3ee3e64e605ae7ca2b24d06e5f8e03bed`.
- Official Smoke run `35184580008346af9f0a3ce5e685a48f`: **5/5 PASS**, with the same stable Unity input inventory. XML SHA-256: `f4e7197b963aceea3ce9611737aa38c999855e941c2decdedf434dc871f82c4e`; log SHA-256: `440d4ddb5ddb17806cecae7e50928cdae73efb59b96542c05ce971d44d095e7b`.
- Final source SHA-256 values: `DailyV1ExecutionBoundaryState.cs` `2066875429c71ce7a1b1932da45f3e34ec4200ed17136ad9fe7d6d06e3454dc3`; `SimulationRuntime.cs` `65dd9f58f67625d9585c84957c31a1771de32c42e6be553d7a7be627c8475455`; `DailyV1ApplicationSessionGateTests.cs` `4644f61d1ae0557aabd15ed929ed570fcda5ee9c504cf6770fbba09166d77e89`.
- The Unity runner's input inventory is unchanged between its before/after snapshots and the candidate's only post-Unity change is the CoreValidation test source. The exact Unity production-source hashes match the final candidate.
- `git diff --check` from the exact base to candidate: PASS.

The CoreValidation runner reports that this worktree contains a Unity Library; this evidence does not claim Library-free .NET validation. The reviewer made no candidate edits and did not rerun tests.
