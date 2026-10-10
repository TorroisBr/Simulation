# P12-G live owner identity and same-attempt package evidence

**Canonical base:** `a39bbd49ef8755f5eaa6613594e143de4f9d6c3e`
**Initial evidence candidate Git tree:** `59ab054c5a4a89281176ef30dd64e8cd39a16402`
**Initial validation Assets tree:** `85f1ff86e02e3fe25d947c42584f5bf8b6364705`
**Unity Editor:** `6000.3.9f1`
**Status:** validation passed; independent exact-tip code review passed for
Assets tree `c35e2a82d5607f191fb0d31bb82be4f14e7e7756`. The review record is
[`PHASE12_G_LIVE_INVENTORY_CLOSURE_REVIEW_7B2846A.md`](../../design/PHASE12_G_LIVE_INVENTORY_CLOSURE_REVIEW_7B2846A.md).

Each result XML reports `Passed`, zero failed, zero skipped, and zero
inconclusive tests. Result XML and runner log hashes are SHA-256.

| Run | Result | XML | XML SHA-256 | Runner log | Runner log SHA-256 |
|---|---:|---|---|---|---|
| `SimulationBootstrapCompositionTests` | 27/27 PASS | `OwnerIdentity/EditMode-20261010-023630-ddd97f18bab9460d81b7f278221af166.xml` | `5C11091A0BE550F228EE3CBE146DFCEFD1427C86E4BAC8C7566699BA185A4B2C` | `OwnerIdentity/EditMode-20261010-023630-ddd97f18bab9460d81b7f278221af166.log` | `9E4992E66158EBDE0FBA5D25FFB5E68A2F5148121AFAC897919D2BF8C0810F9C` |
| `P12CPrivateRootCompositionTests` | 53/53 PASS | `OwnerPackage/EditMode-20261010-023823-440bf41997de44ccb38be8bef91790fb.xml` | `9F2DD09D1E9C0A6E5650B66860F150509CC93E6DBF5E2219C00FBEF2E638372C` | `OwnerPackage/EditMode-20261010-023823-440bf41997de44ccb38be8bef91790fb.log` | `79C772411C22AF3918916870D17A2CF84BC5E93E899947504B751ADA33B7D8A9` |
| ALL EditMode | 2740/2740 PASS | `FinalAllEditMode/EditMode-20261010-023842-667f078c81d3464bbc2a093faf05ce66.xml` | `F4483431FE0228B87FBD75E331E635F276765766768EA4A54DA6E4FE018547AF` | `FinalAllEditMode/EditMode-20261010-023842-667f078c81d3464bbc2a093faf05ce66.log` | `2B261A64E7B9E94A6EC6A7048D1D392512DC36C4ADB2044B1F8ECB7FC08EBE2C` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 PASS | `FinalOfficialSmoke/EditMode-20261010-023921-d34c520503a64ccd9e4d649747339792.xml` | `14D7626219C80729CCC7511097076F4DDEB0F583ABD2D91825935E3582D866BE` | `FinalOfficialSmoke/EditMode-20261010-023921-d34c520503a64ccd9e4d649747339792.log` | `65D080E9022293DEDAD0AE785D4B64607E69DB3135C67293C9D8C5DFB22DB81E` |

Commands:

```powershell
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter SimulationBootstrapCompositionTests -ResultsDirectory docs/validation/P12GSameAttemptOwnerVectorEvidence/OwnerIdentity
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter P12CPrivateRootCompositionTests -ResultsDirectory docs/validation/P12GSameAttemptOwnerVectorEvidence/OwnerPackage
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -All -ResultsDirectory docs/validation/P12GSameAttemptOwnerVectorEvidence/FinalAllEditMode
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter Smoke -ResultsDirectory docs/validation/P12GSameAttemptOwnerVectorEvidence/FinalOfficialSmoke
git diff --check
```

`git diff --check` passed. The two focused runs exercise the 299-row registered
source-owner map and same-attempt D-to-F row correspondence. ALL EditMode and
official Smoke then passed against this Assets tree. No unrelated
`ProjectSettings` or `.meta` paths were staged or included.

## Supplemental omitted-read-model cardinality revalidation

**Code commit:** `7b2846aa49272a8467e930529eee8f162755cf4f`
**Assets tree:** `c35e2a82d5607f191fb0d31bb82be4f14e7e7756`
**Observation:** the selected pre-day-one Daily-v1 fixture reports
`DomainEventStore.Events.Count == 0` and
`NpcDecisionStore.Decisions.Count == 0`. These are observed cardinalities for
this live admission boundary. Both remain `OmittedNonCausalReadModel`; zero
here does not make either an explicitly empty owner or predict later counts.

| Run | Result | XML | XML SHA-256 | Compressed runner log | SHA-256 |
|---|---:|---|---|---|---|
| `SimulationBootstrapCompositionTests` | 27/27 PASS | `NonVectorCardinality/EditMode-20261010-024614-545fedb15f0444cfbb62105f79174115.xml` | `5BE12699B8FED4068E9E56692444042093E037D9E96EB57A7F9B9799750DDEA5` | `NonVectorCardinality/EditMode-20261010-024614-545fedb15f0444cfbb62105f79174115.log.gz` | `66200E898FF699A51B8841BCB500481CA0B734166792269101D5371418EEB376` |
| ALL EditMode | 2740/2740 PASS | `FinalAllEditModeWithNonVectorCounts/EditMode-20261010-024642-4f03eeb399ba4d2a9adab7cceb89e529.xml` | `CE4E0558A1EC47724627428F88C03C46AA2DAD9E636762C3B0AE4D6D05842F1D` | `FinalAllEditModeWithNonVectorCounts/EditMode-20261010-024642-4f03eeb399ba4d2a9adab7cceb89e529.log.gz` | `0F85FF25D880EB6874AF0339D8BEEE1B321D210024DC2818B4E3BB97284A9AB7` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 PASS | `FinalOfficialSmokeWithNonVectorCounts/EditMode-20261010-024726-e16d48af9f3b46649a03de63ac34efa4.xml` | `B827E60714AD66C59D2EC636738E925B913320D1DD8F977B1DE6D295A346F424` | `FinalOfficialSmokeWithNonVectorCounts/EditMode-20261010-024726-e16d48af9f3b46649a03de63ac34efa4.log.gz` | `CC8EA94207752E75C02A136DB685A4C56058771F10F9D4E9CD905C2526809D89` |

The focused composition result includes all current bootstrap-composition
tests (27/27). The P12-C private-root suite remains 53/53 on the unchanged
package test tree from the primary validation above. The full suite and Smoke
were rerun against this updated test tree. `git diff --check` passed after the
source and documentation updates. Independent exact-tip code review passed;
the durable record is
[`../../design/PHASE12_G_LIVE_INVENTORY_CLOSURE_REVIEW_7B2846A.md`](../../design/PHASE12_G_LIVE_INVENTORY_CLOSURE_REVIEW_7B2846A.md).
