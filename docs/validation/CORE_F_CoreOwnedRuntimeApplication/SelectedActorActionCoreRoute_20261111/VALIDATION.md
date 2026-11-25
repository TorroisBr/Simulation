# CORE-F selected actor-action routing

Status: validated candidate checkpoint. CORE-F remains incomplete and Core canonical is unchanged.

## Source identity and scope

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`.
- Implementation commit: `c8b8420816daa2c091ea2b4259e215de4704d535`.
- Implementation tree: `8f56a4ba1ac9decede2b6cfea50c855e90650283`.
- Base: `aef01355dc72ecbf409d19639c80d0ddae715f37`.
- Core canonical at validation: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Scope: the existing selected Unity actor-turn adapter now advertises the Core-owned selected action execution contract. Exact selected actors use Core's existing configured-action resolution, deterministic choice, decision identity, P12 action-slot admission, dispatch, outcome, and status stages. Unity continues to provide the existing action-object projection and effect/admission ports. `EvaluateAndExecuteAction` remains the fallback outside this selected Core route; no action provider or P12 identity contract changed.
- The existing roster mapping, stage-level whole-Unity fallback selection, and P12 census implementation are unchanged. Unselected runtime members retain their prior compatibility classification and are not assigned Core owner identities by this change.

## Validation

| Gate | Result | Evidence |
|---|---:|---|
| Focused selected actor-turn host boundary | 1/1 PASS | Official EditMode run `0043a767972549ac894dc8d9f8a6cac8`; input stable; original XML SHA-256 `59a48a683da5e834145686ba5807d914bf555c36f8a30dd0e8e82c8019b58890`. |
| Full Unity EditMode | 3,306/3,306 PASS | Official run `214d3808748c42d0b73bf64d03950ab5`; input stable; original XML SHA-256 `f780c8876e0185c0782e094c6db313930c806d85c078cf2ed51cab25a9eb9ddf`. This full result includes all seven `SimulationRuntimeLongRunTests` cases. |
| Official Smoke | 5/5 PASS | Official EditMode filter `Smoke`, run `99f2f1c210bf4ffcab446ce73d789775`; input stable; original XML SHA-256 `2d4f840efe62719c5c996774b6d8bc296685c5f3f4a872b9a8e7252990bb7318`. |
| `git diff --check` | PASS | Implementation diff and staged commit. |

All three official runs recorded input digest `419f633a71b66c52b8beaee4856ca28308fe02cab6e346949fdc83b907cd8334`. Validation ran before the two-file implementation was committed, at Git HEAD `aef01355dc72ecbf409d19639c80d0ddae715f37`; the source/test file hashes below were captured by the runner and independently match the committed candidate files. No implementation or test edits followed validation. The worktree's unrelated local modifications were not staged.

The focused and filtered Smoke source reports and raw logs remain in protected local evidence storage. The full EditMode report, which includes the focused and Smoke identities, was normalized by the approved packager. The original XML and logs were preserved unchanged.

## Public evidence package

- Normalized package: `UnityFull/coref-selected-action-routing-editmode-20261111.zip`.
- Package manifest: `UnityFull/coref-selected-action-routing-editmode-20261111.manifest.json`.
- Package-manifest SHA-256: `e1a265eb8a9c75dc7b6c3084b936ec32745fdc9e00f8af2b42a369ac93623373`.
- Archive SHA-256: `848034b6088c408d0ed2721fb6ec4da8a7364831debe68011f6ea98c4a8a147e` (174,892 bytes).
- Official package verification against the protected run manifest: PASS; one normalized full EditMode report.
- The archive contains normalized result XML only. Raw logs and private workstation paths are not included.

## Source hashes captured and rechecked after commit

| File | SHA-256 |
|---|---|
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `b8c675703329e1db7809fba1df8e0ccb00a267ab2cdfae0cad33b37360f24f5a` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `64252b0bdf9675345309b7e0a3f71adac9f5336ef4ee4af4bedfe33600f724f5` |

This checkpoint does not complete CORE-F, establish a detached .NET host, or promote Core canonical.
