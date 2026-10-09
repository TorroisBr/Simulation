# P12-E PoliticalDecision Owner Snapshot Design

**Status:** Current-base owner technical-design candidate; implementation has not started.

**Checkpoint:** P12-E — Profile-selected core and official daily-domain owners.
**P12 canonical base:** codex/phase12/canonical at c9d2d8f9ff7176d4d36c5e0a007d2f9ddc7210f8.
**Architecture baseline reviewed:** codex/architecture/world-identity-projection at 47eff220c7ce00f6e7c759bdc2b76780bb46f628, together with the P12-E owner boundary and the intraday/extensibility and multi-participant alignment records already reviewed by that boundary.
**Accepted scope:** exact owner export and private staged hydration for configured P12-E authorities. P12-E prerequisite implementation authorization is already accepted; this owner-specific design still requires independent review before implementation.

## 1. Contract boundary

PoliticalDecisionStore is an E-owned append-only history of immutable political decision records. This slice exports and privately stages that owner’s existing facts. A decision record describes a proposal or selection; it does not execute its outcome or mutate office, claim, faction, support, or Person truth.

The existing census section is authoritative for identity and admission: PoliticalDecisionStoreCensusProvider.SectionId, exactly p12f.political-decisions.records, schema version 1. Preserve this ID and schema despite the historical p12f prefix. Capture requires the exact Required witness and exact installed PoliticalDecisionStore instance named by the current completed-boundary token.

PoliticalKnowledgeStore remains P12-F. This design does not export or hydrate Knowledge, validate Knowledge-holder membership, allocate decision IDs, or duplicate P12-C’s shared sequence. It adds no runtime/bootstrap composition, census registration, mutation wiring, PoliticalWorldRevision snapshot, save format, global graph publication, new gameplay, or profile scope.

The design is revalidated against P12 canonical c9d2d8f. The latest architecture baseline and its alignment records do not change PoliticalDecision ownership, identity, outcome, or P12-E semantics. The owner remains downstream of the exact P12-B token and P12-C identity/sequence roots, with staged D/E references supplied by their own owner packages. This design makes no new P12-A or Phase 12 readiness claim.

## 2. Exact immutable payload

Export one Required schema-v1 section with:

| Value | Source and rule |
|---|---|
| Section ID | The existing PoliticalDecisionStoreCensusProvider.SectionId constant; retain p12f.political-decisions.records. |
| Owner identity | The exact PoliticalDecisionStore from the matching P12-B witness. |
| Cardinality | Exact PoliticalDecisionStore.Count and number of exported records. |
| Revision | Exact nonnegative PoliticalDecisionStore.Revision, preserved as a value. Current owner semantics are append-only: each successful TryRegister adds one record and increments revision once; no delete or terminal mutation exists. Require the source invariant Revision == Count and reject a mismatch. Never silently replace the captured value with a derived value. |
| Captured boundary | The exact AbsoluteDay from the completed P12-B token, stored as snapshot metadata, not as an owner row or a new census section. |
| Records | All immutable records in the owner’s existing deterministic order: DecisionAbsoluteDay ascending, then DecisionId.Value using ordinal comparison. |

Each record preserves:

- DecisionId.Value;
- Decider kind and its matching typed PersonId, InstitutionId, or FactionId;
- DecisionKind;
- the complete ordered CandidatePersonIds set;
- Outcome.Kind and its matching SelectedCandidatePersonId or ReferencedClaimId, where applicable;
- ObservedAbsoluteDay and DecisionAbsoluteDay;
- all EvidenceReferences and KnowledgeReferences;
- ExpectedWorldRevision and ExpectedKnowledgeRevision;
- optional OfficeId and RecognizingInstitutionId.

CandidateFingerprint is derived from CandidatePersonIds and is not duplicated in the payload. Bound PersonStore references, mutation guards, dictionaries, and other runtime indexes are not serialized. Decision IDs remain caller-supplied identities; hydration does not allocate or renumber them.

Snapshot values are detached from PoliticalDecisionRecord and its live PersonStore binding. Reconstruct each record against the exact staged PersonStore rather than cloning a live record, because PoliticalDecisionRecord.Clone preserves its original bound PersonStore.

## 3. Capture

TryCapture accepts the admitted runtime, exact DailyCaptureEligibilityToken, and the token’s exact owner-section vector. It requires reference equality with token.OwnerSections and revalidates the completed-boundary token before and after copying.

Capture finds exactly one Required schema-v1 witness for the existing section ID. Its OwnerInstanceIdentity must be the installed PoliticalDecisionStore; witness cardinality and revision must match the owner before copying. Validate the owner’s ordered record view, Count, and Revision == Count. Copy every field into detached snapshot rows.

Reject capture if the token/profile is unsupported, the witness is missing/duplicated/wrong-role/wrong-schema, owner identity differs, the token expires, cardinality or revision changes during copy, or a record is malformed or outside the captured boundary. Decision and observed days must satisfy the current record constructor’s nonnegative/order constraints, and DecisionAbsoluteDay must not exceed the token day. Expected revisions must be nonnegative. Do not obtain a later clock value or create another capture lock.

The capture path preserves references exactly. It does not resolve P12-F Knowledge links against another owner while exporting PoliticalDecisionStore.

## 4. Validation and staged reconstruction

Validate the detached section before creating a candidate:

- exact existing section ID and schema version;
- nonnegative cardinality and revision, record count equal to cardinality, and the append-only Revision == Count invariant;
- non-null unique DecisionId values using ordinal equality;
- rows in the owner’s exact DecisionAbsoluteDay/DecisionId order;
- one defined Decider kind with exactly one nonempty matching typed ID;
- defined DecisionKind and Outcome kind, with the current PoliticalDecisionRecord constructor’s field-presence and kind/outcome rules;
- candidate IDs non-null, unique, and already in the constructor’s canonical ordinal order;
- EvidenceReferences and KnowledgeReferences non-null, nonblank, unique, and already in their canonical ordinal order; reject rather than rely on constructor normalization;
- observed/decision day ordering and captured-day bound;
- nonnegative ExpectedWorldRevision and ExpectedKnowledgeRevision;
- exact optional OfficeId and RecognizingInstitutionId shape for the decision kind.

Validate references only against owners assigned to D/E and available at this stage: candidate and selected Person IDs against the exact staged PersonStore; OfficeId against the exact staged OfficeStore; RecognizingInstitutionId against the exact staged InstitutionStore; and ReferencedClaimId against the exact staged PoliticalClaimStore. No reference is remapped or inferred from matching text under a different typed ID.

Preserve Decider and KnowledgeReferences as typed unresolved P12-F bindings. P12-E must not consult PoliticalKnowledgeStore. The package reports each record’s decider kind/ID, KnowledgeReferences, and ExpectedKnowledgeRevision for P12-G validation after F is staged. Preserve ExpectedWorldRevision exactly and report it with the record ID for P12-G validation against the fully reconstructed runtime political revision. P12-E does not serialize PoliticalWorldRevision or assert that the unresolved F/G references are valid.

Staging uses the snapshot’s CapturedAbsoluteDay, never a caller-supplied later runtime day. Reconstruct values with the existing typed ID/holder/outcome constructors, then use one private unpublished PoliticalDecisionStore factory bound to the exact staged PersonStore. The factory builds a fresh owner, registers every reconstructed row through the existing owner mutation semantics, and accepts the candidate only when its resulting count and revision exactly match the snapshot. Any invalid row or late failure returns no staged owner and leaves source/live and other staged roots unchanged. The factory does not publish, bind a live mutation guard, mutate staged D/E roots, or execute outcomes.

Stage after the required Person, Institution, Office, and PoliticalClaim roots are available. PoliticalKnowledgeStore resolution and whole-graph publication remain P12-G responsibilities. P12-C’s shared identity/sequence remains unchanged; this snapshot stores the decision identities already present in the owner.

## 5. Validation plan

The focused P12EPoliticalDecisionOwnerSnapshotTests suite should cover:

1. Required empty capture, exact section/schema/owner identity, captured day, count, and revision.
2. Every existing DecisionKind/Outcome combination and all Decider kinds, with exact field fidelity.
3. Deterministic record order, canonical candidate/reference ordering, derived CandidateFingerprint equivalence, and repeated capture equality.
4. Private staged round trip against the exact staged roots, including append-only revision preservation and records bound to the staged PersonStore.
5. Rejection of wrong/missing/duplicate witness, owner mismatch, wrong role/schema, stale token, and changed count/revision.
6. Rejection of duplicate IDs, invalid tags/typed unions, invalid outcome field combinations, noncanonical/duplicate lists, invalid dates/revisions, future decision rows, and declared cardinality mismatch.
7. Rejection of dangling staged Person, Office, Institution, and PoliticalClaim references, with no mutation/publication of any source or staged root after failure.
8. Exact preservation and explicit unresolved reporting of Decider, KnowledgeReferences, ExpectedKnowledgeRevision, and ExpectedWorldRevision; no P12-F lookup or E-owned PoliticalWorldRevision export.
9. Captured-day enforcement: staging a row later than the original token day fails even if the caller’s runtime has advanced.

Retain the existing PoliticalDecision foundation, runtime registration, political decision clone, and P12 census regressions. On the final code tree run the focused owner and affected regressions, ALL EditMode, official Smoke, and git diff --check, retaining XML/log/hash artifacts. A changed executable tree requires fresh exact-tip implementation review. Documentation-only design review does not require Unity tests.

## 6. File ownership and exclusions

Implementation owns:

- Assets/_Project/Scripts/PoliticalDecision.cs only for a private P12-E staged-owner factory;
- one new Assets/_Project/Scripts/P12EPoliticalDecisionOwnerSnapshot.cs and its Unity .meta;
- one focused Assets/_Project/Tests/EditMode/Editor/P12EPoliticalDecisionOwnerSnapshotTests.cs and its Unity .meta;
- its validation artifacts and the owner-specific implementation/review evidence.

Do not edit SimulationRuntime.cs, P12DeferredOwnerCensusProviders.cs, bootstrap/profile assets, P12-B operation or census wiring, P12-C sequence ownership, PoliticalKnowledgeStore, other owner snapshots, or P12-G coordination in this slice. Recheck hotspot ownership before implementation; serialize any later integration that needs SimulationRuntime or shared persistence composition.

This slice does not claim complete P12-E owner coverage, profile-wide export/hydration, global quiescence, complete owner or shared-epoch coverage, capture eligibility beyond P12-B, P12-G publication, P12-A readiness, P13 readiness, or Phase 12 closure. P12-E remains IN PROGRESS and Phase 12 remains open.

## Readiness

The owner boundary is already in accepted P12-E scope and its prerequisite implementation work is authorized. This artifact defines only the implementation boundary for PoliticalDecisionStore. Independent current-base technical review is required before implementation begins.