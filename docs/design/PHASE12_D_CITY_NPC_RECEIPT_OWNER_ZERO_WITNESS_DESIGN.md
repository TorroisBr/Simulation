# P12-D — City/NPC embedded receipt-owner exact-zero witness design

**Status:** Docs-only technical-design candidate; ready for independent exact-content review. It does not authorize implementation. It proposes only evidence needed by the accepted P12-D NPC capture boundary.

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
3. Surface the fixed provider families on `SimulationRuntime` and `SimulationBootstrapComposition` so the validated live Daily-v1 inventory asserts exact coverage, not just registration side effects.
4. Include both rows in the same `ContinuationCensusProtocol.TryCaptureQuiescentOwnerSectionSnapshot` result consumed by D. No separate read before/after the token may be substituted for those rows.

At initial inventory admission and every subsequent successful-boundary assessment, each provider must return the exact same section ID/schema/owner object with cardinality 0 and revision 0. A missing/extra/replaced owner, nonempty ledger, nonzero revision, null receipt list, provider exception, or stale roster invalidates admission/token assessment. During D capture, the token must contain both rows for every captured NPC; the D capture stamp and whole component vector must still match after the detached NPC fields are copied. A receipt write between token issue and capture necessarily changes the append-only owner's revision/cardinality and invalidates the token/vector. No P18 operation is admitted by this profile and no new notification callback or epoch reservation is added.

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
- `Assets/_Project/Tests/Editor/P12DCityNpcReceiptOwnerCensusTests.cs`: focused owner/witness/vector tests. Keep tests in a new dedicated file; integration into existing runtime/composition tests occurs only if required and serially.

After witness promotion, the City/NPC D snapshot implementation can consume the unchanged relation-order design: City roots first with ordered pending NPC IDs; one NPC stage with direct staged City/Location links; fill City membership once and verify reciprocity; then stage Person bindings. Do not alter P8 one-owner-per-Location, Daily-v1's required-empty `p12d.explorable-sites`, or P12-E LocalTopology `NOT_COMPOSED`. No Battle/War owner changes are required.

## 7. Focused validation and promotion evidence

Future implementation validation must include:

1. Each normally constructed NPC has two distinct exact owner instances and emits precisely two required schema-v1 sections; exact section ID/schema/owner reference/cardinality/revision values are asserted.
2. Ten-NPC selected Daily-v1 inventory has twenty rows from these families, with duplicate/missing NPCs, duplicate IDs, aliased child owners, missing child owner, missing receipt list, and unstable owner replacement rejected. Do not invoke a lazy getter in provider/factory tests; assert that a null/malformed field remains unmodified after failure.
3. Each owner at `(count=0, revision=0)` is accepted. Each owner with one or more receipts, empty receipts with nonzero revision, negative/saturated revision, or malformed null list rejects before a token is issued. Populate receipts only through the existing P18-D test commit seam; receipt data is never asserted as exported DTO content.
4. A completed-boundary vector contains all two-per-NPC sections and exact owner identities. Change either owner after token capture and prove D's unchanged-section/vector validation rejects before any package is returned. Verify no new epoch reservation/notification is generated by the read-only witness.
5. D staged NPC construction yields fresh exact-empty receipt owners and preserves no receipt/history data. Force each failure after City/NPC private staging has begun and prove active source objects and City membership/revisions remain unchanged.
6. Re-run the P12-D City root, NPC/action, Person/materialization, census protocol and selected-profile inventory suites; ALL EditMode; official Smoke; and `git diff --check` on the integrated exact code tree. Retain exact XML/log and source-blob hashes. No P18 replay behavior is tested as part of P12 beyond confirming a populated receipt makes Daily-v1 capture fail.

No code or tests are run for this docs-only proposal.

## 8. Dependency, readiness and exclusions

Dependencies are promoted P12-B completed-boundary protocol and P12-C identities, the current P12-D City root snapshot, existing D Person/SpatialNetwork/Site owners, and this crosswalk's exact source evidence. The design may be implemented independently of P12-E Battle owner code; the later whole-graph E/G integration remains separate. `SimulationRuntime` and the NPC receipt owners require serialized implementation/integration ownership.

This design introduces no receipt export, P18 temporal continuation, P18 daily writer, P12-B operation/epoch change, new P12-B completion claim, P12-A integration, P12-G publication, P13 history/fork behavior, P10 topology, gameplay semantics, new checkpoint ID, or Phase closure. It does not assert that all P12-D NPC fields are implemented or that City/NPC composition is otherwise ready. On independent design PASS, only this exact-zero witness capability may be considered implementation-ready under the accepted P12-D boundary; City/NPC snapshot implementation must still pass its exact-base integration and validation gates.

There is no unresolved product or canonical semantic decision in this proposal. The accepted Daily-v1 exclusion determines exact-zero-and-reject semantics; the remaining work is technical implementation and independent review.
