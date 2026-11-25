# CORE-F Unity admission-context adapter recovery review

## Candidate identity

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate commit: `28f23cf9f67a58aa48b88e018d0f925a0b797b3d`
- Parent: `c11aa4e6f6d0b455993786db57e9233caaca0405`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Changed files: `TesteSimulacao.cs`,
  `SimulationBootstrapCompositionTests.cs`, and
  `SimulationRuntimeAdmissionTests.cs`.

## Independent exact-tip review

**Verdict: PASS for the scoped adapter change.** The Unity adapter no longer
stores duplicate bootstrap thread and thread-ID fields. Existing owner-thread
checks use the same immutable `SimulationRuntimeAdmissionContext` captured by
the Core application. The EditMode fixtures now configure that context, and
the Start test continues to assert both the exact `Thread` reference and
managed thread ID. No stale references to the removed fields remain. Review
confirmed the exact parent, patch identity, and worktree-to-commit match; no
actionable findings remain.

This is a recovery checkpoint within the larger CORE-F implementation. It does
not complete the shared runtime/host contract or authorize canonical promotion.

## Validation status

- `git diff --check`: **PASS**.
- CoreValidation: **not rerun for this adapter/test-only commit**; the parent
  code tree has recorded CoreValidation 687/687 cold and warm PASS at run
  `cf386688d00c436cbafb9a41b1ed9228`, and no Core source or Core test changed
  in this commit.
- Unity EditMode: **not run for this commit**. The known Licensing Client
  restriction remains; prior attempts produced no test XML, and no Unity pass
  is claimed.
