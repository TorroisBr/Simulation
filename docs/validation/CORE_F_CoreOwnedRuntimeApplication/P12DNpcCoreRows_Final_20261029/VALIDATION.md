# P12-D Core-bound NPC rows — validation record

## Source identity

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Tested base commit: `dbe896a51937bea6c3e464033997c840dbd5cfc6`
- Base tree: `1274b8f028f4e5ed6217868d7dba4362975e67d7`
- CoreValidation working-tree input digest: `78b52cd4aea26d1da18106b01cf6a05a6efca708a7a073e18b5e123b8894373e` (stable)
- Unity validation input digest: `b8bbdc89c1a40b74859b1751e3f298fdeefbf7e115cc831021b94765f63a9fa8` (identical before and after focused, full EditMode, and Smoke runs)

Changed implementation and test inputs:

| Repository path | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | `2aec9c70fdc2c51862aadd5c3e6786a60d3e8e11fe95685fa666d3af05f91ae6` |
| `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GContinuationSourceWitness.cs` | `c519dc9f0021e6f053c5c0853d1e68769e3e591c96d00e426a5dd2cd2452d808` |
| `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GCoreBoundOwnerSources.cs` | `43b83f4f1bce695fbf97d40e5d06c5112fcdda0dc61c0a3a3109d49ad1851f0c` |
| `Assets/_Project/Scripts/P12DDailyV1OwnerPackage.cs` | `bd75d5677cdc193bcc19087703e7534ff624b1a940b585933544c645eb0b4232` |
| `Assets/_Project/Scripts/P12DNpcRootOwnerSnapshot.cs` | `397195ed393dfc17a7a31e70db474d1700b0fd76cb49a452d6bd1b3ae07bcede` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs` | `924cbc60a50478ffa6e84e64162b3a9c7a61b7169f848ce656776da4818ada0f` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `877ec262b1db867868b762c5ea1c54326c0c5162bf18cfe569c2b36c1de8b839` |

## Passing validation

| Gate | Run | Result | Evidence identity |
|---|---|---:|---|
| Official CoreValidation cold and warm | `882cf6f2597e477ab716d48220bbd696` | 936/936 each | Native manifest SHA-256 `4fd2dae385750c5997418401731c75fe1beebe3cd432dde1b3b399160316c06d`; cold TRX `41e945a788a8c28ffd81eeddbfe34dd917b6282d4a13d70c309e6d3c7df92d8e`; warm TRX `de335afc09987cdaf95a97ba857aa7af06e829931f61d0182224bc9566cbfbdc` (protected originals) |
| Focused detached image restore and Core-row binding | `50ba066efc9243f3b17e6fc9281144ea` | 1/1 | Input stable; protected run manifest `241fb4e70e3081bd7faeacca6a4cca26524ac7094578a5aaf5a004c0026587ef`; original XML `357ab8b993914438d4b55c66d731df58e7a4f3d291d1a324e5301ced7590eb36`; log `7ab9fd6e28bff0249c6ab292e67b42b9ccf6b89f88c46ff8c559bccc8202e5e6` |
| Full Unity EditMode | `1e924c533f454cd0a221fc05d68f8ee1` | 3279/3279 | Input stable; protected run manifest `5c251187fc8739573c9dc0f95459d7ab3b53c7161e8913cb88ddba40eee844b8`; original XML `053bdd4c2c6edc24821e82fe3e87c42be719c44b380bbb48433841d1e3d1dddb`; log `cf9b688bba0c5cff6e341c950b15d16aa51d79bf7c2e0a9bd6c2ea0b2a2c5c30` |
| Official EditMode Smoke filter | `52fa1e1c054146e4b50dd92ff7e8dce3` | 5/5 | Input stable and identical to full EditMode; protected run manifest `387b34588284ff48bf0caea65794326a46c7ce80861743b4468e195e8de46556`; original XML `4b77c77741846c016ade13f5dac209efb2c8a2065ec67be2d049e28f66990b7f`; log `e9450cd1d7945731cb97c779f8aa0c1a8a2bb9a2db74abe9fe595bd48a0db75f` |

The focused and mixed-roster regressions passed in the full run. All five Smoke test identities also appear as passing cases in the complete EditMode result. The approved normalized full-EditMode package is `coref-p12d-npc-editmode-3279-final.zip` (174,071 bytes; SHA-256 `ced9dd0b2c6c82ecabc2f10937c50a8dd181eae82425a9044c655f9639a7149a`) with manifest `coref-p12d-npc-editmode-3279-final.manifest.json` (SHA-256 `ee882b883c8194438ce32649fc82b8bef59f384d5d7b356dee25a3906074d03e`). Package verification against the protected original run manifest passed (`runs=1`, `entries=1`). The filtered Smoke original remains protected locally and is not included as a separate package.

`git diff --check` passed on the tested source and test changes.

## Excluded diagnostics

These attempts are not acceptance evidence:

- The first CoreValidation invocation used Windows PowerShell and failed before compilation because the evidence helper could not load `System.IO.FileSystem.AccessControl`. The same official runner then completed under the installed PowerShell 7 host.
- The first PowerShell 7 compile found a test-only reference to a non-existent census helper; the test was corrected to use the existing `NpcLifeResidenceSectionIdFor` API before the passing cold/warm run.
- An EditMode run filtered by category `EditMode` and a PlayMode run filtered by `GMConsolePlayModeSmokeTests` each reported zero tests. Both are excluded; the unfiltered full EditMode and correct EditMode `Smoke` runs above are authoritative.

## Scope and preservation

The Core save operation now retains ordered P12-D/F rows exported from the exact selected NPC owner graph under the completed-boundary witness. The Unity adapter consumes those exact rows only after the complete selected roster, owner references, definition graph, order, and row RuntimeIds match; it still validates the compatibility projections. When the selected graph does not cover the entire admitted roster, no partial Core row set is bound and the existing whole-roster Unity fallback captures every member. Legacy no-operation capture is unchanged. No P12 section, row schema, or image format changed.

The three pre-existing `ProjectSettings` modifications, unrelated untracked files, private evidence, and protected originals are outside this candidate payload.
