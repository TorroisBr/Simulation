# CORE-F selected TravelState and SpatialKnowledge baseline review

## Identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Reviewed implementation commit: `783994b50d0cef619d54ea5c35706fa43fb7515a`
- Reviewed implementation tree: `5b4b8060b8d0f4c8349f106ac09e2a974c86a445`
- Base commit: `206174e28c69664f011c81d42ac6b5db4dc48739`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

## Independent source-review verdict

**PASS — bounded source review.** No actionable source or test-integrity finding
was reported for this slice.

The Core execution boundary records exact selected `DailyV1NpcRuntimeOwners`
TravelState identity/cardinality/revision baselines and both paired
SpatialKnowledge section rows for the exact `SpatialKnowledgeRuntime` instance.
It captures them on both sides of the initial host full-vector validation,
checks the current Core-owned rows directly during operation admission, and
advances a local baseline only after the reported commit satisfies the owner,
revision, and cardinality delta. Paired spatial rows must share one revision
and arrive together. The initial full owner-vector validation and final full
mutation notification remain routed through the host protocol. Unselected
compatibility-roster rows are not rebound to Core and remain host-validated.

The added tests cover sequential acknowledged TravelState changes, an
acknowledged spatial discovery, unrecorded TravelState and SpatialKnowledge
drift, incomplete paired spatial receipts, and the existing fail-closed
multiple-TravelParty-completion case. Existing assertions were not weakened.

## Validation evidence

- Official CoreValidation run `448c5f897ad84574b2cb438d872607d2`:
  **1,032/1,032 cold and 1,032/1,032 warm**.
- The private run manifest's source hashes matched the three implementation
  and test files at the reviewed code tree.
- `git diff --check`: PASS.
- Unity EditMode and official Smoke: **NOT RUN**. The approved Unity evidence
  wrapper pins PowerShell Core 7.6.5; the available PowerShell is 7.6.9. The
  wrapper was not invoked with an alternate runtime, and no Unity test result
  XML was produced for this code tree.

Raw CoreValidation artifacts remain in the existing private evidence store;
this record does not publish or normalize them.

## Disposition

This is a source-review PASS only. The candidate is **not a validated
integration candidate**, is **not approved for canonical promotion**, and does
not establish full CORE-F acceptance while the required Unity validation gate
is outstanding. No canonical ref was changed.
