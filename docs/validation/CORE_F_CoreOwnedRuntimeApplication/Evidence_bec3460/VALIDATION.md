# CORE-F initial-genesis publication reentrancy — validation

## Scope and identity

This validates the bounded reentrancy correction on the existing CORE-F candidate. It is not full CORE-F package acceptance or Core canonical promotion.

- Candidate branch: codex/core/CORE-F-core-owned-runtime-application
- Candidate commit: bec3460e15370b6fcdf3afe102ce760dbb983799
- Candidate tree: ac691e79dd486b07a1de65e4ba8e7b70ea03a5cb
- Parent: faf8b03c5f7e1d887c6b1945108430130c3dcc61
- Core canonical: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Validation ran from the E: candidate/validation worktrees. The input snapshots were stable and bound to this commit/tree. The preserved unrelated ProjectSettings changes were left untouched and remain outside the committed candidate.

## Results

| Gate | Result | Exact run/evidence |
|---|---|---|
| Official CoreValidation cold | PASS, 703/703 | Run 7d6d7b26fd834a9896e0de20eba7d3c9; private manifest SHA-256 8d42f554dc638fdef4f553b07224558533ab83122c2ba82dc90daf56fc773d6; cold TRX SHA-256 4f69e3cd007fd75e43b1fe706eaf2deaf7f2dda9c563e2282ca22f28031485b9 |
| Official CoreValidation warm | PASS, 703/703 | Same run; warm TRX SHA-256 27d339709b9b795323982016323fbe8d5e084e060c36b3c10b5a0a2889184d59 |
| Full Unity EditMode | PASS, 3258/3258; 0 failed, skipped, or inconclusive | Run 71437fbe265c4839a1595d4fd7c46822; Unity 6000.3.9f1; stable input snapshot |
| Official filtered Smoke | PASS, 5/5 | Run 8174aee319be4a9b81fa22984cd11f3c; all five Smoke identities are also included in the full EditMode report |
| Unity public package verification | PASS | [Package manifest](EditMode-PlainTemp/coref-bec3460-editmode.manifest.json); [normalized XML archive](EditMode-PlainTemp/coref-bec3460-editmode.zip) |
| git diff-tree --check | PASS | Candidate parent to bec3460 |

CoreValidation protected cold/warm reports and Unity original XML/logs remain in their owner-only E: evidence roots. They were not copied into this public record. The Unity package is the official XML-only normalized package; the official verifier compared it with the protected original run manifest. Its archive SHA-256 is 2294f69bf0ef7cf1afcdff10874c6dd0123962f1a075f39158e5b462cdfc70ff, and its public manifest SHA-256 is ac4af693606ba765c977d4881b8ae8c68930a9d0de9195641c3ff42fd5cc0dd1. A targeted scan found no workstation/home paths or credential patterns in the public manifest.

An initial Unity attempt using the owner-only evidence directory as TEMP/TMP produced five P12 storage failures. That run is retained as a diagnostic and is not used as passing evidence. The runner was then executed with temporary storage outside the protected evidence directory; the full exact-source run passed 3258/3258. A later packaging attempt failed because Unity had removed its temporary subdirectory, so packaging was completed with a stable E:-local temporary directory and verified successfully. No production or test source changed between these attempts.

## Review and limits

Independent exact-tip review passed with no actionable findings for commit bec3460e15370b6fcdf3afe102ce760dbb983799, tree ac691e79dd486b07a1de65e4ba8e7b70ea03a5cb. See [the review record](../../../reviews/CORE_F_DailyV1GenesisPreparation_bec3460_REVIEW.md).

This evidence closes only the validation/review boundary for the initial-genesis publication reentrancy correction. It does not establish the full shared Daily-v1 runtime, detached P12 restoration, fixed .NET host, T/T+1/T+2 continuation, or CORE-F completion. No Core canonical ref was changed.
