# Phase 10 — Local Generation & Pre-start Authoring

**Authority:** planning entry subordinate to `../SIMULATION_ARCHITECTURE.md`. **Readiness:** P10-A's user-approved bounded scope and refreshed technical design/checkpoint record passed independent review at `345dcbc8f05e0d64fed1b058d30f08ad0be8937d`; its implementation candidate passed independent review, validation and promotion preflight, then was user-approved and promoted at code tip `9501bf076d506fb64d6ee3e6d178574fff36e153` (State record `9e79b58`). P9-B's geography capability is promoted at code tip `d9a62d7c6bea242653c2d68cc0a70911bb5ed1bf`; the current P9 canonical closure/State tip is `82396ae7ffaf407fda278928da456b06dc5394d4`. P10-A is delivered; Phase 10 remains open for its broader objective and any separately accepted mandatory checkpoints.

## Objective and closure

The approved first profile composes exactly one `ExplorableSiteKind.Ruin` at an existing canonical P8 `LocationId`, with the minimum finite local topology needed to represent semantic places, one or more entry points, containment only where needed, and explicit local connections. P9 authored-bootstrap genesis/provenance is consumed; this profile adds no new generated content.

**Checkpoint:** P10-A — Ruin LocalTopology Genesis Composition. The user approved its scope; its durable record and technical design passed independent review. The bounded Ruin/LocalTopology capability is promoted on `codex/phase10/canonical`; this checkpoint promotion does not close Phase 10.

## Dependencies and gates

- **Hard semantic contracts:** relevant P9 genesis/provenance contract and P8 Location/LocalTopology/anchor contract.
- **Hard capabilities:** promoted P9-B genesis and P8-A/P8-C spatial/anchor authorities are available. P10-A adds the bounded LocalTopology owner adapter and Ruin facts before publication.
- **Promoted P9-B source:** the selected geography-enabled P9 profile supplies exactly one authored Hex and one Location anchored to it, with the accepted P8-A coordinate, terrain-reference and scale provenance inputs. P9 composes these through `SpatialAuthorityStore.TryComposeGeography` before validation/publication and exposes the same P8 authority and profile manifest through `SimulationBootstrapComposition`. P10 must resolve the actual selected-profile LocationId and anchor from that handoff; it cannot mint/adapt a Location, substitute a test-fixture ID, or infer geography from legacy runtime locations.
- **Integration dependency:** the Ruin's stable identity and Location binding resolve through one factual Location/local-topology model.
- **Soft ordering:** broader Phase 8 civil-travel validation is not needed for this profile.
- **Architecture boundary:** the technical design specifies local containment/entry and the pre-start versus post-start mutation boundary; independent review passed for the recorded P10-A checkpoint candidate.
- **Product gate:** resolved only for the bounded Ruin profile above. No City, Market, population/NPC/economy, Passage/Route, Knowledge, activity, loot, encounter, construction, or other gameplay content is in scope.
- **P10-A implementation scope:** implement the minimal stable-owner/LocationId-neutral LocalTopology ownership/migration seam needed to publish the Ruin through normal authorities. P8-C's promoted `ExplorableSite` anchor contract is consumed; its current runtime owner resolution and LocalTopology owner references retain runtime-ID assumptions. Preserve existing City/Site consumers, and do not expand P8-C or generalize the adapter beyond this profile.
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
