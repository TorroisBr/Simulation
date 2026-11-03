# CORE-F restored-boundary quiescence validation

## Scope

This candidate closes the restored-target admission gap found in the exact-tip
review of `a8070ad79748ffe2c63e8a101fdf9e3f239b75a0`. Core now checks that the
selected runtime census protocol is ready, healthy, owner-thread-bound, and has
no active Core operation scopes before it binds the restored candidate's
mutation guard. The Unity regression creates a failed, unscoped
`NpcMembership` context and verifies that restoration returns
`TargetAdmissionFailed` without binding that guard or replacing the source
session.

This is a bounded CORE-F integration correction. It does not complete the
Daily-v1 application/host contract and is not a canonical promotion.

## Validated source identity

The official runners captured the candidate worktree at base commit
`a8070ad79748ffe2c63e8a101fdf9e3f239b75a`. Their before/after input snapshots
were stable. The exact validated source-file hashes were:

| Repository path | Bytes | SHA-256 |
| --- | ---: | --- |
| `Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs` | 115975 | `d9a9fd36a64851fdac99632810fd276002ba2fcd27ab39675d5fafc71aa658c0` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs` | 171668 | `3ef38f8103911fb36336b6fa8558a6d4b45efe4227ba137176b5ca4e0e73d497` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | 1064366 | `28d112517f64054c27bf7e0e3df877f5d3392707c5912b4decfb77f1051e5631` |

The two pre-existing local `ProjectSettings` edits were excluded from the
candidate and left untouched. The protected runner manifests retain their
input hashes, including those local files. The documentation record was added
after execution and is not a validation input.

## Validation

| Gate | Result | Run ID | Manifest SHA-256 |
| --- | --- | --- | --- |
| CoreValidation cold/warm | 1002/1002 each; PASS | `920e275265de46fdac965b98b3a12191` | `ebe2eea84c285cef482f704c884a38377facb1ba7ee679cab80c2aecd490bae4` |
| Focused restored-boundary regression | 1/1; PASS | `24496a2a92794610aedbbb140ccf29fc` | `119b759948a8a43e9b1677756541e89237646c9b68644fb2372b69a24da043eb` |
| Full Unity EditMode | 3298/3298; PASS | `f52c908885274f9e9ac3340276964758` | `60e627148ce83b6873867b89b2e8bf32ebc898bd482f485386723e4b7c6a3b38` |
| Official Smoke | 5/5; PASS | `2bf54835d96644c2a9b1910aba1b0249` | `192a47bb2c36a0dd340d4f77fa239c02f0d862e7cb0983cf8a9a9858cc269071` |
| `git diff --check` | PASS | — | — |

The full EditMode report SHA-256 is
`425850d44ec87766b28edb2d7e212d50c9e674f79fddc9ca106767ed8dc387b8`; the
Smoke report SHA-256 is
`0593771fb0c513dac2a7be803c409f35eaa2fa99e1a2542d26a86c4967d164b0`. The
focused report SHA-256 is
`1390343390fe1bcdd2281c6dafbfc2cd60c68e17ea7039f26d47d7582169c336`. Original
reports and logs remain in the official owner-only evidence store; they were
not added to the repository.

## Diagnostic attempt and disposition

The first full EditMode attempt, `d418cf4b79cc490fac3084c5aa36a726`, reported
3297/3298 passed. Its new test called the legacy restore overload without the
Core application reservation, so it stopped in legacy target binding and did
not exercise the reviewed Core finalization boundary. The test was corrected
to use the active `DailyV1Application` and its exact exclusive replacement
reservation. The corrected focused test, full EditMode run, and Smoke run all
passed against the hashes above. The failed attempt is preserved as diagnostic
evidence and is not counted as a passing gate.
