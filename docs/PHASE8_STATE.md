# Phase 8 — IN PROGRESS

## Canonical baseline and current candidate

- The verified Phase 7 canonical baseline entering Phase 8 is
  `1f4651e99db2c357dd3be3c6b9284d104379f706` on `codex/phase7/canonical`.
- P8-A was promoted from integration commit
  `094971b` on `codex/phase8/P8AGeographyIntegration` to the new
  `codex/phase8/canonical` branch. The canonical branch is pushed and its
  remote SHA was verified; the Phase 7 canonical baseline remains preserved.
- Phase 8 remains open. P8-A through P8-C are canonical. P8-D is design-approved
  and has a separate integration candidate; its pre-refresh READY classification
  now requires the targeted architecture impact review recorded below. P8-E is
  design-approved and waits for promoted P8-D capability and that impact review.

## Architecture requirement refresh — 2026-09-26

The user approved intraday temporal simulation, player-owned code extensibility
and dependency-aware extensible genesis. This is a documentation-only alignment
against canonical `ed7a40a86a6a16e9f4fda75703470c38135fda0e`; it adds no temporal,
mod or generation capability and does not alter the retained validation records.
See `architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md`, the Roadmap and P18/P19
Briefs for the new dependencies and candidate impact.

P8-A/B/C remain canonical and valid in their delivered scope. P8-D's separate
integration candidate requires targeted temporal-profile revalidation before
promotion; this update does not mark that candidate canonical. P8-E's existing
explicit-operation proving schedule remains a bounded transitional slice, with
automatic intraday travel deferred to P18-D. Do not promote either candidate by
assuming its pre-refresh readiness is sufficient without the recorded impact
review. The spatial A → B/C → D → E capability graph remains unchanged.

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

- **P8-B — Factual Passages:** feature commit
  `3814d814087d97f28de75447740b3db715532ed6` is published on
  `codex/phase8/P8BFactualPassages`; independent implementation review: **PASS**.
  Its focused suites passed 44/44 with no failures or skips. P8-B is canonical
  through combined integration implementation commit
  `1c84519740db8a245e103678b38f692e13522383`. Passage option, barrier, and
  crossing condition facts are included in the integration snapshot, canonical
  output, formatter, diff, and invariant validation.
- **P8-C — Legacy Anchors and Civil Presence:** feature commit
  `d238f4bcef9faaf622329130f02b706e19bb8b4d` is published on
  `codex/phase8/P8CLegacyAnchorsCivilPresence`; independent implementation
  review: **PASS**. Its focused suite passed 4/4. P8-C is canonical through
  combined integration implementation commit
  `1c84519740db8a245e103678b38f692e13522383`. Person At/InTransit positions
  and City/Site anchor bindings are composed through `SimulationRuntime`,
  cloned against the runtime-owned authorities, mutation-guard bound, and
  included in spatial diagnostics.
- **P8-D — Knowledge and Route Plan:** the technical design at
  `6800d3d289e2f8be730f082ee7457c518ed22050` passed independent review and is
  included in this documentation integration. It defines Hex-only route
  endpoints, deterministic same-subject observation resolution, and excludes
  a traversal from new candidates when the actor's resolved belief is
  `KnownUnavailable`. P8-B and P8-C satisfy its capability dependencies.
  The 2026-09-26 architecture refresh adds targeted temporal-profile
  revalidation of its separate integration candidate before promotion.
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

## P8-B/C integration — canonical

- Integration branch: `codex/phase8/P8BCSpatialDiagnosticsIntegration`.
- Integration order: reviewed P8-B, then reviewed P8-C, then the accepted
  design documentation integration. The resolved shared composition preserves
  one spatial/passage authority and uses its actual passage registry as the
  runtime transit resolver. Contextual passage evaluation remains query output
  and is not persisted in World Truth. Runtime-owned City/Site anchor bindings
  reject unregistered owners, and passage/boundary diagnostic keys encode each
  nullable string component with a length prefix to remain injective for
  delimiter-bearing IDs.
- Promoted implementation commit: `1c84519740db8a245e103678b38f692e13522383`.
- Canonical promotion: **COMPLETE**. `codex/phase8/canonical` was fast-forwarded
  to validated integration record `799194e5bad0d2e52406cb7aa4b9198ef8923d9d`
  on 2026-09-24 and pushed; this state update records that promotion.
- Independent integration code review: **PASS**. Independent validation
  evidence review: **PASS**.
- Validation record commit: `ea09f5d305753486246d9e811dc8bf0f20cb2d46`.
- Validation evidence at the final candidate code state:

| Gate | Invocation | Result | Retained XML and log |
|---|---|---:|---|
| Focused spatial diagnostics and P8-B/C | `-Mode EditMode -TestFilter Spatial -ResultsDirectory .\Library\ValidationResults\P8BC` | 79/79 passed; 0 failed, 0 skipped | `Library/ValidationResults/P8BC/EditMode-20260924-181137-4cd4afb83d844cfaba185f6601c0b6ef.xml` and `.log` |
| Person spatial presence/runtime snapshots | `-Mode EditMode -TestFilter PersonSpatialPresenceTests -ResultsDirectory .\Library\ValidationResults\P8BC` | 8/8 passed; 0 failed, 0 skipped | `Library/ValidationResults/P8BC/EditMode-20260924-181219-7b0987f62da7465282eac7d709709923.xml` and `.log` |
| World state diagnostics | `-Mode EditMode -TestFilter WorldStateDiagnostics -ResultsDirectory .\Library\ValidationResults\P8BC` | 54/54 passed; 0 failed, 0 skipped | `Library/ValidationResults/P8BC/EditMode-20260924-181201-1532753bcff94bad8abbc8bbb941725d.xml` and `.log` |
| ALL EditMode | `-Mode EditMode -All -ResultsDirectory .\Library\ValidationResults\P8BC` | 1679/1679 passed; 0 failed, 0 skipped | `Library/ValidationResults/P8BC/EditMode-20260924-181240-912de8769bd84132aca7d1a7846dcbb3.xml` and `.log` |
| Official complete Smoke | `-Mode EditMode -TestFilter Smoke -ResultsDirectory .\Library\ValidationResults\P8BC` | 5/5 passed; 0 failed, 0 skipped | `Library/ValidationResults/P8BC/EditMode-20260924-181318-ea5a008cf1d64ea29c98804bd2cefbed.xml` and `.log` |

All listed final-gate XML files are parseable passed reports with coherent
counts. `git diff --check` is part of the final candidate gate. No daily-loop
or long-horizon behavior changed. P8-D implementation follows promoted B/C;
P8-E follows promoted B/C/D and the Phase 8 integration validation gate.
