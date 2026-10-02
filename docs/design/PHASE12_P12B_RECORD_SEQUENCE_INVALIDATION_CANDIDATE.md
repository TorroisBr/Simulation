# P12-B SimulationRecordSequence invalidation candidate

**Status:** Submitted for independent exact-tip implementation review. This
candidate promotes no canonical code and does not complete P12-B.

## Candidate identity

- Canonical base: `1ac675cc558aa919a749167647c10506c11303fc`
- Code candidate: `1e9f11966c88fd207e28264bdecb77b7f2a2f1a6`
- Code tree: `ca4ea6db1cf6797c9908bbb95c0bc5023c719709`
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
epoch advancement, wrong-thread and stale-baseline rejection, exhaustion,
post-commit notification failure with the sequence retained, and rejection of
a sequence that differs from the decision recorder.

## Validation on exact code tree

All XML files reported `result="Passed"`, zero failures, zero skipped, and
zero inconclusive tests. Their exact paths and SHA-256 values are:

| Gate | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| Focused `SimulationRecordSequenceP12InvalidationTests` EditMode | 5/5 | `B9A4C867957AE011DBE944766671C58D3C44346C3FCFAAD78359B5328CA8A287` | `C0D4B61B7C3B42E33991EF88C5646853C1A2545EF2F02BA0A14D61036FD8427C` |
| ALL EditMode | 2179/2179 | `B5A523FB5510A45CEF653006527947E6FD69D56BE4EA7133E93C4175FD9BAAFE` | `76F69A1A5A1053ABD6EEBA81CA531E282E08F18B2F5876ACC26FC902375DEDE6` |
| Official Smoke (`EditMode -TestFilter Smoke`) | 5/5 | `07386D7878862169252F99713BA69D8290E98F6A869DD73C8F2625DD71A59B39` | `FEB6EADCF9C9BC9C228C8DF09A27318D2887B97E4C4736B490F0556FB05078D5` |

Artifacts are under
`Library/ValidationResults/P12BRecordSequenceInvalidation/` in the validation
worktree, named respectively:

- `EditMode-20261002-213259-2353e5076cae4cfa99fa29d37d766a0c.xml` and `.log`
- `EditMode-20261002-213317-aaf7d5801f584a35b1273f4dcfce7312.xml` and `.log`
- `EditMode-20261002-213356-60153f95fd2c4384bd0de8c0300484b1.xml` and `.log`

`git diff 1ac675cc558aa919a749167647c10506c11303fc 1e9f11966c88fd207e28264bdecb77b7f2a2f1a6 --check` passed.

## Limits retained

Only the record-sequence owner mutation edge is invalidated. Event, Decision,
and occurrence-receipt owner sections remain outside this slice; the sequence
adapter does not imply the preceding domain mutation was covered. This does
not establish complete owner/operation or shared-epoch coverage, global
quiescence, capture eligibility, export, hydration, P12-A readiness, P12-B
completion, P13 readiness, or Phase 12 closure. P12-B remains incomplete;
P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked.
