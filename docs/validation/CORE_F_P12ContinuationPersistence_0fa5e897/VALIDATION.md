# CORE-F Core-owned P12 continuation persistence

**Disposition: validated candidate slice only.** This advances the Core-bound save route after P12-G has produced an image. CORE-F remains incomplete and unpromoted; Phase 12/13 status is unchanged.

## Candidate identity

- Candidate branch: `codex/core/CORE-F-runtime-app-session-boundary-20261025`.
- Code commit/tree: `0fa5e8970b67abae16061ad0fc69a78d15602f69` / `1926ec004537f8e53228665dbb1b28243cbcfaec`.
- Code base: `422d54e016bbd323c402a5f2c7ea4845fa9ab260`.
- Current Core canonical at validation: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Changed source: `DailyV1Application` now verifies the exact active Core save operation, revalidates its completed-boundary witness around census/encoding, and dispatches to the existing v1 or .NET-v2 atomic store. `DailyV1P12GContinuationSaveOperation` exposes this operation-bound route. The selected Unity path delegates persistence to Core; the legacy path with no Core operation keeps its previous census/encode/store implementation.

## Validation

| Gate | Result | Evidence |
|---|---:|---|
| CoreValidation | 820/820 cold and 820/820 warm | Run `8a663653376f4322bc75cd240717982e`, PASS. The official manifest records stable compile inputs and confirms the modified Core file hashes. The worktree contained a Unity `Library`; this result is not a Library-free proof. |
| Persisted save/restore/continuation regression | 1/1 | Run `a90ba1a012ef4c8fabb4e591cf7038b3`, PASS. |
| Core save-operation invalidation regression | 3/3 | Run `839bd2f6da974477a865524ed98febce`, PASS. |
| Full Unity EditMode | 3269/3269 | Run `3ede93fd8a954f0592ecc69500458c4a`, exact candidate commit/tree, stable input snapshot, no failures/skips/inconclusive cases. |
| Official Smoke (`TestFilter Smoke`) | 5/5 | Run `1045810aeb6f402ead6d2205a4901d5e`, exact candidate commit/tree, stable input snapshot, no failures/skips/inconclusive cases. |
| `git diff --check` | PASS | Exact code candidate. |

The two focused Unity runs were first executed while the same source bytes were uncommitted at base commit `422d54e`; the official input snapshots were stable and their hashes for all three changed source files match code commit `0fa5e897`. The full EditMode and official Smoke runs then passed against committed candidate `0fa5e897` itself.

A filtered XML package attempt was rejected by the approved packager because the parent `Simulation` suite advertises the full-suite case count while the filtered XML contains only selected descendants. Those protected originals remain preserved and are not presented as packaged evidence. A `TestCategory Smoke` invocation produced zero tests and is excluded; the documented `TestFilter Smoke` invocation above is the valid Smoke evidence.

## Privacy-safe full-suite package

- Normalized full EditMode package manifest: [p12-core-owned-continuation-save-full-editmode.manifest.json](p12-core-owned-continuation-save-full-editmode.manifest.json), SHA-256 `bd4685967309485d0a4705315ea70272bd86b3d01f1e334075ffc9a2e869a9cc`.
- Normalized result archive: [p12-core-owned-continuation-save-full-editmode.zip](p12-core-owned-continuation-save-full-editmode.zip), SHA-256 `c11afab692cbac862eaa18b49d214a7b5d513631b95abefb6ad793f6738d5bae`, 173,093 bytes.
- Official package verification: PASS (`runs=1`, `entries=1`). Original XML and logs remain in protected E: evidence storage; the committed package contains normalized result XML only.

The full Unity runner's before/after snapshot identifies the exact committed candidate and stable input digest. The two pre-existing local `ProjectSettings` changes remained unchanged and outside the candidate commit; the evidence inventory records them as test inputs.

## Independent review and limits

Independent exact-tip review passed; see [INDEPENDENT_REVIEW.md](INDEPENDENT_REVIEW.md). This is only the census/encode/store half of the Core-bound route. The current P12-G image capture, detached source/target staging, graph checks and restored admission remain in the Unity coordinator and must move with their selected owner/session-root closure. This slice does not deliver the final public `DailyV1Application.TrySaveContinuation`, `TryStart`, detached .NET host, process-separated continuation or CORE-F acceptance.