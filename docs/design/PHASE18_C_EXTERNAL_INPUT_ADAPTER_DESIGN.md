# P18-C — External-input and deferral adapter design

**Status:** Proposed technical design; independent review pending. This is a
bounded additive adapter required by P18-D, not a new P18-C capability claim.
P18-C remains promoted at `7aa7626`; current Phase 18 State is `311baa9`.
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
sequence and disposition owner. P18-A remains the sole timeline/input agenda
and ordering authority. P18-C owns decision-request identity/sequence,
availability-triggered reevaluation and the actor decision attempt. The adapter
is a narrow mapping/selection seam between those owners; it must not add a
second input queue, retained-command store, scheduler, or activity authority.

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
| P18-A input sequence/reference | Timeline-owned dispatch order at one exact `LogicalTick`; it does not order actor decisions. |
| P18-C `ActorDecisionRequest.Id` | Causal decision boundary identity, derived using the C-owned `BoundarySequence`, actor, instant, boundary ID and revision. |
| P18-C `BoundarySequence` | Monotonic C decision/request order. Never copied from P11 input sequence or P18-A sequence. |
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

P18-C's existing pending-request/retry state remains the request owner. Its
external-input adapter may keep a deterministic correlation/projection keyed
by stable request and input IDs, but may not retain a second independently
mutable copy of accepted commands. P11 remains authoritative for whether an
input is pending, deferred, dispatch-started, rejected or terminal. Queue/index
entries are rebuildable from P11 owner facts plus committed P18-C request and
trigger facts.

## 3. Ingress and eligibility

Ingress accepts only a committed P11 input receipt after the outer timeline
advance has successfully returned. The receipt carries exact
`ActorChoiceInputId`, actor `PersonId`, immutable target `LogicalTick`, P11
input sequence, and committed receipt revision/correlation. It is not accepted
from an owner callback during timeline dispatch. P18-C validates the correlation
against the retained P11 record by ID, allocates its own positive
`BoundarySequence`, derives the initial request ID, and records the immutable
originating C sequence used for FIFO selection. Duplicate delivery of the same
committed receipt is idempotent: it resolves to the same request/binding and
does not allocate another C sequence or attempt.

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
attempt. Every other due pending input stays pending and receives, at most, a
nonterminal deferral receipt identifying `DecisionBoundaryAlreadyUsed` at `t`.
“Defer without consuming/mutating the P11 input” means the accepted payload,
input identity, target, P11 sequence and pending eligibility are unchanged;
the store may append its already-supported nonterminal `Deferred` disposition
for diagnostics, but must not transition it to dispatch-started, rejected, or
terminal. If P11's current transition API cannot record a temporal deferral
without changing these command facts, the implementation must add an additive
owner operation or keep the deferral marker in P18-C's correlation projection;
it must not fake an absolute day/roster ordinal for an intraday tick.

An attempt is bound to one exact `ActorChoiceInputId`; never select through
`TryGetNextPendingForActor`, which can let an earlier future-dated choice mask
a due one. P11 retains its one-shot rule: a rejected, failed, returned-without
result, or durably thrown attempt has no autonomous fallback at that boundary.
Existing terminal P11 outcomes remain terminal. They are not retried as pending
commands.

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
terminal disposition. The adapter must resolve uncertain owner commit through
the existing owner receipt/idempotency contract before retrying. Only a
committed P11 terminal disposition retires the selected input. If the coordinator
cannot atomically retain request status, proposal/retry state and the P11
dispatch/disposition correlation, this is a design gap for the implementation
review, not permission to add a generic cross-domain transaction framework.

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
  input sequence, command correlation, current status, ordered dispositions and
  next transition/input sequences;
- P18-C next `BoundarySequence`, every external-input request ID and binding,
  immutable original C selection sequence, distinct later trigger identity/tick/
  revision/sequence, per-actor/per-boundary attempt or deferral marker, pending
  and processed request state, and any retryable proposal/idempotency receipt;
- the P18-B activity-instance identities, participant relations, lifecycle
  revisions/receipts and owner state required to recompute availability triggers;
- stable action semantic ID/version and configuration/content inputs needed to
  resolve the existing action provider.

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

P11 owns `ActorChoiceStore` payload, pending/terminal lifecycle and disposition
authority. P18-C owns external request IDs/sequences, request binding and
availability-triggered coordinator handoff. P18-A owns timeline agenda,
accepted input ordering and exact logical time. P18-D later owns composition
between those seams and the selected SellGoods execution path. Keep the adapter
out of `SimulationRuntime.cs`; P18-D alone edits that hotspot during its
explicit serialized ownership window. P18-D also owns its daily profile adapter,
boundary-yielding chronological driver, daily owner manifest/barrier and
SellGoods consumer. This design changes none of those files or behaviors.

## 8. Open technical gaps for independent review

These are implementation-contract questions, not product choices:

1. The promoted P18-C implementation has only lifecycle-receipt ingress and
   an in-memory pending/retry coordinator. Review must decide the smallest
   owner-backed representation for external request/binding/deferral cursors
   needed by this profile and its reconstruction inventory. It must not create
   a second command queue or assume a save implementation.
2. P11's current disposition boundary is day/roster based. Review must establish
   an additive exact-`LogicalTick` deferral/attempt receipt seam for intraday
   records while preserving historical daily records. A synthetic day or roster
   ordinal is not acceptable.
3. Exact atomic coordination among P11 dispatch-start/terminal disposition,
   C request/proposal state, and the existing action owner's idempotency receipt
   must be checked against concrete APIs before P18-D code is declared retry
   safe. No cross-domain transaction framework is authorized.

If these gaps cannot be resolved within existing owners and the bounded
contracts, return for a focused architecture review. Do not broaden scope to
solve them speculatively.
