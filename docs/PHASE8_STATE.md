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

- **P8-B — Factual Passages:** implementation candidate
  `fe46a8e19bb04ddd487b067c6ea068db7d59d487` is published on
  `codex/phase8/P8BFactualPassages`. Its focused suites passed 44/44 with no
  failures or skips; `git diff --check` passed. Independent implementation
  review is pending. The reviewed Crossing identity/anchor diagnostic seam is
  at `99ddc300e13bd15925c1d43218b665e858845d99`; mutable option, barrier, and
  crossing-condition projections remain part of the named diagnostics
  integration before P8-B promotion.
- **P8-C — Legacy Anchors and Civil Presence:** implementation candidate
  `a33d4a7d548836b58578dee496b612194109a232` is published on
  `codex/phase8/P8CLegacyAnchorsCivilPresence`, based on the independently
  reviewed Crossing/API seam. Its focused suite passed 4/4 and
  `git diff --check` passed. Independent implementation review and shared
  diagnostics integration are pending; P8-C is not promoted.
- **P8-D — Knowledge and Route Plan:** the technical design at
  `6800d3d289e2f8be730f082ee7457c518ed22050` passed independent review and is
  included in this documentation integration. It defines Hex-only route
  endpoints, deterministic same-subject observation resolution, and excludes
  a traversal from new candidates when the actor's resolved belief is
  `KnownUnavailable`. Implementation waits for promoted P8-B and P8-C
  capabilities and their published APIs.
- **P8-E — Civil Travel Vertical Slice:** the technical design at
  `4b7127f57d354c851e4d8ddaaeb27e8fbd51686c` passed independent review and is
  included in this documentation integration. Its proving scenario uses
  explicit actor-known estimates with an inclusive one-day freshness window;
  replan relies on P8-D's accepted `KnownUnavailable` rule. Implementation
  waits for promoted P8-B/C/D capabilities and their published APIs.

The accepted P8-B/C shared segment contract at
`5faa5817a11b0ae7412ec3ed98240fb1d633de11` on
`codex/phase8/P8BCSharedSegmentDesign` passed independent review and is included
in this documentation integration. It limits persistent Person positions and
trip endpoints to stable Hex, Location, and Crossing identities, and excludes
runtime-only SubLocation references. That resolves the reconstruction blocker
without expanding P8-C into a LocalTopology migration. The included contract
and P8-D/P8-E technical designs approve design semantics only and do not mark
their capabilities implemented.

P8-B and P8-C may proceed in isolated feature worktrees against the reviewed
stable identity seam. Integrate B before C because transit progress consumes
B's boundary/option identities, then compose the shared runtime and diagnostics
projections through one named integration owner. P8-D implementation follows
promoted B/C capabilities; P8-E implementation follows promoted B/C/D
capabilities and the Phase 8 integration validation gate.
