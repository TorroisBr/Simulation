# P12-G ActorChoice temporal target-owner rejection — exact-tree validation

## Candidate identity

- P12 canonical base: `1ba43992e9c35d80c2f31a0ecf8be67d7ba65267`.
- Test code commit: `f486d8f23366b10afbc9f069d4a9427b6499f9a2`.
- Candidate Git tree: `10ad6806c1b734121547d6117fd0f5eaa5d93cf6`.
- Candidate `Assets` tree: `a15ffd4409d928a71a53b72dac925194495e854c`.
- Changed code path: `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` only; no production behavior changed.
- Unity Editor: `6000.3.9f1`; runner: `Tools/UnityValidation/Invoke-UnityValidation.ps1`.

## Bounded evidence

A new case, `p12f-actor-choice-temporal-owner`, extends
`DailyV1RestoreRejectsCorruptedRootOrOwnerVectorAtomically`. It substitutes the
source session's `ActorChoiceTemporalCensusProvider` into the private staged
composition. Both source and target temporal witnesses have zero cardinality
and the same census revision, but the source provider has a distinct owner
identity. The test separately confirms the target's required P11 input row
still identifies the target ActorChoice store at that same revision. The
existing target sentinel therefore rejects the wrong temporal provider before
publication with its established `TargetOwnerVectorFailed` diagnostic.

The shared harness verifies source session/token/health/graph preservation,
continuation parity against an uninterrupted control, and successful valid
retry. This is test-only target identity rejection evidence; it adds no
ActorChoice payload, runtime wiring, or new profile scope.

## Validation

All gates ran after the final test edit and before the code commit. The code
commit's `Assets` tree equals the tested tree above.

| Gate | Result | XML | XML SHA-256 | Raw log SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|---|---|
| Focused `SimulationRuntimeAdmissionTests` | 125/125 | `EditMode-20261010-180535-57eb39a4f6dc400cb414a54b3287199b.xml` | `CC9DC68BED3D833D1CAA5B4CCFFAA9E7BB2A3D330FBE6455B3EFB973B1A1C367` | `654A6FFC1A235FB3A869D426DA87EBAFF3AC8AD1098CCE90091D9B95A748AD32` | `3F1FC7F482A35D8FD19DB71ACC5ECBAE0CBBB54AAF575FC29EA1563941D80244` |
| ALL EditMode | 2799/2799 | `EditMode-20261010-180602-1a65aedf741249f4aae3d07a822001e7.xml` | `C07ED61DE264DD41F9F9825D960CD964A5FDF07831E9DB709ED227D5BA28F8B3` | `6FF6375334AB04BDE8BBC85253BA1E68CDFBA84FD7E3C3B0BF035EC1F96BCA9D` | `F1F8134B9AC0905C5BB0D711E8508ED10D99F4648B9D70C6E59EFCE63D7748BB` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 | `EditMode-20261010-180646-5b1710634a514f779bb083fb121754d1.xml` | `1F2467AF5317E438E2D265141403E6031C29CF39214423D5A2172543BB1198BF` | `76140E732B31E3D73BCE32512D15D212B2CB4241E8E7B795AE3895B4C3F30E85` | `0377FE01F78D28842098278DF7F62E5AD9C41B49A8E6122CE99403C164013A7E` |
| `git diff --check` | PASS | `1ba4399..f486d8f` | — | — | — |

The focused XML explicitly reports the new temporal-owner case as `Passed`.
Each compressed log was decompressed and its SHA-256 matched the raw hash
listed above.

## Status boundary

P12-G remains `WAIT_DEPENDENCY`; P12-A remains `WAIT_DEPENDENCY`; P13 remains
`BLOCKED`; Phase 12 remains `OPEN`. This increment does not establish complete
target-owner coverage, P12-G completion, capture eligibility,
export/hydration, downstream readiness, or Phase closure.