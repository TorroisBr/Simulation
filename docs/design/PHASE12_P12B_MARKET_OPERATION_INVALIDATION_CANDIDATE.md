# P12-B selected Market operation invalidation candidate

**Status:** Implementation and required validation complete; independent exact-tip code review pending. This candidate does not request canonical promotion.

## Exact boundary

- Canonical base: `codex/phase12/canonical` at `b8a7da54864bee3fb9b8916793240e91fbce0955`.
- Implementation commit: `55cb17109292930a4ac73026c3a4cf186403d5bf`.
- Exact-tip coverage commit: `c81c5159ff71ace1e64e8e74d9f027393a5299ca`.
- Exact candidate code/test tree: `e5182972550e5eab9bf9a926ca4bf5d483ddaec3`.
- Candidate branch: `codex/phase12/P12BMarketOperationInvalidation`.
- Reviewed design: `PHASE12_P12B_MARKET_OPERATION_INVALIDATION_DESIGN.md`; exact-tip design review PASS at `9a71eb65b00d7425172225d0812de5f5cfb2c121`, durable record `PHASE12_P12B_MARKET_OPERATION_INVALIDATION_DESIGN_REVIEW.md`.

## Delivered slice

For a runtime using the accepted `UnityBootstrap-Daily-v1` admission context, `SimulationRuntime` registers the existing exact City Market census providers as required owner sections before sealing the partial section inventory. In the selected two-City `Simulation-GeneralTest` profile, this adds its two exact Market owners to the existing 142-section partial set. The Market sections retain live row cardinality and local revision; this does not complete the owner inventory.

The runtime registers `runtime.economy.market-purchase` and `runtime.economy.market-sale`. Bound Open-market transactions validate the exact rostered NPC account and Inventory sections plus the exact registered Market owner and unchanged baselines before their first write. Each committed account or Inventory revision and each successful Market-local revision advances the existing partial mutation epoch while the named operation scope is active. Successful account compensation is also reported. Account-backed City/Counterparty paths are rejected before writes by this profile-bound service; unbound standalone transaction behavior remains available.

Composed City Markets bind the exact transaction-service instance already shared with `MerchantSystem`, before bootstrap publication. `MarketRuntime.BuyItem` and `SellItem` use that same instance. Direct `AddStock`, `RemoveStockUpTo`, and changed `UpdatePrices` commits run owner-thread and baseline admission before mutation and report the post-commit Market witness. The existing daily production, Free-consumption, and price-refresh paths therefore report Market writes under `runtime.advance-day`. A changed price refresh performs no writes when the Market revision is exhausted. Purchase and sale both preflight that a Market revision can advance before changing NPC owners; a failed sale stock install is never reported as success.

The transaction scope does not include later Merchant plan completion, TravelPlan, Commercial Knowledge, Crime/Justice, other economy operations, account-backed owners, or P18 prepared daily-economy installs. It does not create a universal lock or retroactively cover unrelated direct account/Inventory writers. P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked. No complete owner coverage, shared-epoch coverage, capture eligibility, export, or hydration claim is made.

## Validation

All results below are from the exact code tree above. XML and log paths are under `Temp/ValidationResults`; SHA-256 values were read immediately after each successful run.

| Suite | Result | XML | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|---|
| `SimulationRuntimeAdmissionTests` | 25/25 | `EditMode-20261001-223127-cedd201a95e9419b858a959e60d5dc27.xml` | `A41056E35C51DFD71661837B06DB9BB1705EA8C710517035BF65A902F294DB22` | `ED2134566BE413892D9ECCE137E4E83BE9E2B0608E610860DEFFBA29AA6332DC` |
| `EconomyTransactionTests` | 45/45 | `EditMode-20261001-223144-632904a70d4e458a84ea4792845a3c90.xml` | `D6E5BB7D528C9EA9C042A6CA9554B1441613B722AA0C5778B76054BFA013B1F0` | `CF94692ED1690880FB8E3F91E334368DA60283C3526A5BC7A005B3FFDC403B6D` |
| `CityDailyEconomyContinuationTests` | 7/7 | `EditMode-20261001-223159-49f2f5a67456441b8c3599314707f37f.xml` | `93DEA8D988BD5CF158CA162609B6BE1C724ABCB601F6092EA00B99D57E83C69D` | `2EFF329C941CCC253BCB9483EDF3DEEF5DA456944CBA1B3548CE48B5129ECE2F` |
| `CityMarketCensusTests` | 2/2 | `EditMode-20261001-223214-98a6f7b5406948b490f4c92f7ea0b79a.xml` | `809FCFEF10BAFF5D213B14940A019CD2BCC330177487B90D8E1A1533A1770114` | `9E809A57EAE5D00971E2E0FB833417219404AB87B4DB4B7DDE559DE3EC44A7E4` |
| ALL EditMode | 2136/2136 | `EditMode-20261001-223232-ae3dbf2402714b8aabc2ab4e14a1780b.xml` | `1DF27DE68F5B484D3687F037294FB18B89FA0AFBC74EA20392F608F8C2042B0D` | `A432907436FCF85E5A3598A1D1CACDDCE114E22C017E26068AE2A3114241DB99` |
| Official `Smoke` | 5/5 | `EditMode-20261001-223307-98d5f9776e5b4ed4be87aff5743ef800.xml` | `BEA30C3188E1A10D524501D457DB2418894A01900682F7A55E07EA23D36C2BE4` | `01622E85104E96AB2C7F89A0799F9C3A127135ABC786D1E5598A9FD1524A7DA6` |

`git diff --cached --check` and `git diff --check` passed for the implementation/test files. The initial daily-production test attempt exposed a fixture that had disabled economy simulation; the fixture was corrected before the final exact-tree results above. Unity-generated `ProjectSettings` edits and unrelated untracked `.meta` files remain unstaged and untouched by the candidate commit.

## Review and promotion boundary

The first independent exact-tip implementation review identified missing witnesses for changed-price refresh and daily Free-consumption/price-refresh commits. Coverage commit `c81c5159ff71ace1e64e8e74d9f027393a5299ca` adds those four assertions: direct changed-price revision/epoch plus no-op stability, and daily Free-consumption and changed-price revision/epoch. The expanded focused suites, ALL EditMode, official Smoke, and diff-check pass on the exact candidate code/test tree above. Independent exact-tip implementation re-review is pending. Canonical promotion requires separate approval. Promotion would advance only this bounded Market-operation invalidation slice; it would not complete P12-B, make P12-A ready, or unblock P13.
