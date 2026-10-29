# Selected NPC census owners — validation

Date: 2026-10-29
Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
Validated code commit: `67d384c79cfd9942955fa6fdfb9aece18e2c8901`
Validated code tree: `96a32d93d2d5e550270f293d580be31025dc7659`
Parent: `457a17da8a7d1b0df0a852af9a65fc8062204244` (tree `93d1332c1f5793517b838eebf80fb229d13e5259`)

## Scope

This checkpoint binds selected Daily-v1 NPC Inventory and MoneyAccount census witnesses to their exact Core owners. The selected-only census uses those Core object identities, cardinalities, and local revisions. When any roster member lacks selected owners, Inventory retains the established whole-family Unity projection fallback; MoneyAccount remains projection-backed for that mixed roster. Census reconciliation permits the Core-owner/projection identity transition only when the projection aliases that exact Core owner. P12 snapshot identity checks follow the same selected-versus-mixed rule. This is an implementation checkpoint only; it does not complete CORE-F or claim detached-host, complete runtime, capture-readiness, or Phase acceptance.

## Validated input identity

The official CoreValidation runner captured 509 source/project inputs and four generated dependencies with stable snapshots. Its working-input digest is `4007d7a1d9708d4a44d42116f767d074c62dfc983d039e7795da6d8949d35599`. The runner recorded pre-commit HEAD `457a17da8a7d1b0df0a852af9a65fc8062204244` and tree `93d1332c1f5793517b838eebf80fb229d13e5259` while the candidate source changes were in the working tree. The committed code tree above contains the tested implementation; source hashes below were checked after commit. No source or test edits followed validation. The official Unity run used a separate E: validation worktree; every changed Unity production/test input and the new Core provider source/metadata compared byte-for-byte with this candidate. That validation checkout had independent local ProjectSettings edits; they were left untouched and were not included in this candidate commit. Its complete input snapshot is preserved in the official package. No claim is made that those local settings equal the candidate worktree.

| Changed source/test input | SHA-256 |
|---|---|
| `Assets/_Project/Scripts/ContinuationCensusProtocol.cs` | `c88e13093e08260133e72db18d403dbe10da034f1c409d9ee6dd6fc526687289` |
| `Assets/_Project/Scripts/NpcInventoryCensusProviders.cs` | `cbe02eac70c048e625620e2fada59d4fec632b7ccda0737e86ace1f5eadfa225` |
| `Assets/_Project/Scripts/NpcMoneyAccountCensusProviders.cs` | `8e21cd354a2c97dc7f642c1c0ca09a56f5972ae757149f1d9335b234462f7b9b` |
| `Assets/_Project/Scripts/P12DNpcRootOwnerSnapshot.cs` | `070098181216523b935bbe3bf3c06df0848447475ba0cffd2976fd8b86d81774` |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `7918c4b23111f61ae6e9a5f5e7c2a8e3ceb8791977173dfc8c9c636af75b3f61` |
| `Assets/_Project/Tests/EditMode/Editor/Core/CoreFDailyV1OwnerClosureTests.cs` | `97eed2c2b178fd9b5b9fb2eff9920ef7b4aea5da9178a53cb8697afe19575d52` |
| `Assets/_Project/Tests/EditMode/Editor/NpcInventoryCensusTests.cs` | `10fe59400b3020fc0b48c4b1829176793856cc7a44eee403e2b5bc3400a4af95` |
| `Assets/_Project/Tests/EditMode/Editor/NpcMoneyAccountCensusTests.cs` | `41aa379f831810a496783babf34edc6ec60fac4ce4107d19dc882e9859bcbe11` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs` | `8001576be057116bfcd34b39f0a9bb6e60c06916d38c2cc2b8ad8358b1f95abc` |
| `src/Simulation.Core/Simulation.Core.csproj` | `562c9897a6c27d2b9298b44972a0f817722444c3fb0b47aa8dbcde23cadff68b` |
| `tests/Simulation.Core.Tests/DailyV1CityEconomicOwnerCoreTests.cs` | `ba27fa54ff7f0ed7fc2953063f87072c62919d1faf5de5fd53436fe5996bad01` |
| `Packages/com.simulation.core/Runtime/Person/DailyV1NpcOwnerCensusProviders.cs` | `640ef3740d9109c9561ebced676b5b45f900a7545f8d7ea5e319932a81cdb158` |
| `Packages/com.simulation.core/Runtime/Person/DailyV1NpcOwnerCensusProviders.cs.meta` | `66adf508d2b615fcee17a241433884fb4abb1e03f5a728b2202c8211b6679e0c` |

## Official CoreValidation

- Runner: `simulation-core-validation/v1`; run `fe1855aa498549cd82ef5ec13060fe6f`; status `Passed`.
- Cold: `919/919` PASS. Warm: `919/919` PASS. No failed, inconclusive, skipped, or unexecuted cases. Generated dependencies remained stable.
- Original cold TRX SHA-256: `49b71d8945b8b6ce1137e7a5ba9d7c3bb1257169d51dcc8847d69754abb5add9`. Warm TRX SHA-256: `7f8c193e1b629ce42719800d907fdfa9f3a666259f4ab1cc2c1f16e644590752`.
- Private runner manifest SHA-256: `4bdd3b4e93eea35daabb54aa3599a60d91bac59dc49f89d3a31922f4e82f8007`.
- The original reports and raw logs remain unchanged in protected local evidence. This runner did not emit the separate approved `dotnet-trx-source-context-v1` adapter; no public normalized .NET TRX package is claimed. The run detected a Unity Library in the checkout, so this is not a Library-independent .NET claim.

## Official Unity validation

- Full EditMode run `2d041fbfc27742ebb1501a9a7803e86f`: `3279/3279` PASS, zero failed/skipped/inconclusive; input stable; harness `1.1.0`.
- Official Smoke run `67d28f0ce86f41d3ab92ae49ce2d2ecf` (EditMode filter `Smoke`): `5/5` PASS, zero failed/skipped/inconclusive; input stable. The full EditMode report also includes these five Smoke cases.
- Focused EditMode runs also passed: owner closure `cbd03dc3540a402ebfc49250c07cfcc0` (1/1), P12-D NPC root snapshot `337c60882e674957838ccc3c59f994c9` (33/33), Runtime Admission `7bee9f3061d24ce791cf4e863d8260db` (228/228), and Bootstrap Composition `ff40773ec709498691c59e7c9ea622b4` (39/39). Their originals remain protected; the complete case results are represented in the full-suite package.
- Normalized full EditMode package: manifest SHA-256 `1eb46cb2b6fedb75ada8412b6ec4f457fe15347ec86a465f0ad43fc90416af59`; ZIP SHA-256 `bcac673aca538aa63f15d36bd4076ce572f34b76c45f46cdc8c8e508e300898e`. The official packager verification passed against the protected original run manifest (one run, one complete result entry). Original XML/logs remain private and unchanged.
- The standalone filtered Smoke XML passed as an official run and remains protected. The packager rejected this filtered XML because its parent suite count does not match its descendants; it was not normalized or relabelled. The full EditMode package remains the complete normalized result record.
- `git diff --check`: PASS.

Unrelated ProjectSettings edits, untracked files, metadata, and private evidence were excluded from the candidate commit and preserved. This candidate has not been promoted to Core canonical.