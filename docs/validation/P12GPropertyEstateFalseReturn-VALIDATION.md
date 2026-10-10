# P12-G Property/Estate partial-stage false return — validation

**Scope:** A null-by-default restore-test seam substitutes a detached
Property/Estate snapshot after Institution and Office owners have staged
privately. The fixture starts from one valid Property ownership row and keeps
schema versions, row counts, and owner revisions unchanged while replacing
only its owner PersonId with one absent from the staged P12-D root. Normal
Property/Estate hydration rejects with its typed unresolved-reference path
before the Property/Estate pair is staged. The integrated test verifies no
publication, preservation of the active session/token/health/owner projection,
continued parity with an uninterrupted runtime, a successful retry, and
parity after another boundary. It covers this one bounded false-return case;
the remaining P12-G failure, graph, no-replay, inventory and all-owner parity
requirements remain open.

The accompanying runtime change permits populated Property/Estate owner
cardinality only for a restored-continuation runtime. Fresh Unity bootstrap
still requires these owners to be empty. This aligns the restored runtime
census registration with the accepted staged-hydration contract and enables
the test's later valid retry.

**P12 canonical base:** `f2a6ff357d739b58ae1757f4870b4b75aef1a7e9`

**Implementation commit:** `4f9de0ca84b4d2518e0c19405b48609e014b839b`

**Implementation Git tree:** `7348f6ef113d98bb4648875b4ae186a776f556dc`

**Validated Assets tree:** `47d26eca4ed1d7e3e4f319153aa5297f225dbbc0`

**Unity Editor:** `6000.3.9f1`

**Runner:** `Tools/UnityValidation/Invoke-UnityValidation.ps1`

| Gate | Result | XML artifact | XML SHA-256 | Compressed log | Compressed SHA-256 | Uncompressed log SHA-256 |
|---|---:|---|---|---|---|---|
| Focused `SimulationRuntimeAdmissionTests` | 178/178 PASS | `P12GPropertyEstateFalseReturn-Focused.xml` | `393577204344867F9C7ED23ECC2470CA92E9AFEB55DD0660311F409A4F49170E` | `P12GPropertyEstateFalseReturn-Focused.log.gz` | `7BD46893E7B25DD49A6BA735A1F24C7C4CEDA597CFAA9F53F1662277AB365F95` | `4E17945C40D53EC699789A4696ED6B9ECAE6EC175F33AD8974C3CEA2AC3FDEC4` |
| ALL EditMode | 2852/2852 PASS | `P12GPropertyEstateFalseReturn-AllEditMode.xml` | `6D6963391E5A737A5E0ACA21E64F3636BD82F2DBFD2A39BCECD2B6CA3B093D8F` | `P12GPropertyEstateFalseReturn-AllEditMode.log.gz` | `54CB842F8137C69FE23B496786DB4D1208F823C79C880022145B6974D327EF4C` | `8E005CC1D975E1D2D4C38D5442E391E5DBA9105F54F749521E73124F4C5504FD` |
| Official Smoke (`EditMode -TestFilter Smoke`) | 5/5 PASS | `P12GPropertyEstateFalseReturn-OfficialSmoke.xml` | `7100908DE410F054ADC2931A8E677491A3B5674E73A5793E5B21CC89056B0A37` | `P12GPropertyEstateFalseReturn-OfficialSmoke.log.gz` | `1F0925D049F78CD54FD162339D538DB976BEC9CDD57F41AFF0D21EF9054358AD` | `ACF443D44D25A202A1B470363E3724D50D584921C83A30C1A723EEC4C9936BF3` |
| `git diff --check` | PASS | `f2a6ff3..4f9de0c` | — | — | — | — |

All recorded XMLs report zero failed, skipped or inconclusive tests. The
`DailyV1RestoreRejectsPropertyEstateDanglingOwnerAfterEarlierEOwnersStageAtomically`
case passed in both focused and ALL EditMode results. The exact XML and
compressed-log artifacts are retained beside this manifest. Unity-generated
ProjectSettings edits and unrelated untracked `.meta` files were excluded from
the candidate and were not staged.
