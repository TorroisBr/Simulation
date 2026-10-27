# CORE-F Core-owned detached P12-C root staging — validation

## Scope

This bounded integration moves detached P12-C root reconstruction behind the
exact `DailyV1Application` boundary. The application authorizes staging against
either an unused bootstrap application or the exact published source session
held under its owner-thread replacement reservation. Core decodes and stages
the immutable P12-C roots; the Unity adapter projects only the retained P9
manifest for the legacy Unity runtime view.

The change does not move P12-D/E/F owner hydration into Core and does not
complete detached restoration, `DailyV1Application.TryStart`, the independent
.NET host, or CORE-F.

## Source identity

- Repository: `https://github.com/TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-shared-daily-v1-app-host`
- Validated parent: `05cd17b366ba768fff0ed60ecfee9f388fe4847f`
- Parent tree: `9827ba125e0e37239f2f272e5f120c2aedb70173`
- Core canonical baseline: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- CoreValidation run input digest: `a04d60512b91216dac4153f29d7b1b51b725f65a98d458d4ff6d5755306ed0df`
- Unity validation input snapshot: `bbfa8eb16672334ff1db40942a3f18a5771260f3a7361fc32080ccf60a58264c` (1,956 inputs; stable before/after)

Validated implementation and test file SHA-256 values:

| File | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | `2ea5755bff56000dcbb48a365b0f67ccc24a01d2920086a99d2ba0174e7fb165` |
| `Assets/_Project/Scripts/P12ACausalRootImageAdapter.cs` | `d920bde6f51bea70549b0759c2c095e221f296ef2d2ebf7e589f1a152bb45c45` |
| `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.ImageRestore.cs` | `fc1c8df8e32002576e3c4c7a2dab2d02f3d796fec7942a6bddf6f00a0e9c317a` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs` | `b93b5603a16265b144ff46eb4159ffbf76aa3a5de025601c4ae180b036653dc9` |

The three pre-existing `ProjectSettings` edits remained unchanged and were not
staged. The Unity input inventory recorded them as part of the tested checkout.

## Validation

- Official CoreValidation run `cb58cdd169274f418e37292f6201fd26`: cold **893/893**, warm **893/893**, zero failures. Private run-manifest SHA-256: `49e0c281776de97b80d4215114350fdc1143b818f7610f71f7739950996e90b7`. Original cold TRX SHA-256: `94e5b4f5498b288391ccd4d5037ca040339ba481bf270c95759845d137d9fd89`; warm TRX SHA-256: `043339d7f81ae94554940e1dbb6b03bc2fe2c96d5a67794f14483d1b90f7ebe9`.
- Focused Unity continuation restoration test, run `70b785281b224313b2f1ca087848a1f3`: **1/1 passed**.
- Unity P12 regression selection, run `90aab40d42fe41cc8622b9577c61a4dd`: **564/564 passed**.
- Full Unity EditMode, run `14aa2c0880854c03adb093a4dbaa9bc2`: **3274/3274 passed**, zero failed, skipped, or inconclusive.
- Official Smoke selection, run `abb85d271f18466787f758941820c314`: **5/5 passed**. The P12 and Smoke case identities were each verified as subsets of the full EditMode report.
- The full EditMode result package is [`coref-p12c-editmode-20261027.zip`](Evidence/UnityFull/coref-p12c-editmode-20261027.zip) (172,962 bytes; SHA-256 `14a9ec718afac9d0fd0fd9bb5f8c132180badb116702d2164bc8aba31fcfa55c`). Its manifest is [`coref-p12c-editmode-20261027.manifest.json`](Evidence/UnityFull/coref-p12c-editmode-20261027.manifest.json) (SHA-256 `7d701baf36bedc36d7f6f31935db12de13bf1f34833d12bc2e6fc54636f03d65`). The official verifier passed with one run and one result entry.
- `git diff --check`: passed.

The original CoreValidation TRX/logs and the focused, P12-filtered, and Smoke
Unity XML/logs remain unchanged in protected local E: evidence retention. No
public .NET TRX package is claimed: the current CoreValidation manifest is the
`simulation-core-validation/v1` format, not the separate
`dotnet-trx-source-context-v1` adapter context required by the TRX packager.
No substitute context was fabricated.

The Unity packager rejected a combined package containing the filtered reports
because their XML retains unfiltered enclosing-suite `testcasecount` metadata.
The filtered originals were not rewritten. The complete EditMode XML passed
the packager and verifier and contains every P12 and Smoke identity reported
above.

## Status and limits

This is a validated implementation candidate only. It does not claim CORE-F
completion or canonical promotion. P12-D/E/F owner hydration and whole-target
graph validation remain in the Unity restore coordinator and are the next
coupled restoration boundary to move behind the Core application.
