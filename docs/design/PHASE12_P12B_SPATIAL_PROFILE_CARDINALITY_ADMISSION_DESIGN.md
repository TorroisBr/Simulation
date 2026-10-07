# P12-B — Daily-v1 spatial owner registration and initial cardinality admission

**Checkpoint:** P12-B — Profile admission and completed-boundary lifecycle.
This is a bounded continuation of the accepted P12-B capability, not a new
checkpoint ID or a readiness/closure claim.

**Status:** Proposed technical contract for independent review. No
implementation starts until review passes.

**Current base:** `codex/phase12/canonical` at
`d8e6c9919d9359003dfd370fbd38a47424256b26`; executable code tree
`c9e763e2ebc2f63d9772701a0c03e35c271392f7`, code-bearing tip
`5b055be864afa0ace56d56381eb00fe4e993ed86`. The selected Daily-v1 profile
uses the SampleScene's dedicated `Simulation-DailyV1.asset` P9-B profile.
`Simulation-GeneralTest.asset` remains the separate P10-A
Ruin/LocalTopology proving profile.

## Purpose and evidence

The accepted profile's current P12 protocol contains 268 sections. Existing
P8-A, P8-B, and P8-C census providers are read by selected-profile tests, but
their seven section IDs are absent from the sealed runtime protocol. The
currently registered Required RuntimeIdentity/network sections accept any
nonnegative cardinality at initial registration, and the P8-A providers are
not admission sections yet. The current expected cardinalities are asserted
by a profile test but are not enforced by admission. The profile must reject
a missing or wrong-sized Required owner before the inventory baseline is
sealed.

The retained exact-tree P12-E validation runs the selected profile test on
code tree `c9e763e`: composition 24/24, five added owner-provider suites 5/5,
ALL EditMode 2434/2434, official Smoke 5/5, and `git diff --check` PASS. The
test asserts the 268-section inventory and current profile split. A current
source audit confirms the existing fixed `P8-A` geography is one Hex, one
anchored Location, and one scale context; the legacy network contains two
Locations and two Routes. The eight RuntimeIdentity sections report 10 NPCs,
2 Cities, 2 Locations, 2 Routes, and four exact-zero indexes at initial
publication. P9-B specifies exactly one authored Hex and anchored Location.

## Bounded implementation contract

1. Before sealing the selected Daily-v1 census protocol, register seven
   existing schema-v1 providers against the exact cloned/runtime-installed
   owners:

   | Section | Role | Initial cardinality | Owner |
   |---|---|---:|---|
   | `p8a.hexes` | Required | 1 | installed `SpatialAuthorityStore` |
   | `p8a.locations` | Required | 1 | installed `SpatialAuthorityStore` |
   | `p8a.scale-context` | Required | 1 | installed `SpatialAuthorityStore` |
   | `p8b.passage-option-barrier-state` | ExplicitlyEmpty | 0 | installed passage child of `SpatialAuthorityStore` |
   | `p8b.crossings` | ExplicitlyEmpty | 0 | installed `SpatialAuthorityStore` |
   | `p8c.city-site-location-bindings` | ExplicitlyEmpty | 0 | installed `LegacySpatialAnchorBindingStore` |
   | `p8c.person-positions` | ExplicitlyEmpty | 0 | installed `PersonSpatialPositionStore` |

   Reuse `SpatialHexCensusProvider`, `SpatialLocationCensusProvider`,
   `SpatialScaleContextCensusProvider`, `SpatialPassageStateCensusProvider`,
   `SpatialCrossingCensusProvider`,
   `LegacySpatialAnchorBindingCensusProvider`, and
   `PersonSpatialPositionCensusProvider`. Do not add schemas, owner state,
   providers, or a second source of truth.

2. Extend the existing profile-only fixed-section admission helper with an
   optional expected initial cardinality. Enforce these current Daily-v1
   initial counts at protocol registration:

   | Existing Required section group | Expected initial cardinality |
   |---|---:|
   | P8-A Hexes / Locations / scale context | 1 / 1 / 1 |
   | RuntimeIdentity NPCs / Cities / Locations / Routes | 10 / 2 / 2 / 2 |
   | Legacy spatial-network Locations / Routes | 2 / 2 |

   Existing `ExplicitlyEmpty` P8-B/C/D and ExplorableSite sections continue
   to require exact zero. Compare each witness's owner identity with the exact
   installed runtime clone, as the current registrations already do. The
   resulting selected-profile protocol inventory is exactly 275 sections.

3. The NPC identity count of ten is an initial bootstrap condition only.
   `runtime.npc-membership` may add a materialized NPC after publication; its
   existing commit path reconciles the RuntimeIdentity owner baseline and
   shared epoch. Unregistering removes the NPC from the active roster but
   intentionally retains its typed RuntimeId identity. Preserve this
   `10 → 11 → 11` temporal behavior; do not freeze or cap the identity index.

4. Keep P8-B/C sections explicitly empty for this profile. Their providers
   reject populated state before protocol publication. Add no operation ID,
   runtime writer, mutation callback, or shared-epoch claim for P8-B/C; the
   accepted Daily-v1 path has no supported producer for these excluded P8
   states. Keep P10-A LocalTopology out of Daily-v1.

## Validation and review obligations

- Extend selected Daily-v1 composition/admission tests to assert all 275
  sections, each new section's exact ID/schema/owner/cardinality/revision, and
  stable repeated reads.
- Exercise the actual admission path with a missing/wrong Required spatial,
  identity, or legacy-network cardinality, including zero for a Required
  section; prove rejection occurs before protocol sealing/publication.
- Exercise nonzero P8-B/C sections and prove they reject under the existing
  `ExplicitlyEmpty` role.
- Retain temporal NPC identity coverage: adding an NPC advances the identity
  cardinality and shared epoch, while unregistering retains identity. The
  current exact-tree test already covers this; rerun it after code changes.
- Confirm `SampleScene` still selects Daily-v1 and GeneralTest still selects
  P10-A independently.
- Run focused affected suites, ALL EditMode, official Smoke 5/5, and
  `git diff --check` on the exact implementation tree.

## Limits

This adds seven existing spatial census sections and exact initial cardinality
admission only. It does not connect P8-B/C writes to the selected P12 epoch,
claim complete owner/cardinality or shared-epoch coverage, prove global
quiescence, issue a successful-boundary capture token, implement export or
hydration, add P10-A to Daily-v1, complete P12-B, make P12-A READY, unblock
P13, or close Phase 12.
