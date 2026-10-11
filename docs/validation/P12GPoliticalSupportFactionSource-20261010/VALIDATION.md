# P12-G PoliticalSupport Faction-source rejection — validation

**Scope:** One selected Daily-v1 source runtime contains one Faction, a valid `StatusRecognition` PoliticalClaim, and a Faction-sourced PoliticalSupport relation targeting that Claim. The detached restore-only override preserves section schema, record count, local revision, relation identity, source kind, Claim target, disposition and temporal fields while changing only the `FactionId` to a distinct well-formed ID absent from the exact staged Faction owner. After earlier E owners and PoliticalClaims stage, normal PoliticalSupport owner staging rejects before publication. The test verifies source session/token/health/facts/roots/owner graph preservation, source/control continuation parity, retry of the unmodified snapshot, exact populated Faction retention, and parity after another boundary.

**Contract-supported implementation correction:** The required Faction and affiliation sections retain exact owner identity, schema, revision and actual staged cardinality in restored Daily-v1 continuations. Exact-zero remains required for a fresh bootstrap. This corrects the restored-census path so P12-E populated Faction facts round-trip as its technical design requires; it does not add profile scope.

**Canonical base:** `870fdeddc5fbeea2625c61a81d9142b41f3ac1ea`

**Implementation commit:** `9d1100f0c07c5faf662b6241579cabb0f38f7cf9`

**Implementation Git tree:** `99a15ab6693bcc2238eb7e1a2200bd1f9038c14d`

**Validated Assets tree:** `ca0df3c40caf184f0fcbcaae4866aae82094c74b`

**Unity Editor:** `6000.3.9f1`

**Runner:** `Tools/UnityValidation/Invoke-UnityValidation.ps1`

| Gate | Result | XML | XML SHA-256 | Compressed log | Compressed SHA-256 | Uncompressed log SHA-256 |
|---|---:|---|---|---|---|---|
| Focused new case | 1/1 PASS; failed=0, skipped=0, inconclusive=0 | `raw/focused.xml` | `49184DEACE3C062B291E3BF808FB86569587C55699F862952E92CD1889DBC0F0` | `raw/focused.log.gz` | `AF21458F7C6EEDA87FDB5812937D06DC41F0B1BF2B740F2861E5F5E40AD8119E` | `C97F77A1D7B9BC4DC6325762486AC95CF80EAE2CC5FC934A2D20CB782DE4A24B` |
| SimulationRuntimeAdmissionTests | 183/183 PASS; failed=0, skipped=0, inconclusive=0 | `raw/admission.xml` | `44BE40FEE416E5B0ED8009BEC9D883AE6EF6D99A6B93EE75015EE0AEC7F01B6A` | `raw/admission.log.gz` | `35666D24B2A552770F5D4FDB88D8C7498BC66FF8B2BD2E6E68B967600C3C65F9` | `0E5A9AAECE45C78B631A93B44A9876880529F4DF67FFB0EEC35DCB72A22B5B0E` |
| faction-census | 5/5 PASS; failed=0, skipped=0, inconclusive=0 | `raw/faction-census.xml` | `E2754F4D81F3F764425894F1771726C2DF5A79831BADF71A744D56EBA147D132` | `raw/faction-census.log.gz` | `81C5953B427E8AA73D4CEB32A6047E753582D4D8148A997995E89F51BC5667CF` | `E4347A5E9C066AAD8A9E2C68D43DBF007F189489EE9EF710A3379DA77D149943` |
| faction-snapshot | 12/12 PASS; failed=0, skipped=0, inconclusive=0 | `raw/faction-snapshot.xml` | `80909194573382C40CFA8524879AE73DAAF7BB2EFD893ECBC97765E9F19B957C` | `raw/faction-snapshot.log.gz` | `C8195ADC47E1B448CCBC07E38F8099DB61F72DBFEA2202C6D6489B8D7C36C57B` | `4311B8C7BD43B1ED2A87F4E0FCF88272E2D15126E1465361CF419B09EC0B0AD5` |
| support-snapshot | 6/6 PASS; failed=0, skipped=0, inconclusive=0 | `raw/support-snapshot.xml` | `A398401E887A9180C63EE0895C1FE24A62AF30980692058384A48DE98F64A70E` | `raw/support-snapshot.log.gz` | `CAC785E4B33122D66863F924B3E2CD576E8E5D6A98A55CF14524F624A1FEB4FC` | `3314FA06B162D2E7A2B4C899C249779B83B3D6311747DC58443E2D75AF2B21F9` |
| ALL EditMode | 2857/2857 PASS; failed=0, skipped=0, inconclusive=0 | `raw/all-editmode.xml` | `B8ECAB5D32502B90312B10BFF996F43608FB76504A4A62476257B3F728E7AA5F` | `raw/all-editmode.log.gz` | `50BEACA9AC36A6C0CA10FEB733FB0EFCA45616CD9C22B858E8076C758A4285E2` | `20CCD25D888091604F5A1704F652FAF569DBA1E1EC765FBCBEB6094F0A955B23` |
| Official Smoke | 5/5 PASS; failed=0, skipped=0, inconclusive=0 | `raw/smoke.xml` | `8B78B95E121611DBDC046E4345832E70DE2BCE3F1A7D37E920CF9738464CD08F` | `raw/smoke.log.gz` | `998DFD7DB100A531DA6D043A1653DF477DF3F5AD1B7B533F1C0D8F24EED32BF6` | `521556C9895BDA1E9F23BD16F7E8681C228DBEF823D7E999795D0121EC9501B0` |

All XML reports parse as `Passed` with zero failures, skips, and inconclusive tests. XML, compressed-log, and raw-log SHA-256 values were computed from the local artifacts and retained above. `git diff --check` passed on the implementation diff. P12-G remains `WAIT_DEPENDENCY`; P12-A remains `WAIT_DEPENDENCY`; P12-B remains `INCOMPLETE`; P13 remains `BLOCKED`; Phase 12 remains `OPEN`. This checkpoint adds only the named Faction-source rejection/retry case and its required restored-census correction. It does not establish complete owner/epoch coverage, global quiescence, capture eligibility, export/hydration readiness, P12-A/P13 readiness, or Phase closure. No ProjectSettings or unrelated `.meta` path is included.
