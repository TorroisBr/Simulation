# Validation — initial restored-session publication gate

**Status:** recoverable candidate checkpoint; Unity validation is incomplete. No Core canonical promotion is claimed.

| Identity | Value |
| --- | --- |
| Code candidate | `c906c2eb7983b89a0f838236fdaa0d5a0e8b46b7` |
| Code tree | `642e7dd221a2cb83a947c247d0f13a6195b8f59f` |
| Base | `d68940ebf7f2f3eb2679cb5ef5b7d83bfa4a6f01` |
| Core canonical | `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea` |
| Candidate branch | `codex/core/CORE-F-try-start-transaction` |

The implementation closes four tokenless initial-publication routes for restored sessions: hostless generic restore commit, generic host-projection restore commit, `TryPublishInitial`, and `TryPublishInitialWithHostProjection`. The exact initial-start admission/image method remains the only initial restored-session publication path. Non-null-session replacement remains supported. Regression coverage exercises rejection without publication or projection registration, foreign application/image rejection, replacement, and the valid exact-token restore path.

## Validation

| Gate | Result | Run identity / evidence |
| --- | --- | --- |
| Official CoreValidation | **PASS — 1007/1007 cold and 1007/1007 warm**; build, demo, and Lab checks passed | Runner run `ac2df8aac636499cbe39547de1f50a33`; stable input digest `39b97bb241217a712b8dd487789bddcc887113bbfafc5603cf33fe677ff2d5ca`; private manifest SHA-256 `70199f0d44ee3d7861b676588356cd4dc4cb1ad26bc84c0398d11135daa0ce29`; cold TRX SHA-256 `e5ecdb5d3f4a60b0393ab662e42790da0a3a4e8063498aebedda343a04621244`; warm TRX SHA-256 `c332164df9d946e81953aa1db056e9e20b1c331e72cfc31b25f4beee3932bf0f` |
| Focused Unity EditMode restore test | **No result; validation blocked operationally** | Unity `6000.3.9f1`; filter `P12GPoliticalBindingRestoreTests.IntegratedMilitaryAndSiteKnowledgeOwnersPersistRestoreAndContinueWithExactParity`; runner run `5b3e7bc882074c3d9004cef6b76e8869`; no XML was produced. Licensing entitlement initialized. The log stopped advancing at Bee `ScriptCompilation` for several minutes. After confirming no progress, the orchestrator stopped only the Unity process started for this run. The wrapper then failed while trying to read the still-locked original log. Private retained-log SHA-256: `c39a1e42dda6b13de3a3c160865af1a9298298abedda627304638257c4f9a0c8`. This is not a source failure or a test pass. |
| Full Unity EditMode / Smoke on this code tip | **Not passed / not available** | A prior full-suite attempt used an earlier, different source snapshot and timed out without XML; it is not reused for this candidate. |
| `git diff --check` | **PASS** | Passed before the source/test commit. |

The CoreValidation run used SDK `9.0.315` in the candidate worktree with its Unity `Library` present (`worktreeLibraryAbsent=false`). Its private input snapshot included the worktree's pre-existing ProjectSettings edits as local inputs; those files were not staged or changed by this candidate. The runner recorded the application and .NET test source hashes matching the committed bytes. Current committed file identities are:

| Source | Worktree SHA-256 | Git blob |
| --- | --- | --- |
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | `5bf36ce582dc381ab26939d7a2f46da9fca88d5210cf6086824b5033d0529c1e` | `d46b9a59ebf9676555c8caff6f93437553325fb6` |
| `Assets/_Project/Tests/EditMode/Editor/P12GOwnerEntryQualificationTests.cs` | `bd8f0a27c72eb57a5b9c2bc59313e0afdedd2b70730c7189bd64866ec3cb44c4` | `247ba4bc46dd7ddfc6a93b33a490e347ea89b0ec` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs` | `b2ef42b6b9e1cb16bbc2f887ca9eaa3a167c13c50ae60be935d4f823b0e64543` | `419a0103dc25fcbef18d2e78ba874b02ab0cd532` |

Original Core TRX and Unity log remain in protected local evidence storage. No raw result, private path, or machine log is published here. No public normalized .NET evidence package is claimed. Because current-tip Unity results are absent, this checkpoint is not ready for CORE-F acceptance or Core canonical promotion.
