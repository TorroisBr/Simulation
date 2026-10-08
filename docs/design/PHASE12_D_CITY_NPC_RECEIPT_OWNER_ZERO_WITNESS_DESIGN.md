# P12-D — City/NPC embedded receipt-owner exact-zero witness design

**Status:** Docs-only technical-design candidate; revised after exact-content review and awaiting fresh independent review. It does not authorize implementation. It proposes only evidence needed by the accepted P12-D NPC capture boundary.

**Exact base:** P12 canonical `04e7c3f49a7c9690ebc091fcfc50f05ef67009d6`; architecture `47eff220c7ce00f6e7c759bdc2b76780bb46f628`.

**Inputs:** accepted P12-D technical design; current-base NPC field crosswalk and PASS source review `PHASE12_D_CITY_NPC_CURRENT_BASE_FIELD_CROSSWALK_REVIEW_3B1C850.md`; P12 Daily-v1 brief/profile boundary; current source types `NpcRuntime`, `NpcLocalKnowledgeObservationRuntime`, and `NpcMerchantTradeStateRuntime`.

## 1. Purpose and accepted semantic boundary

The P12-D City/NPC owner capture is blocked because two mutable receipt ledgers are nested in each `NpcRuntime` but are not represented in the current P12 completed-boundary owner vector:

1. `NpcLocalKnowledgeObservationRuntime`, defined in `P18DLocalKnowledgeObservation.cs`;
2. `NpcMerchantTradeStateRuntime`, defined in `P18DMerchantTradeStateOwner.cs`.

P12-D's accepted profile is `UnityBootstrap-Daily-v1`. It excludes P18 temporal continuation and does not compose these P18-D daily consumer operations. Therefore the bounded contract for this P12-D slice is **prove both installed embedded owners are exactly empty at the successful Daily-v1 capture boundary, and reject otherwise**. Neither ledger becomes D/E/F snapshot payload. No P18 receipt, occurrence identity, descriptor, or snapshot is exported, staged, hydrated, or replayed.

The existing completed-boundary token/vector and serialized owner-thread/quiescence protocol remain the capture authority. This design adds two D-required owner facts to that existing vector through its existing owner-section registration seam. It adds no P12-B operation, mutation scope, epoch meaning, admission lifecycle, or completion/readiness claim. P12-B remains `COMPLETE/PROMOTED` only within its recorded admission/completed-boundary contract; this candidate neither reopens nor completes any broader P12-B owner/operation census obligation. P12-D remains open, P12-A remains `WAIT_DEPENDENCY`, and P13 remains blocked.

## 2. Source-backed owner facts and non-mutating read path

Both owner classes currently have:

- a serialized private `long revision`;
- a serialized private `List<...Receipt> receipts`, initialized to a new empty list;
- a public `Revision` property;
- a private `ReceiptList` getter that substitutes a new list when `receipts` is null.

Their `TryCommit` paths append one receipt and advance the owner revision for a new operation; an already retained matching operation is replay/idempotency handling and does not add a second receipt. Receipt removal is not a supported operation. P18-D writers are the only current normal successful paths identified by the source crosswalk. They are not composed by accepted Daily-v1.

The census path must never use `ReceiptList`, `NpcRuntime.LocalKnowledgeObservationRuntime`, or `NpcRuntime.MerchantTradeStateRuntime`: all are lazy and can manufacture an owner/list while attempting to prove absence. Add narrowly named internal `ExistingLocalKnowledgeObservationRuntime` and `ExistingMerchantTradeStateRuntime` accessors on `NpcRuntime` that only return the current fields. Add a non-mutating internal read on each owner, for example `TryReadP12ReceiptCensus(out int cardinality, out long revision)`, which directly reads the backing `receipts` field and `revision` without allocating, normalizing, or enumerating through the lazy getter.

The read returns false for a null list, negative revision, or malformed owner. It reports the raw count and revision when structurally readable. The P12-D provider accepts only `cardinality == 0 && revision == 0`; any nonzero count or revision, including an empty list with a nonzero revision, is populated/unknown state and fails closed. It does not require revision to equal historical count generally; the exact-zero test requires both independently observed values to be zero. The read exposes neither receipt content nor mutable collection references.

## 3. Witness schema and roster identity

Provide one required schema-v1 owner section for each receipt owner on every exact NPC in the admitted runtime roster:

| Embedded owner | Section ID | Witness owner identity | Cardinality | Revision | Accepted value |
|---|---|---|---:|---:|---|
| Local observation receipts | `p12d.npc-local-observation-receipts/{RuntimeId}` | Exact `NpcLocalKnowledgeObservationRuntime` instance currently embedded in that exact `NpcRuntime` | Raw receipt-list count | Owner's local `Revision` | `0`, `0` |
| Merchant trade-state receipts | `p12d.npc-merchant-trade-state-receipts/{RuntimeId}` | Exact `NpcMerchantTradeStateRuntime` instance currently embedded in that exact `NpcRuntime` | Raw receipt-list count | Owner's local `Revision` | `0`, `0` |

Use the existing P12 stable `RuntimeId` uniqueness contract for the suffix; do not derive owner identity from `PersonId`, list index, display name, or hash. If section ID construction requires escaping, use the same canonical stable key encoding already used by the adjacent P12 per-NPC owner families. The section schema is 1 and role is `Required`, not optional or inferred empty. `OwnerSectionCensusWitness.OwnerInstanceIdentity` must be the exact embedded receipt-owner object (not the parent NPC, a newly created wrapper, or receipt list). The provider also retains the exact parent NPC reference and verifies that its non-lazy embedded-owner accessor still returns the same object.

The provider factory takes the canonical `npcRuntimeSnapshot`, validates every NPC/object and nonempty unique RuntimeId, and rejects missing receipt owners, duplicate/aliased receipt-owner instances across actors, duplicate IDs, or an NPC object appearing more than once. It returns exactly two ordered sections per NPC, sorted by ordinal RuntimeId, with stable family order. For the validated current Daily-v1 bootstrap of ten NPCs this is twenty required rows; cardinality remains derived from the actual accepted roster, not made a universal ten-NPC limit.

## 4. Existing-vector binding and completed-boundary behavior

Implement one P12-D provider family, analogous to the existing per-roster owner families in `ContinuationCensusProtocol`, rather than a side-channel counter or an untracked D-only recapture. During the existing runtime census initialization:

1. Build both provider families from the exact selected `npcRuntimeSnapshot` before owner-section inventory sealing.
2. Register each section as `Required` with schema 1 using the existing generic `OwnerSectionContract`, `RegisterExpectedSection`, and `RegisterCensusProvider` flow. The D family tracks section IDs, exact receipt-owner references, parent NPC references, and provider instances so a changed NPC roster or replaced nested owner does not leave a stale family unnoticed.
3. Surface the current provider families on `SimulationRuntime` and `SimulationBootstrapComposition` so the validated live Daily-v1 inventory asserts exact coverage, not just registration side effects.
4. Include both rows in the same `ContinuationCensusProtocol.TryCaptureQuiescentOwnerSectionSnapshot` result consumed by D. No separate read before/after the token may be substituted for those rows.

At initial inventory admission and every subsequent successful-boundary assessment, each provider must return the exact same section ID/schema/owner object with cardinality 0 and revision 0. A missing/extra/replaced owner, nonempty ledger, nonzero revision, null receipt list, provider exception, or stale roster invalidates admission/token assessment. During D capture, the token must contain both rows for every captured NPC; the D capture stamp and whole component vector must still match after the detached NPC fields are copied. A receipt write between token issue and capture necessarily changes the append-only owner's revision/cardinality and invalidates the token/vector. No P18 operation is admitted by this profile and no new receipt-write notification callback or epoch reservation is added.

### Dynamic NPC roster reconciliation

The receipt-owner families are dynamic families keyed by the current authoritative `NpcRuntime.RuntimeId`; the initial ten-NPC/20-section inventory is only the current fixture count. Do not retain the initial provider arrays as a complete family and do not implement providers as a live view whose enumeration can silently differ from the protocol's sealed section set.

The implementation must extend the existing roster-family reconciliation performed by `ContinuationCensusProtocol.TryReconcileSpatialKnowledgeRosterAndNotifyCommittedMutations`. `SimulationRuntime.TryRegisterNpc` and `TryUnregisterNpc` run within `NpcMembershipCensusScope`; after a committed membership change, scope exit invokes that reconciliation with the current world-owned roster. The receipt families must participate in the same staged reconcile transaction as the existing SpatialKnowledge, Inventory, MoneyAccount, Knowledge, plan, travel-state, and lifecycle families:

- On a committed NPC addition, build exactly two candidates from that exact new `NpcRuntime` and its non-lazy embedded-owner accessors. Add both required section contracts, registered providers, owner/NPC identity mappings, and provider-family entries together. The new owners must independently witness `(cardinality=0, revision=0)` before they are admitted.
- On a committed NPC removal, remove both section IDs, their expected contracts, registered providers, identity mappings, and family entries together. Do not preserve detached receipt-owner sections after their parent leaves the accepted roster.
- For an unchanged RuntimeId/NPC object, require each embedded owner to remain the exact same object as the registered witness; a missing or replaced child owner is a protocol fault, not an implicit empty replacement.
- If an NPC is unregistered and later a new NPC successfully registers with the same RuntimeId, the removal boundary first removes the old rows; the later addition installs the new NPC and child-owner identities with fresh exact-zero baselines. Never carry the old owner identity or baseline to the new instance.
- Derive expected row count from the current authoritative roster (exactly two per current NPC) and reject duplicate RuntimeIds, duplicate NPC objects, aliased child owners, missing/extra rows, stale parents, or any nonzero/null/malformed child witness. No fixed roster cardinality is part of the contract.

The protocol must stage the receipt family’s new IDs, contracts, registered sections, NPC/child identity maps, and provider array before publishing any of them, following the existing reconcile pattern. A committed membership change that changes this family contributes to the same `anySectionChanged` decision as other family/fixed-section changes: without a reservation, advance the shared mutation epoch once for the outer membership operation; with an existing reservation, complete that reservation once. Do not add a second increment or a receipt-specific epoch. Preserve the existing owner-thread/active-operation requirements and overflow behavior. If any provider rebuild, identity/cardinality check, epoch-capacity check, or staged reconcile step fails, publish none of the new family maps and fault-close the existing census boundary as the current reconciliation caller does. A membership attempt rejected before commit must not alter the receipt family or epoch.

This corrects the prior design's stale-roster gap. It adds only roster membership reconciliation for exact-zero witness rows; it does not admit P18 writes or relax the count/revision zero requirement.

This does not make P12-B responsible for persisting P18 records. The two schemas are D-owned negative-profile evidence rows registered into the existing generic P12 protocol solely because D's accepted exact capture vector is defined by that protocol. The owner values remain outside the D/F export DTO. If the selected profile ever composes either P18-D writer, this design is invalid: that profile must receive a separately reviewed contract that captures the relevant P18 causal history rather than weakening this zero-only rule.

## 5. Stage and failure semantics

The D immutable NPC DTO contains no fields for either receipt owner. It carries only the approved D/F NPC values and transient capture stamp/vector metadata. During private staged NPC construction, the normal `NpcRuntime` construction path may initialize fresh empty embedded receipt owners as it does today; the staged factory verifies those exact instances read as `(count=0, revision=0)` without invoking lazy accessors. It must not copy receipt ledgers, set their revisions from source, call `TryCommit`, or replay P18-D work.

Any failed source witness, vector mismatch, missing owner, populated receipt, null list, revision mismatch, roster/owner replacement, or stage default that is not exact-empty aborts the unpublished D package. Do not return partial City/NPC/Person composition, and do not mutate the active runtime. The downstream P12-G publication boundary remains unchanged.

## 6. File ownership and integration order

This slice needs one serialized implementation window because its census protocol registration touches the shared capture mechanism and the concrete NPC owner:

- `Assets/_Project/Scripts/P18DLocalKnowledgeObservation.cs` and `P18DMerchantTradeStateOwner.cs`: add only raw, non-mutating census reads; do not alter receipt commit/replay semantics.
- `Assets/_Project/Scripts/NpcRuntime.cs`: add non-lazy `Existing*` accessors only; preserve field initialization and gameplay behavior.
- New `Assets/_Project/Scripts/P12DNpcReceiptOwnerCensusProviders.cs`: own the two required exact-zero schema-v1 provider families and roster validation.
- `Assets/_Project/Scripts/ContinuationCensusProtocol.cs` and `SimulationRuntime.cs`: register/track the families and include them in existing vector assessment/capture. This is the shared P12-B completed-boundary hotspot; no separate concurrent writer may modify these files.
- `Assets/_Project/Scripts/SimulationBootstrapComposition.cs` and selected-profile inventory tests: expose and assert the exact rows in the live Daily-v1 composition.
- `Assets/_Project/Tests/EditMode/Editor/P12DCityNpcReceiptOwnerCensusTests.cs`: focused owner/witness/vector tests. Keep tests in a new dedicated file; integration into existing runtime/composition tests occurs only if required and serially.

After witness promotion, the City/NPC D snapshot implementation can consume the unchanged relation-order design: City roots first with ordered pending NPC IDs; one NPC stage with direct staged City/Location links; fill City membership once and verify reciprocity; then stage Person bindings. Do not alter P8 one-owner-per-Location, Daily-v1's required-empty `p12d.explorable-sites`, or P12-E LocalTopology `NOT_COMPOSED`. No Battle/War owner changes are required.

## 7. Focused validation and promotion evidence

Future implementation validation must include:

1. Each normally constructed NPC has two distinct exact owner instances and emits precisely two required schema-v1 sections; exact section ID/schema/owner reference/cardinality/revision values are asserted.
2. Selected Daily-v1 inventory has exactly two rows per current NPC, with no assumed roster size. Exercise successful membership transitions such as 10→11→10 and assert after each committed boundary that additions create the exact two current child identities, removals delete both prior rows, and section IDs/cardinality/provider lists match the current roster. Include unregister/re-register with the same RuntimeId and assert new identities replace removed identities only across those separate committed boundaries. Reject duplicate/missing NPCs, duplicate IDs, aliased child owners, missing child owner, missing receipt list, and replacement of a child owner while its parent remains registered. Do not invoke a lazy getter in provider/factory tests; assert that a null/malformed field remains unmodified after failure.
3. Each owner at `(count=0, revision=0)` is accepted. Each owner with one or more receipts, empty receipts with nonzero revision, negative/saturated revision, or malformed null list rejects before a token is issued. Populate receipts only through the existing P18-D test commit seam; receipt data is never asserted as exported DTO content.
4. A completed-boundary vector contains all two-per-current-NPC sections and exact owner identities. Change either owner after token capture and prove D's unchanged-section/vector validation rejects before any package is returned. Verify no new epoch reservation/notification is generated by the read-only witness itself.
5. For successful roster addition/removal, assert that the receipt family is reconciled atomically with all already-supported owner families and that the shared mutation epoch advances exactly once for the outer committed membership operation. Failed/rejected membership leaves the family and epoch unchanged; a reconciliation/epoch-capacity failure publishes no partial family state and fault-closes admission. Existing membership operation reservation semantics remain authoritative.
6. D staged NPC construction yields fresh exact-empty receipt owners and preserves no receipt/history data. Force each failure after City/NPC private staging has begun and prove active source objects and City membership/revisions remain unchanged.
7. Re-run the P12-D City root, NPC/action, Person/materialization, census protocol and selected-profile inventory suites; ALL EditMode; official Smoke; and `git diff --check` on the integrated exact code tree. Retain exact XML/log and source-blob hashes. No P18 replay behavior is tested as part of P12 beyond confirming a populated receipt makes Daily-v1 capture fail.

No code or tests are run for this docs-only proposal.

## 8. Dependency, readiness and exclusions

Dependencies are promoted P12-B completed-boundary protocol and P12-C identities, the current P12-D City root snapshot, existing D Person/SpatialNetwork/Site owners, and this crosswalk's exact source evidence. The design may be implemented independently of P12-E Battle owner code; the later whole-graph E/G integration remains separate. `SimulationRuntime` and the NPC receipt owners require serialized implementation/integration ownership.

This design introduces no receipt export, P18 temporal continuation, P18 daily writer, P12-B operation/epoch change, new P12-B completion claim, P12-A integration, P12-G publication, P13 history/fork behavior, P10 topology, gameplay semantics, new checkpoint ID, or Phase closure. It does not assert that all P12-D NPC fields are implemented or that City/NPC composition is otherwise ready. On independent design PASS, only this exact-zero witness capability may be considered implementation-ready under the accepted P12-D boundary; City/NPC snapshot implementation must still pass its exact-base integration and validation gates.

There is no unresolved product or canonical semantic decision in this proposal. The accepted Daily-v1 exclusion determines exact-zero-and-reject semantics; the remaining work is technical implementation and independent review.

## 9. Review correction record

The first exact-content review, recorded in `PHASE12_D_CITY_NPC_RECEIPT_OWNER_ZERO_WITNESS_REVIEW_22D1BEF.md` at commit `b587ba3a27d4104f8bc40e99ffdcede4000e6d98`, returned `NEEDS_CHANGES` for two design omissions: the test path omitted the `EditMode` directory, and the design did not specify how the per-NPC section families remain aligned with supported roster changes. This revision corrects the path, adds dynamic add/remove/re-register and child-identity rules, and binds family reconciliation to the existing committed membership census and shared-epoch behavior. It changes design detail only; it does not change the accepted receipt-zero semantics, D scope, P12-B contract, or implementation authorization. A fresh independent exact-content review is required.
