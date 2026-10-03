# P12-B ActorChoice owner invalidation design

**Status:** Independent design review PASS; bounded implementation may proceed
within the accepted P12-B prerequisite capability scope.

**Accepted capability:** P12-B — selected-profile admission and
completed-boundary lifecycle, within the already accepted P12-B prerequisite
scope.

**Canonical base:** `codex/phase12/canonical` at
`f1ec63ea7fa0592b3a280e138a80023e3cacc6b7`.

**Current owner evidence:** `ActorChoiceP11CensusProvider` and
`ActorChoiceTemporalCensusProvider` in
`Assets/_Project/Scripts/ActorChoiceCensusProviders.cs`; P11 store and
terminal lifecycle in `Assets/_Project/Scripts/ActorChoiceStore.cs`; current
selected-profile provider/operation registration in
`Assets/_Project/Scripts/SimulationRuntime.cs`.

## Gap

`SimulationRuntime` binds `ActorChoiceStore` to its ordinary
`AuthoritativeMutationGuard`, and the store exposes passive P11 and temporal
census providers. The selected P12 protocol does not register either provider
or bind a P12 commit callback for actor-choice mutations. Thus successful
P11 capture and lifecycle writes can change the store revision without
invalidating the partial P12 shared mutation epoch.

The selected `UnityBootstrap-Daily-v1` profile accepts preservation of P11
choice records and terminal dispositions, but excludes intraday state and has
no external `WorldCommand` service/queue. P12-A requires full choice history
when present and rejects pending/deferred or dispatch-in-progress choices at
a completed capture boundary. This design implements none of that final
capture/export policy; it only closes the existing P12-B owner-write
invalidation gap.

## Exact owner and cardinality boundary

Register the existing `ActorChoiceP11CensusProvider` as the
`p12f.actor-choice-inputs` schema-v1 required section on the exact
`SimulationRuntime.ActorChoiceStore` instance. Its owner identity is the
store's stable opaque `CensusOwnerIdentity`; its cardinality is the dynamic
`P11InputCount`, which can be zero or many. Do not assert one choice per actor,
one ActorChoice record total, or an Activity-to-actor cardinality.

The existing P18 temporal provider is a distinct section,
`p12f.actor-choice-temporal-inputs`, but intentionally reads the same store
owner identity and shared `CensusRevision`, with its own `TemporalInputCount`.
The P18 provider explicitly identifies temporal inputs as excluded from the
P12 daily profile. Keep P11 and temporal section identities/counts distinct;
do not combine their cardinalities or reinterpret a `PersonId`,
`ActivityInstanceId`, or shared `SourceReceiptId` as a one-to-one owner key.
This slice does not register the temporal section, admit intraday state, or
change P18 behavior. A future P12 profile that composes intraday inputs must
revalidate and separately specify that owner inventory.

At P12 protocol setup, verify the provider's section/schema, stable owner
identity, dynamic cardinality, and revision against the runtime's exact
ActorChoiceStore. A clone, similarly populated store, stale revision, wrong
schema, or mismatched owner must fail closed.

## Mutation boundary

Bind one P12 preflight/committed callback pair to the runtime-owned
`ActorChoiceStore` after its section/provider inventory has been sealed and
assessed. Retain the existing mutation-guard binding. For this daily-profile
slice, cover the existing P11 writes only:

- successful `TryCapture`;
- successful P11 `TryDefer`, `TryReject`, `TryMarkDispatchStarted`,
  `TryRecordAttemptReturned`, and `TryRecordAttemptThrew`, through their
  common `Append` commit point.

Preflight the exact current P11 witness, owner thread, current section
baseline before each supported P11 store commit. Standalone commits also
preflight epoch capacity; an enclosing supported batch reserves its single
close-time epoch at entry.
On success, advance the store's existing census revision and notify the
`p12f.actor-choice-inputs` section once after the commit. Route notification
through the existing `CanCommitP12MutationSections` /
`NotifyP12MutationSections` path so an already-active supported batch can
collect the section; when no batch is active, the protocol records one
committed owner mutation directly. Any impossible post-commit notification
failure faults the P12 runtime; do not retry or mutate a second time.

These are individual single-owner commits. Preserve existing transition
ordering and partial-result behavior: recording a Decision or executing its
action remains a separate existing operation, and this design does not wrap
SellGoods, introduce a new actor-turn operation, batch P11 with market or
DecisionStore writes, or change P11's no-fallback/rethrow semantics. Existing
P12 nested Market/TravelParty/solo-travel batching remains unchanged.

Rejected input, duplicate/replay no-op, invalid transition, exhausted input
sequence, exhausted census revision, wrong owner thread, or failed P12
preflight must not change ActorChoice records/revision or emit a mutation
notification. A successful mutation advances the existing store-local revision and is recorded by the P12 protocol. Outside a supported batch, each commit advances the partial epoch once; inside a supported batch, distinct changed sections advance it once when that enclosing batch closes. Preserve
normal non-P12 behavior when no `runtimeAdmissionContext` exists.

Temporal `TryCaptureTemporal` and temporal disposition methods remain outside
this accepted daily-profile mutation surface because P12 explicitly excludes
intraday state. Do not add P18 callbacks or alter P18-D's serialized timeline
window in this slice. If repository review finds that the accepted daily
profile can normally invoke those temporal writers, stop implementation and
classify the profile contract before widening this scope.

## Validation and review obligations

Focused coverage should prove exact store owner/schema, zero/one/multiple P11
input cardinality with a live owner identity, revision/current-baseline
matching, and no temporal-to-P11 cardinality conflation. Exercise successful
P11 capture and each disposition kind; assert one local revision per committed P11 mutation and one partial-epoch advance per standalone commit (or per enclosing batch when batched), including terminal returned and thrown outcomes. Cover failed/replayed no-op paths, wrong-thread/stale
baseline rejection, census-revision exhaustion, and epoch-capacity rejection
before store commit. Existing temporal identity/cardinality fanout tests must
remain unchanged and pass; they are not evidence of P12 temporal inclusion.

Run the focused ActorChoice and P12 protocol/composition suites, affected
EditMode regressions, ALL EditMode, official Smoke where applicable, and
`git diff --check`. Preserve result artifacts and exact code/tree hashes.
The final code tree requires fresh independent exact-tip review. Documentation
or unrelated Unity-generated ProjectSettings/`.meta` changes must not enter
the candidate.

## Exclusions and status

No ActorChoice export/hydration, capture token, P12-A readiness, P12-F
completion, global operation completeness, complete owner or shared-epoch
coverage, P13 readiness, or Phase closure is included. P12-B remains
`INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked.
