# P12-G Genealogy partial-hydration failure atomicity — validation

**Base P12 canonical:** `f5f1a247bf72b8d1cfdffe62f3486989e4692d5c`

**Implementation candidate:** `45692c94c6f22fb3798cc246a89007d151930315`

**Git tree:** `23727c85a540bf30f595875294532e5080ce3c9f`

**Assets tree:** `a3b896bb1e2ff3005be642557780ea563ed98f88`
**Unity Editor:** `6000.3.9f1`; runner: `Tools/UnityValidation/Invoke-UnityValidation.ps1`.

## Scope

The restore stage now exposes a test observer after a Genealogy edge and its adjacency entries have been added to the private staged store. The existing private-stage atomicity test supplies three Persons and two valid parentage edges, then injects `InvalidOperationException` at that point after the first of two edges is staged. It verifies rejection before publication, preservation of the active session and completed-boundary token, healthy source owner state and unchanged included-owner projection, deterministic continuation parity with a control, and successful retry.

This covers one thrown failure after partial private Genealogy hydration. It does not cover the factory's false-return branches, every Genealogy internal failure path, other owner hydrators, or all §6.4 boundaries. It changes no gameplay semantics and adds no persistent encoding or P12-A behavior.

## Validation

All XML files report zero failures, skips, and inconclusive tests. Compressed log decompression was verified against each uncompressed log SHA-256 before the raw runner logs were removed.

| Gate | Result | XML artifact | XML SHA-256 | Compressed log | Compressed SHA-256 | Uncompressed log SHA-256 |
|---|---:|---|---|---|---|---|
| Focused `SimulationRuntimeAdmissionTests` | 176/176 PASS; injected private-stage cases 56/56 | `Focused/EditMode-20261010-212529-013262426c934d17bc2f438f621c0e4e.xml` | `E5CA00175EF307D7EF4C9F2229218E84D81ECB650A491A50282335508753B68D` | `Focused/EditMode-20261010-212529-013262426c934d17bc2f438f621c0e4e.log.gz` | `7E5C7C211F62BA4CC7FA10E0035EDBFE8205EF3318A9B09AE495E044CB2C4F94` | `0BAB3AAEF879AA58467DECC6E845A04B703E3EF489EF3C61FFE74DBDF90C4CE9` |
| ALL EditMode | 2850/2850 PASS | `AllEditMode/EditMode-20261010-212727-dd6de5c15b2e47d5a490ea2f0e4ff15e.xml` | `79DBB3BADD7E1985DEA5F36D0E4D5A9C88CCCB72C519F65BAB0E2262FF10E9EA` | `AllEditMode/EditMode-20261010-212727-dd6de5c15b2e47d5a490ea2f0e4ff15e.log.gz` | `2C74C0E87339BC613C31699F2DF78882F60D3BBBCBE27135B659EE54F8CCC0F9` | `92BE80D10723D09182AB641C3C846265F978D15577A9D3657B66F1816E86A4EE` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 PASS | `OfficialSmoke/EditMode-20261010-212820-b04da06939294286a5d494b995c8c519.xml` | `6C1F7E6C8D4B565C8FC50BD9863CBAEE37D05358D58333F7A336ABA4D3E26535` | `OfficialSmoke/EditMode-20261010-212820-b04da06939294286a5d494b995c8c519.log.gz` | `0756D00B23A1174B3C8A534877268431EC57C0BF0D04C401FF637102E05DE0FA` | `4AFD9BFA475E3C5AE434E9E1DE51DADDD977CF67F7B8DD9B88FAEBD0320FE830` |
| `git diff --check` | PASS | — | — | — | — | — |

P12-G remains `WAIT_DEPENDENCY`; P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains `OPEN`. This evidence does not establish complete owner/epoch coverage, capture eligibility, export/hydration readiness, downstream readiness, or Phase closure. Protected ProjectSettings changes and Unity-generated unrelated `.meta` files were excluded from the candidate.
