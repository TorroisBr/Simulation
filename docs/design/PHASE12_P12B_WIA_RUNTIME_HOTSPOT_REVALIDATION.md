# P12 / WI-A Runtime-Hotspot Revalidation

**Disposition: PASS** for the combined WI-A and P12 runtime/bootstrap interaction described below. This is a targeted source and test review. Tests were reviewed rather than rerun; no code or test files were changed.

## Reviewed tips

- P12 canonical at record creation: `b4c100d256fa0d6c624353ebefd437695100a85a` (tree `8fde7d291f92198a590bf8437ab149a560f9d652`).
- Promoted P12 direct-owner integration ancestor: `9d1474b4299d8e888dd387e02e9018d9e8627f84`.
- P12 direct-owner implementation: `c49f957e45c3059231e9ec66e4010a7c3a389988` (tree `627af2fbd7f93e0025106ee5ba87e72bae6c4ed2`).
- WI-A implementation: `3b39e0d89858dce517ad72cbb76da621eb954bad`, directly based on `9d1474b4299d8e888dd387e02e9018d9e8627f84`; full tree `b9a1a6fa5f003a0149ae9e41c2af1bc99b6b2b9d`; Assets subtree `492d3a17747e9537b25c4f16c957abf2a80904be`.
- Architecture baseline: `451340c56e9b676bf6ea43412bcb856b9ccde3de`, §§91A–91B.
- The supplied WI-A implementation review reference is `93b6f0e`; this record documents the separate targeted P12 interaction revalidation.

P12 canonical `b4c100d` contains `9d1474b) in its ancestry and has no Assets changes since that promotion. The current State keeps P12-B incomplete, P12-A `WAIT_DEPENDENCY`, and P13 blocked.

## Findings

### Bootstrap publication and P12 operation admission

The WI-A candidate preserves the P12 bootstrap publication scope. The composed runtime binds the P12 economy transaction service and begins the bootstrap publication scope before candidate validation. Genesis creates a private draft composition. Public accessors route through the `worldPublished` gate, which remains closed during all genesis callbacks, including the publish-stage callback.

After the pipeline returns, the code verifies the composition-entry thread and selected Unity Start thread, assigns the private composition while the public gate is still closed, disposes the P12 bootstrap scope, and requires a healthy `TryAssessNpcRosterCensus` result. It clears the unpublished ID and opens `worldPublished` only after those checks pass. Failure clears the draft, ID, and published reference, leaves the gate closed, faults active P12 admission, and latches bootstrap against retry. If the scope remains open in `finally`, admission is faulted before disposal.

The composition requires the exact same `WorldId` instance as its runtime. The typed P18 profile also requires that exact identity instance for a composed runtime; the legacy string profile remains scoped to identity-less standalone fixtures.

### Preserved P12 owner hooks

Compared with P12 implementation `c49f957`, the WI-A Assets tree leaves `MoneyAccountRuntime.cs`, `InventoryRuntime.cs`, `EconomyTransactionService.cs`, and `ExpeditionSystem.cs` unchanged. In `SimulationRuntime.cs`, the WI-A delta adds the `WorldId` property and constructor injection; it does not change P12 owner binding, admission checks, notification callbacks, or operation scopes.

Accordingly, the exact MoneyAccount and Inventory notifications, owner rebinding, partial-epoch checks, and existing NPC-trade/Open-market duplicate suppression remain intact. The WI-A bootstrap handoff closes and assesses the existing P12 scope; it does not replace or widen that scope.

### FR-B coherence boundary

This candidate adds WorldId publication/handoff only. It does not implement a factual read session or claim that the partial P12 mutation epoch proves a coherent Faction/Person read cut. The FR-B design requires its exact Faction/Person admission checks as the read-cut proof; P12 epoch/quiescence is optional reuse only when the selected profile and complete projected owner set are proven. The WI-A handoff explicitly preserves this boundary for later FR-B composition.

## Tests and retained validation evidence reviewed

The candidate test tree includes coverage for:

- `GenesisCallbacksCannotObserveWorldBeforeFinalPublicationGate`: public accessors remain unavailable at every genesis callback, including publication.
- `FailedPublishCallbackDiscardsIdentityAndPermanentlyLatchesBootstrap`: injected publish failure exposes no retained public composition or ID and prevents retry.
- `UnityBootstrapDailyV1PreDraftFailureNeverPublishesIdentityOrRetries`: selected-profile failure keeps identity/publication unavailable and latches retries.
- `AuthoredGeographyProfilePublishesOneReconstructibleP8AuthorityBeforeSimulation`: successful selected-profile composition is available after bootstrap and its P12 census/epoch assessment is healthy.
- `TypedWorldIdentityUsesLegacyCanonicalEncodingAndRequiresExactRuntimeHandoff`: typed P18 identity handoff and legacy encoding remain compatible.

The WI-A handoff note records Bootstrap focused 19/19, P18D consumer 10/10, Runtime Admission 25/25, ALL EditMode 2155/2155, official Smoke 5/5, and `git diff --check` PASS on the validated Assets tree. Those results were reviewed as retained evidence; this revalidation did not rerun tests or recompute artifacts.

## Scope and caveats

This revalidation does not close P12, make P12-A ready, unblock P13, complete a global P12 epoch, or implement FR-B. P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked.

The WI-A handoff note records that P12 canonical was still `2f7c742` pending promotion. That statement reflects its pre-promotion snapshot and is superseded by current canonical `b4c100d); the handoff note itself was not changed here. Later FR-B integration must again serialize the shared `SimulationRuntime` hotspot and preserve both the WI-A publication gate and P12 notifications while retaining FR-B's exact Faction/Person admission as its coherence proof.
