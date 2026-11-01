# Selected Core Steal Dispatch Validation

This is a bounded candidate checkpoint for rebuilding the Unity compatibility projection of an exact Core-owned Steal action slot. It does not mark CORE-F complete or promote Core canonical.

## Contract exercised

- Core owns the selected action definition and exact Steal payload. The payload names the admitted target NPC RuntimeId, amount, origin decision, and stable occurrence.
- If the Unity action projection is absent, dispatch may reconstruct it only from that exact Core slot and exact admitted target owner/runtime binding.
- Missing or duplicate target runtime projections reject before RNG or authoritative owner mutation.
- Reconstructing the compatibility projection does not change the action-slot payload/revision, money accounts, Justice owner, or P12 mutation epoch.
- Core executes the Steal. The execution uses the exact deterministic stream key, transfers the admitted amount, records the theft against bound Person identities, commits Justice state, and publishes the admitted P12 epoch. The final census succeeds after the operation scope closes.
- No assertion or production validation was weakened to make the fixture pass.

## Exact source identity

- Repository: `https://github.com/TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Validation base HEAD: `1336577d40d246a07caf3786a559dd053efc6e11`
- Base tree: `8da21699f8e8626e7a76a3a39284232d864d3c4f`
- Unity: `6000.3.9f1`
- Stable input snapshot SHA-256: `1fe06ccb56f3a1d15c10a308cae0fea38d6236da191919b17f6b23744f9b954f`
- `Assets/_Project/Scripts/SimulationRuntime.cs` SHA-256: `714230054e118482d0a752e36fddbdc6b943cd33ca7872e5b485f779cc2daae1`
- `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` SHA-256: `cab4029567b9768b1adf13cd78227192e754a207709224cedf1fda0cf0716b27`
- Each accepted runner manifest reports `inputStable: true`; the current file hashes match the full-run input snapshot.

## Accepted validation results

All three accepted runs used the same stable input snapshot above. The private original reports and logs remain preserved under their run IDs in the protected local evidence store; the full EditMode XML is also included in the sanitized package below.

| Gate | Result | Run ID | XML SHA-256 | Raw log SHA-256 |
| --- | ---: | --- | --- | --- |
| Focused `SelectedDailyV1StealDispatchRebuildsProjectionFromExactCoreSlot` | 1/1 | `9b74ed26f8374e31821f6533ef7a4934` | `c3bb65f6f654444f4aeb65ddfd563241f5a1566227f2885ccd3b38c5d7fd09de` | `b25eea67c8498923eae5b9131bdbe5c499f5be0eaf3c5dcc61cf91e7df175ead` |
| Full EditMode | 3289/3289 | `b3ef8789dc6c400cade77ac3bd2e54cf` | `7b16f967363aebc96f7e5212fd4feba2b611826d1b4bf40891e781002586d139` | `d0e9d1a818f61f30ed9687e7c278fc8c0cedb92ff07d1ad46daaf5d4d3d60ce9` |
| Official EditMode `Smoke` filter | 5/5 | `c1c9ccbf2f6c44f382582b92b5dd0f18` | `676030b7f44ecb934670d6f1bbaba9926a5c69df25281ff4bdb7aa6b635448a9` | `e0c7852400c021d4a765eed19746545f4975847599a1e6d962500c17e7748ddd` |

The full EditMode report includes the focused Steal case as Passed and includes the Smoke cases. The separately filtered Focused and Smoke XMLs remain unmodified in the protected local evidence store.

## Packaged evidence

The official evidence packager creates a normalized full-suite XML-only archive; it removes output elements and redacts absolute local assembly/suite paths. The archive was independently verified against the full-run private manifest. A package attempt containing the filtered Focused and Smoke reports was rejected because Unity's filtered XML retains a parent-suite `testcasecount` inconsistent with its filtered descendants. No XML was edited. The filtered reports are preserved privately and are not represented as package contents.

- Package manifest: `UnityEvidence/coref-steal-dispatch-editmode-full-20261101.manifest.json`
- Archive: `UnityEvidence/coref-steal-dispatch-editmode-full-20261101.zip`
- Archive SHA-256: `2e449d404df9f8f10f3359cddb131b852e9dfa968b7a5ad3603f23c6f102010f`
- Package manifest SHA-256: `3039e9e3e6178e2d2e6466ec7e8c0d2d0b5a9b37e3bf508bac84f304ce1c5eb1`
- Archive entry policy: normalized full-suite test-result XML only
- Official package verification: PASS, one run and one entry
- Local Windows home-path scan of archive contents: no matches
- `git diff --check`: PASS

## Diagnostic attempts and dispositions

These earlier attempts are not accepted validation results and remain preserved in the protected local evidence store:

| Run ID | Result | Disposition |
| --- | --- | --- |
| `05ef77f9126e4a25b8a4e35fff946c9f` | Focused 0/1 | The fixture still had its matching actor action projection, so the dispatcher used the existing-projection path. The fixture now clears only the Unity projection after establishing the initial census baseline. |
| `2b12cd373be84e60921d887550c00d35` | Focused 0/1 | Calling the runtime setter to clear the Unity projection also committed a Core action-slot clear. The fixture now clears only the two Unity projection fields and asserts that the exact Core payload remains installed. |
| `3e869f6358584a728f61707b17b29f7e` | NoResultXml | Compilation rejected null generic inference in the test reflection helper. The fixture now supplies the exact field generic types; subsequent focused and full runs pass. |
| `6509753d52254b1f8f77bbec7e4ac279` | PlayMode XML, 0 cases | The documented class filter selected no PlayMode cases because these smoke fixtures reside in the Editor test assembly. This run is not counted as Smoke. The repository's accepted EditMode `Smoke` filter subsequently passed 5/5. |

## Review status

This validation record is bound to the exact candidate source hashes above. Independent exact-tip code review and durable candidate publication are pending; this evidence alone does not accept or promote the checkpoint.