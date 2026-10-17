# CORE-F Owner Consumer Boundary — WIP Progress

Status: implementation candidate only. This record does not claim independent review, full package validation, CORE-F completion, or canonical promotion.

## Source identity

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