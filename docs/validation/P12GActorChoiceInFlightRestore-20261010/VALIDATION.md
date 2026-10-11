# P12-G in-flight ActorChoice restore rejection validation

## Candidate and scope

- Canonical base: `d3bddfd7a4ffb00fbccfb22c84ee62c49380e71f`.
- Implementation commit: `617932280bfa60297f0a617faf2a1ad344d89293`.
- Git tree: `8ff334ad6a2e3f35498bea959d5731874c0a6f86`.
- Assets tree: `0fab41ff0d654c6607c4f6d144bb66c6cbaa3463`.
- Changed implementation path: `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` only.

The parameterized integrated restore case starts from a valid terminal SellGoods `AttemptReturned/Succeeded` source boundary. Through the existing null-default detached-snapshot test seam, it separately substitutes (1) a structurally valid `Pending` input with no lifecycle dispositions and (2) a structurally valid `ConsumedAwaitingTerminalAttempt` input with its existing `DispatchStarted` disposition prefix. Both retain the captured input, command, Person, action, sequence, day, snapshot schema, census revision, and next-sequence identities.

The normal `ActorChoiceStore.TryCreateP12FStaged` rejection occurs before `FActorChoicesStaged`, `FStaged`, and publication. Each case verifies the active source session and exact completed-boundary token remain installed and valid, source health/facts/roots/included-owner projection remain unchanged, the source continues in parity with an uninterrupted control, the unmodified retry succeeds, and the next completed boundary remains in parity. No production behavior or domain semantics changed.

This closes integrated F-hydrator rejection evidence for the two existing in-flight states only. It does not establish general causal no-replay, complete §6 evidence, P12-G readiness/completion, capture eligibility, P12-A export/hydration, P13 readiness, or Phase closure.

## Validation

Unity Editor: `6000.3.9f1` (`7a9955a4f2fa`). Validation used `Tools/UnityValidation/Invoke-UnityValidation.ps1` on implementation commit `61793228` / Assets tree `0fab41ff`. All reports have zero failures, skips, or inconclusive cases. XML, compressed logs, and decompressed raw logs are retained or referenced by SHA-256 below.

| Gate | Result | XML | XML SHA-256 | Compressed log | Compressed SHA-256 | Raw log SHA-256 |
|---|---:|---|---|---|---|---|
| Integrated Pending + Consumed cases | 2/2 PASS | [`XML`](results/focused/EditMode-20261011-014315-32c66fcf8c8d46779ac385f88f64768a.xml) | `558CDEF66F7405E9E4B4FEB861D19616D7CB33895252266BC0A44FC035D63FE3` | [`log.gz`](results/focused/EditMode-20261011-014315-32c66fcf8c8d46779ac385f88f64768a.log.gz) | `C580BD1E729AE504133404E70FC0543B244A26C50BEF009BB1CF71A455A55A8A` | `829A72E1E98FA47C3F1B72ED1873B02FF0A1408C0DC55B7CE11239A55AA973DB` |
| `SimulationRuntimeAdmissionTests` | 188/188 PASS | [`XML`](results/admission/EditMode-20261011-014339-2f50329ce1a1499581d9630236c3336b.xml) | `BC9F441D9F9F56D2D3B0337FD81B91E5B90339C753511037705FE7BD50E4CBED` | [`log.gz`](results/admission/EditMode-20261011-014339-2f50329ce1a1499581d9630236c3336b.log.gz) | `2D5DCEAD3FBE150AAF29AF728DECC36D713CB5A2BFFE865B8C6A16AE61EDEFC3` | `65A6A53C073A0584C125419BF120E5A42F8DDA528502D16D9D3AF07FDE866F87` |
| ALL EditMode | 2862/2862 PASS | [`XML`](results/all-editmode/EditMode-20261011-014419-8bc25bfdb18b4f849a7b9dfd7cf9369a.xml) | `CBB02E44AA3C8C2B767CFE8E90FB5E235500C19DC1882FDF41D5C5C6F2831093` | [`log.gz`](results/all-editmode/EditMode-20261011-014419-8bc25bfdb18b4f849a7b9dfd7cf9369a.log.gz) | `C41A04E4882F5ECE4A07885A77E5E12E5A4F774E0002345FFFAE08ED285D6D5D` | `BB1D73972EAF4857D29EA391703128D930B54A5DE896C8755A612E9E1758BF65` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 PASS | [`XML`](results/smoke/EditMode-20261011-014518-4e70d100a04242ee9a46abff501161d1.xml) | `3DBEBC78717CBCCA11553757F1352B85A52FCBCD83BE29495A520F2615CD1DE4` | [`log.gz`](results/smoke/EditMode-20261011-014518-4e70d100a04242ee9a46abff501161d1.log.gz) | `CA9718E457177056D26F2DAEED4044D737A6711ECCF151756BC3E09B2C45D833` | `EF48FA67735452BCB5CA818BC957DBD090F47DF67334E237820CD1604CEDB862` |

`git diff --check d3bddfd7a4ffb00fbccfb22c84ee62c49380e71f..617932280bfa60297f0a617faf2a1ad344d89293` passed. No long-run validation was required because this changes only test evidence and introduces no daily-loop/domain behavior. Protected ProjectSettings edits and unrelated untracked `.meta` files were not staged.
