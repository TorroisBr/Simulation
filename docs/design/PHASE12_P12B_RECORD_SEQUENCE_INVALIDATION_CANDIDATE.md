# P12-B SimulationRecordSequence invalidation candidate

**Status:** Updated after the prior exact-tip review returned NEEDS_CHANGES;
the nested-operation coverage finding is addressed and this exact tip is ready
for independent re-review. This candidate promotes no canonical code and does
not complete P12-B.

## Candidate identity

- Canonical base: `1ac675cc558aa919a749167647c10506c11303fc`
- Code candidate: `cd7ca4498d2c1d3591c227bd9011429f9bd06d8f`
- Code tree: `5282d65fbc4311bb6b907770a4e6fa363ad633df`
- Candidate evidence commit: `2394c9ada04cec1588550e4b12f757f1efb630a6`
- Design: `a9c1214bff81907f6f0b34b329e2d391401f4a7a`
- Independent design review: `codex/phase12/P12BRecordSequenceInvalidationDesignReview`
  at `6c46dfc` (PASS).
- Validation worktree: `E:/GitHub/GeneralSimulation/MainSimulation/.worktrees/p12-record-sequence-validation`

## Bounded delivery

The selected P12 daily runtime now registers the existing cardinality-one
`p12c.simulation-record-sequence` census section for the exact bootstrap
sequence. It binds owner-thread and unchanged-section preflight before each
allocation, then advances the partial shared epoch exactly once after a
successful sequence increment. Notification failure faults the P12 protocol
and propagates after the sequence increment, preserving that allocation while
preventing the event/decision append that followed it. Bootstrap verifies its
passive sequence witness has the same opaque owner identity and current
revision as the runtime registration.

Non-P12 sequences retain their prior behavior. The selected runtime reuses
the decision recorder's exact sequence when no explicit sequence is supplied
for direct/test compositions; the production bootstrap passes its shared
sequence explicitly. A supplied sequence that differs from the selected
decision recorder is rejected before runtime owner binding.

Tests cover ordinary decision recording, domain-event recording,
first-occurrence decision recording, idempotent receipt replay, exact one-step
epoch advancement, one allocation under nested registered bootstrap
publication scopes (one owner revision and one shared-epoch increment),
wrong-thread and stale-baseline rejection, exhaustion, post-commit
notification failure with the sequence retained, and rejection of a sequence
that differs from the decision recorder.

## Validation on exact code tree

All XML files reported `result="Passed"`, zero failures, zero skipped, and
zero inconclusive tests. Their exact paths and SHA-256 values are:

| Gate | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| Focused `SimulationRecordSequenceP12InvalidationTests` EditMode | 6/6 | `551DF9AC56675CCC03983AFE0D3477AF690A95A07078B39CD3D4A8EA611AF8BA` | `ED66DD6E92F410E1246C96585078CEDD9DAF08B4B9B010FD124099B86B29B529` |
| ALL EditMode | 2180/2180 | `18FCA8A8E1628F425596AEFAC8CA36C955789E71057021D02C6CDE5EDBA23B53` | `DE9947AEE3829A044875DC0552A35B378C183C633D8B8E3AD251EA51EC2A776C` |
| Official Smoke (`EditMode -TestFilter Smoke`) | 5/5 | `2D71B0C94154D75A6DCCCEB16343B03D24789EEEDEE043BEC02F368560629DE2` | `6E30DFB03E820EE7E7939C0B02E75784B9A247182BD1FF8C66465E9259F175BC` |

Artifacts are under
`Library/ValidationResults/P12BRecordSequenceInvalidationFix/` in the validation
worktree, named respectively:

- `EditMode-20261002-214926-8c2a6bb8a31e41bc87fea1e68a7bbed3.xml` and `.log`
- `EditMode-20261002-214951-881ac17e779d4565a48c7960c8b1c76e.xml` and `.log`
- `EditMode-20261002-215118-5bd4180cbe4744e097deee9312e72eff.xml` and `.log`

The prior exact-tip implementation review of code `1e9f119` returned
NEEDS_CHANGES solely for missing evidence that nested surrounding P12
operation scopes do not duplicate a sequence allocation notification. The new
focused test enters the registered bootstrap-publication operation twice,
allocates once, then asserts owner revision 1, shared epoch 1 after scope
closure, and a healthy final census. No production code changed in response.

`git diff 1ac675cc558aa919a749167647c10506c11303fc cd7ca4498d2c1d3591c227bd9011429f9bd06d8f --check` passed.

## Limits retained

Only the record-sequence owner mutation edge is invalidated. Event, Decision,
and occurrence-receipt owner sections remain outside this slice; the sequence
adapter does not imply the preceding domain mutation was covered. This does
not establish complete owner/operation or shared-epoch coverage, global
quiescence, capture eligibility, export, hydration, P12-A readiness, P12-B
completion, P13 readiness, or Phase 12 closure. P12-B remains incomplete;
P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked.
