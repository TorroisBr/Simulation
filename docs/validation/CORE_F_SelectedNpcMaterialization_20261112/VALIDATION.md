# CORE-F selected Daily-v1 NPC materialization validation

## Scope

For a fully selected `UnityBootstrapDailyV1` roster, dynamic Person-to-NPC materialization now requires the exact `NpcData` object to resolve through the admitted `DailyV1DefinitionAdapter.SelectedBindings` to the exact node in the runtime's immutable effective-definition graph. The candidate NPC is built with the existing `NpcRuntime.CreateSelectedDailyV1` factory and Core owner composition before it is published. Existing membership/P12 registration and city-link commits remain in their original transaction. An unadmitted asset is rejected before owner writes. Mixed or unselected rosters retain the existing Unity constructor path.

The regression verifies exact asset, definition-node, graph and owner identity; the successful transaction completes one membership mutation epoch. The rejected unadmitted asset leaves `PersonStore.Revision`, roster size, City presence cardinality/revision and mutation epoch unchanged. This is a bounded materialization integration; it does not claim a complete Core-owned runtime or `DailyV1Application.TryStart`.

## Validation

All three runs used the same stable validation-input inventory SHA-256 `4a037ce97fecc6c8595acebed7c4bb590ea93c0e4d630c220699a3ad33ebf908` (1,980 files in the runner's required input roots), with matching before/after inventories and a stable Unity executable. The captured base identity was `357cf9357500b486224467aafb5635ca81898c5f` / tree `3c8bd35c376de462a963dbde49d15f63693523f7`; the inventory records the tested working-file bytes, including the candidate source/test edits. The subsequent candidate commit was restricted to those source/test files and this evidence. Pre-existing user `ProjectSettings` and unrelated validation-document changes were preserved and excluded from the candidate diff.

- Unity Editor `6000.3.9f1`; Unity Test Framework `1.6.0`; official evidence runner/packer `1.1.0` on pinned PowerShell `7.6.5`.
- Focused `SelectedDailyV1PersonMaterializationReconcilesDynamicOwnerCardinality`: 1/1 passed, zero failures, skips or inconclusive; run `496ab9a77d3944df99d1161bd07ebaa6`. Original XML SHA-256 `76688592d9c641f5ddd4be63156368679196fd406e4f786de7369c42a34bd812` (2,729 bytes).
- Full EditMode: 3,316/3,316 passed, zero failures, skips or inconclusive; run `3aa52e426d134a19827f9d78e74985b8`. Original XML SHA-256 `1126a1aff96c270b2b81ad130f909cac84ddb2af2611b4525457d45a26a44cee` (3,302,345 bytes).
- Official Smoke name filter `Smoke`: 5/5 passed, zero failures, skips or inconclusive; run `7a79586d6b53422baf204d8e6fe62ebd`. Original XML SHA-256 `8140781bec219634395f3624fc2f740705c7ede1d949cf88accd6436a9486d59` (6,359 bytes). A comparison of full test identity and outcome keys confirmed the focused case and all five Smoke cases are present in the full EditMode result.
- `git diff --check`: passed.

The complete ALL EditMode report was normalized by the approved packager and independently verified against its protected original run manifest. The result-only package contains all 3,316 cases and preserves their hierarchy, identities and outcomes; arbitrary report output is removed. Package manifest SHA-256: `ee191ee8168994c77b8891dc246d3a940d7251f6c75b4fecc29d00db967dda16`. Archive SHA-256: `2d74397d018b684bbe42a2218815880bf394ad45dc9f897cd483c80068b040ec`.

The filtered Smoke XML was not included as a second package entry: the packager rejects its filtered parent `Simulation` suite count because it disagrees with the selected descendants. The original filtered report was left unchanged and remains in protected local retention; its five case identities/outcomes match the complete packaged report. An initial verifier invocation from an unsupported PowerShell host was rejected; package generation and source comparison then passed under pinned PowerShell `7.6.5`. No CoreValidation run is claimed because `Simulation.Core` sources did not change in this slice.

## Evidence and boundaries

The public package above is the complete accessible result evidence. Immutable original XML and runner logs for the focused, full and Smoke runs remain in owner-only local evidence retention; no raw reports, logs, private absolute paths, or unrelated user files are included in this record or package. The `ProjectSettings/ProjectSettings.asset` local edit was part of the runner's input inventory, remains untouched, and is excluded from the candidate commit.