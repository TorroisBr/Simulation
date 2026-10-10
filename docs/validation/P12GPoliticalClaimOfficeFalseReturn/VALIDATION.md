# P12-G PoliticalClaim Office-target rejection — validation

**Scope:** One populated P12-E `OfficeEntitlement` PoliticalClaim snapshot is changed only to a well-formed Office ID absent from the exact privately staged Office root. The test verifies capture ordering, earlier E-owner staging, rejection before Claim staging/publication, source session/token/health/facts/roots/owner-graph preservation, continued parity with an uninterrupted control, successful valid retry, and parity after another boundary. The restored-continuation census now validates observed Institution/Office cardinality and revision while ordinary fresh bootstrap remains exact-empty.

**Canonical base:** `fb4f89a05f6d00114176e05df26968325605dc07`

**Implementation commit:** `5813e3fbe1824e23c98ea69c0d598d9a66c0f664`

**Implementation Git tree:** `c03566fa4c7547d98b40a3068ed2d9a432067912`

**Validated Assets tree:** `90bf5c9415e4feb7d96d23eaa47071e6fcfd2292`

**Unity Editor:** `6000.3.9f1`

**Runner:** `Tools/UnityValidation/Invoke-UnityValidation.ps1`

| Gate | Result | XML | XML SHA-256 | Compressed log | Compressed SHA-256 | Uncompressed log SHA-256 |
|---|---:|---|---|---|---|---|
| Focused case | 1/1 PASS; failed=0, skipped=0, inconclusive=0 | `P12GPoliticalClaimOffice-FocusedCase.xml` | `373AB06EEE14A6744E60CCAB93576C7821341DD84B04C7BE40D5143CCCA9F9BC` | `P12GPoliticalClaimOffice-FocusedCase.log.gz` | `9FDCFB6DF3F8B4CE0F283AE4D282372DBE8F26AB4C4F3B8E868B6160EAD3BC6A` | `B29C91FBFDBD80D2D134B3DD5F771B151DF0F262A78CE831376F08F4B85FA6E2` |
| SimulationRuntimeAdmissionTests | 180/180 PASS; failed=0, skipped=0, inconclusive=0 | `P12GPoliticalClaimOffice-Admission.xml` | `AB0458BC2649FC0CF2BF8BE888E14BD778E8B3EEB4E48DC590C9F719EA6AD55E` | `P12GPoliticalClaimOffice-Admission.log.gz` | `B651A9A620BC9BD7E5093E044EA4A7FEFD705592304B04FF78B32CB15E2AB53D` | `70516649DD44CAC1F36286F4A8C4935990A7A4B367569EC722583A4FF22B6007` |
| InstitutionOfficeCensusTests | 4/4 PASS; failed=0, skipped=0, inconclusive=0 | `P12GPoliticalClaimOffice-InstitutionOffice.xml` | `7DA7846748CD696739AC666944972B5FE5628D4533CB687EF816D9544612145C` | `P12GPoliticalClaimOffice-InstitutionOffice.log.gz` | `617D00C33D863CE697D7B1788618584D2949C3ADACF05A4989B95CCB8784C99E` | `54C5D7C3469E7AB6E0EA9167A6B046E73821F0E07A1205606234828630A2FFF0` |
| ALL EditMode | 2854/2854 PASS; failed=0, skipped=0, inconclusive=0 | `P12GPoliticalClaimOffice-AllEditMode.xml` | `06E6CC6C18910FAC83032B21C3C05654D788AD15DA3B9426F9A4EF1EE63ABBB7` | `P12GPoliticalClaimOffice-AllEditMode.log.gz` | `B83203B201EFAEC8AEEAF9A42004529848445DF2433E4A21774D03C459D8AF89` | `8BEB794D27717193651F2393E97E3B3A63E3B0D9D55654CE390F88B71C3B40C5` |
| Official Smoke | 5/5 PASS; failed=0, skipped=0, inconclusive=0 | `P12GPoliticalClaimOffice-OfficialSmoke.xml` | `EEA5C001812B8077812F9F615C7CDA653066802528456DFAF66E02FA1C8BBAC4` | `P12GPoliticalClaimOffice-OfficialSmoke.log.gz` | `BA68754F56BE65B476624BCF0512BBC7966E9E8E935BCBB1935BF2BEDDF5F732` | `A55C1F6849BEB31188911ADD517144F92758087D36C1AD499D10F99DA4E6DD7B` |

All XML files are retained with compressed logs. Focused and full-suite validation completed on the implementation commit exact Assets tree; no code changed after those runs.
