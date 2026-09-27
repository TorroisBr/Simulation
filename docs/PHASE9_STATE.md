# Phase 9 — CLOSED

## Current authority and candidate

- Current canonical architecture baseline: `c285466c355103d3637ac165246591b72eb7bda0`.
- Upstream canonical baseline: `codex/phase8/canonical` at
  `470667d37863384edadb3d93ef64d8004aff46a3`.
- Promoted P9 branch: `codex/phase9/canonical`.
- Source integration branch: `codex/phase9/GenesisAuthoredBootstrapIntegration`.
- P9-A candidate integration record: `12a1e0dfb8b856525a84a9f8373711e9584f95a9`.
- P9-A implementation candidate: `974a8d72a158962619d7ba8aa1ccf854eeadd47e`.
- P9-A code promotion commit: `43f08b3dfbf042380c2f8a8b037bbf3ebd309ccb` on `codex/phase9/canonical`.
- **P9-A status: PROMOTED.** The user approved promotion of the reviewed candidate; this State-only commit records that promotion.
- **Phase 9 status: CLOSED** within the approved authored-bootstrap-first-delivery scope. P9-A is the sole approved Phase 9 checkpoint and is canonical. Local/pre-start content generation is tracked by P10; public mod loading, retrofit and runtime expansion remain deferred consumers, not uncompleted P9 checkpoints.

## Phase-level closure review

- Independent closure review: **PASS** for this State/Brief closure candidate,
  against exact canonical baseline
  `988b6f5d14e12359e93464bae5e0048ca970ad86`.
- The review confirmed that P9-A satisfies the approved initial-world objective
  for the selected existing authored Unity bootstrap profile, that no other
  Phase 9 checkpoint is approved or marked must-complete, and that later
  generation algorithms/content and the P19 loader remain outside this Phase's
  closure boundary.
- The closure boundary preserves the approved limits: no new procedural
  terrain, settlements, population, local topology, generated backstory,
  runtime expansion or new gameplay. P9-A's existing authored content and
  deterministic pre-start pipeline remain canonical.
- Required implementation, integration and compatibility evidence is recorded
  below and in `docs/design/PHASE9A_AUTHORED_BOOTSTRAP_CHECKPOINT.md`.
- Formal State marker: **CLOSED**. This is a State-only closure record; no
  runtime files or P9-A behavior changed.

## P9-A — Authored Bootstrap Genesis v1

P9-A proves the dependency-aware deterministic pre-start pipeline against the
existing authored Unity bootstrap. Its scope, stage contract, selected-input
inventory, compatibility identity, provenance, atomic publication boundary,
reconstruction inventory and acceptance criteria are defined in
`docs/design/PHASE9A_AUTHORED_BOOTSTRAP_CHECKPOINT.md`. The approved first
delivery adds no generated terrain, settlement, population, local topology,
pre-simulation backstory, runtime expansion or new gameplay. P19 loader/public
API mechanics remain deferred. P9-A has no P18, P19 or P20 capability
dependency; the built-in profile creates no timed activities or
multi-participant state.

Current extensibility constraints are review constraints: explicit stage
identities, versions, inputs/outputs, declared dependencies, deterministic
ordering, compatibility and provenance, with outputs published through their
owning domain authorities. The checkpoint adds no speculative extension
registry or adversarial security layer.

### Candidate ancestry and review

- The implementation candidate was refreshed to include the then-approved
  Phase 8 State-only closure and subsequently integrated from current Phase 8
  canonical `470667d`; the architecture contract remains `c285466`.
- Independent checkpoint-contract review: **PASS** for the reviewed contract
  recorded at `f14586b`.
- Independent implementation review: **PASS** on exact code candidate
  `974a8d72a158962619d7ba8aa1ccf854eeadd47e`.
- Independent integration evidence review: **PASS** on the candidate record
  before this Phase State was added.
- P8 closure State wording change `77f3e1a → 470667d` was classified
  `UPSTREAM_IRRELEVANT` to P9 runtime behavior. No P9 runtime contract changed.

### Integration validation

The integration branch passed the following gates on the P9-A candidate:

| Gate | Result |
|---|---:|
| `SimulationBootstrapCompositionTests` | 10/10 passed |
| `SpatialRoutePlanningTests` | 20/20 passed |
| All EditMode tests | 1708/1708 passed |
| Complete official `Smoke` filter | 5/5 passed |
| `git diff --check` | Passed |

P9-A and the P11 Actor Choice candidate were also checked together
on the validation-only ref `codex/phase9/P11CompatibilityValidation` at
`004c6f99e72c63dfd374fca9197ba7ac0818ebca`. That ref passed Actor Action
Choice 6/6, Actor Choice 24/24, P9 bootstrap 10/10, all EditMode 1738/1738,
complete Smoke 5/5, and `git diff --check`. This is compatibility evidence.
P11 Actor Choice was subsequently promoted to `codex/phase11/canonical` at
`0803670cfa2c39163b54ff46a21daa06df5a16f6`; its post-promotion changes are
State-only, so the tested P11 runtime remains the promoted implementation.

No long-run suite was required: P9-A introduces no daily-loop behavior.

## Dependency and downstream status

- P9-A's selected authored profile does not consume new P8-owned geography,
  route, travel, or presence facts. P8's promoted baseline is recorded for
  ancestry and regression compatibility, not as an invented P9-A capability
  dependency.
- P9-A promotion satisfies the P9 genesis prerequisite for P10, and the
  relevant P8-C local/anchor capability is canonical. P10 may advance under
  its own reviewed entry architecture, checkpoint scope and technical gates.
- P11 Actor Choice is independently canonical at `0803670`; the combined
  compatibility validation confirms the promoted runtime composition with P9-A.
- P18, P19 and P20 work remains governed by the explicit dependency edges in
  the Roadmap and Phase Briefs; none is pulled forward by P9-A.

Promotion record: the approved integration candidate was promoted to
`codex/phase9/canonical` at `43f08b3`; local and remote refs were verified.
Phase-level closure is separately reviewed and recorded above, with the
approved scope limits and deferred consumers retained.
