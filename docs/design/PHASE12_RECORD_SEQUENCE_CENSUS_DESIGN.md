# P12-B SimulationRecordSequence census witness design

**Status:** Submitted for independent technical review.

**Canonical base:** `codex/phase12/canonical` at
`1ada62b031e738e2bdd5d3d623e028a114961d6e`.

**Authority:** Accepted P12-B–P12-G capability decomposition,
`docs/design/PHASE12_B_BLOCKER_RESOLUTION.md`, the current P12 Brief and
Execution Model, and the selected-profile owner inventory. This is one
bounded P12-B live-owner census capability; it does not implement P12-C
serialization or claim P12-B completion.

## Gap and bounded outcome

The selected `UnityBootstrap-Daily-v1` profile creates one
`SimulationRecordSequence` at authored genesis. Both its `NpcDecisionRecorder`
and `DomainEventRecorder` receive that same object. Its private cursor starts at
1; the normal day-zero bootstrap performs no event or decision allocation, so
the expected live state is one sequence cursor at next value 1.

Add one owner-issued passive census witness for that installed sequence. The
provider reports schema version 1, the exact `SimulationRecordSequence`
instance as owner identity, fixed cardinality 1 for the one sequence cursor,
and a local revision equal to the number of successful sequence allocations.
The owner can derive the exact next sequence value as `Revision + 1` while
preserving its current allocation semantics. Keep the cursor private and do
not expose a raw mutable sequence handle through the bootstrap composition.

## Owner and revision semantics

`SimulationRecordSequence.Allocate()` is the only supported mutation path for
the private `nextSequence` field. For the current owner state:

- `nextSequence` starts at 1;
- a successful allocation returns the old cursor and increments it once;
- the witness revision is `nextSequence - 1` after the operation;
- a sequence allocation remains consumed if later record construction or
  storage fails; that is existing causal ordering behavior and the census must
  report it;
- when `nextSequence` is `long.MaxValue`, `Allocate()` throws before changing
  the cursor, so the witness revision remains `long.MaxValue - 1`.

The witness cardinality is 1 because the owner contains one sequence cursor;
it is not the number of persisted decisions or events. Revision is the
allocation count and therefore changes even when an allocated sequence is not
retained in either record store. No separate mutable revision field is needed:
the current cursor exactly determines the monotone revision.

The normal game bootstrap must publish a provider constructed from the same
`recordSequence` field already passed to both recorders. Add a fixed
`SimulationRecordSequenceCensusProvider` to the existing
`SimulationBootstrapComposition` handoff and expose only its
`IOwnerSectionCensusProvider` interface. Use section ID
`p12c.simulation-record-sequence`. Do not add a provider registry, change
sequence allocation order, or treat independent demo/test sequences as part
of the selected game profile.

## Required tests

1. On a fresh sequence, assert one section, schema 1, exact owner identity,
   cardinality 1, and revision 0. Repeated reads remain stable and do not
   allocate.
2. Allocate multiple values and assert values remain consecutive while the
   witness revision advances once per successful allocation.
3. Use one shared test sequence with the existing decision and event
   recorders. After a successful decision allocation and successful event
   allocation, assert their record sequence values remain consecutive and the
   same provider reports revision 2. This proves both writers reach the
   witnessed owner without changing ordering semantics.
4. Cover a record construction/storage failure after sequence allocation and
   assert the consumed allocation remains reflected in the revision, matching
   current semantics.
5. At exhaustion, assert `Allocate()` fails without changing the witness.
6. In the selected-profile bootstrap test, assert the published witness has
   the expected installed-owner identity on repeated reads, cardinality 1,
   and revision 0 before day one.

The existing `CoreRuntimeTests.RecordSequence_SharesOneMonotonicSequenceAcrossDecisionsAndEvents`
continues to cover consecutive record sequence values. Extend the most
relevant existing test class or add a focused test file without changing
`SimulationRuntime`, `SimulationTime`, P18's advance lease, or P11 choice
behavior.

## Exclusions and readiness

Do not add RuntimeIdAllocator cursors, RNG provider state, record-store counts,
committed-write invalidation, shared mutation-epoch wiring, runtime
registration, owner-thread/quiescence enforcement, capture eligibility,
serialization, staged hydration, or restore behavior. The owner witness does
not establish that all P12-B owners are covered. P12-B remains incomplete and
P12-A remains `WAIT_DEPENDENCY` pending the complete included-owner census,
committed-write coverage, owner-thread/quiescence proof, and separate P12-A
implementation gate.
