# P12-B RuntimeIdAllocator Event-counter invalidation design review

**Result:** PASS — the bounded technical design is sufficient for
implementation under the accepted P12-B prerequisite capability
authorization.

## Review identity

- P12 canonical base: `aa8f0305bea9f10c15045e07400d8785c2bd9e23`
- Exact design commit: `b7788cfbbf960b1d2279ff4ec7457462c0af1c6d`
- Design branch: `codex/phase12/P12BRuntimeIdEventCounterInvalidationDesign`
- Scope: selected-profile shared-epoch invalidation for successful
  `RuntimeIdAllocator.AllocateEventId()` writes only.

## Findings

The design correctly limits registration and mutation callbacks to the
existing schema-v1 `p12c.runtime-id-allocator.events` section, with
cardinality one, the exact allocator owner identity, and local revision
`nextEventSequence - 1`. The provider's opaque owner-identity token refers to
that exact allocator instance; bootstrap composition must compare the live
witness to a provider built from its allocator.

Source confirms `AllocateEventId()` is the production entry point that
advances this counter. `DomainEventRecorder.Record` allocates the EventId
before invoking the event factory and attempting storage. Reporting
immediately after successful allocation therefore preserves invalidation
even when later construction or insertion fails. Exhausted or rejected
allocation leaves the counter unchanged and must not notify.

The existing `NotifyP12MutationSections` helper batches the section into an
active TravelParty or Merchant changed-section context and otherwise reports
the single owner commit directly. The proposed tests cover exact identity,
successful/rejected/exhausted allocation, post-allocation event failure,
nested TravelParty batching, and non-P12 behavior. No additional product or
canonical architecture decision is needed.

No actionable design findings remain. This review approves implementation
readiness only; it does not promote code or claim P12-B completion, complete
owner/epoch coverage, capture eligibility, export, hydration, P12-A readiness,
P13 readiness, or Phase 12 closure.
