# P12-B direct NPC owner-write invalidation contract

**Status:** Bounded technical contract for the accepted P12-B capability work. Design only; no runtime implementation, P12-B completion, P12-A readiness, or P13 readiness is claimed.

**Canonical design base:** `codex/phase12/canonical` at `b77e154e86b510c9f47ea9042fe5a1edb39749b0` (tree `af98d7172ae0c2ba22dfbf2a95695a194f4ca6bb`).

**Scope:** Connect successful direct writes through the existing per-NPC `MoneyAccountRuntime` and `InventoryRuntime` owners to the already-established census baseline and mutation epoch. Preserve current domain semantics, existing owner revisions, operation IDs, and runtime ownership. Do not claim complete P12-B coverage.

## Evidence and current gap

The selected P12 owner census already observes these exact owner sections:

- Money account: `p12e.npc-money-account/{RuntimeId}`, schema v1, exact `NpcRuntime` and `MoneyAccountRuntime`, cardinality exactly one, owner-local `Revision`. The provider rejects missing, duplicate, replaced, or aliased account owners. Promoted census: `9f615d84c80b797397b85ea1fac2e32361081370`; reviewed code tree: `58f0e76525a0ee6dc9b7f73f34dcbf71db944a09`.
- Inventory: `p12f.inventory/{RuntimeId}`, schema v1, exact `NpcRuntime` and `InventoryRuntime`, cardinality equal to live item-row count, and owner-local `Revision`. The provider does not reject aliased Inventory owners. Promoted census checkpoint: `15e5a543f3896c02f9dbd9c36c9ba75bc90dac2b`.

The two passive censuses do not by themselves invalidate the shared mutation epoch. The leaf methods themselves do not notify the protocol when they advance local revisions. Existing transaction-service callers manually notify for a bounded subset of named paths, but that path-specific wiring does not cover direct owner calls:

- `MoneyAccountRuntime.TryCredit` and `TryDebit` increment the local revision after successful positive-value balance changes; successful zero-value calls do not advance it.
- `InventoryRuntime.AddItem` increments the local revision for a supported addition; `RemoveItem` increments it only after a successful removal.
- `ContinuationCensusProtocol.TryValidateUnchangedSections` already verifies owner-thread affinity and exact registered-section baselines before a child write. `NotifyCommittedMutations` validates every named changed witness before updating baselines and advances the shared epoch once for the supplied changed-section batch.
- The existing Market mutation binding in `MarketRuntime` / `SimulationRuntime` is the local precedent for attaching pre-write and post-commit callbacks to an exact owner.

Relevant writer evidence is inventoried in `docs/design/PHASE12_B_BLOCKER_RESOLUTION.md`, “Committed-write invalidation map,” including the direct account and inventory leaves and their transaction-service call sites. NPC trade was promoted at `522cf9158d9f650675eccfb6bcec4144dbaa32e2` (reviewed code `49b32c548e4b8c666657c031105401246cff2563`; exact-tip review record `0279c8e40b5c51bdf5cd6dd03247832766a726de`). The Market operation candidate is `b577312c2edc6d3124c6d2b9dd3a1201dc9055ae`. Those integrations currently contain service-level account/inventory notices for their own paths; these must be removed when owner-level hooks are enabled to avoid double notification.

## Contract

### Owner and baseline identity

1. Keep the census section identities and witness schemas unchanged. Account cardinality remains exactly one per NPC. Inventory cardinality remains the actual live row count, alongside its revision; revision is required because quantity and cost can change while row count stays constant.
2. Bind mutation callbacks to the exact owner instances represented by accepted census providers. A callback may not resolve an owner afresh by RuntimeId and silently follow an in-place replacement.
3. Bind only after the relevant baseline and owner family are accepted. On a supported NPC roster add/remove or unregister/re-register operation, reconcile the existing owner families first, then bind the exact new owner set. Unbind removed owner instances. Retained NPCs keep the same owner binding. An unannounced same-roster owner replacement or an unresolved identity/section mismatch faults admission closed.
4. Account-owner aliasing remains invalid under the existing account census. Inventory-owner aliasing remains permitted by the existing inventory census: one successful write to a shared exact Inventory owner must name every currently registered NPC inventory section that refers to that same object in a single `NotifyCommittedMutations` call. Never select one alias, notify each alias separately, or count one physical write as multiple epochs.

### Pre-write admission

When the runtime admission adapter is active, each direct leaf calls its bound pre-write callback only on a path that is about to mutate:

1. Verify the current caller is the already-bound runtime owner thread.
2. Verify the callback is still associated with the exact account/inventory instance and current section IDs for the installed NPC roster. Inventory resolves to its full alias section set.
3. Call `TryValidateUnchangedSections` for every section that the write will change. This must happen before the first state mutation. Preserve the existing outer NPC-trade and Open-market operation preflights; the leaf check is a final owner-local guard, not a replacement.
4. If owner identity, thread, protocol state, or baseline validation fails, fault admission closed and perform no write. Preserve `TryCredit`, `TryDebit`, and `RemoveItem` rejection behavior (`false`). Add the explicit result-bearing `bool TryAddItem(...)`; it returns `false` when the write is rejected before commit. Keep the existing `void AddItem(...)` signature as a compatibility wrapper that delegates to `TryAddItem` and discards its result, but reserve that wrapper for legacy/nontransactional callers. No transactional caller may use `AddItem` or report success without checking `TryAddItem`.
5. A no-op/invalid path must retain its current domain behavior and must not fault or notify merely because it did no write: account amount zero, rejected debit/credit, invalid/no-op addition, failed removal, and owner revision exhaustion do not publish an epoch change.

Do not add player/actor authorization or defensive validation at this boundary. It is a runtime consistency and write-invalidation adapter for the trusted local game flow.

### Post-commit notification

After a successful leaf has changed its local revision, attempt to notify exactly the section(s) bound to that owner through `ContinuationCensusProtocol.NotifyCommittedMutation` for an account or `NotifyCommittedMutations` for an Inventory owner (including all current Inventory aliases). Use the already-existing witness provider to refresh the same local revision/cardinality baseline; do not copy or synthesize census facts.

- A successful positive debit/credit is one owner commit and attempts to advance its bound section baseline and shared epoch once. If notification is accepted, the baseline advances; a successful zero-value call does neither.
- Each successful `TryAddItem` / `RemoveItem` is one owner commit and attempts to advance the relevant Inventory baseline(s) and shared epoch once, even where only quantity or average cost changes and row count stays constant.
- Failed leaf calls and no-op calls do not notify. A successful compensating write is a separate successful owner commit and is notified; never erase prior commits or pretend an attempted compensation restored a prior epoch.
- Notification occurs after the local revision advances. Do not add a preflight epoch-capacity check. If post-commit notification fails or throws, including when the epoch is already `long.MaxValue`, fault the protocol closed and preserve the already-committed domain result: successful account methods and `TryAddItem` still return success. The protocol rejects the epoch update and subsequent census assessment is unavailable. Do not roll back or report that an already-completed leaf failed solely to disguise an unrecorded epoch. This matches the existing Market mutation-boundary behavior.
- Keep callback state nonserialized, matching the existing runtime mutation-boundary pattern. With no P12 runtime-admission context, these hooks must preserve current behavior and make no new admission/readiness claim.

Epoch saturation is a documented post-commit fault case, not a pre-write admission check. It does not grant a P12-B readiness claim.

### NPC trade and Open-market duplicate avoidance

Keep the outer named scopes and preflight behavior:

- `TryBeginNpcTradeCensusOperation` continues to validate both NPCs' account and inventory sections and opens the existing NPC-trade operation scope.
- `TryBeginP12MarketOperation` continues to validate the actor's account/inventory plus exact Market section and opens the existing purchase/sale scope.
- The direct MoneyAccount/Inventory leaves own their own commit notifications. Remove the matching service-level per-leaf account/inventory notifications in NPC trade, Open-market purchase, and Open-market sale, including successful compensating writes.
- Keep Market's own direct mutation callbacks/notices. The sale/purchase service must still notify the Market owner when its Market mutation commits; this contract changes only NPC account/inventory notification ownership.
- Keep transaction result and compensation rules otherwise unchanged. The leaf callbacks are nested in the current outer named scope where those transaction paths already use one. This contract does not add operation IDs or assert that every other writer or in-flight operation is tracked.

A trade may contain several independently committed account/inventory leaf writes. Each accepted notification updates its own baseline while the already-open outer operation keeps owner assessment unavailable. A compensating leaf is also notified. If any post-commit notification faults the protocol, the owner write remains committed and later baseline updates may be rejected. This contract promises committed-write invalidation for these owners; it does not redefine the trade as a single new transaction protocol.

### Result-bearing Inventory addition and transactional callers

`InventoryRuntime.TryAddItem(ItemData item, int amount, float unitCost = 0f)` returns `true` only when the call commits one local Inventory revision increment. It returns `false` for the existing invalid/no-op cases and for P12 pre-write admission rejection; rejected calls leave the Inventory unchanged. Preserve `void AddItem(ItemData item, int amount, float unitCost = 0f)` as a source-compatible wrapper for legacy/nontransactional setup and content callers only. It delegates to `TryAddItem` and intentionally ignores the bool. Transactional code must call `TryAddItem` and branch on the result.

Update and test every current transactional NPC-owner call site:

- `EconomyTransactionService.TryExecuteNpcTrade`: use `TryAddItem` for the buyer after buyer debit, seller credit, and seller Inventory removal. If it returns `false`, return `TransactionCommitFailed`; never return trade success. Those earlier owner writes are already committed. The current trade path has no rollback branch for a failed final add, and this bounded contract does not promise a new atomic transaction or guaranteed reversal. Any existing compensation that a caller performs remains a sequence of separately committed/notified writes.
- `EconomyTransactionService.ExecuteMarketPurchase` (the Open-market purchase path): use `TryAddItem` for the NPC after the money write and Market stock removal. If it returns `false`, return `TransactionCommitFailed`; never report purchase success. Preserve the already-committed money/Market effects truthfully; this contract does not promise rollback or atomicity.
- `ExpeditionSystem.TryRetrieveTargetResource` (the resource-retrieval path): use `TryAddItem` after `PlaceContentStore.TryTakeStack`. If it returns `false`, return `false` with a failure reason and do not commit/record objective completion. The place-stack removal has already committed; no restoration guarantee is introduced. This path gains leaf invalidation only when its performer Inventory is one of the bound NPC census owners; this does not add an expedition operation scope or cover its other owners.

`PlaceContentStackRuntime` uses its own InventoryRuntime and is not an NPC census owner; its existing `AddItem` use remains a legacy/nontransactional wrapper caller. Bootstrap fixture/setup writes in `TesteSimulacao` likewise occur outside the bound runtime transaction contract. Inventory test fixtures may continue using `AddItem` when they do not need a result. Audit all call sites so a transactional path never silently discards a `TryAddItem` rejection.

### Added test obligations

The implementation validation must include focused tests that prove:

- `TryAddItem` returns `true` and advances revision exactly once for a valid add; returns `false` without mutation for invalid/no-op inputs and for rejected pre-write admission; and preserves the `void AddItem` compatibility wrapper behavior for legacy callers.
- NPC trade, Open-market purchase, and `TryRetrieveTargetResource` return failure and never success when the destination `TryAddItem` rejects. Assert and document the already-committed earlier effects in each path; do not assert rollback that the contract does not provide.
- Inventory alias sections are included in one batch notification and the epoch advances once for that physical write; trade/Open-market tests detect duplicate leaf notifications while retaining Market notifications.
- With mutation epoch set to `long.MaxValue`, an otherwise successful account write and Inventory `TryAddItem` still commit and return success; notification faults the protocol closed, the epoch remains saturated, and a subsequent census assessment returns unavailable/faulted. This is the specified behavior; no capacity-preflight rejection is expected.

## Prepared installs: explicit non-coverage

The callbacks cover only the public direct mutation leaves named above. They do not intercept internal `InstallPrepared` methods.

- `EconomyTransactionService.TryExecuteKeyedMarketSale` installs prepared NPC Inventory/account and Market states as one receipt transaction. It remains outside this direct-leaf contract and the selected P12 profile. If a later accepted profile includes it, that operation needs a separately reviewed whole-operation preflight and one batch notification after the complete prepared install, covering every changed included owner section. Do not add notification to generic `InstallPrepared` methods, which could double-notify or publish a partially installed multi-owner transaction.
- `CityRuntime.TryCommitDailyEconomy` installs prepared Market and City population/settlement account state. Those are not per-NPC MoneyAccount/Inventory sections and remain in their independently inventoried writer families.
- `PlaceContentFoundation.TryCommitDayAdvance` installs content-stack Inventory state; it is not an NPC Inventory owner in this contract.
- Any other future prepared write to an included NPC account/inventory remains uncovered until it has an explicit outer operation adapter and exact changed-section batch. Changed revisions without a corresponding notification continue to fail census assessment closed; no passive rebase is allowed.

## Excluded writer families and claims

This checkpoint does not resolve or claim:

- all P12-B owner census coverage;
- all remaining City, Market, population, faction/group, PersonStore, genealogy/lifecycle, spatial, expedition, content, clock, or other C/D/E/F operation footprints;
- operation-scope coverage for arbitrary callers, runtime-wide serialization/quiescence, or all multi-owner transaction atomicity;
- shared-epoch coverage outside the exact direct NPC MoneyAccount/Inventory leaves and existing covered transaction paths;
- capture eligibility, export/hydration, P12-A readiness, Phase 12 closure, or P13 readiness.

Leaf writes made by a system such as an expedition are invalidated only if they pass through these exact bound public owners in the active profile. The expedition's other owners, outer operation scope, and coupled multi-owner semantics are not covered or promoted by that fact.

**Phase status remains:** P12-B incomplete; P12-A `WAIT_DEPENDENCY`; P13 blocked.

## Likely implementation ownership and collision map

Expected implementation files, if the separate post-review implementation gate is met:

- `Assets/_Project/Scripts/MoneyAccountRuntime.cs`: pre-write / post-commit hooks around `TryCredit` and `TryDebit`.
- `Assets/_Project/Scripts/InventoryRuntime.cs`: hooks around `TryAddItem` and `RemoveItem`, retaining `AddItem` as a compatibility wrapper.
- `Assets/_Project/Scripts/SimulationRuntime.cs`: bind/unbind exact owner callbacks, reconcile them with roster changes, resolve Inventory aliases to all section IDs, and implement owner-thread/baseline notification adapters.
- `Assets/_Project/Scripts/EconomyTransactionService.cs`: remove only duplicated account/inventory notifications on NPC trade/Open-market paths and handle `TryAddItem == false` as transaction failure; retain Market notices, scopes, and current partial-commit/compensation behavior.
- `Assets/_Project/Scripts/ExpeditionSystem.cs`: handle `TryAddItem == false` in `TryRetrieveTargetResource` after the source stack removal, without claiming rollback or operation-scope coverage.
- Focused new tests should prefer a new test source file to avoid collisions with active P12 census/composition test partitions. Extend `ContinuationCensusProtocol.cs` only if review proves the existing protocol lacks a required narrow preflight primitive; no general protocol redesign is in scope.

Do not modify `FactionStore`, `PersonStore`, `SimulationBootstrapComposition`, `TesteSimulacao`, ProjectSettings, or unrelated Unity metadata for this contract.

## Independent reviewer checklist

1. Confirm all file/method claims against canonical base `b77e154` and the exact promoted census/trade/Market records above.
2. Confirm account cardinality/identity and Inventory row-count-plus-revision are unchanged; confirm alias Inventory owners fan out once to all exact section IDs.
3. Verify pre-write checks occur before any state mutation, on the bound owner thread, and against every changed baseline. Confirm stale/replaced owner callbacks cannot silently rebind.
4. Verify only successful revision-changing leaves notify; zero/failed/no-op paths preserve existing semantics and do not notify.
5. Verify a post-commit notification failure faults closed without lying about or undoing the committed domain result.
6. Verify the trade and Open-market service no longer double-notify NPC owner writes, while Market notifications and existing outer operation scopes remain.
7. Inspect every `InstallPrepared` path and confirm the exclusions and future whole-operation batch requirement are accurate.
8. Confirm the selected contract does not imply global shared-epoch coverage, runtime quiescence, capture eligibility, P12-B completion, P12-A readiness, or P13 readiness.
9. Confirm no epoch-capacity preflight was added, saturation behavior matches the Market precedent, and tests cover committed domain success plus failed subsequent census assessment.
10. Confirm `TryAddItem` result semantics, the legacy `AddItem` wrapper boundary, every transactional caller above, and failure results after the earlier commits listed for each path.
