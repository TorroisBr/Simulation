# P18-D Merchant Trade-State Owner Execution Plan

**Status:** bounded owner contract for the accepted P18-D SellGoods consumer. This document defines the compatibility and receipt obligations for the existing daily `MerchantSystem.AdvanceNpcTradeState` operation. It does not claim that the operation is currently ready for inclusion in the resumable intraday profile: the current method has no complete prepared-install receipt, and its writes span owners that do not yet expose the required coordinated owner-local boundary.

**Scope:** one frozen daily-boundary step per eligible merchant runtime, preserving the existing call position and behavior. This is not a new trade feature. It does not execute a sale, advance travel, evaluate autonomous actions, add action semantics, or change the P14/P20/P19 boundaries.

## 1. Canonical ordering and operation identity

The step remains in the P18-D §4 daily sequence at the existing per-actor location: after the shared local-observation/commercial-sharing pass and the earlier daily owners, before scheduled-directive handling. P18-D excludes directives and autonomous action evaluation/execution from this intraday consumer; the trade-state operation itself remains included only if its full effect can meet this contract.

At boundary activation, freeze the exact `SimulationRuntime` NPC runtime-list membership and order as evidence, including total cardinality and every `NpcRuntimeId`. Preserve the constructor's initial `NpcRuntimeId` sort and the later append/remove behavior; do not sort again during retry. The eligible merchant subset is a separate ordered projection of that roster. Each projected descriptor carries the `NpcRuntimeId`, optional `PersonId`, and frozen disposition/eligibility inputs. `PersonId` is optional because NPC-only runtimes exist. Runtime-list ordinals express legacy execution order only and are never identity.

The stable operation identity is `(BoundaryOccurrenceId, StepId)`, where the boundary occurrence retains P18-A's `(worldId, profileId, absoluteDay)` identity and `StepId` is the semantic name `merchant-trade-state:<NpcRuntimeId>`. If the nested market observation is exposed as a distinct owner operation, give it the separate semantic identity `merchant-trade-observation:<NpcRuntimeId>`; do not derive either identity from a list ordinal. A repeated identity with the same immutable descriptor fingerprint resolves the retained receipt. Reuse of that identity with a different actor, optional backing `PersonId`, owner version, or frozen descriptor is an invariant failure, not a replacement operation.

Freeze inclusion and order from the activation snapshot; do not regenerate the roster, re-pair actors, or change the manifest after a partial boundary failure. The existing call-level guard is `IsMerchant(actor)`, alive, `CurrentCity != null`, and not traveling, after the outer runtime loop's null/dead/travel/expedition/reservation checks. Record enabled/absent merchant-system disposition as the manifest requires. Current mutable actor and market truth is captured when the owner prepares the step, after its declared predecessor steps have committed. If current truth no longer permits an effect, retain an explicit no-op/current-disposition receipt for the frozen step rather than silently dropping or substituting an actor.

## 2. Existing effect and compatibility contract

`MerchantSystem.AdvanceNpcTradeState` currently performs the following ordered work:

1. Enforce the merchant mutation fault guard and return for an ineligible actor.
2. Call `ObserveCurrentMarket(actor)` from inside the method. This second observation follows shared local observations and commercial sharing. It discovers `CurrentCity.Location`, then records that city's ordered valid item observations and liquidity observation. It is distinct from the earlier local-observation pass, even when the location IDs are equal.
3. Normalize merchant plan state: clear plan data for a local merchant, an inactive plan, or a plan whose item is no longer in inventory.
4. If an active plan targets another city, either set its trade travel intent and return, or clear/rewrite the incompatible intent and redirect the plan to the current city with the existing diagnostic.
5. Compute the planned amount as the minimum of configured maximum, remaining plan amount, and current inventory. Clear the plan and return if no positive amount remains.
6. If current local knowledge has no useful observation, increment destination wait days and return.
7. Evaluate the current observed local price, best local buyer, market-sale quantity limit, and configured minimum profit. If the existing local opportunity test succeeds, reset destination wait days and return.
8. Otherwise search the existing redirect opportunity in its existing deterministic order. When found, record the autonomous `TradeRedirect` decision and evidence, update the plan with the exact returned `DecisionId`, set travel intent, log the existing redirect diagnostic, and return. If no redirect exists, increment destination wait days.

The implementation must retain the same guard conditions, ordered observation calls, normalization rules, calculation inputs, destination/buyer tie-breaking, plan and travel-intent mutations, decision type/origin/evidence, diagnostics, and early-return behavior. The method does not execute a market sale. Setting `TravelPlan` is existing intent state; travel progression remains out of scope. Do not remove the internal `ObserveCurrentMarket` call, collapse it into the earlier refresh pass, move its writes across commercial sharing, or claim that a stale proposal/current-truth check authorizes a sale.

## 3. Owner boundary and prepared-install requirement

The P18-D coordinator owns only the frozen manifest/order, continuation cursor, and barrier status. It cannot own, stage, compensate, or combine the trade effect. The operation owner must expose an immutable prepare result and owner-local install/receipt boundary. A successful install atomically binds the operation identity and descriptor fingerprint to the committed effect, the before/after snapshot needed for diagnosis, and any owner-retained signals/outbox entries. Receipt lookup must be possible after an uncertain response without rerunning planning.

Preparation is read-only. It captures current truth and revisions after prior boundary effects, computes the exact branch and proposed writes once, and returns a prepared value. Immediately before install, every owner participating in that prepared value must still match its captured revision and identity. A stale revision, actor replacement, changed `CurrentCity`/location binding, or changed required input rejects the prepared value without partial authoritative mutation. Retry prepares again only after proving no install occurred; an unresolved install result is resolved by receipt lookup for the same identity/fingerprint and must never cause a second application.

The current `AdvanceNpcTradeState` path does not satisfy this boundary. The operation mutates `NpcRuntime` plan/travel state, can write `SpatialKnowledge` and `CommercialKnowledge` through its nested observation, can append a `TradeRedirect` decision through `NpcDecisionRecorder`/the decision store and shared record sequence, and can append log output through `SimulationLogger`. Those are distinct authorities, and the current method mutates them sequentially without a shared receipt or prepared commit. The promoted sale receipt is not a receipt for this operation. The existing merchant plan-urgency receipt covers only its separate `PendingTravelDays` pass and cannot stand in for these effects. No cross-owner transaction, rollback promise, or generic transaction framework is implied here.

The smallest compatible implementation seam is owner-local, receipt-backed suboperations under the already-frozen P18-D barrier, with explicit dependencies that preserve legacy order:

- retain the nested `ObserveCurrentMarket` occurrence as an owner operation after sharing and before plan normalization/decision; its affected spatial and commercial knowledge writes need an actor-owned atomic prepared install plus receipt, or an equivalent existing single owner that demonstrably owns both stores;
- install merchant plan and travel-intent mutations through a receipt-capable actor/merchant owner operation;
- when a redirect is chosen, append the decision and its stable sequence/identity through an idempotent decision outbox/receipt, then install the returned decision ID into the plan in a way that can be reconstructed without appending another decision;
- retain the existing diagnostic as an idempotent, keyed output/outbox if exactly-once replay-compatible logging is required; logger text itself is not domain truth and cannot be used as the completion receipt.

The decomposition may split the legacy method internally, but must preserve the operation's externally visible ordering and conditional results. Each child occurrence must have stable semantic identity beneath the frozen actor step, and the parent actor step is complete only when all included child receipts resolve. If owner-local prepared installs cannot represent these dependencies without cross-owner partial effects, the actor's trade-state step remains excluded from the intraday manifest until the missing owner seam is delivered and reviewed. Do not claim atomicity merely because the per-runtime advance lease prevents reentrant runtime advancement; that lease serializes the composition window but does not make multiple owner writes atomic.

## 4. Snapshot, revision, replay, and collision rules

The prepared descriptor and result must retain enough evidence to explain and safely replay the selected branch. At minimum capture:

- boundary occurrence, profile/configuration identity, owner operation/version, stable step identity, exact full roster membership/order and eligible projection;
- actor runtime object binding and `NpcRuntimeId`, optional `PersonId`, alive/job/merchant and travel state, `CurrentLocation`, `CurrentCity` object/runtime identity and its location identity;
- merchant plan identity and all relevant fields (active state, target city, item semantic identity, remaining amount, purchase price, wait state, and linked decision ID), travel-plan identity/state, and current inventory/item amount;
- simulation absolute day and relevant effective merchant/travel/knowledge configuration versions;
- the ordered current-city market/item/liquidity values and counterparties read by the nested observation, plus `SpatialKnowledge` and `CommercialKnowledge` revisions and the exact facts proposed for installation;
- every target city, route/travel predicate, market observation, buyer/redirect candidate, and ordering input actually read to choose a branch;
- decision recorder/store and causal record-sequence revisions/allocated identity when a redirect decision is needed;
- logger sink/configuration revision and the exact keyed diagnostic payload when a legacy log branch is taken.

Do not freeze post-predecessor mutable values at boundary activation. Freeze target identities/cardinality and declared versions there; prepare later against current truth. Before each owner commit, validate the exact captured object/semantic bindings and owner revisions for that child. Preserve distinct calls and results even if two observations refer to equal location IDs. Retained receipts are scoped to the owning `SimulationRuntime` lifetime unless a separately approved persistence contract says otherwise.

The canonical idempotency key is `(BoundaryOccurrenceId, StepId)` for the frozen actor operation and a deterministic semantic child key for each owner suboperation. Its fingerprint covers the immutable descriptor and inputs that define the intended effect. Equal key/equal fingerprint returns the original receipt, snapshot and child progress with no repeated observation, plan mutation, decision append, sequence allocation, or diagnostic delivery. Equal key/different fingerprint fails as an invariant violation. Do not create a new key for retries, use timestamps/list ordinals as identity, or treat a possibly committed but unresolved operation as safe to reapply.

If redirect decision creation and plan installation are separate commits, persist an owner-retained pending result containing the exact decision identity/payload and plan continuation before any retryable boundary can lose it. Reconciliation must attach that same `DecisionId`; it must never append a second TradeRedirect decision because the runtime response was lost. Similarly, keyed diagnostics are delivered from a retained outbox and do not define domain completion. All such partial progress remains visible to the barrier resolver so only the first unresolved child is retried.

## 5. Required behavior tests and validation

Focused tests for a future implementation should cover:

- frozen roster cardinality, exact membership/order, append/remove semantics, optional `PersonId`, merchant projection, stable semantic IDs, and rejection of duplicate/mismatched descriptors;
- all legacy eligibility guards and each early-return branch, including dead/nonmerchant/traveling/no-city actor, normalization, stale/missing item, invalid amount, missing observation, viable local buyer/market, redirect and no-redirect wait increment;
- exact full write-set and order, especially the second `CurrentCity.Location` discovery and ordered market observations after the earlier shared pass and before plan mutation;
- same-location duplicate discovery calls retained as two ordered occurrences; no observation or sharing reordering and no same-day propagation;
- stale actor object, city-object/runtime/location rebinding, plan/inventory/configuration/market/knowledge revisions, and stale route/destination data rejected before install with no partial mutation;
- no-op operation receipt; exact snapshot/revision provenance; same key/fingerprint replay after later actor/recipient changes returns the original result; conflicting key/fingerprint fails;
- fault injection before prepare, between prepare/install, after each owner-local install, and after install before response; retries resolve retained child receipts and never double-update plan, observations, decision sequence/record, or log output;
- TradeRedirect evidence, stable returned `DecisionId`, link to the plan, decision sequence collision/mismatch, and diagnostic/outbox ordering;
- deterministic results under identical frozen roster and owner state, without changing the legacy actor order or daily profile output.

Before integration, run the focused merchant/trade-state, local observation/spatial and commercial knowledge, NpcRuntime/travel-plan, decision/record-sequence, logger, and P18-D boundary continuation suites affected by the implementation. Then run the required affected daily regressions, ALL EditMode, complete official Smoke, and `git diff --check`; apply the P18-D long-run requirement because this changes daily-loop behavior. Keep validation tied to the exact integration tip and record the evidence with the candidate. This plan itself is documentation only and makes no validation or readiness claim.

## 6. Dependencies and ownership hotspots

The step depends on the promoted P18-A frozen manifest/subphase barrier and the per-runtime advance lease; all earlier P18-D boundary steps, including local observation and commercial sharing, must commit first. It precedes the legacy directive position, though directives remain excluded from the P18-D intraday profile. The sale owner capability is relevant only to the later SellGoods action and is not a substitute for merchant trade-state receipts.

Implementation touches architectural hotspots in `SimulationRuntime.cs` (composition and exact order), `MerchantSystem.cs` (legacy operation and owner adapter), `NpcRuntime.cs`/merchant plan runtime (actor-owned plan/travel state), spatial and commercial knowledge owners (nested observation), `DecisionRecords.cs` and its sequence owner (redirect decision), and `SimulationLogger.cs` (diagnostic delivery). Each hotspot needs a single isolated owner at a time, with owner-local receipt contracts reviewed before composition integration. Do not let the P18-D coordinator acquire these authorities.

This contract does not deliver P18-D consumer readiness or full-profile readiness. It does not add travel advancement, P14 material flow, P20 multi-participant behavior, P19 extension loading, autonomous action evaluation/execution, market sales, or new gameplay. If implementation evidence shows the required owner-local atomicity cannot be achieved within these seams while retaining the existing behavior, record the exact missing capability and keep this operation outside the intraday profile pending a separate reviewed technical checkpoint; do not weaken the owner boundaries or broaden this contract.
