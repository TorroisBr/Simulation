# Phase 10 — Local Generation & Pre-start Authoring

**Authority:** planning entry subordinate to `../SIMULATION_ARCHITECTURE.md`. **Readiness:** P10-A is `PROMOTED` in the owning State; Phase 10 remains open. Any further local-generation/authoring slice is `READY_FOR_PRODUCT_SCOPE_DECISION`, not an approved implementation checkpoint. P9-B is promoted at P9 canonical `d9a62d7c6bea242653c2d68cc0a70911bb5ed1bf` and supplies the authored P8 Hex/Location source through the P9 genesis handoff.

## Objective and closure

The approved first profile composes exactly one `ExplorableSiteKind.Ruin` at an existing canonical P8 `LocationId`, with the minimum finite local topology needed to represent semantic places, one or more entry points, containment only where needed, and explicit local connections. P9 authored-bootstrap genesis/provenance is consumed; this profile adds no new generated content.

**Checkpoints:** P10-A — Ruin LocalTopology Genesis Composition is promoted within its bounded first profile; see the owning State for exact delivery evidence. No further P10 checkpoint or phase-wide closure contract is approved.

**P12 dependency clarification (2026-10-03):** P10-A's pre-start world composition did not require Save. A future supported profile that includes additional generated facts must preserve their exact initial state and compatible provenance, but full P12 closure is not a prerequisite to author a separately scoped P10 contribution. Its product scope remains open.

The [next-scope product decision packet](../architecture/P10_P14_P20_NEXT_SCOPE_DECISIONS.md) compares bounded P10 follow-ons. Its recommendation is advisory; no next checkpoint is approved.

## Dependencies and gates

- **Hard semantic contracts:** relevant P9 genesis/provenance contract and P8 Location/LocalTopology/anchor contract.
- **Hard capabilities:** promoted P9-B genesis and P8-A/P8-C spatial/anchor authorities are available. P10-A adds the bounded LocalTopology owner adapter and Ruin facts before publication.
- **Promoted P9-B source:** the selected geography-enabled P9 profile supplies exactly one authored Hex and one Location anchored to it, with the accepted P8-A coordinate, terrain-reference and scale provenance inputs. P9 composes these through `SpatialAuthorityStore.TryComposeGeography` before validation/publication and exposes the same P8 authority and profile manifest through `SimulationBootstrapComposition`. P10 must resolve the actual selected-profile LocationId and anchor from that handoff; it cannot mint/adapt a Location, substitute a test-fixture ID, or infer geography from legacy runtime locations.
- **Integration dependency:** the Ruin's stable identity and Location binding resolve through one factual Location/local-topology model.
- **Soft ordering:** broader Phase 8 civil-travel validation is not needed for this profile.
- **Architecture boundary:** P10-A's reviewed design specifies local containment/entry and the pre-start versus post-start mutation boundary; see the owning State for delivery.
- **Product gate:** resolved only for the bounded Ruin profile above. No City, Market, population/NPC/economy, Passage/Route, Knowledge, activity, loot, encounter, construction, or other gameplay content is in scope.
- **P10-A delivered scope:** the minimal stable-owner/LocationId-neutral LocalTopology ownership/migration seam needed to publish the Ruin through normal authorities. P8-C's promoted `ExplorableSite` anchor contract is consumed; preserve existing City/Site consumers and do not expand P8-C or generalize the adapter beyond this profile.
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

Current alignment constraints also include the intraday/extensibility record
(`../architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md`) and the
multi-participant record
(`../architecture/MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md`). P10's selected
profile creates no timed work, activity, participant, role, reservation, or
cardinality facts, so P18/P20 do not become dependencies. Stable stage
identity/version, typed inputs/outputs, explicit dependencies, deterministic
composition and causal provenance remain review constraints; P19 loader/API
and retrofit work stay deferred.
