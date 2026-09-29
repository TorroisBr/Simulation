# P12-B P8-B passive census candidate

**Status:** Implementation and validation complete; independent implementation review pending.

**Canonical base:** `codex/phase12/canonical` at
`4d48a5f88145d748c32c8dc42ab251dcb04f81e4`.

**Reviewed design:** `codex/phase12/P12BP8BZeroWitnessDesign` at
`2bb5edbb28be7bee76f87c27fb957c0ababb08ac`. Independent technical review
passed against the stated canonical base after a docs-only source-path
correction.

**Implementation commit:** `e77d671` (`feat(p12): add passive P8-B census witnesses`).

## Delivered scope

- Added schema-v1 `p8b.passage-option-barrier-state`, identifying the installed
  `SpatialPassageAuthority` child and reporting checked `Options.Count +
  Barriers.Count` with its parent `SpatialAuthorityStore.Revision`.
- Added schema-v1 `p8b.crossings`, identifying the installed
  `SpatialAuthorityStore` and reporting `CrossingCount` with the same parent
  revision.
- Added selected-profile assertions for the exact zero baseline, owner
  identity, revision 1 after authored P8-A geography, and valid authority
  invariants.
- Added mutation tests for option, barrier and crossing registration and
  condition changes; rejected duplicate/invalid mutations preserve both
  witnesses. Crossing projections in `OptionStates` remain excluded from
  passage membership cardinality.

## Validation

All gates passed on implementation commit `e77d671`:

| Gate | Result | Retained XML |
|---|---:|---|
| `SpatialPassageAuthorityTests` | 13/13 | `Library/ValidationResults/P12BP8B/EditMode-20260929-204342-0534f171b05349a99ba090d3c6b3d6bd.xml` |
| `SimulationBootstrapCompositionTests` | 14/14 | `Library/ValidationResults/P12BP8B/EditMode-20260929-204407-9c7af7fb95884174af3eaa71ec6259f6.xml` |
| ALL EditMode | 1954/1954 | `Library/ValidationResults/P12BP8B/EditMode-20260929-204432-8e101f823d874d0fa52afd4e2960afa6.xml` |
| Official Smoke | 5/5 | `Library/ValidationResults/P12BP8B/EditMode-20260929-204514-130318e3c6e44df481cbc640f020c9a4.xml` |
| `git diff --check` | PASS | — |

## Limits

These providers are passive, unsynchronized reads. They are not registered in
the census protocol, do not wire the shared mutation epoch, and do not establish
owner-thread/quiescence or capture eligibility. P8-B owner semantics remain
unchanged. P12-B is incomplete, P12-A remains `WAIT_DEPENDENCY`, and this
candidate does not supply P12-A implementation authorization or readiness.
