# P12-G Daily-v1 live-graph blocker crosswalk — `02009f9`

**Purpose:** reconcile current source-level owner, writer, package, and publication findings for the accepted `UnityBootstrap-Daily-v1` profile. This is a bounded source audit, not the validated complete live-profile inventory required by P12-G §7. It does not make P12-G implementation-ready.

**Canonical source:** P12 `02009f9063dd252bd4b177fd6aef1e74dcd947f5`; architecture `47eff220c7ce00f6e7c759bdc2b76780bb46f628`. The active candidate's `Assets/_Project/Scripts` subtree is unchanged from the P12 canonical source; its only code-tree delta is the separately reviewed P12-F capture-order test.

## Status boundary

Current canonical State records P12-B through P12-F promoted within their bounded checkpoint scopes, P12-G `WAIT_DEPENDENCY`, P12-A `WAIT_DEPENDENCY`, and P13 `BLOCKED`. P12-C's accepted root/provenance capability is promoted. The remaining restore, whole-graph, publication, and parity obligations belong to P12-G/P12-A; they are not unfinished P12-C scope.

The selected bootstrap asserts a registered census vector of 299 sections at the authored day-zero fixture. `SimulationRuntime.InitializeNpcRosterCensusProtocol` builds and seals an explicit section/provider and operation inventory. The protocol validates that declared set and its witnesses; it does not enumerate every mutable object reachable from the constructed runtime. A 299-section successful assessment therefore does not by itself prove that no composed owner or supported writer was omitted.

## Owner families and current package boundary

| Family | Current source owners and normal paths found | B–F consumer and evidence limit |
|---|---|---|
| B admission | `SimulationRuntime` registers the selected profile's fixed and dynamic section/provider families, known runtime operation scopes, owner-thread binding, and completed-boundary evidence. | B emits a source-runtime-bound token and owner vector; it has no domain payload. The source token cannot be reused for a restored runtime. The sealed inventory is explicit, not a generic graph walk. |
| C roots | WorldId, genesis manifest/provenance, deterministic-random root, P8-A geography, typed allocator counters, and record sequence originate in authored genesis. Allocator event/decision/travel-party counters and the record sequence have P12 mutation boundaries; no ordinary post-publication writer was found for the other roots. | C's typed root snapshot and `P12CContinuationRootStager.TryStage` validate identity/schema, P8-A/P9 roots, counters, sequence, and seed. This is the promoted P12-C contract, not the G publication protocol. |
| D identity and factual roots | Runtime identity registry, legacy spatial network, P8-C/D owners, Person/Genealogy, City/NPC, Market stock, population, Inventory, MoneyAccount, and D-owned NPC facts. Writers include genesis, Person materialization/binding and lifecycle, migration/population, market/trade/transfer, and travel flows. | D owner snapshots and `P12DDailyV1OwnerPackage.TryCaptureAndStage` bind exact identities/revisions and typed relations; D's detached NPC facts are merged by F. P8-C/D remain exact-zero target-owner obligations with no B–F payload stage. |
| E authorities | Justice, Crime/Social, institution/office, faction, political claim/support/decision, property/estate, armed-force/manpower/position, conflict, war, and battle stores. Civic/justice paths use their runtime commit and invalidation coordinators. | E snapshots stage the typed owner families. Justice receipt sentinel validation is in `P12EJusticeRecordsOwnerSnapshot.TryCapture`; Crime's required P18 sentinel has no E/B–F snapshot consumer. Military public writers and their P12 operation/epoch coverage remain unresolved below. |
| F commitments and knowledge | PoliticalKnowledge, directives, P11 ActorChoice, TravelParty, Expedition, and dynamic NPC Knowledge/spatial Knowledge/travel/plans. Writers include directive advance, actor-choice command handling, party/expedition start, and daily/travel/trade paths. | `P12FDailyV1OwnerCapture.TryCapture` captures before C/D/E staged domain allocations; `TryStage` binds the retained capture to the same attempt. Temporal ActorChoice is not a 299-row section and is checked by F source capture before allocation. |
| Empty and census-only rows | P8-C site bindings/person positions, P8-D route observations/person route plans, global NPC-decision/economy receipt caches, per-NPC D receipt rows, and Crime/Justice P18 receipt sentinels. | D handles the existing empty identity/site owners; C checks P8-B passage/crossing zero; E checks Justice's sentinel. G must create and verify fresh target owners for P8-C/D and global caches, verify per-NPC target zero receipts, and explicitly validate the Crime sentinel. |

The detailed current package API summary and registered-vector arithmetic are in [`PHASE12_G_LIVE_INVENTORY_AND_INTERFACE_AUDIT_8F66402.md`](PHASE12_G_LIVE_INVENTORY_AND_INTERFACE_AUDIT_8F66402.md). The source crosswalk above does not replace a per-section exact inventory tied to the constructed live graph.

## Concrete writer/quiescence gaps

### Expedition and nested TravelParty start

`TesteSimulacao.TryStartExpedition` forwards to `ExpeditionSystem.TryStartExpedition` (`Assets/_Project/Scripts/TesteSimulacao.cs`, around lines 106–118). That operation reserves and writes Expedition state, invokes `TravelPartySystem.TryStartTravelParty`, and then completes the Expedition reservation (`Assets/_Project/Scripts/ExpeditionSystem.cs`, around lines 82–179; `Assets/_Project/Scripts/ExpeditionStore.cs`, around lines 52–113). The Expedition section is registered, but the sealed P12 operation inventory has no Expedition operation ID or Expedition-store mutation callback. Its store lock protects individual reads; it does not itself prove participation in P12 owner-thread/quiescence admission across the multi-owner operation.

The normal facade call is synchronous. That fact alone does not prove every supported call uses the bound runtime owner thread or cannot overlap capture through direct store references. Before G claims global quiescence, either demonstrate the complete supported Daily-v1 call path is serialized under the accepted owner-thread boundary or add a reviewed bounded shared admission scope covering Expedition and nested TravelParty mutations.

### Required Military owners

The selected census registers three ArmedForce sections plus ContingentManpower, ArmedForceSpatial, PersistentConflict, PersistentWar, and PersistentBattle (`SimulationRuntime.TryRegisterP12EMilitaryOwnerSections`; provider registration is also visible in `P12RuntimeIdentitySpatialCensus.cs`). These are Required sections and E has matching snapshot consumers.

The stores expose public mutators, including `ArmedForceStore.TryRegister`, `TryAssignCommander`, and `TryRegisterContingent`, plus `PersistentConflictStore`, `PersistentWarStore`, and `PersistentBattleStore` registration/participant/end/start APIs. They bind the generic `AuthoritativeMutationGuard`, whose `CanMutate` only reports whether the runtime is faulted. The selected P12 sealed operation inventory has no matching Military commit scope or section/epoch notification path. No normal Daily-v1 caller for these mutators was found, but public reachability means that absence is not proof of an empty owner or complete supported-ingress boundary.

G must reconcile these Required owners and public mutation paths with the selected profile: either prove the supported Daily-v1 contract excludes those direct paths and rejects unsupported populated state, or provide the reviewed owner-thread/admission and invalidation seam that makes their writes observable. Do not infer P17 gameplay support from the stores' presence, and do not silently treat their Required sections as empty.

## Publication owner and consumer aliases

`SampleScene` contains one `TesteSimulacao`; no `WorldObserverDemoBootstrap` is composed by that scene. `TesteSimulacao` currently has `draftComposition`, `publishedComposition`, and a separate `worldPublished` flag. `PublicComposition` reads the published composition only when that flag is true. Bootstrap publishes in stages by assigning `publishedComposition`, then validating the roster and marking factual reads, and finally setting `worldPublished` (`TesteSimulacao.cs`, around lines 64–75 and 353–380). This is the existing startup gate, not a restored-runtime swap.

The component also retains separate private `simulationRuntime`, runtime identity registry, spatial network, NPC/City lists, economy transaction service, logger, and other bootstrap services. `Simulate` advances the private `simulationRuntime`; runtime lookup methods gate on `PublicComposition` but resolve through private registry/network fields; end-of-day and economy reports read private lists/services. A change to `publishedComposition` alone would leave these consumers on the old graph. Public helpers that already capture `PublicComposition` once (`TryStartTravelParty`, `TryStartExpedition`) provide the safer access pattern; `CurrentDate` and several aliases read the property more than once.

The accepted G design's single-holder requirement therefore still needs an exact consumer-to-owner inventory and integration proof. The restored flow must publish one authoritative composition snapshot in one reference exchange, and each simulation/report operation must capture that same snapshot for its full operation. The old graph remains authoritative on any failed prepublication step.

## Remaining evidence before P12-G implementation

1. Complete the §7 per-section inventory: bind all 299 expected rows and every additional composed object to exact source owner, identity, cardinality/revision source, supported successful mutation path, publication owner, and B–F consumer. Include unregistered objects and prove which are absent, excluded, explicitly empty, or noncausal read models.
2. Resolve the Expedition/nested-TravelParty and Required Military mutation/quiescence gaps against actual Daily-v1 supported ingress. Add no speculative gameplay or security boundary.
3. Verify each B–F package's exact current interface and its source/target zero-owner behavior; close the P8-C/D, global receipt, per-NPC target receipt, and Crime P18 sentinel target obligations.
4. Complete the reviewed fresh restored-boundary admission seam and the `TesteSimulacao` single-holder/consumer audit from the accepted G design.
5. Only then evaluate G implementation readiness and its §6 full validation. Whole-graph validation, atomic publication, and continuation parity remain implementation work under the accepted G scope.

This crosswalk is source evidence only. It does not reopen or downgrade promoted P12-B/P12-C checkpoints, mark P12-G READY, grant P12-A authorization, or change P13's dependency gate. P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains open.
