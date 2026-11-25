# CORE-F Action Dispatch Projection Boundary Review — 4F5F6BA2

## Verdict

**PASS — VALIDATED CANDIDATE SLICE.** The independent exact-tip review found no actionable issues.

## Identities

- Repository: `https://github.com/TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate commit: `4f5f6ba2399b319e079c3bcc02fc3c5ad70ba14b`
- Candidate tree: `84a4404b8ab140cff417feb3d1a1834fdd5555cf`
- Parent: `992f0f68ab378f444d19e585b4a91f79e36fa4df`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Candidate remote ref matched the candidate commit. A remote `ls-remote` read confirmed canonical remained at the SHA above; refreshing the local canonical tracking ref was blocked because Git could not write this worktree's `FETCH_HEAD`.

## Reviewed boundary

Core no longer asks the host whether Unity's `NpcActionRuntime` projection matches the selected action. Core still validates the exact selected owner graph and definitions, and dispatch reads the exact action and payload from `DailyV1NpcActionSlotOwner`. The Unity adapter retains its existing projection check as a private implementation detail and performs it before constructing the Unity dispatch port. This preserves Unity's mismatch rejection while allowing another host to provide a dispatch port without implementing a Unity projection predicate.

The test fixture provides that port directly from Core owners and has no Unity `NpcRuntime` or `NpcActionRuntime`. Existing action ordering, slot revision, decision identity, result handling, status planning, and RNG assertions remain intact. This slice does not claim that the Unity host is already projection-free: its dispatch and status adapters still use Unity runtime projections.

## Validation evidence

- Official CoreValidation: PASS, cold and warm, 680/680. Run ID `e7c951a13fb74a9d8766c7547fa50b66`; working-tree input digest `746def53e56960bf2d845b9e17065a463c309fbfe1474ee15479508879019ea1`; input stability passed. The only post-run source adjustment before commit was making the Unity projection checker private; that Unity source was then covered by the exact Unity run below.
- Official Unity EditMode `ActorChoiceRuntimeTests`: PASS, 11/11, zero failed/skipped/inconclusive; run ID `6f30cc03999949a0b55f74b1c8ec6d34`; Unity input stability passed. Result XML SHA-256: `d6e8bd4e1d6a885e0a19f65482ab738a55fccfb68c446c1cbc5a30f90d1939e2`.
- `git diff --check`: PASS before commit.
- Validation ran with the three pre-existing ProjectSettings changes untouched. They were excluded from the candidate commit.

## Independent review

The independent reviewer inspected the committed diff from parent `992f0f68` to candidate `4f5f6ba2399b319e079c3bcc02fc3c5ad70ba14b`, confirmed that the Unity exact-projection guard remains before adapter construction, and confirmed Core's exact owner/action identity checks remain. Test changes did not weaken assertions. Verdict: **PASS; no actionable findings.**

## Scope limits and next integration

This is an incremental source-boundary candidate, not CORE-F completion or canonical promotion. `DailyV1Application.TryStart`, one Core-owned active session/runtime, complete shared P12 capture and detached restoration, descriptor-v2 end-to-end persistence, the fixed .NET host, and separate-process T/T+1/T+2 continuation remain outstanding. No full Unity EditMode, Smoke, long-run, or complete package acceptance is claimed by this slice.
