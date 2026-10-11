# P12-G ActorChoice Duplicate Command Idempotency Validation

## Candidate

- Canonical base: `33bf813e50c36b7aeb22dbcb3602d0805af17682`.
- Implementation commit: `2b5f00e99b1a76279135dcce1847584743ff79ce`.
- Git tree: `6b6ff1351fff9f95981d8d1b823fee2aebc22c8c`.
- Assets tree: `baaac7f7b5294e1bd4201a610e841bc020873a3a`.
- Changed implementation path: `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` only.

The test extends an existing Daily-v1 restore/continuation case. It queues a terminal ActorChoice through the existing trusted LocalPlayer/Request handler, restores it, and submits the same command ID and payload through that handler. The duplicate is rejected before it creates another input. Assertions cover terminal identity/disposition, input count, local revision, next input sequence, selected facts, the complete included owner projection, continuation roots, and the existing two-boundary continuation parity. The fixture intentionally leaves the Person unmaterialized, so the original command terminates as `ActionUnavailable`.

This is test-only. It does not prove replay of a successful SellGoods command, generalize idempotency to every command kind, change production semantics, or close P12-G's wider no-replay, owner/epoch, quiescence, or parity obligations.

## Validation

Official runner: `Tools/UnityValidation/Invoke-UnityValidation.ps1`, EditMode, on the candidate worktree. XML reports parse with zero failed, skipped, and inconclusive tests. Each compressed log was decompressed and its raw SHA-256 verified before the raw log was removed.

| Gate | Result | XML SHA-256 | Compressed log SHA-256 | Raw log SHA-256 |
|---|---:|---|---|---|
| Focused duplicate ActorChoice case | 1/1 PASS (`EditMode-20261011-005633-542880c4194046a89b4e967ceb9ca055.xml`) | `D50B3AA0BB1F71E246CB9B52B239627C467E2351297F61434C2C8C31BFBDED4C` | `96BD6C640278E04C55902E7965FF9CC475DF3E3658619DC8CA74E09E2485EE33` | `27246F99FFBDD1A3EC599D2DC06BEC4C40B2944AF2E8867822AC01F86A06487E` |
| `SimulationRuntimeAdmissionTests` (`EditMode-20261011-005716-0984f73a12db47df9ba07151c262b14b.xml`) | 185/185 PASS | `578A9C5180A1361162D1465EC77C1699DBC366AFB41A648D75091A04E0CBF4B8` | `53C40765836F4B189CD475676B56FC83E78C15F306766102846205F6A468F57E` | `C3648B5E04A84473275B07C3D6EA4050AB12FD32E940AE3866D43C97AA085C82` |
| ALL EditMode (`EditMode-20261011-005750-5a2050f02dc042f3934de55c4ee75c9b.xml`) | 2859/2859 PASS | `A3590D636229B23BBF5A79D9AF8EFA8B24FE54E2FCEA2CF4C09B8F3C6C5CA58E` | `64DF02E1D2E36AE64487B7336DBF7CD36BED93C2EC3EBED3F926BB2ECAC1768F` | `A320C3D9C21CADC561A4BA5F16EC2FF0F4C1B9CF701AB1B7CD0D79D486E3777B`
| Official Smoke (`-TestFilter Smoke`; `EditMode-20261011-005912-39965672a6b74d1898125fd676bf3475.xml`) | 5/5 PASS | `7A371A8AB4A53705F12C41E5F624F191D4B7E7ED2DA9A1767CD4BF29E9A5D3F9` | `07F3FDA187E0594F89A41F184645C2BADB86B7A2CBC434B90E305546247F7552` | `B8BF344B87D2C1ED766D1B7F3820270D5A396DA089FC814E74885722AC84AEF9` |

`git diff --check 33bf813e50c36b7aeb22dbcb3602d0805af17682..2b5f00e99b1a76279135dcce1847584743ff79ce`: PASS.

The run used the reused candidate worktree while the separate user-owned `ProjectSettings` edits and untracked `.meta` files were present; they were not changed or included in the candidate. This evidence validates the candidate test source in that existing project configuration. The reports do not establish P12-G completion, exhaustive owner/epoch coverage, runtime-wide quiescence, general no-replay behavior, P12-A/P13 readiness, or Phase closure.
