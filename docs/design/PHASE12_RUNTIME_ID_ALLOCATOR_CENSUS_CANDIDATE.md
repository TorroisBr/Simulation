# P12-C RuntimeIdAllocator passive census candidate

**Status:** Promoted at cumulative canonical tip `b889b47`. The integrated
code tree `65ebc7f` passed ALL EditMode `1985/1985` and official Smoke `5/5`;
exact integration review passed at `35ec988`.

**Canonical base:** `codex/phase12/canonical` at
`69f456d5e3c6d6f7e4b85b36e98968ced0549bf3`.

**Reviewed design:** `codex/phase12/P12CRuntimeIdAllocatorCensusDesign` at
`f1cc78632f2de3f33a62568171057111e8bb1751`; exact-tip independent design
review PASS is recorded by
`codex/phase12/P12CRuntimeIdAllocatorCensusDesignReview` at `e0a4970`.
The accepted P12-B–P12-G capability scope authorizes this bounded passive
evidence slice without a new checkpoint-acceptance gate.

## Delivered boundary

The exact selected-profile `RuntimeIdAllocator` now provides fourteen fixed
schema-v1 sections through `SimulationBootstrapComposition`. Each section
reports cardinality 1 for its typed counter, revision equal to that counter's
`next*Sequence - 1`, and the same stable opaque token from that allocator
instance. The composition receives the existing allocator used by startup
systems and recorders. It does not create a second allocator or expose an
allocator mutation handle.

A successful allocation advances only its typed revision. If later registry
installation fails, the allocated ID stays consumed and the revision remains
advanced. Allocation at sequence exhaustion throws before mutation, leaving
that counter revision unchanged.

On the selected authored bootstrap profile, day-zero revisions are NPC 10,
City 2, legacy Location 2, Route 2, and zero for the other ten families. The
P9-B P8-A authored Location source remains separate from this legacy runtime
ID namespace.

## Validation

| Gate | Result | Evidence |
|---|---:|---|
| `RuntimeIdAllocatorCensusTests` | 3/3 | `Temp/ValidationResults/EditMode-20260929-231923-3613ea3268d14af0a4994d67599902f9.xml` |
| `SimulationBootstrapCompositionTests` | 14/14 | `Temp/ValidationResults/EditMode-20260929-231944-d4108af99ec340978353ffcf0fb2a6e0.xml` |
| `CoreRuntimeTests` | 11/11 | `Temp/ValidationResults/EditMode-20260929-232000-67a436bad78f454896f4db76b956e5e8.xml` |
| ALL EditMode | 1966/1966 | `Temp/ValidationResults/EditMode-20260929-232017-3fb2df191dbf44008e99d4233f823ef3.xml` |
| Official complete Smoke | 5/5 | `Temp/ValidationResults/EditMode-20260929-232052-1b2b76129c614c38b1b40578f93f6288.xml` |
| `git diff --check` | PASS | Candidate tree before review/commit |

## Limits retained

This adds passive live counter evidence only. It adds no shared mutation-epoch
wiring, owner-thread or quiescence enforcement, atomic census collection,
capture eligibility, snapshot/export, staged hydration, or restore behavior.
The other C/D/E/F owner witnesses and committed-write coverage remain
incomplete. P12-B remains incomplete and P12-A remains `WAIT_DEPENDENCY`.
