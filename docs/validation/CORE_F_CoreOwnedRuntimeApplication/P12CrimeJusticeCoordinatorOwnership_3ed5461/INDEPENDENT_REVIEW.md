# Independent exact-tip review: Core-owned selected P12 Crime/Justice coordinator

## Verdict

**VALIDATED_CANDIDATE — PASS.** No actionable implementation or test-integrity findings on the reviewed increment. This review does not promote Core canonical or claim CORE-F completion.

## Exact identities and scope

- Repository: TorroisBr/Simulation
- Candidate branch/tip: codex/core/CORE-F-core-owned-runtime-application / 3ed546104ac5da52cdcd3c9335ebbe2064c88b66
- Candidate tree: d884e4893eaf8a537fd9af68d1269edfe8761b62
- Base: 8dbc81bc4307d2da3e35e6970552512117711514
- Core canonical at review: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Remote candidate and Core canonical refs were independently checked at those SHAs.
- The commit changes exactly four files: P12CrimeJusticeInvalidation.cs, SimulationRuntime.cs, DailyV1ExecutionBoundaryState.cs, and DailyV1P12CrimeJusticeMutationCoordinatorTests.cs.

The increment transfers selected P12 Crime/Justice coordinator construction and retention to the exact Core execution-boundary state. Unity supplies its registered census protocol; the existing non-selected/legacy coordinator path remains.

## Review findings

No actionable findings.

- Core creates and retains a single coordinator when the host protocol is first bound. Binding requires the admitted selected execution, current owner thread, and exact selected owner graph. Subsequent calls return the same coordinator only for the same protocol; a protocol replacement closes the old and incoming protocols and is rejected.
- The compatibility overload still exists for existing internal callers, but it verifies that the passed coordinator is the exact Core-created instance. Tests cover rejection of both a different protocol and a substituted coordinator; both are rejected before a new epoch reservation or mutation.
- The Core-owned daily operation obtains the coordinator from this session state and verifies exact coordinator/protocol/graph/session/thread identity before running the existing four separately reserved phases.
- The selected Unity adapter now asks Core for its coordinator and no longer constructs or injects it. The fallback path when no selected Core graph/boundary applies still creates the prior Unity-owned coordinator with its original admission, owner-thread, and fault callbacks.
- Core owner checks, revision capacity, section identity, protocol reservation, commit notification, and immediate-epoch behavior from the prior reviewed boundary remain unchanged. Core fault closure calls the same protocol closure that the prior selected path used; legacy runtime admission faults still route through the host callback.
- Existing assertions were not weakened. The changed tests now assert Core-created coordinator ownership and retain assertions that replacement protocol/coordinator calls fault and do not reserve another epoch.
- No broader persistence, TryStart, P12 profile, or CORE-F closure scope was added.

## Exact-source validation evidence

Private E: manifests were accessible. Their hashes for the changed files match the current candidate worktree:

- P12CrimeJusticeInvalidation.cs: 1325a7dd2b3a43b8dfbc10dfae65e4a009ca07b53947d7a5134e51b23b70ee60
- SimulationRuntime.cs: 32f73bf2001f678a826700bbe21e8fab35bb22c930a7e0b985a939933ff437fc
- DailyV1ExecutionBoundaryState.cs: 749e8ad2aa7977aa6b703d16a74a1e01002ed7bfc37515a5976b279fb9b783af
- DailyV1P12CrimeJusticeMutationCoordinatorTests.cs: a909cab7d257b18584db4e3eba3d798aae9193b7053ac756bfd7570bd72e5c44

Official CoreValidation passed 907/907 cold and 907/907 warm, run b7df41494ed043c3ad7c1dd5a5cefb8a. Its manifest records the base commit/tree 8dbc81bc / e0516e65 and a complete stable input digest; the changed Core/test hashes above match the exact candidate worktree.

Official full Unity EditMode passed 3275/3275, run fb6847bb54cd41878ee77fa35483dd89. The separately selected Smoke EditMode suite passed 5/5, run 0b236625431d4dab8a678429d60fb5bf. The focused selected Crime/Justice suite passed 4/4, run fb7bdb16dcd547d8b225876791323e86. All three Unity manifests report stable inputs and match the three changed production-source hashes above. The supplied validation record also reports git diff --check PASS. Protected manifests and original evidence were not modified.

## Limits

This review validates only the bounded transfer of selected P12 Crime/Justice coordinator ownership to Core. It does not establish complete CORE-F integration or authorize canonical promotion.
