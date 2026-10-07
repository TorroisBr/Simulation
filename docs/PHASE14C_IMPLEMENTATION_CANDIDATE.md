# P14-C Implementation Candidate — 2026-10-07

**Checkpoint:** P14-C — Multiple Identifiable Sources v1  
**Status:** validated implementation candidate; exact-tip independent review pending  
**Implementation commit:** `12eadd3648b30d444b0fbb6b6f829d19ee256e9e`  
**Implementation tree:** `0f0323b0a9b0ec0c579535691590beeaa259d22c`  
**Implementation base:** `ce29f21878bda260ac96b723965d49ebe2aa5320`  
**P14 canonical at candidate start:** `06e9c30101a74bd618d3651885c489c79fe866bb`  
**P12 canonical at candidate start:** `405f70e58a7a1dd8be255798b795faff095f44b4`  
**Architecture:** `e16796014d348e3b59da7ed848101c4c03926ba5`

## Accepted scope and limits

The user accepted the reviewed P14-C design on 2026-10-07. The implementation uses one City, one item, one exogenous source, and one finite-reserve source as a bounded proving profile only. This fixture does not define a universal limit on Cities, items, or source count/type. It preserves distinct stable source identity, explicit source-specific policy and availability, deterministic contribution composition, and one coherent atomic daily stock result. Finite reserves cannot silently underflow or produce twice for one occurrence.

P14-A and P14-B contracts remain unchanged. The P10 Ruin profile remains separate. `UnityBootstrap-Daily-v1` continues to reject P14 material-flow profiles before world identity or runtime-owner construction. This candidate makes no P12 readiness, complete owner/epoch coverage, capture eligibility, export/hydration, universal economy, Phase 14 closure, or P19 extension-loader claim.

## Implementation summary

- Adds explicit exogenous and finite-reserve source kinds and a mixed-source P14-C proving profile while retaining existing profile ordinals.
- Validates the proving profile's stable City/location/settlement/store identity, single item/Market row, explicit distinct source IDs and kinds, and free same-City consumption.
- Shares one deterministic mixed-source preparation path between direct local daily flow and the P18-D daily production route. Contributions are ordered by stable source ID. Market overflow rejects the whole contribution; finite reserve is debited only when its contribution is actually applied.
- Prepares the Market and finite-source owner changes together, checks admission/revisions/configuration before installation, and emits notifications after the prepared operation succeeds. Exhaustion is represented as exact zero without an additional owner mutation.
- Carries occurrence/day identity, ordered source outcomes, production opening/post stock, post-production Market revision, actual free consumption, and closing stock through P18-D receipts. Consumption requires the matching production occurrence and unchanged post-production Market revision/stock; replay returns the existing receipt without applying production twice.
- Keeps P12 Daily-v1 admission rejection ahead of identity/owner creation and adds profile, atomicity, stale-state, replay, diagnostics, and admission regression coverage.

## Changed implementation files

- `Assets/_Project/Scripts/CityRuntime.cs`
- `Assets/_Project/Scripts/Data/CityData.cs`
- `Assets/_Project/Scripts/Diagnostics/WorldStateCanonicalWriter.cs`
- `Assets/_Project/Scripts/Diagnostics/WorldStateDiff.cs`
- `Assets/_Project/Scripts/FiniteProductionSourceStore.cs`
- `Assets/_Project/Scripts/FiniteSourceProfileAdmission.cs`
- `Assets/_Project/Scripts/LocalDailyMaterialFlow.cs`
- `Assets/_Project/Scripts/MarketRuntime.cs`
- `Assets/_Project/Scripts/P14CMixedSourceProductionPreparation.cs` and its Unity `.meta`
- `Assets/_Project/Scripts/TesteSimulacao.cs`
- `Assets/_Project/Tests/EditMode/Editor/P14CMixedSourceProductionTests.cs` and its Unity `.meta`
- `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`

## Validation on the exact implementation tree

Unity version: `6000.3.9f1`.

| Gate | Result | XML SHA-256 |
|---|---:|---|
| P14-C focused suite | 8/8 passed | `7C0FD533AF2336AED5CEBDFFBDF413917E1FFE59AE98D88F7B1329EE8520018C` |
| ALL EditMode | 2454/2454 passed | `FAF8DC59BB6896D5371B588357AF918C2F6E05531E20DE1E00FE95F76F4C7DC8` |
| Official Smoke | 5/5 passed | `3F5045644113F98DB134EC2A05FA5E1F91A96ECB7888F156A555BB560C15361A` |
| `SimulationRuntimeLongRunTests` | 7/7 passed | `085F4926619B68BE0B0CEB5C74C38145FF3AE2DD1DD57264D8868454686AE583` |

`git diff HEAD^ HEAD --check` passed for implementation commit `12eadd3`. The durable archive [`P14C-implementation-validation-0f0323b.zip`](validation/P14C/P14C-implementation-validation-0f0323b.zip) contains each exact XML and Unity log listed above; archive SHA-256: `5D27F6DBDE8FFB8198DD0417AAF50A0AB3528F7CAFBC1DBD0D7EB463575D10E7`.

Focused suites for P14-A/B material flow, P18-D production, and runtime admission were also rerun during implementation; their regressions are included in the final ALL EditMode result. The full diff still requires independent exact-tip review against base `ce29f218` before this becomes a validated candidate. No canonical promotion or Phase closure is recorded here.
