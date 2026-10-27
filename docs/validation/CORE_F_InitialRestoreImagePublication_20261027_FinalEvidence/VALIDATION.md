# CORE-F initial restored-image publication gate

## Scope

This candidate binds the first Core-owned restored-session publication to the
exact continuation image successfully loaded by the same `DailyV1Application`.
The Unity adapter passes that image through the publication path only after the
candidate's Core runtime has its Unity advance boundary bound. Core authorizes
the initial restored candidate against that exact image reference, then consumes
the authorization when publication succeeds. Replacement restore continues to
use its existing source-session and exclusive-operation reservation path.

The `RestoredSessionExchangeWaitsForIdleWindowAndSwitchesActiveAliases` fixture
was updated to reflect the internal publication overload's added image
parameter. The replacement-path assertion remains unchanged; its reflection
call passes `null` for the image because replacement does not use the initial
restore authorization. No gameplay assertion was removed or weakened.

This is a bounded initial-publication admission change. It does not complete
P12-A, detached-host restoration, or CORE-F, and it does not promote Core
canonical.

## Source identity

| Identity | Value |
|---|---|
| Core canonical base | `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea` |
| Candidate branch | `codex/core/CORE-F-initial-publication-admission` |
| Tested implementation and validation-tool tip | `bf4b2ac9fe52c3a4abd25a02d83c4f0a2c9f761c` |
| Tested committed tree | `c90c9868c86f21411fc3ef832c54e7dee07a52e5` |
| Unity input digest (1956 source/configuration files) | `65b1af751c6b64cfb0efb16685249ff99f77cc70d51ca23d34763b31f43708f8` |
| CoreValidation input digest | `266f8f5f7c9d0eae5cbaa008e6f9470a5f4e845411e5596806118fae532f807f` |

The Unity full run and Smoke run used the same repository commit/tree and the
same stable input digest. Their before/after snapshots were stable. The
CoreValidation run was made at the exact tested tip with stable source and
generated dependency inputs.

## Validation results

| Gate | Run identity | Result |
|---|---|---|
| Unity validation harness self-tests | exact committed runner bytes | 34/34 passed, including rejection of a coherent zero-test result |
| CoreValidation cold/warm | `3050f1233507460da962638a83d88172` | 902/902 cold; 902/902 warm; zero failed, skipped, inconclusive, aborted, or unexecuted cases |
| CalendarDemo cold/warm | same CoreValidation run | both commands passed |
| Simulation.Lab build and cold/warm | same CoreValidation run | all three commands passed |
| Unity ALL EditMode | `e17b1f05bb0e4233a55f3be47fe6ca3f` | 3274/3274 passed; zero failed, skipped, or inconclusive; stable inputs |
| Official filtered Smoke (`EditMode`, `TestFilter=Smoke`) | `6531b16b8aab4879ae7831aabae74043` | 5/5 passed; zero failed, skipped, or inconclusive; stable inputs |
| `git diff --check` | final candidate source/docs diff | passed |

The Smoke run's five passing case identities are present in the complete
EditMode report. Its filtered NUnit XML is retained privately with original
SHA-256 `f0cdcb6c984578a3e19e00165f91012493ec4183b2da22c692fd6aff8e511bd9`.
The evidence packager correctly rejected that filtered XML because Unity leaves
the unfiltered parent-suite `testcasecount` values in a selected five-case
report. The report was not rewritten. The public package below contains the
complete coherent EditMode hierarchy and all 3274 case results, including the
same five Smoke cases.

The CoreValidation original cold and warm TRX SHA-256 values are
`f9f2711075047e6aa6ecd2b6db230146880bddc2fef7675ffa1a1c9a75feaba6` and
`cfbd557ec377e92bb608019f5a9756df3c03cfedcd3d2756b33b5c7fd2eaac32`,
respectively. They and the complete run manifest remain in the protected local
evidence store. They are not presented as a public DotNetTrx package: the
approved DotNetTrx adapter requires its `dotnet-trx-source-context-v1` input,
which this CoreValidation runner does not emit. No context was fabricated and
no raw TRX/log was published.

## Public Unity evidence package

- Package manifest: [`coref-initial-restore-editmode-final-20261027.manifest.json`](coref-initial-restore-editmode-final-20261027.manifest.json)
- Archive: [`coref-initial-restore-editmode-final-20261027.zip`](coref-initial-restore-editmode-final-20261027.zip)
- Manifest SHA-256: `2f546064980abc454540c5054d03c7ee56a0482746dc5a353979cadd99dfc754`
- Archive: 173247 bytes; SHA-256 `480f04bb2613533ee09b314b9f014e9ece590129d1a33fff21074c43f1320c11`
- Original full EditMode XML SHA-256: `383f43be9a81cb3ee4a6102315c0634ee133a0e3c090fc1af813d4043781b484`
- Normalized packaged XML SHA-256: `cadf721830fd3811883c7d813ac85811c4feb3efe0a0bf176607df32a902623a`

The repository packager's `Verify` action passed against the protected
original run manifest. The ZIP contains normalized NUnit XML only; raw Unity
logs and original reports remain unchanged in protected local evidence storage.
The packager removed test output and applied its declared local assembly-path
projection. The manifest binds the package to the exact tested branch, commit,
tree, Unity version, complete input inventory, run result, and artifact hashes.

The sibling preliminary package is retained unchanged as an earlier full
EditMode capture: [manifest](../CORE_F_InitialRestoreImagePublication_20261027_Evidence/coref-initial-restore-editmode-20261027.manifest.json)
and [archive](../CORE_F_InitialRestoreImagePublication_20261027_Evidence/coref-initial-restore-editmode-20261027.zip).
Its archive SHA-256 is
`08ceb6b27ad6391adfeaacb4b7ea6b25d117b68122df9b862231cb62ab54dd5e`.
That package predates the zero-test runner safeguard; it is preserved for
traceability but superseded for acceptance by the final package above.

## Validation dispositions and protected local changes

- An earlier full EditMode attempt on the pre-fixture-fix source reported
  3273/3274. Its only failure was the reflection lookup for the old three-
  parameter private overload. The fixture now binds the current four-parameter
  overload; the full exact-tip run above passes. The failed original run remains
  private and unmodified.
- An initial PlayMode probe using `GMConsolePlayModeSmokeTests` discovered
  zero cases. The previous runner incorrectly marked that coherent empty XML
  as passed. It is excluded from all gates. This candidate adds the distinct
  `NoTestsDiscovered` status, a regression self-test, and the correct repository
  Smoke command. That probe also caused Unity to rewrite
  `ProjectSettings/ProjectSettings.asset`; the file is preserved locally and
  was not staged. `ProjectSettings/EditorBuildSettings.asset` and
  `ProjectSettings/ShaderGraphSettings.asset` are likewise preserved and
  unstaged. The final Unity runs captured a stable snapshot after those local
  settings settled.
- The zero-test PlayMode XML and the filtered Smoke XML were not rewritten,
  normalized, or used as substitutes for the complete public EditMode report.

## Remaining limits

The validated work is a candidate only. Core canonical remains unchanged;
P12-A, detached .NET host restoration, and the complete CORE-F application
remain open. This record does not claim broader persistence or runtime-host
readiness.
