# P18-D Actor-Choice Temporal Bridge — Execution Record

## Scope and base

This bounded P18-D consumer step connects retained P11 actor-choice inputs to
the promoted P18-C request-state API. It is based on
`codex/phase18/P18DConsumerIntegration` at `2dd57cc`, whose history contains the
current P18 canonical prerequisite tip `9e790c5`. The implementation is isolated
on `codex/phase18/P18DActorDecisionBridge`.

The bridge runs only after a successful outer timeline advance. It binds a
committed P11 input to a P18-C request at its exact target instant, selects at
most one due request per actor in retained FIFO order, and records deferrals for
other due inputs at the same boundary. A later due input can retrigger the
oldest deferred input through a P18-C trigger receipt. The originating request
sequence, accepted input identity, and source sequence remain distinct.

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

`ActorChoiceTemporalDecisionBridgeTests` passed **3/3** in EditMode. The suite
covers exact P11-to-P18-C binding and receipt replay, one-request-per-actor
ordering and deferral, and a future input becoming the deterministic trigger
for an older deferred input at its due boundary.

Result XML: `Temp/ValidationResults/EditMode-20260929-001645-91d53a945e95499f9a5db7a538cfc9f2.xml`.
`git diff --check` passed. This is owner-step evidence only; it is not evidence
of complete P18-D composition or Phase 18 closure.
