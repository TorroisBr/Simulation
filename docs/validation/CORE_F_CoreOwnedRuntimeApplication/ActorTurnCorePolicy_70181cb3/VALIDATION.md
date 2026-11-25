# CORE-F Core-owned actor-turn policy boundary — validation

- Candidate: `codex/core/CORE-F-core-owned-runtime-application` at `70181cb3a2ee8a774365a2de7bd831a8f50f3130`.
- Candidate tree: `414e871b125401bc0e249f40bebc4acf979ad436`.
- Parent: reviewed candidate `e0ee2bf1d0602345c4d3db587da6dfcc00faf6d5`.
- Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

## Validation

| Gate | Result | Evidence |
|---|---|---|
| CoreValidation | **993/993 cold and 993/993 warm PASS** | Official run `38637d6bcb6e4c108e47b0cbcad4d4c0`; native protected manifest SHA-256 `04a3348eed37d8b717e66ea74143c6c457335bed1faa371bf774eaee7ef1684a`. The runner reported `LibraryAbsent=false`; this confirms Core build/tests in this run, but does not claim a Library-free .NET host. |
| Full Unity EditMode | **3296/3296 PASS** | Unity 6000.3.9f1; run `435ad508364e44799c672c4c6c4e83ce`; `inputStable=true`; source commit/tree match above; input digest `767ad630f5549cbebecff36cb7a62d168d2f9affa3fc92de7d58eb3d22fabd27`. Original XML SHA-256 `5f76e9187755d560f6ef1795a409e0eee8113505bbd458e1e289120e750f3212`; normalized XML SHA-256 `471d443a7212bda82d9bc33982d0fb25f70e899d58ba96be64af6174bdd75f21`. |
| Official Smoke filter | **5/5 PASS** | Run `f75442e3f1d3423da96c1e97aaaf8787`; `inputStable=true`; private run-manifest SHA-256 `9d0ea667f65e3ae082ef3d3c8afce4f5b8febc4d934878c4a2508a3f0791a808`. |
| LongRun | **7/7 PASS** | `SimulationRuntimeLongRunTests`; run `3b3e3898f9af4bebadefa88d89f46512`; private run-manifest SHA-256 `2ce497a6c26bc0f4eb5866cdce2b58783951ee37aa29f3966d1a73b0e85d64b1`. |
| `git diff --check` | **PASS** | Checked against the candidate parent. |

The result-only Unity package is [coref-actor-turn-full-editmode.zip](coref-actor-turn-full-editmode.zip), SHA-256 `96e73619bbdbb6aba4e0b162eea9b3c26227c4338ab17f863b899c1a221033f7`, 174061 bytes. Its manifest is [coref-actor-turn-full-editmode.manifest.json](coref-actor-turn-full-editmode.manifest.json), SHA-256 `e8553b6dc7adc71b30e5688b9b6efc332025707cd508c6cfdf25d89b38d3a678`. The official verifier returned `VALID: runs=1 entries=1` with the matching archive hash.

Unity changed two ProjectSettings files on the first disposable-worktree import. That first test run passed but is excluded because `inputStable=false`; its original evidence remains private and unchanged. For the successful run, only the disposable validation worktree received copies of the three already-existing local ProjectSettings files from the candidate worktree. Their hashes matched before test start; the successful wrapper recorded a stable input snapshot. The candidate worktree's protected ProjectSettings files were not changed by this procedure.

The CoreValidation runner uses its native evidence schema, which is not accepted by the current DotNet TRX evidence packager without the separately approved source-context evidence contract. Its native manifest and original reports remain in protected local evidence; no public .NET package is claimed.

## Scope limit

This validates the Core-created admitted actor-turn policy boundary and its Unity effect/P12 ports. It does not complete `DailyV1Application.TryStart`, detached restoration, the fixed .NET host, or T/T+1/T+2 continuation, and it does not promote Core canonical.
