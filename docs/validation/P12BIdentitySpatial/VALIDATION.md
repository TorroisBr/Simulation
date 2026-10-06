# P12-B Runtime Identity / Spatial Census Integration Validation

**Status:** corrected code and required validation complete; independent exact-tip code review pending.

## Candidate identity

- Canonical base: `80d0ec825a9ad8da819cc43f8ac214fb49e27291`.
- Design candidate: `d73c44202affc4adc936187313145f11a166a027`.
- Independent design review PASS: `106a4c6` on `codex/phase12/P12BIdentitySpatialProtocolDesignReview`.
- Architecture baseline: `e16796014d348e3b59da7ed848101c4c03926ba5`.
- Initial implementation code commit: `791dfd2cb980883207f6fb851ccd3a045e393009`.
- Corrected implementation code commit: `6dbe17744c9cd4670de89624b8a0099e215701bc`.
- Corrected implementation code tree: `30f422f76bfa20bc16661f937dca823cda951819`.

The selected Daily-v1 census inventory increases from 242 to 253 sections by
registering the eight existing RuntimeIdentityRegistry sections, two legacy
SpatialNetwork sections, and the existing ExplorableSiteStore section. Four
identity sections and ExplorableSiteStore are `ExplicitlyEmpty`; NPC, City,
Location, Route, legacy Network Location, and legacy Network Route sections
are `Required`. The implementation uses the existing `runtime.npc-membership`
operation to add a new NPC to the exact identity index and notify all eight
shared-revision identity sections in the same mutation epoch. The selected
Daily-v1 roster must already exist as the exact same NPC objects in its
installed `RuntimeIdentityRegistry`; missing bootstrap identity fails
composition without mutating the registry. Only a post-genesis membership
operation may add a new NPC identity. Unregister keeps the append-only identity
mapping; re-registering the same object leaves the registry revision unchanged.
No runtime geography or site insertion operation is added.

## Corrected exact-tip validation results

These runs validate corrected code commit `6dbe17744c9cd4670de89624b8a0099e215701bc`, tree `30f422f76bfa20bc16661f937dca823cda951819`, on Unity `6000.3.9f1`.

| Gate | Result | XML | Committed XML blob SHA-256 | Unity output SHA-256 | Raw log archive member | Log SHA-256 |
| --- | ---: | --- | --- | --- | --- | --- |
| `SimulationBootstrapCompositionTests` | 24/24 | `CompositionCorrectionFocused-Retry1.xml` | `0E5EC142E34C76EF3A4E65EC07C65DD9A8DBC2C89FFFD308D062EDAE217E039D` | `D4DA3D7CFA7267955499E588E769907E599EA285BA05A28FCBCC93A28DAD5A14` | `raw-logs-corrected.zip: CompositionCorrectionFocused-Retry1.log` | `7F6339DF9819625B7883476DCCA4F44C38091D757C3938FEFBDE4B7D50E35541` |
| `PropertyEstateMutationEpochTests` | 5/5 | `PropertyEstateCorrectionFocused.xml` | `52BC7640B797E95FCE1DDE06442FBFBBEA910412B6D873AA3E1F163419317808` | `5AF1E1BBDFCBE72F2C47199777ADDED3CAE6D74F94128EA78324C335D1E2C1D6` | `raw-logs-corrected.zip: PropertyEstateCorrectionFocused.log` | `1387C4618EDCFF4D37699602B86987C9644E50ADE84ABAF297A1713FBA9349FF` |
| ALL EditMode | 2417/2417 | `AllEditMode-Corrected.xml` | `073CFA6D5F377F0924AAF38A29D823D1D75E2308C60ED6CC35CA7741FA11527F` | `20C99FD365473A68E018C3C8FC58DE4A4F41C9F67CC6B4E2A92D12E914B23CE5` | `raw-logs-corrected.zip: AllEditMode-Corrected.log` | `4CE5642490B6B11EDFEAEEB5C34705A49C08844486EE371CBBE41C967F43454F` |
| Official Smoke (`-testFilter Smoke`) | 5/5 | `OfficialSmoke-Corrected.xml` | `B8A1DD7BA178C4B66F5998335FBBF900F93B74ED09B648AE21BF593B2E23109D` | `EFF6C0410F6152A228900BB4EA220A4A5243ACA98C7E40C8C404543C9467CFC7` | `raw-logs-corrected.zip: OfficialSmoke-Corrected.log` | `D1CA89853CA3D216312927478602C98FC4EF51CB58BF3FD03F14E74DC1A7E17C` |

`git diff --check` passed for the corrected implementation commit and its full
candidate diff against canonical base `80d0ec825a9ad8da819cc43f8ac214fb49e27291`.
The archive `raw-logs-corrected.zip` contains the four successful logs above
and the first corrected-test compilation diagnostic for audit; archive
SHA-256: `5FC15793969F71FA273FE418B23A6C2AE644514EFB188C41D72B339DEBCAF3A5`.
The first focused compile attempt found a test-only interface access error;
`CompositionCorrectionFocused.log` records it. The corrected retry compiled and
passed all 24 composition tests. Git normalizes text line endings when storing
the XML files; both the Unity-emitted file SHA-256 and the committed Git-blob
SHA-256 are recorded so the test-runner output and promoted evidence can each
be verified exactly.

## Initial pre-correction validation results (retained for audit)

| Gate | Result | XML | XML SHA-256 | Raw log archive member | Log SHA-256 |
| --- | ---: | --- | --- | --- | --- |
| `PropertyEstateMutationEpochTests` | 5/5 | `PropertyEstateFocused.xml` | `460F3FD62C180A5B0659FA1D27B65D009E2FAC3C816192C51CAF835005A22265` | `raw-logs.zip: PropertyEstateFocused.log` | `361988686BBABD9F7B84AE515431180F716B52B45B1334E3021532D2954E0807` |
| `SimulationBootstrapCompositionTests` | 23/23 | `CompositionFocused.xml` | `1D2A0C44B0EED977AE68ECB27CCE77DA7F44C071A21B55FB17F577AC2FE75DB9` | `raw-logs.zip: CompositionFocused.log` | `22F56D4008CC23EE7FF3562145FDC6D0840AB8D9E5F1C81AAB9D811695C88B73` |
| ALL EditMode | 2416/2416 | `AllEditMode.xml` | `056D42A53CFB36CAA6B901DE28D5E0E1FB5FA42E169FC528AA0D66E0C7D47B06` | `raw-logs.zip: AllEditMode.log` | `62F17E8B5CC900EBF16DB391D5D8D5E5CC431BAF9EB89C3F68AB8C442E6D92A3` |
| Official Smoke (`-testFilter Smoke`) | 5/5 | `OfficialSmoke.xml` | `CCE9432EC425A203D42CDB53F2A1A15568C1C5FC5A49945050DA72BC15EB1301` | `raw-logs.zip: OfficialSmoke.log` | `A1077E50C2EBF327226A629D18F8DAB78BF8F3B0874505225590858AB9FCAF1C` |

`git diff --check` passed on the implementation candidate. The archive
`raw-logs.zip` contains exactly the four successful logs above; SHA-256:
`5F68F7A6DB4DC171D26EA9FF2E0BE9A00F97FBC35A39CDF1722C50BBDD68056D`.

The first full EditMode run exposed one stale assertion expecting 242 sections
and completed 2415/2416 tests. The assertion now expects 253, the focused
Property/Estate suite passes 5/5, and the complete rerun passes 2416/2416. The
first run's XML and log remain preserved at
`docs/validation/P12BPropertyEstateEpoch/all-editmode/IdentitySpatial-All.xml`
and `IdentitySpatial-All.log`.

## Committed source blob SHA-256

These hashes are over exact Git blob bytes at code commit
`6dbe17744c9cd4670de89624b8a0099e215701bc`.

| File | SHA-256 |
| --- | --- |
| `Assets/_Project/Scripts/RuntimeIdentity.cs` | `89B19AF073510DB56024090736086A842272153A462AE31AD6C7EF81572EE1BF` |
| `Assets/_Project/Scripts/SimulationBootstrapComposition.cs` | `02E5651E06A7AF08FF42FC18793D47A2F11EC1C4D5C6E49F5E1C27E82DFC87DB` |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `59E9A583A8FB4E3BA46967F06857726CD9B181956A0E10FA1F5D0F7E1B8D050D` |
| `Assets/_Project/Scripts/SpatialRuntime.cs` | `04EF382399A046A99126EB18C15D335796660B5F52C0045FC6CB48443A7A287E` |
| `Assets/_Project/Scripts/TesteSimulacao.cs` | `EE03A52BA3F25C8A6C62919EC872F58FC0E86D62A762BF19220C331A945D44E2` |
| `Assets/_Project/Scripts/P12RuntimeIdentitySpatialCensus.cs` | `C7325C2B63A826DE25F4E3E13620C5AD29442E5A44F7643EF612285937749124` |
| `Assets/_Project/Scripts/P12RuntimeIdentitySpatialCensus.cs.meta` | `FCEA3DC29500FEFC50E0EE0E8D2DDDF80786C6253E466C6C9602307A8FBF14DF` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs` | `69EB164A303AAC2BB6FE7201606DEDE252B60EC5A9925C194AF8739061075F6D` |
| `Assets/_Project/Tests/EditMode/Editor/PropertyEstateMutationEpochTests.cs` | `00C991DD9018E787227A0E51D6EF229C73DBD9426B81C771B5C57A426C68EFDC` |

## Scope and evidence limits

This is a bounded owner inventory and NPC membership invalidation slice. It
does not establish complete owner coverage, complete shared-epoch coverage,
global quiescence, capture eligibility, export, hydration, P12-A readiness,
P12-B completion, P13 readiness, or Phase 12 closure. P12-B remains
`INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked. Daily-v1
continues to exclude P10-A Ruin/LocalTopology.
