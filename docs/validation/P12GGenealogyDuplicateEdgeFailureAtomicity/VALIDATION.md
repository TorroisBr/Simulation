# P12-G Genealogy duplicate-edge partial-stage rejection — validation

**Scope:** A restore-only, null-by-default test seam substitutes a detached
schema-v1 Genealogy snapshot at the private snapshot-to-hydrator boundary. The
fixture preserves revision `2` and row count `2`, replacing two valid source
edges with `[A→B, A→B]`. The coordinator test verifies exactly one private edge
was staged before the factory returned false, rejects before session
publication, preserves the original session/token/health/owner projection,
matches uninterrupted continuation, and permits a later successful retry.
The normal restore path supplies no snapshot override. This is one bounded
Genealogy false-return case; it does not complete the remaining hydrator,
validator, rejection, no-replay, or all-owner parity matrix.

**P12 canonical base:** `ded30aee3c8ccbc28257103a6af04fda99b0da3f`

**Implementation candidate:** `728365f836abd61ed6e884a7e756b85e16c7ba15`

**Candidate Git tree:** `b2472f81d81dac51740bfe3c649a9d4f50c35d25`

**Validated Assets tree:** `e801be2441e566bd51bdd328e68ff13ca3eafcdc`

**Unity Editor:** `6000.3.9f1`

**Runner:** `Tools/UnityValidation/Invoke-UnityValidation.ps1`

| Gate | Result | XML artifact | XML SHA-256 | Compressed log | Compressed SHA-256 | Uncompressed log SHA-256 |
|---|---:|---|---|---|---|---|
| Focused `SimulationRuntimeAdmissionTests` | 177/177 PASS | `Focused/EditMode-20261010-214746-33478eab1cbf4324904c3f8c8c760596.xml` | `9BD8ACD269E5733C8ADD9732E9A5803FE2E1BC9B61A7811341F97101B5559926` | `Focused/EditMode-20261010-214746-33478eab1cbf4324904c3f8c8c760596.log.gz` | `335F80F171ADD585E13BE351F3D82F3475A41713B009D2866894D4BCE2461C91` | `827E6225955C3BBC4EB500F24D43B5FB0E3E19A7D321D64D1756F0752C0C3DEE` |
| ALL EditMode | 2851/2851 PASS | `AllEditMode/EditMode-20261010-214814-b548764f30a346eb9e7bda4edf493410.xml` | `1054DCA477F5EAE8514EE15E2FBFAE7B1EA46225785F8BF740D439DCB105C188` | `AllEditMode/EditMode-20261010-214814-b548764f30a346eb9e7bda4edf493410.log.gz` | `4CCFA634EE5A7728058006C56E6411C9119A70D743F14CA2D99321E5630D26D4` | `38E9E99CE5CD537DA89EAEBFE453B39C6832A2A3273761C4CF5F6A6D533FA680` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 PASS | `OfficialSmoke/EditMode-20261010-214908-ce363defeaa64e32889b5a92bcc0ea7c.xml` | `11F211447B5F10E70619A54F4B4C3013FF45CB81835131B065C5DCEEC7A96448` | `OfficialSmoke/EditMode-20261010-214908-ce363defeaa64e32889b5a92bcc0ea7c.log.gz` | `D9D8BA50A0CC46D90E9EE9A0198788952B4BB53EB55BB0FFBC9A6E2F8AED83C9` | `D695E7EAC2876F733BA833971D4F81BF609235DD921DEC6907D507F0EF17AC9A` |
| `git diff --check` | PASS | `ded30aee..728365f` | — | — | — | — |

Each XML reports zero failed, skipped, or inconclusive tests. The new
`DailyV1RestoreRejectsDuplicateGenealogySnapshotAfterOnePrivateEdgeAndKeepsSourceRetryable`
case passed in both focused and ALL EditMode results. Compressed logs were
decompressed and independently rehashed against the recorded uncompressed
SHA-256 values. Unity-generated `ProjectSettings` edits and untracked `.meta`
files were excluded from the candidate and were not staged.
