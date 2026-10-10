# P12-G PoliticalSupport partial-stage false return — validation

**Scope:** Integrated P12-G evidence for one well-formed PoliticalSupport relation whose Claim target is absent from the exact staged PoliticalClaimStore after earlier E owners, including PoliticalClaims, have staged. The detached snapshot preserves the existing section identity, schema, record count, revision, source endpoint, relation identity, disposition, and dates; only `TargetId` changes to a missing Claim. The test proves rejection before PoliticalSupport staging/publication, preservation of the old session/token/health/facts/roots/included owner projection, continuation parity with an uninterrupted control, valid retry, and parity after another boundary. A restored-continuation census accepts the populated PoliticalClaim and PoliticalSupport stores; fresh bootstrap retains exact-empty admission. No other E owner families are populated by this fixture.

**P12 canonical base:** `3fa9966cf3e00ee135f79144635821ad118e8171`

**Implementation commit:** `f64ab8416c9dcbaa11a0dae6b4daa6453451d920`

**Implementation Git tree:** `91fa860bfda96f1c616d580b3bdd5ef8a2bceee7`

**Validated Assets tree:** `021853f5314fb6a0377c76008f4199e97e1eccea`

**Unity Editor:** `6000.3.9f1`

**Runner:** `Tools/UnityValidation/Invoke-UnityValidation.ps1`

| Gate | Result | XML artifact | XML SHA-256 | Compressed log | Compressed SHA-256 | Uncompressed log SHA-256 |
|---|---:|---|---|---|---|---|
| Focused `SimulationRuntimeAdmissionTests` | 179/179 PASS | `P12GPoliticalSupportFalseReturn-Focused.xml` | `F1BFC089D483A47A8DC75E53EEC4780EF66199A5ABB0081B38D299C2B6A8B02C` | `P12GPoliticalSupportFalseReturn-Focused.log.gz` | `B3E1AEBDA7F5E98FEC0DC363FDE76FE2A8CC3D9CC8A1A13C04C93F01C9CDE555` | `D0A27ABF85107D499521A8859922FC506F008E23BE056C6F5F929DD3B4B02A44` |
| ALL EditMode | 2853/2853 PASS | `P12GPoliticalSupportFalseReturn-AllEditMode.xml` | `D75F1344B1659840930D71DF4D6DC79C039E037E6FC1AF088D16DD14BB5A0884` | `P12GPoliticalSupportFalseReturn-AllEditMode.log.gz` | `C1C8B84B54A7F64F9A849CE62A75829003FAEAA91B4F920349B05F4A8430B7DA` | `7F72A33E11B4E017C6E396B81A27803E18A68E829C16DAD1BAFF40448DB41798` |
| Official Smoke (`EditMode -TestFilter Smoke`) | 5/5 PASS | `P12GPoliticalSupportFalseReturn-OfficialSmoke.xml` | `78A1A94DCE1B5ABB71326245A097756E98F543DCB658112341D23F77B2A26252` | `P12GPoliticalSupportFalseReturn-OfficialSmoke.log.gz` | `C8F0062DFEDA737E529173D79F648F60BC145ADE4787F1325141C8E831FBAD8E` | `7D75986AE3E90058CAF186B2B75EAEE7594C91D7ADDDD6222619D9101D5C62F4` |
| `git diff --check` | PASS | `3fa9966..f64ab84` | — | — | — | — |

All retained XML files report zero failed, skipped, or inconclusive tests. The named integrated PoliticalSupport rejection test passes in the focused and full EditMode results. Raw copied artifact hashes and decompressed log hashes were recomputed from the committed files and match this manifest. The validation ran against the implementation commit's exact `Assets` tree; no code changed after the runs. ProjectSettings changes and unrelated untracked `.meta` files were excluded and left unstaged.
