# P12-G registered source-owner witnesses

**Status:** validated, test-only evidence slice; exact-tip independent review pending.

**P12 canonical base:** `7ad325b7fe5f9003be0947d18798182496eace31`
**Code commit:** `a5af39f3323be8a8de4e01eb81161d200b7f0e53`
**Reviewed code tree to review:** `Assets` tree `480136d37053888910a54c134154ce9a4c105256`
**Architecture baseline:** `47eff220c7ce00f6e7c759bdc2b76780bb46f628`

## Scope

The selected Daily-v1 composition test now binds four registered protocol rows
to their independently installed source owners: Justice records, Justice P18
receipts, NPC decision occurrence receipts, and economy keyed-sale receipts.
Justice record cardinality and local revision are read from its installed
owner. The Justice sentinel is checked at cardinality one against the installed
Justice owner and its current receipt revision. The two global receipt rows
remain `ExplicitlyEmpty`, require cardinality zero, and use the direct
owner-issued revision rather than assuming that an empty cache has revision
zero. The existing registered Crime receipt source-owner assertion remains
unchanged.

This adds no production behavior, owner, profile row, or operation. It closes
these source-registration identity checks only. It does not close the complete
299-row identity/cardinality/revision and writer/operation/B-F-consumer join,
whole-vector transition coverage, exhaustive supported-writer or shared-epoch
coverage, runtime-wide quiescence, same-attempt G target census, restored-graph
coordinator, pre-allocation rejection, typed graph validation, failure
atomicity, no replay, continuation parity, P12-G readiness, P12-A readiness,
P13 readiness, or Phase 12 closure.

## Validation

Unity Editor: `6000.3.9f1`. Every final XML reports `Passed`, zero failed,
zero skipped, and zero inconclusive tests. The initial compile attempt found
duplicate local variable names in the edited test; they were renamed before
the final runs below. That initial run produced no test XML and is not claimed
as validation.

| Run | Result | XML SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|
| `SimulationBootstrapCompositionTests` | 27/27 PASS | `E6046EA05ACB337C4D86010741DAA2DBC94D6C437AC49692F1A8E6C003EA5FF6` | `5DF1F5D904F600B16D1F0FE7864189F40EDC4A85EEC6C6B8ADF21EDD3449302E` |
| ALL EditMode | 2740/2740 PASS | `F67B27ADC1DEC19BBC8C6557076A5E0B42BD110CE70C8BAAB9060FE11A747EFA` | `BFEC3873E0D914C5B9CFF91BBD7AFEB33AC06C9FE49928D23226E712A56C6194` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 PASS | `0742C6AE7E3F976192739AB5D1464D0B0BA5990EF2844A115D1C9E773B0870A7` | `F50EAC6F0056797249E26A1E22B659641B6AADB127EC0240E6F9FC3C9A222551` |

The successful runs used:

```powershell
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter SimulationBootstrapCompositionTests -ResultsDirectory docs/validation/P12GRegisteredOwnerSourceWitness/Focused
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -All -ResultsDirectory docs/validation/P12GRegisteredOwnerSourceWitness/AllEditMode
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter Smoke -ResultsDirectory docs/validation/P12GRegisteredOwnerSourceWitness/OfficialSmoke
git diff --check
```

The matching XML and compressed logs are stored beside this file. `git diff
--check` passed on the code and evidence changes. No unrelated `ProjectSettings`
or `.meta` files were included in this clean worktree.
