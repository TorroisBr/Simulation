# P12-B Institution and Office Mutation-Epoch Design

**Status:** Proposed for independent technical review; no code implementation
has started.

**Base:** `codex/phase12/canonical` at
`0daa72addc1f186d23713adf75f6c2f83a5aff9b`.

**Authority:** This is a bounded P12-B prerequisite-capability slice within the
accepted P12-B–P12-G implementation authorization. It composes the already
promoted P12-E Institution/Office census owners with the existing P12 mutation
epoch. It adds no political behavior, new profile, checkpoint ID, export or
hydration capability, P12-A authorization, or Phase-closure claim.

## Source finding

The corrected `UnityBootstrap-Daily-v1` profile composes an installed empty
`InstitutionStore` and child `OfficeStore`. The four existing P12-E providers
are passive witnesses but are not registered in the selected profile's sealed
`ContinuationCensusProtocol` inventory. Their section IDs are:

- `p12e.institution.records`
- `p12e.office.records`
- `p12e.office.incumbencies`
- `p12e.office.tenures`

The runtime's supported mutation entry points are
`SimulationRuntime.TryRegisterInstitution`, `TryRegisterOffice`,
`TryAssignIncumbent` (both overloads), `TryVacateOffice`, and
`TryApplyInstitutionalVacancyRecognition`. Each successful call installs one
InstitutionStore or OfficeStore commit and currently advances only
`PoliticalWorldRevision`. Proposal and read methods do not commit. Runtime
construction replays historical tenure state before the P12 protocol is sealed
and establishes the initial local owner revisions as composition baselines.

`OfficeStore` owns three distinct census sections under one owner and one
revision. Office registration changes its records; assignment changes the
active incumbency and appends an open tenure; vacancy removes the active
incumbency and closes that tenure in place. Assignment and vacancy therefore
change the shared Office revision, including vacancy when tenure cardinality
stays constant. A successful Office commit must notify all three Office
sections so their shared revision baselines move together.

## Bounded contract

Register the four existing providers as `Required` sections only when the
selected Daily-v1 runtime-admission context is present. Build them over the
exact installed runtime stores, not constructor inputs or public snapshots.
Before sealing the inventory, require schema version 1, exact section IDs,
nonnegative revisions, exact owner identity, and day-zero cardinality zero for
all four sections. The three Office witnesses must share the same owner
identity and revision. The selected protocol count therefore changes from 235
to 239. This is a partial selected-profile census and does not establish that
all authoritative owners are registered.

Register one synchronous operation contract for these single-owner commits:
`p12.institution-office.owner-commit`. Before each supported mutation, the
runtime must verify its bound owner thread, validate the exact affected
section baselines, validate shared mutation-epoch capacity, and enter that
registered operation. A failed admission faults the P12 admission path and
returns the existing domain `RuntimeFaulted` failure without calling the owner
mutator. Non-P12 runtimes keep their current behavior.

After an unsuccessful domain operation, dispose the operation scope without
notifying a changed section or advancing the shared epoch. After a successful
owner commit, preserve the existing `PoliticalWorldRevision` update and notify
the exact committed owner sections once. If post-commit notification fails,
preserve the successful domain result and fault-close P12 admission; do not
report a committed domain write as rejected. Dispose the scope on the bound
owner thread on every return/exception path.

| Successful public commit | Sections notified in one epoch | Owner revision |
|---|---|---|
| `TryRegisterInstitution` | Institution records | InstitutionStore revision advances once |
| `TryRegisterOffice` | Office records, incumbencies, and tenures | OfficeStore revision advances once |
| `TryAssignIncumbent` | Office records, incumbencies, and tenures | OfficeStore revision advances once |
| `TryVacateOffice` | Office records, incumbencies, and tenures | OfficeStore revision advances once |
| `TryApplyInstitutionalVacancyRecognition` | Office records, incumbencies, and tenures | OfficeStore revision advances once |

The affected Office cardinalities remain independently witnessed and dynamic:
office count changes only on registration; active incumbencies change on
assignment/vacancy; retained tenure count increases on assignment and remains
constant on vacancy. No future cardinality is hard-coded. The operation uses
the existing current-day and vacancy-recognition semantics unchanged.

## Integration and validation

Implementation owns `SimulationRuntime` and the Institution/Office mutation
tests; no other candidate may edit the `SimulationRuntime` admission or
institutional mutation hotspot during this integration window. Use the
existing `InstitutionOfficeCensusProvider` factory inside the runtime after
its installed stores exist and before the protocol inventories are sealed.
Leave `SimulationBootstrapComposition`'s existing passive provider surface
unchanged unless exact source review proves a composition change is required.

Required tests cover: selected Daily-v1 four-section admission at exact zero
and total 239 sections; exact installed owner identity; one epoch increment for
each successful public commit; all three Office baselines refreshed together;
same-count vacancy invalidation; rejected domain operations leaving owner
counts, revisions, and epoch unchanged; stale vacancy-recognition rejection;
scope count returning to zero; and non-P12 runtime behavior remaining
unchanged. Re-run the exact Daily-v1 admission/profile inventory, affected
institution/succession and runtime-boundary suites, ALL EditMode, official
Smoke, and `git diff --check` on the final code tree. Retain result hashes.

After implementation, obtain independent exact-tip code review and record it
durably. Any code-tree change after validation or review requires the affected
validation and fresh exact-tip review. Promote only after refreshed canonical
preflight and the standing autonomous bounded-promotion conditions pass.

## Limits

This slice covers only the listed Institution/Office commits and their exact
census sections. It does not cover Property/Estate, claims, factions, support,
political Knowledge/decisions, force/manpower/position, conflict/war/battle,
other unregistered owners, all P12 shared-epoch writers, global owner-thread
quiescence, capture eligibility, exports, hydration, P12-B completion, P12-A
readiness, P13 readiness, or Phase 12 closure. Expedition remains deferred to
P12-F until P12-C/D/E prerequisites are met.
