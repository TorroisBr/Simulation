# P12-C — Identity, Genesis Provenance, and Deterministic Roots

**Status:** Documentation-only technical design candidate. It requires an
independent exact-tip review. It defines no implementation readiness and does
not establish P12-A readiness.

**Design base:** P12 planning/design tip
`8db8cfc0096c0a2bc05981e7577da048441f65e4`.

**Authority reviewed:** architecture `c285466c355103d3637ac165246591b72eb7bda0`;
P8 `470667d37863384edadb3d93ef64d8004aff46a3`; P9 State/closure
`82396ae7ffaf407fda278928da456b06dc5394d4` and P9-B code integration
`d9a62d7c6bea242653c2d68cc0a70911bb5ed1bf`; P11 State
`308e24d0744112e8f2b741521b8b3e4acb51ebbf` and actual code promotion
`0803670cfa2c39163b54ff46a21daa06df5a16f6`; P18 State/canonical
`ba8076c3bc2c8c354a8755e6efaca30bfeab7bf7`; and the intraday/extensibility
and multi-participant activity alignment records. The proposed P9-B/P11 additive
composition is candidate `af656e7710fce0ba171fae1d6684331d2dc0b743`, not
canonical. Its independent code review passed. The refreshed P12 owner/profile
inventory `ba6f79fb87e851316be84d4f2a89d94186b8f802` completes a local audit of
that exact candidate's selected composition and configured providers, and
records its selected-profile validation results. The audit did not fetch
remote refs, and this candidate is still unpromoted; it is candidate-based
composition evidence, not canonical live-composition evidence for P12-A.

P12-C is accepted as prerequisite capability work under the P12 Brief and
capability decomposition. Its dependency is the reviewed P12-B profile
admission/completed-boundary contract. The accepted profile remains
`UnityBootstrap-Daily-v1`. This design specifies private staging of identity,
P9/P8 provenance, and verified deterministic roots only; it is not a save
envelope, public serializer, restore coordinator, or profile integration.

## 1. Contract boundary

P12-C captures causal roots that later P12-D/E/F owner hydrators refer to. It
must restore original identities and counters, never allocate replacement IDs,
rerun genesis, or infer hidden state from diagnostics. Every export is an
immutable value snapshot issued by its owning authority. Every hydrator builds
an unpublished private candidate and either validates it completely or
returns a typed rejection without mutating the active runtime.

The identity concepts stay distinct: definition IDs, persistent `PersonId`,
loaded `NpcRuntimeId`, `HexId`, `LocationId`, generated runtime IDs, and
Unity/object references are not interchangeable. In particular, the selected
P8-A geographic IDs are typed authored IDs, not values allocated by
`RuntimeIdAllocator`.

## 2. Owner sections and value contracts

Names below describe contract roles; implementation names should follow the
existing repository conventions.

### 2.1 Runtime ID allocator

`RuntimeIdAllocator` owns one independent next-sequence/high-water value for
each supported generated ID family. Its immutable section contains a schema
identity and the exact next value for every family present in the source
allocator, including families unused at the capture boundary. Current source
families include NPC, City, Location, Route, Event, Directive, Decision,
TravelParty, Organization, ExplorableSite, Expedition, LocalPlace,
LocalConnection, and NotableItem. The refreshed live composition census must
confirm this inventory against the exact supported source; a future family
cannot be silently omitted under an old section schema.

The allocator owner provides the snapshot and a private factory/hydrator. It
does not accept a caller-provided counter map as mutation of a live allocator.
Hydration validates known schema and exact family set; every next value is
positive and representable; no value overflows its allocator's next
allocation; and the restored runtime IDs in other owner sections are unique
across the registry's actual global uniqueness domain. Preserve gaps: the
counter is not reconstructed as `max(present IDs) + 1`, because consumed IDs
may no longer be represented in a store. Restore the exact counters without
calling any `Allocate*` method.

The allocator snapshot is not an identity registry snapshot. The relevant
owner for each instantiated entity remains authoritative for that entity's
typed identity; P12-D/E/F restore those owner records and later graph validation
cross-checks their runtime IDs against the restored registry and allocator.

### 2.2 Shared simulation record sequence

`SimulationRecordSequence` is shared by the configured recorders that use it;
it is not a sequence per event store or per decision store. Its immutable
section captures the exact next sequence value. A private staged factory
restores it without allocating a record.

Reject non-positive or exhausted/overflowing values, duplicate record
sequences, non-positive stored sequence values, and a next value that is not
strictly greater than every sequence owned by the P12-D/E/F record sections
that share this sequencer. Validation is against those actual owner exports,
not `History`, diagnostics, or a guessed maximum from one store. If live
inventory finds another sequencer in the selected runtime, it requires a
separate typed section and cannot be folded into this counter.

### 2.3 Selected P9-B genesis manifest and lineage

The genesis owner exports the already-produced P9 manifest as immutable
historical provenance. The section preserves:

- P9 profile contract identity `unity-authored-bootstrap/authored-geography-v1`
  and its schema version, plus its fingerprint and canonical provenance
  records;
- inherited P9-A stage identities and the P9-B stage
  `p9.genesis.authored-geography/v1`, each compatible version, declared input
  and output identities, and dependency edges;
- the actual ordered stage lineage from the P9 manifest, including resolve
  profile, authored world, authored geography, authored actors, profile
  validation, and publication;
- the authored input identities and exact published output identities, with
  the seed value/source, effective configuration identity, resolved calendar
  identity/version, and first simulated boundary `advance-day:1`;
- the identity/version of the bootstrap profile and genesis implementation
  needed to identify compatible interpretation of that historical output.

The accepted P9 pipeline currently builds the manifest from resolved authored
configuration and computes its fingerprint/provenance during genesis. P12-C
captures those values after genesis. Hydration validates their declared
structure and references but does not call `SimulationGenesisPipeline`, rerun
any stage, recreate output facts, or substitute current authored assets for
the historical records. P12-B remains owner of exact profile admission and
effective configuration/calendar compatibility; C binds its references and
captured genesis values to the admitted profile fingerprint rather than
duplicating an independent configuration authority.

Reject unsupported profile/schema/version, empty or mismatched fingerprint,
unknown or duplicate stage IDs, dependency cycles/missing predecessors,
stage input/output or lineage inconsistencies, incompatible seed source,
calendar/configuration cross-reference mismatch, malformed provenance, or a
first-boundary value inconsistent with the profile contract. Preserve the
exact source/output records even if the compatible asset changes later.

### 2.4 P8-A geographic roots

The P8 `SpatialAuthorityStore` is the authority for this section; legacy
`SpatialNetworkRuntime`/City locations are not substitutes. The accepted
P9-B profile requires exactly:

- one `HexRecord`: `HexId("hex/sample-origin")`, axial coordinate `(0,0)`
  with convention `axial-hex-v1`, and terrain definition/revision provenance
  `terrain/sample-plains` / `sample-world-v1`;
- one `LocationRecord`: `LocationId("location/sample-origin")`, anchored to
  that exact `HexId`;
- the authored `SpatialWorldScaleContext` and all of its stable provenance
  values: convention, source identity/version, distance per neighbor step,
  and unit, as declared by the selected genesis profile.

Export the exact owner records and typed references. Private hydration checks
one-of-each cardinality, exact profile IDs and coordinate/convention, terrain
identity and authored revision, valid positive scale and complete source
provenance, and the Location-to-Hex anchor. Reject missing, duplicate, extra,
null, malformed, unsupported, or dangling facts. The current selected profile
does not include P8-B through P8-E; those remain explicit empty/excluded
sections under P12-B admission and are not invented as P12-C data.

## 3. Deterministic random roots: known contract and unresolved coverage

The pinned built-in provider is `DeterministicRandomSource` under the accepted
current-host profile. The source seed is already recorded by P9 genesis
provenance and effective profile admission. `NextUnit(streamKey, drawIndex)` is
a pure keyed draw for a fixed seed/key/index; that call has no mutable source
cursor to serialize. By contrast, `CreateStream(streamKey)` returns a
`DeterministicRandomStream` with mutable `DeterministicRandomState.DrawIndex`.
Any retained stream cursor that can affect future supported execution is
causal state and must be included with exact stream key and draw index.

The refreshed candidate inventory identifies the selected configured provider
graph: legacy travel, travel-party, expedition, scheduled-directive, justice,
and NPC-decision systems are composed; the selected effective configuration
also enables merchant trade/commercial Knowledge sharing, crime infrastructure
and its action provider, guard action provider, and CityRuntime economy work.
Natural mortality and aggregate demography are disabled by the selected
asset's default overrides. The fixed-seed toggle is false, so this bootstrap
constructs `DeterministicRandomSource(0)`; the serialized seed field alone is
not a representation of live stream cursors. This exact provider-composition
audit improves the source basis but does not finish the P12-C RNG section: it
has not proved the exhaustive set of mutable stream instances retained by
those consumers, their lifetime/ownership, or whether any cursor remains
causally live at an eligible completed-day boundary. The random API has no
source-level registry that enumerates all live streams. Therefore this design
does **not** declare seed-only capture sufficient, does not claim complete
draw-state coverage, and does not authorize adding a speculative global
random-stream registry.

Before implementation closes this section, the live owner inventory must map
each actual random consumer in the validated profile to one of:

1. a keyed draw whose full key and draw-index inputs are reconstructed from
   other exact owner state at continuation;
2. a retained mutable stream with an owner-issued export and private staged
   hydrator for its `Seed`, `StreamKey`, and exact `DrawIndex`; or
3. an owner section proven not composed or proven to retain no cursor across
   an eligible boundary.

The mapping must be exhaustive and tested at owner boundaries. Unknown
consumer, custom provider, missing stream owner, duplicate/conflicting stream
identity, negative draw index, or a cursor that cannot be restored to the same
next draw rejects P12-C/profile admission. P12-C may implement seed/provider
identity and verified stream owners after the exact inventory is complete;
until then, deterministic-root coverage is a hard readiness blocker. This is
an evidence gap, not a product or architecture choice.

## 4. Composition and dependency order

P9-B bootstrap and P11 ActorChoice were historically separate code lines. The
additive candidate `af656e7710fce0ba171fae1d6684331d2dc0b743` has independent
code review, and refreshed inventory `ba6f79fb87e851316be84d4f2a89d94186b8f802`
audits its exact merged composition and selected provider graph. The inventory
records bootstrap 14/14, ActorChoice 24/24, Spatial 99/99, ALL EditMode
1742/1742, official Smoke 5/5, and `git diff --check` passing on the exact
candidate; only the final Smoke XML remains available as a retained result
artifact. These results validate composition behavior, not P12 owner exports,
hydration, random draw-state coverage, or profile readiness. No remote fetch
was performed for the inventory, and the composition candidate is not
promoted. P12-C implementation must target the exact composition only after
the applicable current-source evidence is refreshed; it must not treat
P9-B or P11 in isolation as the supported runtime.

Dependency order within P12-C:

1. Resolve and validate the exact P12-B admitted profile and owner section
   census for this runtime.
2. Export allocator counters and the shared record sequence from their owning
   instances.
3. Export genesis manifest/lineage and P8-A facts from their owners; validate
   the profile references, exact cardinalities, and cross-section identity
   consistency.
4. Export each deterministic root/cursor from its verified consumer owner and
   prove exact next-draw equivalence. If inventory is incomplete, reject this
   profile candidate rather than publish a partial section set.
5. Construct each private staged owner candidate in dependency order without
   binding it to the active runtime. Return typed validation results for P12-G
   whole-graph validation; do not publish a runtime here.

P12-C outputs become inputs to P12-D and P12-E. P12-F consumes these roots
and the related owner capabilities. P12-G owns whole-graph validation and
single publication. P12-A final profile integration still needs complete
P12-B through P12-F delivery, validated live owner inventory, and its separate
implementation authorization.

## 5. Rejection and atomic staging semantics

Every owner export is immutable and detached from mutable collections. No
Unity object reference, delegate, logger, service instance, or derived cache is
part of a persisted owner record. Unknown required fields/schema versions,
duplicate identities, invalid counters, broken references, unexpected
cardinality, or unsupported causal random state return typed failures.

Staged constructors validate all local invariants before producing a candidate.
Cross-owner invariants (global identity uniqueness, shared sequence maxima,
P9 output-to-P8 facts, provider-to-stream ownership) are returned as
validation evidence for P12-G. On any failure, discard the private candidate;
the active runtime, its counters, stores, and random streams remain untouched.
Do not advance counters to “repair” malformed input, regenerate provenance,
or silently drop unknown sections.

## 6. Validation evidence required for eventual implementation

Focused owner tests should cover:

- capture/restore of every current `RuntimeIdAllocator` family at untouched,
  advanced, and gapped high-water values, followed by identical next allocated
  IDs; zero/negative/exhausted values, unknown/missing families, and duplicate
  IDs reject without mutation;
- exact `SimulationRecordSequence` continuation after mixed decision/event
  records, strict next-after-restored-maximum ordering across every sharing
  owner, and malformed/duplicate/exhausted sequence rejection;
- P9 manifest round-trip retaining stage order, dependency lineage, authored
  input/output and provenance, seed/config/calendar references, fingerprint,
  schema and first boundary, while proving no genesis stage executes during
  hydration;
- P8-A one-Hex/one-Location/one-scale export and round-trip, anchor and terrain
  revision checks, exact cardinality, plus missing/extra/duplicate/malformed
  rejection;
- each mapped RNG consumer's same-next-draw continuation and query/preview
  non-consumption, with every retained stream covered; provider/consumer
  inventory mismatch and unknown mutable cursor reject;
- cross-section corruption cases for duplicated IDs, sequence ordering,
  provenance/output mismatch, and unknown schema, asserting active source
  objects remain byte/value unchanged on failure.

After implementation, run the targeted P12-C and affected owner suites, all
EditMode tests, the complete official Smoke filter, and `git diff --check`
according to repository validation policy. This design candidate itself makes
no test claim and contains no code change.

## 7. Likely ownership hotspots and blockers

| Surface | Owner / risk | Design constraint or unresolved evidence |
|---|---|---|
| `RuntimeIdentity.cs` | `RuntimeIdAllocator` counters are private; `RuntimeIdentityRegistry` validates uniqueness across runtime types. | Add owner snapshot/private construction seam; keep registries/derived indexes rebuildable and separate from IDs. |
| `DecisionRecords.cs`, `DomainEvents.cs`, `TesteSimulacao.cs` | One `SimulationRecordSequence` is injected into decision and domain-event recorders in the selected bootstrap. | Census every owner sharing it; export its exact next value once, not independently per recorder. |
| `DeterministicRandom.cs` and injected systems | Inventory `ba6f79f` establishes the selected provider graph and seed construction for candidate `af656e7`; retained mutable stream/cursor ownership remains unproven. | **Blocker:** exhaustive live-cursor/draw-state census and exact next-draw restoration contract. Do not infer seed-only sufficiency. |
| P9 genesis pipeline/manifest | P9 pipeline creates and publishes the selected authored manifest/outputs. | Preserve generated output as history; never rerun genesis on hydration. |
| P8 `SpatialAuthorityStore` | Owns typed Hex/Location truth and authored geography provenance. | Capture exactly the accepted P8-A facts; never infer these from legacy spatial objects. |
| P12-B / composition manifest | Owns profile compatibility and section census. Candidate inventory `ba6f79f` audits the exact P9-B/P11 merge and its selected provider graph, but that composition remains unpromoted and remote refs were not fetched. | **Blocker:** apply the accepted/reviewed P12-B contract and obtain current canonical live-composition evidence plus final profile-specific section census before P12-A; this candidate audit alone does not establish canonical P12-A readiness. |
| P12-G | Owns whole-graph validation and publication. | P12-C stages remain private; no active-runtime mutation/publication. |

No P12-C implementation starts until this design passes independent review,
the accepted/reviewed P12-B dependency contract is applied to the exact
supported source, and the deterministic-root coverage blocker is resolved.
Candidate composition/provider auditing is complete at `ba6f79f`, but
canonical live-composition evidence and P12-A's separate implementation
authorization remain outstanding. The accepted P12-B through P12-G scopes
authorize prerequisite capability work; they do not waive this checkpoint's
evidence requirements or P12-A's separate implementation gate.

## 8. Explicit exclusions

This design does not add a save envelope or storage format, final P12-A save or
load flow, schema migration, P13 history/fork guarantees, P8-B through P8-E
spatial truth, P10 Ruin/local topology, procedural or generated worlds, P14
material flow, P18 intraday/timeline/activity continuation, P19 module/loader
state, P20 shared activities, new gameplay, random framework/registry, or
cross-host deterministic guarantees. The intraday/extensibility alignment
requires temporal and extension causality only when those capabilities enter
the supported profile; the selected daily profile explicitly excludes them.
The multi-participant alignment's identity/cardinality rules remain relevant
as future compatibility constraints, but no P20 state is in this P12-C
profile.
