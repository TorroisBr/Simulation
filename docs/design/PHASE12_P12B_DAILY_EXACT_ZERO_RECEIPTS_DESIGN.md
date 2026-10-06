# P12-B selected-profile exact-zero receipt owners

**Status:** Bounded technical design candidate; base P12 canonical `ea4decdaffa26e80e76ee72135ead0a7d673f358`. This is a slice within the accepted P12-B owner/cardinality and fail-closed admission contract. It does not add a checkpoint ID or change P12 scope.

## Finding

The selected `UnityBootstrap-Daily-v1` composition creates both `NpcDecisionRecorder` and `EconomyTransactionService`. Their owner census providers expose stable section identities and exact owner instances:

- `NpcDecisionRecorder.OccurrenceReceiptSectionId` (`p12f.npc-decision-occurrence-receipts`), schema v1;
- `EconomyTransactionService.KeyedSaleReceiptSectionId` (`p12e.economy-keyed-sale-receipts`), schema v1.

Both providers currently report cardinality zero and revision zero in the accepted Daily-v1 bootstrap test. The Daily-v1 profile does not compose the P18-D consumer paths that populate either retained receipt collection. The providers are exposed by `SimulationBootstrapComposition`, but neither section is registered in `SimulationRuntime.InitializeNpcRosterCensusProtocol`; therefore the sealed partial P12 protocol does not bind their exact-zero role or recheck them during a later `TryAssessNpcRosterCensus` call.

The current selected-profile writer audit found no additional unnotified committed write in the supported Daily-v1 paths. Autonomous theft has successive account, Crime/Social, and Justice sub-commits with their existing owner callbacks; the accepted completed-day boundary is serialized outside the actor turn. No new theft-wide operation is justified by current source evidence. This design addresses the remaining concrete exact-zero owner gap only.

## Bounded implementation

1. Register both existing providers before the P12 section/provider inventories are sealed, using `OwnerSectionRole.ExplicitlyEmpty` and each provider's declared schema version.
2. Validate the initial provider witnesses against their exact section IDs, schema versions, non-null stable owner identities, cardinality zero, and nonnegative revisions. Missing providers or any nonzero baseline fail the selected P12 admission closed.
3. Keep the sections fixed and owner-backed. Do not add a P18-D writer, receipt mutation callback, receipt export/hydration, or a general dynamic receipt protocol. The P18-D consumers remain excluded from this profile.
4. Use the existing `TryAssessNpcRosterCensus` validation behavior for later exact-zero checks: `ExplicitlyEmpty` rejects a populated witness, and unnotified revision/cardinality drift fails closed. This is a bounded partial-census check, not a completed-day capture API.
5. Add the already-composed `EconomyTransactionService` as an optional final `SimulationRuntime` constructor dependency and pass that same instance from `TesteSimulacao`; `InitializeNpcRosterCensusProtocol` runs in the constructor, before the existing `BindP12EconomyTransactionService` call. Keep the later bind for its current Market and transaction callback wiring. The existing `NpcDecisionRecorder` constructor dependency supplies its provider.
6. Add a constructor flag, defaulting to `false`, that requires these two sections for the full selected bootstrap inventory. `TesteSimulacao` sets it when constructing the actual `UnityBootstrap-Daily-v1` runtime. If required and either provider is absent, selected bootstrap admission fails closed before publication. Adapter-only unit fixtures that intentionally exercise a partial protocol leave the flag false and do not claim the full profile inventory.

The current ten-NPC/two-City Daily-v1 protocol has 144 sections before this slice: the 142 NPC/Person sections and two registered per-City Market sections. The two fixed receipt sections make it 146. Test coverage must assert both exact receipt witnesses and the 146-section sealed inventory on the authored Daily-v1 composition; other complete-profile fixtures should assert the count derived from their actual NPC and City roster. Add fail-closed cases for missing each required provider with the full-profile flag set. Non-P12 runtimes and adapter-only unit contexts retain their existing partial protocol behavior.

## Tests and validation

- Extend selected Daily-v1 admission/inventory tests to assert exact receipt section ID, schema, stable owner identity, zero cardinality, and current revision through the sealed runtime protocol.
- Verify a missing provider under the full-profile flag, wrong section/schema/owner, or initially populated receipt owner prevents selected-profile admission.
- Verify post-admission populated or revision-changed receipt evidence makes `TryAssessNpcRosterCensus` fail closed; do not add anti-cheat or forged-command handling.
- Verify unbound/non-P12 `SimulationRuntime` behavior and the existing P18 receipt consumer tests remain unchanged.
- Run focused receipt/admission/protocol suites, all EditMode tests, official Smoke, and `git diff --check` on the exact implementation tree.

## Limits

This closes two exact-zero sections in the selected partial census only. It does not establish a complete profile owner manifest, all receipt writers, complete shared-epoch coverage, runtime-wide quiescence, capture eligibility, P12-B completion, P12-A readiness, export, hydration, P13 readiness, or Phase 12 closure. P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked. The P12-to-P18 profile boundary is unchanged.
