# Cross-application UI architecture review request

**Requested reviewer:** General Architect, through the existing `architecture general` thread.

**Status:** review prompt prepared, not sent. The Codex approval reviewer rejected sending this repository metadata to the identified thread because the trusted transcript did not explicitly authorize that transfer. No alternate delivery route was used; user approval is needed before sending it.

**Review scope:** recommend a small, staged sharing boundary for World Atlas and Dungeon Studio, with City Studio as a future consumer. This is architecture evaluation only; it does not authorize a shared-library extraction or changes outside `WorldAtlas/`.

## Baselines and evidence

- Current architecture: `codex/architecture/world-identity-projection` at `158f04d4a021c3984b3ba841e246b45e1b0a457b`.
- Atlas source: `codex/atlas/standalone` at `dba4a8c6ac85c4a7913a9a27b6e9d6ae0b69f9e0`; this iteration uses an isolated C: worktree/branch and merges the current architecture docs first.
- Dungeon Studio source baseline: `codex/dungeon/studio` at `b3ebfaa89cb91958a3908a6bdf96509416735878`.
- The Dungeon Studio worktree currently has uncommitted edits in 10 files, including `MainWindow.cs`, `MapCanvas.cs`, `MapScene.cs`, and headless tests. These are preserved and have not been edited or tested by Atlas. Review any suggested API against that in-flight state without treating it as a stable extraction baseline.

Both desktop applications use Avalonia. The Atlas `HexMapView` currently owns a read-only axial-hex transform, pan/zoom/fit, hit testing, and selection notification; its map geometry and observation identities are Atlas-specific. Dungeon Studio's `MapCanvas` is an editor with room/corridor geometry, editable gestures, and command previews over `Dungeon.Core.MapScene`; its editing semantics must remain owned by Dungeon Studio. `WorldAtlas.Desktop` and `Dungeon.Studio` target .NET 10, while `Dungeon.Core` targets `netstandard2.1` and should not acquire UI dependencies.

Atlas's current About dialog is created with `SizeToContent.Height` around wrapped text. The bounded Atlas fix will give it explicit dimensions and keep the layout independent of hover-state measurement. This is an Atlas-specific finding; it is not evidence of Dungeon Studio's root cause. Atlas typography/scale preferences and observation Inspector facts are also currently application-owned.

## Questions for the General Architect

1. Which responsibilities merit a shared, application-neutral Avalonia library after the Atlas and Dungeon implementations are stable: viewport transform math, input/gesture primitives, selection plumbing, dialog shell/window sizing, theme tokens, typography/DPI scaling, or inspector presentation?
2. Which candidates should remain local because their meaning differs: axial-hex projection and identity, Dungeon geometry and editing commands, observation availability/provenance, and each application's Inspector facts?
3. What is the smallest shared interface that could support a read-only Atlas viewport and an editable Dungeon canvas without making Atlas reference `Dungeon.Core`, putting UI into `Dungeon.Core`, or forcing both apps to adopt the same selection model?
4. Should any extraction wait for a stable Dungeon Studio commit and a second measured consumer, rather than being inferred from similar-looking pan/zoom or About behavior?
5. What ownership, target framework, package/versioning, independent .NET tests, DPI/scaling policy, and integration sequence would make a future shared component independently testable and reversible?

## Requested output and exclusions

Please return a bounded recommendation with: (a) share now / defer / keep application-specific decisions per component, (b) the smallest proposed owner and interface if sharing is justified, (c) prerequisites and test/packaging gates, and (d) any current architecture conflict.

Do not edit Dungeon Studio or Atlas code in the architecture review. Do not create a generalized UI framework, move map/domain geometry, define a common world/dungeon coordinate model, or make City Studio an implementation dependency. This Atlas iteration may ship its own proven typography tokens, settings persistence, and dialog fix while the architecture review remains separate.
