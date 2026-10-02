# P12-B SimulationRecordSequence mutation-invalidation adapter

**Status:** Bounded technical contract for independent review. This is a
P12-B owner-invalidation slice based on current P12 canonical
`1ac675cc558aa919a749167647c10506c11303fc`. It does not change the accepted
`UnityBootstrap-Daily-v1` profile, deliver save/load, or make P12-B ready.

## Purpose and boundary

Connect the selected profile's existing causal record-sequence owner to the
canonical P12 census protocol so each successful sequence allocation advances
the partial shared mutation epoch after the sequence owner commits.

The exact owner section is `p12c.simulation-record-sequence`, schema 1,
cardinality 1. Its existing passive provider binds to the exact
`SimulationRecordSequence` instance and reports `CensusRevision`, which is the
last allocated sequence (zero before the first allocation). Production writes
to this owner use one method, `SimulationRecordSequence.Allocate()`. The source
call sites are the domain-event recorder and the NPC-decision recorder's
ordinary and idempotent-record paths. `Allocate` advances one monotone causal
cursor; it does not change an event/decision record store itself.

## Proposed integration

1. Pass the bootstrap's exact `SimulationRecordSequence` into the selected
   `SimulationRuntime` construction. Require it only when the P12 daily
   runtime-admission context is present; non-P12 test/demo compositions retain
   their existing behavior.
2. Before the partial census inventories are sealed, register the existing
   `SimulationRecordSequenceCensusProvider` as one required section under its
   existing ID/schema. Construct it from the exact sequence supplied by
   bootstrap and compare its opaque owner-identity token/revision with the
   already exposed bootstrap provider's witness; do not expose the sequence
   object through the census witness.
3. After the P12 owner thread is bound, bind one mutation boundary to that
   sequence owner. Before allocation, require the admitted owner thread and an
   unchanged witness for this exact section. After the increment, report one
   committed mutation for this section. A failed preflight or exhausted
   sequence does not increment the sequence or shared epoch.
4. On boundary-binding or post-commit notification failure, fault P12
   admission closed. Preserve the successful allocation as committed truth;
   do not pretend it rolled back. Existing call sites already treat
   `InvalidOperationException` during sequence allocation as a failed record
   attempt, so the implementation review must confirm that boundary-preflight
   failure is translated consistently and does not leak a partial record.
5. Reuse the existing sequence owner and provider. Do not add a second
   allocator, expose mutable sequence internals, allocate an entity ID, or add
   a new runtime operation ID. This is an owner-local mutation adapter, not a
   Travel, Expedition, ActorChoice, or daily-operation adapter.

The integration must be established before any post-bootstrap selected-profile
record allocation. Allocations performed while authored genesis is still
constructing the private composition remain part of the initial baseline; the
normal bootstrap publication scope and final census assessment must verify
that baseline before world publication.

## Source and dependency checks for implementation

- Verify all production `SimulationRecordSequence.Allocate()` call sites on
  the exact implementation base. The current source has one event-recorder
  call and the `NpcDecisionRecorder.RecordWithParticipants` and
  `NpcDecisionRecorder.TryRecordOccurrenceOnce` paths; no alternate setter or
  supported cursor replacement exists.
- Verify the bootstrap passes the same sequence to event and decision
  recorders and to `SimulationRuntime`. Compare opaque owner-identity tokens
  from the runtime-created and bootstrap-created providers; a matching
  revision from a different sequence is a rejection.
- Keep the adapter disabled outside the selected P12 runtime-admission
  context. Do not alter P18's timeline sequence, external world commands, or
  non-P12 demo/runtime behavior.
- Confine changed files to `DecisionRecords.cs`,
  `SimulationRecordSequenceCensus.cs` only if its existing provider needs an
  owner accessor, `SimulationRuntime.cs`, `TesteSimulacao.cs`, and focused P12
  tests. Any additional owner or runtime hotspot requires a revised reviewed
  contract.

## Required evidence

- Exact provider identity, section ID/schema, cardinality 1, and initial
  revision 0 register and assess on the selected bootstrap profile.
- Every successful `Allocate()` on that runtime increases the sequence
  revision and shared mutation epoch exactly once; the registered baseline is
  current immediately after notification.
- Event and decision recorder allocation paths both exercise the same bound
  owner and invalidate the epoch. An idempotent receipt replay that performs
  no allocation does not advance either revision or epoch.
- Wrong-thread or changed-baseline preflight rejects before sequence change.
  Sequence exhaustion also leaves sequence and epoch unchanged.
- A notification failure after allocation faults P12 eligibility closed;
  the test records the sequence allocation as committed and does not claim
  rollback. Repeated or nested surrounding P12 operation scopes do not cause
  duplicate notifications for one sequence increment.
- Existing non-P12 sequence users and existing cross-event/decision ordering
  remain unchanged.

## Explicit limitations

This closes only the `SimulationRecordSequence` writer edge for the partial
P12 epoch. It does not notify the corresponding event, decision, or receipt
owner sections; does not cover every mutation in any daily or action
operation; and does not prove complete owner/operation coverage, a universal
shared epoch, capture eligibility, owner-thread/quiescence for all supported
operations, export/hydration, P12-A readiness, P12-B completion, or P13
readiness. P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, and P13
remains blocked.

The remaining owner/operation matrix must continue to identify independent
uncovered stores and writers. A sequence allocation may follow a separate
domain-owner commit; this adapter invalidates the shared epoch for the
sequence change only and must not be used as evidence that the preceding
domain commit was covered.
