# Phase 10 — Local Generation & Pre-start Authoring

**Authority:** planning entry subordinate to `../SIMULATION_ARCHITECTURE.md`. **Readiness:** `WAIT_DEPENDENCY` for the authored P9-B Location source; technical/design and authority gates also remain open. The first profile's product scope is approved below, but no P10 checkpoint ID or implementation authorization is established.

## Objective and closure

Generated or authored local sites, sublocations and related content become the same valid initial World Truth and local topology, without a separate generator-only spatial ontology.

**Approved first-profile scope (2026-09-27):** one manually authored `ExplorableSiteKind.Ruin` bound to an existing canonical P8 `LocationId`, with only the Ruin's site identity/binding and the finite `LocalTopology` facts needed for a tiny local layout: semantic places, one or more entry points, containment where needed, and explicit local connections. An illustrative fixture may be `Entrance → Courtyard → Inner Chamber`; these labels/layout are proof content, not a universal Ruin schema. No City/Market/population/NPC/economy, regional Passage/Route facts, Knowledge, activities, loot, encounters, construction or other gameplay systems are included. P8 Location/anchor truth and P9 genesis/provenance are consumed dependencies, not new P10-generated scope.

The P10-owned seam is the bounded, `LocationId`-neutral LocalTopology ownership/migration capability required to publish this profile through normal authorities. P10 does not mint or own a P8 `LocationId`, Hex, anchor, terrain or world scale. No P10 checkpoint ID is yet approved; architecture/technical entry must define exact authority/API and implementation gates before any checkpoint becomes schedulable.

## Dependencies and gates

- **Hard semantic contracts:** relevant P9 genesis/provenance contract and P8 Location/LocalTopology/anchor contract.
- **Hard capabilities:** this approved Ruin profile needs P9-B's authored existing-`LocationId` source and P8-C's promoted Ruin/site owner plus `LocationId`-neutral LocalTopology ownership/migration capability. P9-B remains proposed/unaccepted, so this profile is currently blocked on that dependency. Entry into actual world composition also requires accepted technical/authority design and the promoted implementation capabilities for the bounded slice.
- **Integration dependency:** authored and generated sites must resolve through one factual Location/local topology model.
- **Soft ordering:** broader Phase 8 civil-travel validation can exercise sites, but does not define all local generation.
- **Architecture gate:** local containment/entry and the exact pre-start versus post-start mutation boundary require consumer-specific design.
- **Product gate:** the bounded Ruin-first profile scope above is approved. Broader content or gameplay scope is not implied; remaining gates concern architecture, authority semantics, readiness and implementation authorization.
- **Exclusions:** runtime construction/founding, automatic world expansion, renderer authority, and a universal WorldEntity.
- **Replay/fork sensitivity:** site IDs, local topology, initial state, provenance/content versions and initial randomness must be reconstructible.
- **Hotspots/parallelism:** Location anchors, LocalTopology, City/Site authoring, generator composition and diagnostics; design can be isolated, integration follows stable P8/P9 contracts.
- **Downstream unlocks:** richer initial world content for later consumers, without granting runtime creation authority.
- **Deferred:** exact local generation passes, layouts, mod schemas and runtime expansion.

## Generation extension alignment — 2026-09-26

This Ruin profile participates in P9's ordered dependency-aware pipeline, or
an explicit compatible subpipeline with the same publication and provenance rules.
Independent contributors can consume earlier data and contribute stages/scoring
only with declared deterministic dependencies/order. This is not a P19 loader
implementation or a new mod schema. Existing-world install/retrofit is separate
from pre-start authoring; it does not implicitly regenerate historical local
layouts. Dependencies on P8/P9 remain scoped to the actual chosen capability.
