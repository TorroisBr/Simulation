# Ordered P12-C/D/E/F Core restore staging validation

This record covers the ordered, unpublished Core owner-stage operation wired into the existing image-restore flow. It is a recoverable CORE-F candidate checkpoint only; it does not complete CORE-F or authorize canonical promotion.

## Tested source and inputs

- Candidate base commit: `1a4ff9d42675ea4f8833cf8c612abc50a341ca67`.
- Base committed tree: `52aa0a29e5db9e00fdbd05479d2ee5ceb5075e3f`.
- CoreValidation working-tree input digest: `03492e8babc4e856b46c1e7c795793e2e683ac925ee9db8a6330d199ae16824d`; stable before and after the run.
- Unity validation-input digest: `bd40de198b2906face582545ac644b295a3b5c9fa0533458a53e648b6e85a44a`; stable before and after the runs. The protected ProjectSettings inputs were captured by hash and were not staged or modified by this change.
- Changed production files: `DailyV1Application.cs`, `P12EDailyV1OwnerPackage.cs`, `P12FDailyV1OwnerPackage.cs`, and `P12GDailyV1RestoreCoordinator.ImageRestore.cs`.

## Validation results

| Gate | Result | Evidence |
|---|---:|---|
| CoreValidation cold | 988/988 passed; 0 failed | Private run `cbf8726cde5644539e6e24b3bddddb89`; original TRX SHA-256 `1142a2b800816534a4f0c6fc0f3e62d4c5784356c268ef8d8a30cdf0f429a038` |
| CoreValidation warm | 988/988 passed; 0 failed | Same private run; original TRX SHA-256 `45c41524dbdc010402cbbe7dbf4aefd498ef38e2760fef9294171b51dac5a481` |
| P12G political-binding restore suite | 16/16 passed; 0 failed/skipped/inconclusive | Private run `7be1c4ea71af44a7b02cd6d0f5ad448e`; report SHA-256 `b6e1481c94a609a3919c8f848579e211cf107303f8f65119f845156043d52293` |
| ALL Unity EditMode | 3294/3294 passed; 0 failed/skipped/inconclusive | Public result-only package below; original report SHA-256 `931d7cbd0565bc2304d39b41f14234bd2ee788518fb2135cab5cfc70cbdbab0c` |
| Official EditMode Smoke filter | 5/5 passed; 0 failed/skipped/inconclusive | Private run `78b7ba4000e3444ba060ad1e41d00ca9`; original report SHA-256 `106c4dce104fa3c89653e97f1a4aefefdd0fe521aef18eb48b55bb81143fafa3`. All five Smoke cases also appear as passed identities in the complete EditMode archive. |
| `git diff --check` | PASS | Candidate diff |

Unity used Editor `6000.3.9f1`, Test Framework `1.6.0`, and the official repository runner. The complete EditMode result package is `coref-ordered-restore-stages-1a4ff9d.zip`, SHA-256 `11870a73e70f03dfb18db60896d9894978700af842facd3ae252180930f627c5`. The approved verifier reports `VALID`, one run, one complete result entry. Its archive contains all 3294 cases, including the five Smoke identities. The separate filtered Smoke XML has inconsistent parent-suite aggregate counts for packaging and remains private and unmodified; no result was edited or inferred from it.

CoreValidation used pinned SDK `9.0.315` with `-SkipLab` because this checkout does not contain `tools/Simulation.Lab/Simulation.Lab.csproj`. It did run the Core cold/warm tests and CalendarDemo cold/warm gates. The worktree contained Unity `Library`; this run makes no Library-independent .NET-host claim.

The CoreValidation runner emitted `simulation-core-validation/v1`, not the separately required `dotnet-trx-source-context-v1`. Therefore no public DotNetTrx package or original-to-public comparison is claimed. Its private manifest SHA-256 is `7e23498d702f4ab1ba8750b5486057097cf9d0220b81cd7e3667310119b33805`. Original CoreValidation reports, logs, and manifest remain unchanged in owner-only local retention; no source context was fabricated.

Earlier diagnostic invocations that ran from the C: checkout, selected zero tests, or had the wrong working-directory provenance are excluded from these results. Their protected diagnostics remain unchanged.

## Evidence boundary

The Unity result-only ZIP and manifest are permanent review evidence. Protected original XML/logs and the complete CoreValidation evidence remain in local owner-only retention. This candidate is not ready for canonical promotion: complete detached-host acceptance remains outside this slice, and the .NET reports do not yet have a public package under the approved TRX adapter contract.
