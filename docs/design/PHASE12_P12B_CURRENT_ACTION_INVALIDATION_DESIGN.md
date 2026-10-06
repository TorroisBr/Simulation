# P12-B selected-profile current-action census and invalidation design

**Status:** Proposed for independent design review. This is a bounded P12-B
owner/operation/epoch sub-slice, not a new numbered checkpoint and not a
P12-D/E/F export or hydration implementation.

## Baseline and authority

- P12 canonical base: `90a8a2aef212bec2c68d7224739ce512d44c9fe3`.
- Canonical source tree at that tip: `e3fbbd53689a4dd582e086e8c1677935d21ef78f`.
- Latest architecture source: `codex/architecture/world-identity-projection`
  at `e16796014d348e3b59da7ed848101c4c03926ba5`.
- P12 brief: `docs/phases/PHASE12_BRIEF.md`; P12 canonical State:
  `docs/PHASE12_STATE.md`; current owner inventory:
  `docs/design/PHASE12_OWNER_COVERAGE_INVENTORY.md`.
- The user-accepted P12-B capability scope and prerequisite implementation
  authorization are recorded in
  `docs/design/PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md`. P12-A remains
  `WAIT_DEPENDENCY` and its separate implementation authorization is not
  implied.
- This design assumes the promoted P12 Daily-v1 profile separation at
  `90a8a2a`: SampleScene uses the P9-B-only `Simulation-DailyV1.asset`, while
  the P10-A Ruin proving profile stays separate.

No architecture, product, or checkpoint-scope change is proposed.

## Evidence and gap

`NpcRuntime.CurrentActionRuntime` is part of the accepted NPC state carried by
P12-D/E/F's one whole-NPC snapshot. The owner inventory explicitly assigns
NPC current-action and commitment values to that same `NpcRuntime` owner and
snapshot. P12-B therefore needs to observe current-action committed writes
without creating a second authoritative action owner or writer.

At this base, the selected Daily-v1 composition registers per-NPC inventory,
account, Knowledge, travel-state, merchant-plan, and travel-plan witnesses;
`NpcPlanCensusProvider` contains only the latter two plans. No current-action
section or mutation callback is registered. `NpcRuntime.CurrentActionRuntime`
is a live mutable object. `SetCurrentAction` and
`SetCurrentActionRuntime` replace or clear it without a P12 boundary, and its
public setters can mutate it after installation. The normal Daily-v1 loop installs a chosen action before execution; the
scheduled-directive path also installs it, and NPC death clears it. Although
the selected profile begins with no Person-backed NPCs, its supported
`TryMaterializePerson` and `TryBindExistingNpcToPerson` operations can create or
bind them later. The P11 `ActorChoiceStore` is composed, so a supported
Person-backed SellGoods choice can reach action replacement/rejection inside
the registered daily-advance operation. A rejected choice clears an existing
action before domain truth checks; an accepted choice installs and executes
the action, which remains in the slot after the attempt. The profile still
composes no external `WorldCommand` queue. The current loop, not a new gameplay
operation, remains authoritative for when supported writes happen.

## Bounded contract

### Owner identity and cardinality

- The concrete owner is the existing `NpcRuntime` in the admitted NPC roster;
  no `NpcActionRuntime` owner registry or parallel action store is introduced.
- Add one passive section per exact installed NPC, with a stable section ID
  derived from the actor's unique `NpcRuntime.RuntimeId` (proposed prefix
  `p12b.npc-current-action/`). RuntimeId is a runtime owner key for this
  same-host selected profile, not a replacement for `PersonId` or a historical
  person identity.
- The section cardinality is `0` when both `CurrentAction` and
  `CurrentActionRuntime` are null, and `1` when they are consistent. Any split
  pair, duplicate roster RuntimeId, duplicate NPC instance, or action object
  installed as the current action of more than one NPC fails the census.
- When present, `CurrentActionRuntime.Action` must be the exact same
  `NpcActionData` object as `NpcRuntime.CurrentAction`. Any target NPC and City
  references must resolve to the exact admitted roster/City instance for their
  stable RuntimeId; a target item must resolve to its admitted definition ID.
  Do not allocate or repair identities during census.
- Providers are emitted in ordinal RuntimeId order. Dynamic roster add/remove
  uses the existing P12 membership operation and publishes the matching
  provider/boundary changes atomically with the roster change.

### Revision and supported writes

- Add a non-serialized, non-negative action-section revision owned by
  `NpcRuntime`; it starts at zero for a newly composed actor and advances once
  for every effective post-bind action-slot or installed-action-field change.
  A normalized setter call that leaves the value unchanged does not advance the
  revision. Revision overflow rejects before mutation and faults the P12
  protocol as its existing revision/epoch rules require.
- Route `SetCurrentAction` through the same action-slot boundary as
  `SetCurrentActionRuntime`. Preflight, detach the previous action boundary,
  install/bind the incoming action by exact reference, update the paired
  `CurrentAction` field, advance the section revision, then notify the existing
  P12 shared census epoch. A rejected or failed preflight leaves both fields
  and bindings unchanged.
- Bind all five existing `NpcActionRuntime` public mutators
  (`SetSuccessChanceMultiplier`, `SetOriginDecisionId`,
  `SetStableOccurrenceKey`, `SetCommercialDecisionEvidence`, and
  `SetCommercialScoutingEvidence`) only while that exact object is installed
  in its owning NPC slot. Mutations made before installation remain ordinary
  action construction and do not publish a runtime owner write.
- Replacing or clearing an action detaches its callback. A stale reference to
  a detached action cannot advance the current NPC section revision/epoch.
  Binding rejects one mutable action instance being current for two NPCs.
- NPC death already flows through a registered population/lifecycle mutation.
  Clearing a present action there advances the same action-section revision
  while the enclosing lifecycle operation reports its committed sections; it
  must not create an unscoped second epoch notification.
- Existing P12 operation contracts remain authoritative: normal autonomous,
  scheduled-directive, and supported Person-backed ActorChoice assignments run
  within the registered daily-advance operation. Person materialization runs
  within the existing membership operation and adds exactly one action section
  for a newly installed NPC; binding an existing NPC to a Person keeps that
  same section identity. Death clearing runs within the existing
  population-lifecycle operation. Preserve the existing rule that a pending
  actor choice first clears its prior action, then either rejects on current
  domain truth or installs the supported requested SellGoods action and
  records the returned/thrown attempt. P18 intraday action writes remain
  outside this selected Daily-v1 profile.
  Do not add an operation ID or change action semantics. A P12-bound setter
  invoked outside an admitted operation or from a non-owner thread fails
  before writing. Unbound non-P12 simulations keep their current behavior.
- A successful committed action write invalidates the shared census epoch
  through the existing `ContinuationCensusProtocol`; it does not issue a
  capture token. Any batching must use an already-supported surrounding
  operation scope and preserve the protocol's existing epoch semantics.

This is an action-section witness on the existing `NpcRuntime` instance.
It is not a whole-NPC revision guarantee: the later P12-D/E/F owner work must
continue to produce one coherent whole-NPC snapshot and revision over all of
that owner's included facts and relations. This sub-slice neither creates a
competing action writer nor claims that the remaining NPC state is covered.

## Implementation and integration sequence

1. Add an action census provider mirroring the existing per-NPC travel/plan
   provider style, but binding the witness to `NpcRuntime`, cardinality `0/1`,
   and the action-section revision.
2. Extend P12 composition and dynamic roster membership registration to seal,
   assess, bind, unbind, and refresh exactly one current-action section per
   installed NPC, including `TryMaterializePerson` and
   `TryBindExistingNpcToPerson`. Verify exact owner instance, RuntimeId mapping,
   paired action references, and single-owner action references at every
   read/write boundary.
3. Add pointer-bound mutation admission/commit hooks to `NpcRuntime` and
   `NpcActionRuntime`, including current-action replacement, clearing,
   in-place mutators, and lifecycle clearing. Keep all domain action choice and
   execution behavior unchanged.
4. Reuse the existing DailyAdvance and population-lifecycle operation
   protocols and shared epoch. Do not modify intraday P18 state, `CurrentAction`
   definition semantics, or any P12 export/hydration path.
5. Integrate serially at the `SimulationRuntime`/NPC census hotspot on the
   current P12 canonical base; no other writer may edit those same files during
   this slice.

## Required validation for an implementation candidate

- Focused tests for exact Daily-v1 action-section roster identity/cardinality,
  including empty day-zero slots, first install, same-definition replacement,
  clear, and stale/double-owner aliases.
- Focused tests for each of the five installed-action mutators, verifying
  unchanged writes do not claim a changed revision, successful writes advance
  the owner revision and shared epoch, and rejected writes leave action fields
  unchanged.
- Tests for autonomous and directive assignment, plus live Person materialize/
  bind and ActorChoice action replacement. Verify a current-truth rejection
  clears a prior action before recording the existing rejection disposition,
  while an accepted SellGoods attempt preserves its existing returned/thrown
  disposition and leaves the resulting action slot exactly as current runtime
  behavior specifies. Lifecycle death clearing must likewise retain domain
  semantics and produce the expected census invalidation. P18 intraday tests
  remain regression evidence for their own profile, outside Daily-v1 readiness.
- A roster add/remove temporal-identity test proving section identity and
  cardinality are updated at the registered membership boundary and do not
  silently rebind an old NPC/action reference.
- Focused suites, ALL EditMode, official Smoke, and `git diff --check` on the
  exact reviewed implementation tree. Retain inspectable results and hashes.

## Explicit limits

This slice does not implement or imply:

- complete P12-B owner, operation, or shared-epoch coverage;
- a global `NpcRuntime` composite revision, global quiescence, capture
  eligibility, or a completed-boundary token;
- P12-D/E/F immutable whole-NPC export, staged hydration, or serialization of
  action target/evidence references;
- new actor behavior, action types, actor-control/security rules, or gameplay;
- P12-A readiness, P13 readiness, or Phase 12 closure.

P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains
blocked.