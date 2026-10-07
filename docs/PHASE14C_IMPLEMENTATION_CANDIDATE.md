# P14-C Implementation Candidate — 2026-10-07

**Checkpoint:** P14-C — Multiple Identifiable Sources v1
**Status:** VALIDATED_CANDIDATE; exact-tip independent review PASS
**Implementation commit:** `6d9498d31bff1dc75e7071fe88ce2f80c37a7ebf`
**Implementation tree:** `76142065bdcfcd01c2f58e7ba4dd0cdfef01ea92`
**Implementation base:** `ce29f21878bda260ac96b723965d49ebe2aa5320`
**P14 canonical at candidate start:** `06e9c30101a74bd618d3651885c489c79fe866bb`
**P12 canonical at candidate start:** `405f70e58a7a1dd8be255798b795faff095f44b4`
**Architecture:** `e16796014d348e3b59da7ed848101c4c03926ba5`

## Accepted scope and limits

The user accepted the reviewed P14-C design on 2026-10-07. The one-City, one-item, one-exogenous-source, one-finite-reserve-source composition is a bounded proving profile only; it does not define a universal limit on City, item, or source cardinality. The implementation preserves distinct stable source identity, explicit source-specific policy and availability, deterministic contribution composition, and one coherent atomic daily stock result. Finite reserves cannot silently underflow or produce twice for one P18D occurrence.

P14-A and P14-B contracts remain unchanged. The P10 Ruin profile remains separate. `UnityBootstrap-Daily-v1` continues to reject P14 material-flow profiles before world identity or runtime-owner construction. This candidate makes no P12 readiness, complete owner/epoch coverage, capture eligibility, export/hydration, universal economy, Phase 14 closure, or P19 extension-loader claim.

## Implementation summary

- Adds explicit exogenous and finite-reserve source kinds and a mixed-source P14-C proving profile while retaining existing profile ordinals.
- Validates the proving profile's stable City/location/settlement/store identity, one item/Market row, explicit distinct source IDs and kinds, and free same-City consumption.
- Shares deterministic mixed-source preparation between direct local daily flow and the P18-D daily production route. Contributions are ordered by stable source ID. Market overflow rejects a whole contribution; finite reserve is debited only when its contribution is applied.
- Prepares the Market and finite-source owner changes together, checks admission/revisions/configuration before installation, and emits notifications after the prepared operation succeeds. The finite source revision/day is checked on every prepared commit, including zero-output outcomes. Exhaustion is represented as exact zero without an owner mutation.
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

## Independent review history

The first exact-tip review returned `NEEDS_CHANGES` for implementation `12eadd3648b30d444b0fbb6b6f829d19ee256e9e`, tree `0f0323b0a9b0ec0c579535691590beeaa259d22c`. It found that a zero-applied finite source outcome did not recheck the captured source revision/day before installation. Commit `6d9498d` fixes the guard and adds `PreparedP18ProductionRejectsStaleFiniteOwnerWhenFiniteSourceOverflowed`. The fresh exact-tip review passed and is recorded in [P14C_IMPLEMENTATION_REVIEW.md](design/P14C_IMPLEMENTATION_REVIEW.md).

## Validation on the exact implementation tree

Unity version: `6000.3.9f1`. All results below were run after the final code change and match implementation tree `76142065`.

| Gate | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| P14-C focused suite | 9/9 passed | `6B887242B49FDDF25D2C54740364F83D28F441216E9DC42410FEB141B7F6FA37` | `58A99F8912FD898569C7C3559D13FC9335B3C1753AAE43FBE5137157335AEEC4` |
| ALL EditMode | 2455/2455 passed | `8147137338B56433B8C209FD66710B6234F195DADDBF4FC38B0BBF26BF037C18` | `440EEFD0B930C728009E8585E675DC937745E0281A4D6700A9E5E7A463B3D5C3` |
| Official Smoke | 5/5 passed | `4877FE4E280AC19AC1E17D41343891877524CA21C2879E173142FBCC204A24E8` | `93874FE26DF59D0003BE15640ADCB8E56492FC579A20F9222366399B42661282` |
| `SimulationRuntimeLongRunTests` | 7/7 passed | `3DE719E09C3997C9775C017D551A410D097782A143D9064348706E5B13921DB0` | `08EDAADBBDF5FBB0C43BB4BF5D7BA1B05F90E67AB1DB9E00679835F5D4C8E1F0` |

`git diff --check` passed on the implementation diff. The durable archive [`P14C-implementation-validation-7614206.zip`](validation/P14C/P14C-implementation-validation-7614206.zip) contains each exact XML and Unity log above; archive SHA-256: `EECC8970EC94890BA502E1439B6470A876E3FA6630DEE31EDE9AB0B7A9FA08E0`.

Exact-tip independent review passed on 6d9498d / tree 76142065. This candidate is not canonical promotion, P12 readiness, P12-B completion, P12-A readiness, P13 readiness, or Phase 14 closure.
