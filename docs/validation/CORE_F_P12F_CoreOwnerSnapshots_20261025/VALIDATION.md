# CORE-F P12-F Core owner snapshot migration validation

> **Supersession:** The first validation summary in this file is historical; the final exact-source rerun below governs the candidate. The earlier package is retained and explicitly marked superseded.

Status: **validated candidate work only**. This record does not promote Core canonical, close CORE-F, or change P12/P13 status.

## Scope

This candidate moves the selected Daily-v1 P12-F snapshot/census and scheduled-directive wire-codec responsibilities that can run without Unity into `Simulation.Core`. It keeps Unity-only cross-owner NPC/spatial relation checks in the forwarding/staging adapter and keeps the Unity-owned PoliticalKnowledge snapshot on the Unity side. ScheduledDirective staging now binds its persisted action ID to the exact selected `DailyV1EffectiveDefinitions` entry and requires the admitted EscapePrison action type. Owner revision, row order, terminal disposition, exact capture token/vector and existing image semantics are retained.

This is not a claim that all P12 owner composition is Core-owned, that `DailyV1Application` or the detached host is complete, or that CORE-F is accepted. It does not change P12 phase status or scope.

## Source and validation inputs

- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`.
- Validated base commit: `1706c17139c270ed60b9d34776b45f8e45ecbdc6`; base tree: `ff9108d9d508f560e654d24762b13aef43d24223`.
- CoreValidation recorded the changed source/test inputs as a stable working-input digest `537a6e9b937596bd609030ed8ffde4588c7ee114c6f1615d10c6bd801a5d2b0c`; its exact per-file inventory remains in protected local run `8c19dd5a6dc8463ba2374fd67a8e0c02`.
- Unity EditMode runs recorded 1,884 stable inputs with digest `6fa7522a3ee507f5040830639436bc90f3de2c2e5bb3c88e11bb1b73da3348d4`. The two pre-existing local ProjectSettings edits remain unstaged; the run manifests bind their exact input hashes as `EditorBuildSettings.asset` `58BCBFB23DA7AAB5ACD696E7D83E9D75A86442ED39A582159D2493049361BA28` and `ShaderGraphSettings.asset` `5C432C9C73FDF73FEE91C8823EA02AB98E3B7A8EDF341B5AE6901D699CCE52A7`.
- Unity: `6000.3.9f1`; Test Framework: `1.6.0`. Temporary files and original reports remained on E: in the runner's owner-only evidence roots.

## Results

| Gate | Result | Run identity and evidence |
|---|---:|---|
| CoreValidation cold | 756/756 passed; 0 failed, skipped or inconclusive | Run `8c19dd5a6dc8463ba2374fd67a8e0c02`; original TRX SHA-256 `950a17a437157e96900ce1ced5725c6808625fb8bb1ce14c5e376a9909a54814` |
| CoreValidation warm | 756/756 passed; 0 failed, skipped or inconclusive | Same run; original TRX SHA-256 `0d64608d499f949a11208b0c8c2dc2c84161250cee9353ceeea3732a481464d5` |
| Focused P12F snapshot EditMode | 4/4 passed; 0 failed, skipped or inconclusive | Run `242bc0b6e9724c829cdbadae84149617`; input digest above; original XML SHA-256 `3a792e6acc22e733d44c4152f47266ab40f35e29c814d6e7fdea4c25fde1dcca` |
| Full Unity EditMode | 3,263/3,263 passed; 0 failed, skipped or inconclusive | Run `d63103ccd54b477bb4e6131acf1bf280`; input digest above; original XML SHA-256 `d23343bb0c581fb951c4cc825457e2c805812859c377bacf92b60294a8cc95ff` (3,264,437 bytes); normalized package below |
| Official Smoke filter | 5/5 passed; 0 failed, skipped or inconclusive | Run `90cb0c02cc5b4e15ac87cfb696f251d7`; same Unity input digest; original filtered XML SHA-256 `3b7e897c4da68c1e343d15ff33cf4d7e372444b143f13bcb121cf09ff3ffb324` |
| `git diff --check` | PASS | Exact candidate worktree |

The five official Smoke identities each occur once with outcome `Passed` in the full EditMode report and in the Smoke-filter report: `GMConsolePlayModeSmokeTests.GmConsoleCanPreviewApplyAndRefreshObserver`, `GMConsolePlayModeSmokeTests.GmConsoleNaturalLanguageCanTranslatePreviewApplyAndRefreshObserver`, `WorldObserverPlayModeSmokeTests.ObserverSceneCanRenderAndSelectNode`, `WorldObserverReadModelTests.ObserverPrefabSmokeInitializesWithoutMissingReference`, and `WorldObserverVisualTests.ObserverPrefabSmokeHasNoMissingSerializedReferences`. A separate `TestCategory=Smoke` diagnostic selection returned zero cases; it is not counted as a validation gate. The documented `-TestFilter Smoke` run above is the accepted Smoke result.

## Retained public Unity package

- Normalized full EditMode manifest: [coref-p12f-owner-snapshots-editmode-full.manifest.json](coref-p12f-owner-snapshots-editmode-full.manifest.json), SHA-256 `C756F13B32BC7CB505F6F0409DDECB78D8D817ABBFD21B6247690733CCD12A7B`.
- Deterministic result archive: [coref-p12f-owner-snapshots-editmode-full.zip](coref-p12f-owner-snapshots-editmode-full.zip), SHA-256 `1650EC9DE65678796E5020F8E5C872A49CDF9886B90B04B0F2FD397ACA4EB446`, 173,330 bytes.
- The approved verifier passed against the protected original run manifest. The archive contains one normalized NUnit XML entry with all 3,263 case results. An independent scan found no workstation path in the archive or public manifest.
- Original Unity XML and raw logs, including the filtered Smoke output, remain unchanged in protected local evidence; raw logs are not published.

## CoreValidation evidence packaging limit

The protected CoreValidation run manifest and cold/warm TRX reports remain unchanged in the owner-only E: evidence store. Directly passing `simulation-core-validation/v1` to the DotNetTrx packager was rejected because that adapter accepts the separate `dotnet-trx-source-context-v1` input schema. No runner manifest or TRX was edited, projected, or published, and no public .NET package is claimed by this record. Resolve the approved source-context packaging handoff before any Core canonical promotion that requires a permanent .NET result package.

## Review and remaining boundary

Independent exact-tip implementation review is recorded separately after this candidate commit. P12F cross-owner relationship checks remain in Unity staging where their selected NPC/spatial authorities live. CORE-F remains incomplete; shared application/session ownership, full runtime composition, detached-host continuation and the other accepted CORE-F closure gates remain outstanding.

## Final exact-source rerun after whitespace cleanup

The original run/package details above are retained as history, but they were captured before removing two trailing blank lines flagged in newly moved codec files. This addendum is the final validation authority for the candidate's code/test bytes. The cleanup changed no executable meaning; validation was nevertheless repeated on the resulting exact input.

### Final input identity

- Base commit/tree: `1706c17139c270ed60b9d34776b45f8e45ecbdc6` / `ff9108d9d508f560e654d24762b13aef43d24223`.
- Final CoreValidation working-input digest: `4c663acf833c85408a031d11f3e44af0869d16aec48e8c6251208ca6e23ab276`, stable; run `2c7b78f4a5a3427e94f109a200f14faf`.
- Final Unity input inventory: 1,884 files, digest `9a36bf0e091e60ea10d9c08b2e406e18a54f300321afa6835748ff969a4cde4e`, stable in both full and Smoke-filter runs.
- The pre-existing local ProjectSettings edits remained unstaged; the run manifests bind their exact input hashes as `EditorBuildSettings.asset` `58BCBFB23DA7AAB5ACD696E7D83E9D75A86442ED39A582159D2493049361BA28` and `ShaderGraphSettings.asset` `5C432C9C73FDF73FEE91C8823EA02AB98E3B7A8EDF341B5AE6901D699CCE52A7`.
- CoreValidation recorded `LibraryAbsent=False`; this is not evidence of a Unity-Library-free .NET host.

### Final results

| Gate | Final result | Run identity and original evidence |
|---|---:|---|
| CoreValidation cold | 756/756 passed; zero failures, skips, inconclusive, errors, timeouts or aborts | `2c7b78f4a5a3427e94f109a200f14faf`; original TRX SHA-256 `3f9c750d362b55e196907806de1488225f1420e59b79521da9bc1931db7cac29`, 1,069,622 bytes |
| CoreValidation warm | 756/756 passed; zero failures, skips, inconclusive, errors, timeouts or aborts | Same run; original TRX SHA-256 `73b63cb8bdce63aa4d96457df98d0c955aa0be9ff75179aaad5c0e9105b718e4`, 1,069,622 bytes |
| Full Unity EditMode | 3,263/3,263 passed; zero failed, skipped or inconclusive | `b59f4949c07645078ef182e79395d785`; original XML SHA-256 `678f72138b6fbdc079cb740a92db7e301bc739a6df151513884c53b80a1bb6f2`, 3,264,377 bytes |
| Official Smoke (`-TestFilter Smoke`) | 5/5 passed; zero failed, skipped or inconclusive | `3d0013c7937b404a8764f99bc5301095`; original filtered XML SHA-256 `3743bdd3b45edc91c54c1cfa31eedaff7d13c84f831353319121094adec4ebf3`, 6,354 bytes |
| `git diff --check` and staged diff check | PASS | Final exact candidate sources |

The full EditMode and filtered Smoke manifests share the same final Unity input digest. All five official Smoke identities listed above occur exactly once as Passed in the complete full-suite report and in the filtered report.

### Final public Unity package

The normalized full EditMode package was regenerated from final run `b59f4949c07645078ef182e79395d785` and independently verified against its protected original:

- Manifest: [Final/coref-p12f-owner-snapshots-editmode-final.manifest.json](Final/coref-p12f-owner-snapshots-editmode-final.manifest.json), SHA-256 `504FBD26FA9E9EA3BE4B54A42803BE0F04DDCD800BB972475234575B4E8749F1`, 446,329 bytes.
- Archive: [Final/coref-p12f-owner-snapshots-editmode-final.zip](Final/coref-p12f-owner-snapshots-editmode-final.zip), SHA-256 `DFFE7D206DD2AE5F951E53A1872B6E1149E6E67A038B09CFBA10E7C8F8D71492`, 173,222 bytes.
- The archive contains 3,263 complete normalized case results, including all five Smoke cases. The archive and public manifest contain no detected workstation filesystem paths. Raw logs and original XML remain protected and unchanged.
- The earlier normalized archive in this directory is preserved but superseded because its source-input digest predates the two trailing-blank-line cleanup.

### Operational and packaging disposition

A first post-cleanup CoreValidation invocation (`0902e8211b29419891f14c00289284cc`) was preserved as a failed operational attempt: the runner was passed an E: ProjectRoot while its inherited current directory remained C:, so relative `restore-lab-locked` resolution failed with MSB1009 before any test gate. The runner did not retry. The successful run above was launched from the E: repository root, matching the documented invocation.

The CoreValidation originals remain in private owner-only E: retention. The DotNetTrx packager was not given a fabricated context: passing the `simulation-core-validation/v1` manifest directly is rejected because the approved adapter requires `dotnet-trx-source-context-v1`. No normalized .NET package is claimed. Resolve that established source-context packaging step before any Core canonical promotion requiring permanent .NET evidence; the private cold/warm results remain available for this candidate review.

Independent exact-tip review follows the candidate commit. The implementation remains a bounded source migration only; the rest of CORE-F runtime/session, application, restoration-host and continuation acceptance remains open.