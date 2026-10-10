# P12-G live owner identity and same-attempt package evidence

**Canonical base:** `a39bbd49ef8755f5eaa6613594e143de4f9d6c3e`
**Candidate Git tree:** `59ab054c5a4a89281176ef30dd64e8cd39a16402`
**Candidate Assets tree:** `85f1ff86e02e3fe25d947c42584f5bf8b6364705`
**Unity Editor:** `6000.3.9f1`
**Status:** all required validation passed; independent exact-tip review pending.

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
