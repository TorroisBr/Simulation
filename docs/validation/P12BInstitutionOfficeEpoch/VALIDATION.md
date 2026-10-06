# P12-B Institution/Office Mutation Epoch Validation

Status: **PASS** on the implementation source tree based on canonical
`0daa72addc1f186d23713adf75f6c2f83a5aff9b`.

Unity version: `6000.3.9f1`.

## Validated scope

The selected `UnityBootstrap-Daily-v1` census inventory now contains 239
required/accepted sections, including the four P12-E Institution/Office
sections. Its Institution and Office witnesses begin with zero rows and
revision zero. The three Office witnesses share one installed `OfficeStore`
identity and revision.

The implementation admits the listed runtime Institution/Office commits as
one `p12.institution-office.owner-commit` operation. Institution commits
notify their one changed section; Office commits notify records, incumbencies,
and tenures together because they share the `OfficeStore` revision. A committed
owner write remains successful if post-commit notification fails; the P12
protocol faults closed in that case. Admission failure occurs before the owner
mutator. No daily-loop behavior changed.

## Test results

| Run | Result | XML SHA-256 | Log SHA-256 |
| --- | --- | --- | --- |
| `InstitutionOfficeCensusTests` | 4/4 passed | `ABFF855D9DF980FD9334A2C8F7421F6DF4683C88F024126CDDAF8F39751B9FC5` | `C946F927C8217271CF87D941C60B028A6C9A0A6C1B76DECE138EAFF7BC4761E6` |
| ALL EditMode | 2410/2410 passed | `DD63FB49BD4B8E37EC8D6C81A0B79BCE87F57EEA57D58D98F08BBDCE78738217` | `A223ECAA4CE368CAED09A56210BC9E1BBDFB157D925F688CB988EA1F3C723794` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 passed | `7131A3803455288A8D7ABD39EE8071DD3F9040EC80E3ECE648C9CFA40B9E94D5` | `BF721BD4D9FAB381FEA14ED645FB59190197A14E71A752AA7C850C5066D15930` |

The ALL EditMode result includes a pass for
`SimulationBootstrapCompositionTests.SelectedDailyV1ProfileBootstrapsItsAuthoredP8GeographyBeforeDayOne`
and all four `InstitutionOfficeCensusTests`. The selected-profile test verifies
the 239-section inventory, required roles for the four new sections, zero
cardinality/revision, and shared Office owner identity.

`git diff --check` passed. The exact changed source hashes used by the three
isolated Unity checkouts were:

| File | SHA-256 |
| --- | --- |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `E7302D3F90CEA667A62FA65A7C71D63AEAF4689BC18D6454359ACC60C42EDC48` |
| `Assets/_Project/Scripts/Institution/InstitutionalVacancyRecognition.cs` | `5014D21FD8E0F90372577E9E6FAF1BB38668F673EC4CCA30456828F5B192ED41` |
| `Assets/_Project/Tests/EditMode/Editor/Institution/InstitutionOfficeCensusTests.cs` | `714EDB8C7BBBD6BFDCE58DB0541525C38F73462578B5AFD37C0D325E04F0E086` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs` | `56FFA4D11B90896F414C09426E2A23250101A7E0E03777F067373258BD10ABEE` |

The XML and log files are retained alongside this report under `Focused/`,
`AllEditMode/`, and `OfficialSmoke/`. Raw Unity logs are bundled in
`P12BInstitutionOfficeEpoch-test-logs.zip` (SHA-256
`003186CCDE77A96CA483328A1D51E279E0ED997B463E1095D40293EB8413D8A8`); the ZIP
entries retain their focused, all-tests, and official-Smoke directory names.

## Limits

This is a bounded P12-B Institution/Office mutation-invalidation slice. It
does not establish complete owner coverage, complete shared-epoch coverage,
global quiescence, capture eligibility, export, hydration, P12-A readiness,
P13 readiness, P12-B completion, or Phase 12 closure.

## Corrected operation-contract validation

Status: **PASS** on implementation source commit `95fac36e02299e7683d9bcb33bccf581c1429e5d`, based on canonical `0daa72addc1f186d23713adf75f6c2f83a5aff9b`. Its Git tree is `abf39bd55d22dc4f52fda26e79d44ea732b84804`. Unity version: `6000.3.9f1`.

The corrected tree uses the accepted `p12.institution-office.owner-commit` identifier. The selected Daily-v1 composition test asserts that the sealed protocol contains this identifier and excludes the mismatched `runtime.institution-office.owner-commit`. It also passes the 239-section owner inventory check. `Simulation-DailyV1.asset` has no P10 Ruin; `Simulation-GeneralTest.asset` retains its authored P10 Ruin.

| Run | Result | XML SHA-256 | Log SHA-256 |
| --- | --- | --- | --- |
| `InstitutionOfficeCensusTests` | 4/4 passed | `CCC35707FECBBB67579070ADF488C7B1DE5C9D069F5D531ECC3E671836634562` | `09E9CC4574325B9DB435C42D38CA1D15D9BFC2BBED7B0B826B36126CE375D0E8` |
| ALL EditMode | 2410/2410 passed | `58A28908ECE8554D3E540FFAD3D92A3CBDAD0ED3CE9D38CC3F0ACCEB0DB5D228` | `8E522F818A89ABDBF72D3B32E6E4831D1B0640ADD6A8667A1259F7C084E2A965` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 passed | `203E3AC617FE2930D01FEF323B0ADE42B8135549AF7E02AE2D29C7CE2488923B` | `CB23BD66263211EB84584F00909A44A54FA5874734886769286A7BB432379AD8` |

The ALL EditMode XML confirms `SimulationBootstrapCompositionTests.SelectedDailyV1ProfileBootstrapsItsAuthoredP8GeographyBeforeDayOne` passed. It also confirms all four Institution/Office tests passed. `git diff --check` passed after the corrected source and evidence update.

The corrected source hashes are:

| File | SHA-256 |
| --- | --- |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `51EE84AF510720773B5FD567C9C4B32A76B9ADB93427109A007E56CC02D41AFE` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs` | `229D4A8EBA1998A695B298937B0C8177AB77F1A122E1FA7AC8DED3A7382927E7` |

XML is retained under `CorrectedTree/Focused/`, `CorrectedTree/AllEditMode/`, and `CorrectedTree/OfficialSmoke/`. The three raw logs are retained in `CorrectedTree/P12BInstitutionOfficeEpoch-corrected-tree-logs.zip` (SHA-256 `FEF06B0E9F1FA5F57076E25A5DD72820A5313770AD50D5C73F14DB0C5B188F36`). The preceding test table documents commit `1048073d03fd92767d0ae135800073da8bd60053`, whose operation identifier did not match the accepted design; those historical results do not replace this corrected-tree validation.
