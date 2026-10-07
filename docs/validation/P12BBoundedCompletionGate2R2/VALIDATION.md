# P12-B bounded completion — Gate 2 R2 validation

**Date:** 2026-10-07
**Unity:** 6000.3.9f1 (7a9955a4f2fa)
**P12 canonical base:** 94551b08be8cc9347de35eae5051b8e578ea4c1e
**Architecture canonical:** 47eff220c7ce00f6e7c759bdc2b76780bb46f628
**Code candidate:** d6988c966288257bcc9ca2b79b264c634d6a28ab
**Candidate tree:** 91a36918b354a42d7645f942694b61de1cc90d2
**Validated Assets subtree:** a39501b1b17e2160144f5692e8fede7f93740235

## Scope

This rerun validates the exact pushed P12-B bounded completion candidate after adding final-census, disposal-fault, witness-tamper, and every-registered-operation-scope probes. It covers the accepted Daily-v1 profile without widening profile scope. P12-C allocator state remains deferred; Knowledge and Expedition execution are excluded. P12-B remains incomplete until independent exact-tip review and canonical promotion pass.

The accepted handoff has a 275-section pre-Gate-1 inventory. Gate 1 registers three fixed owner witnesses for existing composed owners, making the validated formula 68 + 20*N + U + P and authored Daily-v1 count 278 (N=10, U=10, P=0). This reconciles registration of existing owners; it adds no profile scope.

## Exact validated source

The validation worktree source files were byte-identical to the candidate worktree for all nine files below. SHA-256 values:

| File | SHA-256 |
|---|---|
| `Assets/_Project/Scripts/ContinuationCensusProtocol.cs` | `72c5772daaf213d16fd6627e9df70cdb94e43b14cf3e2788dc542d23b4a6fea2` |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `4745002d029baf1c9af8adf7f261168d4b345d35f439763c4ab6974228b336a7` |
| `Assets/_Project/Tests/EditMode/Editor/ContinuationCensusProtocolTests.cs` | `e4321e853b5a34f45d083e0f8bbbc1ea98dc91b922a40f0c2ba1e610b2d21120` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `bd0b5b7b741793d9d8692afb99d20f9c2beacbc3e74420d1e3f69a8cf55dc8ec` |
| `Assets/_Project/Tests/EditMode/Editor/P12CrimeJusticeInvalidationTests.cs` | `6d34f04c0aae06cbe954bd3c7e296341d8b3fe70312326f16aa4c5410bb524c5` |
| `Assets/_Project/Tests/EditMode/Editor/P12PopulationLifecycleInvalidationTests.cs` | `6bd4d08203528eeffd91cdd17b2ec9f44f9f4052d0e54fd32b2c47f915cbfede` |
| `Assets/_Project/Tests/EditMode/Editor/P12SoloTravelStartOperationTests.cs` | `9ce294523eebf09cf67bb22cb1c200df441fee193f7c36090a8d5adebaeaccb0` |
| `Assets/_Project/Tests/EditMode/Editor/P12TravelPartyAdvanceTests.cs` | `bcf02e91cdcdf218cbd9ef03d1fd9753db167e7c48b48dce63e17d2ac07566eb` |
| `Assets/_Project/Tests/EditMode/Editor/PropertyEstateMutationEpochTests.cs` | `b56bda57368c68049b49671a74e81880024f8480ac139b2abdd9a4129b89ebe4` |

## Results

Each retained XML reports zero failed, skipped, and inconclusive tests. XML and raw log hashes bind these reports to the archived run outputs.

| Suite | Result | XML SHA-256 | Archived log | Log SHA-256 |
|---|---:|---|---|---|
| ContinuationCensusProtocolTests | 24/24 PASS | `e0b9a579d1cb1f160b835f05cb24a54453795c25c230bd971d02ec3b2c5291f3` | `EditMode-20261007-231312-64ee40fe15d7423fbfe7d0aa9c48f55b.log` | `bd675baa8c445a2431316c2d3cec6ea1191abb75b6b568f75332b3fed500ce05` |
| SimulationRuntimeAdmissionTests | 68/68 PASS | `ddaf18f7687516be415301bfb0ab95241f161fc75547c302e61b75056762806a` | `EditMode-20261007-231321-9a9c0610f9314282b804b23bca243b82.log` | `4b30a7cccff20f50f2ce510eb6cf10f78224285bee3f12676ded30821bddc1bc` |
| GenealogyCensusTests | 7/7 PASS | `e841b59ae14d0cd1f258c0f3f77da94526683c69ec72460a474aab342efcc4e7` | `EditMode-20261007-231332-dba28a7075d343f49852b9c609db6d02.log` | `aec0edfad8c7d9255f99506bc0ff17b84eda5ca50ad76b517fa86064f80dcc5e` |
| SimulationBootstrapCompositionTests | 25/25 PASS | `064796973f487c1c71e48303a5ef2fc3ea024a8001ee7086cd8a77d001a17f08` | `EditMode-20261007-231342-178ec59fa76847c58140fae0c9f19787.log` | `21effd37722b34a5782cea932827fe5e20678c853d18ecbc55c37b931bc53072` |
| P12CrimeJusticeInvalidationTests | 12/12 PASS | `260502d58212dd2709c2f247e4b92bd8480c9d815495be223a17e56288513ed5` | `EditMode-20261007-231352-129bf93679a647c0aca8c12696dd3b3e.log` | `6b9f4e3f75a89a00be3b488cfa88fe1b10cf3d35dc9fc785b8309c410ff29ba1` |
| P12PopulationLifecycleInvalidationTests | 20/20 PASS | `2ed08ba98a9f16f675196304cdbbd7685e1b129f6493ae917658dda41503c25d` | `EditMode-20261007-231403-3eb9cef014644eb28bc639f69089ce39.log` | `10e5c6ec643bd3d553fdc9fecf8bac59908fcaad9591c7f4af9a9a51330ff65a` |
| P12SoloTravelStartOperationTests | 11/11 PASS | `bee8d506416b598b57764314433bf59932787957790aa27c0257d64c98e4babc` | `EditMode-20261007-231417-b2c1a431ee604496acc7fd4ff58d78ba.log` | `0654144703298f691d3b84b2f34ad757a3882aae9dda0751938543cdca4c0a42` |
| P12TravelPartyAdvanceTests | 10/10 PASS | `c2c397020bd71e47cc030078515ca8434977b21da4881acb65402c5ad88d4b1a` | `EditMode-20261007-231429-3b94f13583b14faba79b493a02cbf6a5.log` | `24f8723d94a6ad93bc13bb03b3c56011a11e15eaff044f559e3c53c834864341` |
| PropertyEstateMutationEpochTests | 5/5 PASS | `2f4dd189013eeb243c0f4995c8045f287f85df72e96db66e501ff5da414dd6dd` | `EditMode-20261007-231439-2615a901f66e451d8fb19decc97fa6b1.log` | `a2933f72b29b8f1be6b55ab81bbb0374339d2d2a8f8332993acb49745c2d20ce` |
| AllEditMode | 2458/2458 PASS | `856fdd3b948ca5a24b93abcd92b1df9c1640f1e636f3a0258492bf1d0c0a2581` | `EditMode-20261007-231449-4c39c48b05344e118c16b4b2091ad606.log` | `f18822cc0bdad2467dfeea6910489c52fec5ef56f120d8c17cad33991a1163d5` |
| OfficialSmoke | 5/5 PASS | `deec04840ef12e3890a41421d9f08acee01b5be2f951238119dfb35c597e87b6` | `EditMode-20261007-231523-456d07e9d7e148149417816d4f8a5c62.log` | `bc9dba5273a0bea122d667d9bb52fba8608f9f62ddd0642c85c652c3cdd3e50d` |
| SimulationRuntimeLongRun | 7/7 PASS | `441c6f0da2f7e307f4a9b87ea543de2128fcfd5e50e72c92e76de9f70d6a3889` | `EditMode-20261007-231543-6c76ec97bf98462d94ba3e19d2cb6455.log` | `1ff1e8b1e25993047d177d605c4c8490052978c1a8ab904bebaa3c53bf00710f` |

RawLogs.zip SHA-256: `33c2b7f5e9b675d27d74a265a572ee3ec6f1dd7eca34b4cdc6c6b9f395639992`.

Validation was run on the exact nine-file source set recorded above, followed by ALL EditMode, official Smoke, and SimulationRuntimeLongRunTests. All passed. `git diff --check` passed on the candidate code diff. P12-B remains INCOMPLETE pending exact-tip independent review and canonical promotion.
