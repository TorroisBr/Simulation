# P12-E Institution and Office Owner Snapshot Design

**Status:** Bounded technical-design candidate; pending independent exact-content review. This design specifies one accepted P12-E owner slice. It is not a new checkpoint, code delivery, P12-A readiness, or Phase 12 closure.

**P12 canonical base:** `codex/phase12/canonical` at `f5d99cb7008023d14a0ed16ea2149a7d7c18def1`.
**Architecture authority:** `codex/architecture/world-identity-projection` at `47eff220c7ce00f6e7c759bdc2b76780bb46f628`.
**Owner scope:** accepted P12-E in `PHASE12_BRIEF.md`, `PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md`, and `PHASE12_E_TECHNICAL_DESIGN.md`; existing Institution/Office census and P12-B invalidation promotions.

## 1. Bounded objective and dependencies

Add exact detached schema-v1 export and private staged reconstruction for the already included, required-empty-at-bootstrap Daily-v1 `InstitutionStore` and `OfficeStore`. Empty at bootstrap does not limit the save slice to empty state: all later supported Institution/Office records, active appointments, and retained open and closed tenure history must round-trip. The four existing P12-B census sections remain the source of owner identity, schema, cardinality, and local-revision evidence.

This is an owner adapter and factory design only. It does not add an owner, section, checkpoint identity, institution/office rule, capture lifecycle, runtime callback, profile, or save envelope. It consumes the existing P12-B completed-boundary token and its section vector; it does not claim that P12-B's complete profile-wide eligibility gates are finished. P12-B remains incomplete and P12-A remains `WAIT_DEPENDENCY`.

Dependencies:

- P12-B's existing admitted Daily-v1 composition, required section providers, owner-thread/quiescence rules, and token validation. The token must be tied to a successful completed daily boundary and remain current before and after copying.
- P12-C's compatible world identity and semantic-ID boundary. InstitutionId, OfficeId, and PersonId values are persisted by their exact string value; no new IDs or sequence values are allocated.
- P12-D's staged PersonStore for resolving incumbent PersonIds. Hydration must occur after that owner exists. Person existence is required; this slice adds no alive/dead, membership, NPC, or political eligibility rule.
- P12-G's later whole-graph validation and one publication boundary. This owner factory returns an unpublished candidate only and never installs it into an active runtime.

The technical design can be reviewed and implemented as an isolated P12-E owner slice under existing prerequisite capability authorization. A passing independent design review is still required before its implementation handoff. P12-A still requires every included owner, the validated live profile inventory, and its separate authorization.

## 2. Authority and current code evidence

Architecture §§91–92 require semantic IDs, typed relations, store state, compatible schema, and a consistent boundary for continuation. Architecture §25 keeps Office distinct from Title and Social Status. Institutional vacancy recognition remains distinct from factual Person death. This owner slice preserves those distinctions; it does not infer an office vacancy, recognition, political selection, title, support, succession, or factual consequence while saving or loading.

At the design base:

- `InstitutionStore` and `OfficeStore` are constructed fresh-empty for the selected profile; they are required composed owners, not absent/disabled owners.
- `InstitutionOfficeCensusProvider` exposes the existing sections `p12e.institution.records`, `p12e.office.records`, `p12e.office.incumbencies`, and `p12e.office.tenures`, schema 1. The first binds to the installed InstitutionStore; the latter three bind to the same installed OfficeStore. The provider rejects an OfficeStore whose `InstitutionStoreForWorldBoundary` is not that exact InstitutionStore.
- The current promoted owner code already has local `Count`/`Revision` values. Institution registration advances the InstitutionStore revision once. Office registration, incumbent assignment, vacancy, and accepted historical-tenure insertion advance the OfficeStore revision once per successful owner commit. Capacity is checked before the first write. A vacancy replaces its open tenure row, so tenure count is unchanged while Office revision advances.
- P12-B's promoted Institution/Office epoch slice registers the four sections and notifies the Institution section or all three Office sections after the relevant owner commit. The P12-B State records this as a bounded invalidation slice, not complete owner coverage, global quiescence, capture eligibility, export, or hydration.
- Runtime construction currently clones Institution before Office. It replays institution registrations, office registrations, active incumbencies (which create open tenure rows), then closed historical tenures in `TenureHistoryInMutationOrder`. These clones establish new local revisions by replay; they do not preserve the source revision. This behavior is appropriate to runtime composition but is not an exact persistence hydrator.
- `Institutions`, `Offices`, `Incumbencies`, and `TenureHistory` return defensive read-only copies. Institution, Office, Incumbency, and Tenure records are immutable. The public tenure view is sorted for reading; the internal `TenureHistoryInMutationOrder` exposes a defensive copy of the retained list's mutation order, which clone construction already uses.
- The owner coverage inventory's older institutional/military audit paragraph says Institution/Office have no local revision. That statement predates the promoted revision and invalidation work recorded later in the same inventory and in current P12 State. Use the current code, census candidate, and State record for revision behavior; the inventory remains accurate that no exact P12 export/private hydrator is delivered.

## 3. Exact owner sections and detached values

Use exactly the current four required schema-v1 section IDs. Do not add per-section revisions or an aggregate political/world revision.

| Existing section | Exact owner | Value count | Local revision |
|---|---|---:|---:|
| `p12e.institution.records` | Runtime-installed `InstitutionStoreForWorldBoundary` | Institution rows | `InstitutionStore.Revision` |
| `p12e.office.records` | Runtime-installed `OfficeStoreForWorldBoundary` | Office rows | `OfficeStore.Revision` |
| `p12e.office.incumbencies` | The same installed OfficeStore | Active incumbencies | The same OfficeStore revision |
| `p12e.office.tenures` | The same installed OfficeStore | All retained open and closed tenure rows | The same OfficeStore revision |

The private owner snapshot is a detached immutable value graph. Store only primitives, nullable primitive values, strings, and immutable read-only copied collections. Do not retain identity wrapper instances, owner records, stores, runtime references, provider objects, mutation guards, token objects, census witness objects, or CLR owner references in the DTO. Do not serialize section counts as substitutes for rows. The outer P12 envelope owns profile/build/content compatibility and schema negotiation; this slice does not invent a second profile identity or schema registry.

Preserve all current record fields:

| Value | Exact fields |
|---|---|
| Institution | `InstitutionId.Value`, `DisplayName` |
| Office | `OfficeId.Value`, `InstitutionId.Value`, `DisplayName` |
| Active incumbency | `OfficeId.Value`, `PersonId.Value`, nullable `StartAbsoluteDay` |
| Tenure | `OfficeId.Value`, `PersonId.Value`, nullable `StartAbsoluteDay`, nullable `EndAbsoluteDay`, nullable numeric `EndReason`, and exact `IsClosed` |

Preserve null versus present day values and the exact enum value. Do not normalize display text, fold ID casing, remove repeated tenure values or other valid history occurrences, invent dates, or derive the current incumbent from the last history row. The active `OfficeIncumbency` map remains the current-occupancy authority; tenure rows preserve history. Preserve every tenure occurrence and its mutation order because the store retains a list, vacancy closes the existing open row in place, and supported assignment/vacancy cycles can append equal closed rows. Runtime cloning already consumes this order. Institution/Office/incumbency collections may use their existing stable ordinal-ID order; that order is a deterministic DTO order, not a new domain ordering rule.

Each section value carries its owner-local source revision for exact restoration. Cardinality is the detached row count and must match the token's owner-section stamp. Owner object identity, capture token, completed sequence, mutation epoch, runtime identity, provider identity, and owner-section vector are transient capture proof only; they are never written to the snapshot.

## 4. Capture and token checks

Implement a bounded capture adapter outside the active runtime's lifecycle, using the existing internal runtime accessors and token methods. The design target is a new owner-specific capture helper; it need not alter `SimulationRuntime`, census registration, the shared epoch, or P12-B operation scopes.

Capture proceeds as follows:

1. Call `TryGetCompletedDailyCaptureToken`. Reject missing, stale, wrong-profile, wrong-runtime, wrong-day, non-successful-boundary, active-scope, unhealthy-runtime, or owner-thread/quiescence failure returned by the existing protocol. Do not infer eligibility from CurrentDay.
2. Resolve `InstitutionStoreForWorldBoundary` and `OfficeStoreForWorldBoundary` from that runtime. Require both present, require `ReferenceEquals(office.InstitutionStoreForWorldBoundary, institution)`, and match their exact installed object identities to the four token sections. Require all four expected IDs, schema 1, Required role, nonnegative counts/revisions, exact per-section cardinalities, and the shared Office identity/revision vector. Do not use constructor inputs or source stores.
3. Read each owner through defensive immutable views once. Copy every field to detached value rows. For tenures use the mutation-order view, not the sorted convenience view. Verify copied list counts and local revisions against the same token stamps; validate the local Institution/Office/Person relation shape without altering the source.
4. Call `TryValidateCompletedDailyCaptureToken` with the same token after all copies. Also compare exact owner identities and revisions/cardinalities used for the copy with the original section stamps. If either check fails, discard the whole partial value graph and return a typed capture failure. Never return a partial Institution or Office section.
5. Return an immutable snapshot. A repeated capture at the same unchanged token produces equal field values/order. Mutating the live owners after return cannot mutate the captured values.

This consumes the current capture token as evidence; it does not create a save guarantee before P12-B closes its full profile-wide owner/epoch/quiescence/boundary obligations. A provider ID, owner revision, or diagnostic snapshot alone is insufficient capture authority.

## 5. Private staged reconstruction and validation

Add an owner-local exact factory, internal to the existing owner assembly boundary, that produces a new, unpublished Institution/Office pair from detached values plus the already staged PersonStore. It must not call the normal `SimulationRuntime` clone helpers, public mutation APIs, succession execution, vacancy recognition, bootstrap, daily systems, or historical/event replay. Public operations establish runtime changes; using them as a save loader would alter owner revisions and can synthesize an open tenure from an incumbency.

Construct exactly one staged InstitutionStore first, restore its records and exact captured revision, then construct exactly one OfficeStore bound to that staged InstitutionStore. Restore office rows, active incumbencies, and the complete tenure list directly through a validated owner factory and restore its exact captured revision. The snapshot factory is not a public revision setter or a new gameplay mutation API. It accepts only fully validated detached values; no candidate owner is returned on any failure. Returned stores remain unbound to the active runtime's mutation guard. P12-G may bind a fresh guard only after complete B–F graph validation.

Validate before returning:

- Each ID is non-empty and unique within its typed identity collection; use ordinal identity semantics. Reject invalid or duplicate rows; do not omit, merge, remint, or repair them.
- Every Office references an Institution in the staged InstitutionStore.
- Each active incumbency references one registered Office and one Person in the staged PersonStore. A dead Person may remain an incumbent until institutional recognition; do not require alive state.
- Every tenure references a registered Office and staged Person. Preserve all retained rows, occurrence multiplicity, and order. Equal closed tenure values at distinct list positions are valid history occurrences and must not be coalesced.
- Each active incumbency has exactly one value-equal open tenure row for the same Office, Person, and nullable start day. Every open tenure has the corresponding active incumbency; reject orphan or multiple open rows. This validates the existing dual representation instead of reconstructing or omitting one half.
- Open rows have `IsClosed == false`, no end day and no end reason. Closed rows have `IsClosed == true` and a defined `InstitutionalVacancyRecognitionReason`. All present days are nonnegative; when both start and end exist, end is not earlier than start. Preserve a null start or end where the existing record contract permits it.
- Do not reject repeated closed tenure values: supported assignment/vacancy cycles can produce equal closed rows, and the retained list preserves both occurrences. An exact-value duplicate closed row remains a separate occurrence during export, staging, and round-trip. Still reject multiple open tenure rows for one active incumbency as described above. Reject unsupported enum values rather than relying on constructors that accept undefined enum casts. The clone-time `TryAddHistoricalTenure` duplicate guard is not a validity rule for the full live retained list and must not be reused to validate or hydrate snapshots.
- Every captured local revision is nonnegative and is restored exactly; do not infer it from cardinality or clone operation count. Each owner-section count equals the exact staged collection count, and the Office sections all share one owner/revision.
- Reconfirm the staged OfficeStore points by reference to the exact staged InstitutionStore. Validate the snapshot has exactly these four required sections at schema 1; missing/duplicate/wrong-owner/wrong-schema sections fail closed.

The office/incumbency/open-tenure checks reject inconsistency rather than filling a missing active or open row. This is essential because the current `CloneOfficeStore` creates an open tenure by replaying each incumbent and skips source open-tenure rows. That clone path is not an integrity validator and must not be reused as the hydrator. Returned failures discard the entire private pair; active runtime owners, revisions, guard, ID roots, and random state remain untouched.

No schema migration from clone inputs, Unity object serialization, diagnostics, rollback snapshots, or old ad-hoc snapshot is implied. P12-G owns the containing envelope migration/version policy and the final atomic publication.

## 6. Dependency and integration order

The owner-pair order is fixed by concrete constructor references:

1. P12-B proves/adopts the matching Daily-v1 profile and completed-boundary capture context.
2. P12-C supplies compatible semantic identity/world roots without allocating new IDs.
3. P12-D stages PersonStore and resolves PersonIds. It also remains the owner of Person life/death facts.
4. This slice captures the installed Institution and Office pair once, then privately stages InstitutionStore → OfficeStore. Use the same InstitutionStore object for the Office constructor and all Office sections.
5. Other P12-E owners hydrate in their real reference order. The promoted census design and source code place PropertyOwnershipStore after Institution → Office; PoliticalClaimStore follows and consumes Person, Institution, Office, and Property roots. These are separate owner slices and must not be read or serialized here.
6. P12-F may later resolve cross-bindings after its own sections exist. This slice emits no speculative unresolved relation.
7. P12-G validates the complete graph and binds/publishes only once.

For implementation isolation, own only the Institution/Office owner adapter, exact factory, and its focused tests. Do not edit shared `SimulationRuntime.cs`, `TesteSimulacao.cs`, bootstrap/profile admission, P12-B token/provider/epoch code, D/NPC graph assembly, Property/Estate, Claim/Recognition, or other P12-E owner slices. A later serialized integration owner may connect this package to the common E capture/staging graph.

## 7. Affected files and exclusive ownership

Expected implementation files, subject to exact review before implementation:

- `Assets/_Project/Scripts/Institution/InstitutionStores.cs` — owner-internal exact detached capture/factory seams where private dictionaries, active map, retained tenure list, and local revisions are accessible. Do not add public revision injection or public mutable collection access.
- A new `Assets/_Project/Scripts/Institution/P12EInstitutionOfficeOwnerSnapshot.cs` — immutable DTO, bounded capture helper and local validation types, unless existing source organization requires a more specific owner-local name.
- A new focused test file under `Assets/_Project/Tests/EditMode/Editor/Institution/` for capture immutability, exact staged values, relation checks and fail-closed behavior; add Unity `.meta` files as required by repository conventions.

No architecture, Roadmap, Phase Brief/State, census provider, census registration, runtime clone, profile, or other domain-owner files are in this slice. No code is changed by this design proposal.

## 8. Focused, regression, and full validation for implementation

No Unity validation is run for this design document. Later code implementation should supply:

**Focused owner checks**

- Empty required stores round-trip explicitly with exact zero cardinalities, owner-pair binding and exact revisions.
- Populated stores preserve multiple institutions/offices, nullable dates, multiple closed tenures, one active tenure, its active-incumbency row, exact enum/reason, exact list order and exact revision values.
- A supported repeat-cycle fixture assigns the same Person to the same Office with the same start day, then vacates on the same end day with the same reason twice. The resulting equal closed tenure values at distinct list positions both survive detached export and staged round-trip, with their occurrence count and order unchanged.
- Assignment adds one incumbency and one open tenure; vacancy preserves tenure cardinality, closes that exact open row, removes active occupancy and advances Office revision once. Capture after both states records different values/revision as appropriate.
- Detached snapshots remain unchanged after later source mutations; repeated unchanged-token capture has equal values and deterministic collection ordering.
- Reject wrong/missing/stale token, mismatched census identity/cardinality/revision/schema/role, partial sections, injected source owners, owner-pair mismatch, and any mutation that invalidates the token during capture.
- Reject duplicate/invalid typed IDs, missing Institution/Office/Person endpoints, dangling tenure rows, orphan/missing/multiple open tenure rows, undefined reason enums, invalid dates, and negative revisions. Do not reject equal closed tenure occurrences. Assert failure returns no staged pair and no active-owner mutation.
- Exact round-trip preserves retired/closed tenure history, current incumbency and historic incumbents independently. Verify no selection, succession, vacancy, Person death, or other domain operation runs during hydration.

**Affected regressions and integration gates**

Run `InstitutionOfficeCensusTests`, `InstitutionFoundationTests`, `InstitutionalVacancyRecognitionTests`, and `PoliticalSuccessionIntegrationTests`, plus affected P12-D Person/identity and P12-E property/claim relation suites. Validate the integrated E owner package after its dependency order is established. P12-G owns full graph, publication-failure atomicity, and continuation-parity integration.

**Full gate**

Run ALL EditMode and the complete official Smoke filter required for the implementation candidate, plus `git diff --check`. If the integrated change alters long-horizon daily semantics, add the applicable long-run gate; this owner adapter should not change daily behavior. Unity tests are not part of this docs-only design task.

## 9. Collision profile and exclusions

This is a documentation-only proposal and does not edit or review the concurrent Conflict implementation/review path. Future implementation has a bounded but real shared-owner-file collision on `InstitutionStores.cs`; serialize edits there with any other Institution/Office mutation, census, or snapshot work. Its new DTO/factory helper and focused tests are disjoint from Conflict/War/Battle owner files. It has a high-level semantic dependency on the shared P12-E staged graph, so integration is ordered after the required Person root and before Property/Claim consumers; it does not own their files or their validators.

Explicit exclusions:

- P12-B completion, full capture eligibility, new mutation/epoch wiring, owner-thread or quiescence changes, and any second capture token/lock.
- P12-C identity/sequence/RNG export, P12-D Person facts or NPC facts, P12-E Property/Estate/Claim/Recognition/Politics/Faction/Military/Conflict/War/Battle/provider state, P12-F Knowledge/directives/choices/commitments, and P12-G global graph validation/publication/parity.
- Institution/office gameplay, vacancy inference on death, succession, titles/social status, political selection or recognition, claim truth/recognition, and new relations.
- P10 topology, P14 material flow, P18 temporal state, P19 mods/loader, P20 shared activities, P13 history/fork guarantees, cross-host numeric portability, persistence envelope/storage APIs, event sourcing, diagnostics as save data, and schema migration policy.

No semantic or product decision remains unresolved for this bounded owner slice in the repository evidence. The open dependency is process readiness: independent exact-content review, plus later integration with the complete E/D graph and P12-G. The existing P12-B profile-wide readiness gaps remain outside this design and must not be represented as solved here.

## Source crosswalk

- `docs/SIMULATION_ARCHITECTURE.md` §§25, 91–92; `docs/ROADMAP.md`; `docs/EXECUTION_MODEL.md`.
- `docs/phases/PHASE12_BRIEF.md`; `docs/PHASE12_STATE.md`; `docs/design/PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md`; `docs/design/PHASE12_E_TECHNICAL_DESIGN.md`.
- `docs/design/PHASE12_OWNER_COVERAGE_INVENTORY.md`; `docs/design/PHASE12_P12E_INSTITUTION_OFFICE_CENSUS_DESIGN.md`; `docs/design/PHASE12_P12E_INSTITUTION_OFFICE_CENSUS_DESIGN_REVIEW.md`; `docs/design/PHASE12_P12E_INSTITUTION_OFFICE_CENSUS_CANDIDATE.md`.
- `Assets/_Project/Scripts/Institution/InstitutionContracts.cs`; `Assets/_Project/Scripts/Institution/InstitutionStores.cs`; `Assets/_Project/Scripts/Institution/InstitutionalVacancyRecognition.cs`; `Assets/_Project/Scripts/InstitutionOfficeCensusProviders.cs`; `Assets/_Project/Scripts/SimulationRuntime.cs`; `Assets/_Project/Scripts/ContinuationCensusProtocol.cs`.
- `Assets/_Project/Tests/EditMode/Editor/Institution/InstitutionOfficeCensusTests.cs`; `InstitutionFoundationTests.cs`; `InstitutionalVacancyRecognitionTests.cs`; `PoliticalSuccessionIntegrationTests.cs`.
