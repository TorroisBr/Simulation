# CORE-F Runtime/Host Integration candidate review — e65b8803

**Verdict:** `NEEDS_CHANGES` for the approved full CORE-F application/host integration. The candidate is retained as a validated recovery checkpoint; it is not ready for canonical promotion or full-package acceptance.

**Independent reviewer:** read-only review agent `action_execution_core_review`, separate from the candidate author.

## Reviewed identity

- Candidate branch: `codex/core/CORE-F-runtime-host-integration`.
- Exact candidate: `e65b8803d62f7cf66b42c5c94562841affd42093`.
- Candidate tree: `884c808bef704613f31a706d0ff5217a6f93f002`.
- Candidate parent: `7053361e5efad5fe97fa1ea52d7a694e33e4ceab`.
- Actual merge base and current remote Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- The reviewer inspected the complete candidate diff from the actual merge base, not only the checkpoint commit. The candidate and remote branch matched at review time.

## Review result

The candidate contains a real Core-owned selected-day execution stage and preserves a validated partial integration. The reviewer established no defect in that moved stage. The full approved CORE-F package remains incomplete for these contractual reasons:

1. **Application composition and lifecycle are missing.** `DailyV1ApplicationHost` is only a ports holder, `DailyV1GenesisComposer` returns an internal construction value, and the designed public `DailyV1Application.TryStart` does not exist. Unity's `SimulationRuntime` still owns its public daily advance entry and execution-boundary context creation.
2. **P12 capture and restoration are still Unity-owned.** The P12-G restore coordinator and continuation codec/store remain under Unity `Assets`; no Core application composes the Core-owned continuation lifecycle or the descriptor-v2 store path.
3. **The executable detached host and continuation proof are missing.** `Simulation.DailyV1.DotNetHostIdentity` is a `net8.0` library without an executable entry point. There is no fresh-process Daily-v1 T/T+1/T+2 continuation or Unity/.NET parity evidence.

These are missing required integration capabilities, not reasons to discard the candidate or recreate its completed work. Do not claim CORE-F completion or promote this candidate as a complete package.

## Evidence checked

- The protected CoreValidation run manifest matches SHA-256 `57f2cde830a9cb0781b9cfc1da5827f1c2f53d5289232f5ffbe1e4dd83125dc8`; cold and warm runs each report 640/640 passed. All 145 recorded source-input hashes match the candidate files.
- The sanitized Unity manifest/archive hashes match the candidate validation record; all 1,846 captured input hashes match the reviewed worktree. This inventory includes the separately disclosed protected ProjectSettings values, which are excluded from the candidate commit. The normalized report contains 3,254 test results, all passed, and no absolute local paths. The separate official Smoke run passed 5/5.
- No tests were run by the reviewer. The review verified retained run/package identities and result contents.
- The reviewer left unrelated ProjectSettings edits, private evidence, untracked files, and generated outputs untouched.

## Required continuation

Continue the same serialized integration through the exact approved shared application/session API and `TryStart`; move and wire the existing P12 capture, descriptor, store, staging, restore, and publication path into Core without duplicating authorities; then provide the fixed executable .NET host and separate-process T/T+1/T+2 continuation plus required Unity/.NET parity. Revalidate and obtain a fresh exact-tip review at the complete integration boundary. This review record does not authorize canonical promotion.
