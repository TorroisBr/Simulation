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
| `SpatialKnowledgeCensusTests` | 3/3 | `Library/ValidationResults/P12BSpatialKnowledgeReview/EditMode-20260930-174732-9e86d9c3272144da9eb73ffba24fc8ea.xml` |
| `SimulationBootstrapCompositionTests` | 14/14 | `Library/ValidationResults/P12BSpatialKnowledgeReview/EditMode-20260930-174927-d98bac27819e4dd993503e606874e7c2.xml` |
| ALL EditMode | 2018/2018 | `Library/ValidationResults/P12BSpatialKnowledgeReview/EditMode-20260930-174800-a98b5860ee014d6aa89b22b1d3a50eaf.xml` |
| Official complete `Smoke` filter | 5/5 | `Library/ValidationResults/P12BSpatialKnowledgeReview/EditMode-20260930-174858-f67b8f991aac47f084e98086aef742fa.xml` |

`git diff --check origin/codex/phase12/canonical..de49358` passed for the code-bearing change. The exact candidate-tip documentation diff-check also passed after these retained paths were recorded. The full
EditMode and Smoke runs were completed after the final code change.
