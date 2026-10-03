# P12-B solo travel-start design revalidation

**Purpose:** record source-backed corrections found during exact-tip
implementation review. This is an additive technical-boundary clarification;
it adds no gameplay behavior or Phase scope.

- Canonical base checked: `e76e50bfefc68b827d54ceb74e0b25293e0ad7b8`.
- Reviewed design: `91728e4aee7717c6b002691a9a9e96c4ad22ac71`,
  `PHASE12_P12B_SOLO_TRAVEL_START_OPERATION_DESIGN.md`.
- Original design review: PASS, record `812b04628bd8d3f04c491b1df4adae7e2c3692e2`.
- Triggering implementation review: `NEEDS_CHANGES` for candidate
  `ba1ded6e56d0358de37dbbb70f449c01cbd47318`, recorded in
  `PHASE12_P12B_SOLO_TRAVEL_START_OPERATION_IMPLEMENTATION_REVIEW.md`.
- Independent review of this revalidation: PASS at revalidation tip
  `4b79a5d176dd73e2814dbfdf76b73c6117ec8d23`, recorded in
  `PHASE12_P12B_SOLO_TRAVEL_START_OPERATION_DESIGN_REVALIDATION_REVIEW.md`.

## Source-City ownership refinement

The source-City census section is required only when the installed NPC is
currently present in the City projection that `NpcRuntime.StartTravel` will
remove. `StartTravel` computes `changedCities` only when
`currentCity.ContainsImportantNpc(this)` is true; a non-null `CurrentCity`
reference by itself does not mean that this operation changes the City
presence owner. The preflight must use that same membership condition before
adding and validating the City section. A non-reciprocal current-City pointer
therefore contributes no source-City section. The test injects this stale
pointer/revision condition only to verify selective preflight and does not
claim that such a pointer is a normal authored state.

This refinement does not change any travel transition. The provider still
calls the existing `TravelSystem.TryStartTravel` and `NpcRuntime.StartTravel`
methods; their current presence and partial-commit behavior remain
authoritative.

## Scheduled request-action boundary

The initial design requested a dedicated scheduled-request Travel test. The
current domain contract cannot construct that input: `ScheduledDirective`
accepts only `ScheduledDirectiveOperation.EscapePrison` and requires an
`EscapePrison` action regardless of whether its mode is `RequestAction` or
`ForceOutcome` (`ScheduledDirectives.cs`, constructor validation). Extending
that type to schedule Travel would introduce a new supported product action
and is outside this bounded checkpoint.

Keep the implementation wrapper at the shared `SimulationRuntime.TryExecuteAction`
boundary. Current `ProcessRequestedActionDirective` calls
`TryExecuteCurrentAction`, which uses the same execution boundary for any
currently supported request; `TryExecuteAction` encloses a bound Travel
provider in `runtime.travel.start`. The code review can verify this dispatch
composition, but this checkpoint must not claim or test a scheduled Travel
input that the current domain rejects. Focused coverage verifies that a
RequestAction Travel directive remains rejected, while the selected-profile
Travel provider itself is exercised through the actual runtime wrapper. The
existing ordering that applies success-status changes after action execution
remains unchanged and outside the nested scope.

## Validation adjustment

Implementation review additionally requires end-to-end coverage of the
existing charge/restore path. Set the NPC travel-state revision to its
terminal value before runtime composition so the witness baseline is current;
then the ordinary TravelSystem precheck permits the attempt, the charge
commits, `StartTravel` refuses the saturated state without a state commit,
and the existing `TryRestoreTravelCharge` compensates. The test must assert
two local account revisions, one deduplicated account-section notification,
and no invented travel or Event commit.

No architecture decision, new command authority, schedule API, rollback
policy, TravelParty behavior, or P12 readiness claim is introduced. P12-B
remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked.
