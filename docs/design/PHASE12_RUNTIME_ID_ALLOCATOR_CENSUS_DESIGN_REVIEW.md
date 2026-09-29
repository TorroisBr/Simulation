# P12-C RuntimeIdAllocator census design review

**Result:** Independent exact-tip design review PASS; no blocking findings.

**Design:** `codex/phase12/P12CRuntimeIdAllocatorCensusDesign` at
`f1cc78632f2de3f33a62568171057111e8bb1751`.

**Actual canonical base:** `codex/phase12/canonical` at
`69f456d5e3c6d6f7e4b85b36e98968ced0549bf3`.

The reviewer confirmed the fourteen section IDs map one-to-one to the
allocator's independent `next*Sequence` fields. Cardinality 1 denotes the
fixed scalar counter; revision `next - 1` preserves per-kind consumed-ID gaps,
including allocations followed by failed registration. Exhaustion throws
before mutation. The selected authored bootstrap's day-zero revisions
10/2/2/2 for NPC/City/legacy Location/legacy Route and zero for the remaining
ten counters agree with the blocker inventory, including no startup Event or
Decision allocation.

The recommended implementation threads the existing selected-bootstrap
allocator instance into `SimulationBootstrapComposition`, binds all
providers to that owner, and gives the witness one stable opaque identity
token. Reusing `IOwnerSectionCensusProvider` and
`OwnerSectionCensusWitness` is appropriate; no registration hook or allocator
mutation API is introduced. The P9-B authored P8-A Location identity remains
distinct from the legacy allocator Location namespace.

The accepted P12-B–G capability authorization is sufficient for this
bounded passive evidence slice. The review found no new checkpoint-acceptance
gate. It does not authorize broader P12-C export/hydration or P12-A
implementation. P12-B remains incomplete and P12-A remains
`WAIT_DEPENDENCY`; canonical promotion remains a separate human gate.
