# Phase 8 — IN PROGRESS

## Canonical baseline and current candidate

- The verified Phase 7 canonical baseline entering Phase 8 is
  `1f4651e99db2c357dd3be3c6b9284d104379f706` on `codex/phase7/canonical`.
- P8-A was promoted from integration commit
  `094971b` on `codex/phase8/P8AGeographyIntegration` to the new
  `codex/phase8/canonical` branch. The canonical branch is pushed and its
  remote SHA was verified; the Phase 7 canonical baseline remains preserved.
- Phase 8 remains open. P8-A is canonical; P8-B through P8-E are not
  implemented or promoted.

## P8-A — Factual Geography

**Status: CANONICAL, IMPLEMENTED, INDEPENDENTLY REVIEWED, AND INTEGRATION-VALIDATED.**

- Approved technical design: final design commit
  `ec4e796ddc955b72d7623e7647924aa3f76e3474` on
  `codex/phase8/P8ATechnicalDesign`.
- Implementation candidate: `22e7a27cd5aac7a0fe73046f135023f004836113` on
  `codex/phase8/P8AFactualGeography`, based on the Phase 7 canonical baseline
  and the reviewed P8-A design.
- Promoted integration record: `094971b` on
  `codex/phase8/P8AGeographyIntegration`; canonical branch:
  `codex/phase8/canonical`.
- Independent implementation review: **PASS**; no blocking correctness or
  architecture issues found.
- Integration candidate branch: `codex/phase8/P8AGeographyIntegration`.

P8-A adds finite, manually authored factual geography to the existing spatial
authority: stable Hex identity and axial integer coordinates, deterministic
neighbors over registered Hexes only, one world-local scale record with
provenance, anchored Locations, and applied terrain references consisting of
both `TerrainDefinitionId` and `AuthoredRevisionToken`. The terrain revision
token is retained in cloning and all diagnostic/reconstruction projections.
Legacy Phase 7 identity-only spatial stores remain valid without geographic
coordinates or scale. P8-A adds no passage, travel, City/Site migration, or
daily-loop behavior.

Known boundary: this repository has no terrain catalog or compatibility
resolver. P8-A preserves and structurally validates the stable terrain ID and
authored revision pair; catalog membership and runtime compatibility
resolution remain outside this checkpoint. The explicit scale value in the
current authored test fixture is not a production-world default.

## Validation record

The feature branch's focused P8-A and Phase 7 spatial/diagnostic validation was
reported by its implementation worker as **137/137 passed**, zero failures and
zero skips. The independent integration gates below were run at candidate code
SHA `22e7a27cd5aac7a0fe73046f135023f004836113` using the repository harness.

| Gate | Invocation | Result | Retained XML and log |
|---|---|---:|---|
| ALL EditMode | `pwsh -NoProfile -File .\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -All -ResultsDirectory .\Library\ValidationResults\P8A` | 1653/1653 passed; 0 failed, 0 skipped | `Library/ValidationResults/P8A/EditMode-20260924-162016-2a291f130066421cb61d959b30b812ac.xml` and `.log` |
| Official complete Smoke | `pwsh -NoProfile -File .\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter Smoke -ResultsDirectory .\Library\ValidationResults\P8A` | 5/5 passed; 0 failed, 0 skipped | `Library/ValidationResults/P8A/EditMode-20260924-162140-8ef718319d3748149639d0dbfdba1112.xml` and `.log` |

Both retained XML reports are parseable harness results with `result="Passed"`
and coherent counts. `git diff --check` passed for the candidate implementation
diff and the integration state/brief/roadmap changes. A long-run suite was not
run because P8-A does not change the daily loop or long-horizon behavior.

## Remaining Phase 8 dependency state

- **P8-B — Factual Passages:** ready for isolated implementation from the
  current `codex/phase8/canonical` HEAD under the accepted P8-B/C shared
  segment contract.
- **P8-C — Legacy Anchors and Civil Presence:** design/discovery may proceed;
  implementation waits for P8-B to publish and independently review the stable
  typed boundary/option API seam. City/Site bridge, PersonId position, and
  transit progress remain unimplemented.
- **P8-D — Knowledge and Route Plan:** waits for promoted P8-B and P8-C
  contracts/capabilities.
- **P8-E — Civil Travel Vertical Slice:** waits for the relevant promoted
  P8-B/C/D capabilities and integration validation.

The accepted P8-B/C shared segment contract is a separate design candidate at
`5faa5817a11b0ae7412ec3ed98240fb1d633de11` on
`codex/phase8/P8BCSharedSegmentDesign`; independent review passed. It limits
persistent Person positions and trip endpoints to stable Hex, Location, and
Crossing identities, and excludes runtime-only SubLocation references. That
resolves the reconstruction blocker without expanding P8-C into a LocalTopology
migration. The contract is not part of this P8-A integration candidate.

P8-B can now begin on an isolated feature branch. P8-C design/discovery may
proceed, but its implementation must wait for P8-B's published and
independently reviewed API seam. Integrate B before C because transit progress
consumes B's stable boundary/option identities.
