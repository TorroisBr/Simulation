# P12-G global receipt target-owner rejection — exact-tree validation

## Candidate identity

- P12 canonical base: `78a90d4558b8bffd80fa9cfa1eda7297e2f909ef`.
- Test code commit: `6fc6533d5a0de8bd3bf72b79caac72257a055b3f`.
- Candidate Git tree: `0cc7156e74d37e77b9277f79c6e97951058dddfa`.
- Candidate `Assets` tree: `34da0c22f63cdd0f46cb3b2c3759392526821d31`.
- Changed code path: `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` only; production scripts and runtime behavior are unchanged.
- Unity Editor: `6000.3.9f1`; runner: `Tools/UnityValidation/Invoke-UnityValidation.ps1`.

## Bounded evidence

Two cases extend `DailyV1RestoreRejectsCorruptedRootOrOwnerVectorAtomically`:

- `p12f-decision-occurrence-receipt-owner` substitutes the private staged composition's decision-recorder owner with the source composition's valid empty owner.
- `p12e-economy-keyed-sale-receipt-owner` does the same for the keyed-sale receipt owner.

Each case confirms both owners are exact-zero at cardinality/revision `0/0`, the candidate runtime's target vector continues to identify its own installed receipt owner, and the composition's substituted witness identifies the distinct source owner. The shared restore path must reject with `TargetOwnerVectorFailed` before publication. The shared harness checks active source session/token/health/graph preservation, subsequent continuation parity against an uninterrupted control, and successful valid retry.

This is test-only target-owner identity coverage. It does not add runtime receipt wiring, claim broader owner/epoch completeness, or close P12-G.

## Validation

All three gates ran after the final test edit and before the code commit. The code commit's `Assets` tree matches the tested tree above.

| Gate | Result | XML | XML SHA-256 | Raw log SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|---|---|
| Focused `SimulationRuntimeAdmissionTests` | 124/124 | `EditMode-20261010-175743-cb9e6ba9bd1942a795f4f2c77c0cb815.xml` | `6735DC86EF873EADD71897218294A4BE87051A2F9E6E8BDEDD7882976E8F1B5B` | `9DD2B012BD48A384827B5FC329479B868A426CE7A508CE5129F07B1571DC8BCA` | `565FE480387D2AB7779A72386677465C3423C85CCABA07DE6DDAB1DF30A4FA0A` |
| ALL EditMode | 2798/2798 | `EditMode-20261010-175821-fb3c31d4180549fe8f226baa828298fa.xml` | `DB82001A3AA505B225E450088809989ECAE4F0423F590445BCD526EE6E8F16FF` | `B884136D1E88139BED6E17B8585177F6B375F071595687130082694DB7A39D72` | `616F6433B2FA2D1BCB64F8D2231100471E7A8637F64FE24B304550663120B485` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 | `EditMode-20261010-175912-a38faf830e704077be9160128f551d7a.xml` | `575C60E6C50342FD9818B4F139CB563DC1C080220622CD17CF54DD800CF54B7D` | `5E5289B62C776CBBA9B1CE492CD09320D5B524C5DAAEC2D3EC2DE46B4CC64D04` | `E14D92CE71E3A01DE718A11FFAC888B3036F41A92E4EADFC14863897CE9EDA98` |
| `git diff --check` | PASS | `78a90d4..6fc6533` | — | — | — |

Each compressed log was decompressed and its SHA-256 matched the raw-log hash shown above. Both new parameterized test cases are explicitly `Passed` in the focused XML.

## Status boundary

P12-G remains `WAIT_DEPENDENCY`; P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains `OPEN`. This checkpoint does not claim complete target census, P12-G completion, capture eligibility, export/hydration, downstream readiness, or Phase closure.