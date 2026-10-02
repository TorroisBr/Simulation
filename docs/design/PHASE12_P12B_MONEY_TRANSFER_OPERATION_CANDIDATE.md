# P12-B NPC money-transfer operation implementation candidate

**Status:** promoted to P12 canonical at `2f2b731866aea86eb52ef2b51eb687c88bf91bc4`
after independent exact-tip review, recorded at
`09f9f49ef85faf5c24eae57acba4426ca0bc39f8`. This is part of accepted P12-B
prerequisite work and adds no new checkpoint ID or product behavior.

| Identity | Value |
|---|---|
| Candidate branch | `codex/phase12/P12BMoneyTransferImplementation` |
| Canonical base | `codex/phase12/canonical` at `53989ee940e0dc0b22492873ebaffb2cd02f8f58` |
| Integrated reviewed design | `ea1d5b58ab8c5204c7559d13be10034e3a8732eb`, exact-tip review `74dc9b5360fb356eca58874878942ee90b786fd8` |
| Design integration parent | `3189e5c91b4192fa0008f2db5c0143340c6e369a` |
| Code-bearing commit | `66415aefe6834fc73e9e6c3b22fe0ee3058cfa11` |
| Code tree | `862bceb6164bf5ddeed49ca8007d9be3ed4670d6` |
| Architecture baseline | `451340c56e9b676bf6ea43412bcb856b9ccde3de` |

## Delivered boundary

The selected P12 runtime registers stable operation ID
`runtime.economy.money-transfer`. Before entering it, the NPC overload keeps
the existing no-write amount/account preflight, then resolves each participant
to the exact roster NPC and exact per-NPC MoneyAccount provider/instance,
section, schema, cardinality, and local-revision baseline. The resulting scope
covers the existing debit, destination credit, any successful source refund,
and result selection, and is disposed on every return path.

The already-promoted MoneyAccount owner hooks remain the sole commit
notification path, so each committed local revision advances only its own
section once while this operation is active. The P12-bound raw-account overload
fails before writes; the unbound standalone raw overload remains available.
Finite zero-value transfers between distinct valid accounts still succeed
without local revision or epoch changes. The existing `CrimeSystem` calls the
NPC overload; its theft outcome and any reverse transfer remain separate
operations and outside this slice.

Implementation changes are limited to `EconomyTransactionService.cs`,
`SimulationRuntime.cs`, and `SimulationRuntimeAdmissionTests.cs`. The full
candidate diff from the canonical base also carries the reviewed technical
design and review record listed above. This does not claim P12-B completion,
complete owner or operation coverage, universal shared-epoch coverage,
capture eligibility, export/hydration, P12-A readiness, or P13 readiness.

## Exact-tree validation

All suites below passed against code tree `862bceb6164bf5ddeed49ca8007d9be3ed4670d6`.
The Unity harness wrote into `Temp/ValidationResults`; each XML/log was copied
immediately to the local candidate archive
`Library/ValidationResults/P12BMoneyTransferImplementation/` because Unity
clears its Temp results on the next launch. These ignored local artifacts are
retained for independent review and are identified by SHA-256 below.

| Gate | Result | XML SHA-256 | Log SHA-256 | Artifact stem |
|---|---:|---|---|---|
| `SimulationRuntimeAdmissionTests` | 28/28 | `9B6A4C815DB77297F55A41AA09A6D92DA8ABE80615D483552593BEDB4FEE729E` | `B415BDE978ACA6125850A411C08761F8AA8D14E2B33CB7D836EAA84C3746E3D0` | `EditMode-20261002-035144-d2d2fc1fa2e34578a87f49aaeda8920d` |
| `EconomyTransactionTests` | 45/45 | `BDAAA5C688CB57ECE1E94515A77B58A330B50146807BD0671942E6E597172980` | `44F839C841048A93C3FE0B83D3EE59B5CC3ECD5F4129053A32F99E5CA489A08C` | `EditMode-20261002-035208-a6817ef50cd64e61ad255a32c54e98ec` |
| `CrimeSocialAppraisalIntegrationTests` | 12/12 | `4BA4E6BC1C9C5DF4F3F0260F997C8A6941EE40372D9C475E4E8C89A7E490A274` | `D62E77AC88BF3FBA2D55751B5A5302F944588B3219C907D0A9D0E3272293D2CB` | `EditMode-20261002-035232-1fba5db9a5124ee387c13dc40f66fe99` |
| ALL EditMode | 2152/2152 | `3DB31897180BF052D546B1665B959F18D3EC3A683CDBFCC5364537E321B5CE27` | `03BFDCBA60BEE310CB60CB3993CEE9A73D0A4ABB89C279AE2290FD6120F2FB21` | `EditMode-20261002-035256-854f8689ee824a7ca209f498770a8f41` |
| Official EditMode `Smoke` | 5/5 | `1FA9289BC72B95D0775EFDF5A148297CFE3E9A1227DC877DAB711B54C706FC70` | `E8F9C3CB91CE65090398B4D6186195AC82E3A3C80E17EAECBD3B3DCD3389FD0F` | `EditMode-20261002-035344-904217fce373478a8f46cca217695566` |
| `git diff --check 53989ee..66415ae` | PASS | — | — | — |

The focused tests cover both exact account witnesses and in-scope owner
notifications; successful compensation as a second source revision within the
same scope; finite zero/no-op and invalid-amount behavior; P12-bound raw-route
rejection; stale roster identity and replaced-account rejection; and
wrong-thread admission before writes. Existing transaction and Crime tests
remain green.

## Integration limits

The candidate was fast-forwarded onto P12 canonical from `53989ee` after the
review and approval gates passed. The exact reviewed code tree is unchanged.
Preserve the existing State limits: P12-B incomplete, P12-A
`WAIT_DEPENDENCY`, and P13 blocked. No other Economy transaction family,
crime outcome, City account, merchant, travel, Justice, epoch, capture, or
continuation capability is included.
