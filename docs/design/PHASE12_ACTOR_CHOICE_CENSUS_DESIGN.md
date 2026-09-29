# P12-B ActorChoiceStore live census witness design

**Status:** Submitted for independent technical review.

**Canonical base:** `codex/phase12/canonical` at
`1ada62b031e738e2bdd5d3d623e028a114961d6e`.

**Accepted capability scope:** P12-B through P12-G were accepted and their
prerequisite implementation work authorized at reviewed decomposition tip
`7585863`. This document narrows only the passive owner-witness work inside
that accepted scope; it does not deliver code, complete P12-B, or make P12-A
ready.

## Evidence and need

The selected `UnityBootstrap-Daily-v1` composition installs an empty
`ActorChoiceStore`. P12-F retains the P11 one-shot choice history, including
terminal dispositions and command-ID idempotency state. P18 temporal actor
inputs and dispositions are excluded from this daily profile. The store
contains both kinds of input, so a single total count cannot prove that the
excluded temporal subset remains empty after P11 choices have been captured.

`ActorChoiceStore` currently exposes total `Count` and copy-returning input
reads, but no owner revision. Successful input capture and later disposition
transitions can change owner truth while total count stays fixed. Its runtime
clone copies inputs, indexes, idempotency sets, and sequence state; the
installed store is a distinct owner from the constructor's source store.

## Bounded contract

Add two schema-v1 passive owner sections, both issued from the exact
`ActorChoiceStore` installed in `SimulationRuntime`:

| Section | Cardinality | Selected daily profile role |
|---|---|---|
| `p12f.actor-choice-inputs` | Number of retained inputs without a temporal capture | Required; zero at day zero |
| `p12f.actor-choice-temporal-inputs` | Number of retained inputs with a temporal capture | Explicitly empty; exact zero required |

Both witnesses use one stable opaque identity token owned by that exact store
instance and the same owner-local monotonic revision. The public composition
surface exposes only `IOwnerSectionCensusProvider`; it exposes no mutable
store or identity handle. The provider binds to the installed
`runtime.ActorChoiceStore`, not the pre-runtime store that may have been
cloned.

The owner revision begins at zero and advances exactly once after each newly
committed store mutation:

- successful new `TryCapture` or `TryCaptureTemporal`;
- each successful P11 disposition append through `TryDefer`, `TryReject`,
  `TryMarkDispatchStarted`, `TryRecordAttemptReturned`, or
  `TryRecordAttemptThrew`;
- each successful new P18 temporal disposition append through the temporal
  transition methods.

Every lifecycle transition keeps both cardinalities fixed. A same-semantics
temporal capture replay and an identical temporal operation replay remain
successful idempotent reads of prior results and do not advance revision.
Invalid, conflicting, duplicate, stale, or runtime-faulted attempts leave
cardinality and revision unchanged. A revision-exhaustion check must happen
before installing any new row or disposition; the existing boolean/outcome
API reports that bounded technical failure without wrapping the witness.

`ActorChoiceStore.Clone` preserves the copied input state, sequence,
idempotency indexes, and revision. The clone retains its own newly created
opaque owner token, so its identity is distinct from the source. The provider
is attached only after runtime construction to the installed clone.

The day-zero profile evidence must show both section counts at zero, stable
identity within the installed owner, and matching revisions. Once the runtime
has evolved, P11 terminal history remains censusable while the P18 temporal
section remains exactly zero for this profile. Read-only witness access does
not prove owner-thread affinity or quiescence; those remain separate P12-B
blockers.

## Files and validation boundary

Expected implementation ownership is limited to `ActorChoiceStore.cs`, the
passive provider/composition seam, and focused ActorChoice/bootstrap tests.
The existing `ActorChoiceStore.Clone` path is the only clone integration
required; no `SimulationRuntime.cs`, `SimulationTime`, daily loop, P18 lease,
shared mutation epoch, registration protocol, capture eligibility, export,
serialization, or staged hydration change is in scope.

Focused tests must establish day-zero two-section exact zero, shared stable
identity and revision, P11 and temporal capture cardinality, every same-count
transition's single revision increment, failure/replay stability, temporal
exact-zero separation, revision exhaustion without mutation, and clone state
and revision preservation with a distinct owner identity. Full EditMode,
complete official Smoke, and `git diff --check` remain required before
integration review. This passive witness does not close the complete live
owner census or any shared invalidation/owner-thread/quiescence obligation.

P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`.
