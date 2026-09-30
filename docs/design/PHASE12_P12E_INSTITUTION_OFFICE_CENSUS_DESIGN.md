# P12-E Institution and Office Census Design

**Status:** Design review passed; bounded P12-B live-owner/cardinality
implementation may proceed under accepted capability authorization.

**Base:** property census candidate
`codex/phase12/P12EPropertyCensus` at
`1dc9ea2` (Property implementation code tip `b3cdeae`).

**Authority:** accepted P12-B–P12-G capability work covers prerequisite
owner-census evidence. This proposal adds no checkpoint ID, political
behavior, export/hydration, or P12-A implementation authorization.

## Selected-profile sections and exact owners

The selected `UnityBootstrap-Daily-v1` profile composes empty Institution and
Office stores. Empty means the owners exist and have zero records; it is not
an absent or excluded section. Publish four required schema-v1 sections:

| Section | Exact installed owner | Cardinality | Revision |
|---|---|---|---|
| `p12e.institution.records` | `Runtime.InstitutionStoreForWorldBoundary` | institution record count | `InstitutionStore.Revision` |
| `p12e.office.records` | `Runtime.OfficeStoreForWorldBoundary` | office record count | `OfficeStore.Revision` |
| `p12e.office.incumbencies` | `Runtime.OfficeStoreForWorldBoundary` | active incumbency count | `OfficeStore.Revision` |
| `p12e.office.tenures` | `Runtime.OfficeStoreForWorldBoundary` | all retained tenure records, open and closed | `OfficeStore.Revision` |

Office rows, active incumbencies, and tenure rows are separate cardinalities
from one authority and therefore share its exact owner identity and revision.
Tenure cardinality includes open records. Assigning an incumbent appends one
open tenure; vacating replaces that record with a closed tenure, so vacancy
changes authoritative content even though tenure cardinality remains
constant. A local revision is required to witness that change.

Providers must be constructed from the runtime-installed stores after normal
composition using the existing in-assembly
`Runtime.InstitutionStoreForWorldBoundary` and
`Runtime.OfficeStoreForWorldBoundary` accessors. The installed office owner
must remain the child of the installed institution owner; source constructor
inputs and public snapshot copies are not the witness owner.

## Bounded revision capability

Neither owner has a local revision today. Add read-only count/revision
accessors and owner-local monotonic revisions only for the existing store
commit paths:

| Owner operation | Cardinality effect | Revision effect |
|---|---|---|
| `InstitutionStore.TryRegister` success | institutions +1 | +1 |
| `OfficeStore.TryRegister` success | offices +1 | +1 |
| `OfficeStore.TryAssignIncumbent` success | incumbencies +1; tenures +1 | +1 once for the logical store commit |
| `OfficeStore.TryVacateOffice` success | incumbencies −1; tenure row is closed in place | +1 once for the logical store commit |
| `OfficeStore.TryAddHistoricalTenure` success during runtime-clone construction | tenures +1 | +1 on the newly constructed owner |

All normal returned rejected/faulted operations leave cardinalities and
revisions unchanged. This does not claim process-level exception rollback.
Check revision capacity before the first write in each mutation so overflow
cannot leave partial owner state; add the minimal `RevisionOverflow` failure
code to the existing institution foundation failure contract. One successful
store commit advances its own revision once even when multiple owned facts
change. If a higher-level operation commits more than once to the same owner,
each committed store operation advances the local revision; the later shared
P12-B epoch must still notify once only after the enclosing operation's full
commit. When that epoch is wired, every OfficeStore commit must update all
three section baselines sharing its revision, even if only one cardinality
changes. Keep `PoliticalWorldRevision` separate.

Runtime construction rebuilds owners by replaying institution and office
registrations, active incumbencies, and closed tenures through existing store
operations. Revisions therefore establish a valid local post-composition
baseline and advance for later live commits; they are not persistence fields,
and the cloned revision is not required to equal the source input revision.
Do not edit runtime transaction/clone semantics beyond what these accessors
and existing replay paths require.

## Required evidence

- Selected authored profile exposes all four required sections at exact
  zero with stable identity. The Office three-section family shares one
  identity and revision; the institution section uses its distinct owner.
- Through normal `SimulationRuntime` operations, register one institution
  and office, assign a registered Person, then vacate or recognize vacancy.
  Assert cardinalities and a single local revision increment per successful
  store operation. Specifically, successful vacancy increments revision
  while tenure count stays constant.
- Rejected duplicate/missing-parent registrations, unregistered-person or
  invalid/occupied assignments, and already-vacant/invalid/stale recognition
  leave owner counts and revision unchanged.
- Compose a runtime from populated source stores containing an active
  incumbency and a closed tenure. Assert the census witnesses the installed
  runtime clones, their separate cardinalities and consistent records, and
  that source stores remain unchanged. Validate clone-local revision as a
  post-composition stamp; do not require equality with source revision.
- Add overflow-safe preflight coverage at owner mutators if practical without
  reflection or exposing a test-only revision setter; do not add a public
  revision injection API.

## Deferred and dependency boundaries

This design supplies passive owner witnesses and the minimal existing-owner
change stamp only. It does not supply shared mutation-epoch wiring,
owner-thread binding, general operation quiescence, capture eligibility,
institution/office export or staged hydration, political Claim/Recognition
census, or P12-A readiness. P12-B remains incomplete and P12-A remains
`WAIT_DEPENDENCY`. Claim/Recognition census follows the Institution, Office,
Person, and Property owner evidence in actual constructor dependency order.
