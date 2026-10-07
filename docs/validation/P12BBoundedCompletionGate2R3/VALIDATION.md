# P12-B bounded completion — Gate 2 R3 validation

**Date:** 2026-10-07
**Unity:** 6000.3.9f1 (7a9955a4f2fa)
**P12 canonical base:** 94551b08be8cc9347de35eae5051b8e578ea4c1e
**Architecture canonical:** 47eff220c7ce00f6e7c759bdc2b76780bb46f628
**Code candidate:** bb887989ac96c7bbf405bb9d9b3d9f904d4098b1
**Candidate tree:** c61aa4a0f63c244044bde97878c66a44444e42fb
**Validated Assets subtree:** 3f3f0971129004635b9de3bfb5260ab3918d6233

## Scope and correction

This exact-source rerun includes two Daily-v1 admitted-profile boundary cases requested by independent review R1: a valid token at `long.MaxValue` survives a clean `AbsoluteDayOverflow` preflight unchanged, and a three-day batch starting at `long.MaxValue - 2` completes two cores, returns false on the third, reports two completed days/sequence increments, and publishes no token. The initial review and gap are recorded at `docs/design/PHASE12_P12B_BOUNDED_COMPLETION_IMPLEMENTATION_REVIEW_R1.md`.

The bounded profile and scope remain unchanged. The accepted 275-section pre-Gate-1 inventory is reconciled by three fixed existing-owner witnesses: formula 68 + 20*N + U + P, or 278 at the authored baseline N=10, U=10, P=0. P12-C allocator-root scope, Knowledge/Expedition execution, P12-A, P13, and Phase closure remain excluded.

## Exact validated source

Author and isolated validator worktrees were byte-identical for all nine listed files. SHA-256 values:

| File | SHA-256 |
|---|---|
| `Assets/_Project/Scripts/ContinuationCensusProtocol.cs` | `72c5772daaf213d16fd6627e9df70cdb94e43b14cf3e2788dc542d23b4a6fea2` |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `4745002d029baf1c9af8adf7f261168d4b345d35f439763c4ab6974228b336a7` |
| `Assets/_Project/Tests/EditMode/Editor/ContinuationCensusProtocolTests.cs` | `e4321e853b5a34f45d083e0f8bbbc1ea98dc91b922a40f0c2ba1e610b2d21120` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `d5a0b62fdd36f63b78d9104118e79138aec4716380e39648fa5f8f97b1b42312` |
| `Assets/_Project/Tests/EditMode/Editor/P12CrimeJusticeInvalidationTests.cs` | `6d34f04c0aae06cbe954bd3c7e296341d8b3fe70312326f16aa4c5410bb524c5` |
| `Assets/_Project/Tests/EditMode/Editor/P12PopulationLifecycleInvalidationTests.cs` | `6bd4d08203528eeffd91cdd17b2ec9f44f9f4052d0e54fd32b2c47f915cbfede` |
| `Assets/_Project/Tests/EditMode/Editor/P12SoloTravelStartOperationTests.cs` | `9ce294523eebf09cf67bb22cb1c200df441fee193f7c36090a8d5adebaeaccb0` |
| `Assets/_Project/Tests/EditMode/Editor/P12TravelPartyAdvanceTests.cs` | `bcf02e91cdcdf218cbd9ef03d1fd9753db167e7c48b48dce63e17d2ac07566eb` |
| `Assets/_Project/Tests/EditMode/Editor/PropertyEstateMutationEpochTests.cs` | `b56bda57368c68049b49671a74e81880024f8480ac139b2abdd9a4129b89ebe4` |

## Results

Every XML reports zero failed, skipped, or inconclusive tests. The archived logs are SHA-256 bound below.

| Suite | Result | XML SHA-256 | Archived log | Log SHA-256 |
|---|---:|---|---|---|
| ContinuationCensusProtocolTests | 24/24 PASS | `a0e46d3d42dc0ec3c36fe4f6696cda9735fd728ba8016441c9d0ee8aab01c3fc` | `EditMode-20261007-232656-ffe19aa05b7e42c7945d5104920e146d.log` | `1519ece194187f2324a5713e73deb00d9ad5ce738965eb5bbfa0696864d0e329` |
| SimulationRuntimeAdmissionTests | 70/70 PASS | `b8e241282648b04ea2bc6ecd355610321dabaa2488ac61442a6c6d177e613311` | `EditMode-20261007-232707-119a0ac1a7f142a1bc66e5125c56ad94.log` | `f29d21d392bee0defbc34ab323516f8793ca5e2bce3c51f3d6759d0a26da612a` |
| GenealogyCensusTests | 7/7 PASS | `cd2026a6180e3f226a5703c1381b7556b5e45b21862ab2046dc19e6a1625a72e` | `EditMode-20261007-232718-c555f7bbbeb94da1ac179642dcc4201a.log` | `fb9a5dd2728f5238546cf45a1e180630fc906c1b4247624b7cd6099336da4850` |
| SimulationBootstrapCompositionTests | 25/25 PASS | `438d09346e0f71c47dbd509a807fa3b6eed441c17c4a42bd5f57f2d910a232f0` | `EditMode-20261007-232728-0a845700f53b47c98e40ab42ff84e49c.log` | `599057f278ca7bc03a272400f532fbda901de1e9fa1e3e00513742862da38980` |
| P12CrimeJusticeInvalidationTests | 12/12 PASS | `940d420d20a0b78e825fee1386f2bbaa44d812f9a9b04634340186c2d9bcaa01` | `EditMode-20261007-232739-ac15324bfb804050aa7863743e86a4d8.log` | `b3a66f925be7e9a24dfb287983cb54a8950773749afcb024870156ecb6723846` |
| P12PopulationLifecycleInvalidationTests | 20/20 PASS | `e0ead46f8e0a50af12daa6138b412f22446f9b267ff8fb6929a10f9e93f1779d` | `EditMode-20261007-232749-87ee59c4b5e44b949bffbab3272879c7.log` | `e7ad9a82c451ce9c98bf93d0c83b425d21b29dee44423ed383934b63e8ae8a6b` |
| P12SoloTravelStartOperationTests | 11/11 PASS | `3ec23290b8684b3b104d806f94c4930b015c5bdb9332d5142211ad88a65b7ebe` | `EditMode-20261007-232800-908a4c2a712c441f936a0eabcca7b5ef.log` | `f8da7d4bb300ef9da5338397559da661763bee2a5726f0ac43eb8daba8ccba9a` |
| P12TravelPartyAdvanceTests | 10/10 PASS | `a57e8c6967a37fa478207e58c98da67b8f815afcee91b84169290902f2d73c00` | `EditMode-20261007-232810-16594bb769ea4475853f520ab6bb9a81.log` | `539c9a7684fa34a025580aac19c0ab1ffaa5dcabc68643424c950c6fd8c328d5` |
| PropertyEstateMutationEpochTests | 5/5 PASS | `b650565e53e47010a6a9c2eb1db8e3661cb75fe5ea83ca0ee7eca50f50d4b793` | `EditMode-20261007-232820-c02c94780b9140b9b3f7adb4caf0e906.log` | `0e4e7bb934c823b6d529801dc15b05182c3d911a3e109c377eef528ad8a1100a` |
| AllEditMode | 2460/2460 PASS | `7a0f9f20f79f959e6a94b5c2cd1213d64340a59d6d9ae22db2442b1a14ab3e8b` | `EditMode-20261007-232830-6ec1e2ce108447c68159978cc2745115.log` | `170aef54b544b92f981e9af4cf58bc3c2096eb953f06fef64e4dd4f2229a2ee3` |
| OfficialSmoke | 5/5 PASS | `83aa53443d589f8f8b4978f3d4968cd2afb0daf5f64a4b06212b92dfb375a437` | `EditMode-20261007-232904-c3a0b427f41c4502af3b3a991623579a.log` | `4c24857fce2f47c3a862ae852c9824ae9c7be9b941b25a6f3f20f348a4f4d739` |
| SimulationRuntimeLongRun | 7/7 PASS | `39a71a429d9807ce2672534dc1269921637760e6c29a141561aa231833f296e7` | `EditMode-20261007-232925-e1d4f74709554f9fb65cca607502b50a.log` | `c514ec3a8b2c9ac57b0e92ffdfb0fc8991cb2e2a81334cea966f5287eca249fa` |

RawLogs.zip SHA-256: `65476fde292afc368cd0d9e400b810c12f8a9ddb78d19d8cbed18048d735477f`.

Nine focused suites, ALL EditMode, official Smoke, and SimulationRuntimeLongRunTests passed on the exact source set above. `git diff --check` passed on the implementation diff against the P12 canonical base. This validation does not substitute for a fresh exact-tip independent implementation review or canonical promotion preflight.
