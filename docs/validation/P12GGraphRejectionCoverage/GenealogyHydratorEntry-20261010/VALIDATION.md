# P12-G Genealogy hydrator-entry failure-atomicity cutpoint — validation

**Scope:** add a restore-only observer cutpoint immediately before `GenealogyStore.TryCreateFromOwnerSnapshot`, plus one case in the existing private-stage fault-injection harness. The new case confirms rejection before publication and reuses the existing assertions for source session/reference, completed-boundary token, owner-thread health, selected facts, continuation roots, complete included-owner projection, deterministic next-boundary parity, and successful retry. This covers the Genealogy hydrator entry only; it does not cover every internal false/throw path or every B–F hydrator.

**Base P12 canonical:** `95e922eb3c2083a87bf1353bce60303f42a2a62c`  
**Code candidate:** `732864564bbf2fa8bd7373cac1994876280e876d`  
**Code tree:** `8754f563b43e0a3b3994b1acc65ccfca2846fb89`  
**Assets tree:** `161d5e89f178a43672804f4f5b28739e51ceaee2`  
**Changed code:** `P12DDailyV1OwnerPackage.cs`, `P12GDailyV1RestoreCoordinator.cs`, and `SimulationRuntimeAdmissionTests.cs`.  
**Unity Editor:** `6000.3.9f1`; runner: `Tools/UnityValidation/Invoke-UnityValidation.ps1`.

| Gate | Result | XML artifact | XML SHA-256 | Compressed log | Compressed SHA-256 | Uncompressed log SHA-256 |
|---|---:|---|---|---|---|---|
| Focused `SimulationRuntimeAdmissionTests` | 175/175 PASS; injected private-stage cases 55/55 | `Focused/EditMode-20261010-202635-f2c128780fd744e48c50bfba60359d4f.xml` | `3B94887FB12A08425FA9ECD99F41A146D2C5485DBAA4B14DA7CFC27D80C0D13E` | `Focused/EditMode-20261010-202635-f2c128780fd744e48c50bfba60359d4f.log.gz` | `26EE380642AF47DC84D558A23D68054F891FACF306ACD2039F4FAA4F1EB4DEDC` | `7ECCBF6337B066DC63203B3D52C91306E00669FFBDCECDDFC5AA9B867621ECD5` |
| ALL EditMode | 2849/2849 PASS | `AllEditMode/EditMode-20261010-202923-7456a4e250e54f61b7208abf820ad804.xml` | `5B67EF4DF5DD48724FB096D158420ABFC9A2126FD7CA877341410AD11138D235` | `AllEditMode/EditMode-20261010-202923-7456a4e250e54f61b7208abf820ad804.log.gz` | `F8DD673C71C844B8277087F54A5C8DA493DF767FA0B4972D37D3C09D66A818C6` | `386F2CE99892BBEFB8E0CBBADCC2517E11093E73C4AAD73B33B6E343C2906F29` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 PASS | `OfficialSmoke/EditMode-20261010-203017-d554c763a5994cabba129cce30fc8f6b.xml` | `C74B4738E9A8563AB0A9CF12A808C400F8F6792E1360D65C0ACAC208146C6EDA` | `OfficialSmoke/EditMode-20261010-203017-d554c763a5994cabba129cce30fc8f6b.log.gz` | `329A4730C91DEB07F322B9FD79304140E9BFB741FFE527638DBDB7AA1A3825F1` | `A276069866A9008CCA6BBB00A88C67AE54A2D6CF203CF84109819A9D31568FD3` |
| `git diff --check` | PASS | — | — | — | — | — |

All XML summaries were checked for zero failed, skipped, or inconclusive tests. Each compressed log was decompressed and its SHA-256 matched the original runner log before the original temporary log was removed. Validation is bound to the code candidate and tree above. The Unity-generated `ProjectSettings` working changes and untracked `.meta` files were excluded from the candidate and left untouched.
