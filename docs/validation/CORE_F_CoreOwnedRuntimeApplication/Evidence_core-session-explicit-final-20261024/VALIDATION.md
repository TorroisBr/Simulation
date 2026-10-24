# Explicit Core active-session projection validation

## Scope and source identity

This is a bounded candidate checkpoint on `codex/core/CORE-F-core-owned-runtime-application`, based on candidate `e48d43d0693e7df537f396ee31e85495cdefbd36`. It changes the selected Unity projection so it cannot implicitly synthesize a `DailyV1ActiveRuntimeSession`; restore composition now supplies the session explicitly. This does not establish that `DailyV1Application` owns restore composition or close the shared application/host contract.

The official Unity input snapshot digest is `975bd0b0b40eb66d06b0763e26a9bc9d14132730dda4ed294035b6f3d2e0750a`. The runner captured 1,858 source/configuration inputs. Every captured path and SHA-256 was compared with the candidate worktree; all matched. The validation checkout's Git HEAD was `49d823ef81178107795939bdf317a8393ef50c49`; the input digest and complete file comparison bind the run to the candidate contents despite that checkout HEAD.

Changed candidate source hashes:

| File | SHA-256 |
|---|---|
| `Assets/_Project/Scripts/TesteSimulacao.cs` | `7ff35b05079e2594378a98cd92a0d98a6a63b5f218452326082fbb8b9fd41e04` |
| `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.cs` | `dd7460ab4416018a2ba0e6e5861c62c8c619cfbf8a0b75b26d150403b8c385bc` |
| `Assets/_Project/Tests/EditMode/Editor/P12GPoliticalBindingRestoreTests.cs` | `8da7879d543734b5fb6d57371c65ddc1f0e27668252af28ec7e84947281482da` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs` | `41cffdbae74ad72904dd64d5bb0b91584a1536ab29f2c4dba7fe913171eb94c0` |

## Results

| Gate | Result | Run |
|---|---|---|
| Explicit-session bootstrap composition test | PASS, 1/1 | `b16880b4485b4756974a96b3ccba4ccc` |
| P12-G political-owner restore and continuation test | PASS, 1/1 | `5c51199d9d0841b7941754c8da9c56eb` |
| Full Unity EditMode | PASS, 3259/3259; 0 failed, skipped, or inconclusive | `4489b87b2c27403ebbd20ea8ea9ea672` |
| Official filtered Smoke | PASS, 5/5 | `6ef1a9999b7d407d91407815912b2118` |
| Unity evidence package private-context verification | PASS | `runs=1`, `entries=1` |
| `git diff --check` | PASS | Candidate worktree |

The five Smoke test identities selected by the official `Smoke` filter are all present in the full EditMode report. The public normalized XML package contains the complete full-suite report; filtered XML reports are retained only in the private evidence store because their parent NUnit suite counts describe the full assembly and are not independently packageable as complete-suite reports.

Unity version: `6000.3.9f1`. The full-run input was stable before and after execution. Its original XML SHA-256 is `a820b82753d9b2bde2f59dcd100b82a67ed883a4b198dee0c94522f79654bec6`.

The official public package is [the manifest](coref-session-explicit-final-20261024.manifest.json) plus [the normalized XML archive](coref-session-explicit-final-20261024.zip). Manifest SHA-256: `ed06ddf2f750b50b654edbecbd31bc240767be5ba325f025a1685608a42c1818`. Archive SHA-256: `e2284e6aa1372958330b408ed002a822962c35ae90cee9deef8cf6d7efc56ef1`. The official verifier compared the package to the owner-only original run manifest and report. Original XML and logs remain in the protected E: validation store and are not included in this repository package.

The private filtered runs and full-run manifest/log are retained for audit. An earlier test-filter typo selected zero cases and is not counted as evidence. A preliminary unqualified exception type caused a compile failure before the final source was corrected; that run is also not counted. The final focused, full-suite, and Smoke runs above all passed on the corrected source snapshot.

## Limits

This checkpoint only enforces explicit Core active-session handoff at the Unity projection constructor and makes restore composition provide the candidate session explicitly. It does not establish the final Core-owned `DailyV1Application.TryStart` restore lifecycle, full shared P12 restore, descriptor-v2 integration, detached .NET host, separate-process T/T+1/T+2 continuation, or CORE-F completion. No Core canonical promotion is claimed.
