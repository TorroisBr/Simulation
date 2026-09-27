# Phase 9 — IN PROGRESS

## Current authority and candidate

- Current canonical architecture baseline: `c285466c355103d3637ac165246591b72eb7bda0`.
- Upstream canonical baseline: `codex/phase8/canonical` at
  `470667d37863384edadb3d93ef64d8004aff46a3`.
- Current P9 integration candidate: `codex/phase9/GenesisAuthoredBootstrapIntegration`.
- P9-A candidate integration record: `12a1e0dfb8b856525a84a9f8373711e9584f95a9`.
- P9-A implementation candidate: `974a8d72a158962619d7ba8aa1ccf854eeadd47e`.
- **P9-A status: VALIDATED_CANDIDATE; canonical promotion awaits the initial human approval required by `docs/EXECUTION_MODEL.md`.**
- **Phase 9 remains open.** This record establishes no Phase 9 canonical branch or promoted capability and does not claim the full Phase 9 objective is closed.

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

P9-A and the unpromoted P11 Actor Choice candidate were also checked together
on the validation-only ref `codex/phase9/P11CompatibilityValidation` at
`004c6f99e72c63dfd374fca9197ba7ac0818ebca`. That ref passed Actor Action
Choice 6/6, Actor Choice 24/24, P9 bootstrap 10/10, all EditMode 1738/1738,
complete Smoke 5/5, and `git diff --check`. This is compatibility evidence;
it does not promote P11, satisfy its separate promotion gate, or change its
canonical status.

No long-run suite was required: P9-A introduces no daily-loop behavior.

## Dependency and downstream status

- P9-A's selected authored profile does not consume new P8-owned geography,
  route, travel, or presence facts. P8's promoted baseline is recorded for
  ancestry and regression compatibility, not as an invented P9-A capability
  dependency.
- P10 implementation still waits for promoted P9 genesis and its relevant
  promoted P8-C local/anchor capability. Its reviewed entry architecture may
  continue as design evidence; it is not implementation readiness.
- P11 Actor Choice remains a separate unpromoted candidate. Combined
  compatibility validation does not merge or promote it.
- P18, P19 and P20 work remains governed by the explicit dependency edges in
  the Roadmap and Phase Briefs; none is pulled forward by P9-A.

Canonical promotion requires the exact validated integration tip, clean and
verified local/remote state, this State record, independent review, retained
validation evidence and explicit human approval. Phase closure requires its
own objective/checkpoint review and State update; P9-A promotion alone is not
Phase 9 closure.
