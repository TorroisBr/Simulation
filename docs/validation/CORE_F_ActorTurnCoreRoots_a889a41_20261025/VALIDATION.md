# CORE-F Actor Turn Core Roots - validation record

Status: **VALIDATED_CANDIDATE** for this bounded checkpoint only.

## Source identity

- Branch: `codex/core/CORE-F-p12d-core-hydration`
- Candidate commit/tree: `a889a41c6451afc8c13fd2e3289487b5dc7259d0` / `bb651f573a4368dfeca80a622d97ed8d097c68a5`
- Base: `334c386abe5cce8ed68ab72a907a0a2424c3acbb`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

This slice binds admitted actor-turn actions, choices, and Merchant operations to the RNG, allocator, record sequence, and decision store already bound to Core execution state. It preserves the unadmitted/legacy path. It does not complete the shared application or CORE-F.

## Validation

### CoreValidation

- Run `731a8294326a4df9b4b652e3baa54b17`: cold/warm **804/804 PASS**, cold build exit 0.
- Private manifest SHA-256: `15b11ca642f2d9c84aaad9d2805404fc9f7c8f7a33ad98c40af48cadbd1c1f25`
- Input digest: `08dfb7d2b9881d8e9016ac8612826ebdc31283a5a765d01ed9dfa0fa426c7dfb`

The manifest captured the candidate's changes on base `334c386`. Its nine changed Core-package/test input hashes match the exact candidate worktree. Unity `SimulationRuntime.cs` is outside this runner inventory and is covered by Unity EditMode. Original .NET TRX and logs remain protected; no public .NET package is claimed.

### Unity EditMode

- Run `122d30905b1a4325a53a23ebcb08d0bf`, Unity `6000.3.9f1`: **3264/3264 PASS**.
- Stable input snapshot; exact candidate HEAD/tree as above; digest `2786c53ae6267b08454864f0f5efd7fc5d1c5dbc01a78c9824d00ca584982768`.
- Result-only archive `coref-actor-turn-a889a41-editmode.zip`, SHA-256 `e6ea6e80a3d19a769b6645b12edbdb2a0bd4b8a83cfa57f5990535fb052306fe`.
- Public manifest SHA-256 `9fbe03e7510a0345367d894d44d223d5090151ef4138fd1f63ba596c8c878229`.

The archive hash matches the manifest. Privacy checks found no workstation paths in the manifest or result XML. Input hashes for all six changed production files under `Assets` and `Packages` match the candidate worktree. CoreValidation covers the four changed Core test files.

The Unity snapshot includes three pre-existing modified `ProjectSettings` files as stable inputs. They are outside the candidate commit and were not changed here; this is not described as a clean-worktree run.

### Official Smoke filter

Private EditMode filter run `0844feacb60446ab9c12e5fc8a0f0656`: **5/5 PASS**, stable inputs. No separate public Smoke archive is claimed. All five IDs, names, and passing results are present in the full public EditMode archive:

- `GMConsolePlayModeSmokeTests.GmConsoleCanPreviewApplyAndRefreshObserver` (2178)
- `GMConsolePlayModeSmokeTests.GmConsoleNaturalLanguageCanTranslatePreviewApplyAndRefreshObserver` (2179)
- `WorldObserverPlayModeSmokeTests.ObserverSceneCanRenderAndSelectNode` (4493)
- `WorldObserverReadModelTests.ObserverPrefabSmokeInitializesWithoutMissingReference` (4521)
- `WorldObserverVisualTests.ObserverPrefabSmokeHasNoMissingSerializedReferences` (4544)

`git diff --check`: PASS.

## Preservation and limits

Earlier failed or operationally blocked attempts remain in their original protected evidence locations; they were not rewritten, deleted, reclassified, or counted as passing evidence. These successful runs are additive.

This record does not claim Core canonical promotion, P12 completion, full CORE-F acceptance, or completion of the detached .NET application/host. Protected ProjectSettings changes, private evidence, and untracked user files remain outside the candidate.
