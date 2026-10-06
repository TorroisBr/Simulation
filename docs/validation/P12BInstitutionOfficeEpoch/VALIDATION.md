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
