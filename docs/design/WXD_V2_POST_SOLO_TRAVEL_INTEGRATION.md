# WX-D v2 integration after P12 solo-travel promotion

**Status:** Reintegrated, revalidated, and independently reviewed at exact tip `fbc6ee053dc034fb12bf374b899e6baeb91c1d05`; see [the review record](WXD_V2_POST_SOLO_TRAVEL_INTEGRATION_REVIEW.md).

## Baseline and drift classification

- Current P12 canonical before this integration: `4d9f48fceea4e0742e7ebc2c5199df5163051b7c`, the documentation-only State record following the approved solo-travel promotion.
- Solo-travel executable code on that base: `fe0e0be03403e92001173deae1fafe58dfe432d2`, tree `d72e84d6a442440ab82bacf6a0eb32165a9d7055`.
- WX-D source candidate code: `bc3d4a31e45549fd91cd22a88022c93d39f2707f`, original tree `088f383db5b4b5fc51e8db0f8687c413709a5796`; source candidate tip `85cd28af9171609f5bd35bd9016e4f0fe919407a`.
- Replayed WX-D code commit: `aab725b89366e65fd839c47b8ae36ad91cd560d5`, code-commit tree `d248892c37bac86142863d034e041384768553eb`.
- Test-run candidate commit: `2d23eb8a05615be98ec12da1c7b90d6317e23131`, tree `3b9dd4738f40b64d778c49a7d15189374d88b363`.
- Source and replayed WX-D implementation patch IDs are both `7491cc42329991071717051a20e2199c7c7a8981`; the same 22 source/package/test paths are changed. No P12 solo-travel production/test paths overlap.
- The P12 base delta is limited here to the reviewed `SimulationRuntime`/`TravelActionProvider` solo-travel operation, its tests, and P12 documentation/State. WX-D reads through the existing factual-read surface and does not use the Travel action path.

**Classification: `BASE_DRIFT_ONLY`.** The reviewed WX-D patch replayed additively without conflict or code edits. Its mapping, truth authority, failure behavior, and file contract remain unchanged. Since the new base contains a `SimulationRuntime` change and the complete executable tree differs from the old review tree, the targeted factual-read/bootstrap regressions, ALL EditMode, official Smoke, and a fresh exact-tree review were still required.

## Current external contract

Simulation-External `origin/main` was live-checked at `0ce8403ba05f778db6850f566a974a4c56cf4edb`, matching the reviewed authority. Its v2 schema source blob remains `5619013647c31d969a7cd49e0563ff68c78cdde3` at `packages/world-schema/src/index.ts`. The retained first-Faction artifact is SHA-256 `AD20EF75B379E4A03E361719ED30A642F28DB54300987B9C0D6280AA579EDE5D`; the original exact producer patch and exact external schema authority are unchanged, and the prior `validateWorldExchange` conformance result remains applicable. Simulation-External was not modified.

## Current validation

All Unity runs below were performed after recomposition on tested candidate commit `2d23eb8a05615be98ec12da1c7b90d6317e23131` / tree `3b9dd4738f40b64d778c49a7d15189374d88b363`. XML/log files are retained under `Library/ValidationResults/WXD-post-solo-travel/`; each XML reports Passed, zero failures, and zero skips.

| Gate | Result | XML / SHA-256 | Log / SHA-256 |
|---|---:|---|---|
| WX-D `WorldExchangeV2ProducerTests` | 7/7 | `EditMode-20261003-184231-47ff4321fdb747f68df07d253e5a1afd.xml` / `9BEF9A332F5942EF86C78B35BF273A68BF8F95D215C6474E1C8FC0FF46868A40` | `EditMode-20261003-184231-47ff4321fdb747f68df07d253e5a1afd.log` / `6BA2F575FC7FB2FEDBADA69E979D70B2EEDB1CA2178CCDDE65D3709F212A113C` |
| FR-B `FactualReadFoundationTests` | 9/9 | `EditMode-20261003-184427-f1bd970e3bf442539d937ef361415223.xml` / `A3677C7EB0C404F0211D4EFA3832AB69D22A111713CAAA148697CF55C7324BF8` | `EditMode-20261003-184427-f1bd970e3bf442539d937ef361415223.log` / `05ED328707E4F03438AE717BBD18D749390B2D3FC53105EFC3C1E38A4CAE2690` |
| FR-C `FactionFactualReaderTests` | 7/7 | `EditMode-20261003-184438-b98c33b8b55e4d0ba52f2f66a0964e6f.xml` / `EBE8C50AB1268E84BB53C524E331CCCE2068F2E435959D7E4E0E0AA160D11974` | `EditMode-20261003-184438-b98c33b8b55e4d0ba52f2f66a0964e6f.log` / `5F7177B40362CCF067F48876D004FB7AD85E9E14A7384D3CE338B01C51E1F13C` |
| `SimulationBootstrapCompositionTests` | 21/21 | `EditMode-20261003-184447-bfa3016f8447465a96d26bff3614fe5e.xml` / `2E55E6AE43AD265BE037FD2D79EAB0B8F770EBB2A72EA1E012B126011ADCBFFF` | `EditMode-20261003-184447-bfa3016f8447465a96d26bff3614fe5e.log` / `919195DC9B61199D55F0D667F53F753FF1F88D1982F386F81A80A5E1606CE126` |
| ALL EditMode | 2223/2223 | `EditMode-20261003-184457-6d129ec05738435ebf61515301e93d1f.xml` / `B2A98ECA1401B6576B53BBABC3D0987076CCCD3C2EB97A069DAAC977B2627ED4` | `EditMode-20261003-184457-6d129ec05738435ebf61515301e93d1f.log` / `116E67A459C3DFF0B7B6935BB69A3B433D7F273036EAB3BEF9746B165EFE3123` |
| Official EditMode Smoke | 5/5 | `EditMode-20261003-184525-4976881737d8475186e50f29ed4866fb.xml` / `BB99BE8F35BD0200E1A205F12CCADABCAFE9C3C5AC9037A7B5DF87B068DDD37B` | `EditMode-20261003-184525-4976881737d8475186e50f29ed4866fb.log` / `1F73D9AAFA7CBC07D72B31E1B631EE529F5ED3B7A0425957D7DB5DA56E2C10D2` |

`git diff --check` passes for the integrated candidate. No WX-D executable file changed after the tested candidate commit; subsequent integration-evidence and review commits are documentation-only.

## Preserved producer boundary

The producer remains limited to WI-A `WorldId`, FR-C Faction factual projection, World Exchange v2, truthful `collectionCoverage`, deterministic artifact/file generation, and fail-closed export for unavailable, malformed, or inconsistent factual reads. Factions are `INCLUDED` when the complete collection is nonempty and `KNOWN_EMPTY` when empty; other unsupported collections remain empty and explicitly `UNSUPPORTED`.

This integration does not establish whole-World projection completeness, P12-B completion, save/load, IPC/live sync, write-back, P19 integration, broader FactualRead completeness, P12-A readiness, P13 readiness, or any new gameplay capability. It is a cross-track WX-D producer handoff, not a P12 continuation checkpoint.

Fresh exact-tip review of the recomposed implementation is pending.