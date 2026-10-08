# P12-D SpatialNetwork Owner Snapshot — Validation

**Base:** `codex/phase12/canonical` at `ad4b20c42a25c9fd453c98695a79ce59490bf4fe`

**Code commit:** `39ad61d60907154c4af52ea1bf01bfe94322d90f`

**Validated `Assets` tree:** `1e00c94447bf202acf7517b14a5aed6043ae4eec`

**Design review:** current P12-D technical design exact-tip review PASS at `b9a0fd1b5941f0b7615a72acec39fd22e6c9ee0e`

**Unity:** `6000.3.9f1`

## Scope

This bounded owner slice adds a detached schema-v1 export and unpublished exact-value factory for the legacy `SpatialNetworkRuntime` locations/routes. It preserves exact IDs, owner iteration/route order, endpoint references, normalized travel days, parallel route identities, and the owner-local revision. The factory validates all rows before building the staged owner, registers exact identities in the supplied unpublished `RuntimeIdentityRegistry`, rebuilds only the outgoing-route index, and returns no staged network on rejection.

This is not P12-D graph composition or whole-profile persistence. The surrounding capture layer must still bind the owner snapshot to the P12-B completed-boundary token/revision vector. City/NPC/site references, P8 geography, P10 LocalTopology, runtime/bootstrap publication, P12 envelope, P12-A readiness, P13 readiness, and Phase closure are outside this slice.

## Validation results

| Run | Result | XML |
|---|---:|---|
| `SpatialNetworkCensusTests` | 11/11 passed | [`Raw/Focused/SpatialNetworkCensusTests.xml`](Raw/Focused/SpatialNetworkCensusTests.xml) |
| ALL EditMode | 2544/2544 passed | [`Raw/Full/AllEditMode.xml`](Raw/Full/AllEditMode.xml) |
| Official EditMode Smoke (`-TestFilter Smoke`) | 5/5 passed | [`Raw/Smoke/OfficialSmoke.xml`](Raw/Smoke/OfficialSmoke.xml) |

Commands, run against the code/test tree at commit `39ad61d60907154c4af52ea1bf01bfe94322d90f`:

```powershell
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter SpatialNetworkCensusTests
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -All
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter Smoke
git diff --check ad4b20c42a25c9fd453c98695a79ce59490bf4fe HEAD
```

`git diff --check` passed. The XML result roots report `Passed`, zero failed/inconclusive/skipped, and the counts above. Each archived `.log.gz` is losslessly compressed; its decompressed SHA-256 was checked against the original runner log.

## Exact artifact hashes

Hashes are SHA-256. Compressed log hashes identify the committed `.gz`; `raw log` hashes identify the original uncompressed runner output.

| Artifact | SHA-256 |
|---|---|
| `Raw/Focused/SpatialNetworkCensusTests.xml` | `598884BDC2FBF3069D6B9B9489A0F5D1989FE8A1F1FE1ED96BD1FCAB34064C57` |
| `Raw/Focused/SpatialNetworkCensusTests.log.gz` | `560190C7EFC15C03057EBA665ADD0E6EFA804DCD6350312AEDFF6F4F28E0422D` |
| Focused raw log | `C48FE2CC3E8B3E205DF3DCF059B19107AD2FE8D97A910F4F71CCB7ECB0895722` |
| `Raw/Full/AllEditMode.xml` | `080B79CA34BECD9E4A411B777D095B48575B2D15109A991BC92B4D3523983534` |
| `Raw/Full/AllEditMode.log.gz` | `B296551AC84D6DB13FFC8B26580C962377A33776769090C91F912FE86BD40249` |
| Full EditMode raw log | `56A743F2321E56124A7D719D1AC298B00D97F6C3C60E2FBE589CAEB266E96501` |
| `Raw/Smoke/OfficialSmoke.xml` | `38D9770592272B52E8990555D9F530C4F23643A39605B6D57825AA5DE021794E` |
| `Raw/Smoke/OfficialSmoke.log.gz` | `985D1AC971AC84A523594C309F8202C1D56E469F48A1C9D9F697CC1E37E7236B` |
| Smoke raw log | `F471B877C6A55ABA8C38A57500DCA57AAEB5D59C4991E6E017BA26E2E53EA3C9` |

## Source hashes

These identify the committed Git blob bytes at code commit `39ad61d` (independent
of checkout line-ending conversion).

| Source | SHA-256 |
|---|---|
| `Assets/_Project/Scripts/SpatialRuntime.cs` | `9459F51C909F66763C7A0AF8D6AB56C5371F3250DAA89137015E3D081CDCE575` |
| `Assets/_Project/Tests/EditMode/Editor/SpatialNetworkCensusTests.cs` | `FB71399DE08BADE2437091FD4AF6744350B0372B140562C71A8040491420917F` |
