# Selected NPC Inventory and MoneyAccount Census — Validation

Candidate code commit: `b3448e171ffbcc90f5e29421e0e20f787540dcb2`
Candidate code tree: `cac40d9ca450e1f21699a6b2253a0591a54a37a1`
Base: `9c5e85cdbc5d094a4b934d63c689ebbb8a972a91`
Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

The candidate binds the selected NPC Inventory and MoneyAccount census rows to the exact Core owner graph. Core recomposes those rows only for an exact selected roster and exact identity/cardinality/revision witnesses. Mixed rosters retain every NPC row on the existing Unity host compatibility path. Stale or substituted rows are not omitted.

## Exact source inputs

The isolated Unity and library-free .NET validation worktrees used the four changed files below. Each copied file was SHA-256 checked against the implementation worktree, and the committed code tree contains the same source.

| Source | SHA-256 |
|---|---|
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `D9726A050442C54B4B8AC08B3AA2662EC936EF92F6CC516508214F7F1A1D4A2D` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `59EAEBDC3C2191880FF9535AF4E3D3FF202F9A010C2F8E095FC2FF2283E88961` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs` | `1D8C3265D68CF0BD8D79BFFD15C8C91FF119F402235F5C11D8EFF8046E2DA8C7` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs` | `B195082B7C0D81D4893E189120397B76E9124C44B6097E7FA2DF258B08523B96` |

The local `ProjectSettings/ProjectSettings.asset` was copied unchanged into the isolated Unity validation worktree as a test input (SHA-256 `F0771A23C3EBCB50D651C13515EE66E6E56D321C6115F54A2193BF35DD994633`). It remains a separate protected user modification and is not part of the candidate commit.

## .NET validation

Official `Tools/CoreValidation/Invoke-CoreValidation.ps1` PASS from a library-free isolated worktree. The stable locked-source input digest was `d6ce096c86b3f06fd67f2eb921264b5cbf92b3125271410fc3b163ab74dbc99d`.

- Cold and warm CoreValidation: `1016/1016` PASS.
- Cold/warm Core, CalendarDemo, and Lab builds/runs: exit code `0` for every gate.
- `Library` absent from the validation worktree.
- Run ID: `a4701bc89ae642ee8a435a6c45434e6e`.
- Private manifest SHA-256: `8CC74F1E1447E8A72DE4C2AEB227033126F7BE28D2C19445A639C40D8F032019`.
- Cold TRX SHA-256: `6edcabb6ae2a8306f3244a1bf7abe8d544d28b4e1740ea086c2d47625e7933d0`.
- Warm TRX SHA-256: `59779a1f3bdf5ce1dc904edbc8fcd0924d21a1ac4f36333f729373b344a19f0f`.

An earlier invocation was excluded because its process working directory was the unrelated C: checkout; it failed at `restore-lab-locked` before testing. The corrected invocation launched from the E: validation worktree and passed. The failed diagnostic was retained unchanged in private evidence.

## Unity validation

Official Unity `6000.3.9f1`, sequentially against the isolated worktree copied from base `9c5e85c` plus the four source files above. The isolated worktree used its own `Library`. The original XML and logs remain unchanged in owner-only private evidence; logs are not published because they contain local execution paths.

| Gate | Result | XML SHA-256 |
|---|---:|---|
| Focused `StartCapturesAndPassesTheSelectedProfileOwnerThread` | 1/1 PASS | `F5E0B3BC4B4CF201729E915EDFD4C3BA30085D57BD39775545C34A447AF6002C` |
| Focused `SelectedCrimeJusticeDailyStagePublishesP12WritesAndUsesWholeRosterCompatibilityFallback` | 1/1 PASS | `BA879FC425C22850F50DAB84672FC658FB3D9E70A954684A9D930595B20E2A74` |
| `SimulationRuntimeAdmissionTests` | 243/243 PASS | `940CC010D9FF0FA05133DF8D81D15D5A01260546640FEF08264F5CB3E4CBEDF4` |
| Full EditMode | 3299/3299 PASS | `C68167926C9624068CC2792F2470508B1617909377E0643D772FA5DD77E9AF03` |
| Official Smoke (`EditMode -TestFilter Smoke`) | 5/5 PASS | `686CBB5F85A75DD692DA21CF02D94C11FC7DDDBD59E3057D416D8489E0B75CBE` |
| `SimulationRuntimeLongRunTests` | 7/7 PASS | `0847ED425833ACCD4831EA53E82ADF4BBD20155E1AF085CB6A8C40E514622BC8` |

A PlayMode Smoke-name attempt returned a coherent zero-test XML and is excluded. Repository validation records define the official Smoke gate as `EditMode -TestFilter Smoke`; that command ran five tests and passed.

## Integrity and limits

- `git diff --check`: PASS; the only line-ending warning concerns the unchanged protected ProjectSettings file.
- Raw .NET and Unity manifests, reports, and logs are retained under the worktree's owner-only `.private-core-validation` and `.private-unity-validation` directories, respectively; those private artifacts are not part of this commit.
- This checkpoint does not complete CORE-F, implement `DailyV1Application.TryStart`, or promote Core canonical.
