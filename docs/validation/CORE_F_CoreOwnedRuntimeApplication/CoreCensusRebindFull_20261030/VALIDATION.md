# Core-owned Inventory and MoneyAccount census boundary — validation

Date: 2026-10-30
Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
Source checkpoint before this implementation: `978e0db0a7aa43ac9b8c71e34a0aca67d4a53acf`
Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

## Scope and result

This validated implementation adds a bounded Core census boundary for selected
NPC Inventory and MoneyAccount owners. It runs only after the host has captured
the complete Daily-v1 census and rechecked the admitted operation scope.

For an all-selected roster, Core requires the host rows to match the exact
selected owner graph by section identity, Required role, schema version, owner
instance, cardinality, and revision. It replaces only those two rows with fresh
witnesses from the exact Core providers. Missing, duplicate, inconsistent, or
stale rows fail closed.

For a mixed roster, the host's full-roster Inventory and MoneyAccount providers
remain the source for both families. Core detects that both complete family
row counts exceed the selected graph count and preserves the host rows intact;
unequal family counts fail closed. This prevents combining a selected-Core
subset with a Unity compatibility subset and preserves the admitted mixed-roster
fallback.

This does not move the full 89-family census, owner-thread protocol, mutation
epoch capture, or completed-boundary token publication into Core. The host
still validates the full census and stable mutation epoch before this bounded
rebind. It does not complete CORE-F, `DailyV1Application.TryStart`, detached
restoration, or the executable .NET host, and it does not promote Core
canonical.

The implementation and tests were validated from the exact worktree inputs
below. The final candidate commit and independent exact-tip review are recorded
separately after checkpoint creation.

## Source identity

The reviewed source inputs at validation time had these SHA-256 values:

| File | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs` | `21a43e981f202366358d8abe722ae04d4adaa99a89a2e2d6b24af3444b9c1392` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs` | `9a9213a323f2bc1d07edaf6e36c48d49a1b131e8efa3b9546623e0ac80c74a40` |
| `tests/Simulation.Core.Tests/DailyV1CityEconomicOwnerCoreTests.cs` | `a070c8b6aa1f0cbee8c601237cdd418506922f8d96c3757d0b741989ec9721e4` |

## Validation evidence

| Gate | Result | Evidence |
|---|---|---|
| Official CoreValidation cold and warm | **944/944 PASS each**; all failures, skips, aborts, inconclusive, timeout, and not-executed counts were zero | Run `16c29b930a81421db3deda04898471e1`; private runner manifest SHA-256 `dde7637aa38bf46c00f7133f2fbffce68cf1e1fe978b244fa1f39ae5e9df4ec0`; stable input digest `a518ff857ed86887c7f048060df6c6f9ad05f7684d84ac714ac5f06d840db2ed`; cold/warm TRX SHA-256 `78feea641b60c2892092fb067ed91ad5ad699aa3dcd4b84c6baa988e4548689d` / `bc8952717b0663279a07095b1781dfa90a494cb529714ee6b587d96adc8e664b` |
| Focused live-vector transition | **1/1 PASS** | Official Unity EditMode filter `SelectedDailyV1OwnerVectorAndSupportedNpcTransitionStayBoundToTheLiveProtocol`; run `a187870a561d47c98b95a44a197bfc1e` |
| Focused mixed-roster Crime/Justice continuation | **1/1 PASS** | Official Unity EditMode filter `SelectedCrimeJusticeDailyStagePublishesP12WritesAndUsesWholeRosterCompatibilityFallback`; run `7b52c83d806947869bb6f3e6c9dcc849` |
| Full Unity EditMode | **3282/3282 PASS**; zero failed, skipped, or inconclusive; includes all 7 LongRun cases | Unity `6000.3.9f1`; run `a74ed852e5e047838f49ede88aa7e262`; 1,974 validation inputs, stable input digest `c7f6b262c1aba61dfc1351717a8ade68f12b316c2c55011314383dc06ea8661a`; protected original XML SHA-256 `e0ad0e1e74ff07f590bd4f62e8c0c7a3d9c85ecfb083a57bcdfddd2a83c68ae7`; protected run-manifest SHA-256 `b9dd7144f755c90f7249e546ccbeffa30d20c301227eed76a2fb7b13c41b0af5` |
| Official Smoke filter | **5/5 PASS** | Official Unity EditMode filter `Smoke`; run `ce8018029d954f4d8152270b070bc075`; stable input digest matches full EditMode; protected original XML SHA-256 `31424eac55eead0efd0aa28a516874fa33e1bb77283abc1ad18476bf60dcaf3a`; protected run-manifest SHA-256 `01212ab59877af9b3c3c097833a56c966d5affddc477247845783c837cd99df3` |
| Unity result package | **Verified** | [Normalized EditMode manifest](coref-census-rebind-editmode.manifest.json), SHA-256 `848978f639db28012a1f42ef6dec01802ecad3f707d2bf9eb05991d5cb969ba9`; [result-only archive](coref-census-rebind-editmode.zip), SHA-256 `c145413699bdb9d7bf21c3af83276b0d98c64f2f3c0beb107b5e2b1c8d112e0f`, 173,678 bytes. The official verifier reported `VALID: runs=1 entries=1` against the protected original run manifest. |
| `git diff --check` | **PASS** | Checked the three implementation/test files; ProjectSettings changes were excluded. |

The CoreValidation originals and its private manifest, Unity original XML/logs,
filtered Smoke original, and focused-run originals remain in protected local
evidence storage. No raw .NET TRX package or public .NET result bundle is
claimed. The Unity full EditMode package contains all five Smoke cases; the
standalone filtered Smoke original is retained privately and is not combined
with the full report.

An earlier intermediate Unity run on the pre-fallback implementation reported
2 failures in the two focused tests above. The implementation was corrected to
preserve both whole-roster families for mixed rosters, then the two focused
tests and final full EditMode/Smoke gates passed. The earlier original remains
preserved privately and is not counted as passing evidence.

The unrelated `ProjectSettings` edits, existing untracked user files, protected
evidence, and other worktree data were left untouched and excluded from the
candidate change.
