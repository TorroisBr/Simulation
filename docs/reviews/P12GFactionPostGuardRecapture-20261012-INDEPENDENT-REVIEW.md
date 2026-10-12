# P12-G Faction post-guard recapture — independent review

**Verdict:** VALIDATED_CANDIDATE
**Review:** PASS — exact-tip test and sanitized-evidence review
**Reviewed evidence tip:** `cd1e01c2ddbf445f59d3fb57e744fc14a4696d2c`
**Candidate branch:** `codex/phase12/P12GFactionPostGuardRecapture`
**Code commit:** `d89aabb64b29d0cb750766b54b1c1c41da03e2b0`
**Code tree:** `484544c20617dab14a93f7cff1f231fa63b9749d`
**Assets tree:** `8f79c02e8c032aa2036e8a314ed34598f9b38cda`
**Test source blob:** `0376885edf6dc3083e3c6f0ef8ee4d065cae1cc0`
**Canonical base:** `e1190fb127cc74a98169f1c07f357cd8a487e186`
**Evidence tree:** `1b3c165ca23377029844f3a89205c92a7fbd7b05`

## Scope and test review

The code commit is a direct child of the canonical base and changes only `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`. Production scripts are unchanged. The evidence commits add only ten files under `docs/validation/P12GFactionPostGuardRecapture-20261012/`; the candidate diff is exactly the test plus that validation package. The published candidate branch was confirmed at the reviewed evidence tip before this review was recorded.

`DailyV1RestoreRejectsFactionAddedAfterTargetChecksAndKeepsSourceRetryable` exercises the existing `CandidateGuardBound` hook. After whole-graph target checks and mutation-guard binding, the test uses supported `SimulationRuntime.TryRegisterFaction` to add one Faction to the private restored target. It verifies the installed FactionStore census changes from zero to one record, its shared revision advances, and the P12 owner mutation epoch advances exactly once. The subsequent target-owner recapture rejects with `TargetOwnerVectorFailed` before recapture success, boundary admission, or publication.

The test confirms the source session and completed-boundary token remain healthy, the source owner/projection does not acquire the injected Faction, retry from the original source succeeds, and the retried graph and next-boundary projection match an uninterrupted control. This is a bounded post-guard target recapture witness, not a production behavior change.

## Validation and artifact integrity

The exact code/test tree is the one named above. Validation reports show zero failed, skipped, or inconclusive tests:

- Focused test: 1/1 passed.
- `SimulationRuntimeAdmissionTests`: 200/200 passed.
- ALL EditMode: 2879/2879 passed.
- Official Smoke: 5/5 passed.
- `git diff --check`: PASS.

The test appears in the Focused, Admission, and ALL EditMode XML reports. It is not expected in the Official Smoke report. I parsed all four committed XML reports, verified suite totals and result counts, decompressed all four committed logs, recomputed each documented stable XML projection using the recipe in `SANITIZATION.md`, and checked each recorded checkout-byte and Git-blob SHA-256 against the exact files. Git blob OIDs and byte sizes were also confirmed:

| Suite | XML blob OID / bytes | XML blob SHA-256 | Checkout XML SHA-256 | Gzip blob OID / bytes | Gzip SHA-256 | XML projection SHA-256 |
|---|---|---|---|---|---|---|
| Focused | `4eb1da5ca89f161a9383200c8aca0b70c67acac6` / 4,680 | `9985FBC0F6FD84D97011D142B82C23DC0EE05DAA743E3A9EB775B94AE55ACC1D` | `D24172157D2B5052B9ABB1AE2B989D85CA1EBE1F4FC0A47DDB68676F6D60FC98` | `e6a621a42bface5c11138d97c9f380bd3c6a7b21` / 310,980 | `E0E683C7D7E6AF29B9DDE442685C659B3A2BA76FABDA9B42F2262ACE22AEEE25` | `673AB82FE95C04B232CC2726A44D27837CDBA8DA94FB9AA4CFA3B7C9AECEB3F4` |
| Admission | `53feff14aa0522c9428d896e86bc3707cf0e4b28` / 440,108 | `45A8DB235C076E059B1582E1EA13063D62CA08560A2552CF29458F78B87D6A97` | `70601C7A185B2D57635D6C5EBA04886C5F9267E49886413D9768E476A4050FC0` | `f65094608f1d43f84e0919ba91cfd8c747dddbda` / 1,949,713 | `C48E1A6FBE08480F5C341811BF621DC38794D4B4427AF08AFBB955DD83859791` | `B8CE096A6C5555027FFB18FB8331A92C2B5BDF64B6A7BD8DDB325466690E9FCD` |
| ALL EditMode | `c9e04a6919f9e4cac9c661cac921290dc1b07deb` / 2,934,264 | `096831C23D4DF5F63AB1671221E38EE9A877DFF96D6EF2218C3DF323421711FF` | `A023FBCBD90762E20DA41E0F847340239FF97C9420DA13FA790CF318D5C1CD14` | `aef79b9d38d0190953079d74d7641ba45f2d5761` / 4,875,769 | `E871CBB0BEEB749A8C8AAA052318233E08362BF915D0E5AFD88A3C49090604E4` | `C537AEBF3D50EFAF8AA7C917E484385D66E5E5C4E2A752E388F5A158D225567C` |
| Official Smoke | `5881431ad7f4508baed5c7a06f10c01d719b4edf` / 6,220 | `D1D52DC419B72F49877E58858AC9762A988E9CBB3FDBAFB3F61F3C4D86FA9E9D` | `A208B6068FD0E540068E3C0798D1DCB36480B62C8DC150ACAC49AAFEF126EFF5` | `d19c73bd7cda609eee9d6cf5ef2d0dac64649f99` / 9,989 | `6A26C9E38C241653F9050658983B40E3E31A7BFACF6DB394DBEA010E2D2AA165` | `DB5E87FF13ADF107309212DA9BF07725EEFA623CB558BEA9968F98526A8B45B6` |

The privacy scan covered candidate XML/docs/test source and all four decompressed logs. It found no absolute local paths, UNC paths, local username/account labels, bearer or GitHub credential patterns, or private-key markers. The generic sanitation placeholders are retained. The Git diff contains no files outside the intended test and evidence package.

The original pre-sanitation report files are excluded from the candidate, so this review revalidated the published sanitized artifacts, result projections, counts, and hashes; it did not independently rehash the excluded originals. Unity was not rerun because the final commits after the code validation changed only sanitized artifacts and documentation; the test source and production code are unchanged.

## Limits

This is one bounded P12-G Faction late-recapture rejection witness. It does not establish complete target-owner coverage, complete owner or shared-epoch coverage, runtime-wide quiescence, all graph-rejection/failure-atomicity cases, causal no-replay, or full included-owner multi-boundary parity. It does not establish capture eligibility, export/hydration, P12-A readiness, P13 readiness, or Phase closure. P12-G and P12-A remain `WAIT_DEPENDENCY`; P13 remains `BLOCKED`.
