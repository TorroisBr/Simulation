# P12-E ArmedForce / Manpower / Position Snapshot Validation

This evidence validates only the reviewed P12-E owner export and private staged reconstruction slice for `ArmedForceStore`, `ContingentManpowerStateStore`, and baseline `ArmedForceSpatialStateStore`.

## Exact source and review boundary

- P12 canonical base: `04e7c3f49a7c9690ebc091fcfc50f05ef67009d6`.
- Architecture baseline: `47eff220c7ce00f6e7c759bdc2b76780bb46f628`.
- Reviewed design candidate/base: `3ba6516bf6bcf081445f9c2c7fea192423487851` (tree `132a179d10e3986af204595b21e7bd07099b3d1d`; design blob `863c90329f1756bccd5135d255e8761891682843`).
- Independent exact design review: `903bd26fc31f015f1e0d89fdf436ec8d240ab5df` on `codex/review/phase12/P12EArmedForceManpowerPositionSnapshotDesignReviewR2`.
- Implementation commit: `31c5fc19fda1c8ed0ec51d0e94d80ea5bde27f0d`.
- Exact implementation tree: `fa1ee31fa9c4469c1ee4916c9d5425ee2f891828`.
- Exact `Assets` subtree: `96a74b341f5c2ae07f53d7a16682a7d3e183f033`.
- Branch: `codex/phase12/P12EArmedForceManpowerPositionSnapshotImplementation`.

Source blobs in the implementation commit:

| File | Blob |
|---|---|
| `Assets/_Project/Scripts/ArmedForceStore.cs` | `44cf3322191a553db86f7e685fad5114272fa4f6` |
| `Assets/_Project/Scripts/MilitaryManpowerFoundation.cs` | `ca97d42b0f440ea7ba76b4d7df4213466de68c77` |
| `Assets/_Project/Scripts/ArmedForceSpatialPosition.cs` | `8eb40bdf9cb4aa3092e58fa6a998201600eb7e5a` |
| `Assets/_Project/Scripts/P12EMilitaryOwnerSnapshot.cs` | `4233f5a8c2aa68152b3db0b496153953101e3c42` |
| `Assets/_Project/Tests/EditMode/Editor/P12EMilitaryOwnerSnapshotTests.cs` | `c3ed388e7ebb5029d4527b8a059a776420757454` |

The implementation was tested from the exact code tree above. This work does not add runtime integration, census admission or epoch wiring, quiescence, P12-D assembly, P12-G, capture eligibility, export/hydration composition, or P12-B/P12-A/P13 readiness claims.

## Validation results

All commands used `Tools/UnityValidation/Invoke-UnityValidation.ps1`, Unity EditMode, and isolated output directories. The official Smoke gate is the complete `-TestFilter Smoke` filter.

| Suite | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| `P12EMilitaryOwnerSnapshotTests` | 7/7 | `CA04045C4A785B8FE246DE7DB651282A4208AAB749D5C522A18A6B4F329CA4EA` | `F017CAE958716BBC44FBE0C9DCB6787382BD6E7A5D2C37269A0A3043A755D959` |
| `ArmedForceFoundationTests` | 10/10 | `6570121092D09E01A5CEF8C978A53E89D0DA1717B6B150DB17532AB7F0C80E98` | `36AA20727104EBE72D77D4671A20D6821049361E747E0ED2C43AF4A1434C1E03` |
| `ArmedForceSpatialPositionTests` | 12/12 | `F6CDFDDF2C737276658F247C7EDD33000D3B53851EC1D61A0BBA3405A14DF542` | `87F3C9A95C3D0FEE9F0C4AA6B13F5BE50E43C108A83D208532BB5F00D45B9D51` |
| `ArmedForceStoreCensusTests` | 1/1 | `EDD623F089187D2E4036CC8BB7744FA39BF64214F74ADE6812CB891D3D03A5BB` | `8417301850287680D56A2AA3D315530C824193BF17DF1BC9F2AEB29684560433` |
| `MilitaryManpowerFoundationTests` | 11/11 | `135BD45FF46085242FBB53CDCE9E839D24B96BB3E85232C3A6228EC9C3AFA464` | `2D23A41709B26DCFD08F678450D8043ABF0AF50913CFDE2420DC8D9D1358EDAA` |
| `ManpowerSourceConsequencePlanningTests` | 23/23 | `50C875D0F84A3EF5C9B4AC2C81E6C68EF496A766DE77BE236A3D4628CEC6133E` | `99488E5E80B0C0CFFB0CAAE0ADCA011CB458FF53F5AB24BFA0ABBBB8EA12F585` |
| `ContingentManpowerCensusTests` | 1/1 | `8DB4E64B3612129F72AD5B33DFF398A857C7ACB23B0E13EA1758940F19E2EE27` | `93234588F8C67CA85236EF66EC3515FA7B54B63A43DE37E7A3DC97A9CEC835BF` |
| `PersistentBattleOwnerSnapshotTests` | 6/6 | `C12DE98D8CD503BB9E114A0DE53B26D0A3B9801FDF598B3ABAB5A9B6D796B268` | `90EADF9E77F64B0846A1FCD2EDF657CBD667B7143C7358C4254F8E618F0DFAA3` |
| `BattleDirectConsequencePlanningTests` | 21/21 | `56D66B7EC81A7B0A0AEFBCFA886A59FAA4F6113B9FE02E1E867619F27FC1BD4C` | `5C18A1D3F5C906AA6A967110A4D3B1DDB15EDB6DA12A06EE14DC3BB0EA2285A8` |
| `P16AMilitaryMovementTests` | 21/21 | `0EF1F0392A85F6E909E6BCC28E33A32167C50890523AF21B01FE226B4FAE39BB` | `7D564EF1DC73E657D4CDC3F7DF257EC1CE99DF6F88E3CF5EE80E2A2D28738F73` |
| `P17ARuntimeTests` | 10/10 | `6C1A6A66056414F4F60AE93552E912A5A2D3DCF394866DF01192DD7D50E8C1CB` | `0A9B2F7EC9701E60DD8537389A3AA2DF0BDB5026BCA6BB0DA5DF4082460B4C8D` |
| ALL EditMode | 2576/2576 | `9295ED0B0C70D01B8DAC8000E583328A412219B310A11F8A327D86FBB41EA5A7` | `0D0B0BDA24AF18928528A06E3811BD9E942851CBDB1E7A7D6D1A1E576D558EB8` |
| Official `Smoke` | 5/5 | `C4210039321D8B58A128E8A1954AFD98F3AA3892A729B869101FD19CE29B64F5` | `43E9B52B1A31D95511B7D4A6104929D02135C2B1457675974B1F41E622E68BAC` |

All XML and matching raw Unity logs are included in [`P12EArmedForceManpowerPosition-validation-20261008.zip`](P12EArmedForceManpowerPosition-validation-20261008.zip), SHA-256 `73236A9CA2DA8890A8F6C6846B71EC9011B5BE176DFE8794E983A4FB590EDB11`.

`git diff --check` passed on the implementation changes.

## Non-evidence attempts

- The initial focused launch compiled before tests and exposed a missing `SpatialReference` validator overload. The overload was added before the final passing runs.
- One intermediate focused run found a test setup error: the provider-null assertion supplied two different owner-vector instances. The test now reuses the exact token-bound vector; the final focused suite passes 7/7.
- A comma-separated `-TestFilter` invocation returned zero tests and is excluded. Each regression fixture listed above was run separately.

## Review-gap follow-up — exact updated candidate

The independent exact-tip implementation review at `e4ff2a2610cd72e6a27fd55789172b8beb220567` requested additional snapshot boundary coverage without identifying a production-source defect. The follow-up adds tests only, within the accepted P12-E design: complete owner-field round trips including detached and terminated forces and all cohort dimensions; deep detachment after mutating source owners; malformed hierarchy/cardinality/identity/cohort/custody/mirror/spatial rejection with all staged outputs null; and failure/no-side-effect assertions at each private owner factory.

- Previous pushed candidate: `7395ab58354bc859b33469c5af3f0b17a82e840b`.
- Updated code/test candidate: `a6ecf55aa8f3aca6d51a1a3d332881383eac5b4a`.
- Exact repository tree: `fa62a40c9698235f0c6bae1acbe0bc45ba1cf141`.
- Exact `Assets` tree: `09ba6fe9c74c2f19f84b318944ce3e0bcc8119b6`.
- Updated focused test blob: `3f88e6da4fddbc34fd3de12f760f8d78a79864d5`.
- Production owner source files are unchanged from the prior candidate.
- `git diff --check` passed after the test-only code commit.

The validation below was run against the exact updated test/source content committed at `a6ecf55`. The complete EditMode run also covers the independent owner regression fixtures whose separate invocations were affected by a Unity runner `NoResultXml` operational failure.

| Suite | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| `P12EMilitaryOwnerSnapshotTests` | 10/10 | `C5149A9A0289A8976638F9F1CCC42DC59E14AB6EF589E82E5C27633F8BD7C93C` | `B23B5DCFE7542814CD0AEA7F5E01E1E28BD1ABE4BC41CA34AC28FF6F5E901EFF` |
| `ArmedForceFoundationTests` | 10/10 | `E1E55337050D01B22F2158613E344FCFB60B24C82A4228D9894863E2D051673A` | `18CEBF73C12ED24E07FD507ACB4DB59D0D7CD5BBE8F2EA79C95E337FD519DB4D` |
| `ArmedForceSpatialPositionTests` | 12/12 | `ED78C85C2ECA4A0028778DCCFBFD7439B73D1A5CBA095D9B23BF3731F5200D8C` | `9D7CCAADE68F8A8767E797509B3F7A0FA44AF48C97656A4433010C00603062B7` |
| `ArmedForceStoreCensusTests` | 1/1 | `711F2F816A296EC02C088D853BB7FE1304E8121FE546F959917AED9758AB1845` | `ED95B7AB08CFD06304F95E1412E61FCB53D8DD88F752CF32CD0D2C6C7C5B9B2D` |
| `P16AMilitaryMovementTests` | 21/21 | `4FCFFA769E8C42A0674B9F9E91663277B9351A8B55407C1EA3996756CC26ED90` | `A6471124E039D40E68A4983CB1439320BAD80C1B8ABCB14005D15610626EFBBC` |
| `P17ARuntimeTests` | 10/10 | `B6CBBD6CF1E9E962419FB79806E952EFDACD65F03239A69A60CF7FB3AF08DDFC` | `FAA7676B0D4006106205066F6CECE481461030470BF1B8FFD65F8C9CD748AE74` |
| ALL EditMode | 2579/2579 | `E8A63DE186F38E1938257DBCCF3D453ADAF072B6A6CBA231C5B65CE1B5AA7511` | `12EC5F9AFB9525E48CA10F0FE7C3D046F7D7E973F2239236B0213C0C5BF9B86B` |
| Official `Smoke` | 5/5 | `2BA9648197C3B4BA58FAB993AC8024B86B2C1F9A5C4C7056E4306633DEF44CF8` | `3F60B1A3C29346C86FEBBA5A91F3046347F9A39505AFB77FFF9CE28F57F64C17` |

All successful XML/log pairs and the operational-attempt summary are included in [`P12E-review-gap-followup-20261008.zip`](P12E-review-gap-followup-20261008.zip), SHA-256 `AC3AF3F4F852880725B01E45225FCE95BD0446279BE227ECBB0134058EEF08C3`.

Non-evidence attempts and the no-result filter list are recorded in the archive's `attempts/attempt-summary.txt`. The no-result filters were not counted as passes; ALL EditMode passed 2579/2579.

## Negative amount/revision matrix follow-up

The subsequent exact-tip review identified that §6's negative-value matrix still lacked direct negative amount and revision cases. This follow-up changes tests only; production owner source remains unchanged.

- Updated code/test candidate: `90481acc0caae36385b3ec2e58d3a9b9b03316c0`.
- Exact repository tree: `15dbe25fd6389a00cdd7ec9472ca9725702f7ab9`.
- Exact `Assets` tree: `af1af0db0799431b796a53509a1e8fa130d5611c`.
- Updated focused test blob: `8f96727b13af4173adf48bff22e819e9447c79cf`.
- Added staged-document rejection cases for negative contingent amount, negative manpower cohort amount, each of the three negative owner revisions, and negative manpower-state revision. Each rejected stage returns no owners.
- Added private factory failures for negative ArmedForce owner revision, negative manpower owner/state revision and amount, and negative position owner revision. Each returns no candidate and diagnostics; tests verify supplied/live revisions, invariants, or authority cardinalities remain unchanged.
- `git diff --check` passed.

| Suite | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| `P12EMilitaryOwnerSnapshotTests` | 10/10 | `21F1D39E541687925841C5A939486E40C1BDA0567871BE0E651059EA7E945971` | `C0007A4193C3FD321E43E7AE4FFC471EAEDBF7C1BC119F0F200530ACEB45F193` |
| ALL EditMode | 2579/2579 | `70FE977837EF862532095BA708779095868D00A93503911140A2D969D25C8FC5` | `77DEA99A5D29E665C728EB429A7C1779A1F1A8742FD397A8DC20140F7A8315DC` |
| Official `Smoke` | 5/5 | `90CCDEF0109FE4193F91263AC503C901A2CA63D5863C93C24AB71259E9C0E551` | `537761907788E7DA09C82B2A7DD5CD425A0B5A3BC447A1FCB1CB15FCB8873E2D` |

The XML/log pairs and run summary are included in [`P12E-negative-values-followup-20261008.zip`](P12E-negative-values-followup-20261008.zip), SHA-256 `ADEB46B26B8A62CFD230DFEFD768298A29C304625A2AFC161E0FA6ECFDB36675`.
