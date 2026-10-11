# P12-G target-owner census-before-validation — validation

Date: 2026-10-11

## Candidate

- Canonical base: ab7c4b4e8347546c6bd8f4353d98353176b99807
- Code commit: 3afda34a8102214403cf633023938ae5d45f2d57
- Git tree: 45437123894742748fb108d86fbca0c4bd7681f5
- Assets tree: d27f432cabffe4f0fd446ee56d60e198534df8d0
- The code change adds one EditMode test in SimulationRuntimeAdmissionTests.cs.
- The test allocates a valid NPC RuntimeId through the candidate composition, registers through TryRegisterNpc after initial target-vector capture, and verifies rejection on final recapture, source-token/owner preservation, and successful retry.

## Results

| Gate | Result | XML SHA-256 | Compressed log SHA-256 | Decompressed log SHA-256 |
|---|---:|---|---|---|
| Focused (1/1) | PASS | C1872F396BBC35664088A296A95393F171DC7855AEC0486F7707A6B62B1440AA | 98422D80B5DB7EBF139D45AE3E1EA4F810E4E40F3D07EC022E37F235B7056B32 | 9B2203F6E22FFBD69F6D87499A4B20B7EBD0CE2AEF368D653F5F3091C21B76C8 |
| Admission (194/194) | PASS | 3E43861662702CB1D333999502F5983EC2EBA4A11B8FAA7F95B8539A1EF1B4F9 | BCDE9FFAA492F4541E9C0361B5F3BEAF15186D127C32B95E5A577302C80CBA60 | 97A677B1D77FD51EAE236CC26ED98A951B5535CEB0559AEF2E3C11936B6C9EB6 |
| AllEditMode (2868/2868) | PASS | 51D991118B490BBF698837E7828C5E4B90C3C276E5F74904FAF23A315F9DBCE5 | 29AD948E8B6A734E5145808181C7C6BFD0966DD0F92F20A0C65D15DD288AB911 | 5AE47459B5B0CB7E30BB18F507C318CC1707BBF2D52AD7973DF33DEE66467142 |
| OfficialSmoke (5/5) | PASS | 203A4595819D82079915906B8FFA450DB2BF039B7F45FBD06247CD39D27C49C7 | B49801AAA55DDE02D962B56AB7084032328B91A538E6EC0227244E120FD36462 | 7095360F4DA561D652E39EB2DD14BC02EDB52C2D0CE36491BEF9D9623898C96C |

All four retained XML reports show zero failures, skips, and inconclusive tests. Exploratory failed runs remain outside this evidence package.

Exact code-range git diff --check is recorded with the candidate review.


## Publication artifact path sanitation

Incidental absolute Windows local filesystem roots in the retained XML/log artifacts were replaced with `LOCAL_PATH_REDACTED`, `PROJECT_ROOT`, or `UNITY_EDITOR_ROOT` tokens. Test-result content and all reported counts remain unchanged. See [SANITIZATION.md](../../P12GSanitizedPublication-20261011/SANITIZATION.md) and [PATH-NORMALIZATION-2.md](../../P12GSanitizedPublication-20261011/PATH-NORMALIZATION-2.md) for both artifact hash mappings. Unity was not rerun because the reviewed executable and test-source trees are unchanged.
