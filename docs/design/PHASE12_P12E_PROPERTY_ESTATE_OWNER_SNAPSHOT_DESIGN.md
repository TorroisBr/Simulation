# P12-E Property and Estate Owner Snapshot Design

**Status:** Bounded technical design candidate; independent exact-content review is required before implementation readiness. This design adds no checkpoint identity, semantic scope, or gameplay behavior.

**P12 canonical base:** `codex/phase12/canonical` at `f5d99cb7008023d14a0ed16ea2149a7d7c18def1`.
**Architecture authority:** `codex/architecture/world-identity-projection` at `47eff220c7ce00f6e7c759bdc2b76780bb46f628`.
**Accepted profile:** the existing `UnityBootstrap-Daily-v1`, successful completed-day boundary.

## 1. Boundary and design choice

Treat the two existing stores as one bounded **owner group**, with three independent owner sections and owner-local revisions. `PropertyOwnershipStore` owns both current ownership and the complete transfer history; `EstateStore` owns explicitly opened estate records and its deceased-Person lookup index. Keep those authorities separate in code and in the value schema. Capture and stage them together as a pair because estate succession consumes a typed `EstateId` + `PropertyId` transition, checks the Estate and Property revisions/rows, and transfers the property to a typed `PersonId`. A single reviewed staging boundary can prove the cross-store read set is mutually compatible and avoids leaving the Estate-to-Property handoff implicit across two designs.

This grouping does **not** create an Estate-to-property membership relation. `EstateRecord` stores only an `EstateId`, deceased `PersonId`, and opening day. A property is associated with a deceased Person only when its current `OwnerPersonId` equals that Person; an Estate can have zero or many such properties. Do not infer or serialize a stronger relation. Retain the three stable sections: `p12e.property.ownership`, `p12e.property.transfer-history`, and `p12e.estate.records`, each schema v1 and `Required` in Daily-v1.

The design consumes the existing P12-B successful completed-boundary token and exact provider/owner-section vector, including exact installed owner identities, schema, cardinality and revisions. These are ephemeral capture evidence and are not serialized. Use the P12-C roots and the already-staged P12-D Person root; do not allocate IDs or duplicate Person facts. Do not add a capture lock, second epoch, invalidation callback, bootstrap registration, envelope, publication path, or P12-A authorization.

## 2. Exact detached values

Use immutable value-only section DTOs (or an equivalent sealed value shape) with no live stores, mutable domain objects, or original runtime references:

| Section | Complete captured rows | Stamp/cardinality |
|---|---|---|
| `p12e.property.ownership` | One row per `PropertyOwnershipRecord`: ordinal `PropertyId.Value`, `OwnerPersonId.Value`. | `recordCount == PropertyOwnershipStore.Count`; exact `PropertyOwnershipStore.Revision`. |
| `p12e.property.transfer-history` | Every retained `PropertyOwnershipTransferHistoryRecord`: `PropertyId.Value`, `PreviousOwnerPersonId.Value`, `NewOwnerPersonId.Value`, `TransferAbsoluteDay`. Preserve the store's existing deterministic ordering by property, day, previous owner, new owner. | `recordCount == TransferHistory.Count`; the same exact `PropertyOwnershipStore.Revision` and owner identity as the ownership section. |
| `p12e.estate.records` | One row per `EstateRecord`: `EstateId.Value`, `DeceasedPersonId.Value`, `OpenedAbsoluteDay`, in ordinal EstateId order. The deceased-Person index is derived from these rows and is not a second section or cardinality. | `recordCount == EstateStore.Count`; exact `EstateStore.Revision`. |

Copy strings and construct fresh `PropertyId`, `PersonId`, and `EstateId` values when building rows and staged stores. Wrap newly allocated lists in read-only collections. Preserve all retained history; do not collapse it into current ownership, infer missing history, deduplicate rows, or substitute diagnostic snapshots. Empty collections are explicit values with their owner identity, schema, count, and revision.

At capture, read each owner only through its own export boundary. Under the already-admitted P12-B quiescent token, copy the sorted defensive owner views and verify before/after owner revision and counts against the same token/vector. The two Property sections must share the exact same captured revision and owner object. The Estate section must match its own owner witness. Any absent section, wrong schema/provider/owner, count mismatch, changed revision/vector, unstable ordering or unsupported profile state rejects the whole private capture. Do not treat an empty-at-bootstrap census as proof that later populated values are absent.

## 3. Live owner contract and validation limits

Source evidence at the design base shows:

- `PropertyOwnershipStore.TryRegister` inserts one unique Property row and increments `Revision` once. `PropertyTransferSystem.TryApplyTransfer` atomically replaces the current row and appends one retained history row, incrementing the same revision once. Runtime estate succession uses that same property transfer commit and does not mutate `EstateStore`.
- `EstateStore.TryRegister` atomically inserts the EstateId row and deceased-Person index entry, incrementing its revision once. The current live write is explicit `TryOpenEstate`; death alone does not open an Estate. Neither store currently has a delete operation.
- Rejected duplicate, stale, malformed, dangling, guarded, or overflow writes leave rows and revision unchanged. The current runtime clone checks Property owner Persons, history Property and Person references, transfer days against current day, and preserves the clone revision by replaying the corresponding owner-level insertions. Estate clone checks the deceased Person is registered and dead, and the opening day is between death and current day. The new private factories must preserve those existing invariants without calling gameplay transitions.
- Each Property revision equals ownership row count plus retained transfer-history row count under current supported operations; each Estate revision equals Estate row count. Validate these equations as current implementation invariants, not as new permanent domain semantics. If future supported owner operations change the relation, update the owner contract and review before implementation.

The owner snapshots preserve current semantic values only. They do not serialize runtime object identity, Person/NPC payloads, mutation guards, events, transition proposals, or derived lookup dictionaries. Exact owner identity is checked against P12-B's capture witness; it is not durable data.

## 4. Private staged reconstruction and typed references

Before creating either candidate, validate section presence, schema, nonnegative counts/revisions/days, exact row counts, deterministic order, nonempty unique PropertyId/EstateId values, valid owner/deceased IDs, and the revision/count equations above. Reject null rows, duplicates, malformed IDs, unsupported schema or unknown populated excluded state.

Stage only against the exact P12-D `PersonStore` and saved absolute day:

1. Confirm the staged PersonStore is the admitted D root; it is not copied into E. Check each current property `OwnerPersonId` resolves there.
2. Check every history `PropertyId` resolves to a captured current Property row, and both previous/new Person IDs resolve to that same staged Person root. Preserve transfer day and both owners verbatim. Do not require that the last history row equals today's current owner unless an existing owner invariant already requires it; current clone code does not impose that condition.
3. Check every Estate deceased `PersonId` resolves to the staged Person root and its exact death fact exists. Preserve the Estate's opening day and require it is not earlier than death or later than the saved world day, matching current runtime composition.
4. Construct a new private `PropertyOwnershipStore(stagedPersonStore)` and `EstateStore(stagedPersonStore)` through internal snapshot factories that install all exact rows and restore each saved revision only after complete validation. The factories do not mutate source or staged Person state, replay registration/transfer/opening/succession, emit events, allocate IDs, rerun death/succession, or bind the final live mutation guard.
5. Verify new owner counts, full row equality, histories, lookup indexes and exact revisions. Return both candidates or neither. P12-G remains responsible for whole B–F identity/reference validation, fresh guard binding and one atomic runtime publication.

The bundle carries typed reference evidence for Property→Person, history→Property/Person, and Estate→Person. The same deceased Person may appear in Estate and Property facts without asserting property membership. The existing succession operation remains responsible for its runtime-time rule that the transferred property's current owner equals the Estate's deceased Person and for candidate eligibility. The snapshot factory does not rerun or infer that operation. There are no P12-F-owned references in this owner group; do not mark these required C/D references unresolved for later repair.

## 5. Integration order and affected files

Implementation should proceed after independent design review and a named owner hotspot handoff:

1. Add detached DTO/export and internal exact-revision factory seams to `PropertyOwnershipStore.cs` and `EstateStore.cs` (or new narrowly scoped snapshot files). Keep their stores, revisions, guard behavior, and mutation APIs authoritative.
2. Add Property/Estate owner tests for export and private factory behavior without editing `SimulationRuntime.cs`, bootstrap/profile admission, or P12-B protocol. Reuse the existing census providers as identity/vector evidence; do not modify their semantics.
3. Integrate the pair into the P12-E owner package coordinator at the same token/vector as other owner slices. Bind its required C/D references to P12-C identities and staged P12-D Persons. P12-G performs the remaining whole-graph validation and publication.
4. Only after those isolated owner slices are accepted, integrate them into the larger P12-E package under an explicit serialized handoff for shared coordinators or runtime files. This design itself makes no P12-A readiness or promotion claim.

Expected implementation/test surfaces: `Assets/_Project/Scripts/Property/PropertyOwnershipStore.cs`, `Assets/_Project/Scripts/Property/EstateStore.cs`, optionally new `PropertyEstateOwnerSnapshot` source and Unity `.meta` files, `Assets/_Project/Tests/EditMode/Editor/Property/PropertyOwnershipCensusTests.cs`, `.../Property/EstatePropertyFoundationTests.cs`, `.../Property/PropertyTransferFoundationTests.cs`, `Assets/_Project/Tests/EditMode/Editor/EstateCensusTests.cs`, plus new focused snapshot/factory tests. P12-E/P12-G coordinator files are integration-only follow-up ownership. `SimulationRuntime.cs`, `TesteSimulacao.cs`, `SimulationBootstrapComposition.cs`, `ContinuationCensusProtocol.cs`, profile assets, Architecture, Brief and State are outside this docs-only design task.

## 6. Required validation for implementation

Focused suites must prove:

- Empty round trip explicitly retains all three schema-v1 sections, exact source identities at capture, zero row counts and exact local revisions; both Property views report the same owner/revision.
- Populated round trip preserves every current ownership row, all transfer history in order, every Estate record, the three independent exact counts/revisions, and derived deceased-Person index behavior. Source writes after export do not mutate the DTO; staged rows and typed IDs are detached from source objects.
- Exact and missing/dangling reference rejection covers property owners, transfer-history Property/previous/new Persons, Estate deceased Persons and death facts, duplicate IDs, invalid days, day-after-saved-world, unknown schema, malformed counts/revisions, and revision/count mismatch. Failed staging returns no candidate pair and leaves active/source stores, revisions, balances, bindings, and random state unchanged.
- Existing registration, transfer, estate opening, succession and rejection behavior stays unchanged. In particular, succession changes the Property owner/history/revision only; it does not advance Estate revision. Do not reapply transfer or succession during hydration.
- P12-B token/vector mismatch, owner identity mismatch, stale component stamp, or omitted required section rejects before any candidate can be consumed by P12-G.
- Subsequent-day parity compares each of the three owner sections and typed D bindings between original and restored runtimes after identical supported inputs; full profile parity remains P12-G's gate.

Focused validation should include the new snapshot/factory suite plus `PropertyOwnershipCensusTests`, `EstateCensusTests`, `EstatePropertyFoundationTests`, `PropertyTransferFoundationTests`, succession integration, P12-B Property/Estate mutation-epoch tests, and P12-D Person staging/reference tests. Integration then runs affected political/property/succession regressions, ALL EditMode, official Smoke, and `git diff --check`. The design reports no test results and requires no Unity execution.

## 7. Collision profile, exclusions, and readiness

This design branch is docs-only. Implementation hotspots are the two owner stores and their tests; those files may be under concurrent P12-E owner work, so serialize store edits and integrate the paired factories only after an explicit handoff. Avoid shared `SimulationRuntime.cs`, daily-loop, bootstrap, P12-B token/vector, P12-D package, and P12-G publication edits in this owner slice. Do not modify or discard unrelated files in the existing dirty Phase20 checkout.

Explicit exclusions: P12-A authorization/envelope/save-load/runtime publication; new checkpoint identity; architecture, Phase Brief or Phase State edits; mutation-epoch/invalidation changes; direct-writer expansion; changes to property transfer, estate opening, death, succession, inheritance or gameplay semantics; ownership law; reconstructed history/events; any duplicate Person/NPC/City roots; P12-F Knowledge, directives, ActorChoice, plans or commitments; P10 LocalTopology/Ruin; P14 material flow; P18 temporal state; P19 loader/mod state; P20 shared activities; P13 history/fork guarantees; and Unity tests in this docs-only task.

The owner fields, revisions and typed reference semantics are specified sufficiently for a bounded implementation design. Implementation readiness remains conditional on a fresh independent exact-content review and an owner hotspot handoff. This slice does not complete P12-E or P12-A; P12-A remains `WAIT_DEPENDENCY`, and P12-G owns final graph validation/publication.
