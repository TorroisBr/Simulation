# World Atlas UX Backlog

## Inspector Entity Reference Navigation

**Status:** Planned; separate bounded UX and architecture review required before implementation.

Let an Inspector property that contains an actual typed, immutable observation reference navigate to its referenced entity. A normal click should select the target and synchronize the current selection, Inspector, navigator and map highlight when verified spatial placement exists. A future Shift+click preview may inspect a target without replacing the primary selection; its exact interaction needs review. Offer a map-locate action only when geographic placement is verified.

Keep navigation read-only and scoped to the current observation identity. Use stable entity identities and typed references only. Never infer a link from a name, label, list position, proximity, legacy identifier similarity or unverified association, and do not fabricate a City-to-Hex binding. Show unavailable for absent, stale, unsupported or out-of-scope references. Preserve keyboard access and visible focus; Shift+click cannot be the only way to inspect reference information.

Before implementation, review reference representation in Inspector rows, primary selection versus preview, history/back behavior, map focus versus selection, missing references, reference chains, accessibility and future entity types. Prefer a small reusable navigation pattern without introducing a universal world graph or expanding the observation schema for UI convenience.

This item is planning only. It is outside the accepted Shared Viewport integration and adds no current UI behavior.
