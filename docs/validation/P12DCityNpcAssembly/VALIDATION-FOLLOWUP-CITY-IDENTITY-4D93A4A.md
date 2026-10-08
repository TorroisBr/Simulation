# P12-D City capture-identity review correction validation

## Scope and provenance

- Canonical base: `1c7b906c172d9e47020996888db64bd2516b451a`.
- Prior exact-tip candidate: `483d24bb818b98b984b96518f360c13611911cf9`.
- Corrected implementation commit: `4d93a4aa38ed66712c7c8b0951a61e0303d6c7af`.
- Corrected implementation `Assets` tree: `60ba6768fd1d9c01bfe365fc29ffca4eed1e9a6c`.
- Prior independent review returned NEEDS_CHANGES and identified City-to-D/F capture identity binding plus a trailing-whitespace error. This follow-up addresses only those items within the reviewed P12-D relation-order assembly design.

The City capture now emits transient token/stamp/owner-vector identity evidence and passes it to the staged relation linker. Before relation validation or any private membership fill, the assembler requires each staged City linker to match both D and F projection evidence on the exact same token, capture stamp, and owner-section vector. The City capture rejects a vector that differs from its token's owner-section vector. Tests cover token, stamp, and vector mismatches and verify that failed assembly leaves City membership lists and revisions unchanged. The revalidation record's reported trailing whitespace was removed.

No P18 export/replay, P12-B mutation semantics, profile-wide readiness, P12-A/P13 behavior, or Phase-completion claim is added.

## Exact-tip validation

Unity Test Framework runs were performed on implementation commit `4d93a4a` in the isolated implementation worktree. The focused City suite includes the three identity-mismatch cases. The focused receipt suite, full EditMode suite, official Smoke suite, and cumulative diff-check all passed.

| Suite | Result | XML SHA-256 | Raw log SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|---|
| `P12DCityRootOwnerSnapshotTests` | 16/16 PASS | `5A95B4E104C8B2B115888371D4F5E806A3895C9C15B3FF3201A2C4EA47C7BAA9` | `2C81530D229C97C67B0FCF95DBD9E78479D28F60A49A100B7BE8423CA5C0BBE5` | `CEB67BCB13301EDEF9B0E56D4BB72139EFAB38D33E02B0EC959D1992110621FC` |
| `P12DNpcReceiptOwnerCensusTests` | 13/13 PASS | `278645D46E30B5D5B164D7655FB485624DB5436A78E4E09C03664C94B281AC0F` | `EACFDBED97414DBE572DD0A6010B950387E3F04B340EF8A315F92FC1A1CF25CC` | `4FB2938D2B5663D41984D5382EB30CFEB291ECCEE50256DF091BEF731BB6688A` |
| ALL EditMode | 2591/2591 PASS | `4744D39597C59603EFE9E04969FF970DC3DF91BA2115AF894F74B20C8150676D` | `3A493F7B27987D531ACBDFA9252525752CC968B98DAB153D3EC818C5521FD54B` | `5BF548CB5B651FB6D7837549F5FCA017BDB0B383903B4C2C721CC7C5F45BDB98` |
| Official Smoke | 5/5 PASS | `F620BC236A987B660BC17FF7F4A7566E4BBE62E860838CE5B92BD15573C6F2A2` | `BA10A7C647EF70A850522D54E2867503D33DFCDDEDE1244D824BC5E2E4438242` | `D78AC86E5C35E0FE8C74A5D578EADAF7BF3A90AFB113FB60E5C39311D1B00E90` |

The compressed logs were decompressed and byte-compared to the raw logs before recording their hashes. XML, compressed logs, and raw local logs are under `docs/validation/P12DCityNpcAssembly/Raw/Followup-CityIdentity/`; raw `.log` files are retained locally but not committed.

| Artifact | Repository-relative path |
|---|---|
| City suite XML | `Raw/Followup-CityIdentity/Focused-City/EditMode-20261008-190456-d1944f5afe8a4f8eb07db4343aececa3.xml` |
| City suite log | `Raw/Followup-CityIdentity/Focused-City/EditMode-20261008-190456-d1944f5afe8a4f8eb07db4343aececa3.log.gz` |
| Receipt suite XML | `Raw/Followup-CityIdentity/Focused-Receipt/EditMode-20261008-190519-523cc32cab8a40dbb338a6981bcbb734.xml` |
| Receipt suite log | `Raw/Followup-CityIdentity/Focused-Receipt/EditMode-20261008-190519-523cc32cab8a40dbb338a6981bcbb734.log.gz` |
| ALL EditMode XML | `Raw/Followup-CityIdentity/AllEditMode/EditMode-20261008-190542-f7b93d987a79441c8b790f3474dd0aef.xml` |
| ALL EditMode log | `Raw/Followup-CityIdentity/AllEditMode/EditMode-20261008-190542-f7b93d987a79441c8b790f3474dd0aef.log.gz` |
| Official Smoke XML | `Raw/Followup-CityIdentity/OfficialSmoke/EditMode-20261008-190623-88db604c8e11403e8f79e4d006c51948.xml` |
| Official Smoke log | `Raw/Followup-CityIdentity/OfficialSmoke/EditMode-20261008-190623-88db604c8e11403e8f79e4d006c51948.log.gz` |

## Source identity

Source byte hashes are from the exact validated worktree. Git blob IDs identify the source objects in implementation commit `4d93a4a`.

| Source file | Worktree SHA-256 | Git blob ID |
|---|---|---|
| `Assets/_Project/Scripts/CityRuntime.cs` | `DBDACBF73A7DBDEE5B9A04249F7B18C09AAB2A79B88B6188774F6355D39068CF` | `54fdf537f5854280f01f6940ef179fd2aa336659` |
| `Assets/_Project/Scripts/P12DCityRootOwnerSnapshot.cs` | `6AF5E36174DCA799B948CEF84657C560B1C463868CCAC0FF0B51359B0584AAFE` | `cb6cf3b8d5d08d22182a53588ca806caf72aa03d` |
| `Assets/_Project/Tests/EditMode/Editor/P12DCityRootOwnerSnapshotTests.cs` | `A4B4476B4B84C7C9168E86DEFD6056CFBECA1E5486149DA151B72FA9D8812AA3` | `5fd3d00baebb07d92c1848af80b33e3dd3fd3b33` |

`git diff --check 1c7b906c172d9e47020996888db64bd2516b451a..4d93a4aa38ed66712c7c8b0951a61e0303d6c7af` passed. Earlier candidate validation remains recorded in `VALIDATION.md`; this follow-up is the current-code evidence for the identity-binding correction.
