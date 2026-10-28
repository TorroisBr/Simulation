# CORE-F Core-bound P12-D City Sources — validation record

**Status:** validated partial candidate; exact-tip independent review is pending. This is one Core-owned source for the existing P12-D City projection. It does not complete P12-G or CORE-F and does not claim full-save readiness.

## Candidate identity and scope

- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`.
- Base: `2acfefcd3e8654fd2c13e1a20b7655ef48b88194`.
- Implementation commit: `13f717d662fcd9a834b76d979e0763195a4f2884`.
- Candidate tree: `4e012e98b86d6c83cd499da4c36f1426a1332fa1`.
- Scope: capture each Core graph City through the existing P12-D City hydration input and encoder, producing its four existing owner sections: important-NPC presence, Market rows, population aggregate, and population operation receipts. The implementation checks canonical City/Item definition binding, exact owner identity/cardinality/revision, the population/receipt owner alias, encoder output, and the same completed-boundary source witness before and after capture. It returns no partial source set on failure.
- Exclusions: NPC owner groups, roots, image assembly, target-graph validation, publication, and full P12-G/CORE-F readiness.

The implementation was validated in the worktree before the code commit. The captured input inventories are stable, and the exact hashes of the changed compiled source/test files below match the blobs in the candidate commit. The only later repository changes are this evidence record and its normalized Unity package.

| Candidate file | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GCoreBoundCityOwnerSources.cs` | `bd6f46447d4cdcc61f1c2abf5646a9e5fcbcb0026d8ffaf22e75d401fbe7a0f0` |
| `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GCoreBoundCityOwnerSources.cs.meta` | `5850cef873189daeee9b48f5631972f3c073f9b1581a6b8e93a1c7d38d50d1f2` |
| `src/Simulation.Core/Simulation.Core.csproj` | `db0dbd15e6445af65e574a8b36d62fdddaa487938d9cbbe4b2e337fc67126664` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs` | `1022006381787e5ae744cdf236c8248ab227241d1cb6a494c944541add8c400f` |

## Validation results

| Gate | Result | Evidence |
|---|---:|---|
| Official CoreValidation cold | **814/814 passed** | Run `9453a327cc3b421c86d21bd40475bcd6`; stable input digest `e969ac280a923e3d4e61d042913c5ff6d3ff9313ebe63d9bcea627da7223c7a5`. |
| Official CoreValidation warm | **814/814 passed** | Same run; zero failed, skipped, inconclusive, aborted, or timed-out cases. |
| Full Unity EditMode | **3264/3264 passed** | Run `7f74348e0b314512bba856aa70042c21`; Unity `6000.3.9f1`; input digest `cb4e374d3c462c0ec8546fd153bc16c524f0ef34ac37f91963deba210a2d4dfd`, stable before/after. |
| Official Smoke name filter | **5/5 passed** | Run `503adb38597140a1a81f647c296e0f93`; same Unity input digest; zero failed, skipped, or inconclusive. The five Smoke cases are also present in the full EditMode package. |
| `git diff --check` | **PASS** | Candidate implementation commit. |

The privacy-safe normalized full EditMode package is [`coref-p12g-city-sources-all-2acfefc.zip`](coref-p12g-city-sources-all-2acfefc.zip) (SHA-256 `9cec3bc94cf2db8ec790a321a258a29c274a4e47735abaab3e4a12fc202e1c2e`); its public manifest is [`coref-p12g-city-sources-all-2acfefc.manifest.json`](coref-p12g-city-sources-all-2acfefc.manifest.json) (SHA-256 `c16504fb6b9837db4b7876f131092ab1736312eec8ae00865fc0e626d98c5d07`). The official verifier passed comparison with the protected source run. The archive includes all 3264 result identities and outcomes, including the five Smoke cases. Raw Unity XML/logs and CoreValidation TRX/logs remain in protected E: evidence retention; they are not published here. The CoreValidation private manifest SHA-256 is `3042e6ac39a5fafe9cc2882a38f175151c363191df3781078b6c7ac65156dace`.

A Unity filter for the .NET-only test class selected zero Unity cases and is not counted. A separate filter for the GM Console smoke class selected two cases and is not substituted for the established five-case `Smoke` gate. The full EditMode suite and the official `Smoke` filter above are the counted Unity results.

## Review and status boundary

Exact-tip independent review is pending. No canonical branch was advanced. This partial source does not complete NPC capture, full source joining, image/roots composition, restored-graph validation, rejection/atomicity, continuation parity, or the CORE-F host/application contract.
