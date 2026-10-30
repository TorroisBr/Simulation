# Independent Exact-Tip Review — CORE-F P12-D Core-Bound Sections

**Verdict: `VALIDATED_CANDIDATE`**
**Reviewed:** 2026-10-30

## Exact identities

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate tip: `67e92e368470a314713649cf254f8a8b383557a3` (validation-record commit)
- Code commit: `7207f91e080763566496e02d4dde3572b0a8c8ef`
- Code tree: `bc2b181879bcb1b1b14a9064c00a4796feb34ec9`
- Candidate tip tree: `1013a7fc9ada293272e9344fa0bc9f30ce2efbde`
- Immediate base / code parent: `c037cff3ad001a136127708703a9f15080c203ea`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Remote candidate matched the reviewed tip at review time. The code change is five implementation/test paths; the validation record is the only additional path. No canonical promotion is claimed.

## Review result

No actionable code or authority findings.

The operation-backed path requires the active continuation-save operation and revalidates its completed-boundary witness before and after P12-D section encoding. City, Person/Genealogy, SpatialNetwork, and optional NPC sources are bound to the operation's exact owner census vector. The Core encoder uses the existing P12-D codecs, sorts the emitted sections ordinally, and rejects null, non-required, or duplicate section identities. The final image composer still matches the complete image against the operation's owner vector and the existing census/manifest checks; missing or duplicate dynamic families fail closed.

The full Core NPC source is admitted only when its exact runtime-ID set equals the complete admitted NPC census and all 22 per-NPC owner-family witnesses match. An incomplete selected Core roster is explicitly treated as the mixed-roster compatibility case: it does not emit partial Core NPC rows. The section encoder then takes every NPC D/F row from the Unity whole-roster projection. The mixed-roster regression verifies omission of Core NPC sections and that the fallback projection contains every runtime NPC, including the unselected member. The focused mixed-roster test checks the projection before final image composition; the branch itself is direct and final census validation rejects missing or duplicated rows.

The complete-roster regression compares each emitted Core P12-D section with the composed image, including section version, role and revision, row count, owner keys, row revisions, and payload bytes. It also checks the owner-section inclusion flag against the operation's complete Core NPC-source binding. No prior assertions were weakened. The pre-existing no-operation/legacy encoder route remains on its former source and ordering behavior.

Only the five source/test paths named by the validation record are in the code delta. The candidate excludes ProjectSettings changes, `.meta` files, private evidence, and unrelated/untracked data.

## Exact-source validation reviewed

The five changed code/test input hashes match the candidate checkout and the Unity runner's stable input inventory:

| Input | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GCoreBoundOwnerSources.cs` | `c7d0ba838b899db8c5dd2752796ecc450de10dc05643899270028fb75f02c842` |
| `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GContinuationSourceWitness.cs` | `54c8502afbd0ce4f706620971379cf0579a56144ae76d83dd276a4ff2975b4bb` |
| `Assets/_Project/Scripts/P12ADailyV1OwnerSectionEncoder.cs` | `8566e0ae4af2ba6cad8b2c951f3104244a8642993bd28243b2249a55cc49ff72` |
| `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.cs` | `40267c1814b7b601899dc08979ee9354c573dceef41c1416b4e3363ead5e1aed` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `7afebe136a26b6ae43cce50610023bbb2ec3c97414f00cc7d2e5afa5f11a09b5` |

- Official CoreValidation run `033531cb7d054d40ac6806853ea6c257`: cold 936/936 and warm 936/936, no skips; stable input digest `31582a5f9599b9284275e45667c4a1cb25ba8c5d4ee056db5e7053260db75fbf`. Protected manifest SHA-256 `73523c99f28240935aec8a072d9225d62fd0e2cfb93e638a27f0c776ede9eb7b`; cold TRX SHA-256 `fa71dfde62dfc4524715ecc5bccd175269015eaa7985c3a71afdee2d8bb75142`; warm TRX SHA-256 `5322c625c0008e40ecc30d9ba8156ac0bd751c0430118576dca7977583ceed48`. The Core input inventory includes the two Core source files; Unity source inputs are covered by the Unity inventory.
- Official Unity EditMode `SimulationRuntimeAdmissionTests`, run `3dd66955d63348fd89c0e5de75425368`: 228/228 passed, zero failures, skips, or inconclusive results; stable before/after input inventory. Protected manifest SHA-256 `4b0dd531313df0e3e9a18f1bb7f55c0e3a8de18fb7889e9ca9232de2e1db5d38`; original XML SHA-256 `036773ff26178ee5131e958a05a00602cc782d35701cc98b479f33ee2082b8c5`; original log SHA-256 `dda28d5c45be4eb8c57625287875dcc125fe6aa77b6974492e8f1dcf899ba68d`.
- Protected report/XML/log hashes matched their manifests. `git diff --check` passes for the exact base-to-candidate range.

Raw reports, logs, and manifests with machine-local metadata remain in protected storage and are not copied into this record. Full Unity EditMode, Smoke, LongRun, permanent privacy-safe evidence packaging, and CORE-F acceptance are not claimed. These broader gates remain outstanding before any eventual promotion.

## Promotion status

This review covers the incremental P12-D Core-bound capture/encoding and mixed-roster branch only. It does not establish complete Core-owned P12 capture/restoration or CORE-F completion. No canonical promotion is approved or claimed.