# P12-D unpairable City capture-envelope validation

## Review finding and correction

- Superseding review addendum: `d36d63a91f5440527a35d1bea724ef89ac4b1623` on `codex/review/phase12/P12DCityNpcAssemblyImplementationExactTipReviewR1`.
- Prior candidate tip with the snapshot/evidence pairing flaw: `92ad87f2a9fffa1f4fb7e1f7d996af48ddfad50e`.
- Canonical base: `1c7b906c172d9e47020996888db64bd2516b451a`.
- Corrected code commit: `5aceb2b7ce49ffe009489627731cd6689fe2d200`.
- Corrected code `Assets` tree: `e6c0776052dbfdd8a83ce51eb15fd15cf9d89b2c`.

The prior API returned owner snapshot S and City capture evidence A independently; `TryStage(S, B)` could therefore stage captured values under unrelated D/F evidence B. The correction replaces that pairable API with `P12DCityRootOwnerSnapshot.StagingCaptureEnvelope`. Its privately constructed envelope owns the City values and their transient token/stamp/vector evidence together, and its only staging method carries that same envelope into `CityRuntime` and the membership linker. The snapshot itself exposes no staging method, and no public staging signature accepts a separate identity-evidence object. Before membership fill, the relation assembler compares the envelope's hidden identity with both D and F evidence.

The new swapped-context test captures City under context A and supplies D/F evidence from context B. Assembly rejects before publication; the staged membership list stays empty and `ImportantNpcRevision` stays unchanged. The test also asserts there is no snapshot-only staging method or staging overload that accepts identity evidence. Existing token, stamp, and vector mismatch tests remain.

This is limited to the accepted P12-D City/NPC relation-order assembly. It adds no P18 export/replay, P12-B mutation semantics, P12-A/P13 behavior, profile-wide readiness, or Phase-completion claim.

## Exact-code validation

Unity Test Framework runs used code commit `5aceb2b`. The final source tree passed both focused suites, ALL EditMode, official Smoke, and cumulative `git diff --check`.

| Suite | Result | XML SHA-256 | Raw log SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|---|
| `P12DCityRootOwnerSnapshotTests` | 17/17 PASS | `CFCD287A9494871D6CF27C7E4028717EA67294DAF634E8CFDBAAC4F651D0BD64` | `A794E138367001879482DEC247285068090D00681105CDE773912E3A02CD8F68` | `E974D32DD97D6AC20E80C3392BA46D06BD1C97F0516F4685A2B91165CFFA35CF` |
| `P12DNpcReceiptOwnerCensusTests` | 13/13 PASS | `BC2137478846916091C2F312ACF34520B35D8F5F1DECEB13D32B4E9E494F8856` | `8FB02B31B1694BEB9D40538811AB08070218C0178207351DD8114AE7E8BD229D` | `A129AB46D7CE4C78D16E21DC858A7D4763E5C08C480A0BE9175DDBD6C8856BD5` |
| ALL EditMode | 2592/2592 PASS | `9C7FA715B469BCAC62AA26C12E524A21F14AA4E43689B07DA406C86B97EB82D9` | `1B3EE1AF9F66F5177B6714D4C27A952803F94C216B0F485929AE9B7F430B95E8` | `F317F28B165084069DD17E764C7BDBBFD2EB3EAE6F7676B23809C094BE5973DB` |
| Official Smoke | 5/5 PASS | `AEAD4707DD4C8CCEEDBC77D3888897FF92A38BC541953898075C0577B1C663B7` | `B26B1469E658A8A58F6596E48237B434275E926D2F73F87D58FB3A068EE007A9` | `4E1E29C11FA48D667A7428FE0D5A2037A68FC1C54AAEE8C48891FD7E7901A9E0` |

Every compressed log was decompressed and byte-compared to its corresponding raw log before recording hashes. XML and `.log.gz` artifacts are committed under `docs/validation/P12DCityNpcAssembly/Raw/Followup-CaptureEnvelopeFinal/`. Raw `.log` files remain local and are not committed.

| Artifact | Repository-relative path |
|---|---|
| City suite XML | `Raw/Followup-CaptureEnvelopeFinal/Focused-City/EditMode-20261008-191901-8ac348e8e1ff42d1ac25679efaab394c.xml` |
| City suite log | `Raw/Followup-CaptureEnvelopeFinal/Focused-City/EditMode-20261008-191901-8ac348e8e1ff42d1ac25679efaab394c.log.gz` |
| Receipt suite XML | `Raw/Followup-CaptureEnvelopeFinal/Focused-Receipt/EditMode-20261008-191921-0ac274ce96984897b0cde566c60bcb59.xml` |
| Receipt suite log | `Raw/Followup-CaptureEnvelopeFinal/Focused-Receipt/EditMode-20261008-191921-0ac274ce96984897b0cde566c60bcb59.log.gz` |
| ALL EditMode XML | `Raw/Followup-CaptureEnvelopeFinal/AllEditMode/EditMode-20261008-191935-70b3a01cfa324f318fb191dcc0e55199.xml` |
| ALL EditMode log | `Raw/Followup-CaptureEnvelopeFinal/AllEditMode/EditMode-20261008-191935-70b3a01cfa324f318fb191dcc0e55199.log.gz` |
| Official Smoke XML | `Raw/Followup-CaptureEnvelopeFinal/OfficialSmoke/EditMode-20261008-192019-25368bbe8ad1422bb707da79c4e5bc28.xml` |
| Official Smoke log | `Raw/Followup-CaptureEnvelopeFinal/OfficialSmoke/EditMode-20261008-192019-25368bbe8ad1422bb707da79c4e5bc28.log.gz` |

## Source identity

Worktree source hashes correspond to the validation source bytes; Git blob IDs identify source objects in commit `5aceb2b`.

| Source file | Worktree SHA-256 | Git blob ID |
|---|---|---|
| `Assets/_Project/Scripts/CityRuntime.cs` | `8CAEB371F122FC677D43491381AF189512CB15F743913C9E9E8D3022BED8CC2F` | `d7b4275970d718134a096776e7840bde70704430` |
| `Assets/_Project/Scripts/P12DCityRootOwnerSnapshot.cs` | `0AB787BF360DC7F410049683EADD2440BBCE811FD488F19F663528EDC595ABCC` | `ce852ade6bd40ab93611065e1433871f76190fa0` |
| `Assets/_Project/Tests/EditMode/Editor/P12DCityRootOwnerSnapshotTests.cs` | `3B1EE1AF1038E7A2A60D94A9FC25888F687E345CEFD90DF98AD82E96019DD9F0` | `30d7881a6d29d0dee45051ae4a7b5979010a5228` |

`git diff --check 1c7b906c172d9e47020996888db64bd2516b451a..5aceb2b7ce49ffe009489627731cd6689fe2d200` passed. The earlier `4d93a4a` validation remains historical and is preserved; this envelope follow-up supplies the evidence for the corrected code tree.
