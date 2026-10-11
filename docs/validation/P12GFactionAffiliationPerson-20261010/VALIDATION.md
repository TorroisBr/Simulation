# P12-G Faction affiliation missing-Person rejection - validation

**Scope:** One selected Daily-v1 source runtime contains one registered Person, one Faction, and one active affiliation. After ordinary source capture, a null-by-default internal restore-test override changes only the detached affiliation `PersonId` to a distinct, well-formed ID absent from the exact staged PersonStore. The fixture preserves both section IDs, schemas, record counts, shared revision, Faction row, affiliation ID/Faction ID/tenure/end state, and every other captured value. Earlier E owners stage, then the existing Faction snapshot validator rejects before `EFactionOwnerStaged` or publication. The integrated test checks source session/token/health/facts/roots/owner graph preservation, source/control continuation parity, retry of the unmodified snapshot, exact restored Faction/affiliation identity, and parity after another boundary.

**Test-only seam:** Added one optional internal Faction owner-snapshot override to the existing restore-only test path (`TesteSimulacao` -> `P12GDailyV1RestoreCoordinator` -> `P12EDailyV1OwnerPackage`). Production calls pass the default null; validation and domain behavior are unchanged.

**Canonical base:** `794ee2b11c412c67a9d86c6d560bccf3ed101237`

**Implementation commit:** `352446f9e350d5dd103ade4010921072b429edbb`

**Implementation Git tree:** `a9b083554601e0958c02e5ba32d4fa5a988dc217`

**Validated Assets tree:** `787f81eafdb25bbd0c33afd194cac793ce825b2b`

**Runner:** `Tools/UnityValidation/Invoke-UnityValidation.ps1`

| Gate | Result | XML | XML SHA-256 | Compressed log | Compressed SHA-256 | Raw log SHA-256 |
|---|---:|---|---|---|---|---|
| Focused case | 1/1 PASS; failed=0, skipped=0, inconclusive=0 | `raw/EditMode-20261011-001345-6c83cb9ffee84ae9bf58de594c5d9156.xml` | `736E37C78E47310F1153B8E70C5BB1A3C110AB46505470858F5FAA3568F3700F` | `raw/EditMode-20261011-001345-6c83cb9ffee84ae9bf58de594c5d9156.log.gz` | `CA0CA97A2B668744CDC89172D4E1A562FB2875A2130CDA60E1C601D53F7465F4` | `F53EA3A14092A9105A1422AEF8E5FC39FB44F899398CE4A6DD4FB191274531D6` |
| SimulationRuntimeAdmissionTests | 184/184 PASS; failed=0, skipped=0, inconclusive=0 | `raw/EditMode-20261011-001412-c10a56c64002475ab7f66d1bab467e36.xml` | `83D418F225E9FE38572E6ED63575AF7EE42A2EA62A3075D123002D9D83077167` | `raw/EditMode-20261011-001412-c10a56c64002475ab7f66d1bab467e36.log.gz` | `2594E132D6BDF565A602D1C880331A178CD23B7620467764D4149A6AAE6A938C` | `E29DCD678FE59288DE8DBDA11CD6946D04FEFF27C0217B943DFC576725B3E103` |
| P12EFactionOwnerSnapshotTests | 12/12 PASS; failed=0, skipped=0, inconclusive=0 | `raw/EditMode-20261011-001446-e1f49c850a81433783cd8111035794d6.xml` | `F500E7D9337A5451B28BDD91E378E5D98C876E378329D2FAB17820D262638AE6` | `raw/EditMode-20261011-001446-e1f49c850a81433783cd8111035794d6.log.gz` | `5ADF8C749501BE562333778A395CB13C2AAE7A60FB7CAE34E6DDE912A8D15452` | `0352E78C537941939B9ADE12C1F1DC371983006B4FCA92CA853BB13ABD7A0C1D` |
| ALL EditMode | 2858/2858 PASS; failed=0, skipped=0, inconclusive=0 | `raw/EditMode-20261011-001501-ef32f7c0d9c24ed0a1d9f44fbcf40eac.xml` | `40C4BB52C4E48FA6B3620E188934FE545808AD7212A70574F5AA4F79EB71B1A8` | `raw/EditMode-20261011-001501-ef32f7c0d9c24ed0a1d9f44fbcf40eac.log.gz` | `D921737CDD3CEC6A6561C4EEF9AF0795F46570A1B4265D1A8E048B9D186EE5DC` | `5B146E8A19036DC8D4D2B5CE6AFACF7EE4D3512BA8EB3C0F66EE892F0438314B` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 PASS; failed=0, skipped=0, inconclusive=0 | `raw/EditMode-20261011-001606-4c38f845327a4147af2989a4663df395.xml` | `DB0CCD12EE28437F0ADB69703DCC5DD9D4612FE90255919A42D7C10DFBBF0845` | `raw/EditMode-20261011-001606-4c38f845327a4147af2989a4663df395.log.gz` | `03EFD5912E0E0C9D6FA1B668760C1F0677CE3FABC755B95534863D9E9C2BB482` | `A9447DFDEB10273E2D8036BD08DF91352AB95B8D0A6B64E65FCBB85F028707D4` |

All XML reports parse with coherent totals, zero failures, zero skips, and zero inconclusive tests. Each compressed log was decompressed and its SHA-256 matched the corresponding raw log hash above. `git diff --check 794ee2b11c412c67a9d86c6d560bccf3ed101237..352446f9e350d5dd103ade4010921072b429edbb` passed. The code and Assets trees were unchanged between validation and the implementation commit.

P12-G remains `WAIT_DEPENDENCY`; P12-A remains `WAIT_DEPENDENCY`; P12-B remains `INCOMPLETE`; P13 remains `BLOCKED`; Phase 12 remains `OPEN`. This slice adds one integrated P12-G FactionAffiliation->Person dangling-reference rejection and the required internal test-only override. It does not establish complete owner/epoch coverage, runtime-wide quiescence, capture eligibility, export/hydration readiness, P12-A/P13 readiness, or Phase closure. No ProjectSettings or unrelated `.meta` path is included.