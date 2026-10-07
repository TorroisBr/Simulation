# P12-B Daily-v1 spatial profile cardinality admission — validation

**Result:** PASS
**Canonical base:** `codex/phase12/canonical` at `d8e6c9919d9359003dfd370fbd38a47424256b26`
**Implementation candidate:** `c2a21d8eb544291d3a46ef0d65d4f4c226feefbe`
**Candidate tree:** `24f2c317ec28a8123b86d558ae0889505ba1aff6`
**Unity:** 6000.3.9f1
**Design:** [bounded P12-B design](../../design/PHASE12_P12B_SPATIAL_PROFILE_CARDINALITY_ADMISSION_DESIGN.md); independent design review PASS at `ec73a0f95ca3bec0d50bd61dbe15a930d8a5635d`.

This is the bounded Daily-v1 census registration and initial cardinality admission slice. It adds the seven existing P8-A/B/C providers and enforces the reviewed Required-owner counts while retaining the P8-B/C exact-zero roles. The selected Daily-v1 inventory is 275 sections. `SampleScene` continues to select the dedicated P9-B-only `Simulation-DailyV1.asset`; `Simulation-GeneralTest.asset` remains the separate P10-A Ruin/LocalTopology proving profile.

## Validation results

| Suite | Result | NUnit XML | Original log SHA-256 | Compressed log and SHA-256 |
|---|---:|---|---|---|
| Bootstrap composition | 24/24 | [`FocusedComposition XML`](Raw/FocusedComposition/EditMode-20261007-031918-1ee92f3c095e49a3bc94d45f9ad712eb.xml) `2FCDF9D53728E28DB98A79E6E7B1CA160E82B22443843A4238F82171018AAE60` | `1CAC7A87B8D6BDA4F8AF9F8B673121F5DB39A749C40E8904DCBC238261A6FE24` | [log.gz](Raw/FocusedComposition/EditMode-20261007-031918-1ee92f3c095e49a3bc94d45f9ad712eb.log.gz) `248D6F5D06B9C6B62D272C9E74F2DF63C16984BC148828F72191EE5961BFBC43` |
| Runtime admission | 58/58 | [`FocusedAdmission XML`](Raw/FocusedAdmission/EditMode-20261007-031931-469e34758352450884d2a4ef55a181d6.xml) `5E9BB8A47E498A96FCF99352CAFA68BBEDA8E07B203C64F8DA985A4FAB13105A` | `E1CE1497705E5FE9918396450E9020C166CBE514474C7FF9027CD82B5CA84681` | [log.gz](Raw/FocusedAdmission/EditMode-20261007-031931-469e34758352450884d2a4ef55a181d6.log.gz) `8113E3A810A2DC53620B9B4AF1D5B7252BF6408099FE36C353BBE5CC4BD577A1` |
| Property/Estate mutation epoch regression | 5/5 | [`FocusedPropertyEstate XML`](Raw/FocusedPropertyEstate/EditMode-20261007-031945-c04ada7050ef497dbb381d3bbe048be0.xml) `8E8F9BF2D0DDBAD36A992BF9E0629970D024EF8D1E561C73394B5A74407D7F56` | `645A08664F2A3AE506ACED9063FEA2CBC49E673D6D62E35D1DD51982CE2AD562` | [log.gz](Raw/FocusedPropertyEstate/EditMode-20261007-031945-c04ada7050ef497dbb381d3bbe048be0.log.gz) `4FCBE2284D6AB0F8D6B4EF604714E2D15036037453877F59F3045F0B9511EF26` |
| World Exchange producer regression | 7/7 | [`FocusedWorldExchange XML`](Raw/FocusedWorldExchange/EditMode-20261007-031959-ae2c1e4b26484f168f28c6aa4ee0bbfa.xml) `D5599D841CA6F60B8687585A76A2C7918CE1AA72C8D12A9A45ABDA6B991CB298` | `10562E9E70FFC89D409CF47DE388BCC1D3AA87FB133C1D624B16F7E1B04A4EC2` | [log.gz](Raw/FocusedWorldExchange/EditMode-20261007-031959-ae2c1e4b26484f168f28c6aa4ee0bbfa.log.gz) `7E921309B01E18A61000EF232AEFA6A921D63FDDD9266F9208C0FE5BCE2CEC8B` |
| ALL EditMode | 2442/2442 | [`All EditMode XML`](Raw/AllEditMode/EditMode-20261007-032012-3fdb4e8c1eb74615978d5b72be60545c.xml) `A925B675105F109A7E50910601FE741C928D88E8E2A5CCBB14DEF5F52DFC7BE6` | `E304E9937490064ACA2DEC38E8D76405C915BB8FDEE019E8E63E06DF9C727A7E` | [log.gz](Raw/AllEditMode/EditMode-20261007-032012-3fdb4e8c1eb74615978d5b72be60545c.log.gz) `1B9CE2B16CA5BD72D55F073561E8DF7051B43E65C210276DEF03FA38A3FF5597` |
| Official Smoke | 5/5 | [`Smoke XML`](Raw/Smoke/EditMode-20261007-032047-5495e0c0b74844c7b019076df5b049fc.xml) `0A4739441F73741939F84A121E47A853963DBAA48F84039A064E7BC79230A944` | `46097C7F946CDECD7C8D66B44478674C3C4FA1A93A306EAD71EC25F2709769DB` | [log.gz](Raw/Smoke/EditMode-20261007-032047-5495e0c0b74844c7b019076df5b049fc.log.gz) `DC3283873F7981A5CB4D689185A0FBEBA6D11E9ED29DC3C97D55F6215A1A254F` |

All compressed logs were decompressed and their bytes matched the recorded original-log SHA-256. `git diff --check origin/codex/phase12/canonical c2a21d8eb544291d3a46ef0d65d4f4c226feefbe` passed. The four focused suites total 94/94.

## Exact-tree and scope notes

The validation checkout was based on `48e6863f0a02a18999c0476b15b543390b323996`; the only source differences from implementation candidate `c2a21d8` were the two downstream test files listed below. Their blob hashes in the validation checkout matched the exact candidate commit:

- `Assets/_Project/Tests/EditMode/Editor/PropertyEstateMutationEpochTests.cs`
- `Assets/_Project/Tests/EditMode/Editor/WorldExchange/WorldExchangeV2ProducerTests.cs`

The candidate changes no P8-B/C writer, operation ID, mutation callback, or shared-epoch behavior. P8-B/C remain exact-zero admission sections in Daily-v1. P10-A remains a separate proving profile. The ten-NPC bootstrap count is initial admission only; the retained temporal behavior remains 10 → 11 → 11.

This validation does not claim complete P12-B owner/cardinality coverage, complete shared-epoch coverage, global quiescence, capture eligibility, P12-B completion, P12-A readiness, P13 readiness, export, hydration, or Phase 12 closure. P12-B remains INCOMPLETE, P12-A remains WAIT_DEPENDENCY, and P13 remains BLOCKED.
