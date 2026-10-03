# P12-B RuntimeIdAllocator Event-counter invalidation design review — revision

**Result:** PASS — the revised bounded technical design is sufficient for
implementation under the already accepted P12-B capability authorization.
This review supersedes the earlier design review only for the revised design
boundary; it does not erase the earlier review history.

## Review identity

- Canonical base: `codex/phase12/canonical` at
  `aa8f0305bea9f10c15045e07400d8785c2bd9e23`
- Exact reviewed design tip: `b82ce73363c0c2e8e6601b461e9846c32ac5ab8b`
- Exact reviewed design tree: `c495a5402324aef9a7498888ef28d553aeefc383`
- Design branch: `codex/phase12/P12BRuntimeIdEventCounterInvalidationDesign`
- Scope: selected-profile shared-epoch invalidation for successful
  `RuntimeIdAllocator.AllocateEventId()` writes only.
- Review mode: independent exact-tip technical review; reviewer did not edit
  the candidate.

## Findings

The revision accurately distinguishes the existing owner-baseline check from
epoch capacity. `CanCommitP12MutationSections` validates thread, registered
section, active changed-section context, and unchanged owner baseline; it does
not reserve or preflight the protocol epoch. `NotifyCommittedMutations`
currently detects `long.MaxValue` after an owner write. The revised design
adds a small internal protocol capacity check before the selected Event
counter increments.

The ordering is bounded and coherent: allocator exhaustion first, selected
owner/thread/baseline validation next, epoch-capacity validation before the
counter write, and existing mutation notification after a successful write.
At `long.MaxValue`, Event allocation is rejected without changing the counter
and admission faults closed. At `long.MaxValue - 1`, the final representable
epoch step remains available. Allocations inside the existing TravelParty or
Merchant batch continue to coalesce into the outer operation's single epoch
step. The design does not add rollback or broaden transaction semantics.

The independent reviewer found no scope expansion or unresolved product or
architecture decision. Exact allocator identity, one-section registration,
post-allocation event-factory/store failure behavior, exhaustion, batching,
and required focused coverage remain in scope. The revised design is ready
for implementation; implementation review, validation, and canonical
promotion remain separate gates.

P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13 remains
blocked. This review does not establish complete owner or epoch coverage,
global quiescence, capture eligibility, export, hydration, or Phase closure.
