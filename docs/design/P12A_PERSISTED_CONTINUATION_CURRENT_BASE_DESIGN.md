# P12-A — Current-base persisted continuation design

Date: 2026-10-12
Checkpoint: P12-A — UnityBootstrap-Daily-v1
Design base: P12 canonical `25d2dcf1371137ceb0e158eef4ba02d2e2304612`
Architecture readiness handoff: `e940714bcc05c0d790e0cb9a7f614ee6b493e21a`
Status: technical design candidate; independent review pending.

This is an additive, current-base implementation contract for the accepted P12-A scope. It supersedes only stale readiness, API, and profile statements identified below in earlier planning records. It does not amend architecture, change the accepted profile, reopen P12-B–G, or claim delivery. The authoritative semantic sources remain the current architecture, Phase 12 Brief, promoted Phase 12 State, accepted P12-A contract, and promoted B–G code.

## 1. Decision and scope

Implement persistence for the normal Unity bootstrap profile `UnityBootstrap-Daily-v1`, selected by `SampleScene` with `Simulation-DailyV1.asset`. It is same-build, same Unity runtime/platform and current-host numeric profile, and captures only a successful completed daily boundary. The selected profile includes P9-B authored geography and exactly its authored P8-A Hex, anchored Location, and scale context.

P12-B through P12-G and the selected live-profile inventory are promoted prerequisites within their recorded contracts. Their existing capture admission, typed snapshots/stagers, graph checks, target census, mutation guard and publication rules are retained. The missing work is the P12-A persistent byte envelope, compatibility gate, source-independent restore input, production lifecycle and durable save/load evidence. The old owner-coverage, live-inventory and separate-authorization sentences in historic Brief/State/design entries are not current blockers after the independently reviewed readiness handoff. This design does not assert implementation readiness until its independent review passes and the State records that transition.

The persisted profile includes all authoritative values and causally relevant roots identified by the current P12 C–F inventory and State. It preserves `WorldId`, logical day/boundary sequence, allocator and record-sequence state, RNG identity/state, P9-A/P9-B stage lineage and provenance, P8-A facts, D factual roots and person/population relations, E included core/daily owner state, and F Knowledge, directives, terminal P11 choices and supported commitments. It includes the selected profile’s fixed exact-empty witnesses. It preserves stable IDs and owner-local revisions where the owner contract requires them. It does not persist shared runtime epoch counters, object identity, context, guard, token, source vector reference, census stamp, process state, caches or indexes; the target recomputes/rebinds those.

The profile rejects, before live domain allocation, any unsupported composition or populated excluded state. Explicit exclusions include populated P8-B/C/D facts and any P8-E facts, P9/P10 generated content, P14 material-flow source state not configured by this profile, optional/uncomposed services, arbitrary injected providers, external WorldCommand queues, P18 timeline state, P19 module state, P20 activities, intraday boundaries, cross-build/host migration and P13 replay/history/fork guarantees. If P11 ActorChoiceStore is composed, retain its complete terminal history and idempotency sequence and reject in-flight dispositions exactly as the accepted contract requires. No gameplay, security/anti-tamper boundary, generalized serializer, migration framework, cloud service or new profile is introduced.

## 2. Immutable capture image and wire contract

### 2.1 Data boundary

Add an immutable `P12AContinuationImage` containing only owned scalar values, immutable value DTOs and deep-copied arrays/lists. No member may reference a Unity object, runtime/store/provider, simulation context, mutation guard, completed-boundary token, owner vector, census record, file handle or mutable collection owned by the source. Stable IDs are values. Capture does not serialize a diagnostic projection.

The source-side coordinator reads the healthy current admission and completed-boundary token on the simulation owner thread. It records the current token/boundary and logical owner-vector rows, obtains the promoted C–F owner snapshots, and rereads token, health and owner-vector rows before sealing. The before/after world, day, boundary sequence and every supported owner row (stable owner key, schema, role, cardinality, local revision and alias group) must match exactly; the existing P12-B protocol and supported mutation invalidation remain authoritative. A mismatch, active operation, unsupported composition, invalid token, faulted source, owner-thread violation, partial owner snapshot or unsupported owner value returns a structured failure and no image. Copy all owner data before releasing the boundary. Do not claim arbitrary concurrent mutation detection beyond promoted P12-B’s supported owner-thread/operation contract.

The byte encoder accepts only this sealed image. The decoder constructs inert DTO values and performs no Unity/domain allocation. Domain reconstruction begins only after complete byte, section and host-compatibility validation.

### 2.2 Version 1 canonical encoding

Use a small explicitly typed binary format; do not use reflection serialization, Unity object serialization, diagnostic JSON, or a package serializer.

All integers are fixed-width little-endian. UTF-8 is strict, no BOM, no Unicode normalization; invalid UTF-8 rejects. Floats/doubles are encoded as their IEEE-754 bit patterns, not formatted decimals. Decimal values, if present in an owner DTO, use the four 32-bit words of the .NET decimal representation and are validated by the owning codec. Enums use fixed-width unsigned 16-bit values and reject unknown members. Booleans are exactly one byte 0 or 1. Nullable fields use a one-byte presence tag; absent and present-empty are distinct. Strings and byte arrays use a signed 32-bit byte length, with -1 reserved only for a declared nullable value and 0 representing empty. Collections use signed 32-bit counts. Every owner field is written/read by an explicit versioned owner codec.

The outer frame is:

1. Eight ASCII magic bytes `SIMDLY1\0`.
2. Unsigned 16-bit format major (1), unsigned 16-bit minor (0).
3. Unsigned 32-bit header byte count and unsigned 32-bit section count.
4. The typed header, then section records in strictly increasing ordinal section-key order.
5. A 32-byte SHA-256 digest of all preceding frame bytes.

The header contains format/profile ID and version, `WorldId`, completed absolute day and boundary sequence, `SimulationSessionReportState.LastEconomySnapshotDay` as a signed 64-bit host-report value, selected P9 profile/stage contract identity and manifest fingerprint, the P8-A profile identity/fingerprint, and the compatibility descriptor in §3. `SimulationRecordSequence` and all other owner-authoritative C roots remain in their typed C sections. The host-report value is envelope metadata, not a census/provider section, so it does not alter the 299-row manifest. The frame has no wall-clock timestamp.

Each section record contains: canonical section key, section schema major/minor, declared role, stable alias-group key (or explicit none), row count, section-local revision where applicable, payload length, and explicit codec payload. Each dynamic row carries its stable owner key (for example the typed `NpcRuntimeId`/city/Person ID) and its owner-local revision. Duplicate keys, duplicate sections, invalid ordering, an alias group that disagrees with the reviewed inventory, or a row count/revision inconsistent with payload values rejects. Dynamic IDs are validated as the correct typed identity; no roster position or object address is an identity.

All required populated sections and all exact-empty witness sections appear exactly once. Required sections may legitimately contain zero rows only where the owner contract permits that. Exact-empty means the section is present with the current schema and zero rows; populated content rejects. Excluded sections are absent and are not silently ignored: if the selected runtime composition or input declares one, profile admission rejects. Missing required, unknown, duplicate, or unsupported sections reject under v1; there is no permissive unknown-field/section skip. A future schema or migration needs a separately reviewed version.

Canonicalize unordered sets/maps by their domain-defined stable typed key using ordinal comparison; preserve order for owner-defined sequences, tie-breaks, receipts, commitments and histories. Do not sort an ordered sequence to make it appear deterministic. The digest detects accidental corruption and inconsistent bytes; it is not authentication or anti-tamper protection.

Enforce bounds before allocation: complete frame at most 128 MiB, header at most 64 KiB, section count at most 4096, section payload at most 16 MiB, individual encoded string at most 1 MiB, and collection/row counts no greater than 1,000,000 and the owner’s stricter domain bound. Checked arithmetic precedes allocation. Exceeding any limit returns a limit error. These limits are format-v1 constants and must have exact boundary tests.

### 2.3 Owner and section mapping

The implementation derives the exact section manifest from the promoted Daily-v1 owner census, not from a hand-maintained second profile. The manifest records section key/schema/role, semantic owner family, dynamic key rule, alias group, and whether empty is exact or owner-permitted.

| Owner family | Persisted values and rules |
|---|---|
| C — causal roots | WorldId; logical time/calendar; all RuntimeIdAllocator counters; SimulationRecordSequence; built-in deterministic-random provider identity/version and continuation seed/state; P9-A and P9-B ordered stage IDs/versions/dependencies, profile/schema/fingerprint, seed source/value, selected authored inputs/outputs, provenance and first boundary; P8-A Hex ID/axial convention/coordinate/terrain ID+revision, Location ID+Hex anchor, scale convention/source/version/distance/unit. Preserve domain order and exact typed identities. |
| D — factual roots and relations | City, market, stock/account/inventory and population facts; configured NPC roster and mutable life/status/action/location/destination/inventory/account facts; Persons, genealogy, materialization/residence and aggregate relations; legacy spatial/site/route facts that the selected profile actually supports. Preserve distinct PersonId and NpcRuntimeId; never infer one from the other. Knowledge/commitments owned by F remain F sections. |
| E — included core and official daily owners | Current supported institution/office/tenure, property/estate, faction/claims/support/decision, justice/crime/social, force/manpower and conflict/war/battle owner facts and local revisions as enumerated in the selected census. Do not synthesize producers or gameplay. |
| F — knowledge and commitments | NPC Knowledge with provenance/freshness/observed revisions; directives and disposition/processed facts; complete terminal ActorChoice records, duplicate identity history and next sequence; travel/party/expedition and merchant commitment facts actually composed. Pending or in-flight actor-choice records reject. |
| Exact-empty witnesses | Exactly the 13 census rows listed below: four RuntimeIdentityRegistry subsections, `p12d.explorable-sites`, six P8-B/C/D subsections, and the two global keyed-receipt caches. P8-E and P10 LocalTopology have no expected Daily-v1 provider section and are not empty witnesses. |
| Excluded | Uncomposed owner families and future P9-generated/P10/P14/P18/P19/P20/external queue state. They have no section and cannot be dropped from a runtime that claims Daily-v1 admission. |

The canonical census at this design base is 299 sections for N=10 NPCs, C=2 Cities, P=0 Persons and M=0 Person-bound NPCs. Its cardinality rule is `61 + 22N + (N - M) + P + 4C`; the role obligation is Required even when a dynamic row’s current payload cardinality is zero. Its 61 fixed rows are 48 Required rows plus these exact 13 `ExplicitlyEmpty` rows:

- `p12c.runtime-identities.explorable-sites`, `p12c.runtime-identities.local-places`, `p12c.runtime-identities.local-connections`, and `p12c.runtime-identities.notable-items` (subsections of the identity registry; these are not a LocalTopology provider).
- `p12d.explorable-sites`.
- `p8b.passage-option-barrier-state`, `p8b.crossings`, `p8c.city-site-location-bindings`, `p8c.person-positions`, `p8d.spatial-route-observations`, and `p8d.person-route-plan-history`.
- `p12f.npc-decision-occurrence-receipts` and `p12e.economy-keyed-sale-receipts`.

The exact fixed Required ID set is: `p12d.person.membership`, `p12d.person.materialization-binding`; `p12c.runtime-identities.npcs`, `p12c.runtime-identities.cities`, `p12c.runtime-identities.locations`, `p12c.runtime-identities.routes`; `p12d.legacy-spatial-network.locations`, `p12d.legacy-spatial-network.routes`; `p8a.hexes`, `p8a.locations`, `p8a.scale-context`; `p12c.runtime-id-allocator.events`, `p12c.runtime-id-allocator.decisions`, `p12c.runtime-id-allocator.travel-parties`, `p12c.simulation-record-sequence`; `p12d.genealogy.parentage`, `p12f.political-knowledge.holders`, `p12f.political-decisions.records`; `p12.crime-social-appraisal.outcomes`, `p12.crime-social-appraisal.knowledge`, `p12.crime-social-appraisal.reactions`; `p12b.justice-records`, `p12b.crime-p18-receipts`, `p12b.justice-p18-receipts`; `p12e.armed-force.forces`, `p12e.armed-force.contingents`, `p12e.armed-force.relevant-person-references`, `p12e.contingent-manpower.states`, `p12e.armed-force-spatial.positions`, `p12e.conflicts`, `p12e.wars`, `p12e.battles`; `p12e.institution.records`, `p12e.office.records`, `p12e.office.incumbencies`, `p12e.office.tenures`; `p12e.faction.records`, `p12e.faction.affiliations`; `p12e.political-claim.records`, `p12e.political-claim.recognitions`; `p12e.political-support.relations`; `p12e.property.ownership`, `p12e.property.transfer-history`, `p12e.estate.records`; `p12f.actor-choice-inputs`, `p12f.scheduled-directives`, `p12f.travel-parties`, and `p12f.expeditions`. The source is `docs/design/PHASE12_OWNER_COVERAGE_INVENTORY.md`, section “P12-G current canonical Daily-v1 census”; the implementation pins its schema/version and verifies the exact full set against current promoted providers. P8-E has no expected provider section; its absence is neither a zero row nor an empty/excluded section. The P10 `LocalTopologyStore` and its indexes are not composed census rows. The two per-NPC `p12f.local-topology-knowledge.*` sections are Required P12-F Knowledge rows from the existing census; they do not admit P10 LocalTopology authority/content. The identity-registry `local-places` and `local-connections` rows remain identity-subsection witnesses only.

Dynamic rows are exact and derive stable keys from the owner identity:
- Each NPC contributes 22 Required rows: `p12f.explorable-site-knowledge`, `p12f.local-topology-knowledge.places`, `p12f.local-topology-knowledge.connections`, `p12f.adventure-intel.opposition`, `p12f.adventure-intel.notable-items`, `p12f.adventure-intel.common-resources`, `p12f.adventure-intel.access`, `p12f.commercial-knowledge.markets`, `p12f.commercial-knowledge.liquidity`, `p12f.commercial-knowledge.share-receipts`, `p12f.spatial-knowledge.locations`, `p12f.spatial-knowledge.routes`, `p12f.npc-travel-state`, `p12f.inventory`, `p12e.npc-money-account`, `p12b.merchant-trade-plan`, `p12b.npc-travel-plan`, `p12d.npc-local-observation-receipts`, `p12d.npc-merchant-trade-state-receipts`, `p12b.npc-status-crime-state`, `p12b.npc-current-action`, and `p12b.npc-life-state`. Add Required `p12b.npc-residence/{NpcRuntimeId}` only for an NPC not bound to a Person.
- Each Person contributes Required `p12b.person-life-residence/{length}:{PersonId}`.
- Each City contributes four Required rows: `p12b.city.important-npcs/{CityRuntimeId}`, `p12e.city-market-stock-rows/{length}:{CityRuntimeId}`, `p12d.city-population.aggregate/{length}:{CityRuntimeId}`, and `p12d.city-population.operation-receipts/{length}:{CityRuntimeId}`. A City operation-receipt row is Required and may be populated; it is not an empty sentinel.

Existing DTO and package seams to adapt (do not replace domain owners) are:
- C: `P12CContinuationRootStager.TryStageForRestore` over `P12CWorldIdentitySnapshot`, `RuntimeIdAllocatorSnapshot`, `SimulationRecordSequenceSnapshot`, `P12CSpatialAuthoritySnapshot`, `P12CP9GenesisManifestSnapshot`, and `DeterministicRandomRootSnapshot`. It currently requires `DailyCaptureStagingAttempt`; add a typed-image staging overload that builds fresh target roots without a source runtime/token.
- D: `P12DDailyV1OwnerPackage.TryCaptureAndStage`, with `P12DCityRootOwnerSnapshot` and `P12DNpcRootOwnerSnapshot` plus the existing Person/Genealogy/SpatialNetwork/Site snapshots. Split immutable export from target staging; preserve the single merged D/F NPC reconstruction and all current relation checks.
- E: `P12EDailyV1OwnerPackage.TryCaptureAndStage` and its typed Justice, Crime/Social, institution/office, property/estate, faction/claim/support/decision, force/manpower/spatial, Conflict/War/Battle snapshots. Split capture from staging while retaining unresolved E-to-F Knowledge bindings until F is staged and G resolves them.
- F: `P12FDailyV1OwnerCapture.TryCapture/TryStage`, `P12FStandaloneOwnerSnapshots.cs`, `P12FCommitmentOwnerSnapshots.cs`, and `P12FActorChoiceSnapshot`. Convert the current token/vector-bound capture into value-only image sections and add source-independent stage adapters; preserve the existing actor-choice terminal-state rules.
- G: `P12GDailyV1RestoreCoordinator.TryCreateRestoredSession` remains the authentic live-source path. Factor its private target composition and whole-graph checks into the shared detached-image path. Retain all G test hooks/invariants, exact target census, final boundary admission and the single `TesteSimulacao` publication owner.

For every row, the explicit owner codec persists exactly the values in the current owner snapshot/factory contract, including typed IDs, truth, revisions, reciprocal references, provenance, commitments, idempotency/terminal outcomes and allocator monotonicity. It must not infer unlisted fields from the diagnostic model. Caches and query indexes are rebuilt through their owner composition. Persist logical alias-group names and expected per-section cardinality/revision, never `OwnerInstanceIdentity`, source object handles or equality-by-reference. Rebuild the target census from fresh target instances and compare its logical rows/alias groups to the image before admission. If an included current DTO/factory leaves an enumerated value uncovered, identify the exact field and do not claim the affected adapter complete.

## 3. Compatibility before live allocation

Define `P12ACompatibilityDescriptor` from the current host and compare every field before constructing a target domain object. It includes:

- exact profile ID/version and selected `Simulation-DailyV1.asset` effective typed configuration digest;
- Unity version, runtime platform and scripting backend, plus a code-build identity computed from `Application.buildGUID` when present and the ordinal-sorted project assembly module MVID set (including the actual running player/editor compilation identity);
- the selected current-host numeric profile/version identifier, tied to the runtime/build identity and the supported IEEE-754 representation;
- official behavior-bearing content/definition catalog fingerprint, built-in provider/system IDs and versions, and exact effective configuration;
- calendar ID/version and complete effective calendar digest;
- P9-A/P9-B manifest/profile/stage fingerprint and P8-A authored geography profile/schema/fingerprint.

The descriptor is hashed from the same canonical primitive encoding and compared for exact equality. Display name or asset path by itself is not identity. The current host resolves definitions/providers only from the already-composed official catalog; no arbitrary provider injection is accepted. If any identity input cannot be derived or the host cannot prove an exact match, reject before live allocation. Do not add a hardware fingerprint, network dependency or secret. Compatibility is not a defense against the trusted local player modifying their own save.

## 4. Detached staging and production lifecycle

### 4.1 Source-independent restore

Add explicit APIs with a detached image boundary:

- `P12ADailyV1CaptureCoordinator.TryCaptureCompletedBoundary(..., out P12AContinuationImage, out P12AResult)`
- `P12AContinuationCodec.TryEncode/TryDecode(...)`
- `P12ADailyV1RestoreCoordinator.TryCreateRestoredSession(image, hostDescriptor, freshContext, out candidate, out result)`
- a typed section-manifest/owner-codec table sourced from the selected profile census.

The implementation may factor existing code but preserves these contracts:

1. Decode and validate framing/digest/limits/schema/complete section manifest and compatibility descriptor into inert values.
2. Confirm the target is a supported Daily-v1 composition and all excluded owners remain absent/exact-zero before allocating live domain state.
3. Create a private composition from current official definitions. Stage C identity/logical roots and P8-A first; D factual roots next; E owners and cross-owner relations in owner dependency order; F Knowledge and commitments after referenced roots. Use current owner-approved typed factories/stagers. Rebuild indexes/caches.
4. Replace source-bound C/D/E/F staging stamps with narrow typed-image adapters. D/E capture is separated from stage; F’s current source/token/vector capture stamp is never put in the image. Do not create a fake source runtime, token, census vector or capture context. Existing G live-source entry stays intact as a wrapper that captures its authentic image and calls the same internal typed staging/graph validator.
5. Reuse G whole-graph, exact-sentinel, P8/P9 lineage, current owner-vector and mutation-guard checks without weakening them. Validate IDs, cardinalities, aliases, required/exact-empty roles and reciprocal references; bind a new guard only after staged owners report bind compatibility. Rebuild fresh target owner objects and indexes; no source reference survives.
6. Derive a fresh restored admission context from the host’s original owner-thread context for every load (not from the previous session’s context). Bind a target-bound completed-boundary token for the same WorldId/day/sequence only after all validation passes. It must permit the accepted immediate post-load save boundary without an extra simulated day.
7. No genesis, identity allocation, daily action, directive, activity, command, replay, retry, replan, random draw or effect runs during decode/staging. Preserve saved WorldId and all allocators; no new WorldId is issued.

A successful image can therefore load after its originating session, token, guard, census and process have been discarded. The test must prove this by loading serialized bytes through the production detached path with those source objects unavailable.

### 4.2 Host startup, idle load, save and publication

The existing `TesteSimulacao.Start` establishes the true owner-thread context, then enters normal genesis through `InitializeSimulation`. Before genesis, check the single selected Daily-v1 slot under `Application.persistentDataPath`:

- no slot: run the existing genesis path unchanged;
- valid compatible slot: decode, stage and admit detached state, then publish once into the currently empty `activeSession`; do not run genesis or allocate a new WorldId;
- present but corrupt, incompatible, unsupported or otherwise invalid slot: leave the host unpublished and show a structured startup error; never silently fall back to genesis.

Expose one production host save/load route for the trusted normal game/UI caller. Save is available only on the owner thread at an eligible completed boundary and runs synchronously before another mutation can invalidate it. Idle load takes the existing serialized session gate, requires zero active operations and a healthy eligible host, stages privately, and swaps `activeSession` once only after target validation/admission. Failure preserves the current active session/reference/token and saved artifact. Startup publication uses the same owning publication method with expected outgoing session = null. Do not add another publication owner, generic lock framework, or test-only context factory.

Preserve `SimulationSessionReportState.LastEconomySnapshotDay` exactly in the typed envelope header field above because it affects observable host reporting. It is not a provider row and does not extend the census; do not assume it is derivable without proof. Runtime/context/token/guard state is always fresh. The current host has no long-lived per-session file/resource owner or disposal hook; after a successful idle swap and zero active operations the outgoing session is detached and collectible. If implementation discovery finds an actual owned resource, add only its narrow owner release at the existing swap and failure-test it.

Use one fixed local slot:
`Application.persistentDataPath/continuations/unity-bootstrap-daily-v1.simcont`.
Write a unique same-directory temporary file with create-new semantics; flush file data to disk and close it; reread and validate the complete frame; then atomically rename into an absent target or use same-volume atomic replace for an existing target. Never delete the old slot before replacement and never fall back to non-atomic overwrite. On any pre-replacement failure, keep the previous slot byte-identical and keep the active source session unchanged. Clean up only this attempt’s temp file. A failed autosave reports the failure and leaves the running world at its already-completed boundary; it does not claim to roll back the completed simulation advance. A stale temp file is not a save and is ignored. If the filesystem cannot provide the required atomic operation, return storage failure.

The single slot is the current proving route; do not add slot selection, cloud sync, save history, user account paths, migration or a new menu/product flow. File integrity is corruption detection, not security. Diagnostics contain stable error/section codes and relevant typed IDs, never absolute paths or account/home-directory text.

## 5. Failure/result contract

Use a closed result enum with stable categories: `NoSave`, `Saved`, `Loaded`, `WrongThread`, `Busy`, `CaptureRejected`, `UnsupportedProfile`, `IncompatibleBuildOrRuntime`, `IncompatibleContentOrConfiguration`, `UnsupportedSchema`, `CorruptOrTruncated`, `DigestMismatch`, `LimitExceeded`, `MissingOrDuplicateSection`, `InvalidOwnerValue`, `OwnerGraphRejected`, `AdmissionRejected`, and `StorageFailure`. Add a stable section code and typed stable owner key when available. Never include a local filesystem path in user-facing diagnostics. Internal logs may state a stable relative slot ID only.

Failures before publication do not alter the active session or saved artifact. Private partially staged objects are abandoned and never become observable. Capture/encode failure publishes no bytes. A failed replace preserves the old valid slot. Startup invalidity never turns into a new genesis world. No invalid value is defaulted, dropped, regenerated or semantically repaired.

## 6. Ordered implementation and validation

These are packages within accepted P12-A, not new checkpoint IDs or human gates. One owner owns the shared persistence/runtime hotspot; read-only review and test planning may run in parallel.

### Package 1 — typed image and codec

Implement explicit owner codecs for the census’s required and exact-empty section manifest; immutable deep-copied image; canonical encoder/decoder, digest and compatibility descriptor. Separate owner export values from G’s source-bound capture attempt. Tests cover deterministic byte equality/re-encode, exact typed fidelity (IDs, floats, RNG, allocators, provenance, knowledge, terminal inputs, commitments and revisions), null/empty distinction, ordered sequences versus sorted sets, all exact-empty witnesses, every limit edge and malformed/unknown/missing/duplicate/populated-excluded sections. No live owner objects are created by decode.

### Package 2 — detached stage, host and durable slot

Add the image-to-owner staging adapters by reusing C–F factories, G validators and target census. Add one atomic slot and structured failures. Integrate startup-before-genesis, idle replacement and save at a successful boundary in `TesteSimulacao` using the existing gate and single reference swap. Tests exercise real temporary files and production entry points; inject I/O failures only at a narrow file-operation seam. Verify invalid startup does not genesis; idle failures preserve both session and save bytes; success preserves WorldId/day/sequence, host report state and fresh target identities. Test with the source session/context/token unavailable.

### Package 3 — full continuation proof

At T, capture and save a boundary; run original and loaded copies with identical supported normal subsequent inputs through at least two further successful boundaries; compare every included authoritative owner section and causal outcomes at T and each next boundary. Repeat save/load cycles through the production host path, including loaded-session recapture. Verify no genesis, duplicate command/directive/action/commitment effect, replay, replan or extra RNG/identity allocation during load. Include startup and idle replacement, corrupt/incompatible inputs, source/target preservation and later valid retry.

Run focused new suites, all affected P12 C–G and owner/domain regressions plus affected Phase 5–8 suites, ALL EditMode, complete official Smoke and `git diff --check`. Use the promoted validation wrapper/packager and current Validation Evidence Policy: preserve protected raw results, bind result bundle and input/source identities to the exact executable and test trees, sanitize incidental local paths without changing results, verify the deterministic package. Do not reuse old Unity evidence as validation for new code. LongRun is required only if the implementation changes daily-loop/long-horizon semantics; this design keeps the established daily loop and adds persistence at its completed-boundary host seam. No Unity run is required for this docs-only design.

### Hotspot order

The P12-A implementation owner serializes edits to `SimulationRuntime`, C–F staging factories, G composition/graph validation, `TesteSimulacao`, profile composition and admission tests. Implement the typed codec/immutable image first; then source-independent staging; then host/storage publication; then integrated parity. Do not simultaneously modify these paths on another branch. After each package, refresh remote canonical and classify upstream changes; revalidate affected seams before proceeding. The existing protected ProjectSettings changes and untracked `.meta` files are outside this worktree and must remain untouched.

## 7. Readiness transition and limits

The architect handoff `P12A_CURRENT_READINESS_MASTER_HANDOFF.md` and its exact-base independent review at architecture candidate `e940714bcc05c0d790e0cb9a7f614ee6b493e21a` mark prerequisites satisfied within scope and P12-A `READY_FOR_TECHNICAL_DESIGN`. They explicitly supersede the old claim that owners/B–G/live inventory are still prerequisites. This design now resolves the implementation boundary; it does not mark itself independently reviewed.

After independent design review passes against the exact design commit and its cited current P12/architecture bases, append a factual P12 State record: design PASS, all required included owner adapters confirmed available (or exact finding), P12-A `READY_FOR_IMPLEMENTATION`, and implementation authority supplied by the current user instruction to continue through implementation after design review. Then continue to implementation without another routine approval request. If review finds an actual unsupported included owner field/factory or unresolved canonical semantics, stop only the affected seam, record its exact gap and continue other authorized work.

P12-A completion requires successful persisted save/load and exact multi-boundary parity on the production lifecycle, final exact-tree code review/validation and a canonical State record. This design and any readiness record do not close Phase 12. P12-B–G remain promoted within scope; P13 remains blocked by its separate historical guarantees; Phase 12 stays open until its separate formal closure gate.

## 8. Evidence sources

- P12 canonical source/State base: `25d2dcf1371137ceb0e158eef4ba02d2e2304612`.
- Architecture readiness assessment and independent PASS review: `e940714bcc05c0d790e0cb9a7f614ee6b493e21a`; architecture base `f3077ac5decbbb726a7e3ab80fc098fa4a8689fb`.
- `docs/EXECUTION_MODEL.md`, `docs/phases/PHASE12_BRIEF.md`, `docs/PHASE12_STATE.md`, accepted `docs/design/PHASE12_TECHNICAL_DESIGN.md`, owner inventory and P12 capability decomposition.
- Promoted P12-B–G code, State, selected Daily-v1 inventory/census and P12-G closure matrix at the design base; current `Assets` tree `a61dad9e1755c28c87fc3f7e01e6e30bb1f9964f`.
- P8–P11 profile inputs and current code; P9-A/P9-B manifest/profile provenance; P11 terminal actor-choice contract.
- `docs/architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md` and `docs/architecture/MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md`: both reviewed for current constraints. This profile remains daily and excludes their conditional temporal, extension and multi-participant payloads.
- Host/runtime audit: startup restore must precede genesis; restore context is freshly derived from the host root context; nullable-session publication must reuse the existing gate; report day is preserved; no current session-owned resource disposal is present.
