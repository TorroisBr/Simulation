# Privacy-safe .NET TRX evidence — independent review

**Date:** 2026-10-13. **Final verdict:** PASS / READY_FOR_IMPLEMENTATION for
TRX-EVIDENCE-v1 after architecture/documentation promotion. This approves a
bounded Tools handoff, not an implemented adapter or CORE-A canonical promotion.

## Review identity and exact scope

- Canonical architecture base: `32358d2a0faf511e098c4f295a5132a4bfb8cfb2`.
- Initial content candidate: `2bd7d20c9240e0acf033e75038ce78e3030b6558`.
- Corrected candidate: `245ec63a3fad7da271d6c0fa8d53e3cc6fbc8402`.
- Final reviewed content: `1ca79d2e781dc6463ea6f0ffba50268d9d4ff3f6`.
- Actual CORE-A candidate inspected:
  `74677a0b5b74c7814874dd2d2b11aead2891bc42`.
- Existing packager source: P12 `4cc31073f0ec6ba5c718084e4cebd6e914931d5d`.
- Independent source/evidence auditor and technical reviewer: `trx_evidence_audit`.
- Independent architecture/technical reviewer: `trx_contract_review`.
- Author/recorder: General Architect. Neither reviewer authored candidate files;
  reviews and original-file inspection were read-only. This record faithfully
  transcribes their findings, rather than substituting author self-approval.

The cumulative content diff has 11 documentation/Skills paths, no runtime,
Assets/Packages/ProjectSettings or production Tools changes. `git diff --check`
passed. No Unity/.NET test execution or tool implementation is claimed.
Final review/State-only successors require independent exact-tip applicability;
reviewed content and promotion marker SHAs remain separate to avoid a SHA loop.

## Findings resolved before PASS

| Severity | Finding | Resolution |
|---|---|---|
| MAJOR | Public manifest whitelist/argument transformation and private-to-public provenance were underspecified. | Closed field/container schema, exact field derivation, opaque handle syntax, argv rules and source/assembly/capture-versus-recovery mapping added. |
| MAJOR | Relative dotnet project arguments lacked explicit command cwd binding. | Mandatory workingDirectory field; CORE-A cwd is recovered from unchanged runner/relative args, retained logs, all three captured-hash project.assets.json paths and original TRX targets; never inferred from ProjectRoot or success alone. |
| MAJOR | Secure new copy did not cure access on the existing broad-inherited original. | Scoped safe ACL correction for the exact dedicated owned run directory/files, all known retained copies verified, no byte edits or active-worktree permissions changes. An unresolved original copy remains blocking. |
| MINOR | Explicit producer identity/provenance was missing from generated context/manifest. | Required VSTest 17.14.1/SDK/logger binary identity; captured log facts and recovered binary bytes are field-specifically distinguished. Test SDK 17.12.0 is not treated as logger version. |
| MINOR | Private manifest hash/length wording conflicted with public linkage. | Manifest bytes remain private; hash/length are public commitments, not availability/authenticity proof. |

No remaining blocker or major finding at the final content tip. No product or
simulation-architecture choice is required for the bounded tool implementation.

## Evidence checked independently

The current corrected CORE-A run `1aa63d7e04854212be037b09d299fccd` has two
61,701-byte original reports, 45 definitions/entries/results each, all Passed,
Completed summary and coherent actual producer counters. The original/private
manifest and archive hashes match their recorded identities. Each report has
one nonempty summary StdOut with a single absolute path under the original
worktree root; no case failure payload or attachment appears in these passing
samples. The closed projection fits these actual reports, not imagined clean XML.

The original evidence folder/files inherit broader ACLs and do not meet the
existing owner-only protection check. This is a required remediation, not a
finding that disclosure occurred. Raw private metadata, paths and ACL identities
are not reproduced in this record or outgoing artifacts.

The original SDK worktree remains at `f725fa87...`, clean, with 27/27 captured
input identities matching. Both original TRX target sets resolve there; retained
Core/test DLL/PDB outputs support the stated recovered assembly identities.
The second reviewer independently verified all three generated dependency file
hashes and resolved project paths, both TRX target sets, the recovered test/Core
DLL hashes, and all 11 retained logs against their manifest records. Recovery
labels do not imply contemporaneous binary hash capture or trusted-execution
cryptographic attestation. Package-time revalidation remains required.

Actual retained logs identify VSTest 17.14.1 with SDK 9.0.315. The tagged upstream
logger/counter source supports `executed = passed + failed` and other native
counters zero. Keeping native counters plus a complete per-case histogram avoids
concealing skipped/not-executed/inconclusive outcomes. Packaging integrity is
distinct from test-gate pass and from original comparison.

## Approved boundaries and next gate

[Contract and exact Master handoff](../operations/DOTNET_TRX_EVIDENCE_CONTRACT.md)
requires one shared packager adapter, closed supported grammar, explicit metadata
projection, immutable case/error payload, protected originals, complete public
normalized reports, honest source/producer/assembly linkage, independent complete
comparison and exact-tip review. Local-only originals or hashes are not sole
permanent evidence. Unsupported/private immutable payload fails closed.

The General Orchestrator may implement the bounded Tools handoff under its active
mandate, run the specified Tools regressions/synthetic and real read-only comparison,
then prepare the actual CORE-A package and final applicability/promotion preflight.
No current sanitized artifact is retrospectively accepted. Existing CORE-A source/
tests and execution gate set remain intact; no rerun solely for documentation or
normalization. Actual execution input/harness/claim changes retain affected gates.
CORE-A remains unpromoted; CORE-B/C retain their own dependency refresh. No P12
scope extension, P13 readiness, Phase reopening or publication bypass.
