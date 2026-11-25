# Selected Core Steal Dispatch Validation

This bounded candidate slice reconstructs the Unity compatibility projection for an exact Core-owned Steal action slot. It does not complete CORE-F or promote Core canonical.

## Contract exercised

- Core owns the selected action definition and exact Steal payload. The payload names the admitted target NPC RuntimeId, amount, origin decision, and stable occurrence.
- If the Unity action projection is absent, dispatch reconstructs it only from that exact Core slot and the exact admitted target owner/runtime binding.
- Missing and duplicate target runtime projections reject through the full Core selected-action execution stage before RNG or authoritative mutation.
- Reconstructing the compatibility projection does not change the action-slot payload/revision, money accounts, Justice owner, or P12 mutation epoch.
- Core executes the Steal. Execution uses the exact deterministic stream key, transfers the admitted amount, records the theft against bound Person identities, commits Justice state, and publishes the admitted P12 epoch. The final census succeeds after the operation scope closes.
- The independent review found that the prior test's zero-draw assertion used an RNG not passed to the dispatcher. The current test passes a counting RNG into `TryExecuteInstalledAction`, requires `InvalidOperationException` for both missing and duplicate projections, then verifies zero draws, unchanged owner revisions, and unchanged P12 epoch.

## Exact source identity

- Repository: `https://github.com/TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Validation base HEAD: `7559de3026580278798b365a8b1c4d90b8eb8605`
- Base tree: `a1c137f896f1435ec3ef1ccec93cedc976fb6e9e`
- Unity: `6000.3.9f1`
- Stable input snapshot SHA-256: `a4b24191c1c6b7bfcb98253843ee246071504e7a561f572c38383a8deca90b9d`
- `Assets/_Project/Scripts/SimulationRuntime.cs` SHA-256: `714230054e118482d0a752e36fddbdc6b943cd33ca7872e5b485f779cc2daae1`
- `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` SHA-256: `3a09f065fad0ec83eba90f2e3d72c03d19a362dbc0df757f678eb955cf709c21`
- Each accepted runner manifest reports `inputStable: true`; the source hashes match the full-run input snapshot.

## Accepted validation results

All accepted runs used the same stable input snapshot above. Original reports and logs remain preserved under their run IDs in the protected local evidence store. The full EditMode XML is included in the sanitized package below.

| Gate | Result | Run ID | XML SHA-256 | Raw log SHA-256 |
| --- | ---: | --- | --- | --- |
| Focused `SelectedDailyV1StealDispatchRebuildsProjectionFromExactCoreSlot` | 1/1 | `88185faa5e2f4b0ab4e58544d1f3bb93` | `05236cddf8de461dc04351569b791e277edb53363ceaddddac4d47ac1e599c1b` | `12049227cbf5ffa5e0726cc9b78c7ae0571aeba6294ec735924c6f12eaeb471b` |
| Full EditMode | 3289/3289 | `0963dcb4ab304fcab9ff6577ae7ac8c8` | `8af1332b78327a47b25f93d652e30020be8deced9d31bbf06dac60b30a6ed6be` | `9e8d748322944f967b0957a1df72ff655d3fd722567cd7678249f098cb0b969f` |
| Official EditMode `Smoke` filter | 5/5 | `e316844571a648faa88e5129b01d6c49` | `fa083e6b5c8e78bfbac7dedbd44b14d1fbcc273df0873989e1b8c353665e63ef` | `2e3dea63c587056cd8d8a48f0d879e8d8b87dc0dce0de45748f6e24171ae3566` |

The full EditMode XML contains the focused Steal case as Passed and includes all five Smoke cases. Filtered Focused and Smoke XMLs remain unmodified in the protected local evidence store.

## Current packaged evidence

The official packager creates a normalized full-suite XML-only archive; it removes output elements and redacts absolute local assembly/suite paths. The archive was verified against the full-run private manifest. A package attempt containing filtered Focused and Smoke reports was rejected because Unity's filtered XML retains a parent-suite `testcasecount` inconsistent with its filtered descendants. No XML was edited; filtered reports are preserved privately and are not represented as package contents.

- Package manifest: `UnityEvidence/coref-steal-dispatch-editmode-reviewfix-20261101.manifest.json`
- Package manifest SHA-256: `c28527b9407df1dd3984aca931810b1985f52ab7cfdcbe53afafd1833f7e6d53`
- Archive: `UnityEvidence/coref-steal-dispatch-editmode-reviewfix-20261101.zip`
- Archive SHA-256: `ccfe11690a0bb2290084fe357e6e48413213593d1be98dafa02dc530a61bac52`
- Archive entry policy: normalized full-suite test-result XML only
- Official package verification: PASS, one run and one entry
- Local Windows home-path scan of archive contents: no matches
- `git diff --check`: PASS

The earlier `coref-steal-dispatch-editmode-full-20261101` package is retained as historical evidence for the reviewed-but-superseded test source. Its code behavior was validated, but it does not satisfy the later independent review finding and is not the package for this source identity.

## Diagnostic attempts and dispositions

The following earlier attempts are not accepted validation results and remain preserved in the protected local evidence store:

| Run ID | Result | Disposition |
| --- | --- | --- |
| `05ef77f9126e4a25b8a4e35fff946c9f` | Focused 0/1 | The fixture still had its matching actor action projection, so the dispatcher used the existing-projection path. The fixture now clears only the Unity projection after establishing the initial census baseline. |
| `2b12cd373be84e60921d887550c00d35` | Focused 0/1 | Calling the runtime setter to clear the Unity projection also committed a Core action-slot clear. The fixture now clears only the two Unity projection fields and asserts that the exact Core payload remains installed. |
| `3e869f6358584a728f61707b17b29f7e` | NoResultXml | Compilation rejected null generic inference in the test reflection helper. The fixture now supplies the exact field generic types; subsequent focused and full runs pass. |
| `6509753d52254b1f8f77bbec7e4ac279` | PlayMode XML, 0 cases | This class filter selected no PlayMode cases because these smoke fixtures reside in the Editor test assembly. It is not counted as Smoke. The repository's official EditMode `Smoke` filter passed 5/5. |

## Review status

The first exact-tip review of commit `7559de3026580278798b365a8b1c4d90b8eb8605` returned NEEDS_CHANGES for the no-draw evidence gap described above; it found no production defect. The test was corrected, revalidated, and re-reviewed at exact commit `a8a65d3dff1cf02713a5fbfea92171ad7f3998cf` (tree `97a3a7a78ae76847e981a2a01f2c831523f136d9`). The independent review passed with no actionable findings and is recorded in [`the exact-tip review`](../../../reviews/CORE_F_SelectedCoreStealDispatch_REVIEW_A8A65D3D.md). This remains a candidate checkpoint only; it does not promote Core canonical.