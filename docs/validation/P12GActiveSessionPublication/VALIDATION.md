# P12-G Active-Session Publication Validation

## Latest exact-tree validation

Implementation candidate: `c5f80a1aaeedbba00e713829a2479544ad50f340`
Candidate Git tree: `e158a38add785c2fbc80f3529f0c6869fa898730`
Candidate `Assets` tree: `a253d7425c9d482da136d0fca8acd17b2445870b`
Base canonical: `8f5ec2cb9211a6520f31e65e83450d5bd9bfb039`

The added `RestoredSessionExchangeWaitsForIdleWindowAndSwitchesActiveAliases`
test closes the earlier review gap. It rejects publication while a source
operation is active, then verifies successful idle-boundary exchange, active
consumer aliases, spatial lookup through the candidate, and preservation of
the detached source boundary. The test source was unchanged between the
validation runs and implementation commit; the validated `Assets` content is
the tree recorded above.

| Validation | Result | XML | XML SHA-256 | Archived log | Log SHA-256 |
|---|---:|---|---|---|---|
| `SimulationRuntimeAdmissionTests` | 76/76 | `Raw/Focused-Admission-Exchange-Pass/EditMode-20261010-000954-d10c3918640945eb868ad0bfe097090a.xml` | `B28FFA4FF015349419EBEC850AA6595D077CA2DF20969200F2A0773A2F395D2C` | `EditMode-20261010-000954-d10c3918640945eb868ad0bfe097090a.log` | `328FF76A146BBB3C04FBA3B171CCBD0BF3C4A1304E03B7427AE3DDB148950413` |
| `SimulationBootstrapCompositionTests` | 26/26 | `Raw/Focused-Bootstrap-ExactTree/EditMode-20261010-001013-aa2eb0a64e6f455fb5c4a13ae9f78603.xml` | `E288041244DAD8A5715AC5A87721685FB43C752A6AF0993875B2A70E95A41EA7` | `EditMode-20261010-001013-aa2eb0a64e6f455fb5c4a13ae9f78603.log` | `9E05DD7336C7046597F49C0D072635A3E6DA5AE9FD94756A8967D6ED55521B1C` |
| `P10BGeneratedRuinGenesisTests` | 10/10 | `Raw/Focused-P10B-ExactTree/EditMode-20261010-001025-7c151de54e2e4736b6940d955a4653c0.xml` | `48BE66FEAB6CB1829319E394746E6E71D32662680C873DAAE43BEB7C4DA3F33C` | `EditMode-20261010-001025-7c151de54e2e4736b6940d955a4653c0.log` | `16EF7E7E73B8ED6461AB1B569E36ED84212C69D30741F173F52199F3FA7115E4` |
| `P12CrimeSocialAppraisalInvalidationTests` | 12/12 | `Raw/Focused-CrimeSocial-ExactTree/EditMode-20261010-001037-0ddac55fa06748978413eda820e3a7bc.xml` | `29F77678AB6CCA8CD27FBA1253486201F0F773734ACB72242DEDF5B147E0F607` | `EditMode-20261010-001037-0ddac55fa06748978413eda820e3a7bc.log` | `BEA93FA88315826359C010A7C905522153F6799674EE3A02170B20244A734A5E` |
| ALL EditMode | 2739/2739 | `Raw/All-EditMode-Exchange/EditMode-20261010-001049-241341553b4e47fd95c9d80c9b0648d2.xml` | `763349FC32F58E3E5581B9742AF81D2C5F32A2DFAFF66AC4C0AD2A67D97B6325` | `EditMode-20261010-001049-241341553b4e47fd95c9d80c9b0648d2.log` | `B22B88AFF44600F27E9B5AA2189AA9CEC0CB38AFCABBE88F47157D4369394943` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 | `Raw/Official-Smoke-Exchange/EditMode-20261010-001122-74920b3bbd4b4964bbe5bc20be840c9b.xml` | `F7210B0C19E11DE9A57D797C034940FEBF18CF31D67AD620130DB2BD17D9C5E2` | `EditMode-20261010-001122-74920b3bbd4b4964bbe5bc20be840c9b.log` | `C813094793AFFF9500B8EA71F8C2BFC9069EBF5374A52275CE7B35C629A5590D` |

The six passing Unity logs are archived in `p12g-unity-logs-exchange.zip`
(SHA-256 `D042CED2F871D233824A6EFE197A4A2E8A7093F6EA0CFE2A88F419643FCFC5F5`).
Unity emits trailing whitespace in raw logs, so the archive is tracked instead
of committing those logs as plain text. The original local logs remain
unchanged. `git diff --check` passed for the candidate patch.

The initial compile-failing attempt for the added test remains in `Raw/` as
diagnostic history and is excluded from the passing evidence above.

## Previous implementation validation

Before the successful exchange test was added, implementation candidate
`02d2d40d197985085c2d2370ebc1b26d9b0dbec5` had focused admission 75/75,
bootstrap composition 26/26, P10-B 10/10, Crime/Social 12/12, ALL EditMode
2738/2738, and Smoke 5/5. Those XMLs and their archived logs remain retained
as historical evidence; the exact-tree table above is the validation for the
updated test candidate.

## Scope boundary

This candidate provides one active-session reference containing the current
composition and runtime/reporting aliases, routes published consumers through
that reference, releases bootstrap aliases after publication, and adds a
serialized owner-thread exchange seam for a separately built restored
Daily-v1 session. It does not implement a restore coordinator, full graph
staging/validation, export/hydration, whole-graph failure atomicity, no-replay
or continuation-parity proof, complete owner coverage, P12-G readiness, P12-A
readiness, P13 readiness, or Phase 12 closure.
