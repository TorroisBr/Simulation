# P12-G Daily-v1 City-bound Person materialization reconciliation

**Base:** `93fd6ab7f39de572fafbfb1fa160f342935839e5`

**Architecture:** `47eff220c7ce00f6e7c759bdc2b76780bb46f628`
**Validated Assets tree:** `032e65318ea510c1009f89c8b0431a4c6aea46c0`

This bounded source/inventory repair covers the existing `runtime.npc-membership` path when a newly materialized Person receives a `startingCity`. `TryRegisterNpc` sees the new NPC before City binding, while `PersonMaterializationSystem` commits City presence later in the same enclosing scope. The runtime now marks that exact City census section as changed after successful materialization. The scope then reconciles Person membership/binding, dynamic NPC owner families, and City presence together under its reserved mutation epoch.

The selected Daily-v1 test checks exact owner identity, City presence cardinality and revision, the independently enumerated provider vector, one membership epoch for the successful materialization, and a successful post-commit census assessment. This closes only the selected-profile City-bound materialization transition witness. It does not complete the live owner/commit matrix or make P12-G implementation-ready; P12-G/P12-A remain `WAIT_DEPENDENCY`, P13 remains `BLOCKED`, and Phase 12 remains open.

## Validation

Unity Editor `6000.3.9f1`. Final successful XML files report `Passed`, zero failed/skipped/inconclusive tests, and coherent counts. Raw XML and compressed `.log.gz` files are retained beside this manifest.

| Gate | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| `SimulationBootstrapCompositionTests` | 26/26 PASS | `Focused/EditMode-20261009-212804-257f8c2f3edb4346a77571c582e0bb8d.xml` — `D03634149AE6E6B8C77F3B25ADB3089F9622726384B858F5FF1ECDA384AFD629` | `Focused/EditMode-20261009-212804-257f8c2f3edb4346a77571c582e0bb8d.log.gz` — `B498010C019F0B4FB62B98855BCF25DCF67D354DE2D381F34DDB988095793DD4` |
| ALL EditMode | 2732/2732 PASS | `AllEditMode/EditMode-20261009-212825-c5aa7e98763c4eb381af9dc92c889d9f.xml` — `D51D7C7446077463B2E44900FF809EFD490F2C6801AB26702E61446CCD0E9FFC` | `AllEditMode/EditMode-20261009-212825-c5aa7e98763c4eb381af9dc92c889d9f.log.gz` — `79C1973D245C73C91700D3F7AEC983F709C7311C8F1FDF23CDCBCBF37525FA70` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 PASS | `OfficialSmoke/EditMode-20261009-212905-33d7fdcadb5e492e972151963b408fde.xml` — `3746F9879735475F1C462BE5CD9CB7A20CF2334B9C3E8E5B6FA549DA4D913296` | `OfficialSmoke/EditMode-20261009-212905-33d7fdcadb5e492e972151963b408fde.log.gz` — `6C613B1CC31B1C86136A44B7046E6504BB105CE48CE672B5420CCF6DF582A09A` |

Commands:

```powershell
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter SimulationBootstrapCompositionTests -ResultsDirectory docs/validation/P12GCityBoundMaterialization/Focused
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -All -ResultsDirectory docs/validation/P12GCityBoundMaterialization/AllEditMode
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter Smoke -ResultsDirectory docs/validation/P12GCityBoundMaterialization/OfficialSmoke
git diff --check
```

An initial focused run on the uncorrected code failed because the selected owner vector expected the materialized NPC's rows while the membership reconciliation had faulted before publishing them. A separate intermediate test run caught that its epoch baseline was sampled before Person registration; the assertion was moved to immediately before materialization. These diagnostic failures are preserved in `Focused/` and are not counted as passing evidence. The final successful runs above used the exact validated Assets tree.

The earlier focused 26/26 run `212221`, ALL EditMode `212255`, and Smoke `212401` passed after the runtime fix but before the final shared-epoch assertion was added. They remain in the directory for traceability and are not used as candidate validation evidence. The runs in the table are the final-tree gates.

The unrelated `ProjectSettings/EditorBuildSettings.asset`, `ProjectSettings/ShaderGraphSettings.asset`, and three untracked `.meta` files were left unchanged and unstaged.
