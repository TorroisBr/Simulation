# P12-B Daily-v1 supported-reachability audit

**Audit status:** the exact-base source audit found one supported
operation/epoch gap and three missing fixed owner rows. The bounded correction
is implemented and focused validation passes; independent exact-tip review of
this Gate 1 evidence is pending. The completed-boundary token remains gated on
that review.

**Exact source baseline:** P12 canonical
`94551b08be8cc9347de35eae5051b8e578ea4c1e`.
**Architecture authority:** `47eff220c7ce00f6e7c759bdc2b76780bb46f628`,
including the P12-B completion master handoff and reviewed contract.
**Selected profile:** `Assets/Scenes/SampleScene.unity` →
`Assets/_Project/Data/Simulations/Simulation-DailyV1.asset`.

## Method

The audit reconciles two source-derived inventories:

1. **Installed-owner inventory.** Start with the source-linked
   `PHASE12_B_DAILY_V1_EFFECTIVE_OWNER_COMMIT_LEDGER.md`: 65 existing fixed sections
   plus `20*N + U + P`, with N=10, U=10 and P=0 in the authored profile.
   Follow runtime construction, cloning, mutation guards, bootstrap providers,
   and every owner/family registration into
   `SimulationRuntime.InitializeNpcRosterCensusProtocol`.
2. **Rooted execution inventory.** Start with selected Unity `Start`,
   `Update`, `Simulate`, the runtime-owned clock dispatch, direct supported
   advance APIs, and the supported Runtime facades. Compare their commits,
   callbacks, changed owners, scope lifetimes, and epoch notifications against
   all 23 entries in
   `PHASE12_B_DAILY_V1_REGISTERED_OPERATION_MATRIX.md`. Inspect unregistered
   public mutation surfaces separately; method visibility by itself does not
   create a supported Daily-v1 ingress.

This is a bounded closure proof for the accepted profile and its supported
runtime operations. It excludes arbitrary external callers and unsupported
raw-alias mutation as specified by the reviewed contract.

## Concrete owner and route gaps

The pre-correction 275-section protocol does not close the selected runtime graph:

- `SimulationRuntime` creates a default `GenealogyStore` when none is passed
  (constructor lines 1090–1091), clones it into the installed runtime owner
  (line 1233), and binds its mutation guard (line 5544).
- The existing `GenealogyCensusProvider` already witnesses that installed
  store's exact object identity, direct-parentage cardinality, and local
  revision. `SimulationBootstrapComposition` exposes the provider bound to
  the installed runtime store (lines 130, 209), but runtime census setup does
  not register it.
- `TryRegisterPerson` is already a supported `runtime.npc-membership`
  transition (lines 5948–5982). Once its endpoints exist,
  `SimulationRuntime.TryAddParentage` and `TryRemoveParentage` (lines
  7560–7585) are validated runtime-domain facades to
  `PersonGenealogySystem` and the installed store. They do not reject the
  selected Daily profile. Successful writes advance the store-local revision
  and the PoliticalWorldRevision, but do not enter a registered operation or
  notify the shared P12 mutation epoch.
- P12's accepted capability decomposition includes `GenealogyStore` and
  parentage among Person/population relation owners
  (`PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md`, lines 185–215).
  Existing P12 State explicitly records the passive
  `p12d.genealogy.parentage` witness as absent from the P12-B sealed inventory
  and without shared-epoch wiring (lines 567–587). No accepted profile
  exclusion covers those direct facades.

The accepted master handoff includes supported `SimulationRuntime` facades
even when the automatic Daily loop does not call them. The promoted P12-D
Genealogy design explicitly names normal `SimulationRuntime.TryAddParentage`
and `TryRemoveParentage` calls as the owner mutation path to observe and test
(`docs/design/PHASE12_P12D_GENEALOGY_CENSUS_DESIGN.md:20–22, 91–95`). Under
the handoff rule, those existing runtime facades are supported roots even
though SampleScene's automatic daily path has no callsite. The reviewed
P12-B bounded completion contract §§2–3 defines the closed composition/root
boundary, requires traversal from supported roots, and states that public API
visibility alone does not establish supported ingress
(`docs/architecture/P12B_BOUNDED_COMPLETION_CONTRACT.md`, contract source
revision `47eff220c7ce00f6e7c759bdc2b76780bb46f628`). Source search at the
exact baseline shows `PersonGenealogySystem.TryAddParentage` and
`TryRemoveParentage` are production callees of those two Runtime facades; the
selected Unity composition has no separate static-system caller. They remain
implementation callees inside the registered Runtime operation. Direct external
invocation of these lower-level public static helpers is outside the accepted
root set under that same rule. The overloads of
`PersonResidenceMembershipSystem.TryBindExistingResident` are likewise helper
methods: the supported production caller is
`SimulationRuntime.TryBindExistingPersonResident`, which enters the existing
`runtime.person.residence-bind` scope before calling the helper and completes
that scope with its commit result. The source classification is directly
visible at `Assets/_Project/Scripts/Person/PersonResidenceMembership.cs:24–103,
106–113` and `Assets/_Project/Scripts/SimulationRuntime.cs:7785–7824`.
Direct `GenealogyStore` mutation is also outside the boundary: the installed
runtime owner is not publicly exposed and its boundary accessor is internal.

Therefore this is a supported selected-profile gap, not an unsupported alias
case or a request to add genealogy gameplay. The smallest correction within
the existing P12-B completion scope is:

- register one Required `p12d.genealogy.parentage` section against the exact
  installed runtime `GenealogyStore`;
- register one `runtime.person.parentage` operation covering successful
  parentage add/remove through the supported `SimulationRuntime` facades;
- register Required p12f.political-knowledge.holders and p12f.political-decisions.records sections against the exact installed stores, requiring initial cardinality and revision zero; do not register mutation operations for these P12-F-deferred owners;
- wrap the shared parentage mutation path in the existing owner-thread,
  unchanged-section, mutation-epoch-capacity, operation-scope, and post-commit
  notification pattern; the political writer facades remain outside P12-B;
- leave rejected/no-op calls unchanged and do not change genealogy semantics,
  birth/death scope, export, hydration, or P12-D completion.

This adds three fixed sections and one supported operation route. The corrected selected
profile equation is `68 + 20*N + U + P`; the authored baseline is 278.
Dynamic Person cardinality continues to be governed by the existing N/U/P
families; the Genealogy section has cardinality equal to its direct-edge count
and stays bound to the same installed owner.

Required correction evidence: selected Daily admission sees the exact Genealogy
owner at zero and exact PoliticalKnowledgeStore/PoliticalDecisionStore
identities at cardinality/revision zero; supported Person registration
followed by successful add and remove through the two Runtime facade methods advances the shared epoch once and
the local owner revision/cardinality as expected;
duplicate/missing-endpoint/missing-edge rejections change neither; the
completed census reassesses; owner-thread or epoch-capacity failure
rejects/faults before an untracked write.

## Deferred political owners and other public mutation surfaces

The audit compared the Runtime mutation surfaces against the 23-route matrix.

- P8-D `TryRecordSpatialObservations` and
  `TryAcceptSpatialRoutePlan` have exact-zero P8-D rows and no selected
  Daily-v1 script caller; P8-D execution is outside this profile.
- Named birth is rejected for P12 by `PersonBirthLifecycle`; conflict-injury
  death is rejected when P12 runtime admission is active.
- P15 structures, P16/P17 military mutations, and P18D temporal methods require
  distinct profile/composition authority.
- Expedition has no selected Daily autonomy producer, Daily has no authored
  ExplorableSite, the P10 site profiles reject before runtime publication, and
  the explicit start path requires a site in that store. Expedition remains
  P12-F/deferred.
- PoliticalKnowledgeStore and PoliticalDecisionStore are constructed as empty
  defaults because the selected Daily constructor passes neither owner. The
  accepted profile inventory includes populated political Knowledge and
  decision state if present (`docs/design/PHASE12_CHECKPOINT_CONTRACT_PROPOSAL.md:172,
  174, 176`), so both exact installed owners need Required
  census sections at their zero-cardinality admission state: p12f.political-knowledge.holders (PoliticalKnowledgeStore.Count/Revision) and p12f.political-decisions.records (PoliticalDecisionStore.Count/Revision). Their three public writer facades have no production Daily caller. The P12-B
  blocker audit, P12 Brief, and capability decomposition assign deeper
  Knowledge/decision execution to P12-F and identify no B ingress
  (`PHASE12_B_BLOCKER_RESOLUTION.md`, lines 1556–1564;
  `PHASE12_BRIEF.md`, line 17;
  `PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md`, lines 257–289). No P12-B
  operation or Knowledge execution is added.
- The eleven RuntimeIdAllocator counter kinds without selected Daily-v1
  post-baseline writers remain assigned to P12-C. Existing Events, Decisions,
  and TravelParties allocation rows remain in the current census.
- The remaining supported Runtime facade commits and callbacks map to the 23
  prior matrix routes plus the parentage route documented in the corrected
  24-route matrix. Bootstrap publication, advance, and nested daily callbacks
  remain synchronous under the captured owner thread and existing
  operation/advance scopes. No asynchronous worker, coroutine, timer, or late
  dispatcher is installed by this profile.

The profile/root and excluded-producer details remain source-linked in the
owner ledger and operation matrix. This audit adds no profile, gameplay,
scheduler, owner registry, allocator export, Knowledge/Expedition execution,
or generic synchronization mechanism.

## Gate result

The pre-correction source baseline had three missing Required owner rows and
one supported Genealogy Runtime operation/epoch route outside the sealed
protocol. The implementation now registers all three exact owners, seals 278
fixed sections plus the existing `20*N + U + P` families, and registers the
24th operation route. Both successful add/remove and rejected parentage
transitions are covered; the updated census reassesses after each committed
change.

Focused evidence on the corrected worktree passed:

| Suite | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| `GenealogyCensusTests` | 7/7 | `7A3F0FAB03CD43204F1FC11F0C63DC39532052315460D547560DB1E9DA0BD53C` | `B691C4E8DC608B7BC070F61C64798FD7C9D0302F34F58CBF9CF54DA2309C3EE5` |
| `SimulationBootstrapCompositionTests` | 25/25 | `D0668C2906D82BEC6061A89F061443DF8762F954DDC391F4D9B0ED6C2720D2A8` | `F094CEECD4CC51E78FA0603F8BABC1B46C86B6B517B0B63745902AE92745D90C` |

Artifacts are retained in `docs/validation/P12BBoundedCompletionGate1/`.
This audit and its implementation still require independent exact-tip review
before Gate 1 can be declared PASS and before completed-boundary token work
starts. This evidence does not claim P12-B completion, capture eligibility,
export/hydration readiness, P12-A readiness, or P13 readiness.
