# P12-B selected-profile RuntimeIdAllocator Decision-counter invalidation design

**Status:** bounded technical design prepared; independent technical review pending. No implementation or canonical promotion is claimed.

**Canonical base:** `codex/phase12/canonical` at `22525cb5f96eb9eed2e168b7e6a23fdc1e420304`, after the approved solo-travel and WX-D integrations and the current P12 State/matrix refresh.

**Accepted scope:** this is a small P12-B prerequisite capability slice within the already accepted and authorized P12-B owner-write invalidation work. It connects only successful `RuntimeIdAllocator.AllocateDecisionId()` writes on the selected `UnityBootstrap-Daily-v1` runtime to its existing partial mutation epoch.

## Contract and current evidence

- Existing census section: `p12c.runtime-id-allocator.decisions`, schema 1, exact `RuntimeIdAllocator` owner identity, cardinality one, and local revision `nextDecisionSequence - 1`.
- `RuntimeIdAllocatorCensusProvider.CreateProviders` already supplies this section. `SimulationRuntime` currently creates, registers, and binds only the Event counter provider; the Decision section is not in the selected protocol inventory and `AllocateDecisionId()` remains on the unbound generic allocator path.
- The selected bootstrap already passes the exact shared `RuntimeIdAllocator` into the runtime for the Event-counter capability. Reuse that owner and the existing owner-thread/admission lifecycle; do not add a new allocator, constructor authority, profile, or runtime operation.
- `NpcDecisionRecorder.RecordWithParticipants` allocates the Decision ID before allocating `SimulationRecordSequence`, constructing the record, and appending it to `NpcDecisionStore`. `TryRecordOccurrenceOnce` has the same order before store append and receipt installation. Therefore a successful Decision-ID allocation remains committed even when a later sequence allocation, record construction, store write, or receipt step fails.
- The Event counter and `SimulationRecordSequence` are independently versioned owners. Their callbacks do not make Decision-counter invalidation redundant. Each successful selected Decision allocation must report its own exact owner section; enclosing existing TravelParty, Merchant, or solo-travel batches may deduplicate sections at the existing outer close.

## Bounded implementation boundary

1. Add a provider factory for only `RuntimeIdAllocatorCensusCounter.Decisions`, using the existing section constant, schema, exact allocator identity, cardinality one, and local revision already defined by the census provider.
2. On the selected P12 admission profile, register exactly `p12c.runtime-id-allocator.decisions` as a required section with that provider. Keep all other allocator counters outside this slice. Ensure normal protocol assessment proves the section set and exact owner before binding.
3. Add a one-time Decision-ID mutation binding to `RuntimeIdAllocator`. `AllocateDecisionId()` checks exhaustion first; when bound, its admission callback verifies selected owner thread, exact registered section and unchanged Decision baseline, then checks shared epoch capacity before incrementing the cursor. An exhausted or rejected call does not change the counter, revision, or epoch.
4. After the counter increments, notify exactly the Decisions section using `NotifyP12MutationSections`. Existing active TravelParty, Merchant, and solo-travel scopes may absorb it; otherwise this one-owner commit advances the partial epoch once. If post-commit bookkeeping fails, fault-close P12 admission and propagate the existing fail-closed signal; do not roll back the consumed ID.
5. At bootstrap composition, compare the runtime-bound Decision provider with one built from the composition's exact allocator by section, schema, cardinality, opaque owner identity, and current revision. Mismatched owners reject composition, mirroring the Event-counter check.
6. Preserve all legacy non-P12 allocation output and behavior. Do not alter Decision ordering, record IDs, `SimulationRecordSequence`, decision-store policy, actor-choice behavior, or event recording.

No new operation ID is required: the ID allocation is a standalone successful owner write, and existing outer-operation contexts remain responsible for batching when active. This slice does not assert that every decision-store or ActorChoice mutation is covered.

## Required exact-tip coverage and validation

Focused tests must prove:

- the selected runtime registers exactly the cardinality-one Decisions section and exposes its local revision;
- composition rejects a provider tied to another allocator and accepts the exact shared owner;
- a direct successful allocation increments only the Decision cursor and advances the partial epoch once;
- admission rejection, stale baseline, wrong thread, exhausted ID counter, and exhausted epoch reject before changing the Decision counter;
- a successful Decision-ID allocation is still invalidated when the subsequent record-sequence allocation or decision-store/receipt step fails;
- an allocation inside an already-supported nested TravelParty/Merchant/solo-travel boundary joins that existing outer epoch step and preserves its existing changed-section validation;
- the `long.MaxValue - 1` epoch boundary permits one final representable step and the next allocation rejects before counter mutation at `long.MaxValue`;
- a non-P12 allocator preserves the existing `decision-000001` sequence and behavior.

Then run affected focused allocator, admission, decision-record, bootstrap, and nested-operation regressions; ALL EditMode; official Smoke; and `git diff --check`. Record XML/log hashes against the exact code tree. Obtain independent exact-tip implementation review before any separate canonical promotion decision.

## Hotspot and integration constraints

`SimulationRuntime`, allocator binding, and bootstrap composition are one serialized hotspot. Do not run a concurrent writer against those files. This design has no overlap with WX-D producer/package files, which are already canonical; it reuses the currently promoted Event-counter and solo-travel hooks without changing their semantics.

## Exclusions and reconstruction boundary

This is invalidation evidence for one existing allocator cursor only. It does not export or hydrate the Decision cursor; cover any other allocator counter; complete DecisionStore or ActorChoice census, write invalidation, or command lifecycle; add operation/security semantics; establish full owner or shared-epoch coverage; prove global quiescence or capture eligibility; implement save/load; make P12-A ready; complete P12-B; unblock P13; or close Phase 12.
