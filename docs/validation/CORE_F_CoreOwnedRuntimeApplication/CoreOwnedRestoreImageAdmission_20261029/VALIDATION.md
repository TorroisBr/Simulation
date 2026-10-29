# Core-owned restored-image admission

**Status:** validated incremental candidate boundary; not a CORE-F completion or promotion.

**Base:** candidate branch commit `459e71997ac1ebff53f490f0dc82c4088a5c2c60`.

## Scope

`DailyV1Application` now checks the exact application-owned bootstrap context, owner thread, image header, selected host compatibility identity, and complete Daily-v1 owner census under its session gate. When an application is supplied, the Unity restore coordinator delegates those decisions to Core before invoking the existing typed owner decoder and staging adapters. The legacy coordinator path without an application retains its prior checks.

The regression covers rejection of a changed content identity, rejection of a split shared ArmedForce alias, and successful restoration of the valid image afterward. The source image and active source session remain unchanged on rejection.

Typed owner decoding, Unity compatibility staging, restored-session publication, `DailyV1Application.TryStart`, and detached .NET continuation remain outside this boundary.

## Exact source identities

SHA-256 values for the candidate working-tree inputs:

| Source | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | `900e9b02eca91ae42484ed26f45bc11d5dd43c8a6cc72e85eb91f9a77c4e5a07` |
| `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.ImageRestore.cs` | `842e9cadcae7e0cea3413bcbada4d1ee4dfc1e85cd699940e1a4304ca63ee33e` |
| `Assets/_Project/Tests/EditMode/Editor/P12GOwnerEntryQualificationTests.cs` | `8fba47d3470afdefa8d729b23d05f1e0313c26fa1a0017ec718c6fddea614122` |
| `src/Simulation.Core/Simulation.Core.csproj` | `562c9897a6c27d2b9298b44972a0f817722444c3fb0b47aa8dbcde23cadff68b` |

## Validation

- Official CoreValidation, `-SkipLab`: **922/922 PASS**, cold and warm. Runner source commit: `459e71997ac1ebff53f490f0dc82c4088a5c2c60`; run ID: `71cc0a7d7af44e93bf38e01ad56db661`. The recorded Core application and project-file hashes match the identities above.
- Official Unity EditMode filtered run for `IntegratedOwnerImageRejectionIsAtomicAndValidImageCanBeRetried`: **2/2 PASS**, no failures or skips. Run ID: `b2f4f6b3c8c74016aa471eedf86805dc`. The three changed C# inputs in the validation worktree matched the candidate SHA-256 values above.
- `git diff --check`: PASS.

Private original reports and manifests remain in the protected local evidence roots:

- `.private-core-validation/71cc0a7d7af44e93bf38e01ad56db661/`
- `.worktrees/validation-coref-dailyadvance-459e7199/.private-unity-validation/b2f4f6b3c8c74016aa471eedf86805dc/`

Core manifest SHA-256: `91825000e00b41af02c01bb3e226073e45bf0992e90e5ae6f3993bb24ae1e67b`.

Cold TRX SHA-256: `92d3baf363631fa9df311ae327130ee38e0292f78254117c1a65b7ebc8ab6296`. Warm TRX SHA-256: `fc1b27234c07e9df2dc4602ea11d63cc7d0fa27f568750b674d466e25ea85cb4`.

Unity run-manifest SHA-256: `31b4a587b2f00f2d1c27d34c20f01537ac5c6c773319806dd2902f2bd9a0879f`. Original XML SHA-256: `8ca444d02802b218ac27bcc58336bdcd6dd082bd7199478c6ab0843c583888c0`. Original log SHA-256: `fc3beea906141bf89c9e01539fd1929b5b445ead6f5bef02198f795ba3a7542c`.

This incremental boundary did not rerun ALL EditMode, Smoke, or LongRun and makes no claim that those full integration gates passed.
