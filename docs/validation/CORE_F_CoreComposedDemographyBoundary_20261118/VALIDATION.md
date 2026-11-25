# Core-composed selected demography boundary

Date: 2026-11-18
Candidate implementation commit: `c17fe044b6053d6a84278843b6d842b80b8366b8`
Parent: `6af0932c3490e9ded814ac6e64004dbafd284a82`
Tree: `1c60ae76289ab132787c5ae83a9920507ad984d8`

## Scope

Core now composes the selected Person-death P12 boundary from the admitted execution state and exact owner graph when creating selected demography ports. Host ports retain sample providers, aggregate providers, optional Unity projection, and compatibility resident-floor facts. A detached host may omit the projection boundary; Core remains responsible for operation admission, authoritative owner writes, census validation, and publishing the single mutation epoch. The Unity mixed-roster fallback was not changed.

The regression creates an admitted resident Person/NPC/action graph and executes `PopulationResidentDeath` through the demography context composed by Core with no Unity projection. It verifies Person, NPC, action-slot, and City mutations and revisions; one epoch; released P12 reservation and runtime operation; healthy protocol; and complete census recapture. Existing null-projection rollback coverage remains in the suite.

This checkpoint does not complete CORE-F, publish public `DailyV1Application.TryStart`, or claim a complete detached .NET host or cross-process continuation.

## Validation

- Official CoreValidation: cold `1,159/1,159` and warm `1,159/1,159`; run `13fcef5cc63c4f52848b7e71af277bf1`; working-input digest `4ec240575be25ba437f8d673b6f64f37d44d2ff63debd0f638d99306ce21a8c5`; private manifest SHA-256 `4ddd6be18a18123548d838b76e79676ee3ec8d96498ecd883024c842194132ab`.
- Official Unity EditMode: `3,322/3,322`; Unity `6000.3.9f1`; run `ed4438df5cec43c0a08aa981efd29277`; stable input digest `8558c82141a8b170c07a1a0495ca69f2e47abaa010dbf6276162f1a40c534b47`; private manifest SHA-256 `69e9ffe9642cf9803e61d96c7ab65f6dd83175adf08e0383bc8617ccef9fa614`.
- Official EditMode Smoke filter: `5/5`; run `9beed1536a864cba9300801d659bbb69`; same stable Unity input digest; private manifest SHA-256 `565e2e49fd551fb3205088abe885e6ba38b62c2921e1870c1f5548847167f063`.
- `git diff --check`: PASS.

The CoreValidation manifest records the base commit and the exact working-input digest; the tested source bytes were committed unchanged in the candidate tree above. The Unity full and Smoke input inventories were stable before and after their runs. Original XML, logs, manifests, and private runtime traces remain in the protected local evidence area; no raw reports or local filesystem paths are published here. The CoreValidation run noted that this checkout has a Unity `Library`; this result does not claim Library-independent execution. The Lab gate was omitted (`-SkipLab`) and is not claimed.

Two earlier Core runs on superseded regression-fixture revisions failed during test setup. Their original reports remain preserved privately and are excluded from this final-source result; the successful run above is from the final fixture source.

## Reviewed source identities

| File | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedDailyExecutionStage.cs` | `f9316d5107fca8b47df05923550b36134a223e865a92c4278577203e335d375c` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedDemographyStage.cs` | `fc4fe4fa494d6bfcf8e4aa693ac4cc4b7f9c583c33d2491bd4245bf81c378fff` |
| `tests/Simulation.Core.Tests/DailyV1SelectedDailyExecutionStageTests.cs` | `b3529c767fbf6e54ead85c89b6c32d176abf56dbcb2102499a78631269b2d336` |
| `tests/Simulation.Core.Tests/DailyV1CoreSelectedPersonDeathBoundaryTests.cs` | `ee512ef9bcfb6d578de78fe667c0981736040dc3e9c71d93dbe71a4cc4a6825f` |