# CORE-F P12-D Core Site and Expedition Staging — independent review

**Verdict:** `VALIDATED_CANDIDATE` for the bounded implementation and its corrected validation record.
**Reviewed candidate:** `02961a842f80e5b36c1986eced4e9f76c7c455e3`
**Implementation commit:** `332565113c118608786ba8681c9a44ce00054bf2`
**Implementation tree:** `5deea1f1920cc8ddd6cd1fdd0ec4ab0b4d092dbe`
**Candidate base:** `84e0934ba4a223fc30f82cf5920df2a3d7010bb9`
**Core canonical at review:** `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
**Review date:** 2026-10-25

## Scope reviewed

The review covered the complete code diff from the stated base and the exact
candidate successor. The implementation moves `ExplorableSiteRuntime`,
`ExplorableSiteStore`, and `ExplorableSiteOwnerSnapshot` into
`Simulation.Core`, preserving source content and Unity `.meta` GUIDs. It moves
the Expedition restore stager into Core and updates the Unity owner-package
adapter to pass the admitted full roster IDs and P12-D/F rows.

The Core stager preserves the prior site, spatial, NPC-row, TravelParty,
member reciprocity, local-reference, and route-endpoint checks. It binds these
checks to Core identity, spatial, site, NPC, and TravelParty authorities. Full
NPC IDs remain paired to the complete F-row vector; selected Core owners may
be a subset. The stager adds no Expedition hard-zero rule. P12-D supplies the
accepted exact-empty Daily-v1 site roots; populated site-store staging is also
tested independently. The bounded change does not expand P12, P10, or the
selected bootstrap profile.

The explicit Core compile/test inputs and validation-input collector include
the moved source, metadata, and tests. Focused tests cover populated Core site
staging, populated Expedition rejection against exact-empty admitted roots, and
missing staged TravelParty rejection. No assertion weakening or second owner
implementation was found.

## Findings

The implementation review found no actionable defects. A checksum discrepancy
in the initial validation record was corrected in candidate `02961a84`: the
record now distinguishes the CRLF worktree-checkout manifest hash
`5834b3de1a00fef391e20f5128d3bd6d15fd398344585cff18501d2c78234dda` from the
committed LF Git-blob hash
`b05d1735cf34b8577ae19f3140ad6f55f862c83382afeec901b5e0235036f77d`.
The result archive hash is
`8240e45f2ede62ef2b3cadda052604040426746bdd61e30d391ee273cb6b648d` and matches
its public manifest.

## Validation applicability and limits

The candidate validation record reports:

- Official CoreValidation: 803/803 cold and 803/803 warm; run
  `11b1802c04ea4ec39b3054b61c3e2f65`.
- Focused Unity EditMode: 2/2; run
  `6405eb32a7f1432ba558e3133267a0f9`.
- Full Unity EditMode: 3264/3264; run
  `e4feb34cad1d49c89021bc057319ff81`.
- Official Smoke: 5/5; run `0665bf03bbc44b49b335c4ff5edce334`.
- `git diff --check`: pass.

The independent reviewer verified the public archive hash and manifest
privacy scan and confirmed candidate source applicability. The reviewer did
not access protected original Core TRX files or the private CoreValidation
run manifest; no public .NET TRX package is claimed. This record does not claim
independent re-execution of the suites.

## Integration limits

This validates only the bounded site/Expedition owner-staging slice. It does
not complete P12-F, shared `DailyV1Application`, detached P12 restoration, the
fixed .NET host, separate-process T/T+1/T+2 continuation, full CORE-F, or Core
canonical promotion. The candidate remains an implementation checkpoint on
its feature branch; no Phase or Core canonical status changes.
