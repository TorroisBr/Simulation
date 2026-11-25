# CORE-F selected Crime/Justice daily stage — revalidation

**Status:** validated candidate; not promoted and not canonical. Full CORE-F remains incomplete.

## Source identity

- Base: `2c11ef7b772e34207f9d7f1592f91e1b08746e66`.
- Candidate implementation commit: `1a5f22870495f84dec19e6539274d6214f17b01e`.
- Implementation commit tree: `7d081e2cad35a139f3a6ea758f181f6d51961a54`.
- Unity runs were captured immediately before that commit from the same effective source bytes. Their input snapshots were stable. The later commit recorded those exact files; no source changed afterward.
- Canonical Core remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

## Bounded change

The candidate executes selected Daily-v1 Crime/Justice start-of-day work against the exact shared owner graph. It preserves the existing four separately admitted P12 scopes and their Justice → Crime → Justice → Justice order. The established Unity lifecycle still handles the entire stage when any roster member is unselected; this is compatibility behavior, not a claim that Core executes a mixed roster.

The candidate does not complete Core-owned daily execution, the shared application, detached restoration, or the .NET host. It does not change the Daily-v1 profile or P12 persistence contract.

## Findings addressed

- The mixed-roster test uses public `TryAdvanceDay` and verifies that both selected and unselected NPC hidden timers and P12 owner revisions advance through the existing Unity fallback. Its assertion and runtime comment identify the whole-stage fallback as compatibility behavior.
- Core tests cover the active-sentence decrement, resolved-warrant release, sentence cleanup when the actor is no longer arrested, release of an arrested actor without an active sentence, subsystem-specific scopes, and no-op revision behavior.
- A selected-profile integration test creates the warrant and arrest through the actual P12 action scope, then invokes the selected daily stage under the real `runtime.advance-day` admission operation. It verifies the Justice owner revision advances twice for the separate arrest-marker reset and sentence decrement; the hidden-state NPC revision advances once; the target revision remains unchanged during wanted-status no-op synchronization; the mutation epoch advances exactly three times; and the census remains valid.
- Unresolved Justice references are rejected before any Core daily scope or mutation. This matches the selected-profile graph invariant: `DailyV1SelectedRuntimeOwnerGraph.TryCreate` checks every wanted target and City and every sentence-to-warrant relation; `TryCreateWithoutNpc` reuses that validation, so unregistering a referenced selected NPC cannot publish a dangling graph. P12-E Justice snapshot/restore validation also rejects dangling selected-owner links. Mixed or unselected rosters remain on the Unity fallback and do not enter this Core validation path.

## Validation evidence

### .NET CoreValidation

The official pinned CoreValidation run completed before the Unity-only test addition. Its explicit 145-file input set was stable and included the selected Core stage, its nine tests, both project compile lists, and the CoreValidation inventory update. Those inputs are unchanged in the implementation commit; the later edit was only to `SimulationRuntimeAdmissionTests.cs`, which is not an input to the .NET runner. The blocked SDK operation was not retried.

- Run: `130b424318894ef79ae37239029835a4`.
- CoreValidation manifest SHA-256: `890016c88a17b68ada81906a2ddcdecb7f3201ed9d6d8c45beb6f9b40645fa3f`.
- Input digest, stable: `43d21f3ae8ff4c7743d82a6b3a1349d1ac25933c3b66a9bb23bfe6f8eb802aee`.
- Cold: 576/576 passed, 0 failed. Warm: 576/576 passed, 0 failed.
- The selected Crime/Justice Core tests were independently matched by test identity in both original TRX reports: 9/9 passed cold and 9/9 passed warm.
- Original TRX and command logs remain in the protected local evidence store; they are not copied into this public package.

### Unity

All runs used Unity `6000.3.9f1`, Test Framework `1.6.0`, the official Unity validation wrapper, and stable before/after input snapshots.

| Run | Result |
|---|---:|
| Selected actual P12 scope integration | 1/1 passed; run `f653609e7f4b4591a06defe06955ad6e` |
| `SimulationRuntimeAdmissionTests` | 216/216 passed within ALL EditMode; the added P12 case is present and passed |
| `SimulationBootstrapCompositionTests` | 38/38 passed within ALL EditMode |
| ALL EditMode | 3,246/3,246 passed; run `3c8be15d4cfe4fbdb9c06f7706144782` |
| Official Smoke filter | 5/5 passed; run `deeab388ce964b54bd0f503337396415` |

The complete ALL EditMode report contains the focused integration case and all five Smoke identities. The strict evidence packager rejects truncated filtered reports because their parent-suite counts do not match, so only the complete report is packaged.

- Package manifest: [`revalidation-20261021-p12scopes/coref-crime-justice-p12scope-editmode-all-20261021.manifest.json`](revalidation-20261021-p12scopes/coref-crime-justice-p12scope-editmode-all-20261021.manifest.json), SHA-256 `6f787dfb78851443878fb63cc80121bd11fc6b5fe4f44a69d11d4f7db86ed8d40`.
- Normalized NUnit archive: [`revalidation-20261021-p12scopes/coref-crime-justice-p12scope-editmode-all-20261021.zip`](revalidation-20261021-p12scopes/coref-crime-justice-p12scope-editmode-all-20261021.zip), SHA-256 `2eac76373d458987ef853974ae68c45d4307b736ed254d6134fedaa418169891`.
- Packager verification: `VALID`, 1 run, 1 archive entry; verification used the protected original run manifest.

`git diff --check` passed after implementation. The implementation commit includes only the eight intended source, test, and Core input-inventory files. Unrelated ProjectSettings edits, other untracked files, and older evidence artifacts remain untouched.

## Review and promotion state

Independent exact-tip review is pending on the candidate commit. This document records validation only. No Core-canonical promotion or CORE-F completion is claimed.
