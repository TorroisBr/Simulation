# Phase 10 — Local Generation & Pre-start Authoring

**Authority:** planning entry subordinate to `../SIMULATION_ARCHITECTURE.md`. **Readiness:** approved first-profile scope; technical design and capability dependencies remain open; implementation is not authorized.

## Objective and closure

The approved first profile composes exactly one `ExplorableSiteKind.Ruin` at an existing canonical P8 `LocationId`, with the minimum finite local topology needed to represent semantic places, one or more entry points, containment only where needed, and explicit local connections. P9 authored-bootstrap genesis/provenance is consumed; this profile adds no new generated content.

**Checkpoints:** no P10 checkpoint ID or closure contract is approved. The technical design may propose one bounded checkpoint for review; that proposal does not authorize implementation.

## Dependencies and gates

- **Hard semantic contracts:** relevant P9 genesis/provenance contract and P8 Location/LocalTopology/anchor contract.
- **Hard capabilities:** entry into actual world composition needs promoted generator and spatial/local authority appropriate to the chosen slice.
- **Integration dependency:** the Ruin's stable identity and Location binding resolve through one factual Location/local-topology model.
- **Soft ordering:** broader Phase 8 civil-travel validation is not needed for this profile.
- **Architecture gate:** local containment/entry and the exact pre-start versus post-start mutation boundary require consumer-specific design.
- **Product gate:** resolved only for the bounded Ruin profile above. No City, Market, population/NPC/economy, Passage/Route, Knowledge, activity, loot, encounter, construction, or other gameplay content is in scope.
- **Remaining technical gate:** inspect and define the bounded LocationId-neutral LocalTopology ownership/migration seam needed to publish the Ruin through normal authorities. P8-C supports `ExplorableSite` anchors, but its current runtime owner resolution and LocalTopology owner references are runtime-ID based; do not assume stable identity mapping is already delivered.
- **Exclusions:** runtime construction/founding, automatic world expansion, renderer authority, and a universal WorldEntity.
- **Replay/fork sensitivity:** Ruin/local topology identities, P9 genesis provenance, and initial state must be reconstructible; this profile adds no local randomness.
- **Hotspots/parallelism:** Location anchors, LocalTopology, City/Site authoring, generator composition and diagnostics; design can be isolated, integration follows stable P8/P9 contracts.
- **Downstream unlocks:** richer initial world content for later consumers, without granting runtime creation authority.
- **Deferred:** generated local-content passes, generalized layouts, mod schemas and runtime expansion.

## Generation extension alignment — 2026-09-26

The Ruin profile consumes P9's promoted authored-bootstrap genesis selection,
pipeline, and provenance; it does not add generated content or alter that
profile. Any P10 contributor must preserve the P9 dependency, validation,
publication, and provenance rules. This is not a P19 loader implementation or
a new mod schema. Existing-world install/retrofit is separate from pre-start
authoring and does not implicitly regenerate historical local layouts.
