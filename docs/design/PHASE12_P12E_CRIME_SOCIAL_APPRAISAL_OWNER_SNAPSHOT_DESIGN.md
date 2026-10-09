# P12-E CrimeSocialAppraisal Owner Snapshot Design

**Status:** Current-base owner-specific technical design candidate; implementation has not started.

**Checkpoint:** P12-E — Profile-selected core and official daily-domain owners.
**P12 canonical base:** `codex/phase12/canonical` at `29f719f428e29cc452ffa8435c0d601c2bed4787`.
**Architecture baseline:** `codex/architecture/world-identity-projection` at `47eff220c7ce00f6e7c759bdc2b76780bb46f628`.
**Accepted scope authority:** P12 capability decomposition `docs/design/PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md`, the current P12-E technical design and its approved LocalTopology correction, and the P12-E owner inventory. P12-B through P12-G prerequisite capability work is accepted and implementation-authorized; P12-A remains separately gated.

## 1. Contract boundary

This slice supplies detached exact owner values and private staged reconstruction for the three existing stores in the selected Daily-v1 `CrimeSocialAppraisalWorldState`:

- `TheftOutcomeStore`, section `p12.crime-social-appraisal.outcomes`;
- `CrimeKnowledgeStore`, section `p12.crime-social-appraisal.knowledge`;
- `SocialReactionStore`, section `p12.crime-social-appraisal.reactions`.

Use the existing schema version 1 and the exact owner identities already registered by `P12CrimeSocialAppraisalCensusProvider`. Preserve all three separately because P12-B binds each child store as an independent required section with its own cardinality and local revision. Capture the `CrimeSocialAppraisalWorldState` from the admitted runtime, verify the exact installed child stores, and require the exact P12-B completed-boundary token and owner-section vector. Revalidate the token around the copy. Do not add census sections, runtime composition, a new capture lock, or mutation/epoch wiring.

The current Daily-v1 stores begin empty, but the accepted profile permits registered Persons and supported crime/social operations after publication. A later successful completed boundary can therefore contain supported rows. Capture and staging must handle empty and evolved owners; do not impose an empty-only fence. This slice does not change when those operations are valid or how they behave.

## 2. Exact stored values

Capture each owner’s exact `Count`, `P12CensusRevision`, and deterministic owner-provided row view. Preserve the revision as recorded; do not derive it from cardinality. Replacements and compensated writes can make the revision differ from the current row count.

### Theft outcomes

For every `TheftOutcomeStore.Outcomes` row, preserve:

- `OutcomeId.Value`;
- perpetrator and victim `PersonId.Value`;
- positive loss amount;
- occurred absolute day and occurrence key;
- nullable `OriginDecisionId` exactly as the owner exposes it.

The existing semantic ID is deterministic from perpetrator, victim, day, and occurrence key. Reconstruct it and reject a mismatched recorded value. Preserve `OriginDecisionId` as opaque owner provenance; this slice does not resolve it against P11 choice history or another owner.

### Current crime knowledge

For every `CrimeKnowledgeStore.CurrentObservations` row, preserve:

- evaluator PersonId and TheftOutcomeId;
- role and `KnowsLoss`;
- perceived-attribution kind and its matching PersonId or InstitutionId;
- optional known-investigator PersonId or InstitutionId;
- cognitive-basis kind, exact reference string, and optional typed PersonId or InstitutionId source;
- observed absolute day.

This store keeps one current row for each evaluator/outcome key. Export the current rows only; do not invent superseded observation history. Preserve the current owner ordering and require each row to remain valid under the store’s existing role, endpoint, date, and replacement invariants. `StableKey` is derived from the stored fields and is not a separate serialized value.

### Social reactions

For every `SocialReactionStore.HistoricalReactions` row, preserve:

- `ReactionId.Value`, evaluator PersonId;
- source domain and stable ID;
- target kind and stable ID;
- perceived-attribution kind and typed endpoint;
- valence, salience, cognitive-basis kind/reference/typed source;
- creation day and nullable superseded-reaction ID.

Capture historical reactions, including superseded rows. The current-reaction view is derived by the owner from the supersession links and is not duplicated. Recompute the stable reaction ID from the exact tuple and reject a mismatch. Preserve generic source values without assigning new semantics to their strings.

## 3. Capture and staged-root order

Capture accepts the admitted `SimulationRuntime`, its exact `CrimeSocialAppraisalWorldState`, the current completed-boundary token, and the token’s exact owner-section vector. Require the exact three Required schema-v1 witnesses and exact child-store reference identities; compare witness count/revision with the child immediately before copying and recheck token and owner stamps after all three copies. Rows must be valid at or before the token’s captured absolute day. Do not read a later clock value or serialize the token as domain state.

Staging runs after the exact P12-D `PersonStore`, P12-E `InstitutionStore`, and P12-B/P12-C time roots are available. It takes those exact staged roots and requires the staged simulation day to equal the snapshot boundary. Construct one new private `CrimeSocialAppraisalWorldState` against those roots so its three child stores share the exact PersonStore and SimulationTime and the knowledge store points to the exact staged outcome store. Do not create a second Person, Institution, clock, or outcome authority.

The package returns one unreachable staged world-state owner group only after every row and relationship validates. A failure returns no staged group and leaves source stores and previously staged roots unchanged. No active runtime reference, mutation guard, or P12-B callback is bound by this stage.

## 4. Validation and private reconstruction

Validate the detached payload before creating the staged stores:

- exact three section IDs, schema version 1, nonnegative count/revision, row count equal to declared cardinality, and unique owner IDs/keys using ordinal comparison;
- owner-order stability, non-null typed IDs, defined enum values, valid scalar ranges, and captured-day bounds;
- exact stable-ID derivation for TheftOutcome and SocialReaction rows;
- each outcome’s perpetrator and victim against the exact staged PersonStore;
- each knowledge row’s evaluator and any typed Person/Institution attribution, investigator, or cognitive-basis source against the exact staged roots, and its outcome against the staged outcome set;
- each reaction evaluator and typed Person/Institution/theft-outcome target against the exact staged roots/owner set;
- every reaction supersession target exists, has the same evaluator/source/target thread, does not predate its predecessor, has at most one successor, and forms an acyclic history;
- each knowledge observation is not before its outcome and preserves the existing role-to-endpoint and replacement-key constraints.

Use the domain constructors and existing owner rules as the semantic authority. Add only private, P12-E staging factories that populate fresh stores without calling public live mutation paths, replaying appraisals, or emitting mutation notifications. Preserve each exact local revision independently of row count. Rebuild only the owner-derived current-reaction view from historical facts. The `CrimeSocialAppraisalIntegration` is rebuilt over the three staged stores; its transient P12-B operation boundary and mutation context are not serialized.

Typed `PersonId` and `InstitutionId` endpoints are resolved against the staged D/E roots because both roots precede this stage. Do not query `PoliticalKnowledgeStore`, P11 `ActorChoiceStore`, `WorldCommand` history, `CrimeJournal`, or diagnostic projections. Do not duplicate NPC status/life/condition values: those remain in the single D/F `NpcRuntime` projection. `SocialCognitiveBasis.Reference` and `TheftOutcome.OriginDecisionId` are preserved exactly as opaque provenance strings.

## 5. Focused evidence plan

Add one owner-snapshot suite covering at least:

1. Exact empty capture and stage for the three independent required sections, including owner identities, counts, revisions, and boundary.
2. Populated round-trip across outcome rows, current knowledge rows, and historical reactions with supersession; compare every stored field, stable ID, count, revision, and deterministic repeated capture.
3. Knowledge replacement and compensated/removed rows preserve the exact current contents and larger local revision without treating revision as count.
4. Person/Institution cross-references use the exact staged roots; staged stores share exact PersonStore, SimulationTime, and outcome-store references.
5. Rejection of wrong/missing/duplicate section, owner mismatch, wrong role/schema, stale token, changed count/revision, duplicate row keys/IDs, malformed IDs/enums/unions, future rows, and declared-cardinality mismatch.
6. Rejection of dangling typed endpoints, dangling outcome/reaction targets, missing or mismatched supersession links, multiple successors, and cycles; failure returns no staged group and leaves all source/staged inputs unchanged.
7. Exact-zero live profile capture followed by populated capture only at a newly successful boundary after supported owner writes; a stale token after a write is rejected. This is covered using existing P12-B operations and does not add B behavior.
8. CrimeSocialAppraisal foundation/integration and P12-B invalidation regressions remain unchanged; no outcomes, knowledge, reactions, money, or NPC consequences are replayed during staging.

For the implementation integration gate, run the focused snapshot and affected Crime/SocialAppraisal, Crime/Justice, P12 census/epoch, political-root, and runtime admission regressions; ALL EditMode; official Smoke; and `git diff --check`. Retain exact XML/log/hash evidence. A changed code tree requires fresh independent exact-tip implementation review. No daily-loop semantics change is intended, so long-run testing is only needed if implementation discovers or changes such semantics.

## 6. Ownership and exclusions

Expected implementation files are:

- `Assets/_Project/Scripts/CrimeSocialAppraisal.cs` for private factories on the existing owner stores/world-state group;
- `Assets/_Project/Scripts/SocialAppraisalContracts.cs` only if the existing reaction owner needs a private staged-factory seam;
- one new `Assets/_Project/Scripts/P12ECrimeSocialAppraisalOwnerSnapshot.cs` and its Unity `.meta`;
- one focused EditMode test file and its Unity `.meta`, plus validation/review evidence.

Do not edit `SimulationRuntime.cs`, `TesteSimulacao.cs`, `P12CrimeSocialAppraisalCensus.cs`, P12-B mutation coordinator/operation registration, `CrimeSystem`, `JusticeSystem`, `NpcRuntime`, P12-D Person/City roots, P12-F Knowledge/ActorChoice/commitment owners, or P12-G publication. The existing P12-B CrimeSocialAppraisal adapter and its direct/composite commit accounting are upstream dependencies and are consumed unchanged.

This slice covers only the three CrimeSocialAppraisal owner stores. It does not cover `CrimeSystem` hidden-status receipts, CrimeJournal/event records, `JusticeSystem` custody/sentence/warrant facts, configured guard/crime provider state, general economy/demography, all P12-E Crime/Justice owners, or full P12-E. It makes no P12-B completion beyond its existing recorded status, P12-A/P13 readiness, P12-G publication, save-profile closure, or Phase 12 closure claim.

## Readiness

The owner family and export/hydration requirement are within accepted P12-E scope and prerequisite implementation authorization. This design requires an independent exact-content review against its stated current base before its implementation handoff. Implementation can begin after that review passes and the code hotspot check confirms the three existing owner files are available; any discovered unowned state or incompatible current-canonical writer is a technical blocker to this slice, not permission to widen it.