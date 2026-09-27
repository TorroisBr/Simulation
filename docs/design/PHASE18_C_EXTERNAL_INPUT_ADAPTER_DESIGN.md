# P18-C — External-input and deferral adapter design

**Status:** Independent design review PASS at exact tip
`358c65c85e1eafdd91ef4a6553ba3b0a8c4af249`; bounded adapter is
`READY_FOR_IMPLEMENTATION`. This is an additive supporting capability required
by P18-D; no implementation or runtime behavior is claimed here. P18-C's
existing core remains promoted at `7aa7626`; current Phase 18 State is `311baa9`.
Architecture baseline is `c285466`. P11's bounded Actor Choice is canonical
at `308e24d` (code `0cd4281`). P18-D's reviewed consumer design is `aa5f182`.

**Authority:** `SIMULATION_ARCHITECTURE.md` §§2, 11–12, 91–92; `ROADMAP.md`;
`EXECUTION_MODEL.md`; Phase 11 and 18 Briefs/States; P18-A/B/C designs; the
P18-D design above; and `INTRADAY_EXTENSIBILITY_ALIGNMENT.md` plus
`MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md`.

## 1. Purpose and bounded boundary

Bridge a committed P11 `ActorChoiceInputId` into P18-C's existing
availability-driven decision coordinator. This design specifies only external
input identity, FIFO selection, retained deferral and re-eligibility. It does
not implement P18-D's SellGoods consumer, timeline driver, daily-boundary
manifest, or `SimulationRuntime` migration.

P11 `ActorChoiceStore` remains the authoritative accepted-input, payload,
input-sequence and status/disposition owner. P18-A remains the sole
timeline/input agenda and ordering authority. P18-C adds a request-state owner
for stable-value request, binding, trigger, deferral, retry and terminal
reconciliation receipts, with its own C sequence allocator. This is not a
second command queue: it stores correlations and causal receipts, never a copy
of P11 payload/status, availability truth, or scheduled work.

The selected command still means trusted local input for one `PersonId` and one
supported action. Its payload remains P11's existing actor/action semantic
identities. Normal action/domain eligibility and current-truth execution remain
in force. No actor-control grants, security boundary, or anti-cheat behavior is
introduced. The command's causal identity is never an `ActivityInstanceId`;
future P20-compatible activities keep instance identity separate from
`PersonId` and participant relations.

## 2. Identities and state ownership

Keep these identities distinct:

| Identity | Owner and meaning |
|---|---|
| `ActorChoiceInputId` | P11 identity of the immutable accepted command and its nonterminal/terminal disposition history. |
| P18-A accepted input reference/sequence | P18-A owns agenda acceptance, exact ordered input sequence and dispatch. P11 stores the accepted reference for correlation; it does not allocate or reorder the agenda sequence. |
| P11 temporal input record | P11-owned immutable command payload plus exact target `LogicalTick`, profile identity and linked P18-A accepted input reference; P11 owns command status/dispositions. |
| P18-C `ActorDecisionRequest.Id` | Causal decision boundary identity, derived using the C-owned `BoundarySequence`, actor, instant, boundary ID and revision. |
| P18-C `BoundarySequence` | Monotonic C decision/request order. Never copied from P11 input sequence or P18-A sequence. |
| P18-B lifecycle receipt sequence | Source sequence for committed activity transitions; it triggers C work but is never allocated from or reused as the C sequence. |
| `ActivityInstanceId` | P18-B identity of a concrete scheduled activity, distinct from Person and command/request IDs. |

On initial command dispatch, the exact input ID is the request's triggering
boundary identity and the request is bound to that same input ID. Retain the
originating C request sequence as the immutable FIFO selection key for that
input. If an input remains pending and a distinct later trigger makes it
eligible again, create a fresh `ActorDecisionRequest.Id` from that trigger's
stable identity, exact tick/revision and newly allocated C sequence; bind the
selected exact pending input ID separately. Do not rewrite the command's
payload, target tick, P11 input sequence, original boundary identity, or original
C selection sequence.

The additive P18-C request-state owner stores immutable stable-value receipts
for request creation, exact input binding, trigger observation, deferral,
retryable proposal, and terminal reconciliation. It owns and atomically
allocates the C sequence with its corresponding receipt; duplicate operation
IDs return the existing matching result, while an ID reused with different
content is an invariant failure. It exposes ordered snapshots, clone semantics,
reconstruction inputs and invariant validation. It does not retain P11 status
or payload. Pending/input lookup indexes are rebuildable projections only.
Lifecycle receipt source sequence is retained as provenance and remains
distinct from the allocated C sequence. The owner does not schedule timeline
work or decide current availability.

P11 alone answers whether its command is Pending, DispatchStarted, rejected,
returned, or thrown. Adapter deferral is represented by a P18-C deferral
receipt; P11 remains Pending and its payload/status are unchanged. P11 receives
only the temporal dispatch/reject/return/throw transition owned by its typed
intraday API below. For an intraday command, P11 also owns the accepted temporal
input record: immutable command payload and `ActorChoiceInputId`, actor,
action-definition identity, exact target `LogicalTick`, profile identity, and
the exact accepted P18-A input reference/sequence. P18-A alone owns that
agenda sequence and dispatch order. The P11 copy of the reference is for exact
correlation/reconstruction only; it cannot publish, reorder, or redispatch an
agenda entry.

## 3. Ingress and eligibility

Ingress accepts only a committed P11 input receipt after the outer timeline
advance has successfully returned. It carries exact `ActorChoiceInputId`, actor
`PersonId`, immutable target `LogicalTick`, profile identity, P11 input
sequence, and committed receipt revision/correlation. It is not accepted from
an owner callback during timeline dispatch. P18-C validates the correlation
against the retained P11 record by ID, then atomically commits its
request/binding receipt and its own positive `BoundarySequence`. The receipt
stores any P18-B lifecycle source sequence separately from this allocated C
sequence. Duplicate delivery of the same committed receipt is idempotent: it
resolves to the same request/binding and does not allocate another C sequence
or attempt.

P11's additive intraday capture operation accepts the typed P18-A accepted
input reference and the immutable local command payload, then retains the
one-to-one temporal input record before reporting accepted input to the caller.
The accepted P18-A reference is the agenda authority; P11 does not allocate an
independent sequence. Retrying the same capture reference and byte-equivalent
payload resolves to the same `ActorChoiceInputId`/record; reference reuse with
different payload, actor, profile or target tick is a correlation invariant
failure. The existing daily capture operation and its stored records remain
byte-level and semantically unchanged. P18-C binds only after both owners can
resolve this exact accepted reference and the P11 record by ID; reconstruction
compares profile, target tick, input sequence and payload correlation, and
rebuilds only indexes, never either owner's authoritative record.

At a completed decision boundary for actor A at tick `t`, the adapter considers
only P11 inputs that remain pending, are bound to A, have target tick `<= t`,
and have not already been attempted or deferred at `t`. Select the one with the
lowest immutable originating P18-C `BoundarySequence`; use stable
`ActorChoiceInputId` ordinal comparison only as a deterministic corruption-safe
tie-break, while duplicate C sequence ownership is an invariant failure. A
future-target input is ineligible and cannot mask an earlier due input. P11
input sequence controls timeline dispatch only; it does not override this
selection order.

For one actor and one exact meaningful boundary, make at most one decision
attempt. Every other due pending input stays pending and receives a P18-C
nonterminal deferral receipt identifying `DecisionBoundaryAlreadyUsed` at `t`.
P11 remains Pending and its payload/status are unchanged. Do not write this
deferral into P11 or map an intraday tick to a day/roster ordinal.

An attempt is bound to one exact `ActorChoiceInputId`; never select through
`TryGetNextPendingForActor`, which can let an earlier future-dated choice mask
a due one. P11 retains its one-shot rule: a rejected, failed, returned-without
result, or durably thrown attempt has no autonomous fallback at that boundary.
Existing terminal P11 outcomes remain terminal. They are not retried as pending
commands.

### P11 intraday transition records

P11 keeps its existing daily `Dispositions` collection and APIs unchanged in
byte-level and semantic behavior. The intraday accepted temporal input record
is retained with (or linked one-to-one from) the P11-owned ActorChoiceInput;
add a separate typed `TemporalDispositions` list for its intraday transitions.
Each record carries an immutable
`TemporalBoundaryReference` containing profile ID, exact `LogicalTick`, source
receipt/correlation ID and source revision, plus a per-input monotonic temporal
transition ordinal and the typed outcome. This temporal ordinal orders only
the temporal records for that input; daily and temporal streams cannot be mixed
for one accepted input. A profile transition is fixed at capture, and a legacy
daily input is never silently assigned a tick.

Add explicit idempotent owner methods for temporal `DispatchStarted`,
`Rejected`, `AttemptReturned`, and `AttemptThrew`. Each operation is keyed by
the stable temporal boundary/operation receipt reference. Repeating the same
reference and byte-equivalent typed result returns the original disposition;
reusing it with different tick, profile, input, source revision, or result is
an invariant/correlation failure. The methods do not accept or synthesize an
absolute day or roster ordinal. P11 remains the authority for resulting input
status. The accepted command payload and original capture facts remain
immutable.

Temporal invariants validate unique accepted-input references and unique
temporal operation references, contiguous positive temporal ordinals,
nondecreasing exact ticks, and valid lifecycle order. `SourceReceiptId` is
causal provenance/correlation, not the transition's operation identity; it may
be shared by distinct per-input and per-actor transitions when one source event
fans out (as P18-B activity receipts do to participants). Repeating one
`OperationId` with matching content is idempotent; reusing it with conflicting
boundary or result content is an invariant failure. This is the technical
interpretation of “unique source references” required to preserve
multi-participant fanout. A temporal rejection may terminalize before dispatch; a returned/thrown
attempt requires exactly one preceding dispatch. No transition may follow any
terminal result, and status must agree with the selected temporal stream. Daily
records continue to use their existing legacy invariant path. Diagnostics
expose both streams with explicit profile/boundary labels and must detect
duplicate operation references, profile mixing, bad ordering, missing
dispatch/terminal correlation, and disagreement between P11 and P18-C terminal
reconciliation.

## 4. Deferral, retries, and re-eligibility

Deferral is nonterminal and does not retarget or consume the P11 command. Record
the deferral reason, logical tick, causing request/boundary identity, and
selection sequence in owner-recoverable state. The same request/boundary cannot
select or attempt that input again at the same tick, including if the outer
driver calls the post-advance coordinator more than once. The normal driver
should perform one handoff only after successful outer advance.

Re-eligibility requires a distinct meaningful trigger, not elapsed wall time,
polling, a repeated call, duplicate receipt or object identity. Accepted input,
committed availability/condition transition, committed action disposition, or
explicitly scheduled domain due work may qualify under the owning P18-C
contract. A later request gets a new C request identity/sequence from that
trigger. The coordinator then selects the oldest still-pending due input under
the original C FIFO sequence. Inputs with target tick after the new trigger's
instant remain ineligible. If no qualifying later trigger occurs, the input
remains pending and reconstructible; it is not silently dropped or retargeted.

Temporary P18-B unavailability may defer only when current lifecycle authority
shows a commitment that can end or otherwise produce a meaningful future
availability trigger. The adapter stores no availability truth. It waits for a
later P18-B committed transition/condition signal, rechecks `IsAvailable` at
that new decision instant, then selects by the same FIFO rule. Permanent P11
terminal ineligibility remains terminal under existing domain/input semantics;
the adapter must not translate it into a retry. This is normal gameplay/domain
coherence, not security validation.

Distinguish decision-boundary deferral from an uncommitted execution result.
P18-C's existing `UncommittedRetryable` retains and retries the same request,
proposal and stable proposal identity; it does not replan or consume a P11
terminal disposition. Extend the P18-C executor boundary with operation-receipt
resolution keyed by the stable `ActorDecisionProposal.Id`: `Committed`,
`ProvenUncommitted`, or `Unresolved`. Retry the same proposal only after
`ProvenUncommitted`; unresolved status holds the request/input pending without
replanning or another effect attempt. The P18-C request-state owner records the
retry and terminal-reconciliation receipts idempotently. P11 dispatch-start and
terminal status remain separate owner commits from the sale effect. Recovery
queries the sale receipt and idempotently finalizes matching P11 and P18-C
terminal state; no generic cross-domain transaction is introduced.

## 5. Determinism, stale inputs, and failure

- Timeline envelopes are ordered by P18-A's stable input sequence at each exact
  tick. Decision selection uses the C-owned sequence described above.
- For several actors at one instant, P18-C's stable `PersonId` ordering and C
  request sequence govern attempts. Current domain truth is re-read for each
  execution, so an earlier committed transaction can affect a later one.
- A receipt whose P11 record is absent, whose actor/target/correlation differs,
  or whose identity is already bound inconsistently is an invariant/correlation
  failure. Do not synthesize or repair command facts in the adapter.
- A command scheduled at a sealed/past boundary follows the existing typed
  stale-boundary capture disposition. Do not move it to `now`.
- A failed outer `TryAdvanceTo` does not hand new input receipts to C. Already
  committed receipt/request state remains recoverable and is delivered once on
  a successful retry. A failed adapter transition must not consume a C sequence,
  P11 input, or retry identity unless its corresponding owner commit succeeded.
- Causal sequence exhaustion, duplicate IDs, stale revisions and incompatible
  source versions fail through explicit owner dispositions/invariants; never
  fall back to collection order or runtime object identity.

## 6. Reconstruction inventory

When this adapter is included in a continuation/fork profile, recover or
deterministically rebuild:

- P18-A profile/calendar, current instant, sealed input boundary, exact accepted
  input receipts and input sequence/order;
- P11 immutable command identity/payload, actor, action/version, target tick,
  intraday profile identity and exact P18-A accepted input reference/sequence
  for temporal records, command correlation, current status, legacy daily
  dispositions or additive temporal dispositions (not a mixture), and next
  transition/input sequences;
- P18-C next `BoundarySequence`, every external-input request ID and binding,
  immutable original C selection sequence, distinct later trigger identity/tick/
  revision/sequence, per-actor/per-boundary attempt or deferral marker, pending
  and processed request state, request-owner receipt sequence/snapshot, and any
  retryable proposal or terminal reconciliation receipt;
- the P18-B activity-instance identities, participant relations, lifecycle
  revisions/receipts and owner state required to recompute availability triggers;
- stable action semantic ID/version and configuration/content inputs needed to
  resolve the existing action provider; and any consumer-owned operation
  receipt needed to distinguish committed, proven-uncommitted, and unresolved
  execution status.

Derived selection indexes and projection entries can be rebuilt only from those
owner facts. `PersonId` is actor identity; `ActivityInstanceId` and participant
relations remain independent. Runtime objects, UI callbacks, history/diagnostics
and dictionary iteration are not the sole record of causality. This inventory
does not implement persistence or prescribe a serialized schema.

## 7. Focused validation and ownership

Implementation validation should cover exact input binding and duplicate
receipt idempotency; C sequence distinct from P11/P18-A sequences; due versus
future input selection; FIFO with several due inputs; same-actor one-attempt
limit; nonterminal deferral preserving the pending P11 payload; later distinct
trigger re-eligibility; repeated same-tick handoff suppression; availability
release and permanent terminal outcomes; stale/missing/mismatched owner facts;
outer-advance failure retention; proposal retry identity/no replanning; and
multiple actors with deterministic ordering/current-truth revalidation.
Reconstruction/invariant tests must detect orphan/multiply-bound inputs,
duplicate or nonmonotonic C sequences, future input masking, forgotten/duplicate
deferral, attempt without exact input binding, same-boundary reattempt, and
confusion among input, request, activity-instance and participant identities.
Also cover request-owner snapshot/clone equivalence, duplicate owner operations
with identical and conflicting payloads, temporal versus legacy P11 disposition
stream invariants, exact tick/profile correlation, and crash/retry between
DispatchStarted, sale receipt commit, and terminal P11/C reconciliation.

P11 owns `ActorChoiceStore` payload, pending/terminal lifecycle and disposition
authority. P18-C owns its request-state records, external request IDs/sequences,
input binding, trigger/defer/retry/terminal receipts, reconstruction and
availability-triggered coordinator handoff. P18-B lifecycle source sequences
are trigger provenance only, distinct from C sequence allocation. P18-A owns
timeline agenda, accepted input ordering and exact logical time. The adapter
code can be implemented independently after this design passes review and P9
and P11 validation is concluded; it does not require the P18-D
`SimulationRuntime` ownership window. Keep it out of `SimulationRuntime.cs`.

P18-D later owns composition with the selected SellGoods execution path, its
daily profile adapter, boundary-yielding chronological driver and daily owner
manifest/barrier. There is currently no SellGoods operation/idempotency receipt
contract. Before P18-D implementation, add the following bounded contract to
the existing economy transaction owner: an operation receipt/lookup keyed by
stable `ActorDecisionProposal.Id`, committed atomically with all sale effects.
Every attempt supplies an immutable request correlation/fingerprint containing
at minimum `ActorChoiceInputId`, P18-C `ActorDecisionRequest.Id`, actor
`PersonId`, action definition semantic ID/version, exact profile and execution
`LogicalTick`, resolved market/site semantic IDs, item semantic ID, requested
quantity, and every other resolved command/action parameter that determines
the requested transaction. The economy owner persists this correlation
immutably with the receipt. Same proposal ID and matching correlation returns
the existing receipt/outcome without reapplying effects; the same ID with any
different correlation is an invariant failure, never a fresh sale.

Separate immutable request parameters from execution-time current-truth facts.
On the first execution attempt that reaches the owner, re-read applicable
market/site mapping, current stock/availability, current price/value, actor
inventory/balance and other domain preconditions through their owners, then
record the exact current-truth snapshot/revisions actually used with the
operation receipt and committed result. A lookup for an already committed
proposal returns that recorded snapshot/outcome before re-reading truth or
applying effects. A `ProvenUncommitted` lookup permits the same stable proposal
and same immutable request fingerprint to retry; the owner may re-read and
record fresh current truth for that new proven-uncommitted attempt. It must not
report proven-uncommitted while an earlier attempt may have committed.

Lookup returns exactly one of committed, proven-uncommitted, or unresolved.
P18-C's executor boundary exposes this receipt resolution but does not
implement SellGoods or economy mutation. P11 `DispatchStarted` and the sale
effect are separate owner commits: after interruption, the adapter looks up
the operation receipt and idempotently finalizes matching P11 temporal terminal
state and P18-C terminal reconciliation. Receipt correlation, terminal
outcome, and input/request IDs must match before finalization. No generic
cross-domain transaction framework is introduced. If the existing economy
owner cannot provide this narrow contract, P18-D stays blocked for a focused
architecture review.

P18-D implementation remains blocked until the additive P18-A
returned-facts/subphase extension is accepted and promoted, this adapter design
passes independent review, and the serialized `SimulationRuntime` ownership
window is released. This gate is separate from the adapter implementation.
No implementation or capability promotion is claimed here.

## 8. Additive implementation contract requirements

These are specified bounded contracts, not unresolved product/canonical
choices:

1. P18-C implementation adds the request-state owner and idempotent stable-value
   receipts/operations, C sequence allocation, snapshots, clone/reconstruction
   inputs and invariant validation. Rebuildable indexes are projections only;
   no duplicate P11 payload/status, availability truth, or scheduled work.
2. P11 implementation adds a typed temporal boundary reference and explicit
   idempotent temporal dispatch/reject/return/throw methods. Exact tick/profile
   live in temporal records; P11 remains status/disposition authority; C deferral
   is a P18-C receipt and leaves P11 Pending. Daily APIs/records preserve their
   existing behavior, with a separate typed temporal disposition stream and
   explicit lifecycle/ordering diagnostics.
3. P18-C executor integration resolves operation receipt status by stable
   proposal ID and only retries after proven-uncommitted. The selected P18-D
   SellGoods consumer adds the operation receipt contract to the existing
   economy owner and idempotently reconciles separate P11/C owner commits.
   P18-C does not implement SellGoods.

These additive adapter contracts may be implemented after independent design
review and completion of P9/P11 validation. P18-D remains blocked by the
separate P18-A extension acceptance/promotion and `SimulationRuntime` ownership
window even if adapter implementation is complete.
