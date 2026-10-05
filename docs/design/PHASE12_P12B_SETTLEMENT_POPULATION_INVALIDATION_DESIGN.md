# P12-B Selected-Profile SettlementPopulation Invalidation Design

**Status:** Revised draft technical design; independent second review pending. This is a bounded P12-B sub-slice proposal, not an implementation candidate or a new numbered checkpoint.

**P12 canonical base:** `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e` (`origin/codex/phase12/canonical`).

**Current integrated runtime reference:** `75a27d7ac97e66c2762835ccea7950a945c2f20d` (`origin/codex/phase16/canonical`), a descendant of the P12 base. It includes the P12 mutation protocol and P16-A's explicit rejection of P15/P16 proving state under `UnityBootstrap-Daily-v1` admission. Any implementation must refresh both refs and serialize `SimulationRuntime` changes with active P17-A work.

**Authority:** The accepted P12-B capability scope and implementation authority are recorded in `PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md` and `docs/phases/PHASE12_BRIEF.md`. P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`. This proposal does not add product semantics, broaden the supported P12-A profile, or claim readiness.

## Purpose and bounded contract

Connect the existing per-City `SettlementPopulationRuntime` aggregate owner to the selected-profile P12-B sealed owner inventory, while placing invalidation at the boundary of the full supported domain operation that commits it. Reuse the already-promoted `SettlementPopulationCensusProvider`; do not create a parallel population owner or census surface. A callback inside the shared population `TryApply` is unsafe: the same path serves aggregate demography and NPC lifecycle operations, and population-only notification would run before the latter finish their other owner writes.

The selected `Simulation-GeneralTest.asset` has absent serialized natural-mortality and aggregate-demography boolean fields; Unity deserialization supplies `false`. `SimulationConfigData.CreateConfigurationOverrides` passes those explicit false values to `SimulationConfigurationResolver`, whose resolver maps them to `NaturalMortalityPolicy.Disabled` and `AggregateDemographyPolicy.Disabled`. `SimulationConfigurationDefaults.Create()` also constructs both effective configurations with their default `enabled = false`. Therefore `DailyDemographicSystem.Advance`, which is called by `SimulationRuntime.AdvanceDayAfterClockAdvance`, executes neither the `TryApplyPersonDeath` natural-mortality branch nor the aggregate-demography loop for this selected profile. The current supported daily path has no population writer from either demography feature. Preserve that contract with a pre-bind/profile assertion that both effective flags remain false; a profile that enables either is outside this bounded checkpoint and must fail closed until its writer operations are separately designed.

Other population writers exist in source and must not be hidden by this flag finding. `SimulationRuntime.TryApplyImmigration`, `TryApplyEmigration`, and `TryApplyResidentDeath` delegate to `NpcPopulationLifecycleSystem`, which changes the settlement aggregate and then NPC residence/death state; its Person-backed death variant also commits the validated Person/NPC death state after the aggregate. Separately, `SimulationRuntime.TryApplyPersonDeath` routes through `PersonDeathLifecycleSystem`, which may commit aggregate and population-operation-receipt state before Person/NPC/residence writes. `NpcResidenceMigrationSystem` commits both City aggregates through `SettlementPopulationRuntime.TryApplyPairedMigration` before changing NPC residence. These are multi-owner operation paths, not an isolated aggregate write. The source contains no P12 operation scope around these commits; a test helper or operation-count facility elsewhere does not establish one.

| Source anchor | Finding |
|---|---|
| `Assets/_Project/Data/Simulations/Simulation-GeneralTest.asset`; `Assets/_Project/Scripts/SimulationConfigData.cs` (`CreateConfigurationOverrides`); `Assets/_Project/Scripts/Configuration/SimulationConfigurationResolver.cs` | Serialized flags are absent/false, passed as explicit false overrides, and resolved to `Disabled` policies. |
| `Assets/_Project/Scripts/Configuration/SimulationConfigurationDefaults.cs`; `Assets/_Project/Scripts/Configuration/SimulationConfigurationTypes.cs` (`EffectiveNaturalMortalityConfiguration`, `EffectiveAggregateDemographyConfiguration`) | Both default configuration constructors have `enabled = false`. |
| `Assets/_Project/Scripts/SimulationRuntime.cs` (`AdvanceDayAfterClockAdvance`, `TryApplyImmigration`, `TryApplyEmigration`, `TryApplyResidentDeath`, `TryApplyPersonDeath`, `InitializeNpcRosterCensusProtocol`) | Daily demographic phase is called; current P12 operation inventory has no SettlementPopulation lifecycle operation/scope or census provider registration. |
| `Assets/_Project/Scripts/Population/DailyDemographicSystem.cs` (`Advance`, `ApplyNaturalMortality`, `ApplyAggregateDemography`) | Natural mortality and aggregate population writes are individually guarded by their effective `Enabled` values. The accepted selected profile skips both branches. |
| `Assets/_Project/Scripts/Population/NpcPopulationLifecycleSystem.cs` (`TryApplyInternal`) | Population `TryApply` commits before NPC residence/death changes; a core population callback would notify mid-operation. |
| `Assets/_Project/Scripts/Person/PersonDeathLifecycle.cs` (`TryApplyDeathCore`) | Resident population aggregate/receipt may commit before Person/NPC/residence writes; replay can avoid a new commit. |
| `Assets/_Project/Scripts/Population/NpcResidenceMigrationSystem.cs` (`TryApply`); `Assets/_Project/Scripts/Population/SettlementPopulationRuntime.cs` (`TryApplyPairedMigration`) | Both City aggregates commit together, then NPC residence changes; no P12 operation scope is bound at this source boundary. |

The effective configuration predicate is part of this contract. The accepted selected asset explicitly resolves both flags to false as described above; revalidate the effective configuration at runtime admission and fail closed if either becomes true. Every composed population aggregate remains a required owner section even when these consumers are disabled. This is a property of the currently selected profile, not a claim that population state can never change through another supported operation.

## Exact owner sections

For each exact installed `CityRuntime.Population` owner, reuse the provider emitted by `SettlementPopulationCensusProvider.CreateProviders` and validate City `RuntimeId` against the owner's `SettlementRuntimeId`.

- Register `p12d.city-population.aggregate/<length-prefixed City RuntimeId>` as a required P12-B section. Its cardinality is `1`; its revision is the owner's aggregate `Revision`. The aggregate value itself is not in the census witness.
- Preserve the already-published `p12d.city-population.operation-receipts/<length-prefixed City RuntimeId>` provider. Its receipt-count/local-revision witness is distinct from the aggregate witness. The selected daily profile's two demographic writers are disabled, so neither aggregate nor receipt population mutation occurs through `DailyDemographicSystem`; receipt-backed resident-death commits remain an operation-level writer when explicitly invoked.
- Resolve and store the provider/section mapping during pre-bind inventory setup, require one aggregate provider for every published City and no duplicates, then seal the inventory. A missing City owner/provider, mismatched owner identity, duplicate stable ID, unsupported schema, or inconsistent initial witness fails closed.

This limited registration is evidence for one owner family. It does not complete P12's selected-profile owner inventory, turn the partial epoch into a global coherence proof, or issue capture eligibility.

## Mutation preflight, commit, and notification

Do not add a generic callback to `SettlementPopulationRuntime.TryApplyTransition` or `SettlementPopulationSystem.TryApply`. Instead, add a P12-aware operation boundary to each supported outer operation that can commit population. At operation entry, validate exact provider/section identity and reserve protocol capacity for the full known changed-section set. Immediately before the first domain write, preflight that exact set and mutation-epoch capacity. After the entire operation's aggregate, receipt, and enclosing Person/NPC/residence writes have completed successfully, notify the set once. On a rejected preflight, no domain owner changes. On a post-commit notification failure, fault/close P12 admission; do not attempt domain rollback.

The boundary must identify writer families explicitly:

1. The bounded Daily profile resolves `NaturalMortality.Enabled == false` and `AggregateDemography.Enabled == false`; assert that at admission. It therefore requires owner registration but no daily population commit hook. If a later profile changes either flag, reject it until the relevant path has its own reviewed operation boundary.
2. `SimulationRuntime.TryApplyImmigration`, `TryApplyEmigration`, and unbacked `TryApplyResidentDeath` are each single-City aggregate-plus-NPC operations through `NpcPopulationLifecycleSystem.TryApplyInternal`. Preflight the City's aggregate section plus every affected resident/Person/NPC section proven by the sealed matrix, then notify only after the method returns success.
3. Person-backed resident death through `NpcPopulationLifecycleSystem` updates aggregate and Person/NPC state; the direct `SimulationRuntime.TryApplyPersonDeath` route through `PersonDeathLifecycleSystem` may also add a population operation receipt. Preflight the exact operation-specific section set before the first population/receipt write and notify only after that full operation finishes successfully. Replay/rejection that applies no new owner mutation sends no mutation notification.
4. `NpcResidenceMigrationSystem` changes two population owners and NPC residence. Its boundary must preflight both exact City aggregate sections and every affected NPC/residence section before `TryApplyPairedMigration`, then notify once after the NPC residence commit. Never use two independent per-owner callbacks around this paired commit.

The selected daily profile's enabled system call graph and any exposed operation entry points must be reconciled against the sealed P12 owner IDs before the implementation review. The supported callers and affected changed-section sets are not yet proven complete by this design; no callback may be implemented against guessed section membership.

At the outer-operation boundary, the P12 admission coordinator must verify:

1. the runtime is using the accepted Daily profile, both demographic flags remain false, and the bound Unity owner thread matches;
2. each exact `SettlementPopulationRuntime` instance still matches its City's aggregate (and, where a new receipt is committed, receipt) census provider and stable section ID;
3. every affected required census witness has its exact owner identity, schema, cardinality, and current revision;
4. each affected baseline is valid and the P12 protocol has capacity for one notification covering the complete operation changed-section set; and
5. all existing domain semantic/revision/overflow validation has succeeded, while no member of the multi-owner operation has mutated yet.

The P12 coordinator must reserve mutation-epoch capacity for the whole changed-section set before the first write. Existing population semantic checks retain their order. A rejected P12 preflight leaves all participating owners unchanged. A successful operation advances the epoch once for the complete set; no intermediate aggregate-only notification is permitted.

No-op, stale, invalid, underflow/overflow, mutation-guard rejection, replay, or exhausted revision does not notify if it made no new owner commit. Successful net-zero aggregate transitions still count as writes when the owner revision advances. Later implementation tests must assert all preflighted owner identities/cardinalities, one epoch delta per completed outer operation, and unchanged full owner state/epoch on rejected preflight.

Do not wrap the entire daily advance as an all-or-nothing transaction or change its error/partial-progress behavior. In the accepted Daily profile, the demographic subphase is disabled, so its existing per-City error behavior is not part of population invalidation. If those flags ever become enabled in another accepted profile, a separate design must define commit/notify behavior for each operation and preserve the existing partial-progress semantics.

Existing P12 changed-section collectors may be reused only where the exact population operation is already nested inside them. `runtime.advance-day` active-operation counting is not an epoch batch and does not protect an intermediate owner notification.

## Explicitly separate mutation families

The design describes operation-level boundaries for the supported runtime lifecycle APIs above, but none is implemented or currently proven by the P12 inventory. It leaves outside this proposal:

- `DailyDemographyBoundaryStepProvider`'s receipt-backed temporal demography path, which belongs to its separate P18-D/intraday composition and is not the selected P12 Daily profile;
- any unclassified writer path (including battle or future War consumers) unless explicitly admitted to P12 and given a complete operation boundary;
- Population receipt `RestoreSnapshot` pruning/compensation; and
- population export, staged hydration, capture token issuance, global owner-thread proof, global quiescence, P12-A readiness, or P12-B completion.

Registration alone does not cover any writer. Before implementation, the reviewer must verify each called writer path and its complete changed-section set against the sealed P12 inventory. Where a Person/NPC/residence fact has no corresponding exact census section, either add that owner witness within accepted P12-B scope or prove the mutation is excluded by the selected profile contract; do not silently omit it. P18-D and unclassified battle/War writers require separate admission exclusion or separate reviewed adapters.

## Integration and ownership

The population census provider is already exposed by `SimulationBootstrapComposition` and its owner/store behavior is in `SettlementPopulationRuntime`. The design now requires exact registration, selected-profile flag admission, and operation-level changed-section boundaries for supported public lifecycle operations; it does not place notification in the shared population writer. `SimulationRuntime.cs`, `NpcPopulationLifecycleSystem`, `PersonDeathLifecycleSystem`, `NpcResidenceMigrationSystem`, and protocol registration are shared/ordered hotspots. Integrate serially with P17-A's selected-profile exclusion/rejection work and any concurrent P12 admission change. Do not edit P17 War/Conflict semantics in this P12 design.

The current P16 runtime has composition-level fail-closed checks for P15/P16 state under `UnityBootstrap-Daily-v1`; preserve them. P17-A separately must reject a P17-A War profile when P12 admission is supplied and reject P17 state under Standard composition. This population design adds no P17 owner behavior and makes no statement that its implementation may proceed concurrently in `SimulationRuntime`.

## Validation expected after design review and implementation authorization

- Focused tests for exact per-City provider registration, stable identity/cardinality/revision, and rejection of an effective profile that enables either demographic writer.
- A selected-profile admission test proving the effective NaturalMortality and AggregateDemography flags are false and reject a profile that enables either before it can enter this bounded population integration; prove the City owner is still required/registered while the disabled consumers make no population mutation during daily advance.
- Operation-path tests for supported immigration/emigration, NPC resident death, person-backed death (including receipt and replay cases), and paired residence migration. Each must show preflight of the full operation's changed-section set before its first write and one post-success epoch notification after every participating owner commits.
- Regression coverage for existing `SettlementPopulation`, `PersonDeathLifecycle`, and `NpcResidenceMigration` semantics while verifying full changed-section preflight and post-commit notification at their outer operation boundary. Aggregate-demography behavior remains out of this checkpoint because the selected profile resolves it disabled.
- Required ALL EditMode, official Smoke, and `git diff --check` at the future implementation candidate gate.

## Readiness and gates

This is a technical proposal only. The accepted P12-B prerequisite-capability authorization covers bounded implementation after an independent exact-tip design review passes; no additional product-scope acceptance is indicated by current authority. This slice is **not `READY_FOR_IMPLEMENTATION`**: the sealed P12 owner-ID mapping for each supported lifecycle operation must be verified, and the operation boundary must prove multi-owner preflight/notification without relying on a nonexistent population P12 scope. P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, P12-C through P12-G retain their accepted dependencies, P13 remains blocked, and Phase 12 remains open.
