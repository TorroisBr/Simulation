# P12 Daily-v1 profile separation — exact-tip review

**Verdict:** `VALIDATED_CANDIDATE`

**Candidate branch:** `codex/phase12/P12BDailyProfileSeparation`

**Code commit:** `75ca59af90d54f3fb307382740ce0ba3fa4e00fa`

**Code tree:** `e3fbbd53689a4dd582e086e8c1677935d21ef78f`

**Exact base / current P12 canonical at review:** `b9fcb54840ae7c4e68d5f4d1812e5591bb36a948`
**Reviewer:** `/root/p12_matrix_review` (independent; did not edit the candidate)

## Scope reviewed

The user's accepted profile decision keeps `UnityBootstrap-Daily-v1` on the
P9-B-only authored-geography continuation profile, keeps the P10-A
Ruin/LocalTopology proving profile intact as `Simulation-GeneralTest.asset`,
and allows future deliberately composed continuation profiles to include
Ruins after their owners and continuation requirements are admitted.

The complete code diff from the stated base was reviewed. SampleScene selects
the new `Simulation-DailyV1.asset`. Daily-v1 rejects both P10-A authored Ruin
and P10-B generated Ruin during profile resolution, before WorldId or runtime
identity allocation, owner construction, or publication. The selected-profile
test sets Daily-v1 admission and checks owner identity, schema, cardinality,
and local revision for City Market, SettlementPopulation, PersonStore,
RuntimeIdentity, SpatialNetwork, NPC owner families, and the other fixed
passive witnesses. P10-A remains independently covered by its existing
profile tests.

## Validation checked

The reviewer verified the archive SHA-256 against
`VALIDATION.md` and inspected the bundled XML results:

- selected Daily-v1 owner/cardinality inventory: 1/1;
- ALL EditMode: 2384/2384;
- official Smoke: 5/5;
- zero failed or skipped tests;
- `git diff --check` clean.

The exact result hashes are recorded in `VALIDATION.md`, and the result files
are bundled in `P12DailyProfileSeparation-validation-20261006-final-exact.zip`.
No executable changes were made after these tests or this review.

## Limitations

This candidate only corrects the selected profile and refreshes day-zero
owner/cardinality evidence. It does not complete P12-B, make P12-A ready, or
unblock P13. It does not establish complete evolved-world owner coverage,
complete shared-epoch coverage, global quiescence, capture eligibility,
export, or hydration. The bounded P10-A scope remains separately available;
the correction does not rule out a future explicitly composed continuation
profile containing Ruin/LocalTopology state.
