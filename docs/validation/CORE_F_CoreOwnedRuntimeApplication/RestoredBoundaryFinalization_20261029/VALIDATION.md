# CORE-F restored-boundary finalization validation

## Source identity

- Candidate implementation: `956a3be1c99b8a678787693f54e55540fefd6992`
- Candidate tree: `e18628437f87e0d952004fa2d0b7d3cc840110b5`
- Candidate base: `5c066fe55f663c1077cb8e2e02ab7e74a79203d7`
- Core canonical at validation: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- The candidate changes only the Core-owned restored-boundary finalization path, its Unity host ports, and focused integration fixtures. Existing user `ProjectSettings` changes and unrelated files were excluded from the candidate.

## Validation results

| Gate | Result | Run identity |
|---|---:|---|
| CoreValidation cold | 928/928 PASS | `5cf6394fc60a4758a29ad632112f782b` |
| CoreValidation warm | 928/928 PASS | `5cf6394fc60a4758a29ad632112f782b` |
| P12-G restore suite | 32/32 PASS | `7436c9c9277448c8b84abbd554ed6301` |
| Runtime admission suite | 228/228 PASS | `1c81f1a677c14a0ea36b8a1483a8e4b8` |
| Runtime orchestration suite | 13/13 PASS | `effcb86b727448a7a309f5dc6f815ab1` |
| Full Unity EditMode | 3,279/3,279 PASS | `6c6daf643aaa460c9444615891eca680` |
| Official Smoke selection | 5/5 PASS | `521d92c938d74c30a68e87c018f6f5bb` |
| Candidate diff check | PASS | `git show --check` on candidate |

The CoreValidation run passed on the exact candidate commit. Its runner reported that a Unity `Library` directory existed in this worktree; this result does not claim Library-independent .NET execution.

The Smoke selection contains these five test identities, and all five also appear in the full EditMode report: the two `GMConsolePlayModeSmokeTests`, `WorldObserverPlayModeSmokeTests.ObserverSceneCanRenderAndSelectNode`, `WorldObserverReadModelTests.ObserverPrefabSmokeInitializesWithoutMissingReference`, and `WorldObserverVisualTests.ObserverPrefabSmokeHasNoMissingSerializedReferences`.

## Public and private evidence

The full EditMode result is packaged at `coref-restored-boundary-editmode-3279.manifest.json` and `coref-restored-boundary-editmode-3279.zip`. The package verifier passed with one run and one report entry. Archive SHA-256: `33b223bfbaea9a5aea3423ec89de29b54532940ff850e2b8728d3493f98e2e5b`. The archived normalized XML has SHA-256 `3ab49f34b453cee97792e2fbdbb3d006cdc4541d656ab8dcfd0e2edbe6a4520c`; the protected original report has SHA-256 `5a238d6c409bf05b7dc771015dc019fbaf274377274b04fa7b7729d3e681891a`.

Focused-run originals and the standalone Smoke report remain in the official owner-only evidence area. The standalone filtered Smoke package was refused by the fail-closed packager because its root `Simulation` suite count did not match its descendants. The original XML was not edited or repackaged; the complete unfiltered EditMode package independently contains the same five Smoke test identities and passing results. A prior PlayMode selector returned zero tests and is excluded from all pass claims.
