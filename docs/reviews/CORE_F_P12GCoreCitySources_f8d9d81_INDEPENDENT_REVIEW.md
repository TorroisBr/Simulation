# Independent exact-tip review — P12-G Core-bound City sources

## Verdict

**PASS.** No actionable implementation or evidence findings remain for this bounded source checkpoint.

## Reviewed identity

- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`.
- Evidence/documentation tip: `f8d9d81ab1bdab21fe189d5bcec70f1a66264202`.
- Evidence-tip tree: `fe0e6977182aab341905b23c17e8100b69712872`.
- Implementation commit: `13f717d662fcd9a834b76d979e0763195a4f2884`.
- Implementation tree: `4e012e98b86d6c83cd499da4c36f1426a1332fa1`.
- Implementation base: `2acfefcd3e8654fd2c13e1a20b7655ef48b88194`.
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Reviewer: `/root/p12d_stage_review`; read-only review, no candidate edits or test reruns.
- The candidate branch tip matched its remote ref at review.

The full base-to-candidate diff was inspected. The implementation delta adds the Core-bound City owner source, its Unity metadata, one explicit Core compile entry, and three additive Core tests. The remaining delta is the validation record and result-only Unity package. No protected ProjectSettings or unrelated worktree files are included in the candidate diff.

## Findings

`DailyV1P12GCoreBoundCityOwnerSources.TryCapture` requires an active continuation-save operation, revalidates the completed-boundary witness before staging, and revalidates it again before publishing the source collection. Each City is staged into a local list; every failure leaves the public output null.

For each City, `TryCaptureCity` uses the exact active graph owner and requires the same admitted effective-definition graph and the exact City definition object. It uses the City's existing stable `LocationRuntimeId` for the legacy and resolved location fields; this partial source performs no new location remapping or identity allocation. Full target-graph/location validation remains an explicitly excluded integration obligation.

The source verifies the existing P12-D section IDs, schema, required role, exact owner-instance identity, cardinality, and local revision. Population and receipt census entries must name the same population owner instance. It captures receipts from that owner and checks receipt cardinality and revision against the completed-boundary census. Market rows must retain the exact admitted item-definition objects. Encoding delegates to the existing P12-D City codec, preserving its established wire schema, section identities, alias group, and deterministic ordering. The P12-D codec also rejects duplicate or invalid row identities and unsupported profile values.

The three new tests are additive and do not weaken existing assertions:

- `CoreBoundCitySourcesCaptureExactCityOwnerValuesAndRevisionVector`
- `CoreBoundCitySourcesRejectMismatchedOwnerIdentityWithoutPartialOutput`
- `CoreBoundCitySourcesDiscardAllStagedRowsWhenWitnessChangesDuringRecapture`

All three passed in both recorded CoreValidation runs. The mismatch and stale-witness cases assert null output and balanced save-operation disposal.

## Validation and evidence applicability

CoreValidation private manifest:

- Run: `9453a327cc3b421c86d21bd40475bcd6`.
- Stable input digest: `e969ac280a923e3d4e61d042913c5ff6d3ff9313ebe63d9bcea627da7223c7a5`.
- Cold and warm: **814/814 passed** each.
- Private manifest SHA-256: `3042e6ac39a5fafe9cc2882a38f175151c363191df3781078b6c7ac65156dace`.

The CoreValidation manifest input hashes match the committed implementation for:

| Source input | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GCoreBoundCityOwnerSources.cs` | `bd6f46447d4cdcc61f1c2abf5646a9e5fcbcb0026d8ffaf22e75d401fbe7a0f0` |
| `src/Simulation.Core/Simulation.Core.csproj` | `db0dbd15e6445af65e574a8b36d62fdddaa487938d9cbbe4b2e337fc67126664` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs` | `1022006381787e5ae744cdf236c8248ab227241d1cb6a494c944541add8c400f` |

Unity input hashes also match the committed source and metadata:

| Unity input | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GCoreBoundCityOwnerSources.cs` | `bd6f46447d4cdcc61f1c2abf5646a9e5fcbcb0026d8ffaf22e75d401fbe7a0f0` |
| `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GCoreBoundCityOwnerSources.cs.meta` | `5850cef873189daeee9b48f5631972f3c073f9b1581a6b8e93a1c7d38d50d1f2` |

The Core test source is covered by CoreValidation; it is not claimed as a Unity input.

- Official Unity EditMode run `7f74348e0b314512bba856aa70042c21`: **3264/3264 passed**, Unity `6000.3.9f1`, stable input digest `cb4e374d3c462c0ec8546fd153bc16c524f0ef34ac37f91963deba210a2d4dfd`.
- Official filtered Smoke run `503adb38597140a1a81f647c296e0f93`: **5/5 passed**, stable before/after with the same Unity input digest. All five selected result identities and outcomes were found in the full EditMode result package.
- Result-only Unity package SHA-256: `9cec3bc94cf2db8ec790a321a258a29c274a4e47735abaab3e4a12fc202e1c2e`.
- Public package manifest SHA-256: `c16504fb6b9837db4b7876f131092ab1736312eec8ae00865fc0e626d98c5d07`.
- The package contains one normalized result XML with 3264 passed cases, zero failures/skips/inconclusive cases, and no absolute local paths. The public manifest uses repository-relative input paths and exposes no private workstation path.
- `git diff --check`: PASS.

The original Unity XML/logs and CoreValidation TRX/logs remain protected and private. No public .NET TRX package is claimed. Tests were not rerun for this independent review.

## Scope boundary

This is one Core-owned source for the existing P12-D City projection: important-NPC presence, Market rows, population aggregate, and population operation receipts. It does not implement NPC owner-group capture, causal roots, image assembly, target-graph validation, publication, complete P12-G save, or CORE-F host/application completion. It does not promote Core canonical or close a Phase.
