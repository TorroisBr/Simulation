# CORE-F selected forced Escape Core boundary

**Status:** validated candidate; independent exact-tip review is pending. This is a bounded runtime/P12 integration and does not complete or promote CORE-F.

## Candidate

- Base: f68e210844ed814863b272ccd9babd4416240876
- Implementation commit: ad2d4325aaf1b718bcc435e708d67c311b763461
- Candidate tree: 7568548644d26ff45364085f9efd2d96605dd19b
- Core canonical at validation: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Changed production file: Assets/_Project/Scripts/SimulationRuntime.cs
- Source SHA-256: 2c7cc655178bbf2f243d021bac290c28cb9afd8d95c8e09d00c44a19cf332b55

The scheduled ForceOutcome/EscapePrison route now constructs the existing Unity projection adapter, then—when P12 admission is enabled—passes that projection through DailyV1CoreSelectedCrimeActionBoundary.TryCreate. Core pins the exact admitted owner graph, actor/action slot, session thread, Justice owner identity, P12 protocol, and operation scope. The non-admitted compatibility route retains the prior adapter. The ordinary RequestAction route, forced-outcome semantics, action projection, no-roll behavior, same-day-arrest rule, owner identities, revision and epoch notifications remain unchanged. No operation ID, section identity, profile, or persistence contract was added.

## Validation

| Gate | Result | Run |
| --- | --- | --- |
| Focused selected Escape EditMode (ForceOutcome and RequestAction) | 2/2 passed | 9bd83d1baf13475cb1e91f0d499784b2 |
| Full Unity EditMode | 3,318/3,318 passed | 24b556baf2ec4f2a803858697f872601 |
| Official Unity Smoke (EditMode filter Smoke) | 5/5 passed | 3f3023779bf44eb088cd96ab26947cfc |
| git diff --check | Passed | implementation candidate |

All three accepted runs captured commit ad2d4325aaf1b718bcc435e708d67c311b763461, tree 7568548644d26ff45364085f9efd2d96605dd19b, with a stable 1,982-file input snapshot digest 5007a9be0e301a95a505f639febefee415434bde77c4d7bf3d4bc0a9f08038ef. The full XML contains both focused Escape cases and all five passing Smoke cases.

The verified, privacy-safe full EditMode package is [coref-selected-forced-escape-editmode-20261112.zip](UnityEvidence/coref-selected-forced-escape-editmode-20261112.zip), SHA-256 3cd251cec193705c358b60f26161c769e3ac6f81d641ee66944679865e91ca36. Its manifest is [coref-selected-forced-escape-editmode-20261112.manifest.json](UnityEvidence/coref-selected-forced-escape-editmode-20261112.manifest.json), SHA-256 57c253276167a2fe4c3ca9c82afc99e12f85c300fee13ceac0c0ee14c8b7f5.

## Evidence notes

Unity's first import changed EditorBuildSettings.asset and ShaderGraphSettings.asset only in the disposable validation worktree; that first focused result was input-unstable and is excluded. The accepted focused, full, and Smoke runs were subsequent stable snapshots. The candidate worktree's protected ProjectSettings edits were not touched.

The official packager rejects filtered NUnit XML whose suite testcasecount describes the full assembly while its descendants contain only filtered cases. Those original focused and Smoke XML/log files remain unchanged in private E: evidence. They are not separately repackaged or rewritten; their cases are covered in the verified complete EditMode package. CoreValidation was not rerun because this change is confined to the Unity adapter and does not alter Core source or tests.

This candidate remains unpromoted. It does not claim the complete shared Daily-v1 runtime, DailyV1Application.TryStart, detached .NET continuation, or CORE-F closure.