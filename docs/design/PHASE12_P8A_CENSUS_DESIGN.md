# P12-B P8-A required-geography census witness design

**Status:** Submitted for independent technical review.

**Canonical base:** `codex/phase12/canonical` at
`06145c7cbc258c56cc1be1a24adaa1751d32bc01` (P8-D witnesses promoted).

**Accepted scope:** The `UnityBootstrap-Daily-v1` selected profile requires
exactly one authored P8-A Hex, one anchored Location, and one scale context.
This design adds owner-issued cardinality evidence for those existing facts;
it does not add geography or alter P8-A behavior.

## Owner sections

Use three schema-v1 sections backed by the installed runtime-clone
`SpatialAuthorityStore`:

| Section ID | Cardinality | Local revision stamp |
|---|---|---|
| `p8a.hexes` | `SpatialAuthorityStore.HexCount` | `SpatialAuthorityStore.Revision` |
| `p8a.locations` | `SpatialAuthorityStore.LocationCount` | `SpatialAuthorityStore.Revision` |
| `p8a.scale-context` | `HasGeography ? 1 : 0` | `SpatialAuthorityStore.Revision` |

All three sections identify the same installed store instance. The provider
must receive `SimulationRuntime.SpatialAuthorityStore`, exposed in the
published bootstrap as `SimulationBootstrapComposition.SpatialAuthority`; it
must not retain the pre-clone genesis source. Separate section IDs preserve
the independent required cardinalities rather than collapsing them into a
sum that could hide a missing Hex or Location.

For the selected profile, all three cardinalities are exactly one and the
post-genesis local revision is one. The existing profile test remains
responsible for exact semantic facts: Hex ID/coordinate/terrain/revision,
Location ID/anchor, and scale convention/source/version/distance/unit. These
passive census sections do not export or serialize those values; P12-C owns
their later exact export/hydration contract.

`SpatialAuthorityStore.Revision` is a conservative parent stamp, not an
P8-A-only revision. It also advances for relevant spatial child mutations
such as P8-B passage/crossing changes. A change to this stamp with stable
P8-A cardinality is valid and must be observed. Any eventual complete census
must collect and revalidate all sections sharing this parent revision; this
design does not claim concurrent-read safety or a coherent snapshot by itself.

## Implementation and tests

- Add passive `IOwnerSectionCensusProvider` adapters in
  `Assets/_Project/Scripts/P12P8ACensusProviders.cs`, with the matching Unity
  `.meta`. The adapters read only the installed store's existing properties.
- Extend `SelectedSampleSceneProfileBootstrapsItsAuthoredP8GeographyBeforeDayOne`
  to assert section/schema IDs, exact cardinalities `1/1/1`, revision `1`,
  stable owner identity across reads, and identity equal to the published
  runtime store. Keep its existing exact authored-value assertions.
- Add a focused `SpatialGeographyTests` proving an identity-only empty store
  reports cardinalities `0/0/0` at revision `0`, invalid geography composition
  leaves the witnesses unchanged, and one successful finite composition
  reports `1/1/1` at revision `1`.
- Keep section role/admission expectations in the later P12-B profile
  requirement inventory: these are required populated sections, not
  `ExplicitlyEmpty` sections.

## Exclusions and evidence boundary

Do not alter P8-A data, P8-B through P8-E behavior, `SimulationRuntime`,
runtime composition, the census protocol, shared mutation-epoch wiring,
thread/quiescence enforcement, capture eligibility, serialization, or
hydration. Do not treat the three counts as proof of geography content,
anchor integrity, owner-thread affinity, or atomicity. They provide only
installed-owner identity, exact per-kind current cardinality, and the
existing conservative local revision.

This is one bounded P12-B live-census evidence slice. P12-B remains incomplete
until all included owners, supported committed writes, and the capture boundary
are covered; P12-A remains `WAIT_DEPENDENCY` until complete export/staged
hydration and the live profile inventory are demonstrated, followed by its
separate implementation authorization.
