# CORE-F Selected NPC Death Owner Boundary — Independent Review

## Verdict

`VALIDATED_CANDIDATE`. The independent exact-tip review found no actionable code, test-integrity, scope, or validation findings.

## Identities

- Candidate branch: `codex/core/CORE-F-runtime-app-session-boundary-20261025`
- Base: `04f998f10457b11d88b434b798a8b4308354705a`
- Reviewed implementation/evidence candidate: `31276ec84e7dc17910b62f901b3a722273f6070e`
- Candidate tree: `f06fe977d301a95ba41f68e6d4f771635ec99be1`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Review was performed by an independent reviewer who did not author or modify the candidate.

## Review findings

The selected Core lifecycle/action-owner commit preflights the exact admitted action and payload identities, local revision capacity, and owner state before mutation. It commits the selected NPC life-state transition and clears the current action in their existing Core owners, advancing the life revision once and the action revision only when an action is cleared. The Unity layer continues to own its established P12 admission callback and compatibility projection. Added tests cover successful commit, stale action rejection, revision exhaustion, and the no-action case.

No deterministic ordering, persistence identity, P12 operation scope, writer, revision, epoch, or restore contract regression was found. The change remains bounded: it does not move Person death validation, City aggregate population mutation, or the complete cross-owner death transaction into Core. It does not establish P12 readiness, a detached host, or CORE-F completion.

## Validation applicability and evidence

The independent reviewer verified the current candidate tree in a clean detached worktree. Its `ProjectSettings/ProjectSettings.asset` was 21,227 bytes with SHA-256 `3115b79c242b072e603104114f847e0a00656fed78eae4061891afb952c218d5`, matching the stable Unity input inventory. The active implementation worktree has a separate protected local version; it was not part of the candidate commit and was not staged. This resolves the apparent settings mismatch; no test rerun was needed.

- Official CoreValidation cold and warm: 838/838 each, zero failures, skips, or inconclusive results. Run `2c00e059595f45199ab52c5edfa5656b`; input digest `eca959891e26908a4c432f19ea256604b340230cc9bd3335abbe917a46eab5af`.
- Focused P12 population lifecycle invalidation: 20/20. Run `d62ddb0616cc487d9b0ad028d4cb215b`.
- Selected runtime/Unity composition: 39/39. Run `cf3af5aa772e4fc3b1f253f3fbbc4a7e`.
- Full Unity EditMode: 3271/3271, zero failed, skipped, or inconclusive. Run `47f767f0ff8d4d0aa267502737a588b6`; stable input digest `4683f3ccd668c79f535f845a185d92cde53e293c0c43aeea6d3d4b9d83923332`.
- Daily LongRun: 7/7, included in full EditMode.
- Official filtered Smoke: 5/5. Run `a46150e266df4207b1789a6178cfd05a`.
- `git diff --check`: PASS.
- Official evidence package verification: PASS; archive SHA-256 `d7f0dcb14dd5f84d70b7ed9584857424c841ab6f91a510d2d83958db4879ef07`; public manifest SHA-256 `5dc9721a6d2a9085d7e6332269082a32727d27a3238ecedf24356a4c30976f61`.
- Public validation document, manifest, and archive scan: no local workstation-root paths.

The attempted PlayMode filter that discovered zero tests is excluded from evidence; the project's five Smoke cases passed under the correct EditMode filter and in the full EditMode run.

## Integration limits

This is a reviewed candidate checkpoint only. Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`. No canonical promotion or Phase closure is implied. Existing unrelated ProjectSettings modifications, `.validation/`, and prior resident-floor evidence remain outside the candidate payload.