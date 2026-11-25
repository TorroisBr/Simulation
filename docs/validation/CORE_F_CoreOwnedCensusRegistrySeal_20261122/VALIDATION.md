# Core-owned selected-profile census sealing: lease quiescence

Date: 2026-11-23
Candidate branch: codex/core/CORE-F-restore-stage-transaction
Base: d4b9f248b7bceebd543e3b0dbaac389c7cce4d6b
Initial seal implementation: dbafcfdda591a1a62c3eaae33402887f98582712
Lease-quiescence correction: f2202a051063cbd6bfa5ce95627dc3a858984dbe
Candidate tree: 1ade3ae48ff8d7728830b1b91466bedaa76ff44b
Core canonical remains unchanged: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea

## Correction and scope

The independent review of the initial seal implementation found that its initial and final quiescence checks omitted a lease-only advance interval. TryAcquireAdvanceLease can hold a lease without an operation scope.

The Core sealing path now rejects an active advanceLease at both checks, under the existing synchronization. The regression acquires a lease without an operation scope, verifies sealing returns false with no registry/readiness publication, disposes the lease, then verifies fresh sampling and sealing succeeds.

Core continues to use the existing census registry. Unity stages and binds providers, supplies host projections and mirrors sealed baselines to its compatibility tracking. P18 receipt rows remain host-projected; mixed/unselected NPCs keep Unity compatibility providers.

The accepted exact 89-family selected-profile contract is unchanged. This checkpoint does not claim that all 89 providers are composed by Core, that every P12 owner is Core-owned, or that CORE-F is complete.

## Validation

| Gate | Result | Evidence |
| --- | ---: | --- |
| Focused Core seal tests | 4/4 PASS | CoreLiveProviderSeal filter |
| Official CoreValidation cold and warm | 1192/1192 PASS | 6c0b189201c54c0c9bdf7316ffa3eb26 |
| Unity census protocol | 32/32 PASS | cb01ba6d8d3f42ee906d7b40ad773025 |
| Unity P12 atomic-save regression | 1/1 PASS | 3f808fb2f70e4b879ba41f8f2381cf1b |
| Full Unity EditMode | 3325/3325 PASS | 5c2f19621737412d9b4e21f01cca3511 |
| Unity Smoke filter | 5/5 PASS | b3913e4712b34038ac494f883841ad9b |
| git diff --check | PASS | tree 1ade3ae4 |

CoreValidation manifest SHA-256: a4294917d1dbb5d6051781dd63376fce0405698290230135b0b84446816ee1a0. It binds commit f2202a051063cbd6bfa5ce95627dc3a858984dbe and tree 1ade3ae48ff8d7728830b1b91466bedaa76ff44b, reports stable inputs, and records digest e5f9e30c7d52bca1a18ee25a7145ba2e4f34fd519c4dbf8db3b59a9acc6d2f27. Cold and warm TRX SHA-256: 1413555e8250a314224169bca2d56b05dd4a4a7a1212bcb4ca66948a366b299d and c756c2fb9714bc8aaa7a1f07f366fe32c5b48a636ecb6f337abb31aaadef1c13. Raw reports/logs remain protected locally; no public DotNetTrx package is claimed.

Unity full-run inputStable=true; it captured Core file SHA-256 e4aea2f47db7e4153a3153ffb81711dbd94a202e935bbe7249812cd82f9bd9af. The unchanged census adapter SHA-256 is 0821afd959c6bd6c9b4e2fe3db7d7f4e562d5c2524f88fe87d649d57367c278c. Unity run identity fields show pre-correction HEAD dbaf/tree b211; source hashes match the final f220 commit. Full-run manifest SHA-256: bd5368a87820bb7555170189cee45e6b86f743bc069dce4401ab59e1c40d3f85.

The full EditMode package is Unity/coref-census-seal-editmode-reviewfix-20261123.zip (176239 bytes, SHA-256 8d7acf1cc89557d423c1f6d00c4f412bee4e267e2f1de3a96659895b8bc8a39e); manifest SHA-256 9a42277fd513fef5dcde2858acbbe73b1f38e5e886e32390c8380c7af1d55e72. The official verifier returned VALID: runs=1 entries=1. Focused/Smoke originals remain private and unchanged; filtered reports were not normalized.

An earlier full EditMode run used a long E-local temp path and reported five P12 storage failures (3320/3325). The constructed atomic-save path measured 275 characters. Its report/log are preserved. With a short E-local temp root, the P12 storage test passed 1/1 and the full suite passed 3325/3325 without source changes.

## Source identities

| File | SHA-256 |
| --- | --- |
| Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs | e4aea2f47db7e4153a3153ffb81711dbd94a202e935bbe7249812cd82f9bd9af |
| tests/Simulation.Core.Tests/DailyV1ExecutionBoundaryStateTests.cs | 9a6ef34543f0c1a4408411d5722b4620cf3f8f461cc48b68fc50c222cce95fb8 |

## Preservation and review

E: free space was 24,458,334,208 bytes before validation and 23,931,699,200 after final CoreValidation (about 22.29 GiB free). No evidence or Unity cache was removed. ProjectSettings edits, the separate TravelParty validation note, private evidence, and unrelated untracked files remain outside this checkpoint.

Independent exact-tip review: PASS for commit f2202a051063cbd6bfa5ce95627dc3a858984dbe, tree 1ade3ae48ff8d7728830b1b91466bedaa76ff44b. It confirmed both lease gates, the lease-only regression, and unchanged census semantics; no code findings.

Core canonical is not promoted.