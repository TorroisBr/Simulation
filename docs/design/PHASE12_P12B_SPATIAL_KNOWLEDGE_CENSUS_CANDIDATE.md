# P12-B SpatialKnowledge census implementation candidate

**Status:** Submitted for independent implementation review.

- **Canonical base:** `81ddfe4bd0620b1b61a0a52aa074ef2ed57c2833`
- **Feature branch:** `codex/phase12/P12BSpatialKnowledgeCensus`
- **Code tip:** `de49358cd8b0baa5df2c12206d3e405dbf31261a`
- **Design review:** exact-tip PASS at
  `codex/phase12/P12BRegistryCommitBoundaryDesign` commit
  `8962a566103f90874892ed5ea8f862bca26b1cd3`, recorded in
  `PHASE12_P12B_SPATIAL_KNOWLEDGE_CENSUS_REVIEW.md`.

## Delivered boundary

The candidate adds two passive schema-v1 census sections per installed NPC:
known spatial locations and known spatial routes. Providers are ordered by
ordinal `RuntimeId`, bind to that NPC's exact `SpatialKnowledgeRuntime`, and
report both cardinalities against the owner's shared revision. The selected
bootstrap test now proves all 20 per-owner sections and exact 2/1 counts at
revision 3 for each of the ten NPCs.

`SpatialKnowledgeRuntime` now exposes live read-only views instead of its
backing lists, and rejects a new discovery before mutation when the shared
revision is saturated. Duplicate and blank discoveries remain no-ops.

This candidate does not wire the protocol or epoch, establish thread or
quiescence evidence, issue capture eligibility, or add P12-F export/hydration.
P12-B remains incomplete and P12-A remains `WAIT_DEPENDENCY`.

## Validation

All results use the required Unity Editor `6000.3.9f1` and the repository
validation harness. Each XML reported a passed result, zero failures, and
coherent counts.

| Suite | Result | XML |
|---|---:|---|
| `SpatialKnowledgeCensusTests` | 3/3 | `Temp/ValidationResults/EditMode-20260930-173946-dc2b685abb734bf6a2909624d8a227b1.xml` |
| `SimulationBootstrapCompositionTests` | 14/14 | `Temp/ValidationResults/EditMode-20260930-174002-d3a7ad52589b40bdbe04ee152d8befcf.xml` |
| ALL EditMode | 2018/2018 | `Temp/ValidationResults/EditMode-20260930-173827-720ee38e1e754528936781466cfb7b79.xml` |
| Official complete `Smoke` filter | 5/5 | `Temp/ValidationResults/EditMode-20260930-173910-e1da9b52a9f748af9cec389a7f510993.xml` |

`git diff --check origin/codex/phase12/canonical..de49358` passed. The full
EditMode and Smoke runs were completed after the final code change.
