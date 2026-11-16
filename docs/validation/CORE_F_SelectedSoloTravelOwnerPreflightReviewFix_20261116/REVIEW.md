# Independent review — selected solo-travel owner preflight

## Verdict

**VALIDATED_CANDIDATE — PASS.** The implementation review found the selected Core-owned solo-travel boundary fail-closed and consistent with the approved authority split. Exact-tip applicability was separately confirmed after the implementation commit.

## Reviewed identity

- Repository: `TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Candidate commit: `e8ba6dbb175747fa2cff0ef5f37ccbd901f6c580`
- Candidate tree: `b03cad617f64d1941484d0302d67a58b97ce62df`
- Reviewed base: `3091b4bd7b5cd3b0668555aa160ee3c22409cfc4`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Diff scope: five implementation/test files listed in the candidate commit.

## Findings

- The Core boundary verifies the active session and mutation permission before projection work, then verifies the exact actor owner graph and selected travel facts before charging.
- The Unity-side preflight is restricted to the bound actor and projection revision capacity needed to apply the travel-presence transition. The legacy host-domain predicate remains on the non-Core path.
- The charge and the existing admission/projection callbacks retain their established order. The new boundary test verifies that Core-selected travel does not invoke the legacy domain predicate and fails closed when the Core mutation guard is faulted.
- No actionable authority, ordering, stale-state, or scope issue remained.

## Exact-tip and validation applicability

The official CoreValidation run `8fc44c121c894d7a89687425d6b60dbe` passed cold and warm at `1136/1136`, with zero failures, skips, inconclusive results, errors, aborts, or timeouts. Its private manifest SHA-256 is `9f6686207ad83b6da7736107ebdfa243c79cc1cf20d3345cf069c7be7dae4d80`. The manifest records the parent commit/tree as the runner Git identity; its working-tree digest equals the input digest, and all three changed Core/test paths match the candidate commit byte-for-byte. The two added regression tests passed in both runs.

The official Unity EditMode run `0c4a3ea0379e4b3da76a2936cc0677d3` passed `3321/3321`. Its input inventory binds the three Unity-side changed paths to the same bytes as the candidate. The result-only package is recorded in `Unity/coref-solo-travel-owner-preflight-reviewfix-editmode-20261116.manifest.json` and `.zip`; official package verification passed. Five Smoke-designated cases were included in the full EditMode run and passed. A separate Smoke-only attempt timed out without XML and is not represented as a passing run.

Thus the executable source/test identities covered by the applicable Core and Unity inventories are byte-identical to candidate `e8ba6dbb`. The earlier parent/tree metadata is retained and disclosed rather than rewritten.

## Evidence boundary

The original Core manifest and TRX files remain in the protected local evidence store and were independently inspected by run ID and manifest digest. The default CoreValidation run includes additional Lab gates, which the closed TRX packaging profile does not accept; no raw TRX was altered or repackaged. Therefore the .NET artifacts remain private and this candidate record is not a permanent .NET evidence package or Core canonical promotion package.

The Unity result-only package is public; its original XML and raw log remain protected locally. Existing unrelated ProjectSettings changes and validation documents were excluded from the candidate and left untouched.

## Review record

Independent reviewer: selected-travel protocol reviewer. The reviewer made no candidate edits and ran no tests. The final exact-tip applicability addendum returned **PASS**, confirming all five changed paths against the applicable Core and Unity input inventories. No canonical promotion is claimed; CORE-F remains incomplete.
