# CORE-F P12-E Core Justice owner capture — validation

## Candidate and scope

- Candidate: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate commit: `0893555a063ec8848264150ffd70c2a067006e0a`
- Candidate tree: `f4aa6da5236ea96d98cbd84598057a490c557278`
- Candidate implementation base: `a23ed4ad03e332bcfd147bc42a8ef459155d3ad4`
- Core canonical at validation: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Unity input digest: `a075f9b64bd795f418d1da85506bcff3fcb49430e08343085217923ca8184f01`

This checkpoint captures P12-E Justice records from the active Core continuation-save operation's exact `DailyV1JusticeRecordsOwner`. It preserves ordered wanted and sentence rows, exact NPC/City identities, reciprocal Person bindings when present, sentence-to-warrant ordinals, the Core owner's local revision, the completed-boundary token, and the required exact-zero Justice receipt witness. The Unity `JusticeSystem` remains a projection and identity witness: when a selected Core owner graph exists, it must be the runtime's exact installed projection over that same Core owner. The package compares its projected values with the Core snapshot before using the Core snapshot for detached staging and image encoding.

Calls that use the legacy P12-E staging boundary without a selected Core owner graph retain their existing Unity capture path. No P12 profile, owner schema, serialized section format, or accepted owner set changed. This is a bounded Core/runtime integration checkpoint; it does not complete P12-A or CORE-F.

The four changed source/test inputs match the exact stable Unity snapshot and the candidate worktree:

| File | SHA-256 |
|---|---|
| `Assets/_Project/Scripts/P12EDailyV1OwnerPackage.cs` | `b95c3bf27828e4e7af1b7496871cc13d4af40cdf158f9408a878816aec57470e` |
| `Assets/_Project/Scripts/P12EJusticeRecordsSnapshotAdapter.cs` | `5b84aaf254f2518b5dc549431f984996ca7c0781c2622d95dcfd89ada7993581` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `07b62a3fabb1b6c85d64b70018db7aff6b3f22a5718f0dc60ae08d0d70c6f7aa` |
| `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12ECoreOwnerSourceCapture.cs` | `5bd49f70edaa0121e53901f08d423901f0d15a9c51f1a675b0b128ee1b1e257b` |

## Validation

| Gate | Result | Run |
|---|---|---|
| CoreValidation cold | 936/936 PASS | `368e311bdf524915b49cdef3d853943f` |
| CoreValidation warm | 936/936 PASS | `368e311bdf524915b49cdef3d853943f` |
| P12-E Justice snapshot focus | 11/11 PASS | `0a118878958a4b2786c33c3f0fe002f9` |
| Full Unity EditMode | 3279/3279 PASS | `a072c6cac55e4502b65e7259c016b345` |
| Official Unity Smoke (`EditMode`, filter `Smoke`) | 5/5 PASS | `21d173b9b95d4b6dac720be8dfd445eb` |
| `git diff --check` | PASS | Candidate range `a23ed4ad..0893555a` |

The Unity runs used Unity `6000.3.9f1`. The P12-E focus, full EditMode, and official Smoke snapshots were stable; each recorded the same input digest above. The normalized full EditMode report contains the populated Justice capture regression and all five official Smoke identities, each with outcome `Passed`.

CoreValidation used .NET SDK `9.0.315`; the runner recorded the required cold and warm full test results. Its private manifest reports that a Unity `Library` directory existed in that worktree, so this run makes no Library-independence claim. The manifest SHA-256 is `ab416544afe5491543eebee2e382995a2cda7e26a99df55821b678f4dcadd756`.

## Evidence and integrity

The normalized full EditMode evidence is in [`UnityEvidence/`](UnityEvidence/):

- Manifest: `coref-p12e-justice-core-capture-editmode.manifest.json`, SHA-256 `0fca17f98daeb48f409598b43cdb9ad605b6e19ec0b18c589a9028ee8dd03e38`
- Archive: `coref-p12e-justice-core-capture-editmode.zip`, 174,129 bytes, SHA-256 `ef03ce87ac2b3aebdfebd6d4fa5148bccfc41e3440e40de72522a7858ec1fbd2`
- The approved packager's private original comparison and archive verification passed (`runs=1`, `entries=1`).

Private run-manifest SHA-256 values are: P12-E focus `5617ef95ad5260eda1fb09c0c1ef94092a001c0bf83d2a01898add8d8f5f94fb`, full EditMode `124baebf59a5ec2fe6142576e1d9d7449b773c920b9053f0acfb1340520046a4`, and Smoke `8ac8b78940618aa0f418def4390522fc324cdc53083330d45df0dc9d503ad906`. Original Unity XML and raw logs remain in protected local retention. The filtered Smoke report is not separately published because the packager rejects its inconsistent parent-suite aggregate; its five passing cases are present in the verified full EditMode package. No report was edited or manually normalized.

CoreValidation's original TRX reports, logs, runtime-selection traces, and manifest remain protected locally. No normalized .NET TRX package is claimed: the current closed DotNetTrx adapter profile does not accept the complete CoreValidation runner manifest. The CoreValidation runner itself performed no normalization.

## Status

The candidate branch is synchronized through `0893555a`. This checkpoint has not been promoted to Core canonical and makes no claim that CORE-F or a Phase is complete. Existing user `ProjectSettings` changes, untracked files, and private evidence were not staged.
