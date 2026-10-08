# P12-E ArmedForce / Manpower / Position — Current-Base Revalidation

**Result: REVALIDATED ON CURRENT BASE; READY FOR INDEPENDENT EXACT-TIP REVIEW.** This record does not promote the candidate or claim P12-E completion.

## Exact refs and tree

- Canonical branch at preflight: `codex/phase12/canonical`, local/remote exact SHA `ed3aad0bcf98fc1b709b6bc632452689448b8803`.
- Prior base: `04e7c3f49a7c9690ebc091fcfc50f05ef67009d6`.
- Revalidation branch: `codex/phase12/P12ECurrentBaseRevalidation`.
- Exact tested candidate tip: `99302cffec8cda35945ed191bd3dba98beb7bd57`; repository tree `2d025424512e4a8c9cda10b840714b444ebbcdfa`; `Assets` tree `811f8018c5722a6cf5a0bbf54ec04a9f73f77397`.
- Candidate is based directly on the current canonical SHA. The E implementation/test delta is additive and applies without conflict.
- Original candidate tip: `d929a57e3d173912666a452ad37987e714a9f8c6`; reviewed implementation/test commit `90481acc0caae36385b3ec2e58d3a9b9b03316c0`; original reviewed `Assets` tree `af1af0db0799431b796a53509a1e8fa130d5611c`.
- Reviewed P12-D promotion changed `CityRuntime.cs`, `NpcRuntime.cs`, `P12DCityRootOwnerSnapshot.cs`, and its tests, plus State/review/validation records. E changes `ArmedForceSpatialPosition.cs`, `ArmedForceStore.cs`, `MilitaryManpowerFoundation.cs`, adds `P12EMilitaryOwnerSnapshot.cs` and its tests, and the E design record. There are no common changed paths. E's staged Person and P8 spatial inputs remain upstream dependencies; D's City/NPC assembly changes do not replace those authorities or the five E census sections.

## Validation

All runs used `Tools/UnityValidation/Invoke-UnityValidation.ps1`, serially, against this exact worktree. Every XML reports Passed with zero failures/inconclusive tests. Compressed logs were decompressed and SHA-256 compared with their raw logs before the raw duplicate was removed.

| Suite | Result | XML | XML SHA-256 | Raw log SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|---|---|
| `P12EMilitaryOwnerSnapshotTests` | 10/10 | [`XML`](Raw/Focused-E/EditMode-20261008-193727-7117afb9d2774a558f2a39e767c356e0.xml) | `49D6F9345680F7627CB31363BFE07016AE1EE068CCADDE596B2CA864A56045CC` | `64E40271C159BA279DBA473D66660A35D9B187F136A3FE34219391ACE0A62AAD` | `13848E5F19176E9377921776823E6FB3973E0917C3053A265C949B95E11F10EF` |
| `ContinuationCensusProtocolTests` | 24/24 | [`XML`](Raw/Focused-ContinuationProtocol/EditMode-20261008-193744-49454196dbf54510817e4dbd294a2ba8.xml) | `EF7479CC543B3647B148D09E6884CDC4207ED45C8EB60A0B3F01B4587AD3BBFE` | `BC52779555D11F0E72B739CC300325279F423E06E280804E71FBDE67FC2DEFCC` | `11CF38CE389EF71085FEFEC3B2E4460EE9D55DBB457702B8F4824D7F47699DCF` |
| `P12DNpcReceiptOwnerCensusTests` | 13/13 | [`XML`](Raw/Focused-ReceiptOwner/EditMode-20261008-193801-345380d0860f40af8a477c29c8d9f90d.xml) | `44F368E9B95E67CFE3792356820F2C32B1DBC6AD28490E990CC0B4D28331F1EF` | `A45059B2C8B1A3327CDA1925832B152CD8B9AB99C10360A08FA966F3B167D963` | `1085E5BF0A35ED874E4CE85F2487245B825E57F3C016A1D04E5D3DE032EBEB0E` |
| `P12DCityRootOwnerSnapshotTests` | 17/17 | [`XML`](Raw/Focused-CityRoot/EditMode-20261008-193818-5adf9e0fb74e4725aafc11e3d4bd37f6.xml) | `3FE0C525C22DF1F82619D1A2C30C033D108DC80E36F5748CA6BF188B2F745690` | `5997269510AE3151074A025D6EBA984EB11904A9EAF1165B36E8BF5A825B35CC` | `6A24461FFAA32B4D7B5F9E9D7FE908F570DACE08746AD88AD6E5515B62EE78A2` |
| `SimulationBootstrapCompositionTests` | 26/26 | [`XML`](Raw/Focused-Bootstrap/EditMode-20261008-193835-2e32709512574d25b8d11911413b5081.xml) | `46BF9BE9A00E57DC96A0698F4646D3CE91CF64F5244D9161B1898208AACFB75D` | `89F382AA85FE2B869D1537F9FD555190639A6B39EE176C21CB67675BF7E4A5B0` | `134593159A717F00C119F7C5FC67DF8B3D141D31BFD29B3D70522CEB229F275A` |
| ALL EditMode | 2602/2602 | [`XML`](Raw/AllEditMode/EditMode-20261008-193851-216b15b3422b414084781899534cbb0c.xml) | `B9E4B3CEFE5025B6832FB5DCB28D634A7C99C969830645C53F3E9094729EE52D` | `86D8AEA0E12E3721C3E50BEFFC41F669182CCA23E66042B981C61DF650948F05` | `671861D7B6810AEB894DECC2290B05739878EF5F4263CE0B59ACF340039FC38E` |
| Official `Smoke` | 5/5 | [`XML`](Raw/OfficialSmoke/EditMode-20261008-193934-0e43a1154fb3411bafbba78114394af5.xml) | `3787FC797332F145DB5026048750F69C6D4C07D3CE30B1E3CE22AD09E3929EE1` | `8873B3E90C6E88FDEEB648C5BB29CACC91F5AC95BE6D34C2309F481CAA5B6173` | `22748D40BF6F1F78E98145EF0F2F424A2F2ECB73106D8CA63656999669311BBB` |

`git diff --check origin/codex/phase12/canonical...99302cffec8cda35945ed191bd3dba98beb7bd57` passed.

## Non-evidence attempts and limits

- An initial harness invocation could not create its output directory under the restricted worktree and launched no Unity process (`NoResultXml`). A later successful set wrote results under Unity's project `Temp`; subsequent Unity startup cleared those temporary artifacts. Those runs are not used as evidence.
- All seven suites were rerun serially with persistent result directories under this record; the table above contains only those final results.
- The exact-tip implementation review must be renewed on the resulting integrated `Assets` tree. This revalidation is not self-review, promotion, P12-E completion, P12-A readiness, P13 readiness, or Phase 12 closure.
