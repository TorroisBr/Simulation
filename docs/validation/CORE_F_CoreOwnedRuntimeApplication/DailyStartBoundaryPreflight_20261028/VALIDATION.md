# Daily-start boundary preflight — validation

Date: 2026-10-28

## Scope

For a fully selected, admitted Daily-v1 runtime, Core now obtains and checks
the required day-start boundary before beginning the day logger or invoking
any enabled demographic writer. A missing or null boundary therefore rejects
before daily work begins.

Core determines the mixed-roster case from its exact ordered actor roster.
That case retains the established single Unity whole-day compatibility
fallback and does not require selected day-start operation ports. Non-admitted
compatibility behavior is unchanged.

The regression enables natural-mortality policy and configures the fake
writer to throw if called. With the required day-start context absent, the
stage reports the boundary failure first, records no logger or writer
callback, and publishes no demography result. The existing missing-context
identity remains unchanged; only its expected preflight callback order changed.

This is an implementation checkpoint only. It does not complete or promote
CORE-F, publish DailyV1Application.TryStart, remove Unity action/P12 adapters,
or claim detached-host continuation.

## Exact source identity

The validation worktree was based on:

- Candidate branch: codex/core/CORE-F-core-owned-runtime-application
- Parent commit: 3a8aa6c8178b1d9c41dc3d918958593b5e7f1237
- Parent tree: 3dad3fe12b5ff37c7390079b2a9db110a527d82e
- CoreValidation working-input digest: 2c4c6ae405901272ead358135a62ec49af6b1dfa0c16225f7f5a3dc21b5b320c
- Unity EditMode input digest: 36bdeca7afca375fdc26e8eb68b0bb0918c2ece25267350709e1920be41d111a

| Changed input | SHA-256 |
|---|---|
| Packages/com.simulation.core/Runtime/Application/DailyV1SelectedDailyExecutionStage.cs | 1cc44468588629f8eccac1f0f565c994f9c697977953a847c1b89b5babe5a7b3 |
| tests/Simulation.Core.Tests/DailyV1SelectedDailyExecutionStageTests.cs | 0b3be4262647199ea4e04ab4ae3511ad926ef69ecfe310a0da326205c189658b |

The official runner snapshots remained stable. These hashes match the CoreValidation source inventory. The Unity run compiled the same Core production file; it does not compile the separate .NET test project.

## Validation

- Official CoreValidation cold: 908/908 PASS.
- Official CoreValidation warm: 908/908 PASS.
- CoreValidation run: 817883ad7c014ab7b81cc4951f02f2c5.
- Private CoreValidation manifest SHA-256: 7ff5d01da134f450fd7bd3d31c5f92a62f06b6f55732a8d3de7dcbd3fdfb2727.
- Original cold TRX SHA-256: e2287a8a6d488a005800ced730e3a4fb5166a31c271f54a93297972ba3fe2b22.
- Original warm TRX SHA-256: eb7b1d80c21210977924e442b6b7ff471cad4e0842ff34e218481610f3f2ff97.
- Official full Unity EditMode: 3276/3276 PASS; zero failed, skipped, or inconclusive tests. The full report includes 7 LongRun cases.
- Unity full run: be3becc0e93547edb8e370cc44fcac94; stable input snapshot.
- Official filtered Smoke: 5/5 PASS, stable input snapshot; run 0978502cab9b4b4a9e5bea4f0c9ce656.
- The standalone Smoke XML remains in protected evidence; it is not combined into the full report.
- The normalized full EditMode package is in UnityEvidence below and passed the official package verifier against its protected source manifest.
- Package manifest SHA-256: d21f15f8b62e203bfbdf6773d93ed65852297dd4c6612853bcaca400ffda84f4.
- Package archive SHA-256: 3d4d50fa699677381dbace21613edc67452182463368aa24df16e7168e5c7b01.
- git diff --check: PASS.

Original Core TRX reports, Unity XML, raw logs, and private runner manifests remain unchanged in the owner-only evidence store. No public path or machine identity is recorded here. The CoreValidation worktree contained a Unity Library; this run makes no Library-independent .NET claim.

The unrelated ProjectSettings edits, untracked files, metadata, and private evidence were not staged or modified.