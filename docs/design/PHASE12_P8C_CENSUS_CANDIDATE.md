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

Validation ran against the implementation tree before its documentation-only
candidate record was added; the executable source and tests are unchanged at
the candidate tip.

| Gate | Result | Evidence |
|---|---:|---|
| `PersonSpatialPresenceTests` | 9/9 passed | `Temp/ValidationResults/EditMode-20260929-200819-cc84c5b90fe84089a9f81a631ba574b4.xml` |
| `SimulationBootstrapCompositionTests` | 14/14 passed | `Temp/ValidationResults/EditMode-20260929-200845-d9633daa3db140baa6fd85ca5fff25d2.xml` |
| ALL EditMode | 1953/1953 passed | `Temp/ValidationResults/EditMode-20260929-200923-5d8b2a91d61043ad9db6d53e6dcec1f1.xml` |
| Official complete Smoke | 5/5 passed | `Temp/ValidationResults/EditMode-20260929-201008-ad5f682821c54a02a49ce85cf9873717.xml` |
| `git diff --check` | passed | implementation commit `c4aaedf` |

The tests establish these two owner-local witnesses only. P8-B/D sections,
complete profile coverage, committed-write invalidation, owner-thread proof,
quiescence, and P12-B admission remain open.
