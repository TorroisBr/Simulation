# Independent review: TRX-EVIDENCE-v1

Date: 2026-10-14

## Exact target and authorities

- Candidate branch: `codex/tools/trx-evidence-v1-private-context-fix`
- Candidate commit: `b7535041a8aeb6e341fe35c6fcc465da48ce313e`
- Candidate tree: `67b595da4c1337f4da29116cb4d3e791ada54d91`
- Candidate parent: `b9c73dd61f09aa95476f39df3784b77ce45c4e31`
- Direct P12 base: `4cc31073f0ec6ba5c718084e4cebd6e914931d5d`
- Current canonical at review: `origin/codex/core/canonical` `ba65e5ad75d8a604d559f3bffb90aa3fb42a4fef`
- Promoted contract: `docs/operations/DOTNET_TRX_EVIDENCE_CONTRACT.md` at `63d3b8352a450b27b9a5f593c4ea166c0343c59e`
- Applicable evidence policy: `docs/operations/VALIDATION_EVIDENCE_POLICY.md` at `58157f9ac7bf573e9a4ed1b2d485a2db190e7c4f`
- Reviewer: independent Codex subagent; the candidate implementation was not edited. This record is on a separate review branch.

The complete cumulative candidate diff from the direct P12 base and the four-line successor delta from `b9c73dd` were inspected. The candidate contract and policy files match the current canonical versions.

## Verdict

**VALIDATED_CANDIDATE — PASS for the exact-tip tooling review.** The prior cold/warm ordering finding is closed. No further code-contract findings remain in the reviewed scope. This verdict does not promote the tool or establish P12-A implementation readiness.

## Review findings

- `Assert-TrxColdWarmAttemptLabels` now requires two attempts and enforces `attempts[0].label == cold` and `attempts[1].label == warm` (`Tools/UnityValidation/TrxValidationEvidence.psm1:360-370`). `Assert-TrxPublicManifest` invokes it for packaging and verification, so archive-only verification checks source order as well as labels.
- The added regression reverses the attempts while their exits and durations remain equal and confirms rejection (`Tools/UnityValidation/tests/TrxValidationEvidenceTests.ps1:154-156`). This closes the ambiguity where equal command facts could not expose swapped report associations.
- The cumulative implementation remains consistent with the promoted closed TRX grammar and projection: native outcomes/counters and result details are preserved; normalized reports are deterministic and the archive is integrity checked; malformed or unsupported source evidence fails closed.
- Source, command, generated dependency, test discovery, report, assembly, and runtime identities remain bound to the private runner/source evidence. The private-context path reconstructs and compares every public claim group and provenance array against that evidence. Protected retention checks validate owner-only ACLs and known copies before reuse; interrupted retention recovery accepts only a complete verified output set and its matching receipt state.
- Public output contains normalized TRX reports only. Private originals, raw logs, absolute local paths, and account-specific data are excluded. The current changes are tooling/tests and documentation; no Unity domain behavior or project runtime files changed.

## Validation

Fresh validation ran against the exact candidate checkout:

- TRX evidence suite: 144 assertions passed.
- Shared evidence packager suite: 64 assertions passed.
- Unity validation runner self-tests: 33 passed, 0 failed.
- `git diff --check 4cc31073f0ec6ba5c718084e4cebd6e914931d5d b7535041a8aeb6e341fe35c6fcc465da48ce313e`: passed.
- The candidate cold/warm package is 13,867 bytes with SHA-256 `540026a1fe878a927bd2ba70c51dc5e03b87526f43f474467f60eb68cb5a43ed`; archive-only verification reports one run, two entries, and `originalComparisonNotPerformed`, as specified.
- Exact-context packaging and verification for this candidate were reported as passing `originalComparisonVerified`; the protected raw inputs were not copied into this review record. The reported public archive hash matches the independently checked candidate archive.

## Integration constraints and remaining evidence gate

The candidate package was produced by this unpromoted tool candidate. It is not the final CORE-A evidence package and must not be treated as P12-A readiness or integration evidence for a promoted tool. The final CORE-A package must be produced and privately compared using the adopted tool tip after canonical integration. This review does not claim canonical promotion or Phase closure.
