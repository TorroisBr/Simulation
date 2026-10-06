# P12-B selected-profile spatial and identity owner inventory registration

**Checkpoint:** P12-B — Profile admission and completed-boundary lifecycle.
This is a bounded registration slice within the already accepted P12-B
checkpoint, not a new checkpoint ID or a readiness/closure claim.

**Status:** proposed technical contract; awaiting independent technical
review. Implementation remains covered by the accepted P12-B capability
authorization and starts only after review passes.

**Base:** P12 canonical `0daa72addc1f186d23713adf75f6c2f83a5aff9b`;
executable code tree `c8f1689d195218351cca0b45ef431883860b3d7d` /
`078ca8a17ea095895c897638178262ca8bb932b6`. The profile correction at
`75ca59af90d54f3fb307382740ce0ba3fa4e00fa` is present in this tree. The
selected-profile live inventory was rerun on the corrected code tree; exact
results are retained in `docs/validation/P12DailyProfileSeparation/VALIDATION.md`
and `docs/validation/P12DailyProfileRevalidation/final/VALIDATION.md`.

## Purpose

The selected-profile test already reads passive witnesses for its spatial,
legacy-network, site, and identity owners, but those witnesses are not among
the 235 fixed/dynamic sections sealed by
`SimulationRuntime.InitializeNpcRosterCensusProtocol`. P12-B cannot describe
that 235-section protocol as the complete effective-profile owner inventory.
This slice registers the existing owner witnesses in that protocol and makes
profile exclusions explicit through `ExplicitlyEmpty` contracts.

The source reachability audit found no normal Daily-v1 actor, action, or
day-advance writer for these roots after genesis publication. Current
`SpatialNetworkRuntime.RegisterLocation/RegisterRoute` and identity
registration call sites are authored genesis, the separate P10 profile, or
the unrelated observer demo. P8-B/C/D mutation APIs remain available to
their own profiles; their public visibility alone does not make them
supported Daily-v1 operations. Accordingly, this slice adds no P12 operation
IDs, write callbacks, runtime freeze, or mutation-epoch claim. The exact-zero
owners are rejected by the sealed census if a later assessment finds them
populated or changed from their accepted baseline.

## Fixed owner-section set

The selected Daily-v1 protocol currently seals 235 sections. Add the following
20 schema-v1 sections, for a tested total of 255:

| Provider family | Section IDs | Daily-v1 role | Day-zero evidence |
|---|---|---|---|
| P8-A `SpatialAuthorityStore` | `p8a.hexes`, `p8a.locations`, `p8a.scale-context` | Required | 1 Hex, 1 anchored `LocationId`, 1 scale context; same installed spatial owner, revision 1. |
| P8-B child owners | `p8b.passage-option-barrier-state`, `p8b.crossings` | Explicitly empty | 0 passage/barrier rows and 0 crossings; installed owner identities retained, parent spatial revision 1. |
| P8-C child owners | `p8c.city-site-location-bindings`, `p8c.person-positions` | Explicitly empty | 0 bindings and 0 Person positions; installed owner identities retained, local revision 0. |
| P8-D child owners | `p8d.spatial-route-observations`, `p8d.person-route-plan-history` | Explicitly empty | 0 observations and 0 retained plan rows; installed owner identities retained, local revision 0. |
| Runtime-identity indexes | `p12c.runtime-identities.npcs`, `p12c.runtime-identities.cities`, `p12c.runtime-identities.locations`, `p12c.runtime-identities.routes`, `p12c.runtime-identities.explorable-sites`, `p12c.runtime-identities.local-places`, `p12c.runtime-identities.local-connections`, `p12c.runtime-identities.notable-items` | First four Required; last four ExplicitlyEmpty | Cardinalities 10/2/2/2/0/0/0/0 on the selected authored bootstrap; one registry owner and its shared local revision. |
| Legacy spatial network | `p12d.legacy-spatial-network.locations`, `p12d.legacy-spatial-network.routes` | Required | 2 runtime-ID Locations and 2 Routes at shared revision 4; one installed `SpatialNetworkRuntime` owner. |
| ExplorableSite store | Existing `ExplorableSiteCensusProvider.SectionId` | ExplicitlyEmpty | 0 sites at revision 0 on the installed store; separate from P10 topology and P8 `LocationId` identity. |

The P8-A `LocationId` and the legacy runtime-ID Locations are separate
authorities/namespaces and must not be conflated. Runtime-identity indexes
remain census observations of their existing registry; this slice does not
declare that registry an independent persisted owner or define its future
hydration. P12-C/P12-D owner export/hydration design must either reconstruct
these indexes from admitted domain owners or separately justify retained
registry state.

P8-E's transaction coordinator has no retained state or section. Its
position/plan commits change the already listed P8-C/P8-D owners and therefore
make those exact-empty sections fail their next inventory assessment. P10-A
`LocalTopologyStore` is not composed by Daily-v1; `Simulation-GeneralTest.asset`
continues to be rejected before identity or owner publication.

## Technical integration boundary

1. Bind providers to the exact instances held by the selected runtime:
   resolved `SpatialAuthorityStore`, resolved P8-C/D stores,
   `RuntimeIdentityRegistry`, `SpatialNetworkRuntime`, and
   `ExplorableSiteStore`. Avoid detached stores, asset-derived counts, or a
   second source of owner truth.
2. Supply the bootstrap-owned identity registry and legacy spatial network to
   `SimulationRuntime` before its census protocol seals. Construct P8-A/B/C/D
   providers from the runtime's resolved owner references. Preserve all
   existing constructor behavior for non-P12 runtimes.
3. Under the selected P12 admission context only, register the 20 fixed
   contracts/providers before sealing section/provider inventories. Validate
   each witness's exact section ID/schema, expected owner instance, and
   nonnegative revision. Also enforce the selected profile's exact initial
   cardinality before baseline establishment; `Required` means presence, not
   a positive or profile-correct count. Use this bounded mapping without
   changing general `OwnerSectionRole` semantics:

   | Section family | Required pre-seal cardinality |
   |---|---|
   | P8-A `p8a.hexes`, `p8a.locations`, `p8a.scale-context` | 1 / 1 / 1 |
   | Runtime identity NPC, City, Location, Route indexes | 10 / 2 / 2 / 2 |
   | Legacy spatial-network Location and Route indexes | 2 / 2 |
   | Runtime identity ExplorableSite, LocalPlace, LocalConnection, NotableItem indexes | 0 / 0 / 0 / 0 |
   | P8-B, P8-C, P8-D, and ExplorableSite sections marked `ExplicitlyEmpty` | 0 for every section |

   For P8-A, compare each provider with the resolved installed spatial owner;
   for the eight identity indexes, require the one installed registry owner;
   for legacy-network sections, require the one installed network owner; for
   child providers, require the exact installed child owner from its accepted
   authority. A missing, duplicate, misowned, or wrong-cardinality witness
   faults profile admission before the protocol seals or publishes its
   baseline. This closes the gap because protocol `Required` validation accepts
   zero cardinality; tests alone do not enforce runtime admission.
4. Keep genesis writes before the initial inventory baseline. Preserve the
   normal bootstrap publication operation and final owner-thread census check.
   Other runtime profiles retain existing P8/P9/P10 behavior.

## Validation obligations

- Extend the selected Daily-v1 composition/admission proof to assert all 255
  registered sections, exact owner-instance identity, schema, pre-seal exact
  cardinality mapping above, current owner revision, and stable repeated reads.
- Prove the selected-profile runtime rejects before publication when a
  Required identity/P8-A/network witness has a wrong cardinality, including
  zero for a Required section; do not rely only on a test assertion after
  registration.
- Prove a nonzero P8-B/C/D or ExplorableSite exact-empty section fails owner
  inventory assessment closed. Do not test or add security behavior for forged
  commands; exercise only existing domain APIs and the profile's declared
  admission invariant.
- Prove the P8-A positive rows and legacy 2/2 network rows remain present and
  that P10-A `Simulation-GeneralTest.asset` remains rejected before identity
  allocation/publication.
- Run focused affected suites, ALL EditMode, official Smoke 5/5 where
  applicable, and `git diff --check` on the exact code tree. Reuse the Unity
  project/worktree and preserve all unrelated ProjectSettings edits and
  untracked `.meta`/validation files.

## Explicit limits

This slice completes neither the owner census nor shared-write coverage. It
does not connect P8-B/C/D or identity/network writes to the P12 mutation epoch,
establish general capture eligibility, add a P12 capture token, prove global
Unity quiescence, export/hydrate any owner, or make P12-B/P12-A/P13 ready.
It does not add P10-A to Daily-v1, change the P8 one-owner-per-Location
invariant, create new gameplay, or claim that the public API surface is a
supported post-genesis Daily-v1 writer contract.
