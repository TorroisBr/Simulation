# P12-B Daily-v1 owner/cardinality matrix — independent implementation review

**Result:** `VALIDATED_CANDIDATE`

**Canonical base:** `89b2368e9756069b2f52cd7cf17c26735f7c103f`

**Code commit:** `c50c4237d1e1023567f5ca0b24376a23d84bd4b7`

**Code tree:** `d26ad1c0235bcc78104930ce9a2fce6883558603`

**Assets tree:** `70e7a06d1f7adfc59e3f07e8c9a5448567750863`

**Reviewed documentation/evidence tip:** `eed4c321c76bc5b734c6afaa7516af86df5f2e70`

**Reviewer:** independent P12 owner-census reviewer; review was read-only and performed against the exact pushed commits.

## Findings

The candidate is a clean descendant of the current P12 canonical base. The full diff changes one EditMode test file and adds the validation manifest and sanitized NUnit XML only. No production source, profile/configuration asset, owner registration, runtime behavior, mutation scope, or operation/epoch wiring changed.

The added assertions check exact TravelState section IDs, schema, NPC owner identity, singleton cardinality, and local revision; both plan section IDs and exact embedded plan owners/revisions; and dynamic NPC/Person lifecycle sections for schema, exact owner, cardinality, and local revision. They also confirm that Person-backed NPCs do not keep a duplicate NPC residence section. Existing MoneyAccount, Inventory, SpatialKnowledge, and Knowledge family assertions remain in place.

The new test follows the normal runtime path through Person registration and NPC materialization. It validates the census after each operation, confirms the resulting NPC-to-Person binding, and finishes with a successful census assessment. The formula `65 + 20*N + U + P` matches the source provider families and observed transitions: 275 sections at the authored 10-NPC baseline, plus a registered Person, then the additional NPC and its changed binding.

The reviewer verified the validation XML hashes and passing counts against the candidate: roster temporal test 1/1, Person registration/materialization 1/1, Daily admission 1/1, selected-profile inventory 1/1, ALL EditMode 2444/2444, and official Smoke 5/5. All XML reports have zero failures and skips; `git diff --check` passes. The manifest is [`P12DailyOwnerMatrix/VALIDATION.md`](../validation/P12DailyOwnerMatrix/VALIDATION.md).

No findings were raised. The tests preserve the accepted profile boundary: SampleScene uses the dedicated P9-B-only Daily-v1 profile, while P10-A Ruin/LocalTopology remains the separate GeneralTest proving profile. The candidate makes no claim of exhaustive supported-write coverage, complete shared-epoch coverage, global quiescence, capture eligibility, export, or hydration. P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, and P13 remains blocked.

## Integration constraint

The reviewed code tree is unchanged by the documentation/evidence additions. The candidate can be integrated only after refreshing P12 canonical and confirming its ancestry and profile assumptions remain current. The promotion remains a partial census-evidence slice and does not complete P12-B.
