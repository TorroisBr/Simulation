# CORE-A Calendar Shared Assembly validation

## Candidate identity and scope

- Production base: P12 canonical `4cc31073f0ec6ba5c718084e4cebd6e914931d5d`.
- Implementation: `e702cdf196703ead98d8d37ff818bd1c0d0219e4`.
- Implementation tree: `4de0dd2dfd557f2112e395f7c69a52e0eff3b0dc`.
- Architecture contract: `32358d2a0faf511e098c4f295a5132a4bfb8cfb2`.

The change moves the existing `CalendarDefinition`, `SimulationCalendar`
(including `CalendarAgeCalculator`) and complete `SimulationDate` value type
into the engine-free `com.simulation.core` UPM runtime assembly. Their global
names, fields, validation, arithmetic and behavior are retained. The original
two `.meta` GUIDs move with their sources; `SimulationTime` remains in the
Unity assembly. The SDK project explicitly compiles only the three Core source
files. No Unity engine, Editor, `Assembly-CSharp` or World Exchange assembly
reference was found in the built Core assembly; the Unity asmdef also sets
`noEngineReferences: true` and has no assembly references.

The `SimulationDate` struct body was compared after normalizing only the final
newline and matched exactly. Unity and .NET both build the same package source;
no second calendar implementation or SDK-produced DLL is imported into Unity.
The Unity P12 descriptor discovery test found the actual Core assembly MVID
`d0020fe8-8737-4820-96bd-bb9740b9da80`; it is discovered rather than hardcoded.
This is same-build continuation validation, not cross-build compatibility.

## Test identity map

The original Unity class had 32 test cases. These 28 portable test full names
were moved from `CalendarFoundationTests` to `CalendarCoreTests` (the class
name is the full name because both are global types with no namespace):

| Before | After |
|---|---|
| `CalendarFoundationTests.AbsoluteDayZeroMapsToFirstCalendarDate` | `CalendarCoreTests.AbsoluteDayZeroMapsToFirstCalendarDate` |
| `CalendarFoundationTests.LastDayOfMonthRollsToNextMonth` | `CalendarCoreTests.LastDayOfMonthRollsToNextMonth` |
| `CalendarFoundationTests.LastDayOfYearRollsToNextYear` | `CalendarCoreTests.LastDayOfYearRollsToNextYear` |
| `CalendarFoundationTests.DaysPerYearComesFromCalendar` | `CalendarCoreTests.DaysPerYearComesFromCalendar` |
| `CalendarFoundationTests.ConversionIsDeterministic` | `CalendarCoreTests.ConversionIsDeterministic` |
| `CalendarFoundationTests.CalendarRejectsZeroMonths` | `CalendarCoreTests.CalendarRejectsZeroMonths` |
| `CalendarFoundationTests.CalendarRejectsZeroDaysPerMonth` | `CalendarCoreTests.CalendarRejectsZeroDaysPerMonth` |
| `CalendarFoundationTests.CalendarRejectsNegativeConfiguration` | `CalendarCoreTests.CalendarRejectsNegativeConfiguration` |
| `CalendarFoundationTests.InvalidMonthRejected` | `CalendarCoreTests.InvalidMonthRejected` |
| `CalendarFoundationTests.InvalidDayOfMonthRejected` | `CalendarCoreTests.InvalidDayOfMonthRejected` |
| `CalendarFoundationTests.DateToAbsoluteDayRoundTrips` | `CalendarCoreTests.DateToAbsoluteDayRoundTrips` |
| `CalendarFoundationTests.AbsoluteDayToDateRoundTrips` | `CalendarCoreTests.AbsoluteDayToDateRoundTrips` |
| `CalendarFoundationTests.BirthDateAtCurrentDateHasAgeZero` | `CalendarCoreTests.BirthDateAtCurrentDateHasAgeZero` |
| `CalendarFoundationTests.BeforeFirstBirthdayAgeIsZero` | `CalendarCoreTests.BeforeFirstBirthdayAgeIsZero` |
| `CalendarFoundationTests.OnFirstBirthdayAgeIsOne` | `CalendarCoreTests.OnFirstBirthdayAgeIsOne` |
| `CalendarFoundationTests.AfterFirstBirthdayAgeIsOne` | `CalendarCoreTests.AfterFirstBirthdayAgeIsOne` |
| `CalendarFoundationTests.MultipleYearsCalculateCorrectAge` | `CalendarCoreTests.MultipleYearsCalculateCorrectAge` |
| `CalendarFoundationTests.OneDayYearProducesBirthdayEveryAbsoluteDay` | `CalendarCoreTests.OneDayYearProducesBirthdayEveryAbsoluteDay` |
| `CalendarFoundationTests.VeryShortYearDoesNotAssume365Days` | `CalendarCoreTests.VeryShortYearDoesNotAssume365Days` |
| `CalendarFoundationTests.VeryLongYearDoesNotAssume365Days` | `CalendarCoreTests.VeryLongYearDoesNotAssume365Days` |
| `CalendarFoundationTests.FutureBirthDateRejected` | `CalendarCoreTests.FutureBirthDateRejected` |
| `CalendarFoundationTests.AgeCalculationIsPure` | `CalendarCoreTests.AgeCalculationIsPure` |
| `CalendarFoundationTests.AgeCalculationIsDeterministic` | `CalendarCoreTests.AgeCalculationIsDeterministic` |
| `CalendarFoundationTests.LongAbsoluteDayDoesNotOverflowIntermediateMath` | `CalendarCoreTests.LongAbsoluteDayDoesNotOverflowIntermediateMath` |
| `CalendarFoundationTests.CalendarDateEqualityIsValueBased` | `CalendarCoreTests.CalendarDateEqualityIsValueBased` |
| `CalendarFoundationTests.CalendarDefinitionStableDuringRuntimeIsDocumented` | `CalendarCoreTests.CalendarDefinitionStableDuringRuntimeIsDocumented` |
| `CalendarFoundationTests.UnequalMonthLengthsConvertCorrectly` | `CalendarCoreTests.UnequalMonthLengthsConvertCorrectly` |
| `CalendarFoundationTests.UnequalMonthYearBoundaryCorrect` | `CalendarCoreTests.UnequalMonthYearBoundaryCorrect` |

These four host-dependent full names remain in `CalendarFoundationTests` and
run only under Unity: `AdvanceOneAbsoluteDayAdvancesCalendarDate`,
`ConversionDoesNotMutateSimulationTime`, `AgeCalculationDoesNotConsumeRng`,
and `AgeCalculationDoesNotAdvanceTime`. Focused Unity and SDK tests include the
moved and retained cases; no original case was dropped. Additional cases cover
strict/null/custom definition validation, overflow, effective snapshot
isolation, semantic identity/version, complete `SimulationDate` value
semantics, nonuniform calendar/birthday boundaries and legacy safe fallback.

## Unity validation

Unity `6000.3.9f1`, Windows x64. Five passing runs used identical input digest
`d7449dd7147ecad644bdc5d0d320c4e5a652f6cd2c4119309263930b86f7945a`:

| Run | Selection | Result |
|---|---|---:|
| `3b33bf9c25864880af06158b6eaf66d5` | Calendar, including parity and authoring round-trip | 64/64 |
| `81037df92465422bbcaad07182889eed` | P12-A compatibility/capture/restore | 131/131 |
| `70f02e9559624d2e8df8623aba39bd35` | Daily-v1 admission and continuation | 169/169 |
| `7bb30395ee3349c28a28b713bf5e8920` | ALL EditMode | 3080/3080 |
| `b9e11f361c074afd93cdbbac0555f7fd` | Official Smoke | 5/5 |

All runs had zero failures, skips and inconclusive tests. The ALL report is
available in the validated Unity evidence package. The five exact Smoke test
names are also present as passed cases in that ALL report; a separate official
Smoke-filter run independently passed 5/5 with the same complete input digest.
The Smoke-filter XML is retained privately because the supported Unity packer
rejects filtered reports whose suite aggregate counts are incoherent. The report
was not edited to bypass that check. The package and manifest are:

- `core-a-final.manifest.json` SHA-256
  `f736a4c038eb1ff501d1f71fa02c364884e78eb2494e06beda1a21404f945736`.
- `core-a-final.zip` SHA-256
  `ad99dea856aac26d94fec35c970ab8e9142dc77f9f7e95a3bcc0a89ffdd0c614`.
- Package verification: PASS, one complete report, 3,080 cases.

The package's run-time input snapshot was recorded immediately before the
implementation commit with repository HEAD/tree at the production base. All
1,545 recorded paths, byte lengths and SHA-256 values were independently
recomputed against the committed candidate and matched exactly (zero missing or
changed inputs). Thus the tested file content is the committed tree content;
the recorded Git HEAD/tree fields remain the pre-commit base and are not
represented as candidate commit metadata.

The negative compatibility case uses the approved controlled old-descriptor
fixture, not a real old save. It verifies rejection before publication and
preserves the slot. The positive case captures, detached-loads, restores and
continues using the actual production Daily-v1 path and current build. No
ProjectSettings file was changed by this candidate; the `ProjectSettings.asset`
digest matches the base.

The earlier unchanged-source parity baseline is run
`47bcaf1eabca4bce9d1f15659bb55833`; it is referenced, not copied into this
package.

## .NET validation and evidence boundary

The sparse SDK-only worktree was pinned to implementation commit
`e702cdf196703ead98d8d37ff818bd1c0d0219e4`, had no Unity `Library`, and used
SDK 9.0.315 with .NET 8.0.28. Locked restore, cold build, cold/warm test, and
cold/warm demo all passed. Both cold and warm test runs discovered and passed
the exact same 45 cases, with zero failed, skipped, inconclusive or not-executed
cases. The deterministic demo produced absolute days 0, 10, 30 and 35 for
`[10,20,5]`, checked before/on-birthday ages 0/1, and rejected a negative day.
The demo is a calendar capability demo, not a world or `AdvanceDay` demo.

The public-safe complete runner manifest is
`core-a-dotnet-66946b2a7bcb441b81bba30f0822f5ac.manifest.json` (67,859 bytes;
SHA-256 `1a2926110c89644983e4782284468ebabcc26ef3c8a754363b7983325048da11`).
It records per-case identity/outcomes/reasons, cold/warm results, source commit
and tree, 27 explicit inputs, toolchain, demo outputs, timings, and storage
measurements. Its input digest is
`cbe9382967618ee74f378b47bc2c75a887e0431985f2cf36eb45aea11bee363a`; input
stability passed. CoreValidation self-tests also passed for populated,
parameterized, failed, skipped, empty, malformed/missing and tampered reports
and input instability.

Cold/warm timings were: Core build 2,397.889 ms; tests 1,415.555/936.678 ms;
demo 1,167.640/483.318 ms. The SDK-only worktree had no Unity `Library`.
Measured local build storage was 26,247 bytes for Core `bin`, 37,131 for Core
`obj`, 4,571,319 for test `bin`, 182,291 for test `obj`, 198,323 for demo
`bin`, 201,911 for demo `obj`, and 132,716 bytes for result files. These are
local measurements, not a project-wide performance claim.

Original cold/warm TRX reports are privately retained with the exact hashes and
lengths in that manifest. They contain a host identifier and absolute local
worktree paths. The approved host evidence contract requires the complete
original TRX reports in one reviewed durable bundle; the Unity packager does not
support TRX, and editing those reports or publishing their machine-local
metadata without a privacy disposition would violate the evidence boundary.
Therefore the original reports have not been altered or published, and the
durable .NET report-bundle requirement remains unresolved. The manifest and
test outcome summary are useful provenance, but are not claimed as a substitute
for the required original reports.

Report identities, retained only in protected local evidence:

| Report | Bytes | SHA-256 | Result |
|---|---:|---|---:|
| `CoreTests-cold.trx` | 61,701 | `39a23439384766b65b1d7141fb536ddbdca39fc0634b46fb24adc51e0bd32a0d` | 45/45 |
| `CoreTests-warm.trx` | 61,701 | `71f25b9bca002d4cdea8d1257b71f4239fab35db8e37e3eae7b5e1c14e97884a` | 45/45 |

The public manifest itself was privacy-inspected: it contains no user-home or
worktree path, machine-name field, or credential-like value. Its SHA and
per-case content do not make the unretained reports independently reviewable.

## Current verdict and limits

Implementation and execution gates passed, including the Unity P12 regressions,
parity, authoring, ALL EditMode, official Smoke, SDK runner checks and demo.
The project is not yet promotion-ready: durable publication of the original
.NET TRX bundle and independent exact-tip implementation review remain pending.
No canonical promotion has occurred. No full SimulationRuntime, full-world
hosting, P12 .NET continuation, cross-build save compatibility, P13 historical
fork, P19 mod persistence or broad Unity independence claim is made.
