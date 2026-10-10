# P12-G PoliticalClaim Property-target rejection — validation

**Scope:** One populated P12-E `PropertyEntitlement` PoliticalClaim is captured from the selected Daily-v1 runtime. At the null-by-default restore-only detached snapshot seam, the test changes only the claim's Property target to a distinct well-formed `PropertyId` absent from the exact staged PropertyStore. Both schema-v1 sections, their row cardinalities and shared local revision, and all other claim values remain unchanged. The integrated restore rejects after the Institution/Office, Property/Estate, and Faction owners stage, before Claim staging/publication. The source session/token/health/facts/roots/owner graph remain intact; source continuation matches an uninterrupted control; the unchanged snapshot retries successfully; and the restored runtime remains in parity after another boundary.

**Canonical base:** `229e2635e390564c7b732e65e4aa639b531d74db`

**Implementation commit:** `252facf665077a5a2ab583c60a08809f308836ba`

**Implementation Git tree:** `1a87fc09e015babbb0e1bd3e4315c5bb07c4d8ce`

**Validated Assets tree:** `7f8925768283f34da8ee42f78d3828c33ab132d8`

**Unity Editor:** `6000.3.9f1`

**Runner:** `Tools/UnityValidation/Invoke-UnityValidation.ps1`

| Gate | Result | XML | XML SHA-256 | Compressed log | Compressed SHA-256 | Uncompressed log SHA-256 |
|---|---:|---|---|---|---|---|
| Focused case | 1/1 PASS; failed=0, skipped=0, inconclusive=0 | `raw/EditMode-20261010-231643-fc224b58f7174ecba541b042b8265694.xml` | `4BC7BB4A33B31A29B41808E2169AAD0CF8205BCACE863A8B07ECD8D333AC2E5B` | `raw/EditMode-20261010-231643-fc224b58f7174ecba541b042b8265694.log.gz` | `B598355467997B6CD5E0389B180F322709F048E0A43EDC3270BF3ABF1A6783E2` | `2BFAFDCEB8A5C1B46727A25CB59EA2636D3423FD9BCFB3CDFD0D8F3D541D97E6` |
| `SimulationRuntimeAdmissionTests` | 182/182 PASS; failed=0, skipped=0, inconclusive=0 | `raw/EditMode-20261010-231702-238156debbe34b818e7908fd99235a5c.xml` | `531A7CF20427718D7966728246084C516E43D07E346BD43669E111A88F9328B6` | `raw/EditMode-20261010-231702-238156debbe34b818e7908fd99235a5c.log.gz` | `D076AD9D1A570D0323D67134F023C7F27AEC41766CDC59FFF38F7D84604A2FB7` | `3AF60496E8EFF2E21F70328B65EA66A6CF2C0DA433ACF479F2A11F6AA0A2D643` |
| ALL EditMode | 2856/2856 PASS; failed=0, skipped=0, inconclusive=0 | `raw/EditMode-20261010-231738-de80bf3d6ba64d82af4fdad3721241bb.xml` | `D72092AA3F08ED5A62228F8E3E318D4988B4A93A9AAB840BF775C9B6E540378F` | `raw/EditMode-20261010-231738-de80bf3d6ba64d82af4fdad3721241bb.log.gz` | `FC8E1E3A339D063908ABBB28A4433C0B880C9FE509D474CB5B489BAFF750A3C0` | `1EDE0CA81898F429E8BE4CFABC8CE1E7105FA1CAD08E34011FE6EE0CF1598F47` |
| Official Smoke | 5/5 PASS; failed=0, skipped=0, inconclusive=0 | `raw/EditMode-20261010-231836-606d41f08fff41f390d6da3db0273f18.xml` | `B4B5F88D3BBAB760F88A9F2084497264ACFD548F156606742736ED5D45DBFACB` | `raw/EditMode-20261010-231836-606d41f08fff41f390d6da3db0273f18.log.gz` | `92F7ED25F993C0B0B78579A7FB28B1C2308424F4161955DE390C6A2A30E6C914` | `E1B48FD65B0C77233C40F8F5C6C75C591F2E386BFADB48B1C62F3051685B97EF` |

All retained XML reports parse as `Passed` with zero failures, skips, and inconclusive tests. The coordinating checkout computed each XML, compressed-log, and raw-log SHA-256 recorded above. `git diff --check` passed. This is one integrated P12-G typed-reference rejection/atomicity case only; it changes no production source and does not establish complete graph coverage or phase readiness. No `ProjectSettings` or unrelated `.meta` files are included.
