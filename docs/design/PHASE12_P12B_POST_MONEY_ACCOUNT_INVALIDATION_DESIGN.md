# P12-B owner-commit invalidation design refresh

**Status:** Bounded technical design candidate; base P12 canonical f913dd088f71b74cfab7c1bd8a1a79b7ce9a29ea. This refines the already accepted P12-B design against the post-Market/MoneyAccount owner and operation matrix. It adds no Phase/checkpoint ID, product behavior, architecture semantics, or implementation authorization beyond the previously accepted P12-B–P12-G prerequisite scopes.

## Purpose and boundary

The selected UnityBootstrap-Daily-v1 profile has owner-local census witnesses for more authorities after the Market and MoneyAccount promotions, but no complete shared commit epoch for supported writes. The existing ContinuationCensusProtocol already provides:

- versioned required-section contracts and owner-backed census witnesses;
- a partial owner-thread binding and caller-registered active-operation accounting;
- a monotonically increasing mutation epoch;
- NotifyCommittedMutation(s), which validates changed section IDs against registered owner identity/revision evidence and advances the partial epoch;
- roster reconciliation for the currently registered PersonStore and dynamic NPC families.

The design uses that coordinator and those owner APIs. It does not introduce a generic observer/event bus, lock manager, rollback framework, new save schema, or second source of world truth. Authoritative writes retain their owning domain's success/failure semantics.

## Required evidence boundary

A P12 profile can be eligible only when its immutable manifest identifies the exact supported bootstrap/build/runtime/configuration/content/provider composition and complete owner section set. Each section must specify stable section identity, schema, role, concrete owner identity, exact cardinality including explicit zero, and owner-local revision. Passive providers alone do not become protocol coverage until registered in this manifest.

The accepted daily profile excludes P10 topology, P14 flow, P18 temporal state, P19 extension state, P20 activities, P13 fork guarantees and external WorldCommand queues. Composed owners that are known empty still require an owner-issued exact-zero witness. P18 keyed-sale receipts remain an exact-zero conditional section for this legacy profile; the P18-D consumer is not imported into the P12 daily path.

## Commit and operation rules

1. **Owner commit notification.** A domain owner reports a change only after its authoritative local install and local revision have committed. The notification names the exact registered section IDs that changed and carries the post-commit census/revision from those owner instances. Failed preflight and no-op do not notify. Local revision deltas without a notification do not advance the common epoch.

2. **Atomic prepared install.** If a domain operation stages one logical install across several owners and publishes it atomically, notify once after the install with the full changed-section set. Do not report staged/uncommitted values.

3. **Multi-step or compensating operation.** A supported outer operation scope begins before the first possible authoritative commit and remains active through success, failure handling, compensation, and result publication. Each committed owner change must be notified while the scope remains active. A successful compensation is itself a committed change and must be represented; a later failure cannot erase an already committed write from the epoch. P12-B does not add whole-operation rollback.

4. **Continuation bookkeeping failure after a domain commit.** It cannot convert a committed domain success into a false domain failure or imply rollback. The P12 admission/eligibility coordinator faults closed, the operation scope exits safely, and no capture token can be issued from the faulty runtime.

5. **Operation scope.** Register a stable operation contract before sealing the profile, enter/exit it around every supported synchronous multi-owner boundary, and reject capture while any registered scope is active. The existing active count is accounting only; it is not a lock, store synchronization, or thread-safety promise. Daily and bootstrap scopes remain as currently reviewed and bounded.

6. **Direct owner entrypoints.** A public writer that can mutate admitted state must either route through an owner-bound notification boundary or be excluded/rejected by profile admission. An outer service scope does not cover a bypassable child writer. Direct call paths that remain supported must be listed explicitly.

7. **Owner/revision drift.** Census assessment rechecks the same registered instance, cardinality, and local revision. Any unnotified revision change, owner replacement, missing owner, or unexpected section faults/blocks eligibility. A later assessment detecting drift is fail-closed detection, not proof that the common epoch was current during the write.

## Dependency-ordered implementation sequence

These are implementation slices within accepted P12-B and its already authorized prerequisite work, not new checkpoint IDs:

1. **Current owner-set registration.** Reconcile the effective selected profile and explicit-zero exclusions into one expected section inventory. Reuse all promoted providers; do not add a provider where exact identity/count/revision already exist. Include only actual composed owners and distinguish P18/P14/P10/P19/P20 exclusions. Owner census completion is a dependency for eligibility, not proof of write coherence.
2. **First owner notification adapter: registered MoneyAccount family.** Bind each exact rostered MoneyAccountRuntime to the selected runtime's partial protocol, and report successful debit/credit commits against that account's existing section ID after its local revision advances. Enter an explicit operation scope before mutation so the existing owner-thread and active-operation checks cover direct account writes as well as daily nesting. Reconcile bindings at supported roster changes; same-ID replacement cannot inherit an old owner's binding. Preserve domain results if the coordinator faults after commit. This proves only MoneyAccount write invalidation; it does not complete EconomyTransactionService, Market, Inventory, City, or Merchant operation coverage.
3. **Selected economy transaction boundary.** After the MoneyAccount adapter, add exact scopes and changed-owner notifications for the supported transaction service and actual daily commerce entrypoints as one reviewed cluster. Map Market, Inventory, City/account, transaction/merchant receipt, NPC plan and Commercial Knowledge commits from current call paths. A scope around the service alone is insufficient while child owner writers bypass it. Keep keyed P18-D sale receipts excluded/zero unless a future accepted profile actually composes that consumer.
4. **Remaining supported operation families.** Integrate lifecycle/population, travel/TravelParty, Expedition, Justice/Crime, directives/ActorChoice, political/military and spatial owners in isolated owner/hotspot slices. Each slice must name owner sections, successful commit points, direct bypass closure, scope, compensation behavior and tests before wiring.
5. **Capture-eligibility lifecycle.** Only after the complete owner set and every supported mutation path are registered and independently validated, implement the accepted successful-boundary sequence/token rules. Recheck all witnesses after P12-G collection. Keep a missing owner or unsupported writer fail-closed; do not let partial protocol assessment advertise CaptureEligible.

The first implementation slice is deliberately the per-NPC MoneyAccount notification adapter because the exact identity/cardinality/revision family is already registered and its successful local commit points are bounded. It materially closes one frequent owner-write invalidation hole without claiming the enclosing economy operation is coherent. Market remains outside the protocol until the next economy-cluster registration; its local witness must not be used as an epoch substitute.

## Validation and review boundary

For each code slice, test successful commits, no-op and rejected operations, local revision drift, owner replacement, roster add/remove/reconciliation, wrong-thread attempts before mutation, nested day scope, and coordinator failure after an owner commit. Then run the required P12 focused suites, full EditMode, official Smoke and git diff --check; retain exact XML/log artifacts. Independent exact-tip implementation review and canonical promotion are separate gates.

The design candidate itself is documentation-only. It requires independent exact-tip design review and git diff --check; no Unity tests apply. The audit in PHASE12_POST_MONEY_ACCOUNT_BLOCKER_REFRESH.md is the current owner/operation evidence base.
