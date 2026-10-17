# CORE-F Owner Consumer Boundary — WIP Progress

Status: implementation candidate only. This record does not claim independent review, full package validation, CORE-F completion, or canonical promotion.

## Initial candidate source identity (`fa593fd`)

- Base commit: `b12aeb21564c88e16fca12b3c85ee3bfd5058e94`
- Candidate branch: `codex/core/CORE-F-owner-consumer-boundary-20261017`
- Source-only tree: `52c3513edee86f6d24ecfa844289cf39d6c74457` (base plus the seven source/test files below; excludes this progress record and all generated or protected files)

| SHA-256 | Repository-relative path |
| --- | --- |
| `F641DC4192E41CB0289109A3D45837B4A6FC7C767301C67ED1B63DE9BF5FB3B4` | `Assets/_Project/Scripts/CityRuntime.cs` |
| `3F2A6B2DBB7AAA2AF2A5C848D30C4E06E78883AA83EB22C934FAEDEEA558755A` | `Assets/_Project/Scripts/FiniteProductionSourceStore.cs` |
| `84C3744303CA337538F4857A8F491FE8760C6791C3C55DA5E0F78ABD1E746322` | `Assets/_Project/Scripts/FiniteSourceProductionService.cs` |
| `32CEBB03234554F738C8A594A6EA6F5DE8EBE3709B12168456EE4D93D3DE7360` | `Assets/_Project/Scripts/MarketRuntime.cs` |
| `4514253BCD93438ED0C37B003EAA4EF8F97394094CCEA2261AED1ACF391442F9` | `Assets/_Project/Tests/EditMode/Editor/P14CMixedSourceProductionTests.cs` |
| `18BB541C90905396CE972F71BF825555BE7550CAD64DFB55A2A7412FC7107E3B` | `Packages/com.simulation.core/Runtime/Economy/DailyV1InventoryMarketOwners.cs` |
| `4B04C3159A1CDCAD484D91B53B882EC2D30AA16568D28640A5544FC16DBE6591` | `tests/Simulation.Core.Tests/DailyV1InventoryMarketOwnerTests.cs` |

## Validation record

- `Simulation.Core` build: PASS on the owner implementation before the final Unity-side mixed-profile dispatch adjustment.
- Focused `DailyV1InventoryMarketOwnerTests`: 7/7 PASS on the recorded owner implementation. The final source-only tree includes a later `CityRuntime` dispatch guard; no final full integration validation is claimed.
- `git diff --check` on the seven source/test files at the recorded source-only tree: PASS.
- Official CoreValidation: BLOCKED before test execution by the execution environment's .NET SDK diagnostic access failure.
- Official Unity evidence wrapper: BLOCKED before Unity launch because the required evidence-directory ACL could not be established under the current execution permissions. A separate ordinary runner attempt returned an operational failure with zero tests executed; it is not a test result.

Original diagnostic output and prior validation artifacts remain in the local worktree and are not included in this candidate payload. No existing result was rewritten or reinterpreted.

## Scope boundary

This WIP implements a bounded typed Inventory/Market consumer boundary and selected City/P14 mixed-source integration. It does not complete the CORE-F shared application/host package, does not claim independent exact-tip review, and is not eligible for canonical promotion on this record.
## Independent review follow-up

Review base: `fa593fd251d65da90d4dfbcfb2beb89d364cdd72`. The review identified two actionable CityRuntime findings:

1. A selected no-op PriceRefresh or Consumption step retained a prepared Market snapshot with zero revision increments. The receipt commit attempted to install that unchanged snapshot, which the selected owner correctly rejects.
2. Selected non-mixed direct prepared Market replacements did not check P12 material-flow admission and did not notify the P12 mutation boundary after installation.

The correction skips prepared Market installation when revision increments are zero, while still recording the ordinary receipt. Selected direct prepared writes now check `CanInstallMaterialFlow` during preparation and again at commit, then notify exactly once after a successful install. Mixed-source and finite-source preparation helpers retain their existing install and notification ownership. An added direct-path regression asserts that selected non-mixed stock writes use owner admission and one post-mutation notification per actual write; this Unity regression has not yet been executed.

Corrected source-only tree: `bf4e70bdb4d0b7f4aeeb2e23c18091a2be99c6cf` (base `b12aeb21564c88e16fca12b3c85ee3bfd5058e94` plus the seven source/test paths below; excludes this record and generated or protected files).

| SHA-256 | Repository-relative path |
| --- | --- |
| `C66E11D99ECE4A8EB55360609E98ADDBA27EE1C12085B07624EB0085FADE81D5` | `Assets/_Project/Scripts/CityRuntime.cs` |
| `3F2A6B2DBB7AAA2AF2A5C848D30C4E06E78883AA83EB22C934FAEDEEA558755A` | `Assets/_Project/Scripts/FiniteProductionSourceStore.cs` |
| `84C3744303CA337538F4857A8F491FE8760C6791C3C55DA5E0F78ABD1E746322` | `Assets/_Project/Scripts/FiniteSourceProductionService.cs` |
| `32CEBB03234554F738C8A594A6EA6F5DE8EBE3709B12168456EE4D93D3DE7360` | `Assets/_Project/Scripts/MarketRuntime.cs` |
| `13F2585623375C0614547474F498248BD7A753923C287AF364041F8682A30BDF` | `Assets/_Project/Tests/EditMode/Editor/P14CMixedSourceProductionTests.cs` |
| `18BB541C90905396CE972F71BF825555BE7550CAD64DFB55A2A7412FC7107E3B` | `Packages/com.simulation.core/Runtime/Economy/DailyV1InventoryMarketOwners.cs` |
| `4B04C3159A1CDCAD484D91B53B882EC2D30AA16568D28640A5544FC16DBE6591` | `tests/Simulation.Core.Tests/DailyV1InventoryMarketOwnerTests.cs` |

Regression coverage added: no-op selected PriceRefresh and Consumption receipt commits; P12 admission denial before preparation and after preparation; successful selected prepared install with exactly one post-install callback; and direct selected non-mixed production/consumption owner admission and callback ordering.

Validation after the review correction:

- Focused `DailyV1InventoryMarketOwnerTests`: 7/7 PASS.
- `git diff --check`: PASS.
- The new Unity regressions were not executed. The approved Unity evidence workflow remains blocked before test execution by the existing evidence-directory ACL restriction. No ACL or runner-mode changes were made.
- No full CORE-F integration validation, independent follow-up review, or canonical promotion is claimed.

## Independent review follow-up — rejected finite contributor at saturated revision

An additional P14 review finding showed that selected mixed-source preparation still required the finite-source owner to be installable when its contribution had already been rejected for aggregate stock overflow. At `long.MaxValue`, this incorrectly rejected the accepted exogenous contribution even though the finite owner had no planned mutation. `P14CMixedSourceProductionPreparation.CanInstall()` now checks finite-source installability only when the finite contribution is positive; `Install()` already leaves the finite owner untouched for an exact-zero contribution.

Added `SelectedMixedOverflowCommitsExogenousContributionWhenFiniteOwnerCannotAdvance`: the selected mixed-source fixture places Market stock at `int.MaxValue - 3` and finite-source revision at `long.MaxValue`. It verifies the exogenous quantity of two is committed, the finite quantity of three is reported as `AggregateStockOverflow`, finite reserve and revision remain unchanged, Market revision advances once, and one post-install callback observes the final stock.

Corrected source-only tree: `9c2a4f7b70783cb82217faf8ff7c11b4264d70e6` (base `b12aeb21564c88e16fca12b3c85ee3bfd5058e94` plus `Assets/_Project/Scripts/CityRuntime.cs`, `FiniteProductionSourceStore.cs`, `FiniteSourceProductionService.cs`, `MarketRuntime.cs`, `P14CMixedSourceProductionPreparation.cs`, `Assets/_Project/Tests/EditMode/Editor/P14CMixedSourceProductionTests.cs`, `Packages/com.simulation.core/Runtime/Economy/DailyV1InventoryMarketOwners.cs` and `tests/Simulation.Core.Tests/DailyV1InventoryMarketOwnerTests.cs`; excludes this progress record, generated outputs, protected files and unrelated changes).

| SHA-256 | Changed repository-relative path |
| --- | --- |
| `C677A3D20223BE2D5BDA060C4B7B8BCFC750E22021EA063E5B3F23F0F977E879` | `Assets/_Project/Scripts/P14CMixedSourceProductionPreparation.cs` |
| `C29FD5F4CD1DCAE85E3FF4A1B92D2750785629A55AFE8B0656CA4FE96E904CFB` | `Assets/_Project/Tests/EditMode/Editor/P14CMixedSourceProductionTests.cs` |

Validation for this follow-up:

- Focused `DailyV1InventoryMarketOwnerTests`: 7/7 PASS.
- `git diff --check` for the candidate source/test changes: PASS.
- The new Unity P14 regression and prior Unity regressions remain UNRUN; no Unity validation is claimed for this source tree.
- No full CORE-F integration validation, independent review of this follow-up, or canonical promotion is claimed.
