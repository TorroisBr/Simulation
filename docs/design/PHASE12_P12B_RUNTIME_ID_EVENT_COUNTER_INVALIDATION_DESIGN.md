# P12-B selected-profile RuntimeIdAllocator Event-counter invalidation design

**Status:** revised technical design independently reviewed PASS and sufficient
for implementation within the accepted P12-B capability authorization. The
earlier version at `b7788cfbbf960b1d2279ff4ec7457462c0af1c6d` overstated the
existing epoch-capacity preflight; that finding is corrected here and recorded
in `PHASE12_P12B_RUNTIME_ID_EVENT_COUNTER_INVALIDATION_DESIGN_REVIEW_REVISION.md`.
No implementation or promotion is claimed.

**Canonical base:** `codex/phase12/canonical` at
`aa8f0305bea9f10c15045e07400d8785c2bd9e23`. This base adds only the
post-TravelParty State/matrix record after code tree
`14e2f4e485a83791af43b781546bd6f90b3913f5`; the Event-counter source and
prior exact-tip evidence are unchanged.

**Accepted scope:** P12-B prerequisite capability work under the accepted
`PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md`. The current owner matrix
identifies the passive Event-counter witness as an unconnected causal root.
This design adds only selected-profile shared-epoch invalidation for successful
`RuntimeIdAllocator.AllocateEventId()` calls.

## Contract and current evidence

- Existing section: `p12c.runtime-id-allocator.events`, schema 1, exact
  `RuntimeIdAllocator` owner identity, cardinality one, local revision
  `nextEventSequence - 1`.
- The passive provider already exists in
  `RuntimeIdAllocatorCensusProviders.cs`, but the selected P12 protocol does
  not register it or bind an owner callback.
- The selected Unity bootstrap creates one `RuntimeIdAllocator` and shares it
  with `DomainEventRecorder` and the composed event-producing systems. The
  same exact allocator is available when `SimulationRuntime` is constructed
  and when `SimulationBootstrapComposition` publishes the profile.
- `DomainEventRecorder.Record` allocates an EventId before invoking the event
  factory and attempting store insertion. A successful ID allocation is
  therefore an authoritative allocator commit even when later event creation
  or storage returns failure.
- The Event counter is independent of the already connected
  `SimulationRecordSequence` counter. One must not stand in for the other.

## Bounded implementation boundary

1. Give the selected runtime the exact bootstrap `RuntimeIdAllocator` through
   an optional constructor dependency. For the P12-authored bootstrap, pass
   `TesteSimulacao.runtimeIdAllocator`; other runtimes without P12 admission
   keep existing behavior.
2. Create/register only the existing Events counter provider as a required
   section when both the P12 admission context and exact allocator are
   supplied. Keep the remaining thirteen passive allocator sections outside
   this protocol.
3. Add a one-time P12 mutation-boundary binding on `RuntimeIdAllocator` for
   `AllocateEventId()` only. Check allocator exhaustion first. Before
   incrementing the counter, the callback verifies the selected runtime owner
   thread, exact registered section and live owner baseline through
   `CanCommitP12MutationSections`, then separately verifies mutation-epoch
   capacity through a small internal `ContinuationCensusProtocol` check. The
   existing `CanCommitP12MutationSections` path does **not** check epoch
   capacity; `NotifyCommittedMutations` currently discovers a saturated epoch
   only after the owner write. If either preflight fails, no EventId is
   allocated. An epoch-capacity failure fault-closes admission, matching the
   protocol's existing saturated-notification behavior.
4. After the Event counter advances successfully, notify exactly
   `p12c.runtime-id-allocator.events` through the existing
   `NotifyP12MutationSections` helper. This appends the section to an active
   TravelParty or Merchant changed-section context where one exists; otherwise
   the one-section owner commit advances the partial epoch directly. A
   notification failure after allocation fault-closes P12 admission and
   propagates the existing failure signal.
5. At bootstrap composition, compare the runtime-bound Event provider with a
   provider built from the composition's allocator. Require schema, section,
   cardinality, owner reference and current revision to match. A mismatched
   allocator must fail composition rather than silently bind a different
   causal root.
6. Check allocator exhaustion before admission. Exhausted or rejected calls
   leave the next counter and local revision unchanged and emit no commit
   notification. Epoch capacity is checked before each selected EventId
   allocation, including inside an active TravelParty or Merchant batch. A
   successful allocation notifies even if subsequent event construction or
   store insertion fails. At `long.MaxValue - 1`, one direct allocation may
   advance the epoch to `long.MaxValue`; the next allocation is rejected
   before changing the Event counter. When nested in a multi-owner operation,
   the existing outer commit remains responsible for the single epoch step.
   This boundary does not add rollback semantics to those operations: it only
   guarantees that a rejected EventId allocation does not advance the Event
   counter after epoch capacity is exhausted.

No `DomainEventRecorder` API change or new operation ID is required. The
existing nested TravelParty and Merchant contexts own batching when active;
this design does not claim that every event allocation belongs to a named
multi-owner operation.

## Files and hotspot ownership

Expected source/test surface:

- `Assets/_Project/Scripts/RuntimeIdentity.cs`
- `Assets/_Project/Scripts/RuntimeIdAllocatorCensusProviders.cs`
- `Assets/_Project/Scripts/SimulationRuntime.cs`
- `Assets/_Project/Scripts/TesteSimulacao.cs`
- `Assets/_Project/Scripts/SimulationBootstrapComposition.cs`
- focused allocator/admission/bootstrap EditMode tests (new or existing).

`SimulationRuntime` and bootstrap composition are a single serialized
hotspot. No concurrent candidate may edit or integrate those files during this
slice. Do not modify unrelated ProjectSettings or untracked `.meta` files.

## Required validation and independent review

Focused coverage must prove:

- exact selected Event section identity/schema/cardinality/revision and
  exact-owner composition;
- successful direct `AllocateEventId()` advances its local revision and the
  partial mutation epoch once;
- rejected admission and exhausted allocation do not advance either;
- an epoch already at `long.MaxValue` rejects before the Event counter
  changes, and the protocol is fault-closed; the `long.MaxValue - 1` boundary
  permits the final representable epoch step;
- successful allocation still invalidates if a later event factory/store step
  fails;
- allocation during `TravelPartySystem.AdvanceParties` joins the existing
  one-step nested operation epoch with record-sequence and domain-owner writes;
- non-P12 allocators retain their existing ID output and behavior.

Then run the affected bootstrap/admission/TravelParty regressions, ALL EditMode,
official Smoke, and `git diff --check`. Commit/push the exact candidate and
validation evidence; an independent exact-tip implementation review is
required before requesting canonical promotion.

## Exclusions and reconstruction boundary

This is invalidation metadata for the existing Event-ID next-value state; it
does not export/hydrate the cursor, alter IDs or ordering, mutate the event
store, cover decision IDs, change record-sequence behavior, add any of the
other thirteen allocator counter sections, or add an outer solo-travel
operation. Existing Event allocation remains causally observable through its
counter; this slice only connects successful writes to the partial P12 epoch.

It does not establish complete owner/operation/epoch coverage, global
quiescence, capture eligibility, export, hydration, P12-A readiness, P12-B
completion, P13 readiness, or Phase 12 closure.
