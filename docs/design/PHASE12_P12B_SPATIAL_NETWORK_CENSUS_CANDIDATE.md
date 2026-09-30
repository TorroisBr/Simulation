# P12-B legacy SpatialNetwork passive-census candidate

**Status:** Promoted to `codex/phase12/canonical` at
`46c457fe12d2b287d55553e606fea471255d292d`. This is partial passive census
evidence only; P12-B remains incomplete and P12-A remains `WAIT_DEPENDENCY`.

**Candidate branch:** `codex/phase12/P12BSpatialNetworkCensus`.

**Canonical base:** `codex/phase12/canonical` at
`eec3fbeecffb52c633ffe0945b3dfa39743ae064`.

**Code-bearing candidate tip:**
`555594ed899f2e191640f758599058a198c53ee7` (tree
`da7edc43dcbe7c56fedf52730d5fb808ff7db6d3`).

**Exact-tip independent implementation review:** PASS. Durable review record:
`codex/phase12/P12BSpatialNetworkCensusReviewRecord` at
`dc10d7376e22af1a7027ea8bdb238f2e56dfe390`.

**Technical design:**
`PHASE12_P12B_SPATIAL_NETWORK_CENSUS_DESIGN.md`, independently reviewed at
design tip `11b4a27`.

## Delivered boundary

The candidate adds two fixed schema-v1 passive witnesses for the installed
`SpatialNetworkRuntime`:

| Section | Cardinality | Revision |
|---|---|---|
| `p12d.legacy-spatial-network.locations` | Registered network locations | Shared network revision |
| `p12d.legacy-spatial-network.routes` | Registered network routes | Shared network revision |

Both witnesses use the exact installed `SimulationBootstrapComposition.SpatialNetwork`
as owner identity. The selected authored bootstrap profile reports two
locations, two routes, and revision four. Successful location and route
registration each advance the shared network revision exactly once. Local
revision saturation is rejected before registry mutation; registry rejections
leave both registry and network census witnesses unchanged. Public collection
reads retain their existing signatures and enumeration behavior while
returning detached, read-only snapshots.

Changed files:

- `Assets/_Project/Scripts/SpatialRuntime.cs`;
- new `Assets/_Project/Scripts/SpatialNetworkCensusProvider.cs` and `.meta`;
- `Assets/_Project/Scripts/SimulationBootstrapComposition.cs`;
- new `Assets/_Project/Tests/EditMode/Editor/SpatialNetworkCensusTests.cs`
  and `.meta`;
- `Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs`.

## Validation on the code-bearing candidate tree

| Gate | Result | Evidence |
|---|---:|---|
| `SpatialNetworkCensusTests` | 7/7 | `Library/ValidationResults/P12BSpatialNetwork/EditMode-20260930-162210-2a714bab7097400091839da334e354d9.xml` |
| ALL EditMode | 2015/2015 | `Library/ValidationResults/P12BSpatialNetwork/EditMode-20260930-162238-16453693433948e786c41671326678ab.xml` |
| Official complete Smoke filter | 5/5 | `Library/ValidationResults/P12BSpatialNetwork/EditMode-20260930-162320-206b1d533ccd49db959886a7a2018055.xml` |
| `git diff --check` from canonical base | passed | `eec3fbe..555594e` |

The independent implementation review also passed `git diff --check` against
the exact code-bearing candidate tip. The ALL EditMode suite covers the
observer and diagnostics regressions required by the reviewed design. The
initial Temp-directory results were not retained across subsequent validation
runs; all three final gates were rerun into the dedicated results directory
above, where their XMLs and logs are present in the candidate worktree. These
validation artifacts are not committed as source files.

## Limits retained

These are unsynchronized passive owner reads. This candidate does not connect
`SpatialNetworkRuntime` or direct `RuntimeIdentityRegistry` writes to the
shared mutation epoch, establish owner-thread/quiescence or capture
eligibility, or add exports/hydration. It changes no P8 stores, new
geography/routes, or gameplay semantics. P12-B remains incomplete; P12-A
remains `WAIT_DEPENDENCY`; and P12-C through P12-G remain blocked as recorded
in `docs/PHASE12_STATE.md`. Canonical promotion requires explicit approval
under `docs/EXECUTION_MODEL.md`.
