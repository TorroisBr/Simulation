# P12-B Selected-Profile SettlementPopulation Invalidation Design

**Status:** Draft technical design; independent review pending. This is a bounded P12-B sub-slice proposal, not an implementation candidate or a new numbered checkpoint.

**P12 canonical base:** `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e` (`origin/codex/phase12/canonical`).

**Current integrated runtime reference:** `75a27d7ac97e66c2762835ccea7950a945c2f20d` (`origin/codex/phase16/canonical`), a descendant of the P12 base. It includes the P12 mutation protocol and P16-A's explicit rejection of P15/P16 proving state under `UnityBootstrap-Daily-v1` admission. Any implementation must refresh both refs and serialize `SimulationRuntime` changes with active P17-A work.

**Authority:** The accepted P12-B capability scope and implementation authority are recorded in `PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md` and `docs/phases/PHASE12_BRIEF.md`. P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`. This proposal does not add product semantics, broaden the supported P12-A profile, or claim readiness.

## Purpose and bounded contract

Connect the existing per-City `SettlementPopulationRuntime` aggregate owner to the selected-profile P12-B sealed owner inventory and invalidate the partial mutation epoch after each supported, successful daily aggregate-demography commit. Reuse the already-promoted `SettlementPopulationCensusProvider`; do not create a parallel population owner or census surface.

The bounded source path is `SimulationRuntime.AdvanceDayAfterClockAdvance` → `DailyDemographicSystem.Advance` → (only when the effective `AggregateDemography.Enabled` flag is true) `ApplyAggregateDemography` → `AggregateDemographySystem.TryPropose/TryApply` without an operation identity → `SettlementPopulationSystem.TryApply` → `SettlementPopulationRuntime.TryApplyTransition`. Each successful per-City transition changes that installed population owner's aggregate revision and current population. The daily caller processes Cities in ordinal `RuntimeId` order. A failed proposal/application does not commit a population change.

The effective configuration predicate is part of this contract. `SimulationConfigurationDefaults.Create()` constructs default aggregate-demography configuration, and the selected `Simulation-GeneralTest.asset` has no serialized `aggregateDemographyEnabled` override in the current source tree. That does not by itself establish the final resolved profile value: resolve and record the effective `UnityBootstrap-Daily-v1` configuration before implementation; do not infer the runtime flag from authored initial values or the scene. Even if disabled in the current profile, every composed population aggregate remains a required owner section; only the daily writer path is inactive under that effective configuration.

## Exact owner sections

For each exact installed `CityRuntime.Population` owner, reuse the provider emitted by `SettlementPopulationCensusProvider.CreateProviders` and validate City `RuntimeId` against the owner's `SettlementRuntimeId`.

- Register `p12d.city-population.aggregate/<length-prefixed City RuntimeId>` as a required P12-B section. Its cardinality is `1`; its revision is the owner's aggregate `Revision`. The aggregate value itself is not in the census witness.
- Preserve the already-published `p12d.city-population.operation-receipts/<length-prefixed City RuntimeId>` provider, but do not silently treat receipt-ledger mutations as covered by this aggregate-only slice. The exact owner/cardinality/revision witness exists; receipt-backed commit invalidation remains an explicitly separate gap until designed and reviewed. The daily `AggregateDemographySystem.TryApply` call uses no operation identity and therefore does not install a receipt.
- Resolve and store the provider/section mapping during pre-bind inventory setup, require one aggregate provider for every published City and no duplicates, then seal the inventory. A missing City owner/provider, mismatched owner identity, duplicate stable ID, unsupported schema, or inconsistent initial witness fails closed.

This limited registration is evidence for one owner family. It does not complete P12's selected-profile owner inventory, turn the partial epoch into a global coherence proof, or issue capture eligibility.

## Mutation preflight, commit, and notification

Add a narrow owner mutation binding on the existing `SettlementPopulationRuntime` for the unreceipted aggregate transition path only. Before the first write, the admission callback must verify:

1. the runtime is using the accepted Daily profile and the bound Unity owner thread;
2. the exact `SettlementPopulationRuntime` instance still matches its City's aggregate census provider and stable section ID;
3. the current aggregate witness is exactly cardinality `1` and matches the owner's current revision;
4. the section baseline remains valid and the P12 protocol has capacity for one committed mutation notification; and
5. no existing incompatible population receipt/migration operation scope is active.

`TryApplyTransition` keeps its current validation and mutation ordering. It invokes P12 admission only after semantic/revision/overflow preflight has established that the transition can otherwise commit, but before changing `currentPopulation` or `revision`. A rejected P12 preflight leaves both unchanged. After the population value and aggregate revision commit successfully, notify precisely that City's aggregate section once. A post-commit notification failure faults/closes the P12 runtime admission path; it must not roll back the already-committed population transition or change existing population semantics.

A no-op, stale, invalid, underflow/overflow, mutation-guard rejection, or exhausted revision does not notify. A successful net-zero transition is still a committed owner write: notify once when the existing owner revision advances even if `CurrentPopulation` is numerically unchanged. Tests for a later implementation should assert exact owner identity, stable cardinality, revision delta, epoch delta, and unchanged state/epoch on rejected preflight.

The normal daily demographic transitions are per-City commits, not one atomic all-City transaction. The first implementation should notify each committed City section once. Do not wrap the full daily demographic pass as an all-or-nothing transaction or change its existing partial-progress/error behavior.

If a supported existing P12 batch collector is actually active around such a commit, reuse its established changed-section set and reserved epoch-capacity behavior. Do not add a generic batch framework. The normal daily aggregate-demography call is at the beginning of `AdvanceDayAfterClockAdvance`, outside the existing TravelParty, solo-travel, and Merchant-specific changed-section collectors, so ordinarily it reports the committed City section directly. `runtime.advance-day` is an active-operation/quiescence counter, not itself a mutation-epoch batch and not a substitute for owner notification.

## Explicitly separate mutation families

This proposal does not connect the following writers to the P12 epoch:

- `SettlementPopulationRuntime.TryApplyTransitionWithReceipt`, including receipt installation and replay semantics from `AggregateDemographySystem` or `PersonDeathLifecycle`;
- `SettlementPopulationRuntime.TryApplyPairedMigration`, which atomically changes two population owners;
- `PersonDeathLifecycle` or `NpcResidenceMigrationSystem` multi-owner commits, including Person/NPC/residence/operation-receipt changes;
- Population receipt `RestoreSnapshot` pruning/compensation; or
- population export, staged hydration, capture token issuance, global owner-thread proof, global quiescence, P12-A readiness, or P12-B completion.

These are not covered by registration of the aggregate section or by a daily-operation scope. A later P12-B slice must separately prove exact multi-owner preflight and one commit notification for receipt-backed and paired operations before treating them as covered. The implementation reviewer must check current Daily-profile reachability for all these paths and establish that any unsupported path cannot enter the selected profile unnoticed; do not solve that by guessing that a method is unused.

## Integration and ownership

The population census provider is already exposed by `SimulationBootstrapComposition` and its owner/store behavior is in `SettlementPopulationRuntime`. The missing work is the exact registration and mutation boundary in the P12 runtime plus the smallest focused tests and selected-profile assertions. `SimulationRuntime.cs` and protocol registration are shared hotspots: integrate serially with P17-A's selected-profile exclusion/rejection work and any concurrent P12 admission change. Do not edit P17 War/Conflict semantics in this P12 design.

The current P16 runtime has composition-level fail-closed checks for P15/P16 state under `UnityBootstrap-Daily-v1`; preserve them. P17-A separately must reject a P17-A War profile when P12 admission is supplied and reject P17 state under Standard composition. This population design adds no P17 owner behavior and makes no statement that its implementation may proceed concurrently in `SimulationRuntime`.

## Validation expected after design review and implementation authorization

- Focused tests for exact per-City provider registration, stable identity/cardinality/revision, successful unreceipted aggregate commit, net-zero commit, rejected/stale/saturated commit, and one epoch invalidation per committed City transition.
- A selected-profile test establishing the effective aggregate-demography configuration and exercising the normal `AdvanceDayAfterClockAdvance` path when enabled; when disabled, prove the owner remains registered and no mutation is inferred from a skipped consumer.
- Regression coverage for existing `AggregateDemography`, `SettlementPopulation`, `PersonDeathLifecycle`, and `NpcResidenceMigration` semantics, ensuring this slice does not claim their receipt-backed/paired writes are covered.
- Required ALL EditMode, official Smoke, and `git diff --check` at the future implementation candidate gate.

## Readiness and gates

This is a technical proposal only. The accepted P12-B prerequisite-capability authorization covers bounded implementation after an independent exact-tip design review passes; no additional product-scope acceptance is indicated by current authority. Until that review resolves the receipt/paired-path separation and confirms the effective-profile reachability boundary, this slice is **not `READY_FOR_IMPLEMENTATION`**. P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, P12-C through P12-G retain their accepted dependencies, P13 remains blocked, and Phase 12 remains open.
