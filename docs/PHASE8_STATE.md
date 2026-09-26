# Phase 8 — IN PROGRESS

## Latest architecture baseline — 2026-09-26

The current canonical architecture update is `c285466c355103d3637ac165246591b72eb7bda0`; it supersedes the prior architecture baseline `4b6dd1d38cffeaf3cc1ac3effea0f8ede8771194`. The Phase 8 canonical branch was advanced to P8-E promotion commit `d95b60d174cb0b17df09e2775b3cbd134c74b21f`, which includes the update, P8-D promotion, and P8-E implementation. Both `architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md` and `architecture/MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md` remain current constraints for review and future integration.

Earlier dated sections preserve their historical baselines; use this section and the latest canonical Phase 8 State when resolving any apparent conflict.

## Canonical baseline and current candidate

- The verified Phase 7 canonical baseline entering Phase 8 is
  `1f4651e99db2c357dd3be3c6b9284d104379f706` on `codex/phase7/canonical`.
- P8-A was promoted from integration commit
  `094971b` on `codex/phase8/P8AGeographyIntegration` to the new
  `codex/phase8/canonical` branch. The canonical branch is pushed and its
  remote SHA was verified; the Phase 7 canonical baseline remains preserved.
- Phase 8 remains open. P8-A through P8-E are canonical. P8-D was promoted from
  integration implementation commit `dccff74831b5ecfa55d32f142185a437afb5579a`
  on `codex/phase8/P8DArchitectureRefreshIntegration`, based on
  `c285466c355103d3637ac165246591b72eb7bda0`, after targeted temporal-profile
  revalidation, independent review, full validation, and final promotion check.
  The candidate State/review record was `d1818fb475de271713efdd480d6a994353a705f2`.
  P8-E's B/C/D capability dependencies are promoted. P8-E was promoted from
  its validated integration candidate at `d95b60d`; the promotion record is
  below.

## Architecture requirement refresh — 2026-09-26

The user approved intraday temporal simulation, player-owned code extensibility
and dependency-aware extensible genesis. This is a documentation-only alignment
against canonical `ed7a40a86a6a16e9f4fda75703470c38135fda0e`; it adds no temporal,
mod or generation capability and does not alter the retained validation records.
See `architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md`, the Roadmap and P18/P19
Briefs for the new dependencies and candidate impact.

P8-A/B/C/D/E are canonical and valid in their delivered scopes. P8-D's
refreshed integration was promoted after the temporal-profile revalidation and
gates recorded below. P8-E's explicit-operation proving schedule remains a
bounded transitional slice, with automatic intraday travel deferred to P18-D.
The spatial A → B/C → D → E capability graph remains unchanged.

## Multi-participant requirement refresh — 2026-09-26

The additional architecture update starts from canonical
`4b6dd1d38cffeaf3cc1ac3effea0f8ede8771194`. Phase 20 layers temporary
multi-participant activities over relevant Phase 18 temporal contracts;
Phase 18 preserves instance/participant boundaries without implementing that
coordination. See `architecture/MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md`.

This adds no P20 dependency to P8's individual civil traveler and no new
promotion gate beyond the already recorded temporal impact review. Existing
P8 implementation/validation scopes are preserved; future traveling-together
integration is a separate consumer.
P18 has a noncanonical integration State for its validated P18-A candidate;
P20 has no State because it has no delivered implementation.

Targeted P8-E design-impact revalidation against canonical `c285466` and both
alignment records passed for the reviewed technical design at
`4b7127f57d354c851e4d8ddaaeb27e8fbd51686c`. Its one-Person explicit-operation
journey remains a bounded profile with no autonomous daily progression or
per-day travel cap, and it establishes no permanent Activity-to-Actor relation.
P8-B/C/D capabilities and their published APIs are promoted. P8-E's
implementation and final validation are recorded below and are now canonical.
Automatic intraday travel remains a P18-D consumer.

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
  The 2026-09-26 architecture refresh required targeted temporal-profile
  revalidation, completed on the refreshed candidate recorded below. Its day-
  based freshness, estimate and current-day acceptance semantics remain bounded
  to the explicit profile; they do not impose a permanent per-day travel/action
  limit. The individual PersonId route-plan slice has no P20 dependency.
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

## P8-D architecture-refresh integration — promoted

- Integration branch: `codex/phase8/P8DArchitectureRefreshIntegration`, based on
  current canonical architecture/code commit
  `c285466c355103d3637ac165246591b72eb7bda0`.
- Promoted implementation commit: `dccff74831b5ecfa55d32f142185a437afb5579a`.
- Integration State and review record at promotion:
  `d1818fb475de271713efdd480d6a994353a705f2`.
- Canonical promotion: **COMPLETE** by fast-forward on 2026-09-26. Local and
  remote synchronization is verified after the State promotion record below.
- The earlier candidate on
  `codex/phase8/P8DKnowledgeRoutePlanIntegration` is preserved. Only its three
  reviewed code commits were transplanted: feature `2a4e765e8a325297c18505e07ef9a93bbbade1d5`,
  runtime/diagnostics integration `2faf3f873a7a434dc7330191e9292d09f6dd3014`,
  and future-date correction `19fc34b395f45819b4ed10387ef9dcadcef08108`.
  No older Phase docs or roadmaps were merged over the current canonical docs.
- Independent architecture impact review confirmed that day-based observation
  freshness, route estimates and plan acceptance remain explicit daily-profile
  inputs; P8-D adds no daily-loop travel progression. Route planning and plan
  history are PersonId-owned, with no Activity-to-Actor cardinality contract.
  Same-day route-plan replacement remains possible; a stale selection from a
  different current world day is rejected. P20 adds no gate to this individual
  traveler. Automatic intraday travel remains a future P18-D consumer.
- The previously found future-dated composition gap remains closed: runtime
  composition rejects future Knowledge/plan history while preserving valid
  past-dated state, and runtime invariants report future-dated owned entries.
  The refreshed candidate's focused and final gates passed with zero failures
  or skips:

| Gate | Result | Retained XML and log |
|---|---:|---|
| `SpatialRoutePlanning` | 17/17 | `Library/ValidationResults/P8DArchitectureRefresh/EditMode-20260926-164348-0fc5d944329945dcb7dafd90a9c0e590.xml` and `.log` |
| ALL EditMode | 1696/1696 | `Library/ValidationResults/P8DArchitectureRefresh/EditMode-20260926-164648-ab39afe3e39f4bb38941fa30a1e99f34.xml` and `.log` |
| Official complete Smoke | 5/5 | `Library/ValidationResults/P8DArchitectureRefresh/EditMode-20260926-164953-cd6f3903f2504c0a999df903aeaafb18.xml` and `.log` |

The daily loop and long-horizon behavior remain unchanged, so no long-run suite
was run. Independent final review against refreshed candidate `8186e8d` passed;
`git diff --check` passes. The final promotion-readiness check also passed
against this candidate and its retained reports. The canonical branch was
advanced to the validated integration, and this State update records the
promotion.

## P8-E implementation integration — promoted

- Canonical branch: `codex/phase8/canonical`. User-approved fast-forward promotion commit: `d95b60d174cb0b17df09e2775b3cbd134c74b21f`; local and `origin` were verified synchronized at that commit. This canonical State update records the promotion.
- Integration branch: `codex/phase8/P8EIntegration`. Validated code candidate: `d4c4c4ff22624785f479fd619a89694c45fab78a`; candidate State/review record: `e7beb5c93908d928c8c85090d6249e473f8296ef`; promotion branch tip: `d95b60d174cb0b17df09e2775b3cbd134c74b21f`; original P8-E integration commit: `af0fd3db826ef3fcf935eca3d21f75c411d83b46`.
- Feature candidate: `16abef139ade3b6d54fe229cf6b5d8831a367298` on `codex/phase8/P8ECivilTravel`. The integration tree exactly matches the tested feature tree (`ea32ffb57ac380d60a760cb69f661ea984101519`).
- Independent implementation review: **PASS**. The runtime-bound `PersonRoutePlanStore.TryAcceptPlan` rejects replacement while its Person is in transit; the check occurs before Knowledge/revision checks or mutation. Runtime composition and cloning preserve the position provider; standalone stores retain existing behavior. Regression tests prove rejection leaves position and plan revisions/history/status unchanged, and explicit replanning at a stable Hex succeeds.
- Targeted architecture revalidation and independent code review of the runtime-day guard at `d4c4c4f`: **PASS**. Interruption rejects a supplied day that differs from `SimulationRuntime.CurrentDay` before preparation, uses runtime-authoritative day for evidence validation/preparation, and has no P11 actor-choice or transit changes in its diff. The regression verifies stale and future supplied days leave position, active plan, Knowledge revision, and observation count unchanged.
- Scope remains the reviewed one-Person explicit-operation slice: current passage truth is re-evaluated at each attempted segment; position/transit and plan lifecycle mutations commit atomically; optional supported same-day Knowledge evidence joins interruption atomically. No `AdvanceDay` progression, daily travel cap, per-Activity cardinality, P18 blanket gate, P19 loader/API, or P20 dependency was added.
- Original P8-E focused gates passed on the pre-guard integration source tree at `af0fd3d`; the affected route-planning suite and final gates were rerun on exact candidate HEAD `d4c4c4f`:

| Gate | Result | Retained XML |
|---|---:|---|
| Original P8-E route lifecycle | 18/18 | `Library/ValidationResults/P8E/EditMode-20260926-183948-9b83c5c895c14c96bb982585d39e29f7.xml` (pre-guard `af0fd3d`) |
| Original Spatial | 97/97 | `Library/ValidationResults/P8E/EditMode-20260926-184007-424745fbc44b46d1b3b8b7dd44478179.xml` (pre-guard `af0fd3d`) |
| Original Travel | 101/101 | `Library/ValidationResults/P8E/EditMode-20260926-184023-66955609ade847658b9412387ffb645b.xml` (pre-guard `af0fd3d`) |
| SpatialRoutePlanningTests | 20/20 | `Temp/ValidationResults/EditMode-20260926-205841-ba66dd806dfc405db07e6d152c476545.xml` (`d4c4c4f`) |
| ALL EditMode | 1699/1699 | `Temp/ValidationResults/EditMode-20260926-210019-f2a0e259d76243ae83058fca59bdcfcc.xml` (`d4c4c4f`) |
| Official complete Smoke | 5/5 | `Temp/ValidationResults/EditMode-20260926-210059-01db1fc4b0314ddcb6d8cd0de4c81728.xml` (`d4c4c4f`) |

`git diff --check c5b2e06 d4c4c4f` passed. No long-run suite was required because the daily loop and long-horizon behavior remain unchanged. The user approved canonical promotion after the complete candidate, independent review, and validation evidence were ready; the canonical branch was fast-forwarded and pushed to `d95b60d`. Independent READY work continues under the refreshed DAG.
