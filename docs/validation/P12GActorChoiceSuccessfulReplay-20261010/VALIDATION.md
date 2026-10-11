# P12-G terminal successful ActorChoice replay validation

## Candidate

- Canonical base: `54e5fadc44b2be938abe18d98355b8a02c3203e4`.
- Implementation commit: `eb1865f242f8f0d422d2d5d020b98c78e98330df`.
- Git tree: `8462f6fa87fb85115e9ea8f6af5c0b79dde117be`.
- Assets tree: `675832457d2084b7d8255047472cb4bcb605f2d0`.
- Changed implementation path: `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` only.

The test extends the already-promoted rejected-command case with a successful
SellGoods sibling. It uses the existing Daily-v1 Pedro NPC, registers one test
Person in both equivalent runtimes, binds that Person to Pedro through
`TryBindExistingNpcToPerson`, and queues the authored `VENDER_MERCADORIA`
choice using the existing trusted LocalPlayer/Request handler. It asserts the
terminal `AttemptReturned/Succeeded` history, restores the same captured
boundary, resubmits the identical command ID and payload, and proves duplicate
rejection leaves terminal history, selected facts, the complete included owner
projection, and continuation roots unchanged. The existing two-boundary
continuation parity assertions then compare restored and uninterrupted worlds.

No production code, security boundary, gameplay behavior, authored content, or
profile configuration changed. The fixture adds only a Person binding to the
existing merchant in both test worlds. This does not establish general
no-replay behavior for every command or owner.

## Validation

All runs used `Tools/UnityValidation/Invoke-UnityValidation.ps1` against the
same implementation commit and Assets tree. Each retained XML has zero failed,
skipped, and inconclusive tests. The raw Unity logs are retained as `.log.gz`;
the raw SHA-256 recorded below was recomputed from each decompressed log.

| Gate | Result | XML | XML SHA-256 | Compressed log | Compressed SHA-256 | Raw log SHA-256 |
|---|---:|---|---|---|---|---|
| Focused terminal ActorChoice cases | 2/2 PASS | [`XML`](results/focused/EditMode-20261011-011651-9f498eb7b63746e087bbba284244e5cd.xml) | `1DFB5087B383C3789E76F919ADA41AB7DCB1E5F58E9790DF877AE33CE61BC8ED` | [`log.gz`](results/focused/EditMode-20261011-011651-9f498eb7b63746e087bbba284244e5cd.log.gz) | `0C59A5E2C89E6E7B69FB2BC4A431337A9F19BE21BFD7E5EE3A439EC60B58C084` | `44DB9B16E06BEF85CAA32DDF9503CDB73E85815A59373F2AD61DF8ECA8E8AE46` |
| `ActorChoiceRuntimeTests` | 11/11 PASS | [`XML`](results/actor-choice/EditMode-20261011-011716-26277252938b4331a199d127c06eada5.xml) | `D6BB38753C925F445EB4EA04EBFB31CE15A7123839D3855B594EEBA97A3C47DE` | [`log.gz`](results/actor-choice/EditMode-20261011-011716-26277252938b4331a199d127c06eada5.log.gz) | `2BD88B790A2AFF3F5C3DBE564587CD1738741D9B7A9CD1D1DEB9A006A841A6C9` | `4EF01C04153FDF270EC66EA647AD1283E59F126D712A59DC77668B9647180C8C` |
| `SimulationRuntimeAdmissionTests` | 186/186 PASS | [`XML`](results/admission/EditMode-20261011-011730-038d1318a2e146deb32bedbb2ce6c906.xml) | `B278318D7490F4740F49098213DE13549C00C242517730F11C7D961D94BE036D` | [`log.gz`](results/admission/EditMode-20261011-011730-038d1318a2e146deb32bedbb2ce6c906.log.gz) | `6641D0DF235DAD33B9DCAC90A1121526CC0E5DF4D2E5548BCB985790027E3372` | `5C3D6F6A77EEAF8F2FC111FE2FF3D535F68B705D625297A9EDC414C94E13FE7D` |
| ALL EditMode | 2860/2860 PASS | [`XML`](results/all-editmode/EditMode-20261011-011804-60dd46d449cb4d50b111e719468eabcd.xml) | `FBEAEC090A23CDC421E9BBD7E96C9E8FDED1148C29D042EBC7243B297AB7C854` | [`log.gz`](results/all-editmode/EditMode-20261011-011804-60dd46d449cb4d50b111e719468eabcd.log.gz) | `2575D7C437B56943BE8091FBF9125B62DB06313D7EE2309B9AB9FB0D8A3BCFC8` | `38810F5547F9FB59932A839190ED226E633C04D06FA5F9BA737980A42F4B2C74` |
| Official Smoke | 5/5 PASS | [`XML`](results/smoke/EditMode-20261011-011903-d7375a7242d34ed2a5324435472b3ae1.xml) | `550F3E42D0196E222F83BC595CBB9FDEF616E5125A5019718310E68720827D53` | [`log.gz`](results/smoke/EditMode-20261011-011903-d7375a7242d34ed2a5324435472b3ae1.log.gz) | `F6E97F06A7FB926C771F2D23A95B6151C35D6EBC195EABD29A8BFCEC2DA41069` | `0178794E6464BAA1823FE0BA037E84E40BBFE01E7DA2A8BD7F627B30DCC39F9D` |

`git diff --check 54e5fadc44b2be938abe18d98355b8a02c3203e4..eb1865f242f8f0d422d2d5d020b98c78e98330df`: PASS. The validation worktree also contained separate user-owned ProjectSettings edits and untracked `.meta` files; none was staged or included.

This evidence closes only the exercised terminal successful ActorChoice
duplicate case. It does not complete P12-G, establish exhaustive causal
no-replay or owner/epoch coverage, or change P12-A/P13 readiness.
