# Independent P12-B SimulationRecordSequence census design review

**Result:** PASS.

**Reviewed design:** `codex/phase12/P12BRecordSequenceWitnessDesign` at exact
tip `c283ca6`.

**Canonical base:** `codex/phase12/canonical` at
`1ada62b031e738e2bdd5d3d623e028a114961d6e`.

## Review findings

The design is bounded and compatible with the accepted P12-B live-owner
census capability. Source confirms the normal genesis creates one
`SimulationRecordSequence` and passes that same instance to both
`DomainEventRecorder` and `NpcDecisionRecorder`. `Allocate()` is the only
supported mutation of `nextSequence`; `Revision = nextSequence - 1` reflects
each successful allocation and leaves exhaustion at
`long.MaxValue - 1` without a cursor change. Cardinality 1 correctly denotes
the single cursor, not the persisted event or decision count.

The review found that exposing the sequence object as `OwnerInstanceIdentity`
would leak its public mutating `Allocate()` API. The design now requires a
stable opaque identity token owned by the exact sequence instance and only an
internal accessor for the provider. The public witness returns the token, not
the mutable sequence. The design also clarifies that the existing core test
calls `Allocate()` directly; the required new test records a decision and an
event through their real recorders using one shared test sequence.

No new product or canonical architecture decision is required. The reviewer
confirmed this bounded passive witness may proceed under prior P12-B
capability acceptance. It does not authorize full P12-B admission or
eligibility, and it does not make P12-B complete or P12-A ready. The design
review was read-only; no tests were run.
