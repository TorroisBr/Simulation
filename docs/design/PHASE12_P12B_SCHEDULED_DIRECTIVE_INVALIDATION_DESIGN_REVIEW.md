# P12-B ScheduledDirective invalidation design review

**Verdict: `PASS` — implementation may proceed within accepted P12-B prerequisite authorization.**

- Canonical base reviewed: `54fc23b89cb13598dd184a2b5b6bacf9dc23b0a7`.
- Exact design tip reviewed: `d9228b1ce8c63a0480e46b00648564516d383601`.
- Candidate branch: `codex/phase12/P12BScheduledDirectiveInvalidationDesign`.
- Independent exact-tip reviewers: `/root/allocator_census_design_review` and `/root/actor_choice_design_review`; both passed the final design tip `d9228b1ce8c63a0480e46b00648564516d383601` and made no candidate edits.
- The initial review of `28529ba` identified false-success propagation and batch epoch-accounting gaps. Those were corrected at `57978ab`; this exact-tip review also covers the final optional-owner clarification at `d9228b1`.

The design reuses the already composed `ScheduledDirectiveStore` and its exact
`p12f.scheduled-directives` schema-v1 census provider. The published
`UnityBootstrap-Daily-v1` composition requires the exact owner and rejects a
missing or mismatched runtime owner; standalone `SimulationRuntime`
compositions may omit the optional system and make no ScheduledDirective
coverage claim. It covers the existing store Add commit and stored-row
terminal transitions, including
`PrepareDay` duplicate/unresolved skips and actor-turn outcomes. Genesis Adds
remain part of the initial selected-profile baseline; transient `TryTake` and
queries remain outside the witness.

For selected P12, denied preflight faults the protocol and propagates before
the incoming directive is bound or the owner row changes. This is required
because existing `PrepareDay` and actor-turn callers ignore `Mark*` boolean
results; returning false alone could let the daily advance report success.
The existing selected-P12 advance catch faults and rethrows. Non-P12 behavior
retains its current boolean results. Earlier domain effects are not rolled
back if terminal-state admission fails after an action, and the design adds no
action-plus-directive transaction.

The local revision advances once per successful owner commit. Standalone
commits notify immediately; writes within existing TravelParty, Merchant, or
solo-travel batches are coalesced by changed section and accounted at the
outer operation boundary. Exact owner identity, count/revision validation,
preflight ordering, post-commit notification failure, validation coverage,
and exclusions are bounded in the design.

Implementation is authorized under the accepted P12-B prerequisite scope.
This review does not authorize canonical promotion or Phase closure and does
not claim P12-B completion, P12-A readiness, P13 readiness, complete owner or
epoch coverage, capture eligibility, export, or hydration.
