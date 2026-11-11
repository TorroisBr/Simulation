# CORE-F selected actor-action routing — independent review

Result: **PASS — no actionable findings.**

## Identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`.
- Exact implementation commit reviewed: `c8b8420816daa2c091ea2b4259e215de4704d535`.
- Exact implementation tree reviewed: `8f56a4ba1ac9decede2b6cfea50c855e90650283`.
- Exact base: `aef01355dc72ecbf409d19639c80d0ddae715f37`.
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- The reviewer inspected the complete two-file implementation/test diff and confirmed the remote candidate matched the reviewed implementation commit. No unrelated worktree files are part of that diff.

## Findings

The Unity actor-turn adapter opts into the existing Core selected-action stage. For exact selected owners, Core owns action selection, deterministic dispatch, slot admission, decision identity, action outcome, and status processing. The same Unity adapter remains responsible for compatibility projection, effect ports, and P12 admission/publication; no second action engine is introduced.

The exact-owner predicate checks the selected runtime definitions and actor definition identity. The action boundary continues to expose `runtime.randomSource`, which is the same authoritative random source supplied to the selected decision system. Unity's `EvaluateAndExecuteAction` remains available outside the selected Core route.

Mixed-roster fallback selection, the census implementation, original roster ordinals, and null-owner compatibility entries are unchanged. Existing Core tests cover selected choice, slot admission, and dispatch semantics; the new Unity assertion confirms that the production adapter opts in only for the exact selected actor.

No actionable correctness, identity, RNG, P12, or test-integrity finding remains.

## Validation applicability

Validation is recorded in `VALIDATION.md`. The reviewer independently checked the protected run manifests, totals, stable input digest, and source hashes against the reviewed implementation files. The normalized report package was separately verified against the protected full-run manifest. The review did not edit the candidate or rerun validation.

This review accepts only the selected actor-action routing checkpoint. CORE-F remains incomplete; this does not establish public `DailyV1Application.TryStart`, detached .NET continuation, or Core canonical promotion readiness.
