# CORE-F - Daily-v1 .NET host technical design

**Status:** design candidate for independent review. This is a design-only artifact; it does not authorize implementation, change architecture, modify Phase 12, or close CORE-F.

**Core baseline:** `codex/core/canonical` at `032259b4177a24ee4c0c536919f65e052cef9572`, including promoted CORE-E. **Source identity:** the readiness inventory is anchored to `ad7a18899a13ec42a3d2b6b3098dff2f0a318492`. The intervening canonical history includes the bounded CORE-E extraction and its validation/evidence records, so the former statement that only AGENTS.md and Git authentication guidance changed is superseded. The P12 owner/census/codec source set is unchanged by CORE-E; recheck exact source blobs, writer paths, and effective values before implementation.

**Architecture handoff:** codex/architecture/world-identity-projection at 7166ed088483c69d37300b4f12528ebb4b27be84, including SIMULATION_CORE_F_READINESS.md and its owner/input inventories. The readiness inventory records production-source provenance at ad7a18899a13ec42a3d2b6b3098dff2f0a318492; its family keys must be checked against the current canonical source before implementation.

**Implementation status:** `WAIT_DEPENDENCY`. CORE-A through CORE-E have their bounded promotions. The promoted CORE-E `PersonId` and demographic-policy sources are available to dependent consumers. The selected Unity Daily-v1 continuation is complete and closed in Phase 12. This design does not claim equivalent .NET hosting, owner closure, or implementation readiness; the remaining selected owner, effective-definition, writer/epoch, and runtime-host closures are still absent or unproven.

## Current adoption note — 2026-10-14

The current user instruction confirms this technical design has passed independent
review while CORE-F implementation remains WAIT_DEPENDENCY. Its original submission
header above is historical. This architecture imports the production document at
Core `8ea3b47e9ab1be9ab3fe6261a73dc33fece8cd8f`; no executable source is imported.
The [reviewed prerequisite handoffs](../architecture/CORE_F_PREREQUISITE_MASTER_HANDOFF.md)
refine its WP1/WP2 into executable work, preserve its §4.3 descriptor-v2 contract
and clarify separate P9 effective versus P12 serialized-content identity inputs.
This adoption is not a claim that values, live owners or the .NET host now exist.

## 1. Objective and accepted boundary

CORE-F proves the already accepted UnityBootstrap-Daily-v1 continuation contract through an independent .NET host, using the same authoritative Core application and domain source as Unity. It covers effective-profile composition, ordinary supported Daily-v1 execution, eligible capture, durable save, detached load, staged restoration, restored-boundary admission, and deterministic continuation.

This is a same-host, same-build continuation proof. A .NET save is not promised to load in Unity, or vice versa. No old-save conversion, cross-runtime interchange, compatibility promise, public mod API, or new profile is introduced. The existing P12 wire format, owner semantics, cardinality, causal roots, admission protocol, and rejection behavior remain authoritative. A new host must identify itself truthfully in compatibility data; it must never fill Unity-specific fields with fabricated Unity values.

The selected authoring root is Assets/_Project/Data/Simulations/Simulation-DailyV1.asset. The accepted content/genesis lineage is the selected P9-A/P9-B authored bootstrap and authored geography. The current inventory reaches 39 authored assets: one simulation config, two cities, ten NPCs, twelve actions, four statuses, five jobs, and five items. These counts describe the proving fixture only. Effective values must be read after the same Unity asset-resolution and defaulting rules as the current bootstrap.

P10 Ruin/LocalTopology, P14 material flow, P18 intraday state, P19 extension/module state, external command queues, and P20 activity state remain outside this profile. Their absence cannot be inferred from a startup toggle or an empty list: each excluded conditional owner remains explicitly empty or not composed under the P12 contract, and populated unsupported state must be rejected. P12 is not reopened or widened.

The owner-family census remains the exact P12 contract. Its shape is:

    section count = 61 + 22*N + U + P + 4*C

N is the runtime NPC owner-key count, U the non-Person NPC residence-key count, P the Person life/residence key count, and C the City owner-key count. The supported set requires at least one NPC and City; dynamic families must have matching owner key sets, and U must refer to NPC keys. Required allows the owner's valid zero-row state; ExplicitlyEmpty requires exactly zero rows. The AliasGroup contract records actual shared owner-instance identity. Neither the initial fixture's count nor an aggregate total may replace those checks.

## 2. Current capability and gap classification

| Area | Current canonical capability | CORE-F status |
|---|---|---|
| Calendar, deterministic random/key primitives, logical timeline, market price kernel, finite-source kernel/runtime/Lab, and bounded demographic policy/PersonId | Promoted Core A-D plus bounded CORE-E sources in the shared package and SDK project | Reuse as-is. This proves those bounded contracts only; CORE-E does not provide population owners, lifecycle, or Daily-v1 hosting. |
| Daily-v1 image/header, owner codecs, exact census, atomic continuation store, detached restore coordinator, owner vector, thread/quiescence/epoch/lease/token admission | Implemented and validated for the closed Unity profile under Assets/_Project/Scripts | Existing behavior to preserve and migrate. It is not a .NET host. |
| Effective configuration and selected City/Item/NPC/job/action/status value graph | Unity ScriptableObjects are resolved by current bootstrap; the asset census records GUID/blob provenance | Missing pure immutable Core definitions and a parity-checked Unity-to-Core conversion. YAML blobs alone are not effective-value proof. |
| Full supported runtime owner closure | Unity runtime, domain authorities, owner providers, codecs and staged hydrators are present; many source files remain under Assets | Missing migration and single-source compilation of all admitted owners, writers, epochs, relations, and restoration constructors. |
| Host composition and environment | Unity bootstrap creates the graph; store already has explicit path/file-operation seams | Missing Core-owned application composition and .NET host adapter. Application.persistentDataPath convenience must stay in the Unity adapter. |
| .NET same-host save/load/restore/continue | No end-to-end equivalent host result exists in the cited Core state | Missing implementation and validation evidence. Existing Unity P12 evidence does not satisfy it. |
| Compatibility identity for a non-Unity host | Current P12 descriptor includes UnityVersion, RuntimePlatform, ScriptingBackend, optional BuildGuid, assembly MVIDs, numeric profile, content/provider/calendar and P8/P9 fingerprints | Missing truthful .NET identity mapping. The current descriptor fields are explicitly Unity-specific, so CORE-F adds a versioned compatibility-descriptor v2 with a typed host kind and host runtime identity; it binds the .NET runtime release, OS/process architecture, relevant Core/application assemblies, approved numeric profile, and the effective content/provider/calendar/P8/P9 identities. Keep descriptor v1 parsing and all Unity-v1 behavior unchanged; parse v1 as Unity identity only, and reject a v1 Unity descriptor as incompatible in the .NET host before graph construction or slot mutation. Unity-v1 readers reject v2 as unsupported without changing their existing slot. The outer P12 image/owner section codec and atomic storage contract remain the same; no old-slot conversion or cross-host loading is promised. |
| Cross-build/cross-host migration and future profiles | Not accepted by CORE-F | Deferred to CORE-X or a separately approved scope. |

Core A-D plus bounded CORE-E currently compile 14 shared runtime files from `Packages/com.simulation.core/Runtime` through explicit links in `src/Simulation.Core.csproj`, while Unity consumes the package assembly. That is the single-source model to extend. The full CORE-F closure is not established by counting files, searching for UnityEngine tokens, or compiling only an apparently engine-free subset. Its exact source closure is proven by explicit SDK compilation of every selected domain source and Unity compilation against the same package source.

## 3. Work package 1 - selected definition and owner-source closure

### 3.1 Effective Daily-v1 definition graph

The Unity bootstrap adapter remains the only reader of ScriptableObjects. It resolves the 39 reachable assets and creates a validated immutable Core definition graph before runtime publication. The graph includes the selected simulation configuration and all effective fields consumed by bootstrap or reachable behavior for CityData, ItemData, NpcData, NpcJobData, NpcActionData, and NpcStatusData. It includes the resolved calendar definition and P9-A/P9-B authored geography/genesis facts already admitted by the profile.

The graph uses stable semantic IDs and explicit references, not Unity object identity, scene references, asset paths, serialized YAML snippets, or ScriptableObject references. It preserves current field defaults, ordering, validation, numeric behavior, and relationships exactly. It rejects missing/duplicate IDs, dangling references, invalid values, unsupported populated definition families, and ambiguous order using the existing domain rules. It must not normalize, silently discard, or invent authored values.

The target Core value is one immutable effective definition set (name is descriptive, not a public API commitment) with deterministic ordered maps for config, cities, items, NPCs, jobs, actions, statuses, calendar, and admitted geography/genesis. Definition conversion records a canonical content identity derived from the resolved values and existing P9 provenance. The Unity adapter and fixed .NET proving input must produce equal effective definitions and equal identity for equivalent selected inputs. An omitted serialized field is covered by its post-resolution default value, not by the absence of a YAML line. The asset Git blob inventory is provenance only.

The current fixture reaches no TraitData, CapabilityAttributeData, ExplorableSiteData, LocalPlaceTypeData, LocalConnectionTypeData, or OrganizationData assets. They are not added to the profile. If a reached consumer has a compiled dependency on one of their types, the WP1 source closure must split the actual pure rule from its Unity authoring adapter or keep the consumer outside the admitted Core closure; it must not add engine stubs or treat an empty asset list as a semantic proof.

### 3.2 Owner families and current source anchors

The exact 89 family keys, role, shape, source path, and census declaration line remain in SIMULATION_CORE_F_OWNER_SECTIONS.csv. The rows below group those keys by authoritative owner closure and identify the source anchors and the migration obligation. They are not a replacement for the per-key implementation manifest.

| Closure | Census families and authoritative Unity source anchors | Required Core closure |
|---|---|---|
| Identity, causal roots, authored geography | p12c.runtime-identities.*; p12c.runtime-id-allocator.*; p12c.simulation-record-sequence; p8a.hexes/locations/scale-context; p12d.legacy-spatial-network.*; p9 genesis manifest. Anchors: RuntimeIdentity.cs, RuntimeIdentityCensus.cs, RuntimeIdAllocatorCensusProviders.cs, SimulationRecordSequenceCensus.cs, P12ACausalRootOwnerCodec.cs, P12CRuntimeIdentityOwnerSnapshot.cs, P9GenesisManifestContinuationSnapshot.cs, SpatialAuthority.cs, SpatialNetworkRuntime. | Preserve WorldId, IDs and high-water/counter state, sequence, deterministic source identity and continuation state, all selected Hex/Location/scale and P9 provenance, legacy network identities, and exact owner revisions/aliases. Genesis is not rerun on restore. |
| City, population, Person and genealogy | p12d.person.membership/materialization-binding; p12d.genealogy.parentage; p12b.city.important-npcs; p12d.city-population.*; p12b.npc-residence; p12b.person-life-residence. Anchors: CityRuntime.cs, SettlementPopulationRuntime.cs, PersonStore/PersonRuntime, Genealogy authorities, P12DDailyV1OwnerPackage.cs, P12DCityRootOwnerSnapshot.cs, P12ACityOwnerSetCodec.cs, P12APersonStoreOwnerCodec.cs. | Preserve aggregate state and receipts; Person/NPC membership, lifecycle, dates, residence, materialization bindings, genealogy edges, identity references and exact revisions. The promoted CORE-E `PersonId` and demographic policy are available to consumers. PersonStore, lifecycle, owner migration, and their complete writer/revision/export/hydration closure remain CORE-F work. |
| Per-NPC execution, economy, travel and knowledge | The 22 per-NPC prefixes in the census: explorable-site, local-topology place/connection, adventure-intel opposition/items/resources/access, commercial market/liquidity/share receipts, spatial locations/routes, travel state, inventory, money account, merchant/travel plans, local-observation and trade receipts, crime status, current action, and life state. Anchors: NpcRuntime.cs, NpcActionRuntime.cs, NpcDecisionSystem.cs, InventoryRuntime.cs, MoneyAccountRuntime.cs, MarketRuntime.cs, TravelSystem.cs, CommercialKnowledge.cs, SpatialKnowledge.cs, ExplorableSiteKnowledge.cs, LocalTopologyKnowledge.cs, AdventureSiteIntelKnowledge.cs, P12DNpcRootOwnerSnapshot.cs, P12ANpcOwnerDecoder.cs. | Preserve each NPC's stable key and all 22 family keys, per-owner revisions and alias groups, observation provenance/freshness, action/life/travel progress, account/inventory rows, plans and receipts. Each supported write path must be paired with its owner revision and enclosing runtime epoch. |
| Per-City economy/population | Four per-City prefixes: important NPCs, Market stock rows, population aggregate, and population operation receipts. Anchors: CityRuntime.cs, MarketRuntime.cs, SettlementPopulationRuntime.cs, P12DCityRootOwnerSnapshot.cs and P12DDailyV1OwnerPackage.cs. | Preserve CityId binding, exact row order/values, source receipts, population state, ownership aliases and revision facts. No new material-flow behavior is included. |
| Crime, justice and social appraisal | p12.crime-social-appraisal.outcomes/knowledge/reactions; p12b.justice-records; p12b.crime-p18-receipts; p12b.justice-p18-receipts. Anchors: CrimeSystem.cs, GuardSystem.cs, JusticeSystem.cs, P12EJusticeRecordsOwnerSnapshot.cs, P12ECrimeSocialAppraisalOwnerSnapshot.cs and their codecs. | Preserve all reachable result, reaction, knowledge, justice state and operation receipt relations, including terminal/duplicate behavior. Empty startup rows do not remove these Required owners. |
| Political, institutional, faction, property and estate | p12f.political-knowledge.holders; p12f.political-decisions.records; P12-E owner families for institution/office/faction/claims/support/property/estate. Anchors: PoliticalKnowledgeStore.cs, PoliticalDecision.cs, Faction/Institution/Office/Property/Estate authorities, P12E/P12F owner snapshots/codecs/packages. | Preserve IDs, relations, transitions, records and revisions even where the initial state is empty. Use actual owner authority, not a diagnostic projection. The accepted P12-G political binding cases are restoration fixtures, not new producers. |
| Military and conflict | p12e armed-force, contingent manpower, relevant Person references, spatial positions, conflicts, wars and battles. Anchors: ArmedForce, ContingentManpower, PersistentConflict/War/Battle owners and P12E military snapshots/codecs. | Preserve complete graphs, references, revisions, and validators, including valid zero states. Do not introduce new military gameplay or treat military as a profile toggle. |
| Commands, directives and commitments | p12f.actor-choice-inputs, p12f.scheduled-directives, p12f.travel-parties, p12f.expeditions; also the NPC occurrence-receipt family and linked NPC/owner rows. Anchors: ActorChoiceStore.cs, ScheduledDirectives.cs, TravelPartySystem, ExpeditionSystem, P12F owner packages and P12G restore coordinator. | Preserve terminal input disposition/sequence/idempotency, directive timing/order, travel/expedition progress and costs, all reciprocal references, and already-applied effects. Reject pending/deferred or consumed-awaiting-terminal inputs as required by P12. No external command queue is composed in this profile. |
| Explicitly empty conditional owners | Exactly the 13 p12c runtime identity site/local-place/connection/item families, p12d.explorable-sites, P8-B passage/barrier and crossings, P8-C City bindings and Person positions, P8-D route observations and Person route-plan history, NPC decision occurrence receipts, and keyed economy sale receipts. See the exact IDs in the census CSV. | Return a verified zero-row witness from the actual optional owner, or an exact not-composed result where the P12 contract permits it. Reject populated/unverifiable state. Do not substitute a missing provider for an empty owner. |

The census algorithm in P12AContinuationImage.cs is the authority for family roles, fixed keys, dynamic key sets, cardinality formula, and alias expectations. Existing P12ADailyV1ImageComposer, P12ADailyV1OwnerSectionEncoder, owner decoders/packages, and P12G restore coordinator are the behavior baseline. The CSV proves family declaration, not a complete live writer map.

### 3.3 Required writer, revision, and stage matrix

Before any owner is admitted to the .NET host, implementation must provide a row-level closure matrix for every fixed key and every dynamic family above. For each row, record:

1. Section key/family, schema, role, key source and exact cardinality.
2. One live owner instance/provider and its stable domain identity.
3. Exact immutable export/snapshot and revision source; whether the revision is owner-local or derived.
4. Every supported mutation ingress and the commit point where the owner state changes.
5. The invalidation/revision update for each successful commit, including direct owner entrypoints and nested operations; failed, rejected, rolled-back, and read-only paths must not claim a committed write.
6. AliasGroup ownership by object identity, not structural equality.
7. Codec, staged constructor/hydrator, validation dependencies, and post-stage binding/revalidation.
8. Cross-owner references, target identities, and the exact rejection if a reference is missing, duplicated, out of scope, or inconsistent.
9. Whether the family is Required, ExplicitlyEmpty, or not composed for this selected profile.

A local owner revision is not a runtime-wide epoch. The existing SimulationRuntime successful-operation epoch, owner-thread binding, single advance lease, same-attempt owner vector, capture eligibility token, and restored-continuation admission must remain one composed protocol. The migration is incomplete if a supported write can change an admitted owner without causing the source/target vector check to fail. P12-G closure and its owner/transition/rejection evidence are finite test inputs for the matrix, not permission to omit reachable domain writes.

The current architecture packet identifies high-risk mutation sources: direct SimulationTime/Runtime APIs; Person/NPC lifecycle and materialization; mutable City ImportantNpcs and NPC CurrentStatus lists; exposed Inventory, MoneyAccount, Market, Knowledge and political/military owners; crime, justice and social appraisal; directives, travel, expeditions, actor-choice dispositions; and nested transactions. Their existing guards, receipts, selective revisions, and diagnostic revisions are not a complete write map. Implementers must inspect each actual commit site against the current canonical tree; lexical scans and aggregate epoch counters alone are insufficient.

## 4. Work package 2 - same-runtime application and host seams

### 4.1 One authoritative application and runtime

Move the selected SimulationRuntime and every transitively required domain authority into the engine-independent package as one source of truth. Keep the current application semantics, operation admission contexts, owner-thread identity, mutation scopes/epochs, deterministic identity and record-sequence ownership, single serialized AdvanceLease, and failure/fault latches. Unity and .NET must instantiate the same application/runtime implementation. Do not create a simplified DailyV1SimulationRuntime beside SimulationRuntime, a second mutable store, or a test-only rule implementation that differs from production.

The composition boundary takes the validated immutable effective definition set, selected P9-A/P9-B genesis data, deterministic provider implementations, and a small host descriptor. It returns the initialized authoritative runtime only after all current genesis validation and initialization succeed. Failed composition publishes no WorldId or partially constructed graph. Restore preserves the saved WorldId and does not invoke genesis, allocation, ordinary action/command processing, random draws, or side-effect handlers.

The host seam is limited to values/resources the domain actually needs:
- resolved effective definitions and admitted genesis facts;
- a truthful runtime/build/numeric identity for the compatibility descriptor;
- explicit continuation storage root and the existing file-operation/atomic-replacement behavior;
- owner-thread attachment at runtime creation;
- logging/diagnostic sink only where a current domain boundary requires one.

No scene, asset path, UnityEngine object, Application static, wall clock, frame time, network, or environment-variable read enters Core. Avoid a general dependency-injection framework. Resolve engine imports by moving complete pure strongly connected closures or by splitting at a behavior-preserving adapter boundary. Preserve namespaces and identity semantics where that avoids unrelated consumers; do not add wrapper copies of authoritative state.

### 4.2 Unity adapter

Unity remains responsible for loading ScriptableObjects, resolving Unity serialization/defaults, converting them to effective Core definitions, selecting Application.persistentDataPath, and presenting the existing game UI. The Unity adapter calls the shared bootstrap/runtime and attaches the Unity build/platform/backend identity using the current descriptor meaning. The P9 genesis pipeline is consumed once during initial creation; restore hydrates its saved facts rather than rerunning it. The existing Unity v1 save slot and descriptor continue to work with the same Unity implementation and are not silently converted.

### 4.3 .NET proving host

Use a fixed net8.0 proving host under the already pinned SDK 9 validation toolchain, not a general scenario loader or public API. This is a proof environment, not a long-term host-support promise. It constructs the same immutable effective Daily-v1 definitions and genesis values as the Unity adapter, invokes the shared application/bootstrap, runs the same supported daily loop and admitted operation paths, and calls the same capture/codec/store/restore path. It accepts an explicit storage root and fixed ordered test inputs; it has no network, Unity dependency, wall-clock input, random source of its own, or alternate simulation logic.

#### Compatibility descriptor v2

The fixed .NET host uses a typed descriptor-v2 variant inside the existing P12 image envelope. The outer envelope remains major/minor `1.0`; owner-section encoding, digest, storage, and atomic-replacement semantics do not change. Descriptor v1 bytes, parser, writer, canonical ordering, and Unity behavior remain unchanged.

The dispatcher distinguishes v1 from v2 using the first little-endian `Int32`. A value other than `-2` is passed to the unchanged v1 parser. The value `-2` (`FE FF FF FF`) is the v2 discriminator: it cannot be a valid v1 `ProfileId` length because v1 strings allow only `-1` for null or a nonnegative byte length, and `ProfileId` is mandatory. Once `-2` is seen, the dispatcher never falls back to v1.

The v2 wire layout is fixed and has no optional fields:

1. `Int32` little-endian discriminator `-2`.
2. Exactly eight raw ASCII bytes `P12ACDV2`.
3. `UInt16` little-endian format version `2`.
4. `UInt16` little-endian reserved flags `0`.
5. `ProfileId`: mandatory length-prefixed string.
6. `ProfileVersion`: `Int32` little-endian.
7. `HostKind`: `UInt16` little-endian (`1` = .NET, `2` = recognized Unity foreign host).
8. `HostFlags`: `UInt16` little-endian reserved value `0`.
9. `RuntimeFrameworkDescription`: mandatory string.
10. `RuntimeVersion`: mandatory string.
11. `OperatingSystemDescription`: mandatory string.
12. `RuntimeIdentifier`: mandatory string.
13. `ProcessArchitecture`: mandatory string.
14. `TargetFramework`: mandatory string.
15. `AssemblyIdentityCount`: `Int32` little-endian.
16. For each assembly identity, in canonical order: `SimpleName`, `Version`, `Mvid`, each a mandatory string.
17. `NumericProfileId`: mandatory string.
18. `NumericProfileVersion`: `Int32` little-endian.
19. `ConfigurationFingerprint`: mandatory string.
20. `ContentFingerprint`: mandatory string.
21. `ProviderFingerprint`: mandatory string.
22. `CalendarId`: mandatory string.
23. `CalendarVersion`: `Int32` little-endian.
24. `CalendarFingerprint`: mandatory string.
25. `P9Fingerprint`: mandatory string.
26. `P8Fingerprint`: mandatory string.

Every v2 string uses the existing P12 wire framing: `Int32` little-endian UTF-8 byte length followed by strict UTF-8 bytes, no BOM. Null (`-1`) is forbidden in v2. A string is limited to 1 MiB and the complete descriptor to 64 KiB, so the complete-descriptor bound is the effective tighter limit. Assembly count is 1 through 4096. Profile, numeric-profile, and calendar versions are positive. All reserved fields must be zero. Parsing validates bounds and the full field set, then re-encodes and requires byte-for-byte equality. A short or malformed magic is `CorruptOrTruncated`; unknown format version, nonzero reserved field, or unknown host kind is `UnsupportedSchema`. These results are decided during detached header admission, before owner staging, graph publication, slot replacement, or runtime mutation.

Unity continues to emit and accept only descriptor v1. Its existing v1 parser and writer are not changed; the descriptor dispatcher recognizes the `-2` marker and reports `UnsupportedSchema` for that variant without altering the slot. The .NET host emits only v2 with `HostKind=1`. It may parse v1 solely to identify it as the existing Unity descriptor, then rejects it as `IncompatibleBuildOrRuntime` before staging. A v2 `HostKind=2` is likewise a recognized foreign Unity descriptor and is rejected as `IncompatibleBuildOrRuntime`; no cross-host load, conversion, or migration is supported. Unknown host kinds are schema errors, not foreign-host mismatches.

#### .NET identity source and normalization

The v2 .NET identity is captured from the running fixed `net8.0` host and contains no filesystem paths:

- `RuntimeFrameworkDescription` is `RuntimeInformation.FrameworkDescription`, trimmed and Unicode NFC-normalized.
- `RuntimeVersion` is `Environment.Version` formatted as four nonnegative decimal components (`major.minor.build.revision`); omitted build or revision components become `0`, with no leading zeroes.
- `OperatingSystemDescription` is `RuntimeInformation.OSDescription`, trimmed and Unicode NFC-normalized.
- `RuntimeIdentifier` is `RuntimeInformation.RuntimeIdentifier`, lower-case invariant ASCII and validated as nonempty hyphen-separated alphanumeric components.
- `ProcessArchitecture` is `RuntimeInformation.ProcessArchitecture.ToString()`, lower-case invariant ASCII.
- `TargetFramework` comes from `TargetFrameworkAttribute.FrameworkName` on the explicit .NET host entry assembly. The only accepted value for this proof is `.NETCoreApp,Version=v8.0`, normalized to `net8.0`; an absent or different target is incompatible.
- The assembly set comes from a fixed compile-time type-anchor manifest for the host adapter, authoritative SimulationRuntime and bootstrap/composition, P12 codec/store/staging/restore components, and every owner admitted by the selected Daily-v1 closure. It does not enumerate `AppDomain` or inspect assembly locations. For each distinct loaded assembly, `SimpleName` is `AssemblyName.Name` trimmed/NFC-normalized; `Version` is exactly four nonnegative decimal components with omitted components set to zero and no leading zeroes; `Mvid` is `ManifestModule.ModuleVersionId` in lower-case canonical GUID `D` format. Identities are sorted by simple name using ordinal comparison, then by four numeric version components, then by MVID using ordinal comparison. Duplicate full identities are rejected. The serialized tuple contains no path, location, or machine-specific directory.
- The numeric profile uses the existing P12 IEEE-754 probe and canonical little-endian hash framing: verify the exact binary32/binary64 positive-one and negative-zero bit patterns; write the existing numeric-profile identity string, backend string `CoreCLR`, empty build-GUID string, `Int32` assembly-MVID count, and the canonical sorted MVID strings using the existing P12 string framing; then use the existing `p12.numeric.ieee754-binary32-binary64/canonical-le-wire-v1/` prefix plus lower-case SHA-256 as the profile ID and version `1`. Paths and assembly names do not enter this existing numeric hash; the v2 assembly tuples independently bind the build identity.

#### Mismatch classification

After successful structural parsing and profile/schema checks, mismatches are classified deterministically:

- `IncompatibleBuildOrRuntime`: host kind; runtime framework description or version; OS description; runtime identifier; process architecture; target framework; any assembly simple name, version, or MVID; numeric-profile ID or version.
- `IncompatibleContentOrConfiguration`: effective configuration, content, provider, calendar ID/version/fingerprint, P8, or P9 identity.
- `UnsupportedSchema`: unknown v2 format version, nonzero reserved fields, or unknown host kind.
- Existing profile/schema result codes remain in force for an unsupported profile or profile schema. Malformed/truncated encoding and bound violations retain the existing corruption result.

Any mismatch rejects before graph publication, mutation, or slot overwrite. The host never maps a .NET identity into a pretend Unity version, platform, backend, or build GUID.

Storage continues to use P12AContinuationStore's explicit-path/file-operations seam, flush/atomic replace-or-move, bounded reads, inaccessible-versus-absent distinction, and one-slot guarantees. Keep Application.persistentDataPath convenience methods in Unity-only code. Do not introduce a second save format, lossy JSON, a parallel slot, or a .NET-only restore coordinator.

### 4.4 Restore and operation sequence

For each successful load:
1. Read the bounded slot bytes without changing the slot or active world.
2. Decode header, integrity digest, schema, limits, exact section set, descriptor and P9/P8 manifest identities before allocating owner graphs.
3. Create a detached private runtime graph through the same Core owner constructors/hydrators; restore causal roots and identities before the owners that reference them.
4. Stage owner groups in dependency order: definitions/genesis and identity roots; geography and legacy spatial graph; City/Person/NPC roots and lifecycle; domain owners (economy, knowledge, justice, political and military); command/commitment records and receipts; derived owner indexes/callback bindings.
5. Validate all row values, exact per-family key sets/cardinality, stable identities, IDs/high-water marks, aliases, cross-owner relations, role semantics, owner revisions, required zero cases, and explicit-empty/not-composed cases. Rebuild only indexes that are derived from restored authoritative owners.
6. Bind the fresh mutation guard and host owner thread only after the graph is internally consistent; then recapture the complete owner target vector and epoch on the same attempt.
7. Ask the existing SimulationRuntime restored-boundary admission path to admit only a private fully validated candidate with the saved positive completed sequence/day/WorldId. Preserve RestoredContinuation provenance; do not manufacture an advance from an idle state.
8. Publish exactly once after successful admission. On any failure, retain the prior active world and original slot byte-for-byte and expose the same failure category as the P12 contract.

All ordinary operations continue through the existing runtime command/day admission. A successful positive outer advance remains the only live-token issuance point. Failed, partial, throwing, faulted, stale-vector, wrong-thread, busy, or in-flight mutation states cannot become capture-eligible. The runtime lease is a serialized operation boundary, not a general cross-thread lock.

## 5. Work package 3 - parity and closure validation

Validation compares complete authoritative values and causal continuation, not console output, row counts, timing, or logs alone.

### 5.1 Effective-definition parity

For the selected 39-asset input graph, the Unity adapter and fixed .NET fixture must produce equal ordered effective definitions and exact configuration/content/provider/calendar/P8/P9 identities. Tests cover omitted/defaulted fields, duplicate and missing IDs, dangling references, unsupported populated definitions, numeric boundaries, ordering, and changes that alter the fingerprint. Confirm every effective value consumed by bootstrap and reachable Daily-v1 behavior is in the compared value graph.

### 5.2 Runtime and continuation parity

Run identical deterministic setup and supported inputs in Unity and .NET, and compare every owner section by stable key, schema, role, row values/order, revision, alias group, and owner identity where the contract records it. Exercise both required zero-row states and accepted populated P12-G restoration fixtures across the relevant owner families. Check actual changes after operations, not only initialization.

At positive completed boundary T, fork the test procedure (not the domain history): one branch continues uninterrupted; the other captures, saves, decodes detached, restores into a fresh host graph, and continues with the same ordered supported inputs. Compare all owner sections and causal roots at T, T+1 and T+2, including WorldId, day/sequence, allocator state, RNG/provider state, calendar, effective content identities, receipts, and applicable owner revisions. Repeat save/load/continue at later positive boundaries and verify parity after multiple round trips. Preserve the existing 49-case TEST_CASE_IDENTITY_MAP identities and add explicit identities for new tests without renumbering historical cases.

### 5.3 Admission, rejection, and atomicity

Test and assert unchanged active-world and storage bytes for every rejection:
- missing/inaccessible, truncated, corrupt, over-limit, digest-invalid, unsupported-schema, wrong-profile, descriptor/content/provider/calendar/geography/genesis mismatch;
- descriptor-v1 golden bytes and Unity parse/write behavior remain byte-identical; the `-2` discriminator cannot be accepted as a valid v1 mandatory `ProfileId` length;
- descriptor-v2 canonical round trip, strict UTF-8, exact field order, zero reserved values, string/descriptor/assembly-count limits, duplicate assembly rejection, and exact re-encode validation;
- unknown descriptor-v2 format version and unknown host kind return `UnsupportedSchema` before staging or mutation; a recognized Unity host descriptor returns `IncompatibleBuildOrRuntime` on .NET and descriptor v2 returns `UnsupportedSchema` on the v1-only Unity path;
- host/runtime/build/numeric/assembly identity mismatches return `IncompatibleBuildOrRuntime`, while selected semantic configuration/content/provider/calendar/P8/P9 mismatches return `IncompatibleContentOrConfiguration`;
- duplicate/missing/extra section, malformed dynamic key, wrong cardinality, duplicate identity, alias mismatch, invalid numeric/enum state, dangling owner link, unsupported populated owner, invalid empty-owner witness;
- wrong-thread, active operation/advance lease, failed or partial advance, faulted guard, stale epoch/owner vector, nonpositive boundary, invalid restored provenance;
- stage/hydration/binding/recapture/admission exception and injected internal owner failure.

A failed replacement must leave the currently active WorldId and every owner unchanged. A rejected save must leave the last valid slot bytes unchanged. A failed restore must perform no genesis, allocation, RNG draw, gameplay callback, command replay, duplicate effect, notification, or token issuance. Successful restored-boundary admission must have the same fresh owner identity/revision vector and epoch as the candidate validated during that attempt.

### 5.4 Required execution gates and provenance

At each executable integration boundary:
- run focused tests for the changed definition/owner/host closure;
- run the full CoreValidation suite cold and warm, preserving the source/test identity map and result accounting;
- run the standalone .NET host same-output/replay test;
- run affected Unity assembly/bootstrap/continuation suites, then ALL EditMode and official Smoke;
- run the existing causal LongRun gate if migration changes daily-loop outputs, ordering, or long-horizon state;
- run git diff --check.

Bind each result to exact code tree, test source tree, toolchain, invocation and profile identity. Use the promoted TRX-EVIDENCE-v1 packager for .NET cold/warm evidence; preserve raw originals under its privacy rules and independently verify package integrity/provenance. Existing Unity P12 evidence remains historical proof of Unity behavior; it is not relabeled as a .NET result. No heavy tests are part of this design-only work.

## 6. Integration and source ownership

Keep one writer on shared runtime, bootstrap, persistence, owner census, and common test harness files. Parallel owner closures only when their source files, package project links, owner schemas, and runtime hotspots do not overlap. Do not let concurrent workers edit the same owner or direct mutation boundary.

For each closure, move or split the actual strongly connected source set into Packages/com.simulation.core/Runtime with its Unity .meta identity preserved and add explicit SDK compile ownership in src/Simulation.Core.csproj. Core test coverage belongs in tests/Simulation.Core.Tests; Unity retains only engine adapters and integration tests. No duplicate authoritative file may remain compiled into both old and new ownership paths.

After a closure promotion, rebuild Unity and .NET from the same package source, recalculate direct and transitive owner dependencies, refresh exact canonical refs, and revalidate affected closures. Reviews are independent and exact-tip; each review covers the complete base-to-candidate diff plus all evidence identities. Do not use a clean checkout as a reason to lose protected test originals or manifests.

## 7. Dependency DAG and readiness

These are work packages within CORE-F, not new checkpoint IDs.

1. CORE-F design review of WP1-WP3.
2. Effective-definition closure and canonical composition primitives; reuse promoted Core A-E, including the bounded CORE-E `PersonId` and demographic-policy sources.
3. Individually closed owner/source groups, ordered by actual identity/reference dependencies from the WP1 row-level matrix. The current proposed order is: identity and admitted genesis roots; geography; City/Person/NPC roots; economy/travel/Knowledge; crime/justice; political/institution/property; military/conflict; terminal commands/directives/commitments and receipts. Adjust the order only when exact code references prove another topological order. The CORE-E prerequisite for `PersonId`/demographic-policy consumers is satisfied; unrelated owner closures remain independently gated by their own source and writer closure.
4. One serialized shared-runtime and environment integration window, including owner-thread/quiescence/epoch/lease behavior, compatibility descriptor, storage binding, codec reuse, and detached staged restore.
5. Two-host effective-definition and all-owner parity/rejection evidence with exact-source .NET cold/warm, Unity regression, Smoke and LongRun gates where applicable.
6. Independent implementation, evidence, and exact-tip review.
7. Bounded CORE-F promotion after current-canonical revalidation and all required review/evidence gates.

Hard prerequisites: reviewed current-base CORE-F design before implementation; promoted Core A/B for primitives; the already promoted CORE-E `PersonId`/policy sources for dependent consumers; closed P12 Daily-v1 semantics and exact owner contract; and each owner group's own complete source/writer/export/hydration closure before that group is admitted. The remaining PersonStore/lifecycle and other owner closures are not supplied by CORE-E. Shared runtime/persistence integration is serialized after those owner closures are ready.

CORE-D is reusable application/Lab precedent, not a hard dependency for all owners. CORE-C's finite-source kernel remains outside this selected P12 profile unless a current Daily-v1 consumer proves otherwise. CORE-X, P13 historical fork, P19 loader, P10 Ruin, P14 material flow, P18 intraday, future content growth, and cross-host migration are not dependencies of this CORE-F proof.

## 8. Human demonstrability

**Classification: NOT_MEANINGFUL_FOR_THIS_CHECKPOINT.** CORE-F here is the fixed infrastructure proof for same-runtime Daily-v1 composition, persistence, detached restore, and continuation. The .NET host is a deterministic validation fixture; this checkpoint adds no user-facing scenario authoring, interactive presentation, or separate supported scenario API. A separate human run would not establish additional correctness beyond the full owner/state parity tests and would suggest a product surface not included in the accepted scope. Automated tests, exact-source validation, and independent review are the appropriate closure evidence for this checkpoint.

## 9. Exit and remaining gates

This design can pass review only if reviewers confirm the exact selected profile, source/owner closure method, host identity boundary, same-runtime reuse, restore order, and validation obligations align with current canonicals. The design does not claim the 89-family live mutation/epoch matrix is already complete; that evidence is required before each corresponding owner is admitted. The architecture CSV inventories the contract keys but does not prove every write path.

Current implementation status remains `WAIT_DEPENDENCY`. The readiness handoff leaves CORE-F behind design/review, exact selected owner/definition/runtime closure, and evidence of complete supported writer and epoch coverage. CORE-E's relevant production dependency is promoted; it is no longer a pending gate. A design review PASS removes only the design gate. It does not create implementation readiness, authorize broader profile support, or change the current canonical Phase 12/13 statuses.
