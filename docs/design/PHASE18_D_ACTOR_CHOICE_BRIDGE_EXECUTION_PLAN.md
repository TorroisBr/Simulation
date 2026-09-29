# P18-D Actor-Choice Temporal Bridge — Execution Record

## Scope and base

This bounded consumer step implements the actor-choice bridge described by
the reviewed P18-D design. It connects retained P11 actor-choice inputs to the
promoted P18-C request-state API. The implementation branch
`codex/phase18/P18DActorChoiceDecisionBridge` is based on
`codex/phase18/P18DConsumerIntegration` at
`2dd57cc87a7c22ff9968c6e1a6a86044a6bc83a0`, which descends from P18 canonical
prerequisite tip `9e790c59e14ca7f7ed195c0e6267e10f3cd039d7`. Current candidate
tip: `4016a73a402c33d33722c276b7c695cdb61e187b`.

The bridge runs only after a successful outer timeline advance. It binds a
committed P11 input to a P18-C request at its exact target instant, selects at
most one due request per actor in retained FIFO order, and records deferrals
for other due inputs at the same boundary. A later due input can retrigger the
oldest deferred input through a P18-C trigger receipt. P11 input identity and
sequence, P18-A accepted timeline-input identity and sequence, P18-C request
identity and boundary sequence, and PersonId remain distinct.

An input that missed its exact post-advance handoff is rejected as stale; the
bridge does not admit it retroactively. Repeated handoff calls reuse P18-C
receipts. P18-C allocates decision-boundary sequence values; timeline input
sequence is retained as source identity and is not used as the decision
sequence.

## Ownership and exclusions

The bridge owns only request/input correlation and due-request selection. It
does not run a timeline callback, execute or validate an action, mutate domain
truth, add an input queue or scheduler, or claim the `SimulationRuntime`
handoff. Runtime registration and exact action admission remain in the P18-D
composition step alongside the merchant and daily owner steps.

## Validation evidence

At candidate tip `4016a73a402c33d33722c276b7c695cdb61e187b`,
`ActorChoiceTemporalDecisionBridgeTests` passed **5/5** in EditMode. The exact
result is retained at
`Temp/ValidationResults/EditMode-20260928-194427-f0e3855f06c64f928d456bc79bbbe0f0.xml`.
The suite covers exact P11-to-P18-C binding and receipt replay, one-request-per-
actor ordering and deferral, retained actor/target identity validation, and a
future input triggering an older deferred input at its due boundary.
`git diff --check` passed at this candidate tip.

This is owner-step evidence only. It does not establish complete P18-D
composition, runtime ordering/handoff, canonical promotion, or Phase 18
closure.
