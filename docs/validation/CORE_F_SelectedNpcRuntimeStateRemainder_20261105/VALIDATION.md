# Selected NPC runtime-state census remainder fix

Date: 2026-11-05

## Candidate and scope

- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Candidate base: `9359ed39056995e3d071312496759982d2bbb161`
- Candidate code commit: `958e7cc58faaf3dc7d0f371f938dfc4471ed6c20`
- Candidate tree: `18d61b8dd19ec636c8279c664b6589b377fc0ee1`
- Changed files: `Assets/_Project/Scripts/SimulationRuntime.cs` and `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`

The Unity host remainder validator now subtracts the Core-owned selected NPC runtime-state rows when checking the expected remainder cardinality. It continues to validate exact identities, cardinalities, owner instances, and revisions before filtering. A test now verifies host remainder capture at the initial completed boundary before advancing, in addition to the existing post-advance assertions. Mixed-roster rows remain on the full Unity compatibility path.

## Validation

- Official CoreValidation: **1017/1017 cold and warm** on candidate `9359ed39`. This change touches only the Unity host adapter and its EditMode test; the Core source and Core test inputs remain unchanged from that run.
- Focused owner-thread regression: **1/1 PASS**, run `f630c22e40f6421b95de4e67c15e94e3`.
- Mixed-roster compatibility fallback: **1/1 PASS**, run `7ba7f7a696954b86a7dad6825b302223`.
- Full official EditMode: **3299/3299 PASS**, run `d3ac101d09464135afced4727fcf2de3`; zero failures, skips, or inconclusive cases; input snapshot stable.
- Official Smoke filter: **5/5 PASS**, run `3bb6108da4504fb7b5ea424547a2b481`; all five selected case identities are present in the passing full EditMode report.
- `git diff --check`: PASS.

The official full EditMode run was captured with HEAD `9359ed39` and the two candidate files as working-tree changes. Their SHA-256 values in the captured source inventory match the committed `958e7cc` files exactly:

- `SimulationRuntime.cs`: `ab255eab49017f44a23dc83cabffad29190035266c8dabab1bc68f2409b720f5`
- `SimulationRuntimeAdmissionTests.cs`: `0cb19d059f1fe609770b37795e2fd9109d1b07edaa9977c8dab9bc2176d966dc`

The full EditMode XML was packaged and independently verified with the approved evidence tool. The full bundle is `Unity/selected-npc-state-remainder-full.zip` (SHA-256 `e30b80f1356e84588ad8ddc927204fa5a63eaa97600391a80ef16b71e6d80ede`); its public manifest is `Unity/selected-npc-state-remainder-full.manifest.json` (SHA-256 `edfc07e4b6cfad4e955f827af400f2d251561271638599644ee07b4a061c51a8`).

The separately filtered Smoke run passed, but its XML did not satisfy the evidence packager's closed suite/descendant count check. That original report remains in protected local retention and was not altered or repackaged. The same five Smoke case identities and passing outcomes are contained in the verified full EditMode package.

## Limits

This is a bounded census composition correction only. It does not complete CORE-F, establish a detached .NET host, or authorize Core canonical promotion. Candidate remains unpromoted.