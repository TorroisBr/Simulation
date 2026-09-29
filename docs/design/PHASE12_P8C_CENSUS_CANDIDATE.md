# P12-B P8-C passive census-witness candidate

**Status:** Submitted for independent exact-tip review. This is a bounded
owner-evidence slice within accepted P12-B work package 3; it is not canonical
delivery or P12-B completion.

**Base:** P12 canonical `a43316858b7006c624ed1a950097210ae55e95f7`.

**Implementation commit:** `c4aaedf7ed3f221f5e04269c3979a85597e6c069` on
`codex/phase12/P12BP8CExactZeroWitness`.

## Delivered candidate scope

- `LegacySpatialAnchorBindingCensusProvider` reports the installed
  `LegacySpatialAnchorBindingStore` under section
  `p8c.city-site-location-bindings`, schema 1.
- `PersonSpatialPositionCensusProvider` reports the installed
  `PersonSpatialPositionStore` under section `p8c.person-positions`, schema
  1.
- Each witness uses the concrete owner reference as ephemeral identity and
  reads that owner's exact `Count` and `Revision`.
- Selected-profile bootstrap evidence constructs providers over the published
  runtime's installed stores and checks stable identity plus exact day-zero
  zero witnesses. Store tests cover successful writes, failed writes, and the
  identical City anchor rebind that succeeds without advancing revision.

These reads are unsynchronized. The adapters are not registered in
`ContinuationCensusProtocol`, do not connect committed-write notifications to
the P12-B mutation epoch, do not establish owner-thread/quiescence, do not
provide capture eligibility, and do not change P12-A readiness. They do not
modify P8 owner semantics or `SimulationRuntime`.

## Validation evidence

Validation was rerun after the implementation commit against the candidate's
unchanged executable tree. Results are retained under
`Library/ValidationResults/P12BP8C` in the candidate worktree so they remain
available for independent inspection.

| Gate | Result | Evidence |
|---|---:|---|
| `PersonSpatialPresenceTests` | 9/9 passed | `Library/ValidationResults/P12BP8C/EditMode-20260929-201724-8ef7886cd7394da9a0c42fb3f7cc744a.xml` |
| `SimulationBootstrapCompositionTests` | 14/14 passed | `Library/ValidationResults/P12BP8C/EditMode-20260929-201743-3c9e7d263102414aa316964f48193e2a.xml` |
| ALL EditMode | 1953/1953 passed | `Library/ValidationResults/P12BP8C/EditMode-20260929-201801-6c816402a8d847dab7fa10d4059d898a.xml` |
| Official complete Smoke | 5/5 passed | `Library/ValidationResults/P12BP8C/EditMode-20260929-201928-17559bab94a641ec8acab3a545ccbc59.xml` |
| `git diff --check` | passed | implementation commit `c4aaedf` and base-to-candidate diff |

The tests establish these two owner-local witnesses only. P8-B/D sections,
complete profile coverage, committed-write invalidation, owner-thread proof,
quiescence, and P12-B admission remain open.
