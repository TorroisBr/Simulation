# Independent Exact-Tip Review — CORE-F Application Image Seal

**Verdict: `VALIDATED_CANDIDATE`**  
**Reviewed:** 2026-10-29

## Exact identities

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate commit: `f2cf0db17d753117f48df77061b22bd8249dc078`
- Candidate tree: `8bb9da7455b54f4cc87743f4c16b472b5efbe9f3`
- Immediate base / parent: `0f047b28285376a93ee7e9ca65e291b7a0ef77f4`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Remote candidate ref matched the reviewed commit at review time.
- The exact base-to-candidate range is one commit and five paths. The reviewed candidate is not promoted.

## Review result

No actionable code or test-integrity findings. The operation-backed coordinator path delegates image sealing to the application. The application rejects a closed, foreign, inactive, or non-current operation; enforces the owning thread; binds the session roots, world/day/sequence and economy boundary; checks P9 profile/fingerprint and exact manifest object identity; and requires valid causal roots. It passes the owner sections from the operation's source witness into the established composer, revalidates before and after composition, and clears outputs on post-compose failure. The composer retains census/image and manifest matching, duplicate rejection, exact-empty encoding, and ordinal section ordering.

The coordinator keeps its no-operation legacy composition route. Supplying a closed Core operation does not fall back to that route. The added tests strengthen the persisted-image assertions for token/header identity, ordering, census/image parity and manifest consistency, and verify closed-operation rejection before host inputs are inspected. No existing assertions were weakened.

The five changed paths are limited to the coordinator, `DailyV1Application`, the continuation-save operation, its focused tests, and this candidate's validation record. No ProjectSettings, `.meta`, private evidence, or unrelated/untracked files are in the candidate diff.

## Exact-source validation reviewed

All four changed executable/test inputs match the candidate checkout bytes and both validation inventories:

| Input | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | `451b185c87d8e96104fc989d0ff96a0d76f65b628c0755d70d44954b9e827ba9` |
| `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GContinuationSourceWitness.cs` | `4535d57fd163e94898b9ed17eb81576b93f7b1e995967c7e96fbc080c6acc9bb` |
| `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.cs` | `3a801c71da9de8b829922065b0d37a94ebaa59b4f6075485fd023c022e4fa8d6` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `cb59c79d7c5c2112b6aaed10c1fa8ead66753353d8448651aeee08d4e2f73116` |

- Official CoreValidation run `56816e7e013d4a27af4a36ca16700780`: cold 936/936 and warm 936/936; stable inputs and generated dependencies. Its protected manifest SHA-256 is `101d9c123cdfbea324ceaa4e488cf979b2b9f8c1aea14e3da77af2fbc9c2220a`; all 13 listed report/log hashes matched the protected originals.
- Focused Unity persisted-image restore/continue run `642c9a8c7c41443faedd3f9813df7f0b`: 1/1 passed, stable before/after inventory. XML SHA-256 `cc2a7d9055b9bf7718a00527ffd81e5925983421a304571acdff8a82c2ce4812`; log SHA-256 `7301aa94db229617885290503d65de7c006e5137374ff2ed97d6211a88d97155`.
- Focused Unity closed-operation run `27c9fa2835c2478690e719c58319ec4f`: 3/3 passed, stable before/after inventory. XML SHA-256 `ca905f133ea7cd51a8c9ad6c26b2cb193bb1d97a25711398950f3b48b17abe9c`; log SHA-256 `3f847e259895d623cd19e2b284a38877848f47ebc4cdb045dd468ea30d38447b`.
- `git diff --check` passes for the exact base-to-candidate diff.

The private originals remain outside the candidate payload. Earlier failed probes are not counted as passing evidence; the passing runs above are the final runs whose stable input hashes match the reviewed candidate.

## Limits and integration constraints

This is a bounded image-sealing increment. The Unity coordinator still supplies the encoded typed sections and owns the remaining staging/capture work. This review does not establish complete Core-owned P12 capture/restoration, `DailyV1Application.TryStart`, detached .NET hosting, T/T+1/T+2 continuation, full EditMode/Smoke validation, or CORE-F completion. The privacy-safe permanent evidence package and broader integration validation required by the applicable promotion gate remain outstanding. No canonical promotion is approved or claimed.