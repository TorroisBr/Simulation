# CORE-F P12 Crime/Justice Core-session boundary

**Scope:** Keep the exact selected P12 Crime/Justice reservation coordinator used by already-bound owner writers attached to the Core DailyV1ExecutionBoundaryState. The Unity adapter continues to provide the existing continuation-census protocol and exact Crime/Justice receipt identities. The Core state now verifies and pins that same coordinator and protocol for the lifetime of the session before running each daily phase.

This is an internal integration checkpoint. It does not complete CORE-F, publish DailyV1Application.TryStart, or promote Core canonical.

## Preserved behavior

- The four daily phases remain four separate scopes in their established order: clear arrested-today, advance hidden statuses, advance sentences, and sync wanted statuses.
- The existing writer-bound coordinator instance is reused. No duplicate coordinator or mutation authority is created.
- The existing P12 section identities, exact owner checks, revision/capacity checks, epoch reservation and changed-section publication remain in the Core coordinator.
- A session rejects replacement of its coordinator or census protocol. Wrong owner-thread, graph, or protocol binding fails closed.
- Mixed/unselected roster handling remains on the existing whole-stage Unity compatibility path.
- No assertions or test expectations were weakened.

## Exact source identity

The validation run started from candidate base 83e2d0f14de1314ab0f16dd290c19158712d9527 (tree 4398458f432f1d034fd193143e1697f06acb95b8) and captured the following final source files:

| File | Bytes | SHA-256 |
|---|---:|---|
| Assets/_Project/Scripts/P12CrimeJusticeInvalidation.cs | 27267 | 1ebc38562d3d3c00c8e4d5d18000b710cd273c417d7133a80b0c5432dc86a9b4 |
| Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs | 60936 | 90b181fd41fdd0b766f3eee991768f86b8b1975f689788f483603738d850be98 |
| Packages/com.simulation.core/Runtime/Application/DailyV1P12CrimeJusticeMutationCoordinator.cs | 26025 | 22888bf549632d406505f8c6012aa2e5bb40e33ceaebd838a13aaaea06b6e9e1 |
| tests/Simulation.Core.Tests/DailyV1P12CrimeJusticeMutationCoordinatorTests.cs | 31963 | 3d391cdb00d86c57813eb89f394c2b480acadd265a49a0a36d830f4435abc492 |

The complete Unity run manifest recorded stable validation inputs for the run. Its public package contains normalized NUnit XML only; the original XML and Unity logs remain in owner-only local retention.

## Validation

- Official CoreValidation, SkipLab: 905/905 cold and 905/905 warm; runner PASS. The full Core test project and CalendarDemo gates passed. The original manifest and TRX remain in owner-only evidence; no public .NET TRX package is claimed.
- Focused Unity Crime/Justice regression: 4/4 passed, including the separate-scope test, continued daily advances, selected P12 writes with mixed-roster fallback, and rejection of a missing exact Crime receipt identity.
- Official Unity EditMode: 3275/3275 passed; zero failed, skipped, or inconclusive; input inventory stable.
- git diff --check: PASS.
- Evidence package: coref-p12-crime-justice-session-boundary-full-20261028.zip, 173517 bytes, SHA-256 beea662f2b8b0389db17c163f69d01d05226d26489bc1cabc3f79e8401ceb505. Verification against the retained exact-source run manifest: PASS.
- Unity Smoke and the full CORE-F integration acceptance gates were not part of this checkpoint.

A first diagnostic Unity run on the initial implementation correctly exposed a split between a newly created Core coordinator and the coordinator already bound to Justice writers. That attempt was retained privately and was not packaged as passing evidence. The final implementation reuses and pins the writer-bound instance; all passing results above are from that corrected source.

## Evidence integrity

The public package is normalized XML only. Its public manifest SHA-256 is 17ecc29d46a66784520f5144e267bc204276907b895da34327207fa011e67a5b. Private CoreValidation evidence remains owner-only and unchanged; its manifest SHA-256 is d2bccecceff7ef4a569450978752efee444146515cd26d057fcf552b876f8088.