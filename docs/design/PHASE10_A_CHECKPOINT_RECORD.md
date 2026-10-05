# P10-A — Ruin LocalTopology Genesis Composition

**Record status:** the user-approved bounded P10-A scope and technical boundary
passed independent review. P10-A is `READY_FOR_IMPLEMENTATION` under this
record. It does not promote a runtime capability or close Phase 10.

## Identity and baseline

- **Checkpoint ID:** P10-A
- **Name:** Ruin LocalTopology Genesis Composition
- **P10 design baseline:** `codex/phase10/P10DesignRefresh` at
  `78c617d959662940b7c14b9c8115dd5917397976`.
- **Reviewed integrated docs candidate:**
  `codex/phase10/P10ARecordIntegration` at
  `345dcbc8f05e0d64fed1b058d30f08ad0be8937d`, which combines the refreshed
  design and record on current P9 canonical State/status tip `14a2e8e`.
- **Architecture baseline:** `codex/architecture/multi-participant-activities`
  at `c285466c355103d3637ac165246591b72eb7bda0`.
- **Intraday/extensibility alignment:**
  `../architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md`, consolidated at the
  architecture baseline above.
- **Multi-participant alignment:**
  `../architecture/MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md`, originating at
  `4b6dd1d38cffeaf3cc1ac3effea0f8ede8771194` and included in the current
  architecture baseline above.
- **Promoted capability baselines:** P8 canonical at
  `470667d37863384edadb3d93ef64d8004aff46a3`; P9-B capability code at
  `d9a62d7c6bea242653c2d68cc0a70911bb5ed1bf`, with the current P9 canonical
  State/status tip at `14a2e8ee01e49f1ffdcecf8fd64d274c728ead44`.
- **P10 canonical implementation baseline:** none exists yet. Begin isolated
  implementation from the reviewed docs candidate above; refresh these refs if
  an upstream canonical tip advances before implementation/integration.
- **Independent technical review:** **PASS** on exact integrated docs
  candidate `345dcbc8f05e0d64fed1b058d30f08ad0be8937d`. The review confirmed the
  P8-C runtime-owner mapping, selected P9-B profile handoff, current P9 status
  reference, scope boundaries and validation obligations.
- **Architecture assumption status:** current architecture `c285466`, P8
  canonical `470667d`, P9-B code `d9a62d7`, and P9 State/status tip `14a2e8e`
  were confirmed current and compatible by that review.

The scope approval is the user's explicit decision for one
`ExplorableSiteKind.Ruin` at a Location supplied by promoted P9-B, with only
its stable site identity/binding and a small finite LocalTopology proving
fixture. The user also approved the minimal LocationId-neutral topology-owner
and migration seam in this checkpoint, including the semantic-to-runtime
resolution needed for P8-C's composed-runtime validation. The approval does
not extend the scope below.

## Accepted scope and closure evidence

Compose **exactly one** domain-owned Ruin at the actual existing canonical P8
`LocationId` selected by the geography-enabled P9-B profile. Consume the P8
Location/anchor and P9 genesis/provenance through the normal P9 bootstrap
handoff. P10 does not create, replace, adapt, or infer a regional Location.

The P10-A proof profile contains the Ruin's `ExplorableSite` identity and
binding plus the minimum finite LocalTopology facts: semantic places, an entry
point, containment only if needed, and explicit local connections. Use the
small deterministic `Entrance → Courtyard → Inner Chamber` fixture to prove
the composition path. These fixture names and values are proof data, not a
universal Ruin schema or a promise of generated layouts.

Closure evidence must show all of the following:

1. The selected P9-B profile supplies the exact P8-owned `LocationId` and
   anchor consumed by P10; selected-profile composition produces one Ruin at
   that same Location before the first simulated boundary.
2. A stable semantic owner keyed by the Ruin `DefinitionId` and canonical
   `LocationId` resolves to exactly one current site `RuntimeId`. The P8-C
   `SpatialAnchorOwnerId.Value` compatibility value resolves through
   `ExplorableSiteStore.TryGetByRuntimeId`; no runtime ID enters semantic
   topology identity.
3. The bounded fixture's places, entry, any required containment, and explicit
   connections form a valid finite topology. Stable, namespaced/versioned IDs
   are injective and independent of source order, display names, and runtime
   allocation. Missing, duplicate, colliding, incompatible, or unresolved
   references fail before publication.
4. The LocationId-neutral `LocalTopology` owner/store seam and semantic-to-
   runtime resolver publish with the Ruin candidate atomically through normal
   domain authorities. Existing City/Site RuntimeId owner handles, anchor
   behavior, and topology factory/consumer paths continue to work.
5. P10 adds exactly one internal built-in genesis stage with declared identity
   and version, typed output schema, stable output owner, and explicit P9
   authored-world/authored-geography dependencies. Validation covers the
   combined selected profile and its deterministic stage order before the
   existing genesis publication boundary; failed composition leaves no partial
   live world.
6. Preserve the selected P9-B geography-profile fingerprint and provenance as
   distinct evidence. Combined provenance records P10 stage/version, dependency
   order, output owner, selected Ruin/Location IDs, fixture inputs/results,
   effective configuration/calendar and the P9 root-seed/random provenance.
   P10 consumes no local randomness and does not rerun genesis.
7. Initial-generation privilege ends at the first simulated boundary; later
   site/topology changes require ordinary domain mutation authority.

## Ownership and explicit exclusions

P10-A owns only this Ruin profile, the narrowly required
LocationId-neutral `LocalTopology` owner/store migration, the stable semantic
owner and its `DefinitionId`/`LocationId`-to-runtime resolution, the single
internal P10 stage, and selected-profile pre-publication validation and
provenance integration. Preserve P8-C compatibility behavior; do not broaden
or retroactively expand P8-C.

Out of scope: City/Market, population/NPC/economy, regional Passage/Route,
Knowledge, activities, loot, encounters, construction, runtime expansion,
rendering authority, generalized layout generation, backstory, local random
generation, persistence/replay/fork delivery, existing-world retrofit,
universal WorldEntity, generic contributor registry, public Mod API/loader,
and speculative mod infrastructure. P19 loader/API and retrofit work remain
deferred to their documented phase. No timed work, ActivityInstanceId,
participant, role, reservation, or participant-cardinality facts are created;
P18 and P20 are not dependencies. Intraday/extensibility and multi-participant
alignment constraints remain review constraints if later scope changes.

## Dependencies and readiness gates

Required promoted inputs are P8-A Location/anchor truth, P8-C's ExplorableSite
kind and legacy anchor contract, and P9-B's authored geography profile plus
genesis publication handoff. P9-A provenance is retained as lineage where
applicable. P8-B/D/E, P18, P19, and P20 are not implementation dependencies for
this bounded profile.

P10-A is **READY_FOR_IMPLEMENTATION** after the independent review PASS on
exact docs candidate `345dcbc8f05e0d64fed1b058d30f08ad0be8937d` recorded above.
No implementation, integration, validation, or runtime promotion is claimed by
this record. Implementation still requires its own isolated work branch,
independent code review, integration, the applicable validation, and a separate
human promotion gate.

## Validation plan

This checkpoint-record change is documentation-only. Its current gate is
`git diff --check`; Unity tests do not apply to the record itself.

Implementation validation must include focused tests for:

- actual selected P9-B profile composition through
  `SimulationBootstrapComposition`, verifying its authored Hex, anchored
  Location, spatial authority, and P9 fingerprint/provenance are the inputs
  consumed by P10;
- exactly one Ruin and the Entrance → Courtyard → Inner Chamber local profile,
  including stable identities across source reordering and different runtime
  allocations;
- unique DefinitionId/LocationId semantic ownership resolving to one site
  RuntimeId, plus composed-runtime validation of the legacy anchor value via
  `ExplorableSiteStore.TryGetByRuntimeId`;
- existing P8-C City/Site anchors and LocalTopology compatibility handles;
- deterministic stage identity/version, typed outputs, declared dependencies
  and order, provenance/fingerprint preservation, and no local RNG use or
  genesis rerun;
- rejection-before-publication and no partial state for invalid IDs, missing
  anchors, duplicate/colliding inputs, invalid topology/references, or failed
  combined-profile validation; and
- the pre-start boundary and ordinary authority for later mutations.

Run the focused LocalTopology, spatial/site, P9 bootstrap/profile and
composition suites, then the current required full EditMode and complete Smoke
promotion gates, plus `git diff --check`. Reassess whether long-run validation
is needed if implementation changes daily-loop or long-horizon behavior.
Selected-profile composition is mandatory; prepopulated test authorities alone
do not satisfy the gate.
