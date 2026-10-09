# P12-G fixed receipt-owner identity witness

**Status:** validated, test-only evidence slice. P12-G and P12-A remain
`WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains `OPEN`.

**P12 canonical base:** `fa5607e3a138365a0ed814cda193edfd4aaab55d`

**Architecture baseline:** `47eff220c7ce00f6e7c759bdc2b76780bb46f628`

**Reviewed code tree to be reviewed:** `d88661ee88554716364855659e4f634b323519eb` (`Assets`)

The selected `UnityBootstrap-Daily-v1` composition test now compares the
`p12f.npc-decision-occurrence-receipts` witness to the exact
`NpcDecisionRecorder` installed in `SimulationRuntime`, and the
`p12e.economy-keyed-sale-receipts` witness to the exact
`EconomyTransactionService` installed there. Each owner continues to report
zero cardinality and revision in this profile, and repeated reads retain the
same identity. The change is test-only and adds no API or runtime behavior.

This closes only the fixed receipt service-to-witness identity assertion. It
does not prove that a future P12-G coordinator binds the target witnesses,
whole-graph composition, complete owner/cardinality or operation/epoch
coverage, target-bound restored-boundary admission, global quiescence,
publication, continuation parity, P12-G/P12-A/P13 readiness, or Phase 12
closure. P12-G remains `WAIT_DEPENDENCY`.

## Validation

Unity Editor: `6000.3.9f1`. All runs reported zero failed, skipped, or
inconclusive tests.

| Run | Result | XML SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|
| `SimulationBootstrapCompositionTests` | 26/26 PASS | `7CB733941A21E716CD3B92829E07AAF0876AEB380392D10C7CB81996588A3653` | `A9D00E52FA0B86EB4D46910F3502C5874EB23E36167CE8CE7F45B6D8720912F4` |
| ALL EditMode | 2733/2733 PASS | `96EC16602D20671BB6593F1B81DB66D10CE9A74653615573C46DD3C4E2C44CE1` | `9DA13F2C89767457221F9D219F513864722D9EAA5172FCBF7783461F8BBAC5BF` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 PASS | `BB881FCD8613C14ADAC08159C3656A7E914F41F7B25C1C6EAF700D2620C1066A` | `6500FC54B71A4381ED59AFA610483D54280BE44E6FCEFDFFDAAE6D63482C659C` |

The successful runs used:

```powershell
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter SimulationBootstrapCompositionTests -ResultsDirectory docs/validation/P12GFixedReceiptOwnerIdentity/Focused
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -All -ResultsDirectory docs/validation/P12GFixedReceiptOwnerIdentity/AllEditMode
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter Smoke -ResultsDirectory docs/validation/P12GFixedReceiptOwnerIdentity/OfficialSmoke
git diff --check
```

Unrelated ProjectSettings edits, untracked `.meta` files, and earlier
diagnostic XMLs remain unstaged and untouched.
