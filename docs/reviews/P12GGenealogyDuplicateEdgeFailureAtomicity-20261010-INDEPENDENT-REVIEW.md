# P12-G Genealogy duplicate-edge partial-stage rejection — exact-tip independent review

**Verdict:** PASS — bounded implementation candidate
**Review date:** 2026-10-10
**Candidate branch:** `codex/phase12/P12GGenealogyDuplicateEdgeFailureAtomicity`
**Candidate tip:** `2e94e65460729f4500c73ca573a5d3765e2091e5`
**Base and current P12 canonical:** `ded30aee3c8ccbc28257103a6af04fda99b0da3f`
**Implementation commit:** `728365f836abd61ed6e884a7e756b85e16c7ba15`
**Implementation Git tree:** `b2472f81d81dac51740bfe3c649a9d4f50c35d25`
**Validated Assets tree:** `e801be2441e566bd51bdd328e68ff13ca3eafcdc`

## Review findings

The candidate is a clean two-commit fast-forward from the stated canonical base. The implementation commit is directly based on `ded30aee`; its descendant candidate commit adds only scoped validation evidence. The candidate tip and implementation commit have the same `Assets` tree. After refreshing origin, canonical still resolves to the base and the remote candidate tip remains `2e94e654`. No implementation code changed after validation or exact-tip review.

The test seam is internal, null by default, and threaded only through an explicitly named restore-for-test path to the private Genealogy snapshot-to-hydrator boundary. Normal restore passes no override, so the production path retains its prior behavior. The candidate does not add public API or broaden the P12-G profile contract.

The test preserves schema v1, revision 2, and row count 2 while substituting two duplicate `A→B` edges. It confirms the transform occurs, exactly one edge is privately staged, and the factory rejects before session publication. The shared assertions preserve the source session, token, health, facts, roots, and included-owner projection; verify continuation parity against the uninterrupted control; then verify valid retry and a later advance. This is one bounded false-return partial-stage case, not complete failure-atomicity coverage.

The focused XML was directly inspected and reports 177/177 with zero failures, skips, or inconclusive tests; the named new test passes. The official Smoke XML was directly inspected and reports 5/5 pass. The retained validation manifest ties the ALL EditMode result (2851/2851), `git diff --check` PASS, and recorded artifact hashes to implementation commit `728365f` and Assets tree `e801be`. The reviewer could not retrieve the large ALL EditMode XML or independently recompute the compressed-log hashes through the review connector. No mismatch was observed in the retained manifest. No Unity tests were rerun during review.

This review validates only the duplicate Genealogy false-return case. Other owner hydrator, validator, relation/commitment, publication, no-replay, and multi-boundary continuation obligations remain open. P12-G and P12-A remain `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains `OPEN`. No complete owner/epoch coverage, global quiescence, capture eligibility, export/hydration readiness, downstream readiness, or Phase closure is implied. Canonical promotion is not performed by this review.
