# P12-G PoliticalClaim Recognition→Institution rejection — validation

**Scope:** One populated P12-E `StatusRecognition` PoliticalClaim and its valid recognition history are captured from the selected Daily-v1 runtime. The null-by-default restore-only snapshot seam changes only the recognition's Institution identity to a distinct well-formed ID absent from the exact staged InstitutionStore and recomputes the derived `RecognitionId`. Claim/recognition schemas, row cardinalities, shared revision, state, day, reason, and complete history are preserved. The integrated restore rejects before Claim staging/publication after earlier E owners stage; the test verifies active source preservation, recognition identity/history retention, source/control continuation parity, successful valid retry, and parity after another boundary.

**Canonical base:** `1232716829900e2985dcf4a43bdb028d95b81ae2`

**Implementation commit:** `51d68c9be01f864df73641435e6781be04ebe00e`

**Implementation Git tree:** `bbb01fb7ce8643c5ddf5885d38c96c8278325485`

**Validated Assets tree:** `f4b593c9c8a1acb945f0b0f3273c782ebae07e9f`

**Unity Editor:** `6000.3.9f1`

**Runner:** `Tools/UnityValidation/Invoke-UnityValidation.ps1`

| Gate | Result | XML | XML SHA-256 | Compressed log | Compressed SHA-256 | Uncompressed log SHA-256 |
|---|---:|---|---|---|---|---|
| Focused case | 1/1 PASS; failed=0, skipped=0, inconclusive=0 | `raw/EditMode-20261010-231014-1146dbb5f1004e6e951e852c2bc0a461.xml` | `48EBA090D68C1F334A79286D017724742CAC20519AA688043A45C63AE46D39B8` | `raw/EditMode-20261010-231014-1146dbb5f1004e6e951e852c2bc0a461.log.gz` | `158622B69A6306D8D599CFEBAF1FDBF71959AD83890B07795D73BA1D2F6AC813` | `84F7F1D7365D39CAAB9E3B08E1338FB2DE32F42DB9319A77C0647CE36EFBEA64` |
| `SimulationRuntimeAdmissionTests` | 181/181 PASS; failed=0, skipped=0, inconclusive=0 | `raw/EditMode-20261010-230814-0c50f60a10de46e69403e122a209c47b.xml` | `BAB6FFB6A3FDA071AA3D74CC80149D27276403D3F532AC94D1A3E9CCFE8660ED` | `raw/EditMode-20261010-230814-0c50f60a10de46e69403e122a209c47b.log.gz` | `04F665B3ED38A28A4AAE829EBAC1BE3D0E09942A3486ED3F687459DE887CF03B` | `464D9132FAEAA785E296D922840FBF77C79864B9DA7B1688923CB3D3F8B39836` |
| ALL EditMode | 2855/2855 PASS; failed=0, skipped=0, inconclusive=0 | `raw/EditMode-20261010-230846-c80c9625d5ff45248bdbdd654a7c900e.xml` | `B2A10F7346794AD63518A2A62E071FF333C5306B6E58AE645BA342C81CB3CD93` | `raw/EditMode-20261010-230846-c80c9625d5ff45248bdbdd654a7c900e.log.gz` | `9119487890A662D6DC40DAB0C5B1AECCCCE2F8CDAEC3AE0926CA6D0D879B24EE` | `457C764C587DFDE7F74C2E9C1A56480060DD500328FA0D13EEBF940DE275BFC0` |
| Official Smoke | 5/5 PASS; failed=0, skipped=0, inconclusive=0 | `raw/EditMode-20261010-230936-9300700745ee466db9cd1d4041fc9a23.xml` | `C8679C0BBAF4AA9CFC6C008E78E46B3E12DF155D90ACCCF975323E0C24E65C18` | `raw/EditMode-20261010-230936-9300700745ee466db9cd1d4041fc9a23.log.gz` | `88F74E97D77C1F9C9B8E6008C23FC1B7C6069C8E395E44B0687B904854A6FA1B` | `1AA93378C265A4ED3B4FD30729F6FA54017802E536FAE2755FBB797FBCFA6E21` |

All retained XML reports parse as `Passed` with zero failures, skips, and inconclusive tests. The coordinating checkout computed each XML, compressed-log, and raw-log SHA-256 recorded above. The code/Assets tree is the exact tree named above. `git diff --check` passed. No `ProjectSettings` or unrelated `.meta` paths are part of the candidate.
