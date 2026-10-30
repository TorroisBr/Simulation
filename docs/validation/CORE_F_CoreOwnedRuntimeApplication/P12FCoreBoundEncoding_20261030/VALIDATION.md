# CORE-F P12-F Core-bound continuation section encoding

## Candidate

- Code candidate: 62157a822bffd2f2b2c9717aecba4ec471f85bec
- Candidate tree: 570615a49938a476052e086283a112d8d17b5b20
- Base candidate: 99aa3218980dae9d78be7bc52bdb8a695ad19efb
- Core canonical: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea (unchanged)

The active Core continuation-save operation now retains its exact P12-F owner snapshots and encodes PoliticalKnowledge, ScheduledDirective, ActorChoice, TravelParty, and Expedition sections from those snapshots. The Unity coordinator combines these Core-produced sections with its remaining host-owned P12 sections. The legacy Unity capture path remains unchanged.

The regression compares each of the five Core-produced section headers and every row key, revision, and payload byte with the sections sealed into the resulting image. The existing restore test also validates the resulting full owner census and deterministic continuation.

## Validation

- Official CoreValidation: PASS; cold 936/936, warm 936/936; run 8b6fac8029ea42169821665ae3b8975d.
- CoreValidation source-input digest: d471f745efd9b96bd36d045c0af87233d58001228665c8924561cc25220c34b0; input stability PASS.
- CoreValidation manifest SHA-256: 85f13bd12a23aa1ccf0cf83455ab43cac3dc01323e60cd28a6d23015bdc1aea7.
- Cold TRX SHA-256: 6be5f2f52a9007fae25f6825d74be3d86236a2afe0502ba130677874b0455d23.
- Warm TRX SHA-256: 1063329e26d11b64d6602202f7baa410ddf942299f16577f204629b6e4deb4d5.
- Official Unity EditMode focused test: PASS 1/1, 0 failed/skipped/inconclusive; run a157170e2dc9406da6f28a524d2f13ed.
- Unity test identity: SimulationRuntimeAdmissionTests.DailyV1RestoreHydratesNonemptyPersonAndGenealogyGraphAndContinuesDeterministically.
- Unity input stability: PASS.
- Unity manifest SHA-256: 7f4c6890718cf727165e402ab1ddfbf225dd83768e576201c05fee8eb3481684.
- Unity result XML SHA-256: d505fdf55eb7a083ab56e7ce76240516c75a2282bbfc29c840bfb569d78ac28c.
- Unity log SHA-256: a259b2ce77beefe0d4700d148c75d2b67a175bbee70a1e8bb882974d57d0d24b.
- git diff --check: PASS.

Private original manifests, TRX, XML, and logs remain in the protected E: validation runs, identified by the run IDs above. They were not copied into this record.

The initial Unity attempt produced no result XML because the newly added Core source was absent from Unity's generated assembly inputs. The same encoder was consolidated into the existing Core owner-source compilation unit; this final Unity run then passed. The no-XML attempt is not counted as validation.

## Exact tested source hashes

- DailyV1P12GCoreBoundOwnerSources.cs: 0d56bd13d64ee2ba481198c2858b4184189bfa1f11e708340e45a2739ab51e78
- DailyV1P12GContinuationSourceWitness.cs: 2a6e51d7684c554ec2ffce84ab6bffc360f1aeca72dc1df673f2141c3af57762
- P12FDailyV1OwnerPackage.cs: bfa9c9e54d11e8aacc524b51c69cdfb272273b4ce102b6176e3bb2828d0d7447
- P12ADailyV1OwnerSectionEncoder.cs: 5847937d0b85c07d57b8c2315ae2d39779be229634fe6bc49ef2f78c8ec1b9b1
- P12GDailyV1RestoreCoordinator.cs: 9b539cb4b29f0a024f35c4df2b2875d634fd4b80fb23ab9d8138e2e8afd460a6
- SimulationRuntimeAdmissionTests.cs: 5c7683be1fc6a4b4cd9358edd5f7de2a86acb2ef844e3562af441b9f55b3f155

## Limits

This is a partial CORE-F candidate checkpoint. Full Unity EditMode, Smoke, LongRun, permanent validation packaging, and full CORE-F acceptance are not claimed. No canonical promotion was performed.